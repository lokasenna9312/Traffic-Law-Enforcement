using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace Traffic_Law_Enforcement
{
    internal static class KeybindingSaveDiagnosticsPatches
    {
        private const string HarmonyId = "Traffic_Law_Enforcement.KeybindingSaveDiagnosticsPatches";
        private const int kMaxTrackedDepth = 12;
        private const int kMaxSaveBreadcrumbs = 20;
        private const int kMaxGlobalEvents = 40;
        private const int kMaxRecentGetterEvents = 60;

        private static readonly AsyncLocal<List<string>> s_DiffObjectStack = new AsyncLocal<List<string>>();
        private static readonly AsyncLocal<string> s_CurrentSettingAsset = new AsyncLocal<string>();
        private static readonly AsyncLocal<List<string>> s_SaveBreadcrumbs = new AsyncLocal<List<string>>();
        private static readonly HashSet<string> s_LoggedFailures = new HashSet<string>(StringComparer.Ordinal);
        private static readonly object s_LogGate = new object();
        private static readonly List<string> s_GlobalSaveEvents = new List<string>();
        private static readonly List<string> s_RecentGetterEvents = new List<string>();
        private static int s_KeybindingBindingsGetterCallCount;

        private static readonly FieldInfo s_KeybindingSettingsIsDefaultField =
            AccessTools.Field(typeof(Game.Settings.KeybindingSettings), "m_IsDefault");

        private static readonly MethodInfo s_KeybindingSettingsBindingsGetter =
            AccessTools.PropertyGetter(typeof(Game.Settings.KeybindingSettings), "bindings");

        private static readonly MethodInfo s_KeybindingSettingsBindingsSetter =
            AccessTools.PropertySetter(typeof(Game.Settings.KeybindingSettings), "bindings");

        private static readonly MethodInfo s_KeybindingSettingsSetDefaultsMethod =
            AccessTools.Method(typeof(Game.Settings.KeybindingSettings), "SetDefaults");

        private static readonly MethodInfo s_DiffObjectMethod =
            AccessTools.Method(
                AccessTools.TypeByName("Colossal.Json.DiffUtility"),
                "DiffObject",
                new[]
                {
                    typeof(object),
                    typeof(object),
                    AccessTools.TypeByName("Colossal.Json.ProxyObject"),
                    AccessTools.TypeByName("Colossal.Json.DiffUtility+Options"),
                });

        private static readonly MethodInfo s_TypeExtensionsGetMemberValueMethod =
            AccessTools.Method(
                AccessTools.TypeByName("Colossal.OdinSerializer.Utilities.TypeExtensions"),
                "GetMemberValue",
                new[] { typeof(MemberInfo), typeof(object) });

        private static readonly Type s_SaveSettingsHelperType =
            AccessTools.TypeByName("Colossal.IO.AssetDatabase.Internal.SaveSettingsHelper");

        private static readonly Type s_SettingAssetType =
            AccessTools.TypeByName("Colossal.IO.AssetDatabase.SettingAsset");

        private static readonly Type s_SettingAssetFragmentType =
            AccessTools.TypeByName("Colossal.IO.AssetDatabase.SettingAsset+Fragment");

        private static readonly Type s_AssetDatabaseType =
            AccessTools.TypeByName("Colossal.IO.AssetDatabase.AssetDatabase");

        private static readonly MethodInfo[] s_SettingAssetSaveMethods =
            s_SettingAssetType?
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => string.Equals(method.Name, "Save", StringComparison.Ordinal))
                .ToArray() ??
            Array.Empty<MethodInfo>();

        private static readonly MethodInfo[] s_AssetDatabaseSaveSettingsMethods =
            s_AssetDatabaseType?
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => string.Equals(method.Name, "SaveSettings", StringComparison.Ordinal))
                .ToArray() ??
            Array.Empty<MethodInfo>();

        private static readonly MethodInfo[] s_AssetDatabaseSaveSettingsWorkerMethods =
            s_AssetDatabaseType?.Assembly
                .GetTypes()
                .Where(type =>
                    string.Equals(type.Namespace, "Colossal.IO.AssetDatabase", StringComparison.Ordinal) &&
                    (type.FullName?.IndexOf("AssetDatabase", StringComparison.Ordinal) >= 0))
                .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(method => method.Name.IndexOf("<SaveSettings>", StringComparison.Ordinal) >= 0)
                .ToArray() ??
            Array.Empty<MethodInfo>();

        private static readonly MethodInfo[] s_AssetDatabaseAsyncMoveNextMethods =
            s_AssetDatabaseType?.Assembly
                .GetTypes()
                .Where(type =>
                    (type.FullName?.IndexOf("Colossal.IO.AssetDatabase", StringComparison.Ordinal) >= 0) &&
                    (type.FullName?.IndexOf("<SaveSettings>", StringComparison.Ordinal) >= 0 ||
                     type.FullName?.IndexOf("<DisposeAsync>", StringComparison.Ordinal) >= 0))
                .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(method => string.Equals(method.Name, "MoveNext", StringComparison.Ordinal))
                .ToArray() ??
            Array.Empty<MethodInfo>();

        private static Harmony s_Harmony;

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            if (s_DiffObjectMethod == null ||
                s_KeybindingSettingsBindingsGetter == null ||
                s_TypeExtensionsGetMemberValueMethod == null ||
                s_SettingAssetSaveMethods.Length == 0)
            {
                Mod.log.Warn("[KEYBIND_DIAG] Required reflection targets were not found. Diagnostics were not applied.");
                return;
            }

            try
            {
                s_Harmony = new Harmony(HarmonyId);
                s_Harmony.Patch(
                    s_DiffObjectMethod,
                    prefix: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(DiffObjectPrefix)),
                    finalizer: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(DiffObjectFinalizer)));

                s_Harmony.Patch(
                    s_KeybindingSettingsBindingsGetter,
                    prefix: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(KeybindingBindingsGetterPrefix)),
                    finalizer: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(KeybindingBindingsGetterFinalizer)));

                if (s_KeybindingSettingsBindingsSetter != null)
                {
                    s_Harmony.Patch(
                        s_KeybindingSettingsBindingsSetter,
                        prefix: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(KeybindingBindingsSetterPrefix)),
                        finalizer: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(KeybindingBindingsSetterFinalizer)));
                }

                if (s_KeybindingSettingsSetDefaultsMethod != null)
                {
                    s_Harmony.Patch(
                        s_KeybindingSettingsSetDefaultsMethod,
                        prefix: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(KeybindingSettingsSetDefaultsPrefix)),
                        finalizer: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(KeybindingSettingsSetDefaultsFinalizer)));
                }

                s_Harmony.Patch(
                    s_TypeExtensionsGetMemberValueMethod,
                    prefix: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(TypeExtensionsGetMemberValuePrefix)),
                    finalizer: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(TypeExtensionsGetMemberValueFinalizer)));

                foreach (MethodInfo method in s_SettingAssetSaveMethods)
                {
                    s_Harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(SettingAssetSavePrefix)),
                        finalizer: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(SettingAssetSaveFinalizer)));
                }

                foreach (MethodInfo method in s_AssetDatabaseSaveSettingsMethods)
                {
                    s_Harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(AssetDatabaseSaveSettingsPrefix)));
                }

                foreach (MethodInfo method in s_AssetDatabaseSaveSettingsWorkerMethods)
                {
                    s_Harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(AssetDatabaseSaveSettingsWorkerPrefix)),
                        finalizer: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(AssetDatabaseSaveSettingsWorkerFinalizer)));
                }

                foreach (MethodInfo method in s_AssetDatabaseAsyncMoveNextMethods)
                {
                    s_Harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(AssetDatabaseAsyncMoveNextPrefix)),
                        finalizer: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(AssetDatabaseAsyncMoveNextFinalizer)));
                }

                WriteDiagnosticLine(
                    "Keybinding save diagnostics patches applied. " +
                    $"bindingsGetter={DescribeMethod(s_KeybindingSettingsBindingsGetter)}, " +
                    $"bindingsSetter={DescribeMethod(s_KeybindingSettingsBindingsSetter)}, " +
                    $"setDefaults={DescribeMethod(s_KeybindingSettingsSetDefaultsMethod)}, " +
                    $"settingAssetSaveMethods={string.Join(" | ", s_SettingAssetSaveMethods.Select(DescribeMethod))}, " +
                    $"assetDatabaseSaveSettingsMethods={string.Join(" | ", s_AssetDatabaseSaveSettingsMethods.Select(DescribeMethod))}, " +
                    $"assetDatabaseSaveSettingsWorkerMethods={string.Join(" | ", s_AssetDatabaseSaveSettingsWorkerMethods.Select(DescribeMethod))}, " +
                    $"assetDatabaseAsyncMoveNextMethods={string.Join(" | ", s_AssetDatabaseAsyncMoveNextMethods.Select(DescribeMethod))}");
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                WriteDiagnosticLine("Failed to apply keybinding save diagnostics patches.", ex);
            }
        }

        public static void Remove()
        {
            if (s_Harmony == null)
            {
                return;
            }

            try
            {
                s_Harmony.UnpatchAll(HarmonyId);
                WriteDiagnosticLine("Keybinding save diagnostics patches removed.");
            }
            catch (Exception ex)
            {
                WriteDiagnosticLine("Failed to remove keybinding save diagnostics patches.", ex);
            }
            finally
            {
                s_Harmony = null;
                s_DiffObjectStack.Value = null;
                s_CurrentSettingAsset.Value = null;
                s_SaveBreadcrumbs.Value = null;
                lock (s_LogGate)
                {
                    s_LoggedFailures.Clear();
                    s_GlobalSaveEvents.Clear();
                    s_RecentGetterEvents.Clear();
                }
            }
        }

        private static void DiffObjectPrefix(object sourceObject)
        {
            List<string> stack = s_DiffObjectStack.Value;
            if (stack == null)
            {
                stack = new List<string>();
                s_DiffObjectStack.Value = stack;
            }

            if (stack.Count >= kMaxTrackedDepth)
            {
                return;
            }

            stack.Add(DescribeObject(sourceObject));
        }

        private static Exception DiffObjectFinalizer(Exception __exception)
        {
            List<string> stack = s_DiffObjectStack.Value;
            string currentSource = stack != null && stack.Count > 0
                ? stack[stack.Count - 1]
                : "<unknown>";

            if (__exception != null && ShouldLogDuringSettingsSave(__exception))
            {
                string key = $"DIFF|{BuildFailureSignature(__exception)}|{currentSource}|{s_CurrentSettingAsset.Value}";
                if (TryRegisterFailure(key))
                {
                WriteDiagnosticLine(
                    "DiffObject failed during settings save. " +
                    $"settingAsset={s_CurrentSettingAsset.Value ?? "<unknown>"}, " +
                    $"source={currentSource}, " +
                    $"diffStack={FormatCurrentDiffStack()}, " +
                    $"saveBreadcrumbs={FormatSaveBreadcrumbs()}, " +
                    $"globalEvents={FormatGlobalSaveEvents()}, " +
                    $"recentGetterEvents={FormatRecentGetterEvents()}, " +
                    $"exception={DescribeException(__exception)}, " +
                    $"rootCause={DescribeException(UnwrapException(__exception))}",
                    __exception);
                }
            }

            if (stack != null && stack.Count > 0)
            {
                stack.RemoveAt(stack.Count - 1);
                if (stack.Count == 0)
                {
                    s_DiffObjectStack.Value = null;
                }
            }

            return __exception;
        }

        private static Exception KeybindingBindingsGetterFinalizer(
            Game.Settings.KeybindingSettings __instance,
            Exception __exception)
        {
            if (__exception == null)
            {
                return null;
            }

            AppendAuxiliaryOnlyLine(
                "KeybindingSettings.bindings getter finalizer entered. " +
                $"instanceType={__instance?.GetType().FullName ?? "<null>"}, " +
                $"isDefault={TryGetIsDefaultValue(__instance)}, " +
                $"exceptionType={__exception.GetType().FullName}, " +
                $"exceptionMessage={__exception.Message}");

            string key =
                $"BINDINGSGETTER|{BuildFailureSignature(__exception)}|{DescribeObject(__instance)}|{DescribeKeybindingContext(__instance)}";
            if (TryRegisterFailure(key))
            {
                WriteDiagnosticLine(
                    "KeybindingSettings.bindings getter failed. " +
                    $"instance={DescribeObject(__instance)}, " +
                    $"keybindingContext={DescribeKeybindingContext(__instance)}, " +
                    $"members={DescribeObjectMembers(__instance)}, " +
                    $"diffStack={FormatCurrentDiffStack()}, " +
                    $"saveBreadcrumbs={FormatSaveBreadcrumbs()}, " +
                    $"globalEvents={FormatGlobalSaveEvents()}, " +
                    $"recentGetterEvents={FormatRecentGetterEvents()}, " +
                    $"exception={DescribeException(__exception)}, " +
                    $"rootCause={DescribeException(UnwrapException(__exception))}",
                    __exception);
            }

            return __exception;
        }

        private static void KeybindingBindingsGetterPrefix(Game.Settings.KeybindingSettings __instance)
        {
            int callCount = Interlocked.Increment(ref s_KeybindingBindingsGetterCallCount);
            bool hasSaveContext = HasSaveContext();
            string eventLine =
                "KeybindingSettings.bindings getter entered. " +
                $"call={callCount}, " +
                $"thread={Thread.CurrentThread.ManagedThreadId}, " +
                $"hasSaveContext={hasSaveContext}, " +
                $"instanceType={__instance?.GetType().FullName ?? "<null>"}, " +
                $"isDefault={TryGetIsDefaultValue(__instance)}, " +
                $"currentSettingAsset={s_CurrentSettingAsset.Value ?? "<unknown>"}, " +
                $"saveBreadcrumbs={FormatSaveBreadcrumbs()}";

            RecordRecentGetterEvent(eventLine);
            if (hasSaveContext || callCount <= 5)
            {
                AppendAuxiliaryOnlyLine(eventLine);
            }
        }

        private static void KeybindingBindingsSetterPrefix(
            Game.Settings.KeybindingSettings __instance,
            List<Game.Input.ProxyBinding> value)
        {
            string eventLine =
                "KeybindingSettings.bindings setter entered. " +
                $"thread={Thread.CurrentThread.ManagedThreadId}, " +
                $"hasSaveContext={HasSaveContext()}, " +
                $"instanceType={__instance?.GetType().FullName ?? "<null>"}, " +
                $"isDefault={TryGetIsDefaultValue(__instance)}, " +
                $"currentSettingAsset={s_CurrentSettingAsset.Value ?? "<unknown>"}, " +
                $"saveBreadcrumbs={FormatSaveBreadcrumbs()}, " +
                $"incomingBindings={DescribeBindingsSummary(value)}, " +
                $"stack={CaptureCompactStackTrace()}";

            RecordGlobalSaveEvent(eventLine);
            AppendAuxiliaryOnlyLine(eventLine);
        }

        private static Exception KeybindingBindingsSetterFinalizer(
            Game.Settings.KeybindingSettings __instance,
            List<Game.Input.ProxyBinding> value,
            Exception __exception)
        {
            if (__exception == null)
            {
                return null;
            }

            WriteDiagnosticLine(
                "KeybindingSettings.bindings setter failed. " +
                $"instance={DescribeObject(__instance)}, " +
                $"keybindingContext={DescribeKeybindingContext(__instance)}, " +
                $"incomingBindings={DescribeBindingsSummary(value)}, " +
                $"currentSettingAsset={s_CurrentSettingAsset.Value ?? "<unknown>"}, " +
                $"saveBreadcrumbs={FormatSaveBreadcrumbs()}, " +
                $"globalEvents={FormatGlobalSaveEvents()}, " +
                $"stack={CaptureCompactStackTrace()}, " +
                $"exception={DescribeException(__exception)}, " +
                $"rootCause={DescribeException(UnwrapException(__exception))}",
                __exception);

            return __exception;
        }

        private static void KeybindingSettingsSetDefaultsPrefix(Game.Settings.KeybindingSettings __instance)
        {
            string eventLine =
                "KeybindingSettings.SetDefaults entered. " +
                $"thread={Thread.CurrentThread.ManagedThreadId}, " +
                $"hasSaveContext={HasSaveContext()}, " +
                $"instanceType={__instance?.GetType().FullName ?? "<null>"}, " +
                $"isDefault={TryGetIsDefaultValue(__instance)}, " +
                $"currentSettingAsset={s_CurrentSettingAsset.Value ?? "<unknown>"}, " +
                $"saveBreadcrumbs={FormatSaveBreadcrumbs()}, " +
                $"currentBindings={DescribeBindingsSummary(SafeGetBindings(__instance))}, " +
                $"stack={CaptureCompactStackTrace()}";

            RecordGlobalSaveEvent(eventLine);
            AppendAuxiliaryOnlyLine(eventLine);
        }

        private static Exception KeybindingSettingsSetDefaultsFinalizer(
            Game.Settings.KeybindingSettings __instance,
            Exception __exception)
        {
            if (__exception == null)
            {
                return null;
            }

            WriteDiagnosticLine(
                "KeybindingSettings.SetDefaults failed. " +
                $"instance={DescribeObject(__instance)}, " +
                $"keybindingContext={DescribeKeybindingContext(__instance)}, " +
                $"currentBindings={DescribeBindingsSummary(SafeGetBindings(__instance))}, " +
                $"currentSettingAsset={s_CurrentSettingAsset.Value ?? "<unknown>"}, " +
                $"saveBreadcrumbs={FormatSaveBreadcrumbs()}, " +
                $"globalEvents={FormatGlobalSaveEvents()}, " +
                $"stack={CaptureCompactStackTrace()}, " +
                $"exception={DescribeException(__exception)}, " +
                $"rootCause={DescribeException(UnwrapException(__exception))}",
                __exception);

            return __exception;
        }

        private static Exception TypeExtensionsGetMemberValueFinalizer(
            MemberInfo member,
            object obj,
            Exception __exception)
        {
            bool keybindingSignal = HasKeybindingSignal(member, obj, __exception);
            if (__exception == null ||
                (!ShouldLogDuringSettingsSave(__exception) && !keybindingSignal))
            {
                return __exception;
            }

            Exception rootCause = UnwrapException(__exception);
            string key =
                $"GETMEMBER|{BuildFailureSignature(__exception)}|{member?.DeclaringType?.FullName}|{member?.Name}|{obj?.GetType().FullName}|{s_CurrentSettingAsset.Value}|{FormatCurrentDiffStack()}";
            if (!TryRegisterFailure(key))
            {
                return __exception;
            }

            string keybindingContext = DescribeKeybindingContext(obj);
            WriteDiagnosticLine(
                "Exception while resolving a member during settings save. " +
                $"settingAsset={s_CurrentSettingAsset.Value ?? "<unknown>"}, " +
                $"member={DescribeMember(member)}, " +
                $"object={DescribeObject(obj)}, " +
                $"keybindingSignal={keybindingSignal}, " +
                $"keybindingContext={keybindingContext}, " +
                $"diffStack={FormatCurrentDiffStack()}, " +
                $"saveBreadcrumbs={FormatSaveBreadcrumbs()}, " +
                $"globalEvents={FormatGlobalSaveEvents()}, " +
                $"recentGetterEvents={FormatRecentGetterEvents()}, " +
                $"exception={DescribeException(__exception)}, " +
                $"rootCause={DescribeException(rootCause)}",
                __exception);

            return __exception;
        }

        private static void TypeExtensionsGetMemberValuePrefix(MemberInfo member, object obj)
        {
            bool keybindingSignal =
                string.Equals(member?.Name, "bindings", StringComparison.Ordinal) ||
                IsKeybindingSettingsType(member?.DeclaringType) ||
                IsKeybindingSettingsType(obj?.GetType());

            if (!keybindingSignal)
            {
                return;
            }

            string eventLine =
                "TypeExtensions.GetMemberValue entered. " +
                $"thread={Thread.CurrentThread.ManagedThreadId}, " +
                $"member={DescribeMember(member)}, " +
                $"object={DescribeObject(obj)}, " +
                $"hasSaveContext={HasSaveContext()}, " +
                $"currentSettingAsset={s_CurrentSettingAsset.Value ?? "<unknown>"}, " +
                $"saveBreadcrumbs={FormatSaveBreadcrumbs()}";

            RecordRecentGetterEvent(eventLine);
            AppendAuxiliaryOnlyLine(eventLine);
        }

        private static bool ShouldLogDuringSettingsSave(Exception exception)
        {
            return HasSaveContext() || HasKeybindingSignal(null, null, exception);
        }

        private static bool HasKeybindingSignal(MemberInfo member, object obj, Exception exception)
        {
            Type objectType = obj?.GetType();
            if (IsKeybindingSettingsType(objectType) ||
                IsKeybindingSettingsType(member?.DeclaringType))
            {
                return true;
            }

            if (string.Equals(member?.Name, "bindings", StringComparison.Ordinal))
            {
                return true;
            }

            string exceptionText = exception?.ToString() ?? string.Empty;
            return
                exceptionText.IndexOf("KeybindingSettings", StringComparison.Ordinal) >= 0 ||
                exceptionText.IndexOf("get_bindings", StringComparison.Ordinal) >= 0;
        }

        private static bool HasSaveContext()
        {
            if (!string.IsNullOrWhiteSpace(s_CurrentSettingAsset.Value))
            {
                return true;
            }

            List<string> breadcrumbs = s_SaveBreadcrumbs.Value;
            return breadcrumbs != null && breadcrumbs.Count > 0;
        }

        private static bool IsKeybindingSettingsType(Type type)
        {
            return type != null &&
                   typeof(Game.Settings.KeybindingSettings).IsAssignableFrom(type);
        }

        private static Exception UnwrapException(Exception exception)
        {
            Exception current = exception;
            while (current is TargetInvocationException && current.InnerException != null)
            {
                current = current.InnerException;
            }

            return current ?? exception;
        }

        private static string DescribeException(Exception exception)
        {
            if (exception == null)
            {
                return "<null>";
            }

            if (exception.InnerException == null)
            {
                return $"{exception.GetType().Name}: {exception.Message}";
            }

            return
                $"{exception.GetType().Name}: {exception.Message} " +
                $"-> {DescribeException(exception.InnerException)}";
        }

        private static string BuildFailureSignature(Exception exception)
        {
            Exception rootCause = UnwrapException(exception);
            return $"{rootCause.GetType().FullName}|{rootCause.Message}";
        }

        private static string DescribeKeybindingContext(object obj)
        {
            if (!(obj is Game.Settings.KeybindingSettings keybindingSettings))
            {
                return "n/a";
            }

            bool? isDefault = null;
            if (s_KeybindingSettingsIsDefaultField != null &&
                s_KeybindingSettingsIsDefaultField.GetValue(keybindingSettings) is bool rawValue)
            {
                isDefault = rawValue;
            }

            return $"isDefault={isDefault?.ToString() ?? "unknown"}";
        }

        private static string FormatCurrentDiffStack()
        {
            List<string> stack = s_DiffObjectStack.Value;
            if (stack == null || stack.Count == 0)
            {
                return "<empty>";
            }

            return string.Join(" -> ", stack);
        }

        private static string DescribeMember(MemberInfo member)
        {
            if (member == null)
            {
                return "<null>";
            }

            return $"{member.DeclaringType?.FullName}.{member.Name}";
        }

        private static string DescribeObject(object obj)
        {
            if (obj == null)
            {
                return "<null>";
            }

            Type objectType = obj.GetType();
            string identity = null;

            try
            {
                PropertyInfo nameProperty = objectType.GetProperty(
                    "name",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (nameProperty != null && nameProperty.PropertyType == typeof(string))
                {
                    identity = nameProperty.GetValue(obj) as string;
                }

                if (string.IsNullOrWhiteSpace(identity))
                {
                    PropertyInfo idProperty = objectType.GetProperty(
                        "id",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    identity = idProperty?.GetValue(obj)?.ToString();
                }
            }
            catch
            {
            }

            return string.IsNullOrWhiteSpace(identity)
                ? objectType.FullName
                : $"{objectType.FullName}({identity})";
        }

        private static void AssetDatabaseSaveSettingsPrefix(MethodBase __originalMethod, object[] __args)
        {
            string message =
                $"AssetDatabase.SaveSettings started. method={DescribeMethod(__originalMethod)}, args={DescribeArguments(__args)}";
            RecordGlobalSaveEvent(message);
            WriteDiagnosticLine(message);
        }

        private static void AssetDatabaseSaveSettingsWorkerPrefix(MethodBase __originalMethod, object __instance, object[] __args)
        {
            string message =
                $"AssetDatabase.SaveSettings worker started. method={DescribeMethod(__originalMethod)}, " +
                $"instanceFields={DescribeInstanceFields(__instance)}, args={DescribeArguments(__args)}";
            RecordGlobalSaveEvent(message);
            WriteDiagnosticLine(message);
        }

        private static Exception AssetDatabaseSaveSettingsWorkerFinalizer(MethodBase __originalMethod, object __instance, Exception __exception)
        {
            if (__exception == null)
            {
                return null;
            }

            string key = $"SAVEWORKER|{BuildFailureSignature(__exception)}|{DescribeMethod(__originalMethod)}";
            if (TryRegisterFailure(key))
            {
                WriteDiagnosticLine(
                    "AssetDatabase.SaveSettings worker failed. " +
                    $"method={DescribeMethod(__originalMethod)}, " +
                    $"instanceFields={DescribeInstanceFields(__instance)}, " +
                    $"diffStack={FormatCurrentDiffStack()}, " +
                    $"saveBreadcrumbs={FormatSaveBreadcrumbs()}, " +
                    $"globalEvents={FormatGlobalSaveEvents()}, " +
                    $"recentGetterEvents={FormatRecentGetterEvents()}, " +
                    $"exception={DescribeException(__exception)}, " +
                    $"rootCause={DescribeException(UnwrapException(__exception))}",
                    __exception);
            }

            if (HasKeybindingSignal(null, null, __exception))
            {
                SettingsFileProtectionService.RestoreBackupIfCurrentLooksCorrupted(
                    $"AssetDatabase.SaveSettings worker failure in {DescribeMethod(__originalMethod)}");
            }

            return __exception;
        }

        private static void AssetDatabaseAsyncMoveNextPrefix(MethodBase __originalMethod, object __instance)
        {
            string inferredSettingAsset = InferAndTrackSettingAssetContext(__instance);
            string currentFragment = DescribeCurrentSettingAssetFragment(__instance);
            string currentFragmentOwner = DescribeCurrentSettingAssetOwner(__instance);
            string message =
                "AssetDatabase async MoveNext entered. " +
                $"method={DescribeMethod(__originalMethod)}, " +
                $"thread={Thread.CurrentThread.ManagedThreadId}, " +
                $"currentFragmentOwner={FirstNonBlank(currentFragmentOwner, "<unknown>")}, " +
                $"currentFragment={FirstNonBlank(currentFragment, "<unknown>")}, " +
                $"inferredSettingAsset={FirstNonBlank(inferredSettingAsset, "<unknown>")}, " +
                $"instanceFields={DescribeInstanceFields(__instance)}, " +
                $"stack={CaptureCompactStackTrace()}";

            RecordGlobalSaveEvent(message);
            AppendAuxiliaryOnlyLine(message);
        }

        private static Exception AssetDatabaseAsyncMoveNextFinalizer(MethodBase __originalMethod, object __instance, Exception __exception)
        {
            if (__exception == null)
            {
                return null;
            }

            string inferredSettingAsset = InferAndTrackSettingAssetContext(__instance);
            string currentFragment = DescribeCurrentSettingAssetFragment(__instance);
            string currentFragmentOwner = DescribeCurrentSettingAssetOwner(__instance);
            string key = $"ASYNCMOVENEXT|{BuildFailureSignature(__exception)}|{DescribeMethod(__originalMethod)}";
            if (TryRegisterFailure(key))
            {
                WriteDiagnosticLine(
                    "AssetDatabase async MoveNext failed. " +
                    $"method={DescribeMethod(__originalMethod)}, " +
                    $"thread={Thread.CurrentThread.ManagedThreadId}, " +
                    $"currentFragmentOwner={FirstNonBlank(currentFragmentOwner, "<unknown>")}, " +
                    $"currentFragment={FirstNonBlank(currentFragment, "<unknown>")}, " +
                    $"inferredSettingAsset={FirstNonBlank(inferredSettingAsset, "<unknown>")}, " +
                    $"instanceFields={DescribeInstanceFields(__instance)}, " +
                    $"stack={CaptureCompactStackTrace()}, " +
                    $"diffStack={FormatCurrentDiffStack()}, " +
                    $"saveBreadcrumbs={FormatSaveBreadcrumbs()}, " +
                    $"globalEvents={FormatGlobalSaveEvents()}, " +
                    $"recentGetterEvents={FormatRecentGetterEvents()}, " +
                    $"exception={DescribeException(__exception)}, " +
                    $"rootCause={DescribeException(UnwrapException(__exception))}",
                    __exception);
            }

            if (HasKeybindingSignal(null, null, __exception))
            {
                SettingsFileProtectionService.RestoreBackupIfCurrentLooksCorrupted(
                    $"AssetDatabase async MoveNext failure in {DescribeMethod(__originalMethod)}");
            }

            return __exception;
        }

        private static void SettingAssetSavePrefix(MethodBase __originalMethod, object __instance, object[] __args)
        {
            string settingAsset = DescribeSettingAsset(__instance);
            s_CurrentSettingAsset.Value = settingAsset;

            List<string> breadcrumbs = s_SaveBreadcrumbs.Value;
            if (breadcrumbs == null)
            {
                breadcrumbs = new List<string>();
                s_SaveBreadcrumbs.Value = breadcrumbs;
            }

            breadcrumbs.Add(settingAsset);
            if (breadcrumbs.Count > kMaxSaveBreadcrumbs)
            {
                breadcrumbs.RemoveAt(0);
            }

            string message =
                $"SettingAsset.Save started. method={DescribeMethod(__originalMethod)}, " +
                $"settingAsset={settingAsset}, args={DescribeArguments(__args)}";
            RecordGlobalSaveEvent(message);
            WriteDiagnosticLine(message);
        }

        private static Exception SettingAssetSaveFinalizer(MethodBase __originalMethod, object __instance, Exception __exception)
        {
            if (__exception == null)
            {
                return null;
            }

            string settingAsset = DescribeSettingAsset(__instance);
            string key = $"SAVE|{BuildFailureSignature(__exception)}|{DescribeMethod(__originalMethod)}|{settingAsset}";
            if (TryRegisterFailure(key))
            {
                WriteDiagnosticLine(
                    "SettingAsset.Save failed. " +
                    $"method={DescribeMethod(__originalMethod)}, " +
                    $"settingAsset={settingAsset}, " +
                    $"diffStack={FormatCurrentDiffStack()}, " +
                    $"saveBreadcrumbs={FormatSaveBreadcrumbs()}, " +
                    $"globalEvents={FormatGlobalSaveEvents()}, " +
                    $"recentGetterEvents={FormatRecentGetterEvents()}, " +
                    $"exception={DescribeException(__exception)}, " +
                    $"rootCause={DescribeException(UnwrapException(__exception))}",
                    __exception);
            }

            return __exception;
        }

        private static string DescribeSettingAsset(object settingAsset)
        {
            if (settingAsset == null)
            {
                return "<null>";
            }

            Type objectType = settingAsset.GetType();
            string name = TryReadProperty(settingAsset, objectType, "name");
            string identifier = TryReadProperty(settingAsset, objectType, "identifier");
            string path = TryReadProperty(settingAsset, objectType, "path");

            return
                $"{objectType.FullName}(" +
                $"name={FirstNonBlank(name, "unknown")}, " +
                $"identifier={FirstNonBlank(identifier, "unknown")}, " +
                $"path={FirstNonBlank(path, "unknown")})";
        }

        private static string TryReadProperty(object instance, Type objectType, string propertyName)
        {
            try
            {
                PropertyInfo property = objectType.GetProperty(
                    propertyName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                return property?.GetValue(instance)?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string FirstNonBlank(params string[] values)
        {
            for (int index = 0; index < values.Length; index += 1)
            {
                string value = values[index];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static string FormatSaveBreadcrumbs()
        {
            List<string> breadcrumbs = s_SaveBreadcrumbs.Value;
            if (breadcrumbs == null || breadcrumbs.Count == 0)
            {
                return "<empty>";
            }

            return string.Join(" -> ", breadcrumbs);
        }

        private static void RecordGlobalSaveEvent(string message)
        {
            lock (s_LogGate)
            {
                s_GlobalSaveEvents.Add(message);
                if (s_GlobalSaveEvents.Count > kMaxGlobalEvents)
                {
                    s_GlobalSaveEvents.RemoveAt(0);
                }
            }
        }

        private static string FormatGlobalSaveEvents()
        {
            lock (s_LogGate)
            {
                if (s_GlobalSaveEvents.Count == 0)
                {
                    return "<empty>";
                }

                return string.Join(" || ", s_GlobalSaveEvents);
            }
        }

        private static void RecordRecentGetterEvent(string message)
        {
            string timestampedLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff}] [KEYBIND_DIAG] {message}";

            try
            {
                lock (s_LogGate)
                {
                    s_RecentGetterEvents.Add(timestampedLine);
                    if (s_RecentGetterEvents.Count > kMaxRecentGetterEvents)
                    {
                        s_RecentGetterEvents.RemoveAt(0);
                    }

                    string logPath = ResolveRecentGetterLogPath();
                    string directory = Path.GetDirectoryName(logPath);
                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllLines(logPath, s_RecentGetterEvents);
                }
            }
            catch
            {
            }
        }

        private static string FormatRecentGetterEvents()
        {
            lock (s_LogGate)
            {
                if (s_RecentGetterEvents.Count == 0)
                {
                    return "<empty>";
                }

                return string.Join(" || ", s_RecentGetterEvents);
            }
        }

        private static bool TryRegisterFailure(string key)
        {
            lock (s_LogGate)
            {
                return s_LoggedFailures.Add(key);
            }
        }

        private static void WriteDiagnosticLine(string message, Exception exception = null)
        {
            string line = $"[KEYBIND_DIAG] {message}";

            try
            {
                if (exception == null)
                {
                    Mod.log.Info(line);
                }
                else
                {
                    Mod.log.Error(exception, line);
                }
            }
            catch
            {
            }

            try
            {
                string logPath = ResolveAuxiliaryLogPath();
                string directory = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string timestampedLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff}] {line}";
                lock (s_LogGate)
                {
                    File.AppendAllText(logPath, timestampedLine + Environment.NewLine);
                    if (exception != null)
                    {
                        File.AppendAllText(logPath, exception + Environment.NewLine);
                    }
                }
            }
            catch
            {
            }
        }

        private static string ResolveAuxiliaryLogPath()
        {
            string persistentDataPath = null;

            try
            {
                persistentDataPath = Application.persistentDataPath;
            }
            catch
            {
            }

            if (!string.IsNullOrWhiteSpace(persistentDataPath))
            {
                return Path.Combine(persistentDataPath, "Logs", "Traffic_Law_Enforcement.KeybindDiag.log");
            }

            return Path.Combine(AppContext.BaseDirectory, "Traffic_Law_Enforcement.KeybindDiag.log");
        }

        private static string ResolveRecentGetterLogPath()
        {
            string directory = Path.GetDirectoryName(ResolveAuxiliaryLogPath());
            if (!string.IsNullOrWhiteSpace(directory))
            {
                return Path.Combine(directory, "Traffic_Law_Enforcement.KeybindDiag.recent_getters.log");
            }

            return Path.Combine(AppContext.BaseDirectory, "Traffic_Law_Enforcement.KeybindDiag.recent_getters.log");
        }

        private static string CaptureCompactStackTrace()
        {
            try
            {
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(2, false);
                System.Diagnostics.StackFrame[] frames = stackTrace.GetFrames();
                if (frames == null || frames.Length == 0)
                {
                    return "<empty>";
                }

                return string.Join(
                    " <= ",
                    frames
                        .Select(frame => frame.GetMethod())
                        .Where(method => method != null)
                        .Take(12)
                        .Select(DescribeMethod));
            }
            catch (Exception ex)
            {
                return $"<failed:{ex.GetType().Name}:{ex.Message}>";
            }
        }

        private static string DescribeMethod(MethodBase method)
        {
            if (method == null)
            {
                return "<null>";
            }

            ParameterInfo[] parameters = method.GetParameters();
            return
                $"{method.DeclaringType?.FullName}.{method.Name}(" +
                string.Join(", ", parameters.Select(parameter => parameter.ParameterType.Name)) +
                ")";
        }

        private static string DescribeArguments(object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return "<none>";
            }

            return string.Join(", ", args.Select(DescribeArgument));
        }

        private static string DescribeArgument(object arg)
        {
            if (arg == null)
            {
                return "<null>";
            }

            return $"{arg.GetType().FullName}={arg}";
        }

        private static string DescribeBindingsSummary(List<Game.Input.ProxyBinding> bindings)
        {
            if (bindings == null)
            {
                return "<null>";
            }

            try
            {
                IEnumerable<string> preview = bindings
                    .Take(5)
                    .Select(binding => binding == null ? "<null>" : binding.ToString());

                string suffix = bindings.Count > 5 ? ", ..." : string.Empty;
                return $"count={bindings.Count}, preview=[{string.Join(" | ", preview)}{suffix}]";
            }
            catch (Exception ex)
            {
                return $"<failed:{ex.GetType().Name}:{ex.Message}>";
            }
        }

        private static List<Game.Input.ProxyBinding> SafeGetBindings(Game.Settings.KeybindingSettings instance)
        {
            if (instance == null)
            {
                return null;
            }

            try
            {
                return instance.bindings;
            }
            catch
            {
                return null;
            }
        }

        private static string DescribeInstanceFields(object instance)
        {
            if (instance == null)
            {
                return "<null>";
            }

            try
            {
                FieldInfo[] fields = instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fields.Length == 0)
                {
                    return instance.GetType().FullName;
                }

                return string.Join(
                    "; ",
                    fields.Select(field => $"{field.Name}={DescribeFieldValue(field.Name, field.GetValue(instance))}"));
            }
            catch (Exception ex)
            {
                return $"<failed:{ex.GetType().Name}:{ex.Message}>";
            }
        }

        private static string DescribeFieldValue(string fieldName, object value)
        {
            if (value == null)
            {
                return "<null>";
            }

            string enumeratorState = DescribeEnumeratorState(fieldName, value);
            if (!string.IsNullOrWhiteSpace(enumeratorState))
            {
                return enumeratorState;
            }

            string keyValuePairState = DescribeKeyValuePair(value);
            if (!string.IsNullOrWhiteSpace(keyValuePairState))
            {
                return keyValuePairState;
            }

            string fragmentState = DescribeSettingAssetFragment(value);
            if (!string.IsNullOrWhiteSpace(fragmentState))
            {
                return fragmentState;
            }

            return SafeDescribeValue(value);
        }

        private static string SafeDescribeValue(object value)
        {
            if (value == null)
            {
                return "<null>";
            }

            if (value is string text)
            {
                return text;
            }

            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                List<string> items = new List<string>();
                int count = 0;
                foreach (object item in enumerable)
                {
                    items.Add(SafeDescribeScalar(item));
                    count += 1;
                    if (count >= 5)
                    {
                        break;
                    }
                }

                string suffix = count >= 5 ? ", ..." : string.Empty;
                return $"[{string.Join(", ", items)}{suffix}]";
            }

            return SafeDescribeScalar(value);
        }

        private static string SafeDescribeScalar(object value)
        {
            if (value == null)
            {
                return "<null>";
            }

            Type valueType = value.GetType();
            if (valueType.IsPrimitive || value is decimal || value is Guid)
            {
                return value.ToString();
            }

            string describedObject = DescribeObject(value);
            return string.IsNullOrWhiteSpace(describedObject)
                ? valueType.FullName
                : describedObject;
        }

        private static string DescribeEnumeratorState(string fieldName, object value)
        {
            if (value == null)
            {
                return null;
            }

            Type valueType = value.GetType();
            bool enumeratorHint =
                string.Equals(fieldName, "<>7__wrap1", StringComparison.Ordinal) ||
                valueType.Name.IndexOf("Enumerator", StringComparison.Ordinal) >= 0;
            if (!enumeratorHint)
            {
                return null;
            }

            PropertyInfo currentProperty = valueType.GetProperty(
                "Current",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (currentProperty == null)
            {
                return SafeDescribeValue(value);
            }

            string currentText;
            try
            {
                object current = currentProperty.GetValue(value);
                currentText = DescribeFieldValue($"{fieldName}.Current", current);
            }
            catch (Exception ex)
            {
                currentText = $"<failed:{ex.GetType().Name}:{ex.Message}>";
            }

            return $"{valueType.FullName}(Current={currentText})";
        }

        private static string DescribeKeyValuePair(object value)
        {
            if (value == null)
            {
                return null;
            }

            Type valueType = value.GetType();
            if (!valueType.IsValueType ||
                !valueType.IsGenericType ||
                !string.Equals(valueType.GetGenericTypeDefinition().FullName, "System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
            {
                return null;
            }

            try
            {
                PropertyInfo keyProperty = valueType.GetProperty("Key");
                PropertyInfo valueProperty = valueType.GetProperty("Value");
                object key = keyProperty?.GetValue(value);
                object pairValue = valueProperty?.GetValue(value);
                return $"KeyValuePair(Key={DescribeFieldValue("Key", key)}, Value={DescribeFieldValue("Value", pairValue)})";
            }
            catch (Exception ex)
            {
                return $"<failed:{ex.GetType().Name}:{ex.Message}>";
            }
        }

        private static string DescribeSettingAssetFragment(object fragment)
        {
            if (fragment == null)
            {
                return null;
            }

            Type fragmentType = fragment.GetType();
            if (s_SettingAssetFragmentType == null ||
                !s_SettingAssetFragmentType.IsAssignableFrom(fragmentType))
            {
                return null;
            }

            object owner =
                TryReadNamedMemberObject(fragment, fragmentType, "settingAsset") ??
                TryReadNamedMemberObject(fragment, fragmentType, "asset") ??
                TryReadNamedMemberObject(fragment, fragmentType, "m_SettingAsset") ??
                TryReadNamedMemberObject(fragment, fragmentType, "m_Asset");

            string ownerDescription = owner != null
                ? DescribeSettingAsset(owner)
                : "<unknown>";

            string name = FirstNonBlank(
                TryReadNamedMemberString(fragment, fragmentType, "name"),
                TryReadNamedMemberString(fragment, fragmentType, "m_Name"));

            string identifier = FirstNonBlank(
                TryReadNamedMemberString(fragment, fragmentType, "identifier"),
                TryReadNamedMemberString(fragment, fragmentType, "m_Identifier"));

            string path = FirstNonBlank(
                TryReadNamedMemberString(fragment, fragmentType, "path"),
                TryReadNamedMemberString(fragment, fragmentType, "m_Path"));

            return
                $"{fragmentType.FullName}(" +
                $"owner={ownerDescription}, " +
                $"name={FirstNonBlank(name, "unknown")}, " +
                $"identifier={FirstNonBlank(identifier, "unknown")}, " +
                $"path={FirstNonBlank(path, "unknown")})";
        }

        private static string DescribeCurrentSettingAssetFragment(object instance)
        {
            object fragment = TryGetCurrentSettingAssetFragment(instance);
            return fragment != null
                ? DescribeSettingAssetFragment(fragment)
                : null;
        }

        private static string DescribeCurrentSettingAssetOwner(object instance)
        {
            object fragment = TryGetCurrentSettingAssetFragment(instance);
            if (fragment == null)
            {
                return null;
            }

            Type fragmentType = fragment.GetType();
            object owner =
                TryReadNamedMemberObject(fragment, fragmentType, "settingAsset") ??
                TryReadNamedMemberObject(fragment, fragmentType, "asset") ??
                TryReadNamedMemberObject(fragment, fragmentType, "m_SettingAsset") ??
                TryReadNamedMemberObject(fragment, fragmentType, "m_Asset");

            return owner != null
                ? DescribeSettingAsset(owner)
                : "<unknown>";
        }

        private static string InferAndTrackSettingAssetContext(object instance)
        {
            string inferred = TryInferSettingAssetContext(instance);
            if (!string.IsNullOrWhiteSpace(inferred))
            {
                s_CurrentSettingAsset.Value = inferred;
            }

            return inferred;
        }

        private static string TryInferSettingAssetContext(object instance)
        {
            if (instance == null)
            {
                return null;
            }

            Type instanceType = instance.GetType();
            if (s_SettingAssetType != null && s_SettingAssetType.IsAssignableFrom(instanceType))
            {
                return DescribeSettingAsset(instance);
            }

            foreach (FieldInfo field in instanceType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object value;
                try
                {
                    value = field.GetValue(instance);
                }
                catch
                {
                    continue;
                }

                string inferred = TryInferSettingAssetContextFromValue(field.Name, value);
                if (!string.IsNullOrWhiteSpace(inferred))
                {
                    return inferred;
                }
            }

            return null;
        }

        private static string TryInferSettingAssetContextFromValue(string fieldName, object value)
        {
            if (value == null)
            {
                return null;
            }

            Type valueType = value.GetType();
            if (s_SettingAssetType != null && s_SettingAssetType.IsAssignableFrom(valueType))
            {
                return DescribeSettingAsset(value);
            }

            object fragment = TryGetSettingAssetFragmentFromValue(fieldName, value);
            string fragmentDescription = DescribeSettingAssetFragment(fragment);
            if (!string.IsNullOrWhiteSpace(fragmentDescription))
            {
                return fragmentDescription;
            }

            return null;
        }

        private static object TryGetCurrentSettingAssetFragment(object instance)
        {
            if (instance == null)
            {
                return null;
            }

            Type instanceType = instance.GetType();
            foreach (FieldInfo field in instanceType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object value;
                try
                {
                    value = field.GetValue(instance);
                }
                catch
                {
                    continue;
                }

                object fragment = TryGetSettingAssetFragmentFromValue(field.Name, value);
                if (fragment != null)
                {
                    return fragment;
                }
            }

            return null;
        }

        private static object TryGetSettingAssetFragmentFromValue(string fieldName, object value)
        {
            if (value == null)
            {
                return null;
            }

            Type valueType = value.GetType();
            if (s_SettingAssetFragmentType != null &&
                s_SettingAssetFragmentType.IsAssignableFrom(valueType))
            {
                return value;
            }

            if (valueType.IsValueType &&
                valueType.IsGenericType &&
                string.Equals(valueType.GetGenericTypeDefinition().FullName, "System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal))
            {
                try
                {
                    object key = valueType.GetProperty("Key")?.GetValue(value);
                    object keyFragment = TryGetSettingAssetFragmentFromValue("Key", key);
                    if (keyFragment != null)
                    {
                        return keyFragment;
                    }
                }
                catch
                {
                }
            }

            bool enumeratorHint =
                string.Equals(fieldName, "<>7__wrap1", StringComparison.Ordinal) ||
                valueType.Name.IndexOf("Enumerator", StringComparison.Ordinal) >= 0;
            if (enumeratorHint)
            {
                try
                {
                    object current = valueType.GetProperty(
                        "Current",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(value);
                    object currentFragment = TryGetSettingAssetFragmentFromValue($"{fieldName}.Current", current);
                    if (currentFragment != null)
                    {
                        return currentFragment;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static object TryReadNamedMemberObject(object instance, Type objectType, string memberName)
        {
            if (instance == null || objectType == null || string.IsNullOrWhiteSpace(memberName))
            {
                return null;
            }

            try
            {
                PropertyInfo property = objectType.GetProperty(
                    memberName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    return property.GetValue(instance);
                }

                FieldInfo field = objectType.GetField(
                    memberName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return field?.GetValue(instance);
            }
            catch
            {
                return null;
            }
        }

        private static string TryReadNamedMemberString(object instance, Type objectType, string memberName)
        {
            object value = TryReadNamedMemberObject(instance, objectType, memberName);
            return value?.ToString();
        }

        private static string DescribeObjectMembers(object instance)
        {
            if (instance == null)
            {
                return "<null>";
            }

            List<string> parts = new List<string>();
            Type objectType = instance.GetType();

            foreach (FieldInfo field in objectType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                parts.Add($"{field.Name}={SafeReadMember(() => field.GetValue(instance))}");
                if (parts.Count >= 20)
                {
                    return string.Join("; ", parts);
                }
            }

            foreach (PropertyInfo property in objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (property.GetIndexParameters().Length != 0 ||
                    string.Equals(property.Name, "bindings", StringComparison.Ordinal))
                {
                    continue;
                }

                parts.Add($"{property.Name}={SafeReadMember(() => property.GetValue(instance))}");
                if (parts.Count >= 20)
                {
                    return string.Join("; ", parts);
                }
            }

            return parts.Count == 0
                ? objectType.FullName
                : string.Join("; ", parts);
        }

        private static string SafeReadMember(Func<object> reader)
        {
            try
            {
                return SafeDescribeValue(reader());
            }
            catch (Exception ex)
            {
                return $"<failed:{ex.GetType().Name}:{ex.Message}>";
            }
        }

        private static string TryGetIsDefaultValue(Game.Settings.KeybindingSettings instance)
        {
            if (instance == null || s_KeybindingSettingsIsDefaultField == null)
            {
                return "unknown";
            }

            try
            {
                object rawValue = s_KeybindingSettingsIsDefaultField.GetValue(instance);
                return rawValue?.ToString() ?? "<null>";
            }
            catch (Exception ex)
            {
                return $"<failed:{ex.GetType().Name}:{ex.Message}>";
            }
        }

        private static void AppendAuxiliaryOnlyLine(string message)
        {
            try
            {
                string logPath = ResolveAuxiliaryLogPath();
                string directory = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string timestampedLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff}] [KEYBIND_DIAG] {message}";
                lock (s_LogGate)
                {
                    File.AppendAllText(logPath, timestampedLine + Environment.NewLine);
                }
            }
            catch
            {
            }
        }
    }
}
