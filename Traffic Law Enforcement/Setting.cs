using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.Localization;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using Game.UI;

namespace Traffic_Law_Enforcement
{
    [FileLocation(nameof(Traffic_Law_Enforcement))]
    [SettingsUITabOrder(kCurrentSaveTab, kNewSaveDefaultsTab, kPolicyImpactTab, kDebugTab)]
    [SettingsUIGroupOrder(kGeneralGroup, kBusLaneAuthorizedGroup, kBusLaneAdditionalGroup, kBusLanePressureGroup, kFineGroup, kRepeatOffenderGroup, kTemplateActionsGroup, kPolicyImpactGroup, kDebugGroup)]
    [SettingsUIShowGroupName(kGeneralGroup, kBusLaneAuthorizedGroup, kBusLaneAdditionalGroup, kBusLanePressureGroup, kFineGroup, kRepeatOffenderGroup, kTemplateActionsGroup, kPolicyImpactGroup, kDebugGroup)]
    public class Setting : ModSetting
    {
        // --- Debug logging toggles for save/load ---
        public const string kCurrentSaveTab = "CurrentSave";
        public const string kNewSaveDefaultsTab = "NewSaveDefaults";
        public const string kPolicyImpactTab = "PolicyImpact";
        public const string kDebugTab = "Debug";

