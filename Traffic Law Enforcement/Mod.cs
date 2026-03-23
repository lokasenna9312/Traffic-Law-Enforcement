using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Simulation;
using Game.Triggers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Traffic_Law_Enforcement
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(Traffic_Law_Enforcement)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        public static Setting Settings { get; private set; }
        public static string LocalizationDirectory { get; private set; }
        public static bool IsEnforcementEnabled => EnforcementGameplaySettingsService.Current.HasAnyEnforcementEnabled();
        public static bool IsPublicTransportLaneEnforcementEnabled => EnforcementGameplaySettingsService.Current.EnablePublicTransportLaneEnforcement;
        public static bool IsMidBlockCrossingEnforcementEnabled => EnforcementGameplaySettingsService.Current.EnableMidBlockCrossingEnforcement;
        public static bool IsIntersectionMovementEnforcementEnabled => EnforcementGameplaySettingsService.Current.EnableIntersectionMovementEnforcement;
        private Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            EnforcementGameTime.Reset();

            m_Setting = new Setting(this);
            Settings = m_Setting;
            AssetDatabase.global.LoadSettings(nameof(Traffic_Law_Enforcement), m_Setting, new Setting(this));
            m_Setting.RegisterInOptionsUI();
            RegisterTextLocales();
            BudgetUIPatches.Apply();
            VehicleUtilsPatches.Apply();
            IntersectionMovementPathfindPatches.Apply();
            IntersectionMovementPathfindReflectionPatches.Apply();
            updateSystem.UpdateAfter<EnforcementSaveDataSystem, EnforcementGameTimeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<PathfindingMoneyPenaltySystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<PublicTransportLanePermissionSystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<PublicTransportLanePermissionSystem, PublicTransportLaneViolationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<CenterlineAccessObsoleteSystem, PublicTransportLanePermissionSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<CenterlineAccessObsoleteSystem, PublicTransportLaneExitPressureSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<CenterlineAccessObsoleteSystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<EnforcementGameTimeSystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<SettingsChangeLoggingSystem, EnforcementGameTimeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<MonthlyEnforcementChirperSystem, EnforcementGameTimeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<MonthlyEnforcementChirperSystem, CreateChirpSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<VehicleLaneHistorySystem, EnforcementGameTimeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<RoutePenaltyRerouteLoggingSystem, VehicleLaneHistorySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<PublicTransportLaneViolationSystem, EnforcementGameTimeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<PublicTransportLaneExitPressureSystem, PublicTransportLaneViolationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<PublicTransportLaneExitPressureSystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<LaneTransitionViolationSystem, VehicleLaneHistorySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<EnforcementFineMoneySystem, PublicTransportLaneViolationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<EnforcementFineMoneySystem, LaneTransitionViolationSystem>(SystemUpdatePhase.GameSimulation);
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            BudgetUIPatches.Remove();
            VehicleUtilsPatches.Remove();
            IntersectionMovementPathfindPatches.Remove();
            IntersectionMovementPathfindReflectionPatches.Remove();
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
                Settings = null;
            }
        }

        private void RegisterTextLocales()
        {
            var localizationManager = GameManager.instance.localizationManager;
            var keyMap = LocalizationKeys.Build(m_Setting);

            LocalizationDirectory = GetLocalizationDirectory();
            string localeDir = LocalizationDirectory;

            if (!Directory.Exists(localeDir))
            {
                log.Warn($"Localization directory not found: {localeDir}");
                return;
            }

            string[] files = Directory.GetFiles(localeDir, "*.properties");
            if (files.Length == 0)
            {
                log.Warn($"No locale files found in: {localeDir}");
                return;
            }

            Dictionary<string, string> englishEntries = null;

            foreach (string filePath in files)
            {
                string localeId = Path.GetFileNameWithoutExtension(filePath);

                if (string.Equals(localeId, "en-US", StringComparison.OrdinalIgnoreCase))
                {
                    englishEntries = PropertiesLocaleSource.LoadKeyValueFile(filePath);
                    break;
                }
            }

            foreach (string filePath in files)
            {
                string localeId = Path.GetFileNameWithoutExtension(filePath);
                Dictionary<string, string> entries = PropertiesLocaleSource.LoadKeyValueFile(filePath);

                ValidateTextLocaleFile(localeId, entries, englishEntries, keyMap);

                TryGetLocaleMetadata(localeId, out string localizedName, out SystemLanguage systemLanguage);

                if (!localizationManager.SupportsLocale(localeId))
                {
                    localizationManager.AddLocale(localeId, systemLanguage, localizedName);
                }

                localizationManager.AddSource(localeId, new PropertiesLocaleSource(filePath, keyMap));
                log.Info($"Registered locale {localeId} from {Path.GetFileName(filePath)}");
            }
        }

        private bool TryGetLocaleMetadata(
            string localeId,
            out string localizedName,
            out SystemLanguage systemLanguage)
        {
            switch (localeId)
            {
                case "en-US":
                    localizedName = "English (US)";
                    systemLanguage = SystemLanguage.English;
                    return true;

                case "ko-KR":
                    localizedName = "한국어";
                    systemLanguage = SystemLanguage.Korean;
                    return true;

                default:
                    localizedName = localeId;
                    systemLanguage = SystemLanguage.English;
                    return true;
            }
        }
        private string GetLocalizationDirectory()
        {
            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset) &&
                !string.IsNullOrWhiteSpace(asset.path))
            {
                string basePath = asset.path;

                if (File.Exists(basePath))
                {
                    basePath = Path.GetDirectoryName(basePath);
                }

                if (!string.IsNullOrWhiteSpace(basePath))
                {
                    return Path.Combine(basePath, "Localization");
                }
            }

            return Path.Combine(AppContext.BaseDirectory, "Mods", "Traffic Law Enforcement", "Localization");
        }

        private void ValidateTextLocaleFile(
            string localeId,
            Dictionary<string, string> entries,
            Dictionary<string, string> englishEntries,
            Dictionary<string, string> keyMap)
        {
            foreach (string symbolicKey in entries.Keys)
            {
                if (!keyMap.ContainsKey(symbolicKey))
                {
                    log.Warn($"[{localeId}] Unknown symbolic locale key: {symbolicKey}");
                }
            }

            foreach (var pair in entries)
            {
                if (string.IsNullOrWhiteSpace(pair.Value))
                {
                    log.Warn($"[{localeId}] Empty locale value: {pair.Key}");
                }
            }

            if (string.Equals(localeId, "en-US", StringComparison.OrdinalIgnoreCase))
            {
                HashSet<string> masterKeys = new HashSet<string>(entries.Keys, StringComparer.Ordinal);

                foreach (string requiredKey in keyMap.Keys.Except(masterKeys))
                {
                    log.Warn($"[en-US] Missing master locale key: {requiredKey}");
                }

                return;
            }

            if (englishEntries == null)
            {
                return;
            }

            HashSet<string> englishKeys = new HashSet<string>(englishEntries.Keys, StringComparer.Ordinal);
            HashSet<string> localeKeys = new HashSet<string>(entries.Keys, StringComparer.Ordinal);

            foreach (string missingKey in englishKeys.Except(localeKeys))
            {
                log.Warn($"[{localeId}] Missing locale key. Falling back to en-US: {missingKey}");
            }

            foreach (string extraKey in localeKeys.Except(englishKeys))
            {
                log.Warn($"[{localeId}] Extra locale key not present in en-US: {extraKey}");
            }
        }
    }
}
