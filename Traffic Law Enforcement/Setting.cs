using System;
using System.IO;
using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.Localization;
using Game;
using Game.Input;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using Game.UI;

namespace Traffic_Law_Enforcement
{
    internal static class KeybindingIds
    {
        public const string SelectedObjectPanelToggleActionName =
            "SelectedObjectPanelToggle";
        public const string FocusedLoggingPanelToggleActionName =
            "FocusedLoggingPanelToggle";
    }

    [FileLocation(nameof(Traffic_Law_Enforcement))]
    [SettingsUITabOrder(kCurrentSaveTab, kNewSaveDefaultsTab, kPolicyImpactTab, kDebugTab)]
    [SettingsUIGroupOrder(kGeneralGroup, kPublicTransportLaneAuthorizedGroup, kPublicTransportLaneAdditionalGroup, kPublicTransportLanePressureGroup, kFineGroup, kRepeatOffenderGroup, kTemplateActionsGroup, kPolicyImpactGroup, kDebugLoggingGroup, kFocusedLoggingGroup, kDebugBindingsGroup, kChirperGroup, kLogPathGroup)]
    [SettingsUIShowGroupName(kGeneralGroup, kPublicTransportLaneAuthorizedGroup, kPublicTransportLaneAdditionalGroup, kPublicTransportLanePressureGroup, kFineGroup, kRepeatOffenderGroup, kTemplateActionsGroup, kPolicyImpactGroup, kDebugLoggingGroup, kFocusedLoggingGroup, kDebugBindingsGroup, kChirperGroup, kLogPathGroup)]
    [SettingsUIKeyboardAction(
        KeybindingIds.SelectedObjectPanelToggleActionName,
        canBeEmpty: false)]
    [SettingsUIKeyboardAction(
        KeybindingIds.FocusedLoggingPanelToggleActionName,
        canBeEmpty: false)]
    public class Setting : ModSetting
    {
        private static string s_ModLogPath;
        private const string EnforcementLoggingMigrationMarkerFileName =
            "enforcement_logging_summary_migration_v1.flag";
        private bool m_EnableFocusedRouteRebuildDiagnosticsLogging;

        public const string kCurrentSaveTab = "CurrentSave";
        public const string kNewSaveDefaultsTab = "NewSaveDefaults";
        public const string kPolicyImpactTab = "PolicyImpact";
        public const string kDebugTab = "Debug";

        public const string kGeneralGroup = "General";
        public const string kPublicTransportLaneAuthorizedGroup = "PublicTransportLaneAuthorized";
        public const string kPublicTransportLaneAdditionalGroup = "PublicTransportLaneAdditional";
        public const string kPublicTransportLanePressureGroup = "PublicTransportLanePressure";
        public const string kFineGroup = "FineAmounts";
        public const string kRepeatOffenderGroup = "RepeatOffenders";
        public const string kTemplateActionsGroup = "TemplateActions";
        public const string kPolicyImpactGroup = "PolicyImpactGroup";
        public const string kDebugLoggingGroup = "DebugLoggingGroup";
        public const string kFocusedLoggingGroup = "FocusedLoggingGroup";
        public const string kDebugBindingsGroup = "DebugBindingsGroup";
        public const string kChirperGroup = "ChirperGroup";
        public const string kLogPathGroup = "LogPathGroup";

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        [Exclude]
        [SettingsUISection(kPolicyImpactTab, kPolicyImpactGroup)]
        public string PolicyImpactTotalStatistics => EnforcementPolicyImpactService.GetTotalStatisticsLine();

        [Exclude]
        [SettingsUISection(kPolicyImpactTab, kPolicyImpactGroup)]
        public string PolicyImpactPublicTransportLaneStatistics => EnforcementPolicyImpactService.GetPublicTransportLaneStatisticsLine();

        [Exclude]
        [SettingsUISection(kPolicyImpactTab, kPolicyImpactGroup)]
        public string PolicyImpactMidBlockStatistics => EnforcementPolicyImpactService.GetMidBlockCrossingStatisticsLine();

