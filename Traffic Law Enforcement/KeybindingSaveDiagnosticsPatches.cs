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

        private static readonly AsyncLocal<List<string>> s_DiffObjectStack = new AsyncLocal<List<string>>();
        private static readonly AsyncLocal<string> s_CurrentSettingAsset = new AsyncLocal<string>();
        private static readonly AsyncLocal<List<string>> s_SaveBreadcrumbs = new AsyncLocal<List<string>>();
        private static readonly HashSet<string> s_LoggedFailures = new HashSet<string>(StringComparer.Ordinal);
        private static readonly object s_LogGate = new object();
        private static readonly List<string> s_GlobalSaveEvents = new List<string>();

        private static readonly FieldInfo s_KeybindingSettingsIsDefaultField =
            AccessTools.Field(typeof(Game.Settings.KeybindingSettings), "m_IsDefault");

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

        private static Harmony s_Harmony;

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            if (s_DiffObjectMethod == null ||
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
                    s_TypeExtensionsGetMemberValueMethod,
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

                WriteDiagnosticLine(
                    "Keybinding save diagnostics patches applied. " +
                    $"settingAssetSaveMethods={string.Join(" | ", s_SettingAssetSaveMethods.Select(DescribeMethod))}, " +
                    $"assetDatabaseSaveSettingsMethods={string.Join(" | ", s_AssetDatabaseSaveSettingsMethods.Select(DescribeMethod))}, " +
                    $"assetDatabaseSaveSettingsWorkerMethods={string.Join(" | ", s_AssetDatabaseSaveSettingsWorkerMethods.Select(DescribeMethod))}");
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
                $"exception={DescribeException(__exception)}, " +
                $"rootCause={DescribeException(rootCause)}",
                __exception);

            return __exception;
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
                    $"exception={DescribeException(__exception)}, " +
                    $"rootCause={DescribeException(UnwrapException(__exception))}",
                    __exception);
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
                    fields.Select(field => $"{field.Name}={SafeDescribeValue(field.GetValue(instance))}"));
            }
            catch (Exception ex)
            {
                return $"<failed:{ex.GetType().Name}:{ex.Message}>";
            }
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
    }
}
