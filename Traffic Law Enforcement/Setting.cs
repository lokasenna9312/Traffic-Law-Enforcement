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
    }

    [FileLocation(nameof(Traffic_Law_Enforcement))]
    [SettingsUITabOrder(kCurrentSaveTab, kNewSaveDefaultsTab, kPolicyImpactTab, kDebugTab)]
    [SettingsUIGroupOrder(kGeneralGroup, kPublicTransportLaneAuthorizedGroup, kPublicTransportLaneAdditionalGroup, kPublicTransportLanePressureGroup, kFineGroup, kRepeatOffenderGroup, kTemplateActionsGroup, kPolicyImpactGroup, kDebugGroup, kLogPathGroup)]
    [SettingsUIShowGroupName(kGeneralGroup, kPublicTransportLaneAuthorizedGroup, kPublicTransportLaneAdditionalGroup, kPublicTransportLanePressureGroup, kFineGroup, kRepeatOffenderGroup, kTemplateActionsGroup, kPolicyImpactGroup, kDebugGroup, kLogPathGroup)]
    [SettingsUIKeyboardAction(
        KeybindingIds.SelectedObjectPanelToggleActionName,
        canBeEmpty: false)]
    public class Setting : ModSetting
    {
        // --- Debug logging toggles for save/load ---
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
        public const string kDebugGroup = "DebugGroup";
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
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowRoadPublicTransportVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowRoadPublicTransportVehicles;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowRoadPublicTransportVehicles = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowTaxis
        {
            get => EnforcementGameplaySettingsService.Current.AllowTaxis;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowTaxis = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowPoliceCars
        {
            get => EnforcementGameplaySettingsService.Current.AllowPoliceCars;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPoliceCars = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowFireEngines
        {
            get => EnforcementGameplaySettingsService.Current.AllowFireEngines;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowFireEngines = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowAmbulances
        {
            get => EnforcementGameplaySettingsService.Current.AllowAmbulances;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowAmbulances = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowGarbageTrucks
        {
            get => EnforcementGameplaySettingsService.Current.AllowGarbageTrucks;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowGarbageTrucks = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowPostVans
        {
            get => EnforcementGameplaySettingsService.Current.AllowPostVans;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPostVans = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowRoadMaintenanceVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowRoadMaintenanceVehicles;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowRoadMaintenanceVehicles = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowSnowplows
        {
            get => EnforcementGameplaySettingsService.Current.AllowSnowplows;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowSnowplows = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowVehicleMaintenanceVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowVehicleMaintenanceVehicles;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowVehicleMaintenanceVehicles = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowPersonalCars
        {
            get => EnforcementGameplaySettingsService.Current.AllowPersonalCars;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPersonalCars = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowDeliveryTrucks
        {
            get => EnforcementGameplaySettingsService.Current.AllowDeliveryTrucks;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowDeliveryTrucks = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowCargoTransportVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowCargoTransportVehicles;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowCargoTransportVehicles = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowHearses
        {
            get => EnforcementGameplaySettingsService.Current.AllowHearses;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowHearses = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowPrisonerTransports
        {
            get => EnforcementGameplaySettingsService.Current.AllowPrisonerTransports;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPrisonerTransports = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowParkMaintenanceVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowParkMaintenanceVehicles;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowParkMaintenanceVehicles = value);
        }

        [Exclude]
        [SettingsUISlider(min = 0f, max = 1f, step = 0.01f, unit = Unit.kFloatThreeFractions)]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLanePressureGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public float PublicTransportLaneExitPressureThresholdDays
        {
            get => EnforcementGameplaySettingsService.Current.PublicTransportLaneExitPressureThresholdDays;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.PublicTransportLaneExitPressureThresholdDays = value);
        }

        [Exclude]
        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kCurrentSaveTab, kFineGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public int PublicTransportLaneFineAmount
        {
            get => EnforcementGameplaySettingsService.Current.PublicTransportLaneFineAmount;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.PublicTransportLaneFineAmount = value);
        }

        [Exclude]
        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kCurrentSaveTab, kFineGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public int MidBlockCrossingFineAmount
        {
            get => EnforcementGameplaySettingsService.Current.MidBlockCrossingFineAmount;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.MidBlockCrossingFineAmount = value);
        }

        [Exclude]
        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kCurrentSaveTab, kFineGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public int IntersectionMovementFineAmount
        {
            get => EnforcementGameplaySettingsService.Current.IntersectionMovementFineAmount;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.IntersectionMovementFineAmount = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
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
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
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
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
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
        public bool DefaultAllowRoadPublicTransportVehicles { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        public bool DefaultAllowTaxis { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        public bool DefaultAllowPoliceCars { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        public bool DefaultAllowFireEngines { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        public bool DefaultAllowAmbulances { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        public bool DefaultAllowGarbageTrucks { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        public bool DefaultAllowPostVans { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        public bool DefaultAllowRoadMaintenanceVehicles { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        public bool DefaultAllowSnowplows { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        public bool DefaultAllowVehicleMaintenanceVehicles { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        public bool DefaultAllowPersonalCars { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        public bool DefaultAllowDeliveryTrucks { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        public bool DefaultAllowCargoTransportVehicles { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        public bool DefaultAllowHearses { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        public bool DefaultAllowPrisonerTransports { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        public bool DefaultAllowParkMaintenanceVehicles { get; set; }

        [SettingsUISlider(min = 0f, max = 1f, step = 0.01f, unit = Unit.kFloatThreeFractions)]
        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLanePressureGroup)]
        public float DefaultPublicTransportLaneExitPressureThresholdDays { get; set; }

        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kNewSaveDefaultsTab, kFineGroup)]
        public int DefaultPublicTransportLaneFineAmount { get; set; }

        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kNewSaveDefaultsTab, kFineGroup)]
        public int DefaultMidBlockCrossingFineAmount { get; set; }

        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kNewSaveDefaultsTab, kFineGroup)]
        public int DefaultIntersectionMovementFineAmount { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
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
        [SettingsUISection(kDebugTab, kDebugGroup)]
        public bool EnableEstimatedRerouteLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugGroup)]
        public bool EnableEnforcementEventLogging { get; set; }


        [Exclude]
        [SettingsUISection(kDebugTab, kDebugGroup)]
        public bool EnablePathfindingPenaltyDiagnosticLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugGroup)]
        public bool EnableType2PublicTransportLaneUsageLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugGroup)]
        public bool EnableType3PublicTransportLaneUsageLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugGroup)]
        public bool EnableType4PublicTransportLaneUsageLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugGroup)]
        public bool EnablePathObsoleteSourceLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugGroup)]
        public bool EnableSaveIdentificationLogging { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugGroup)]
        public bool EnableAllVehicleRouteSelectionChangeLogging { get; set; }

        [SettingsUISection(kDebugTab, kDebugGroup)]
        [SettingsUIKeyboardBinding(
            BindingKeyboard.I,
            KeybindingIds.SelectedObjectPanelToggleActionName,
            ctrl: true,
            shift: true)]
        public ProxyBinding SelectedObjectPanelToggleBinding { get; set; }

        [Exclude]
        [SettingsUISection(kDebugTab, kLogPathGroup)]
        public string ModLogPath => GetModLogPath();

        [Exclude]
        [SettingsUIButton]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsMonthlyChirperPreviewButtonDisabled))]
        [SettingsUISection(kDebugTab, kDebugGroup)]
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
            EnableType2PublicTransportLaneUsageLogging = false;
            EnableType3PublicTransportLaneUsageLogging = false;
            EnableType4PublicTransportLaneUsageLogging = false;
            EnablePathfindingPenaltyDiagnosticLogging = false;
            EnablePathObsoleteSourceLogging = false;
            EnableSaveIdentificationLogging = false;
            EnableAllVehicleRouteSelectionChangeLogging = false;
            ResetKeyBindings();
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

        public bool IsCurrentPublicTransportLaneRepeatSettingsDisabled() => IsCurrentSaveSettingsDisabled() || !EnablePublicTransportLaneRepeatPenalty;
        public bool IsCurrentMidBlockCrossingRepeatSettingsDisabled() => IsCurrentSaveSettingsDisabled() || !EnableMidBlockCrossingRepeatPenalty;
        public bool IsCurrentIntersectionMovementRepeatSettingsDisabled() => IsCurrentSaveSettingsDisabled() || !EnableIntersectionMovementRepeatPenalty;
        public bool IsNewSavePublicTransportLaneRepeatSettingsDisabled() => !DefaultEnablePublicTransportLaneRepeatPenalty;
        public bool IsNewSaveMidBlockCrossingRepeatSettingsDisabled() => !DefaultEnableMidBlockCrossingRepeatPenalty;
        public bool IsNewSaveIntersectionMovementRepeatSettingsDisabled() => !DefaultEnableIntersectionMovementRepeatPenalty;
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

        private delegate void CurrentSaveSettingsMutator(ref EnforcementGameplaySettingsState state);
    }
}
