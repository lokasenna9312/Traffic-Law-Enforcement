using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Input;
using Game.Settings;
using HarmonyLib;

namespace Traffic_Law_Enforcement
{
    internal static class KeybindingPersistenceGuardPatches
    {
        private const string HarmonyId = "Traffic_Law_Enforcement.KeybindingPersistenceGuardPatches";

        private static readonly MethodInfo s_KeybindingSettingsBindingsGetter =
            AccessTools.PropertyGetter(typeof(KeybindingSettings), nameof(KeybindingSettings.bindings));
        private static readonly FieldInfo s_KeybindingSettingsIsDefaultField =
            AccessTools.Field(typeof(KeybindingSettings), "m_IsDefault");

        private static Harmony s_Harmony;
        private static List<ProxyBinding> s_LastKnownEffectiveBuiltInBindings;
        private static List<ProxyBinding> s_LastKnownOriginalBuiltInBindings;
        private static bool s_LoggedEffectiveBindingFallback;
        private static bool s_LoggedOriginalBindingFallback;

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            try
            {
                s_Harmony = new Harmony(HarmonyId);
                HarmonyMethod prefix = new HarmonyMethod(typeof(KeybindingPersistenceGuardPatches), nameof(KeybindingSettingsBindingsPrefix));
                s_Harmony.Patch(s_KeybindingSettingsBindingsGetter, prefix: prefix);
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                Mod.log.Error(ex, "Failed to apply keybinding persistence safeguard.");
            }
        }

        public static void Remove()
        {
            if (s_Harmony == null)
            {
                return;
            }

            s_Harmony.UnpatchAll(HarmonyId);
            s_Harmony = null;
        }

        public static void CaptureCurrentBindings()
        {
            try
            {
                if (InputManager.instance == null)
                {
                    return;
                }

                CaptureBindings(InputManager.PathType.Effective);
                CaptureBindings(InputManager.PathType.Original);
            }
            catch (Exception ex)
            {
                Mod.log.Error(ex, "Failed to capture current keybinding state.");
            }
        }

        private static bool KeybindingSettingsBindingsPrefix(KeybindingSettings __instance, ref List<ProxyBinding> __result)
        {
            InputManager.PathType pathType = IsDefaultSettings(__instance)
                ? InputManager.PathType.Original
                : InputManager.PathType.Effective;

            if (TryReadBindings(pathType, out List<ProxyBinding> liveBindings, out Exception failure))
            {
                __result = liveBindings;
                return false;
            }

            List<ProxyBinding> cachedBindings = GetCachedBindings(pathType);
            if (cachedBindings != null)
            {
                __result = CloneBindings(cachedBindings);
                LogFallback(pathType, failure, __result.Count);
                return false;
            }

            LogFallback(pathType, failure, 0);
            __result = new List<ProxyBinding>();
            return false;
        }

        private static void CaptureBindings(InputManager.PathType pathType)
        {
            if (!TryReadBindings(pathType, out _, out Exception failure) && failure != null)
            {
                throw failure;
            }
        }

        private static bool TryReadBindings(InputManager.PathType pathType, out List<ProxyBinding> bindings, out Exception failure)
        {
            bindings = null;
            failure = null;

            try
            {
                if (InputManager.instance == null)
                {
                    return false;
                }

                List<ProxyBinding> snapshot = CloneBindings(
                    InputManager.instance.GetBindings(
                        pathType,
                        InputManager.BindingOptions.OnlyRebound | InputManager.BindingOptions.OnlyBuiltIn));

                SetCachedBindings(pathType, snapshot);
                ResetFallbackLog(pathType);
                bindings = snapshot;
                return true;
            }
            catch (Exception ex)
            {
                failure = ex;
                return false;
            }
        }

        private static bool IsDefaultSettings(KeybindingSettings settings)
        {
            return settings != null &&
                   s_KeybindingSettingsIsDefaultField != null &&
                   s_KeybindingSettingsIsDefaultField.GetValue(settings) is bool isDefault &&
                   isDefault;
        }

        private static List<ProxyBinding> GetCachedBindings(InputManager.PathType pathType)
        {
            return pathType == InputManager.PathType.Original
                ? s_LastKnownOriginalBuiltInBindings
                : s_LastKnownEffectiveBuiltInBindings;
        }

        private static void SetCachedBindings(InputManager.PathType pathType, List<ProxyBinding> bindings)
        {
            if (pathType == InputManager.PathType.Original)
            {
                s_LastKnownOriginalBuiltInBindings = bindings;
                return;
            }

            s_LastKnownEffectiveBuiltInBindings = bindings;
        }

        private static void ResetFallbackLog(InputManager.PathType pathType)
        {
            if (pathType == InputManager.PathType.Original)
            {
                s_LoggedOriginalBindingFallback = false;
                return;
            }

            s_LoggedEffectiveBindingFallback = false;
        }

        private static void LogFallback(InputManager.PathType pathType, Exception failure, int bindingCount)
        {
            bool alreadyLogged = pathType == InputManager.PathType.Original
                ? s_LoggedOriginalBindingFallback
                : s_LoggedEffectiveBindingFallback;

            if (alreadyLogged)
            {
                return;
            }

            if (pathType == InputManager.PathType.Original)
            {
                s_LoggedOriginalBindingFallback = true;
            }
            else
            {
                s_LoggedEffectiveBindingFallback = true;
            }

            string failureText = failure == null
                ? "InputManager was unavailable."
                : $"{failure.GetType().Name}: {failure.Message}";

            Mod.log.Warn(
                $"[KEYBIND_GUARD] Falling back to cached {pathType} bindings. count={bindingCount}, reason={failureText}");
        }

        private static List<ProxyBinding> CloneBindings(IEnumerable<ProxyBinding> bindings)
        {
            return bindings?.Select(static binding => binding.Copy()).ToList() ?? new List<ProxyBinding>();
        }
    }
}
