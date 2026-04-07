using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Common;
using Game.Modding;
using Game.SceneFlow;
using Game.Serialization;
using Game.Simulation;
using Game.Triggers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Reflection;
using System.Xml.Linq;

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
        public static string CurrentModDisplayName { get; private set; } = "unknown";
        public static string CurrentModVersion { get; private set; } = "unknown";
        public static string CurrentGameVersion { get; private set; } = "unknown";
        private Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            string modAssetPath = null;
            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                modAssetPath = asset.path;
                log.Info($"Current mod asset at {asset.path}");
            }

            EnforcementGameTime.Reset();
            BurstLoggingService.Reset();
            FocusedLoggingService.Reset();
            ObsoleteAttemptCorrelationService.Reset();
            PublicTransportLaneExitPressureTelemetry.Reset();
            SaveLoadTraceService.Reset();
            SaveLoadTracePatches.Apply();
            m_Setting = new Setting(this);
            Settings = m_Setting;
            using (m_Setting.BeginAuditSourceContext("LoadSettings"))
            {
                AssetDatabase.global.LoadSettings(
                    nameof(Traffic_Law_Enforcement),
                    m_Setting,
                    new Setting(this, enableAuditEmission: false));
            }
            m_Setting.ApplyEnforcementLoggingMigrationIfNeeded();
            m_Setting.RegisterKeyBindings();

            ResolveAndCacheModMetadata(modAssetPath);
            LogModVersionInfo(modAssetPath);
            log.Info(
                $"[MB-AHD-RAW] buildFingerprint={MidBlockAccessPathfindingPenaltyPatches.MbAhdRawBuildFingerprint}");
            m_Setting.LogDebugLoggingSettingsSnapshot("OnLoad");

            m_Setting.RegisterInOptionsUI();
            RegisterTextLocales();
            BudgetUIPatches.Apply();
            VehicleUtilsPatches.Apply();
            FocusedRouteDiagnosticsPatchController.Sync();
            PathfindRuntimeDiscoveryPatches.Apply();
            MidBlockAccessPathfindingPenaltyPatches.Apply();
            IntersectionMovementPathfindingPenaltyPatches.Apply();
            if (!IntersectionMovementPathfindingPenaltyPatches.IsApplied)
            {
                IntersectionMovementPathfindingPenaltyReflectionPatches.Apply();
            }
            updateSystem.UpdateBefore<PreDeserialize<EnforcementSaveDataSystem>>(SystemUpdatePhase.Deserialize);
            updateSystem.UpdateAfter<PostDeserialize<EnforcementSaveDataSystem>>(SystemUpdatePhase.Deserialize);
            updateSystem.UpdateAfter<EnforcementSaveDataSystem, EnforcementGameTimeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<EnforcementSaveDataSystem, VehicleTrafficLawProfileSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<VehicleTrafficLawProfileSystem, PublicTransportLanePermissionSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<VehicleTrafficLawProfileSystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<MidBlockPathfindingBiasSystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<PublicTransportLanePermissionSystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<PublicTransportLanePermissionSystem, PublicTransportLaneViolationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<IntersectionMovementPenaltyCacheSystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<EnforcementGameTimeSystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<RouteAttemptTrackingSystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<RouteAttemptTrackingSystem, RoutePenaltyRerouteLoggingSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<FocusedLoggingWatchedVehicleMonitorSystem, RoutePenaltyRerouteLoggingSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<FocusedLoggingWatchedVehicleMonitorSystem, PrepareCleanUpSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<RouteAttemptTrackingSystem, PublicTransportLaneViolationApplySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<MonthlyEnforcementChirperSystem, EnforcementGameTimeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<MonthlyEnforcementChirperSystem, CreateChirpSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<VehicleLaneHistorySystem, EnforcementGameTimeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<RoutePenaltyRerouteLoggingSystem, VehicleLaneHistorySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<PublicTransportLaneViolationSystem, PublicTransportLaneViolationApplySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<PublicTransportLaneViolationApplySystem, EnforcementGameTimeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<PublicTransportLaneExitPressureSystem, PublicTransportLaneViolationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<PublicTransportLaneExitPressureSystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<LaneTransitionViolationSystem, VehicleLaneHistorySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<LaneTransitionViolationApplySystem, LaneTransitionViolationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<EnforcementFineMoneySystem, PublicTransportLaneViolationApplySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<EnforcementFineMoneySystem, LaneTransitionViolationApplySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<SelectedObjectBridgeSystem, VehicleLaneHistorySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<SelectedObjectBridgeSystem, PublicTransportLaneViolationApplySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<SelectedObjectBridgeSystem, LaneTransitionViolationApplySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<SelectedObjectPanelUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<FocusedLoggingPanelUISystem>(SystemUpdatePhase.UIUpdate);
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            BurstLoggingService.Reset();
            FocusedLoggingService.Reset();
            ObsoleteAttemptCorrelationService.Reset();
            PublicTransportLaneExitPressureTelemetry.Reset();
            SaveLoadTracePatches.Remove();
            SaveLoadTraceService.Reset();
            BudgetUIPatches.Remove();
            FocusedRouteDiagnosticsPatchController.RemoveAll();
            PathfindRuntimeDiscoveryPatches.Remove();
            VehicleUtilsPatches.Remove();
            MidBlockAccessPathfindingPenaltyPatches.Remove();
            IntersectionMovementPathfindingPenaltyPatches.Remove();
            IntersectionMovementPathfindingPenaltyReflectionPatches.Remove();
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
                string localeId = NormalizeLocaleId(Path.GetFileNameWithoutExtension(filePath));

                if (string.Equals(localeId, "en-US", StringComparison.OrdinalIgnoreCase))
                {
                    englishEntries = PropertiesLocaleSource.LoadKeyValueFile(filePath);
                    break;
                }
            }

            foreach (string filePath in files)
            {
                string localeId = NormalizeLocaleId(Path.GetFileNameWithoutExtension(filePath));
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

        private static string NormalizeLocaleId(string localeId)
        {
            switch (localeId)
            {
                case "zh-CN":
                    return "zh-HANS";

                case "zh-TW":
                    return "zh-HANT";

                default:
                    return localeId;
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

                case "de-DE":
                    localizedName = "Deutsch";
                    systemLanguage = SystemLanguage.German;
                    return true;

                case "es-ES":
                    localizedName = "Español";
                    systemLanguage = SystemLanguage.Spanish;
                    return true;

                case "fr-FR":
                    localizedName = "Français";
                    systemLanguage = SystemLanguage.French;
                    return true;

                case "it-IT":
                    localizedName = "Italiano";
                    systemLanguage = SystemLanguage.Italian;
                    return true;

                case "ja-JP":
                    localizedName = "日本語";
                    systemLanguage = SystemLanguage.Japanese;
                    return true;

                case "pl-PL":
                    localizedName = "Polski";
                    systemLanguage = SystemLanguage.Polish;
                    return true;

                case "pt-BR":
                    localizedName = "Português (Brasil)";
                    systemLanguage = SystemLanguage.Portuguese;
                    return true;

                case "ru-RU":
                    localizedName = "Русский";
                    systemLanguage = SystemLanguage.Russian;
                    return true;

                case "zh-HANS":
                    localizedName = "简体中文";
                    systemLanguage = SystemLanguage.ChineseSimplified;
                    return true;

                case "zh-HANT":
                    localizedName = "繁體中文";
                    systemLanguage = SystemLanguage.ChineseTraditional;
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

        private void LogModVersionInfo(string modAssetPath)
        {
            Assembly assembly = typeof(Mod).Assembly;
            string assemblyVersion = assembly.GetName().Version?.ToString() ?? "unknown";

            log.Info(
                "[MODINFO] " +
                $"name={CurrentModDisplayName}, " +
                $"modVersion={CurrentModVersion}, " +
                $"gameVersion={CurrentGameVersion}, " +
                $"assemblyVersion={assemblyVersion}, " +
                $"assetPath={FirstNonBlank(modAssetPath, "unknown")}");
        }

        private void ResolveAndCacheModMetadata(string modAssetPath)
        {
            Assembly assembly = typeof(Mod).Assembly;
            string assemblyVersion = assembly.GetName().Version?.ToString();

            string modRootDirectory = GetModRootDirectory(modAssetPath);

            CurrentModDisplayName =
                ReadPublishConfigurationValue(modRootDirectory, "DisplayName") ??
                "unknown";

            CurrentModVersion =
                ReadPublishConfigurationValue(modRootDirectory, "ModVersion") ??
                assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
                assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ??
                assemblyVersion ??
                "unknown";

            CurrentGameVersion =
                ReadPublishConfigurationValue(modRootDirectory, "GameVersion") ??
                "unknown";
        }

        private static string GetModRootDirectory(string modAssetPath)
        {
            if (string.IsNullOrWhiteSpace(modAssetPath))
            {
                return null;
            }

            return File.Exists(modAssetPath)
                ? Path.GetDirectoryName(modAssetPath)
                : modAssetPath;
        }

        private static string ReadPublishConfigurationValue(string modRootDirectory, string elementName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(modRootDirectory) || string.IsNullOrWhiteSpace(elementName))
                {
                    return null;
                }

                string publishConfigurationPath = Path.Combine(modRootDirectory, "PublishConfiguration.xml");
                if (!File.Exists(publishConfigurationPath))
                {
                    return null;
                }

                XElement root = XDocument.Load(publishConfigurationPath).Root;
                XElement element = root?.Element(elementName);
                return element?.Attribute("Value")?.Value;
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