        public const string kGeneralGroup = "General";
        public const string kBusLaneAuthorizedGroup = "BusLaneAuthorized";
        public const string kBusLaneAdditionalGroup = "BusLaneAdditional";
        public const string kBusLanePressureGroup = "BusLanePressure";
        public const string kFineGroup = "FineAmounts";
        public const string kRepeatOffenderGroup = "RepeatOffenders";
        public const string kTemplateActionsGroup = "TemplateActions";
        public const string kPolicyImpactGroup = "PolicyImpactGroup";
        public const string kDebugGroup = "DebugGroup";

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
        [SettingsUISection(kCurrentSaveTab, kBusLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowRoadPublicTransportVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowRoadPublicTransportVehicles;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowRoadPublicTransportVehicles = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kBusLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowTaxis
        {
            get => EnforcementGameplaySettingsService.Current.AllowTaxis;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowTaxis = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kBusLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowPoliceCars
        {
            get => EnforcementGameplaySettingsService.Current.AllowPoliceCars;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPoliceCars = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kBusLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowFireEngines
        {
            get => EnforcementGameplaySettingsService.Current.AllowFireEngines;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowFireEngines = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kBusLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowAmbulances
        {
            get => EnforcementGameplaySettingsService.Current.AllowAmbulances;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowAmbulances = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kBusLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowGarbageTrucks
        {
            get => EnforcementGameplaySettingsService.Current.AllowGarbageTrucks;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowGarbageTrucks = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kBusLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowPostVans
        {
            get => EnforcementGameplaySettingsService.Current.AllowPostVans;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPostVans = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kBusLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowRoadMaintenanceVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowRoadMaintenanceVehicles;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowRoadMaintenanceVehicles = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kBusLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowSnowplows
        {
            get => EnforcementGameplaySettingsService.Current.AllowSnowplows;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowSnowplows = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kBusLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowVehicleMaintenanceVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowVehicleMaintenanceVehicles;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowVehicleMaintenanceVehicles = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kBusLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowPersonalCars
        {
            get => EnforcementGameplaySettingsService.Current.AllowPersonalCars;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPersonalCars = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kBusLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowDeliveryTrucks
        {
            get => EnforcementGameplaySettingsService.Current.AllowDeliveryTrucks;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowDeliveryTrucks = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kBusLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowCargoTransportVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowCargoTransportVehicles;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowCargoTransportVehicles = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kBusLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowHearses
        {
            get => EnforcementGameplaySettingsService.Current.AllowHearses;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowHearses = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kBusLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowPrisonerTransports
        {
            get => EnforcementGameplaySettingsService.Current.AllowPrisonerTransports;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPrisonerTransports = value);
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kBusLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool AllowParkMaintenanceVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowParkMaintenanceVehicles;
            set => UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowParkMaintenanceVehicles = value);
        }

        [Exclude]
        [SettingsUISlider(min = 0f, max = 1f, step = 0.01f, unit = Unit.kFloatThreeFractions)]
        [SettingsUISection(kCurrentSaveTab, kBusLanePressureGroup)]
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

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAuthorizedGroup)]
        public bool DefaultAllowRoadPublicTransportVehicles { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAuthorizedGroup)]
        public bool DefaultAllowTaxis { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAuthorizedGroup)]
        public bool DefaultAllowPoliceCars { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAuthorizedGroup)]
        public bool DefaultAllowFireEngines { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAuthorizedGroup)]
        public bool DefaultAllowAmbulances { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAuthorizedGroup)]
        public bool DefaultAllowGarbageTrucks { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAuthorizedGroup)]
        public bool DefaultAllowPostVans { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAuthorizedGroup)]
        public bool DefaultAllowRoadMaintenanceVehicles { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAuthorizedGroup)]
        public bool DefaultAllowSnowplows { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAuthorizedGroup)]
        public bool DefaultAllowVehicleMaintenanceVehicles { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAdditionalGroup)]
        public bool DefaultAllowPersonalCars { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAdditionalGroup)]
        public bool DefaultAllowDeliveryTrucks { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAdditionalGroup)]
        public bool DefaultAllowCargoTransportVehicles { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAdditionalGroup)]
        public bool DefaultAllowHearses { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAdditionalGroup)]
        public bool DefaultAllowPrisonerTransports { get; set; }

        [SettingsUISection(kNewSaveDefaultsTab, kBusLaneAdditionalGroup)]
        public bool DefaultAllowParkMaintenanceVehicles { get; set; }

        [SettingsUISlider(min = 0f, max = 1f, step = 0.01f, unit = Unit.kFloatThreeFractions)]
        [SettingsUISection(kNewSaveDefaultsTab, kBusLanePressureGroup)]
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

        private delegate void CurrentSaveSettingsMutator(ref EnforcementGameplaySettingsState state);
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            Dictionary<string, string> entries = new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Traffic Law Enforcement" },
                { m_Setting.GetOptionTabLocaleID(Setting.kCurrentSaveTab), "Current save settings" },
                { m_Setting.GetOptionTabLocaleID(Setting.kNewSaveDefaultsTab), "New save defaults" },
                { m_Setting.GetOptionTabLocaleID(Setting.kPolicyImpactTab), "Policy impact" },
                { m_Setting.GetOptionTabLocaleID(Setting.kDebugTab), "Debug / Logging" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kGeneralGroup), "General" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kBusLaneAuthorizedGroup), "PT-lane authorization: vanilla-authorized vehicles" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kBusLaneAdditionalGroup), "PT-lane authorization: vanilla-unauthorized vehicles" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kBusLanePressureGroup), "Illegal PT-lane occupancy response" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kFineGroup), "Fine amounts" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kRepeatOffenderGroup), "Repeat offender policy" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kTemplateActionsGroup), "Template actions" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kPolicyImpactGroup), "Policy impact metrics" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kDebugGroup), "Debug" },
            };

            AddGameplay(entries, nameof(Setting.EnablePublicTransportLaneEnforcement), nameof(Setting.DefaultEnablePublicTransportLaneEnforcement), "Enable PT-lane enforcement", "Turn public-transport-only-lane violation detection, fines, and pathfinding penalties on or off.");
            AddGameplay(entries, nameof(Setting.EnableMidBlockCrossingEnforcement), nameof(Setting.DefaultEnableMidBlockCrossingEnforcement), "Enable mid-block enforcement", "Turn mid-block U-turn and centerline-crossing detection, fines, and pathfinding penalties on or off.");
            AddGameplay(entries, nameof(Setting.EnableIntersectionMovementEnforcement), nameof(Setting.DefaultEnableIntersectionMovementEnforcement), "Enable intersection enforcement", "Turn intersection movement-rule violation detection, fines, and pathfinding penalties on or off.");
            AddGameplay(entries, nameof(Setting.AllowRoadPublicTransportVehicles), nameof(Setting.DefaultAllowRoadPublicTransportVehicles), "Road public transport vehicles", "While this is on, road public transport vehicles are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes. Even then, vehicles actively performing emergency duties are still exempt from this mod's traffic-law fines.");
            AddGameplay(entries, nameof(Setting.AllowTaxis), nameof(Setting.DefaultAllowTaxis), "Taxis", "While this is on, taxis are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes.");
            AddGameplay(entries, nameof(Setting.AllowPoliceCars), nameof(Setting.DefaultAllowPoliceCars), "Police cars", "While this is on, police cars are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes. Even then, vehicles actively performing emergency duties are still exempt from this mod's traffic-law fines.");
            AddGameplay(entries, nameof(Setting.AllowFireEngines), nameof(Setting.DefaultAllowFireEngines), "Fire engines", "While this is on, fire engines are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes. Even then, vehicles actively performing emergency duties are still exempt from this mod's traffic-law fines.");
            AddGameplay(entries, nameof(Setting.AllowAmbulances), nameof(Setting.DefaultAllowAmbulances), "Ambulances", "While this is on, ambulances are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes. Even then, vehicles actively performing emergency duties are still exempt from this mod's traffic-law fines.");
            AddGameplay(entries, nameof(Setting.AllowGarbageTrucks), nameof(Setting.DefaultAllowGarbageTrucks), "Garbage trucks", "While this is on, garbage trucks are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes.");
            AddGameplay(entries, nameof(Setting.AllowPostVans), nameof(Setting.DefaultAllowPostVans), "Post vans", "While this is on, post vans are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes.");
            AddGameplay(entries, nameof(Setting.AllowRoadMaintenanceVehicles), nameof(Setting.DefaultAllowRoadMaintenanceVehicles), "Road maintenance vehicles", "While this is on, road maintenance vehicles are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes.");
            AddGameplay(entries, nameof(Setting.AllowSnowplows), nameof(Setting.DefaultAllowSnowplows), "Snowplows", "While this is on, snowplows are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes.");
            AddGameplay(entries, nameof(Setting.AllowVehicleMaintenanceVehicles), nameof(Setting.DefaultAllowVehicleMaintenanceVehicles), "Vehicle maintenance vehicles", "While this is on, vehicle maintenance vehicles are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes.");
            AddGameplay(entries, nameof(Setting.AllowPersonalCars), nameof(Setting.DefaultAllowPersonalCars), "Personal cars", "While this is on, personal cars are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes. They still do not prefer PT lanes over ordinary lanes.");
            AddGameplay(entries, nameof(Setting.AllowDeliveryTrucks), nameof(Setting.DefaultAllowDeliveryTrucks), "Delivery trucks", "While this is on, delivery trucks are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes. They still do not prefer PT lanes over ordinary lanes.");
            AddGameplay(entries, nameof(Setting.AllowCargoTransportVehicles), nameof(Setting.DefaultAllowCargoTransportVehicles), "Cargo transport vehicles", "While this is on, cargo transport vehicles are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes. They still do not prefer PT lanes over ordinary lanes.");
            AddGameplay(entries, nameof(Setting.AllowHearses), nameof(Setting.DefaultAllowHearses), "Hearses", "While this is on, hearses are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes. They still do not prefer PT lanes over ordinary lanes.");
            AddGameplay(entries, nameof(Setting.AllowPrisonerTransports), nameof(Setting.DefaultAllowPrisonerTransports), "Prisoner transports", "While this is on, prisoner transports are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes. They still do not prefer PT lanes over ordinary lanes.");
            AddGameplay(entries, nameof(Setting.AllowParkMaintenanceVehicles), nameof(Setting.DefaultAllowParkMaintenanceVehicles), "Park maintenance vehicles", "While this is on, park maintenance vehicles are not fined by this mod for using public-transport-only lanes. Turning this off makes them subject to enforcement when using those lanes. They still do not prefer PT lanes over ordinary lanes.");
            AddGameplay(entries, nameof(Setting.PublicTransportLaneExitPressureThresholdDays), nameof(Setting.DefaultPublicTransportLaneExitPressureThresholdDays), "Illegal PT-lane occupancy reroute threshold (days)", "If a vehicle that is not allowed on public-transport-only lanes stays there for at least this many in-game days, mark its PathOwner obsolete to encourage a new route. 0 means apply as soon as possible after the violation starts.");
            AddGameplay(entries, nameof(Setting.PublicTransportLaneFineAmount), nameof(Setting.DefaultPublicTransportLaneFineAmount), "PT-lane violation fine", "Fine amount charged when a vehicle illegally uses a public-transport-only lane.");
            AddGameplay(entries, nameof(Setting.MidBlockCrossingFineAmount), nameof(Setting.DefaultMidBlockCrossingFineAmount), "Mid-block crossing fine", "Fine amount charged for mid-block U-turns and illegal centerline crossings for access.");
            AddGameplay(entries, nameof(Setting.IntersectionMovementFineAmount), nameof(Setting.DefaultIntersectionMovementFineAmount), "Intersection movement fine", "Fine amount charged when a vehicle crosses an intersection with a maneuver not allowed from its lane.");
            AddRepeat(entries, nameof(Setting.EnablePublicTransportLaneRepeatPenalty), nameof(Setting.PublicTransportLaneRepeatWindowMonths), nameof(Setting.PublicTransportLaneRepeatThreshold), nameof(Setting.PublicTransportLaneRepeatMultiplierPercent), nameof(Setting.DefaultEnablePublicTransportLaneRepeatPenalty), nameof(Setting.DefaultPublicTransportLaneRepeatWindowMonths), nameof(Setting.DefaultPublicTransportLaneRepeatThreshold), nameof(Setting.DefaultPublicTransportLaneRepeatMultiplierPercent), "PT-lane", "PT-lane");
            AddRepeat(entries, nameof(Setting.EnableMidBlockCrossingRepeatPenalty), nameof(Setting.MidBlockCrossingRepeatWindowMonths), nameof(Setting.MidBlockCrossingRepeatThreshold), nameof(Setting.MidBlockCrossingRepeatMultiplierPercent), nameof(Setting.DefaultEnableMidBlockCrossingRepeatPenalty), nameof(Setting.DefaultMidBlockCrossingRepeatWindowMonths), nameof(Setting.DefaultMidBlockCrossingRepeatThreshold), nameof(Setting.DefaultMidBlockCrossingRepeatMultiplierPercent), "Mid-block", "mid-block crossing");
            AddRepeat(entries, nameof(Setting.EnableIntersectionMovementRepeatPenalty), nameof(Setting.IntersectionMovementRepeatWindowMonths), nameof(Setting.IntersectionMovementRepeatThreshold), nameof(Setting.IntersectionMovementRepeatMultiplierPercent), nameof(Setting.DefaultEnableIntersectionMovementRepeatPenalty), nameof(Setting.DefaultIntersectionMovementRepeatWindowMonths), nameof(Setting.DefaultIntersectionMovementRepeatThreshold), nameof(Setting.DefaultIntersectionMovementRepeatMultiplierPercent), "Intersection", "intersection movement-rule violation");
            Add(entries, nameof(Setting.ResetCurrentSaveSettingsToCodeDefaults), "Reset current save settings to code defaults", "Reset the current save's gameplay rules to this mod's built-in code defaults.");
            Add(entries, nameof(Setting.CopyCurrentSaveSettingsToDefaults), "Copy current save settings to defaults", "Copy the current save's gameplay rules into the new-save defaults template.");
            Add(entries, nameof(Setting.ResetDefaultsToCodeDefaults), "Reset defaults to code defaults", "Reset the new-save defaults template to this mod's built-in code defaults.");
            Add(entries, nameof(Setting.EnableEstimatedRerouteLogging), "Enable estimated reroute logging", "Debug-only. Writes only 'Pathfinding reroute (estimated)' logs. Turning this off disables reroute debug tracking and logging only; traffic-law detection, fines, repeat-offender logic, and pathfinding penalties still run.");
            Add(entries, nameof(Setting.EnableEnforcementEventLogging), "Enable enforcement event logging", "Debug-only. Writes traffic-law enforcement event logs: PT-lane, mid-block, and intersection violation logs, fine-income collection logs, and bus-lane exit-pressure logs. Turning this off affects logging only; enforcement behavior and penalties still run.");
            Add(entries, nameof(Setting.EnableType2PublicTransportLaneUsageLogging), "Enable PT-lane usage logging for public vehicles denied to use PT lanes", "Debug-only. Writes logs when Type 2 vehicles (vehicles that can use PT lanes in vanilla but are denied to use them by this mod's settings) are observed using PT-only lanes. Turning this off affects logging only; permissions and enforcement behavior still run.");
            Add(entries, nameof(Setting.EnableType3PublicTransportLaneUsageLogging), "Enable PT-lane usage logging for non-public vehicles allowed to use PT lanes", "Debug-only. Writes logs when Type 3 vehicles (vehicles that cannot use PT lanes in vanilla but are allowed to use them by this mod's settings) are observed using PT-only lanes. Turning this off affects logging only; permissions and enforcement behavior still run.");
            Add(entries, nameof(Setting.EnableType4PublicTransportLaneUsageLogging), "Enable PT-lane usage logging for non-public vehicles denied to use PT lanes", "Debug-only. Writes logs when Type 4 vehicles (vehicles that cannot use PT lanes in vanilla and are denied to use them by this mod's settings) are observed using PT-only lanes. Turning this off affects logging only; permissions and enforcement behavior still run.");
            Add(entries, nameof(Setting.EnablePathfindingPenaltyDiagnosticLogging), "Enable pathfinding penalty diagnostic logging", "Debug-only. Writes pathfinding money-axis penalty apply logs and shared PathfindCarData diagnostic logs. Turning this off affects logging only; pathfinding penalties still run.");
            Add(entries, nameof(Setting.EnablePathObsoleteSourceLogging), "Enable path obsolete source logging", "Debug-only. Writes logs only when a system actually marks a vehicle PathOwner obsolete, including the source system and key reason data. Turning this off affects logging only; rerouting behavior still runs.");
            Add(entries, nameof(Setting.PolicyImpactTotalStatistics), "Total violations", "Shows the rolling recent-1-in-game-month total violation rate, suppression failure rate, and fines.");
            Add(entries, nameof(Setting.PolicyImpactPublicTransportLaneStatistics), "PT-lane violations", "Shows the rolling recent-1-in-game-month PT-lane violation rate, suppression failure rate, and fines.");
            Add(entries, nameof(Setting.PolicyImpactMidBlockStatistics), "Mid-block violations", "Shows the rolling recent-1-in-game-month mid-block violation rate, suppression failure rate, and fines.");
            Add(entries, nameof(Setting.PolicyImpactIntersectionStatistics), "Intersection violations", "Shows the rolling recent-1-in-game-month intersection violation rate, suppression failure rate, and fines.");
            entries[BudgetUIPatches.FineIncomeBudgetItemLocaleId] = "Traffic law enforcement";
            entries[BudgetUIPatches.FineIncomePublicTransportLaneLocaleId] = "Public-transport lane violations";
            entries[BudgetUIPatches.FineIncomeMidBlockCrossingLocaleId] = "Mid-block violations";
            entries[BudgetUIPatches.FineIncomeIntersectionMovementLocaleId] = "Intersection violations";
            entries[BudgetUIPatches.FineIncomeBudgetDescriptionLocaleId] = "Fine revenue collected from traffic-law enforcement during the last 1 in-game month.";
            entries[EnforcementPolicyImpactService.kLoadedSaveOnlyLocaleId] = "Available only in a loaded save.";
            entries[EnforcementPolicyImpactService.kWaitingForTimeLocaleId] = "Waiting for in-game time initialization.";
            entries[EnforcementPolicyImpactService.kNoDataLocaleId] = "No pathfinding requests, fined violations, or rerouted pathfinding outcomes that avoided penalized routes have been recorded yet.";
            entries[EnforcementPolicyImpactService.kNoteLocaleId] = "Note: All policy-impact metrics use a rolling recent-1-in-game-month window. A counts pathfinding requests, not unique trips. D counts estimated rerouted pathfinding outcomes that gave up a penalized route. Per-type D counts can overlap when one reroute avoids multiple penalty types.";
            entries[EnforcementPolicyImpactService.kTotalLabelLocaleId] = "Total";
            entries[EnforcementPolicyImpactService.kPublicTransportLaneLabelLocaleId] = "PT-lane violations";
            entries[EnforcementPolicyImpactService.kMidBlockLabelLocaleId] = "Mid-block violations";
            entries[EnforcementPolicyImpactService.kIntersectionLabelLocaleId] = "Intersection violations";
            entries[EnforcementPolicyImpactService.kStatisticsLineFormat] = "{0}: violation rate {1}, suppression failure rate {2}, fines {3}₡.";
            entries[MonthlyEnforcementChirperSystem.kSenderTextLocaleId] = "Traffic Law Enforcement";
            entries[MonthlyEnforcementChirperSystem.kPeriodPointFormatLocaleId] = "{0} {1} {2:00}:{3:00}";
            entries[MonthlyEnforcementChirperSystem.kReportHeaderFormatLocaleId] = "Traffic enforcement report for {0} to {1}: {2} violations.";
            entries[MonthlyEnforcementChirperSystem.kTotalLineFormatLocaleId] = "violation rate {0}, suppression failure rate {1}, fines {2}₡.";
            entries[MonthlyEnforcementChirperSystem.kPublicTransportLaneLineFormatLocaleId] = "violation rate {0}, suppression failure rate {1}, fines {2}₡.";
            entries[MonthlyEnforcementChirperSystem.kMidBlockLineFormatLocaleId] = "violation rate {0}, suppression failure rate {1}, fines {2}₡.";
            entries[MonthlyEnforcementChirperSystem.kIntersectionLineFormatLocaleId] = "violation rate {0}, suppression failure rate {1}, fines {2}₡.";
            entries[MonthlyEnforcementChirperSystem.kNoRateLocaleId] = "No request-based rate available yet.";
            Add(entries, nameof(Setting.SendMonthlyChirperPreviewNow), "Send Chirper report now", "Immediately posts one Chirper report for the rolling recent-1-in-game-month window, from one in-game month ago to the moment you press this button.");
            return entries;
        }

        public void Unload()
        {
        }

        private void Add(Dictionary<string, string> entries, string optionName, string label, string desc)
        {
            entries[m_Setting.GetOptionLabelLocaleID(optionName)] = label;
            entries[m_Setting.GetOptionDescLocaleID(optionName)] = desc;
        }

        public string GetPublicTransportLaneFlagGrantExperimentRoleDisplayName(PublicTransportLaneFlagGrantExperimentRole role)
        {
            switch (role)
            {
                case PublicTransportLaneFlagGrantExperimentRole.PersonalCar: return "Personal cars";
                case PublicTransportLaneFlagGrantExperimentRole.DeliveryTruck: return "Delivery trucks";
                case PublicTransportLaneFlagGrantExperimentRole.CargoTransport: return "Cargo transport vehicles";
                case PublicTransportLaneFlagGrantExperimentRole.Hearse: return "Hearses";
                case PublicTransportLaneFlagGrantExperimentRole.PrisonerTransport: return "Prisoner transports";
                case PublicTransportLaneFlagGrantExperimentRole.ParkMaintenanceVehicle: return "Park maintenance vehicles";
                default: return "None";
            }
        }

        private void AddGameplay(Dictionary<string, string> entries, string currentName, string defaultName, string label, string desc)
        {
            Add(entries, currentName, label, desc);
            Add(entries, defaultName, label, "Default value for newly created saves. " + desc);
        }

        private void AddRepeat(Dictionary<string, string> entries, string currentEnableName, string currentWindowName, string currentThresholdName, string currentMultiplierName, string defaultEnableName, string defaultWindowName, string defaultThresholdName, string defaultMultiplierName, string label, string sentenceLabel)
        {
            AddGameplay(entries, currentEnableName, defaultEnableName, $"Enable repeat-offender penalty for {label}", $"Apply a higher fine to vehicles that repeatedly commit {sentenceLabel} violations.");
            AddGameplay(entries, currentWindowName, defaultWindowName, $"{label} repeat-offender window (in-game months)", $"How long the same vehicle's {sentenceLabel} history is kept for repeat-offender counting. This uses in-game time, not real time. 12 in-game months equal 1 in-game year. On the vanilla time scale, 1 in-game month is roughly 1 in-game day. Mods that alter time progression can distort this window.");
            AddGameplay(entries, currentThresholdName, defaultThresholdName, $"{label} repeat-offender threshold", $"How many {sentenceLabel} violations within the window are required before the repeat-offender penalty applies.");
            AddGameplay(entries, currentMultiplierName, defaultMultiplierName, $"{label} repeat-offender multiplier", $"Multiplier applied to the {sentenceLabel} fine once the repeat-offender threshold is reached.");
        }
    }

    public class LocaleKO : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleKO(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            Dictionary<string, string> entries = new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "교통법규 단속" },
                { m_Setting.GetOptionTabLocaleID(Setting.kCurrentSaveTab), "현재 세이브 설정" },
                { m_Setting.GetOptionTabLocaleID(Setting.kNewSaveDefaultsTab), "새 세이브 기본값" },
                { m_Setting.GetOptionTabLocaleID(Setting.kPolicyImpactTab), "정책 효과" },
                { m_Setting.GetOptionTabLocaleID(Setting.kDebugTab), "디버그 / 로그" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kGeneralGroup), "일반" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kBusLaneAuthorizedGroup), "대중교통 전용차선 진입 허가: 바닐라 허용 차량" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kBusLaneAdditionalGroup), "대중교통 전용차선 진입 허가: 바닐라 불허 차량" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kBusLanePressureGroup), "대중교통 전용차선 불법 점유 대응" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kFineGroup), "벌금 액수" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kRepeatOffenderGroup), "상습 위반 가중처벌" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kTemplateActionsGroup), "기본값 변경" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kPolicyImpactGroup), "위반율 지표" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kDebugGroup), "디버그" },
            };

            AddGameplay(entries, nameof(Setting.EnablePublicTransportLaneEnforcement), nameof(Setting.DefaultEnablePublicTransportLaneEnforcement), "대중교통 전용차선 단속 활성화", "대중교통 전용차선 위반 감지, 벌금 부과, 대중교통 전용차선 경로탐색 페널티를 켜거나 끕니다.");
            AddGameplay(entries, nameof(Setting.EnableMidBlockCrossingEnforcement), nameof(Setting.DefaultEnableMidBlockCrossingEnforcement), "중앙선 침범 단속 활성화", "유턴 및 진출입 관련 중앙선 침범 위반 감지, 벌금 부과, 경로탐색 페널티를 켜거나 끕니다.");
            AddGameplay(entries, nameof(Setting.EnableIntersectionMovementEnforcement), nameof(Setting.DefaultEnableIntersectionMovementEnforcement), "교차로 통행규칙 단속 활성화", "교차로 통행규칙 위반 감지, 벌금 부과, 경로탐색 페널티를 켜거나 끕니다.");
            AddGameplay(entries, nameof(Setting.AllowRoadPublicTransportVehicles), nameof(Setting.DefaultAllowRoadPublicTransportVehicles), "도로 대중교통 차량", "이 옵션이 켜져 있으면 도로 대중교통 차량이 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다. 그래도 긴급 임무 수행 중에는 이 모드의 교통법규 위반 단속을 받지 않습니다.");
            AddGameplay(entries, nameof(Setting.AllowTaxis), nameof(Setting.DefaultAllowTaxis), "택시", "이 옵션이 켜져 있으면 택시가 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다.");
            AddGameplay(entries, nameof(Setting.AllowPoliceCars), nameof(Setting.DefaultAllowPoliceCars), "경찰차", "이 옵션이 켜져 있으면 경찰차가 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다. 그래도 긴급 임무 수행 중에는 이 모드의 교통법규 위반 단속을 받지 않습니다.");
            AddGameplay(entries, nameof(Setting.AllowFireEngines), nameof(Setting.DefaultAllowFireEngines), "소방차", "이 옵션이 켜져 있으면 소방차가 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다. 그래도 긴급 임무 수행 중에는 이 모드의 교통법규 위반 단속을 받지 않습니다.");
            AddGameplay(entries, nameof(Setting.AllowAmbulances), nameof(Setting.DefaultAllowAmbulances), "구급차", "이 옵션이 켜져 있으면 구급차가 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다. 그래도 긴급 임무 수행 중에는 이 모드의 교통법규 위반 단속을 받지 않습니다.");
            AddGameplay(entries, nameof(Setting.AllowGarbageTrucks), nameof(Setting.DefaultAllowGarbageTrucks), "쓰레기 수거차", "이 옵션이 켜져 있으면 쓰레기 수거차가 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다.");
            AddGameplay(entries, nameof(Setting.AllowPostVans), nameof(Setting.DefaultAllowPostVans), "우편 차량", "이 옵션이 켜져 있으면 우편 차량이 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다.");
            AddGameplay(entries, nameof(Setting.AllowRoadMaintenanceVehicles), nameof(Setting.DefaultAllowRoadMaintenanceVehicles), "도로 정비 차량", "이 옵션이 켜져 있으면 도로 정비 차량이 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다.");
            AddGameplay(entries, nameof(Setting.AllowSnowplows), nameof(Setting.DefaultAllowSnowplows), "제설차", "이 옵션이 켜져 있으면 제설차가 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다.");
            AddGameplay(entries, nameof(Setting.AllowVehicleMaintenanceVehicles), nameof(Setting.DefaultAllowVehicleMaintenanceVehicles), "차량 정비 차량", "이 옵션이 켜져 있으면 차량 정비 차량이 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다.");
            AddGameplay(entries, nameof(Setting.AllowPersonalCars), nameof(Setting.DefaultAllowPersonalCars), "개인 승용차", "이 옵션이 켜져 있으면 개인 승용차가 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다. 다만 일반 차선보다 대중교통 전용차선을 더 선호하도록 바뀌지는 않습니다.");
            AddGameplay(entries, nameof(Setting.AllowDeliveryTrucks), nameof(Setting.DefaultAllowDeliveryTrucks), "배달 트럭", "이 옵션이 켜져 있으면 배달 트럭이 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다. 다만 일반 차선보다 대중교통 전용차선을 더 선호하도록 바뀌지는 않습니다.");
            AddGameplay(entries, nameof(Setting.AllowCargoTransportVehicles), nameof(Setting.DefaultAllowCargoTransportVehicles), "화물 운송 차량", "이 옵션이 켜져 있으면 화물 운송 차량이 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다. 다만 일반 차선보다 대중교통 전용차선을 더 선호하도록 바뀌지는 않습니다.");
            AddGameplay(entries, nameof(Setting.AllowHearses), nameof(Setting.DefaultAllowHearses), "영구차", "이 옵션이 켜져 있으면 영구차가 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다. 다만 일반 차선보다 대중교통 전용차선을 더 선호하도록 바뀌지는 않습니다.");
            AddGameplay(entries, nameof(Setting.AllowPrisonerTransports), nameof(Setting.DefaultAllowPrisonerTransports), "죄수 호송차", "이 옵션이 켜져 있으면 죄수 호송차가 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다. 다만 일반 차선보다 대중교통 전용차선을 더 선호하도록 바뀌지는 않습니다.");
            AddGameplay(entries, nameof(Setting.AllowParkMaintenanceVehicles), nameof(Setting.DefaultAllowParkMaintenanceVehicles), "공원 정비 차량", "이 옵션이 켜져 있으면 공원 정비 차량이 대중교통 전용차선을 이용해도 이 모드의 단속을 받지 않습니다. 이 옵션을 끄면 그 차선을 이용할 때 단속 대상이 됩니다. 다만 일반 차선보다 대중교통 전용차선을 더 선호하도록 바뀌지는 않습니다.");
            AddGameplay(entries, nameof(Setting.PublicTransportLaneExitPressureThresholdDays), nameof(Setting.DefaultPublicTransportLaneExitPressureThresholdDays), "불법 대중교통 전용차선 점유 재경로화 기준 시간 (일)", "대중교통 전용차선 진입이 허용되지 않은 차량이 그 차선을 이 정도 게임 시간(일) 이상 계속 점유하면 경로를 다시 찾도록 PathOwner를 obsolete 상태로 표시합니다. 0이면 위반 시작 직후 가능한 한 빨리 적용합니다.");
            AddGameplay(entries, nameof(Setting.PublicTransportLaneFineAmount), nameof(Setting.DefaultPublicTransportLaneFineAmount), "대중교통 전용차선 위반 벌금", "차량이 대중교통 전용차선을 불법으로 이용했을 때 부과할 벌금입니다.");
            AddGameplay(entries, nameof(Setting.MidBlockCrossingFineAmount), nameof(Setting.DefaultMidBlockCrossingFineAmount), "중앙선 침범 벌금", "유턴이나 진출입을 위해 중앙선을 불법으로 침범했을 때 부과할 벌금입니다.");
            AddGameplay(entries, nameof(Setting.IntersectionMovementFineAmount), nameof(Setting.DefaultIntersectionMovementFineAmount), "교차로 통행규칙 위반 벌금", "차량이 자기 차선에서 허용되지 않은 방식으로 교차로를 통과했을 때 부과할 벌금입니다.");
            AddRepeat(entries, nameof(Setting.EnablePublicTransportLaneRepeatPenalty), nameof(Setting.PublicTransportLaneRepeatWindowMonths), nameof(Setting.PublicTransportLaneRepeatThreshold), nameof(Setting.PublicTransportLaneRepeatMultiplierPercent), nameof(Setting.DefaultEnablePublicTransportLaneRepeatPenalty), nameof(Setting.DefaultPublicTransportLaneRepeatWindowMonths), nameof(Setting.DefaultPublicTransportLaneRepeatThreshold), nameof(Setting.DefaultPublicTransportLaneRepeatMultiplierPercent), "대중교통 전용차선", "대중교통 전용차선 위반");
            AddRepeat(entries, nameof(Setting.EnableMidBlockCrossingRepeatPenalty), nameof(Setting.MidBlockCrossingRepeatWindowMonths), nameof(Setting.MidBlockCrossingRepeatThreshold), nameof(Setting.MidBlockCrossingRepeatMultiplierPercent), nameof(Setting.DefaultEnableMidBlockCrossingRepeatPenalty), nameof(Setting.DefaultMidBlockCrossingRepeatWindowMonths), nameof(Setting.DefaultMidBlockCrossingRepeatThreshold), nameof(Setting.DefaultMidBlockCrossingRepeatMultiplierPercent), "중앙선 침범", "중앙선 침범");
            AddRepeat(entries, nameof(Setting.EnableIntersectionMovementRepeatPenalty), nameof(Setting.IntersectionMovementRepeatWindowMonths), nameof(Setting.IntersectionMovementRepeatThreshold), nameof(Setting.IntersectionMovementRepeatMultiplierPercent), nameof(Setting.DefaultEnableIntersectionMovementRepeatPenalty), nameof(Setting.DefaultIntersectionMovementRepeatWindowMonths), nameof(Setting.DefaultIntersectionMovementRepeatThreshold), nameof(Setting.DefaultIntersectionMovementRepeatMultiplierPercent), "교차로 통행규칙 위반", "교차로 통행규칙 위반");
            Add(entries, nameof(Setting.ResetCurrentSaveSettingsToCodeDefaults), "현재 세이브 설정을 코드 기본값으로 복원", "현재 세이브의 게임플레이 규칙을 이 모드의 내장 코드 기본값으로 되돌립니다.");
            Add(entries, nameof(Setting.CopyCurrentSaveSettingsToDefaults), "현재 세이브 값을 기본값으로 복사", "현재 세이브의 게임플레이 규칙을 새 세이브 기본값 템플릿으로 복사합니다.");
            Add(entries, nameof(Setting.ResetDefaultsToCodeDefaults), "기본값을 코드 기본값으로 초기화", "새 세이브 기본값 템플릿을 이 모드의 내장 기본값으로 되돌립니다.");
            Add(entries, nameof(Setting.EnableEstimatedRerouteLogging), "추정 우회 경로 로그 기록", "디버그 전용입니다. 교통법규 위반 단속을 피하기 위해 경로를 수정한 교통량의 로그를 기록합니다. 이 옵션을 꺼도 위반 감지, 벌금 부과, 상습 위반 처리, 경로탐색 페널티는 계속 동작합니다.");
            Add(entries, nameof(Setting.EnableEnforcementEventLogging), "교통법규 단속 이벤트 로그 기록", "디버그 전용입니다. 대중교통 전용차선, 중앙선 침범, 교차로 통행규칙 위반 로그와 벌금 수익 징수 로그, 대중교통 전용차선 이탈 압박 로그를 기록합니다. 이 옵션을 꺼도 단속 동작과 벌금 부과는 계속 진행됩니다.");
            Add(entries, nameof(Setting.EnableType2PublicTransportLaneUsageLogging), "대중교통 전용차선 이용이 불허된 대중교통 차량의 대중교통 전용차선 사용 로그 기록", "디버그 전용입니다. Type 2 차량 (바닐라 기준으로는 대중교통 전용차선을 이용할 수 있지만 이 모드의 설정에서 대중교통 전용차선 이용이 불허된 차량) 이 실제로 그 차선을 이용한 사실을 로그로 기록합니다. 이 옵션을 꺼도 통행 허용 여부와 단속 동작은 계속 유지됩니다.");
            Add(entries, nameof(Setting.EnableType3PublicTransportLaneUsageLogging), "대중교통 전용차선 이용이 허가된 비대중교통 차량의 대중교통 전용차선 사용 로그 기록", "디버그 전용입니다. Type 3 차량 (바닐라 기준으로는 대중교통 전용차선을 이용할 수 없지만 이 모드의 설정에서 대중교통 전용차선 이용이 허가된 차량) 이 실제로 그 차선을 이용한 사실을 로그로 기록합니다. 이 옵션을 꺼도 통행 허용 여부와 단속 동작은 계속 유지됩니다.");
            Add(entries, nameof(Setting.EnableType4PublicTransportLaneUsageLogging), "대중교통 전용차선 이용이 불허된 비대중교통 차량의 대중교통 전용차선 사용 로그 기록", "디버그 전용입니다. Type 4 차량 (바닐라 기준으로도 대중교통 전용차선을 이용할 수 없으며 이 모드의 설정에서도 대중교통 전용차선 이용이 불허된 차량) 이 실제로 그 차선을 이용한 사실을 로그로 기록합니다. 이 옵션을 꺼도 통행 허용 여부와 단속 동작은 계속 유지됩니다.");
            Add(entries, nameof(Setting.EnablePathfindingPenaltyDiagnosticLogging), "경로탐색 페널티 진단 로그 기록", "디버그 전용입니다. 경로탐색 money-axis 페널티 적용 로그와 shared PathfindCarData 진단 로그를 기록합니다. 이 옵션을 꺼도 경로탐색 페널티 자체는 계속 적용됩니다.");
            Add(entries, nameof(Setting.EnablePathObsoleteSourceLogging), "경로 obsolete 원인 로그 기록", "디버그 전용입니다. 어떤 시스템이 실제로 차량의 PathOwner를 obsolete 상태로 만들었는지와 주요 판단 근거를 로그로 기록합니다. 이 옵션을 꺼도 재경로 동작 자체는 계속 진행됩니다.");
            Add(entries, nameof(Setting.PolicyImpactTotalStatistics), "전체 교통법규 위반", "게임 시간 최근 1달 기준 전체 위반율, 억제 실패율, 벌금액을 표시합니다.");
            Add(entries, nameof(Setting.PolicyImpactPublicTransportLaneStatistics), "대중교통 전용차선 무단 이용", "게임 시간 최근 1달 기준 대중교통 전용차선 통행규칙 위반율, 억제 실패율, 벌금액을 표시합니다.");
            Add(entries, nameof(Setting.PolicyImpactMidBlockStatistics), "중앙선 침범", "게임 시간 최근 1달 기준 중앙선 통행규칙 위반율, 억제 실패율, 벌금액을 표시합니다.");
            Add(entries, nameof(Setting.PolicyImpactIntersectionStatistics), "교차로 통행규칙 위반", "게임 시간 최근 1달 기준 교차로 통행규칙 위반율, 억제 실패율, 벌금액을 표시합니다.");
            entries[BudgetUIPatches.FineIncomeBudgetItemLocaleId] = "교통법규 단속";
            entries[BudgetUIPatches.FineIncomePublicTransportLaneLocaleId] = "대중교통 전용차선 침입";
            entries[BudgetUIPatches.FineIncomeMidBlockCrossingLocaleId] = "중앙선 침범";
            entries[BudgetUIPatches.FineIncomeIntersectionMovementLocaleId] = "교차로 통행규칙 위반";
            entries[BudgetUIPatches.FineIncomeBudgetDescriptionLocaleId] = "최근 1달 동안 교통법규 단속으로 징수된 벌금 수입입니다.";
            entries[EnforcementPolicyImpactService.kLoadedSaveOnlyLocaleId] = "세이브를 로드한 뒤 표시됩니다.";
            entries[EnforcementPolicyImpactService.kWaitingForTimeLocaleId] = "인게임 시간 초기화 대기 중입니다.";
            entries[EnforcementPolicyImpactService.kNoDataLocaleId] = "경로탐색 요청, 실제 위반, 또는 벌점 경로를 피한 것으로 추정되는 경로탐색 결과에 대한 기록이 아직 없습니다.";
            entries[EnforcementPolicyImpactService.kNoteLocaleId] = "참고: 정책 효과 지표는 모두 게임 시간으로 최근 1달 기준입니다. A는 고유 이동 건수가 아니라 전체 경로탐색 요청 수입니다. D는 단속 가능성 때문에 벌점 경로를 포기한 것으로 추정되는 경로탐색 결과 수입니다. 여러 위반 유형을 동시에 피한 결과가 있다면 유형별 D 값은 겹쳐 집계될 수 있습니다.";
            entries[EnforcementPolicyImpactService.kTotalLabelLocaleId] = "전체";
            entries[EnforcementPolicyImpactService.kPublicTransportLaneLabelLocaleId] = "대중교통 전용차선 침입";
            entries[EnforcementPolicyImpactService.kMidBlockLabelLocaleId] = "중앙선 침범";
            entries[EnforcementPolicyImpactService.kIntersectionLabelLocaleId] = "교차로 통행규칙 위반";
            entries[EnforcementPolicyImpactService.kStatisticsLineFormat] = "{0}: 위반율 {1}, 억제 실패율 {2}, 벌금 {3}₡."; // This sentence is used in EnforcementPolicyImpactService.cs
            entries[MonthlyEnforcementChirperSystem.kSenderTextLocaleId] = "교통관리과";
            entries[MonthlyEnforcementChirperSystem.kPeriodPointFormatLocaleId] = "{1}년 {0} {2:00}:{3:00}";
            entries[MonthlyEnforcementChirperSystem.kReportHeaderFormatLocaleId] = "{0}부터 {1}까지 교통법규 단속 보고입니다. 총 위반 적발 {2}건.";
            entries[MonthlyEnforcementChirperSystem.kTotalLineFormatLocaleId] = "위반율 {0}, 억제 실패율 {1}, 벌금 {2}₡.";
            entries[MonthlyEnforcementChirperSystem.kPublicTransportLaneLineFormatLocaleId] = "위반율 {0}, 억제 실패율 {1}, 벌금 {2}₡.";
            entries[MonthlyEnforcementChirperSystem.kMidBlockLineFormatLocaleId] = "위반율 {0}, 억제 실패율 {1}, 벌금 {2}₡.";
            entries[MonthlyEnforcementChirperSystem.kIntersectionLineFormatLocaleId] = "위반율 {0}, 억제 실패율 {1}, 벌금 {2}₡.";
            entries[MonthlyEnforcementChirperSystem.kNoRateLocaleId] = "경로탐색 요청 기준 집계 없음";
            Add(entries, nameof(Setting.SendMonthlyChirperPreviewNow), "지금 Chirper 보고 보내기", "지금 시점부터 게임 시간 1달 전까지의 단속 실적을 Chirper로 즉시 한 번 게시합니다.");
            return entries;
        }

        public void Unload()
        {
        }

        private void Add(Dictionary<string, string> entries, string optionName, string label, string desc)
        {
            entries[m_Setting.GetOptionLabelLocaleID(optionName)] = label;
            entries[m_Setting.GetOptionDescLocaleID(optionName)] = desc;
        }

        public string GetPublicTransportLaneFlagGrantExperimentRoleDisplayName(PublicTransportLaneFlagGrantExperimentRole role)
        {
            switch (role)
            {
                case PublicTransportLaneFlagGrantExperimentRole.PersonalCar: return "개인 승용차";
                case PublicTransportLaneFlagGrantExperimentRole.DeliveryTruck: return "배달 트럭";
                case PublicTransportLaneFlagGrantExperimentRole.CargoTransport: return "화물 운송 차량";
                case PublicTransportLaneFlagGrantExperimentRole.Hearse: return "영구차";
                case PublicTransportLaneFlagGrantExperimentRole.PrisonerTransport: return "죄수 호송차";
                case PublicTransportLaneFlagGrantExperimentRole.ParkMaintenanceVehicle: return "공원 정비 차량";
                default: return "없음";
            }
        }

        private void AddGameplay(Dictionary<string, string> entries, string currentName, string defaultName, string label, string desc)
        {
            Add(entries, currentName, label, desc);
            Add(entries, defaultName, label, "새로 만드는 세이브에 적용할 기본값입니다. " + desc);
        }

        private void AddRepeat(Dictionary<string, string> entries, string currentEnableName, string currentWindowName, string currentThresholdName, string currentMultiplierName, string defaultEnableName, string defaultWindowName, string defaultThresholdName, string defaultMultiplierName, string label, string sentenceLabel)
        {
            AddGameplay(entries, currentEnableName, defaultEnableName, $"{label} 상습 위반 가중처벌 사용", $"{sentenceLabel}을 반복한 차량에게 더 높은 벌금을 부과합니다.");
            AddGameplay(entries, currentWindowName, defaultWindowName, $"{label} 상습 위반 기준 기간 (게임 개월)", $"같은 차량의 {sentenceLabel}을 상습 위반으로 볼 기간입니다. 실제 시간이 아니라 게임 내 시간을 사용합니다. 게임 12개월은 게임 1년입니다. 바닐라 기본 시간축에서는 게임 1개월이 게임 1일과 같습니다. 날짜 흐름을 바꾸는 모드는 기준 기간을 왜곡할 수 있습니다.");
            AddGameplay(entries, currentThresholdName, defaultThresholdName, $"{label} 상습 위반 기준 횟수", $"기준 기간 안에 몇 번 이상 {sentenceLabel}을 해야 가중처벌을 적용할지 정합니다.");
            AddGameplay(entries, currentMultiplierName, defaultMultiplierName, $"{label} 가중처벌 배수", $"상습 위반 기준에 도달했을 때 {sentenceLabel} 벌금에 적용할 배수입니다.");
        }
    }
}