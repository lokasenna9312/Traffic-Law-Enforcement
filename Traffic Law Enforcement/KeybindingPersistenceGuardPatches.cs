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
        private static readonly MethodInfo s_TypeExtensionsGetMemberValueMethod =
            AccessTools.Method(
                AccessTools.TypeByName("Colossal.OdinSerializer.Utilities.TypeExtensions"),
                "GetMemberValue",
                new[] { typeof(MemberInfo), typeof(object) });
        private static readonly FieldInfo s_KeybindingSettingsIsDefaultField =
            AccessTools.Field(typeof(KeybindingSettings), "m_IsDefault");

        private static Harmony s_Harmony;
        private static List<ProxyBinding> s_LastKnownEffectiveBuiltInBindings;
        private static List<ProxyBinding> s_LastKnownOriginalBuiltInBindings;
        private static bool s_LoggedEffectiveBindingFallback;
        private static bool s_LoggedOriginalBindingFallback;
        private static bool s_LoggedMemberValueSuppression;

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            try
            {
                s_Harmony = new Harmony(HarmonyId);
                HarmonyMethod getterPrefix = new HarmonyMethod(typeof(KeybindingPersistenceGuardPatches), nameof(KeybindingSettingsBindingsPrefix))
                {
                    priority = 800
                };
                HarmonyMethod getterFinalizer = new HarmonyMethod(typeof(KeybindingPersistenceGuardPatches), nameof(KeybindingSettingsBindingsFinalizer))
                {
                    priority = 0
                };
                HarmonyMethod memberValuePrefix = new HarmonyMethod(typeof(KeybindingPersistenceGuardPatches), nameof(TypeExtensionsGetMemberValuePrefix))
                {
                    priority = 800
                };
                HarmonyMethod memberValueFinalizer = new HarmonyMethod(typeof(KeybindingPersistenceGuardPatches), nameof(TypeExtensionsGetMemberValueFinalizer))
                {
                    priority = 0
                };

                s_Harmony.Patch(s_KeybindingSettingsBindingsGetter, prefix: getterPrefix, finalizer: getterFinalizer);

                if (s_TypeExtensionsGetMemberValueMethod != null)
                {
                    s_Harmony.Patch(s_TypeExtensionsGetMemberValueMethod, prefix: memberValuePrefix, finalizer: memberValueFinalizer);
                }
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
            __result = ResolveBindings(__instance, null);
            return false;
        }

        private static Exception KeybindingSettingsBindingsFinalizer(
            KeybindingSettings __instance,
            ref List<ProxyBinding> __result,
            Exception __exception)
        {
            if (__exception == null)
            {
                return null;
            }

            __result = ResolveBindings(__instance, __exception);
            return null;
        }

        private static bool TypeExtensionsGetMemberValuePrefix(MemberInfo member, object obj, ref object __result)
        {
            if (!TryGetBindingMemberTarget(member, obj, out KeybindingSettings settings))
            {
                return true;
            }

            __result = ResolveBindings(settings, null);
            return false;
        }

        private static Exception TypeExtensionsGetMemberValueFinalizer(
            MemberInfo member,
            object obj,
            ref object __result,
            Exception __exception)
        {
            if (__exception == null)
            {
                return null;
            }

            if (!TryGetBindingMemberTarget(member, obj, out KeybindingSettings settings))
            {
                return __exception;
            }

            __result = ResolveBindings(settings, __exception);
            LogMemberValueSuppression(member, obj, __exception);
            return null;
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

        private static List<ProxyBinding> ResolveBindings(KeybindingSettings settings, Exception failure)
        {
            InputManager.PathType pathType = IsDefaultSettings(settings)
                ? InputManager.PathType.Original
                : InputManager.PathType.Effective;

            List<ProxyBinding> liveBindings = null;
            Exception liveFailure = null;
            if (failure == null && TryReadBindings(pathType, out liveBindings, out liveFailure))
            {
                return liveBindings;
            }

            if (failure == null)
            {
                failure = liveFailure;
            }

            List<ProxyBinding> cachedBindings = GetCachedBindings(pathType);
            if (cachedBindings != null)
            {
                List<ProxyBinding> snapshot = CloneBindings(cachedBindings);
                LogFallback(pathType, failure, snapshot.Count);
                return snapshot;
            }

            LogFallback(pathType, failure, 0);
            return new List<ProxyBinding>();
        }

        private static bool IsDefaultSettings(KeybindingSettings settings)
        {
            return settings != null &&
                   s_KeybindingSettingsIsDefaultField != null &&
                   s_KeybindingSettingsIsDefaultField.GetValue(settings) is bool isDefault &&
                   isDefault;
        }

        private static bool TryGetBindingMemberTarget(MemberInfo member, object obj, out KeybindingSettings settings)
        {
            settings = obj as KeybindingSettings;
            return settings != null &&
                   member != null &&
                   string.Equals(member.Name, nameof(KeybindingSettings.bindings), StringComparison.Ordinal);
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

        private static void LogMemberValueSuppression(MemberInfo member, object obj, Exception failure)
        {
            if (s_LoggedMemberValueSuppression)
            {
                return;
            }

            s_LoggedMemberValueSuppression = true;

            Mod.log.Warn(
                "[KEYBIND_GUARD] Suppressed TypeExtensions.GetMemberValue exception " +
                $"for member={member?.DeclaringType?.FullName}.{member?.Name}, " +
                $"memberInfoType={member?.GetType().FullName}, " +
                $"objType={obj?.GetType().FullName}, " +
                $"reason={failure.GetType().Name}: {failure.Message}");
        }

        private static List<ProxyBinding> CloneBindings(IEnumerable<ProxyBinding> bindings)
        {
            return bindings?.Select(static binding => binding.Copy()).ToList() ?? new List<ProxyBinding>();
        }
    }
}
