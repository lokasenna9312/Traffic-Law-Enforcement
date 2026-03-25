using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace Traffic_Law_Enforcement
{
    internal static class KeybindingSaveDiagnosticsPatches
    {
        private const string HarmonyId = "Traffic_Law_Enforcement.KeybindingSaveDiagnosticsPatches";
        private const int kMaxTrackedDepth = 12;

        private static readonly AsyncLocal<List<string>> s_DiffObjectStack = new AsyncLocal<List<string>>();
        private static readonly AsyncLocal<string> s_CurrentSettingAsset = new AsyncLocal<string>();
        private static readonly HashSet<string> s_LoggedFailures = new HashSet<string>(StringComparer.Ordinal);
        private static readonly object s_LogGate = new object();

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

        private static readonly MethodInfo s_SettingAssetSaveMethod =
            AccessTools.Method(
                s_SettingAssetType,
                "Save",
                new[]
                {
                    typeof(bool),
                    typeof(bool),
                    s_SaveSettingsHelperType,
                });

        private static Harmony s_Harmony;

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            if (s_DiffObjectMethod == null ||
                s_TypeExtensionsGetMemberValueMethod == null ||
                s_SettingAssetSaveMethod == null)
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

                s_Harmony.Patch(
                    s_SettingAssetSaveMethod,
                    prefix: new HarmonyMethod(typeof(KeybindingSaveDiagnosticsPatches), nameof(SettingAssetSavePrefix)));

                Mod.log.Info("[KEYBIND_DIAG] Keybinding save diagnostics patches applied.");
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                Mod.log.Error(ex, "[KEYBIND_DIAG] Failed to apply keybinding save diagnostics patches.");
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
                Mod.log.Info("[KEYBIND_DIAG] Keybinding save diagnostics patches removed.");
            }
            catch (Exception ex)
            {
                Mod.log.Error(ex, "[KEYBIND_DIAG] Failed to remove keybinding save diagnostics patches.");
            }
            finally
            {
                s_Harmony = null;
                s_DiffObjectStack.Value = null;
                s_CurrentSettingAsset.Value = null;
                lock (s_LogGate)
                {
                    s_LoggedFailures.Clear();
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
            if (__exception == null || !IsKeybindingBindingsFailure(member, obj))
            {
                return __exception;
            }

            string key =
                $"{__exception.GetType().FullName}|{member?.DeclaringType?.FullName}|{member?.Name}|{obj?.GetType().FullName}|{s_CurrentSettingAsset.Value}|{FormatCurrentDiffStack()}";

            lock (s_LogGate)
            {
                if (!s_LoggedFailures.Add(key))
                {
                    return __exception;
                }
            }

            string keybindingContext = DescribeKeybindingContext(obj);
            Mod.log.Error(
                "[KEYBIND_DIAG] Exception while resolving a keybinding member during settings save. " +
                $"settingAsset={s_CurrentSettingAsset.Value ?? "<unknown>"}, " +
                $"member={DescribeMember(member)}, " +
                $"object={DescribeObject(obj)}, " +
                $"keybindingContext={keybindingContext}, " +
                $"diffStack={FormatCurrentDiffStack()}, " +
                $"exception={__exception.GetType().Name}: {__exception.Message}");

            return __exception;
        }

        private static bool IsKeybindingBindingsFailure(MemberInfo member, object obj)
        {
            if (!string.Equals(member?.Name, "bindings", StringComparison.Ordinal))
            {
                return false;
            }

            Type objectType = obj?.GetType();
            return objectType != null &&
                   typeof(Game.Settings.KeybindingSettings).IsAssignableFrom(objectType);
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

        private static void SettingAssetSavePrefix(object __instance)
        {
            s_CurrentSettingAsset.Value = DescribeSettingAsset(__instance);
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
    }
}