        [Exclude]
        [SettingsUISection(kPolicyImpactTab, kPolicyImpactGroup)]
        public string PolicyImpactIntersectionStatistics => EnforcementPolicyImpactService.GetIntersectionMovementStatisticsLine();

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kGeneralGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool EnablePublicTransportLaneEnforcement
        {
            get => EnforcementGameplaySettingsService.Current.EnablePublicTransportLaneEnforcement;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.EnablePublicTransportLaneEnforcement = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kGeneralGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool EnableMidBlockCrossingEnforcement
        {
            get => EnforcementGameplaySettingsService.Current.EnableMidBlockCrossingEnforcement;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.EnableMidBlockCrossingEnforcement = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kGeneralGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool EnableIntersectionMovementEnforcement
        {
            get => EnforcementGameplaySettingsService.Current.EnableIntersectionMovementEnforcement;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.EnableIntersectionMovementEnforcement = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowRoadPublicTransportVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowRoadPublicTransportVehicles;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowRoadPublicTransportVehicles = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowTaxis
        {
            get => EnforcementGameplaySettingsService.Current.AllowTaxis;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowTaxis = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowPoliceCars
        {
            get => EnforcementGameplaySettingsService.Current.AllowPoliceCars;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPoliceCars = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowFireEngines
        {
            get => EnforcementGameplaySettingsService.Current.AllowFireEngines;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowFireEngines = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowAmbulances
        {
            get => EnforcementGameplaySettingsService.Current.AllowAmbulances;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowAmbulances = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowGarbageTrucks
        {
            get => EnforcementGameplaySettingsService.Current.AllowGarbageTrucks;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowGarbageTrucks = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowPostVans
        {
            get => EnforcementGameplaySettingsService.Current.AllowPostVans;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPostVans = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowRoadMaintenanceVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowRoadMaintenanceVehicles;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowRoadMaintenanceVehicles = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowSnowplows
        {
            get => EnforcementGameplaySettingsService.Current.AllowSnowplows;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowSnowplows = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowVehicleMaintenanceVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowVehicleMaintenanceVehicles;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowVehicleMaintenanceVehicles = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowPersonalCars
        {
            get => EnforcementGameplaySettingsService.Current.AllowPersonalCars;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPersonalCars = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowDeliveryTrucks
        {
            get => EnforcementGameplaySettingsService.Current.AllowDeliveryTrucks;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowDeliveryTrucks = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowCargoTransportVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowCargoTransportVehicles;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowCargoTransportVehicles = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowHearses
        {
            get => EnforcementGameplaySettingsService.Current.AllowHearses;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowHearses = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowPrisonerTransports
        {
            get => EnforcementGameplaySettingsService.Current.AllowPrisonerTransports;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPrisonerTransports = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowParkMaintenanceVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowParkMaintenanceVehicles;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowParkMaintenanceVehicles = value);
        }

        [Exclude]
        [SettingsUISlider(min = 0f, max = 1f, step = 0.01f, unit = Unit.kFloatThreeFractions)]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLanePressureGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public float PublicTransportLaneExitPressureThresholdDays
        {
            get => EnforcementGameplaySettingsService.Current.PublicTransportLaneExitPressureThresholdDays;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.PublicTransportLaneExitPressureThresholdDays = value);
        }

        [Exclude]
        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kCurrentSaveTab, kFineGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public int PublicTransportLaneFineAmount
        {
            get => EnforcementGameplaySettingsService.Current.PublicTransportLaneFineAmount;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.PublicTransportLaneFineAmount = value);
        }

        [Exclude]
        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kCurrentSaveTab, kFineGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentMidBlockCrossingPolicySettingsDisabled))]
        public int MidBlockCrossingFineAmount
        {
            get => EnforcementGameplaySettingsService.Current.MidBlockCrossingFineAmount;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.MidBlockCrossingFineAmount = value);
        }

        [Exclude]
        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kCurrentSaveTab, kFineGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentIntersectionMovementPolicySettingsDisabled))]
        public int IntersectionMovementFineAmount
        {
            get => EnforcementGameplaySettingsService.Current.IntersectionMovementFineAmount;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.IntersectionMovementFineAmount = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool EnablePublicTransportLaneRepeatPenalty
        {
            get => EnforcementGameplaySettingsService.Current.EnablePublicTransportLaneRepeatPenalty;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.EnablePublicTransportLaneRepeatPenalty = value);
        }

        [Exclude]
        [SettingsUISlider(min = 1, max = 12, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int PublicTransportLaneRepeatWindowMonths
        {
            get => EnforcementGameplaySettingsService.Current.PublicTransportLaneRepeatWindowMonths;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.PublicTransportLaneRepeatWindowMonths = value);
        }

        [Exclude]
        [SettingsUISlider(min = 2, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int PublicTransportLaneRepeatThreshold
        {
            get => EnforcementGameplaySettingsService.Current.PublicTransportLaneRepeatThreshold;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.PublicTransportLaneRepeatThreshold = value);
        }

        [Exclude]
        [SettingsUISlider(min = 100, max = 500, step = 25, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int PublicTransportLaneRepeatMultiplierPercent
        {
            get => EnforcementGameplaySettingsService.Current.PublicTransportLaneRepeatMultiplierPercent;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.PublicTransportLaneRepeatMultiplierPercent = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentMidBlockCrossingPolicySettingsDisabled))]
        public bool EnableMidBlockCrossingRepeatPenalty
        {
            get => EnforcementGameplaySettingsService.Current.EnableMidBlockCrossingRepeatPenalty;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.EnableMidBlockCrossingRepeatPenalty = value);
        }

        [Exclude]
        [SettingsUISlider(min = 1, max = 12, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentMidBlockCrossingRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int MidBlockCrossingRepeatWindowMonths
        {
            get => EnforcementGameplaySettingsService.Current.MidBlockCrossingRepeatWindowMonths;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.MidBlockCrossingRepeatWindowMonths = value);
        }

        [Exclude]
        [SettingsUISlider(min = 2, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentMidBlockCrossingRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int MidBlockCrossingRepeatThreshold
        {
            get => EnforcementGameplaySettingsService.Current.MidBlockCrossingRepeatThreshold;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.MidBlockCrossingRepeatThreshold = value);
        }

        [Exclude]
        [SettingsUISlider(min = 100, max = 500, step = 25, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentMidBlockCrossingRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int MidBlockCrossingRepeatMultiplierPercent
        {
            get => EnforcementGameplaySettingsService.Current.MidBlockCrossingRepeatMultiplierPercent;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.MidBlockCrossingRepeatMultiplierPercent = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentIntersectionMovementPolicySettingsDisabled))]
        public bool EnableIntersectionMovementRepeatPenalty
        {
            get => EnforcementGameplaySettingsService.Current.EnableIntersectionMovementRepeatPenalty;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.EnableIntersectionMovementRepeatPenalty = value);
        }

        [Exclude]
        [SettingsUISlider(min = 1, max = 12, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentIntersectionMovementRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int IntersectionMovementRepeatWindowMonths
        {
            get => EnforcementGameplaySettingsService.Current.IntersectionMovementRepeatWindowMonths;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.IntersectionMovementRepeatWindowMonths = value);
        }

        [Exclude]
        [SettingsUISlider(min = 2, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentIntersectionMovementRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int IntersectionMovementRepeatThreshold
        {
            get => EnforcementGameplaySettingsService.Current.IntersectionMovementRepeatThreshold;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.IntersectionMovementRepeatThreshold = value);
        }

        [Exclude]
        [SettingsUISlider(min = 100, max = 500, step = 25, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentIntersectionMovementRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int IntersectionMovementRepeatMultiplierPercent
        {
            get => EnforcementGameplaySettingsService.Current.IntersectionMovementRepeatMultiplierPercent;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.IntersectionMovementRepeatMultiplierPercent = value);
        }

        [SettingsUISection(kNewSaveDefaultsTab, kGeneralGroup)]
        public bool DefaultEnablePublicTransportLaneEnforcement { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kGeneralGroup)]
        public bool DefaultEnableMidBlockCrossingEnforcement { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kGeneralGroup)]
        public bool DefaultEnableIntersectionMovementEnforcement { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowRoadPublicTransportVehicles { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowTaxis { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowPoliceCars { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowFireEngines { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowAmbulances { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowGarbageTrucks { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowPostVans { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowRoadMaintenanceVehicles { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowSnowplows { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowVehicleMaintenanceVehicles { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowPersonalCars { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowDeliveryTrucks { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowCargoTransportVehicles { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowHearses { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowPrisonerTransports { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowParkMaintenanceVehicles { get; set; }

        [SettingsUISlider(min = 0f, max = 1f, step = 0.01f, unit = Unit.kFloatThreeFractions)]
        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLanePressureGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public float DefaultPublicTransportLaneExitPressureThresholdDays { get; set; }

        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kNewSaveDefaultsTab, kFineGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public int DefaultPublicTransportLaneFineAmount { get; set; }

        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kNewSaveDefaultsTab, kFineGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveMidBlockCrossingPolicySettingsDisabled))]
        public int DefaultMidBlockCrossingFineAmount { get; set; }

        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kNewSaveDefaultsTab, kFineGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveIntersectionMovementPolicySettingsDisabled))]
        public int DefaultIntersectionMovementFineAmount { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultEnablePublicTransportLaneRepeatPenalty { get; set; }

        [SettingsUISlider(min = 1, max = 12, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultPublicTransportLaneRepeatWindowMonths { get; set; }

        [SettingsUISlider(min = 2, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultPublicTransportLaneRepeatThreshold { get; set; }

        [SettingsUISlider(min = 100, max = 500, step = 25, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultPublicTransportLaneRepeatMultiplierPercent { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveMidBlockCrossingPolicySettingsDisabled))]
        public bool DefaultEnableMidBlockCrossingRepeatPenalty { get; set; }

        [SettingsUISlider(min = 1, max = 12, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveMidBlockCrossingRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultMidBlockCrossingRepeatWindowMonths { get; set; }

        [SettingsUISlider(min = 2, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveMidBlockCrossingRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultMidBlockCrossingRepeatThreshold { get; set; }

        [SettingsUISlider(min = 100, max = 500, step = 25, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveMidBlockCrossingRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultMidBlockCrossingRepeatMultiplierPercent { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveIntersectionMovementPolicySettingsDisabled))]
        public bool DefaultEnableIntersectionMovementRepeatPenalty { get; set; }

        [SettingsUISlider(min = 1, max = 12, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveIntersectionMovementRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultIntersectionMovementRepeatWindowMonths { get; set; }

        [SettingsUISlider(min = 2, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveIntersectionMovementRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultIntersectionMovementRepeatThreshold { get; set; }

        [SettingsUISlider(min = 100, max = 500, step = 25, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveIntersectionMovementRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultIntersectionMovementRepeatMultiplierPercent { get; set; }

        [Exclude]
        [SettingsUIButton]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kTemplateActionsGroup)]
        public bool ResetCurrentSaveSettingsToCodeDefaults
        {
            set
            {
                if (!value)
                {
                    return;
                }

                EnforcementGameplaySettingsService.ResetToCodeDefaults();
            }
        }

        [Exclude]
        [SettingsUIButton]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kTemplateActionsGroup)]
        public bool CopyCurrentSaveSettingsToDefaults
        {
            set
            {
                if (!value)
                {
                    return;
                }

                ApplyNewSaveDefaultSettings(EnforcementGameplaySettingsService.Current);
                ApplyAndSave();
            }
        }

        [Exclude]
        [SettingsUIButton]
        [SettingsUISection(kNewSaveDefaultsTab, kTemplateActionsGroup)]
        public bool ResetDefaultsToCodeDefaults
        {
            set
            {
                if (!value)
                {
                    return;
                }

                ApplyNewSaveDefaultSettings(EnforcementGameplaySettingsState.CreateCodeDefaults());
                ApplyAndSave();
            }
        }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnableEstimatedRerouteLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnableEnforcementEventLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnablePolicyImpactSummaryLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnableFineIncomeLogging { get; set; }


        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnablePathfindingPenaltyDiagnosticLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnableType2PublicTransportLaneUsageLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnableType3PublicTransportLaneUsageLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnableType4PublicTransportLaneUsageLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnablePathObsoleteSourceLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnableAllVehicleRouteSelectionChangeLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kFocusedLoggingGroup)]
        public bool EnableFocusedRouteRebuildDiagnosticsLogging
        {
            get => m_EnableFocusedRouteRebuildDiagnosticsLogging;
            set
            {
                if (m_EnableFocusedRouteRebuildDiagnosticsLogging == value)
                {
                    return;
                }

                m_EnableFocusedRouteRebuildDiagnosticsLogging = value;
                FocusedRouteDiagnosticsPatchController.Sync(value);
            }
        }

        [Exclude]
        [SettingsUISection(kDebugTab, kFocusedLoggingGroup)]
        public bool EnableFocusedVehicleOnlyRouteLogging { get; set; }

        [SettingsUISection(kDebugTab, kDebugBindingsGroup)]
        [SettingsUIKeyboardBinding(
            BindingKeyboard.I,
            KeybindingIds.SelectedObjectPanelToggleActionName,
            ctrl: true)]
        public ProxyBinding SelectedObjectPanelToggleBinding { get; set; }

        [SettingsUISection(kDebugTab, kDebugBindingsGroup)]
        [SettingsUIKeyboardBinding(
            BindingKeyboard.L,
            KeybindingIds.FocusedLoggingPanelToggleActionName,
            ctrl: true)]
        public ProxyBinding FocusedLoggingPanelToggleBinding { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kLogPathGroup)]
        public string ModLogPath => s_ModLogPath ?? (s_ModLogPath = GetModLogPath());

        [Exclude]
        [SettingsUIButton]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsMonthlyChirperPreviewButtonDisabled))]
        [SettingsUISection(kDebugTab, kChirperGroup)]
        public bool SendMonthlyChirperPreviewNow
        {
            set
            {
                if (value)
                {
                    MonthlyEnforcementChirperService.RequestManualPreview();
                }
            }
        }

        public override void SetDefaults()
        {
            ApplyNewSaveDefaultSettings(EnforcementGameplaySettingsState.CreateCodeDefaults());
            // Keep debug logging opt-in by default.
            EnableEstimatedRerouteLogging = false;
            EnableEnforcementEventLogging = false;
            EnablePolicyImpactSummaryLogging = false;
            EnableFineIncomeLogging = false;
            EnableType2PublicTransportLaneUsageLogging = false;
            EnableType3PublicTransportLaneUsageLogging = false;
            EnableType4PublicTransportLaneUsageLogging = false;
            EnablePathfindingPenaltyDiagnosticLogging = false;
            EnablePathObsoleteSourceLogging = false;
            EnableAllVehicleRouteSelectionChangeLogging = false;
            EnableFocusedRouteRebuildDiagnosticsLogging = false;
            EnableFocusedVehicleOnlyRouteLogging = false;
            ResetKeyBindings();
        }

        public void ApplyEnforcementLoggingMigrationIfNeeded()
        {
            if (HasAppliedEnforcementLoggingMigration())
            {
                return;
            }

            bool shouldSave = false;
            if (EnableEnforcementEventLogging)
            {
                EnablePolicyImpactSummaryLogging = true;
                EnableFineIncomeLogging = true;
                shouldSave = true;
            }

            if (shouldSave)
            {
                ApplyAndSave();
            }

            MarkEnforcementLoggingMigrationApplied();
        }

        public EnforcementGameplaySettingsState GetNewSaveDefaultSettings()
        {
            return new EnforcementGameplaySettingsState
            {
                EnablePublicTransportLaneEnforcement = DefaultEnablePublicTransportLaneEnforcement,
                EnableMidBlockCrossingEnforcement = DefaultEnableMidBlockCrossingEnforcement,
                EnableIntersectionMovementEnforcement = DefaultEnableIntersectionMovementEnforcement,
                AllowRoadPublicTransportVehicles = DefaultAllowRoadPublicTransportVehicles,
                AllowTaxis = DefaultAllowTaxis,
                AllowPoliceCars = DefaultAllowPoliceCars,
                AllowFireEngines = DefaultAllowFireEngines,
                AllowAmbulances = DefaultAllowAmbulances,
                AllowGarbageTrucks = DefaultAllowGarbageTrucks,
                AllowPostVans = DefaultAllowPostVans,
                AllowRoadMaintenanceVehicles = DefaultAllowRoadMaintenanceVehicles,
                AllowSnowplows = DefaultAllowSnowplows,
                AllowVehicleMaintenanceVehicles = DefaultAllowVehicleMaintenanceVehicles,
                AllowPersonalCars = DefaultAllowPersonalCars,
                AllowDeliveryTrucks = DefaultAllowDeliveryTrucks,
                AllowCargoTransportVehicles = DefaultAllowCargoTransportVehicles,
                AllowHearses = DefaultAllowHearses,
                AllowPrisonerTransports = DefaultAllowPrisonerTransports,
                AllowParkMaintenanceVehicles = DefaultAllowParkMaintenanceVehicles,
                PublicTransportLaneExitPressureThresholdDays = DefaultPublicTransportLaneExitPressureThresholdDays,
                PublicTransportLaneFineAmount = DefaultPublicTransportLaneFineAmount,
                MidBlockCrossingFineAmount = DefaultMidBlockCrossingFineAmount,
                IntersectionMovementFineAmount = DefaultIntersectionMovementFineAmount,
                EnablePublicTransportLaneRepeatPenalty = DefaultEnablePublicTransportLaneRepeatPenalty,
                PublicTransportLaneRepeatWindowMonths = DefaultPublicTransportLaneRepeatWindowMonths,
                PublicTransportLaneRepeatThreshold = DefaultPublicTransportLaneRepeatThreshold,
                PublicTransportLaneRepeatMultiplierPercent = DefaultPublicTransportLaneRepeatMultiplierPercent,
                EnableMidBlockCrossingRepeatPenalty = DefaultEnableMidBlockCrossingRepeatPenalty,
                MidBlockCrossingRepeatWindowMonths = DefaultMidBlockCrossingRepeatWindowMonths,
                MidBlockCrossingRepeatThreshold = DefaultMidBlockCrossingRepeatThreshold,
                MidBlockCrossingRepeatMultiplierPercent = DefaultMidBlockCrossingRepeatMultiplierPercent,
                EnableIntersectionMovementRepeatPenalty = DefaultEnableIntersectionMovementRepeatPenalty,
                IntersectionMovementRepeatWindowMonths = DefaultIntersectionMovementRepeatWindowMonths,
                IntersectionMovementRepeatThreshold = DefaultIntersectionMovementRepeatThreshold,
                IntersectionMovementRepeatMultiplierPercent = DefaultIntersectionMovementRepeatMultiplierPercent,
            };
        }

        public void ApplyNewSaveDefaultSettings(EnforcementGameplaySettingsState state)
        {
            DefaultEnablePublicTransportLaneEnforcement = state.EnablePublicTransportLaneEnforcement;
            DefaultEnableMidBlockCrossingEnforcement = state.EnableMidBlockCrossingEnforcement;
            DefaultEnableIntersectionMovementEnforcement = state.EnableIntersectionMovementEnforcement;
            DefaultAllowRoadPublicTransportVehicles = state.AllowRoadPublicTransportVehicles;
            DefaultAllowTaxis = state.AllowTaxis;
            DefaultAllowPoliceCars = state.AllowPoliceCars;
            DefaultAllowFireEngines = state.AllowFireEngines;
            DefaultAllowAmbulances = state.AllowAmbulances;
            DefaultAllowGarbageTrucks = state.AllowGarbageTrucks;
            DefaultAllowPostVans = state.AllowPostVans;
            DefaultAllowRoadMaintenanceVehicles = state.AllowRoadMaintenanceVehicles;
            DefaultAllowSnowplows = state.AllowSnowplows;
            DefaultAllowVehicleMaintenanceVehicles = state.AllowVehicleMaintenanceVehicles;
            DefaultAllowPersonalCars = state.AllowPersonalCars;
            DefaultAllowDeliveryTrucks = state.AllowDeliveryTrucks;
            DefaultAllowCargoTransportVehicles = state.AllowCargoTransportVehicles;
            DefaultAllowHearses = state.AllowHearses;
            DefaultAllowPrisonerTransports = state.AllowPrisonerTransports;
            DefaultAllowParkMaintenanceVehicles = state.AllowParkMaintenanceVehicles;
            DefaultPublicTransportLaneExitPressureThresholdDays = state.PublicTransportLaneExitPressureThresholdDays;
            DefaultPublicTransportLaneFineAmount = state.PublicTransportLaneFineAmount;
            DefaultMidBlockCrossingFineAmount = state.MidBlockCrossingFineAmount;
            DefaultIntersectionMovementFineAmount = state.IntersectionMovementFineAmount;
            DefaultEnablePublicTransportLaneRepeatPenalty = state.EnablePublicTransportLaneRepeatPenalty;
            DefaultPublicTransportLaneRepeatWindowMonths = state.PublicTransportLaneRepeatWindowMonths;
            DefaultPublicTransportLaneRepeatThreshold = state.PublicTransportLaneRepeatThreshold;
            DefaultPublicTransportLaneRepeatMultiplierPercent = state.PublicTransportLaneRepeatMultiplierPercent;
            DefaultEnableMidBlockCrossingRepeatPenalty = state.EnableMidBlockCrossingRepeatPenalty;
            DefaultMidBlockCrossingRepeatWindowMonths = state.MidBlockCrossingRepeatWindowMonths;
            DefaultMidBlockCrossingRepeatThreshold = state.MidBlockCrossingRepeatThreshold;
            DefaultMidBlockCrossingRepeatMultiplierPercent = state.MidBlockCrossingRepeatMultiplierPercent;
            DefaultEnableIntersectionMovementRepeatPenalty = state.EnableIntersectionMovementRepeatPenalty;
            DefaultIntersectionMovementRepeatWindowMonths = state.IntersectionMovementRepeatWindowMonths;
            DefaultIntersectionMovementRepeatThreshold = state.IntersectionMovementRepeatThreshold;
            DefaultIntersectionMovementRepeatMultiplierPercent = state.IntersectionMovementRepeatMultiplierPercent;
        }

        public bool IsCurrentSaveSettingsDisabled()
        {
            return !IsGameplayContextAvailable();
        }

        public bool IsCurrentPublicTransportLaneSettingsDisabled() => IsCurrentSaveSettingsDisabled() || !EnablePublicTransportLaneEnforcement;
        public bool IsCurrentMidBlockCrossingPolicySettingsDisabled() => IsCurrentSaveSettingsDisabled() || !EnableMidBlockCrossingEnforcement;
        public bool IsCurrentIntersectionMovementPolicySettingsDisabled() => IsCurrentSaveSettingsDisabled() || !EnableIntersectionMovementEnforcement;
        public bool IsCurrentPublicTransportLaneRepeatSettingsDisabled() => IsCurrentPublicTransportLaneSettingsDisabled() || !EnablePublicTransportLaneRepeatPenalty;
        public bool IsCurrentMidBlockCrossingRepeatSettingsDisabled() => IsCurrentMidBlockCrossingPolicySettingsDisabled() || !EnableMidBlockCrossingRepeatPenalty;
        public bool IsCurrentIntersectionMovementRepeatSettingsDisabled() => IsCurrentIntersectionMovementPolicySettingsDisabled() || !EnableIntersectionMovementRepeatPenalty;
        public bool IsNewSavePublicTransportLaneSettingsDisabled() => !DefaultEnablePublicTransportLaneEnforcement;
        public bool IsNewSaveMidBlockCrossingPolicySettingsDisabled() => !DefaultEnableMidBlockCrossingEnforcement;
        public bool IsNewSaveIntersectionMovementPolicySettingsDisabled() => !DefaultEnableIntersectionMovementEnforcement;
        public bool IsNewSavePublicTransportLaneRepeatSettingsDisabled() => IsNewSavePublicTransportLaneSettingsDisabled() || !DefaultEnablePublicTransportLaneRepeatPenalty;
        public bool IsNewSaveMidBlockCrossingRepeatSettingsDisabled() => IsNewSaveMidBlockCrossingPolicySettingsDisabled() || !DefaultEnableMidBlockCrossingRepeatPenalty;
        public bool IsNewSaveIntersectionMovementRepeatSettingsDisabled() => IsNewSaveIntersectionMovementPolicySettingsDisabled() || !DefaultEnableIntersectionMovementRepeatPenalty;
        public bool IsMonthlyChirperPreviewButtonDisabled() => !IsGameplayContextAvailable() || !EnforcementGameplaySettingsService.Current.HasAnyEnforcementEnabled();

        private static void UpdateCurrentSaveSettings(CurrentSaveSettingsMutator mutator)
        {
            EnforcementGameplaySettingsState state = EnforcementGameplaySettingsService.Current;
            mutator(ref state);
            EnforcementGameplaySettingsService.Apply(state);
        }

        private static bool IsGameplayContextAvailable()
        {
            return GameManager.instance != null && GameManager.instance.gameMode.IsGameOrEditor();
        }

        private static string GetModLogPath()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string appData = Directory.GetParent(localAppData)?.FullName;

                if (!string.IsNullOrWhiteSpace(appData))
                {
                    return Path.Combine(
                        appData,
                        "LocalLow",
                        "Colossal Order",
                        "Cities Skylines II",
                        "Logs",
                        "Traffic_Law_Enforcement.Mod.log");
                }
            }
            catch
            {
            }

            return @"C:\Users\(USERNAME)\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\Traffic_Law_Enforcement.Mod.log";
        }

        private static bool HasAppliedEnforcementLoggingMigration()
        {
            try
            {
                return File.Exists(GetEnforcementLoggingMigrationMarkerPath());
            }
            catch
            {
                return false;
            }
        }

        private static void MarkEnforcementLoggingMigrationApplied()
        {
            try
            {
                string markerPath = GetEnforcementLoggingMigrationMarkerPath();
                string markerDirectory = Path.GetDirectoryName(markerPath);
                if (!string.IsNullOrWhiteSpace(markerDirectory))
                {
                    Directory.CreateDirectory(markerDirectory);
                }

                File.WriteAllText(
                    markerPath,
                    DateTime.UtcNow.ToString("O"));
            }
            catch
            {
            }
        }

        private static string GetEnforcementLoggingMigrationMarkerPath()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);
                string appData = Directory.GetParent(localAppData)?.FullName;

                if (!string.IsNullOrWhiteSpace(appData))
                {
                    return Path.Combine(
                        appData,
                        "LocalLow",
                        "Colossal Order",
                        "Cities Skylines II",
                        "ModsData",
                        nameof(Traffic_Law_Enforcement),
                        EnforcementLoggingMigrationMarkerFileName);
                }
            }
            catch
            {
            }

            return EnforcementLoggingMigrationMarkerFileName;
        }

        private delegate void CurrentSaveSettingsMutator(ref EnforcementGameplaySettingsState state);
    }
}
