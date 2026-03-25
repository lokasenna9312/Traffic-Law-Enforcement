using System;
using System.Linq;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Game.Modding;

namespace Traffic_Law_Enforcement
{
    internal static class ShutdownSettingsSnapshotGuard
    {
        private static readonly PropertyInfo s_ModSettingKeyBindingPropertiesProperty =
            typeof(ModSetting).GetProperty("keyBindingProperties", BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool s_Prepared;

        public static void Prepare()
        {
            if (s_Prepared)
            {
                return;
            }

            s_Prepared = true;

            try
            {
                if (AssetDatabase.global == null)
                {
                    return;
                }

                int preparedAssets = 0;
                int preparedFragments = 0;
                int failedFragments = 0;

                foreach (SettingAsset settingAsset in AssetDatabase.global.AllAssets().OfType<SettingAsset>())
                {
                    bool preparedAsset = false;

                    foreach (SettingAsset.Fragment fragment in settingAsset)
                    {
                        if (!ShouldSnapshot(fragment))
                        {
                            continue;
                        }

                        if (TrySnapshotFragment(fragment))
                        {
                            preparedFragments++;
                            preparedAsset = true;
                        }
                        else
                        {
                            failedFragments++;
                        }
                    }

                    if (preparedAsset)
                    {
                        preparedAssets++;
                    }
                }

                Mod.log.Info(
                    "[SHUTDOWN_SAVE_GUARD] Prepared settings snapshots before mod dispose. " +
                    $"assets={preparedAssets}, fragments={preparedFragments}, failed={failedFragments}");
            }
            catch (Exception ex)
            {
                Mod.log.Error(ex, "Failed to prepare shutdown settings snapshots.");
            }
        }

        private static bool ShouldSnapshot(SettingAsset.Fragment fragment)
        {
            if (fragment == null || !fragment.asset.database.canWriteSettings)
            {
                return false;
            }

            ModSetting modSetting = fragment.source as ModSetting;
            return modSetting != null && HasKeyBindingProperties(modSetting);
        }

        private static bool TrySnapshotFragment(SettingAsset.Fragment fragment)
        {
            try
            {
                string json = JSON.Dump(fragment.source);
                fragment.variant = string.IsNullOrWhiteSpace(json) ? null : Decoder.Decode(json);
                fragment.source = null;
                return true;
            }
            catch (Exception ex)
            {
                Mod.log.Warn(
                    "[SHUTDOWN_SAVE_GUARD] Failed to snapshot setting fragment before dispose. " +
                    $"asset={fragment?.asset?.name}, sourceType={fragment?.source?.GetType().FullName}, " +
                    $"reason={ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        private static bool HasKeyBindingProperties(ModSetting modSetting)
        {
            if (modSetting == null || s_ModSettingKeyBindingPropertiesProperty == null)
            {
                return false;
            }

            try
            {
                PropertyInfo[] keyBindingProperties =
                    s_ModSettingKeyBindingPropertiesProperty.GetValue(modSetting) as PropertyInfo[];

                return keyBindingProperties != null && keyBindingProperties.Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
