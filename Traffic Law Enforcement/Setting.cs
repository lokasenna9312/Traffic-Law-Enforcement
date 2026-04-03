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
        private bool m_EnableEstimatedRerouteLogging;
        private bool m_EnableEnforcementEventLogging;
        private bool m_EnablePolicyImpactSummaryLogging;
        private bool m_EnableFineIncomeLogging;
        private bool m_EnablePathfindingPenaltyDiagnosticLogging;
        private bool m_EnableType2PublicTransportLaneUsageLogging;
        private bool m_EnableType3PublicTransportLaneUsageLogging;
        private bool m_EnableType4PublicTransportLaneUsageLogging;
        private bool m_EnablePathObsoleteSourceLogging;
        private bool m_EnableAllVehicleRouteSelectionChangeLogging;
        private bool m_EnableFocusedRouteRebuildDiagnosticsLogging;
        private bool m_EnableFocusedVehicleOnlyRouteLogging;
        private bool m_EnableSettingChangeLogging;
        private bool m_EnableChirperLifecycleLogging;
        private bool m_DefaultEnablePublicTransportLaneEnforcement;
        private bool m_DefaultEnableMidBlockCrossingEnforcement;
        private bool m_DefaultEnableIntersectionMovementEnforcement;
        private bool m_DefaultAllowRoadPublicTransportVehicles;
        private bool m_DefaultAllowTaxis;
        private bool m_DefaultAllowPoliceCars;
        private bool m_DefaultAllowFireEngines;
        private bool m_DefaultAllowAmbulances;
        private bool m_DefaultAllowGarbageTrucks;
        private bool m_DefaultAllowPostVans;
        private bool m_DefaultAllowRoadMaintenanceVehicles;
        private bool m_DefaultAllowSnowplows;
        private bool m_DefaultAllowVehicleMaintenanceVehicles;
        private bool m_DefaultAllowPersonalCars;
        private bool m_DefaultAllowDeliveryTrucks;
        private bool m_DefaultAllowCargoTransportVehicles;
        private bool m_DefaultAllowHearses;
        private bool m_DefaultAllowPrisonerTransports;
        private bool m_DefaultAllowParkMaintenanceVehicles;
        private int m_DefaultPublicTransportLaneFineAmount;
        private int m_DefaultMidBlockCrossingFineAmount;
        private int m_DefaultIntersectionMovementFineAmount;
        private bool m_DefaultEnablePublicTransportLaneRepeatPenalty;
        private int m_DefaultPublicTransportLaneRepeatWindowMonths;
        private int m_DefaultPublicTransportLaneRepeatThreshold;
        private int m_DefaultPublicTransportLaneRepeatMultiplierPercent;
        private bool m_DefaultEnableMidBlockCrossingRepeatPenalty;
        private int m_DefaultMidBlockCrossingRepeatWindowMonths;
        private int m_DefaultMidBlockCrossingRepeatThreshold;
        private int m_DefaultMidBlockCrossingRepeatMultiplierPercent;
        private bool m_DefaultEnableIntersectionMovementRepeatPenalty;
        private int m_DefaultIntersectionMovementRepeatWindowMonths;
        private int m_DefaultIntersectionMovementRepeatThreshold;
        private int m_DefaultIntersectionMovementRepeatMultiplierPercent;

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
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.EnablePublicTransportLaneEnforcement;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.EnablePublicTransportLaneEnforcement = value);
                LogEnforcementToggleChange("currentSave", "publicTransportLane", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kGeneralGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool EnableMidBlockCrossingEnforcement
        {
            get => EnforcementGameplaySettingsService.Current.EnableMidBlockCrossingEnforcement;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.EnableMidBlockCrossingEnforcement;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.EnableMidBlockCrossingEnforcement = value);
                LogEnforcementToggleChange("currentSave", "midBlockCrossing", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kGeneralGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentSaveSettingsDisabled))]
        public bool EnableIntersectionMovementEnforcement
        {
            get => EnforcementGameplaySettingsService.Current.EnableIntersectionMovementEnforcement;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.EnableIntersectionMovementEnforcement;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.EnableIntersectionMovementEnforcement = value);
                LogEnforcementToggleChange("currentSave", "intersectionMovement", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowRoadPublicTransportVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowRoadPublicTransportVehicles;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowRoadPublicTransportVehicles;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowRoadPublicTransportVehicles = value);
                LogEnforcementToggleChange("currentSave", "allowRoadPublicTransportVehicles", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowTaxis
        {
            get => EnforcementGameplaySettingsService.Current.AllowTaxis;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowTaxis;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowTaxis = value);
                LogEnforcementToggleChange("currentSave", "allowTaxis", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowPoliceCars
        {
            get => EnforcementGameplaySettingsService.Current.AllowPoliceCars;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowPoliceCars;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPoliceCars = value);
                LogEnforcementToggleChange("currentSave", "allowPoliceCars", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowFireEngines
        {
            get => EnforcementGameplaySettingsService.Current.AllowFireEngines;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowFireEngines;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowFireEngines = value);
                LogEnforcementToggleChange("currentSave", "allowFireEngines", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowAmbulances
        {
            get => EnforcementGameplaySettingsService.Current.AllowAmbulances;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowAmbulances;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowAmbulances = value);
                LogEnforcementToggleChange("currentSave", "allowAmbulances", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowGarbageTrucks
        {
            get => EnforcementGameplaySettingsService.Current.AllowGarbageTrucks;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowGarbageTrucks;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowGarbageTrucks = value);
                LogEnforcementToggleChange("currentSave", "allowGarbageTrucks", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowPostVans
        {
            get => EnforcementGameplaySettingsService.Current.AllowPostVans;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowPostVans;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPostVans = value);
                LogEnforcementToggleChange("currentSave", "allowPostVans", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowRoadMaintenanceVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowRoadMaintenanceVehicles;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowRoadMaintenanceVehicles;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowRoadMaintenanceVehicles = value);
                LogEnforcementToggleChange("currentSave", "allowRoadMaintenanceVehicles", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowSnowplows
        {
            get => EnforcementGameplaySettingsService.Current.AllowSnowplows;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowSnowplows;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowSnowplows = value);
                LogEnforcementToggleChange("currentSave", "allowSnowplows", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowVehicleMaintenanceVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowVehicleMaintenanceVehicles;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowVehicleMaintenanceVehicles;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowVehicleMaintenanceVehicles = value);
                LogEnforcementToggleChange("currentSave", "allowVehicleMaintenanceVehicles", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowPersonalCars
        {
            get => EnforcementGameplaySettingsService.Current.AllowPersonalCars;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowPersonalCars;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPersonalCars = value);
                LogEnforcementToggleChange("currentSave", "allowPersonalCars", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowDeliveryTrucks
        {
            get => EnforcementGameplaySettingsService.Current.AllowDeliveryTrucks;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowDeliveryTrucks;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowDeliveryTrucks = value);
                LogEnforcementToggleChange("currentSave", "allowDeliveryTrucks", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowCargoTransportVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowCargoTransportVehicles;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowCargoTransportVehicles;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowCargoTransportVehicles = value);
                LogEnforcementToggleChange("currentSave", "allowCargoTransportVehicles", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowHearses
        {
            get => EnforcementGameplaySettingsService.Current.AllowHearses;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowHearses;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowHearses = value);
                LogEnforcementToggleChange("currentSave", "allowHearses", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowPrisonerTransports
        {
            get => EnforcementGameplaySettingsService.Current.AllowPrisonerTransports;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowPrisonerTransports;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowPrisonerTransports = value);
                LogEnforcementToggleChange("currentSave", "allowPrisonerTransports", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool AllowParkMaintenanceVehicles
        {
            get => EnforcementGameplaySettingsService.Current.AllowParkMaintenanceVehicles;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.AllowParkMaintenanceVehicles;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.AllowParkMaintenanceVehicles = value);
                LogEnforcementToggleChange("currentSave", "allowParkMaintenanceVehicles", previous, value);
            }
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
            set
            {
                int previous = EnforcementGameplaySettingsService.Current.PublicTransportLaneFineAmount;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.PublicTransportLaneFineAmount = value);
                LogEnforcementValueChange("currentSave", "publicTransportLaneFineAmount", previous, value);
            }
        }

        [Exclude]
        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kCurrentSaveTab, kFineGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentMidBlockCrossingPolicySettingsDisabled))]
        public int MidBlockCrossingFineAmount
        {
            get => EnforcementGameplaySettingsService.Current.MidBlockCrossingFineAmount;
            set
            {
                int previous = EnforcementGameplaySettingsService.Current.MidBlockCrossingFineAmount;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.MidBlockCrossingFineAmount = value);
                LogEnforcementValueChange("currentSave", "midBlockCrossingFineAmount", previous, value);
            }
        }

        [Exclude]
        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kCurrentSaveTab, kFineGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentIntersectionMovementPolicySettingsDisabled))]
        public int IntersectionMovementFineAmount
        {
            get => EnforcementGameplaySettingsService.Current.IntersectionMovementFineAmount;
            set
            {
                int previous = EnforcementGameplaySettingsService.Current.IntersectionMovementFineAmount;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.IntersectionMovementFineAmount = value);
                LogEnforcementValueChange("currentSave", "intersectionMovementFineAmount", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneSettingsDisabled))]
        public bool EnablePublicTransportLaneRepeatPenalty
        {
            get => EnforcementGameplaySettingsService.Current.EnablePublicTransportLaneRepeatPenalty;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.EnablePublicTransportLaneRepeatPenalty;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.EnablePublicTransportLaneRepeatPenalty = value);
                LogEnforcementToggleChange("currentSave", "publicTransportLaneRepeatPenalty", previous, value);
            }
        }

        [Exclude]
        [SettingsUISlider(min = 1, max = 12, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int PublicTransportLaneRepeatWindowMonths
        {
            get => EnforcementGameplaySettingsService.Current.PublicTransportLaneRepeatWindowMonths;
            set
            {
                int previous = EnforcementGameplaySettingsService.Current.PublicTransportLaneRepeatWindowMonths;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.PublicTransportLaneRepeatWindowMonths = value);
                LogEnforcementValueChange("currentSave", "publicTransportLaneRepeatWindowMonths", previous, value);
            }
        }

        [Exclude]
        [SettingsUISlider(min = 2, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int PublicTransportLaneRepeatThreshold
        {
            get => EnforcementGameplaySettingsService.Current.PublicTransportLaneRepeatThreshold;
            set
            {
                int previous = EnforcementGameplaySettingsService.Current.PublicTransportLaneRepeatThreshold;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.PublicTransportLaneRepeatThreshold = value);
                LogEnforcementValueChange("currentSave", "publicTransportLaneRepeatThreshold", previous, value);
            }
        }

        [Exclude]
        [SettingsUISlider(min = 100, max = 500, step = 25, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentPublicTransportLaneRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int PublicTransportLaneRepeatMultiplierPercent
        {
            get => EnforcementGameplaySettingsService.Current.PublicTransportLaneRepeatMultiplierPercent;
            set
            {
                int previous = EnforcementGameplaySettingsService.Current.PublicTransportLaneRepeatMultiplierPercent;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.PublicTransportLaneRepeatMultiplierPercent = value);
                LogEnforcementValueChange("currentSave", "publicTransportLaneRepeatMultiplierPercent", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentMidBlockCrossingPolicySettingsDisabled))]
        public bool EnableMidBlockCrossingRepeatPenalty
        {
            get => EnforcementGameplaySettingsService.Current.EnableMidBlockCrossingRepeatPenalty;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.EnableMidBlockCrossingRepeatPenalty;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.EnableMidBlockCrossingRepeatPenalty = value);
                LogEnforcementToggleChange("currentSave", "midBlockCrossingRepeatPenalty", previous, value);
            }
        }

        [Exclude]
        [SettingsUISlider(min = 1, max = 12, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentMidBlockCrossingRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int MidBlockCrossingRepeatWindowMonths
        {
            get => EnforcementGameplaySettingsService.Current.MidBlockCrossingRepeatWindowMonths;
            set
            {
                int previous = EnforcementGameplaySettingsService.Current.MidBlockCrossingRepeatWindowMonths;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.MidBlockCrossingRepeatWindowMonths = value);
                LogEnforcementValueChange("currentSave", "midBlockCrossingRepeatWindowMonths", previous, value);
            }
        }

        [Exclude]
        [SettingsUISlider(min = 2, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentMidBlockCrossingRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int MidBlockCrossingRepeatThreshold
        {
            get => EnforcementGameplaySettingsService.Current.MidBlockCrossingRepeatThreshold;
            set
            {
                int previous = EnforcementGameplaySettingsService.Current.MidBlockCrossingRepeatThreshold;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.MidBlockCrossingRepeatThreshold = value);
                LogEnforcementValueChange("currentSave", "midBlockCrossingRepeatThreshold", previous, value);
            }
        }

        [Exclude]
        [SettingsUISlider(min = 100, max = 500, step = 25, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentMidBlockCrossingRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int MidBlockCrossingRepeatMultiplierPercent
        {
            get => EnforcementGameplaySettingsService.Current.MidBlockCrossingRepeatMultiplierPercent;
            set
            {
                int previous = EnforcementGameplaySettingsService.Current.MidBlockCrossingRepeatMultiplierPercent;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.MidBlockCrossingRepeatMultiplierPercent = value);
                LogEnforcementValueChange("currentSave", "midBlockCrossingRepeatMultiplierPercent", previous, value);
            }
        }

        [Exclude]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentIntersectionMovementPolicySettingsDisabled))]
        public bool EnableIntersectionMovementRepeatPenalty
        {
            get => EnforcementGameplaySettingsService.Current.EnableIntersectionMovementRepeatPenalty;
            set
            {
                bool previous = EnforcementGameplaySettingsService.Current.EnableIntersectionMovementRepeatPenalty;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.EnableIntersectionMovementRepeatPenalty = value);
                LogEnforcementToggleChange("currentSave", "intersectionMovementRepeatPenalty", previous, value);
            }
        }

        [Exclude]
        [SettingsUISlider(min = 1, max = 12, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentIntersectionMovementRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int IntersectionMovementRepeatWindowMonths
        {
            get => EnforcementGameplaySettingsService.Current.IntersectionMovementRepeatWindowMonths;
            set
            {
                int previous = EnforcementGameplaySettingsService.Current.IntersectionMovementRepeatWindowMonths;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.IntersectionMovementRepeatWindowMonths = value);
                LogEnforcementValueChange("currentSave", "intersectionMovementRepeatWindowMonths", previous, value);
            }
        }

        [Exclude]
        [SettingsUISlider(min = 2, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentIntersectionMovementRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int IntersectionMovementRepeatThreshold
        {
            get => EnforcementGameplaySettingsService.Current.IntersectionMovementRepeatThreshold;
            set
            {
                int previous = EnforcementGameplaySettingsService.Current.IntersectionMovementRepeatThreshold;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.IntersectionMovementRepeatThreshold = value);
                LogEnforcementValueChange("currentSave", "intersectionMovementRepeatThreshold", previous, value);
            }
        }

        [Exclude]
        [SettingsUISlider(min = 100, max = 500, step = 25, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsCurrentIntersectionMovementRepeatSettingsDisabled))]
        [SettingsUISection(kCurrentSaveTab, kRepeatOffenderGroup)]
        public int IntersectionMovementRepeatMultiplierPercent
        {
            get => EnforcementGameplaySettingsService.Current.IntersectionMovementRepeatMultiplierPercent;
            set
            {
                int previous = EnforcementGameplaySettingsService.Current.IntersectionMovementRepeatMultiplierPercent;
                if (previous == value)
                {
                    return;
                }

                UpdateCurrentSaveSettings((ref EnforcementGameplaySettingsState state) => state.IntersectionMovementRepeatMultiplierPercent = value);
                LogEnforcementValueChange("currentSave", "intersectionMovementRepeatMultiplierPercent", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kGeneralGroup)]
        public bool DefaultEnablePublicTransportLaneEnforcement
        {
            get => m_DefaultEnablePublicTransportLaneEnforcement;
            set
            {
                if (m_DefaultEnablePublicTransportLaneEnforcement == value)
                {
                    return;
                }

                bool previous = m_DefaultEnablePublicTransportLaneEnforcement;
                m_DefaultEnablePublicTransportLaneEnforcement = value;
                LogEnforcementToggleChange("newSaveDefaults", "publicTransportLane", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kGeneralGroup)]
        public bool DefaultEnableMidBlockCrossingEnforcement
        {
            get => m_DefaultEnableMidBlockCrossingEnforcement;
            set
            {
                if (m_DefaultEnableMidBlockCrossingEnforcement == value)
                {
                    return;
                }

                bool previous = m_DefaultEnableMidBlockCrossingEnforcement;
                m_DefaultEnableMidBlockCrossingEnforcement = value;
                LogEnforcementToggleChange("newSaveDefaults", "midBlockCrossing", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kGeneralGroup)]
        public bool DefaultEnableIntersectionMovementEnforcement
        {
            get => m_DefaultEnableIntersectionMovementEnforcement;
            set
            {
                if (m_DefaultEnableIntersectionMovementEnforcement == value)
                {
                    return;
                }

                bool previous = m_DefaultEnableIntersectionMovementEnforcement;
                m_DefaultEnableIntersectionMovementEnforcement = value;
                LogEnforcementToggleChange("newSaveDefaults", "intersectionMovement", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowRoadPublicTransportVehicles
        {
            get => m_DefaultAllowRoadPublicTransportVehicles;
            set
            {
                if (m_DefaultAllowRoadPublicTransportVehicles == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowRoadPublicTransportVehicles;
                m_DefaultAllowRoadPublicTransportVehicles = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowRoadPublicTransportVehicles", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowTaxis
        {
            get => m_DefaultAllowTaxis;
            set
            {
                if (m_DefaultAllowTaxis == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowTaxis;
                m_DefaultAllowTaxis = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowTaxis", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowPoliceCars
        {
            get => m_DefaultAllowPoliceCars;
            set
            {
                if (m_DefaultAllowPoliceCars == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowPoliceCars;
                m_DefaultAllowPoliceCars = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowPoliceCars", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowFireEngines
        {
            get => m_DefaultAllowFireEngines;
            set
            {
                if (m_DefaultAllowFireEngines == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowFireEngines;
                m_DefaultAllowFireEngines = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowFireEngines", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowAmbulances
        {
            get => m_DefaultAllowAmbulances;
            set
            {
                if (m_DefaultAllowAmbulances == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowAmbulances;
                m_DefaultAllowAmbulances = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowAmbulances", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowGarbageTrucks
        {
            get => m_DefaultAllowGarbageTrucks;
            set
            {
                if (m_DefaultAllowGarbageTrucks == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowGarbageTrucks;
                m_DefaultAllowGarbageTrucks = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowGarbageTrucks", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowPostVans
        {
            get => m_DefaultAllowPostVans;
            set
            {
                if (m_DefaultAllowPostVans == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowPostVans;
                m_DefaultAllowPostVans = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowPostVans", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowRoadMaintenanceVehicles
        {
            get => m_DefaultAllowRoadMaintenanceVehicles;
            set
            {
                if (m_DefaultAllowRoadMaintenanceVehicles == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowRoadMaintenanceVehicles;
                m_DefaultAllowRoadMaintenanceVehicles = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowRoadMaintenanceVehicles", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowSnowplows
        {
            get => m_DefaultAllowSnowplows;
            set
            {
                if (m_DefaultAllowSnowplows == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowSnowplows;
                m_DefaultAllowSnowplows = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowSnowplows", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAuthorizedGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowVehicleMaintenanceVehicles
        {
            get => m_DefaultAllowVehicleMaintenanceVehicles;
            set
            {
                if (m_DefaultAllowVehicleMaintenanceVehicles == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowVehicleMaintenanceVehicles;
                m_DefaultAllowVehicleMaintenanceVehicles = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowVehicleMaintenanceVehicles", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowPersonalCars
        {
            get => m_DefaultAllowPersonalCars;
            set
            {
                if (m_DefaultAllowPersonalCars == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowPersonalCars;
                m_DefaultAllowPersonalCars = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowPersonalCars", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowDeliveryTrucks
        {
            get => m_DefaultAllowDeliveryTrucks;
            set
            {
                if (m_DefaultAllowDeliveryTrucks == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowDeliveryTrucks;
                m_DefaultAllowDeliveryTrucks = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowDeliveryTrucks", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowCargoTransportVehicles
        {
            get => m_DefaultAllowCargoTransportVehicles;
            set
            {
                if (m_DefaultAllowCargoTransportVehicles == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowCargoTransportVehicles;
                m_DefaultAllowCargoTransportVehicles = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowCargoTransportVehicles", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowHearses
        {
            get => m_DefaultAllowHearses;
            set
            {
                if (m_DefaultAllowHearses == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowHearses;
                m_DefaultAllowHearses = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowHearses", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowPrisonerTransports
        {
            get => m_DefaultAllowPrisonerTransports;
            set
            {
                if (m_DefaultAllowPrisonerTransports == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowPrisonerTransports;
                m_DefaultAllowPrisonerTransports = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowPrisonerTransports", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLaneAdditionalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultAllowParkMaintenanceVehicles
        {
            get => m_DefaultAllowParkMaintenanceVehicles;
            set
            {
                if (m_DefaultAllowParkMaintenanceVehicles == value)
                {
                    return;
                }

                bool previous = m_DefaultAllowParkMaintenanceVehicles;
                m_DefaultAllowParkMaintenanceVehicles = value;
                LogEnforcementToggleChange("newSaveDefaults", "allowParkMaintenanceVehicles", previous, value);
            }
        }

        [SettingsUISlider(min = 0f, max = 1f, step = 0.01f, unit = Unit.kFloatThreeFractions)]
        [SettingsUISection(kNewSaveDefaultsTab, kPublicTransportLanePressureGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public float DefaultPublicTransportLaneExitPressureThresholdDays { get; set; }

        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kNewSaveDefaultsTab, kFineGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public int DefaultPublicTransportLaneFineAmount
        {
            get => m_DefaultPublicTransportLaneFineAmount;
            set
            {
                if (m_DefaultPublicTransportLaneFineAmount == value)
                {
                    return;
                }

                int previous = m_DefaultPublicTransportLaneFineAmount;
                m_DefaultPublicTransportLaneFineAmount = value;
                LogEnforcementValueChange("newSaveDefaults", "publicTransportLaneFineAmount", previous, value);
            }
        }

        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kNewSaveDefaultsTab, kFineGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveMidBlockCrossingPolicySettingsDisabled))]
        public int DefaultMidBlockCrossingFineAmount
        {
            get => m_DefaultMidBlockCrossingFineAmount;
            set
            {
                if (m_DefaultMidBlockCrossingFineAmount == value)
                {
                    return;
                }

                int previous = m_DefaultMidBlockCrossingFineAmount;
                m_DefaultMidBlockCrossingFineAmount = value;
                LogEnforcementValueChange("newSaveDefaults", "midBlockCrossingFineAmount", previous, value);
            }
        }

        [SettingsUISlider(min = 0, max = 5000, step = 25, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kNewSaveDefaultsTab, kFineGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveIntersectionMovementPolicySettingsDisabled))]
        public int DefaultIntersectionMovementFineAmount
        {
            get => m_DefaultIntersectionMovementFineAmount;
            set
            {
                if (m_DefaultIntersectionMovementFineAmount == value)
                {
                    return;
                }

                int previous = m_DefaultIntersectionMovementFineAmount;
                m_DefaultIntersectionMovementFineAmount = value;
                LogEnforcementValueChange("newSaveDefaults", "intersectionMovementFineAmount", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneSettingsDisabled))]
        public bool DefaultEnablePublicTransportLaneRepeatPenalty
        {
            get => m_DefaultEnablePublicTransportLaneRepeatPenalty;
            set
            {
                if (m_DefaultEnablePublicTransportLaneRepeatPenalty == value)
                {
                    return;
                }

                bool previous = m_DefaultEnablePublicTransportLaneRepeatPenalty;
                m_DefaultEnablePublicTransportLaneRepeatPenalty = value;
                LogEnforcementToggleChange("newSaveDefaults", "publicTransportLaneRepeatPenalty", previous, value);
            }
        }

        [SettingsUISlider(min = 1, max = 12, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultPublicTransportLaneRepeatWindowMonths
        {
            get => m_DefaultPublicTransportLaneRepeatWindowMonths;
            set
            {
                if (m_DefaultPublicTransportLaneRepeatWindowMonths == value)
                {
                    return;
                }

                int previous = m_DefaultPublicTransportLaneRepeatWindowMonths;
                m_DefaultPublicTransportLaneRepeatWindowMonths = value;
                LogEnforcementValueChange("newSaveDefaults", "publicTransportLaneRepeatWindowMonths", previous, value);
            }
        }

        [SettingsUISlider(min = 2, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultPublicTransportLaneRepeatThreshold
        {
            get => m_DefaultPublicTransportLaneRepeatThreshold;
            set
            {
                if (m_DefaultPublicTransportLaneRepeatThreshold == value)
                {
                    return;
                }

                int previous = m_DefaultPublicTransportLaneRepeatThreshold;
                m_DefaultPublicTransportLaneRepeatThreshold = value;
                LogEnforcementValueChange("newSaveDefaults", "publicTransportLaneRepeatThreshold", previous, value);
            }
        }

        [SettingsUISlider(min = 100, max = 500, step = 25, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSavePublicTransportLaneRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultPublicTransportLaneRepeatMultiplierPercent
        {
            get => m_DefaultPublicTransportLaneRepeatMultiplierPercent;
            set
            {
                if (m_DefaultPublicTransportLaneRepeatMultiplierPercent == value)
                {
                    return;
                }

                int previous = m_DefaultPublicTransportLaneRepeatMultiplierPercent;
                m_DefaultPublicTransportLaneRepeatMultiplierPercent = value;
                LogEnforcementValueChange("newSaveDefaults", "publicTransportLaneRepeatMultiplierPercent", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveMidBlockCrossingPolicySettingsDisabled))]
        public bool DefaultEnableMidBlockCrossingRepeatPenalty
        {
            get => m_DefaultEnableMidBlockCrossingRepeatPenalty;
            set
            {
                if (m_DefaultEnableMidBlockCrossingRepeatPenalty == value)
                {
                    return;
                }

                bool previous = m_DefaultEnableMidBlockCrossingRepeatPenalty;
                m_DefaultEnableMidBlockCrossingRepeatPenalty = value;
                LogEnforcementToggleChange("newSaveDefaults", "midBlockCrossingRepeatPenalty", previous, value);
            }
        }

        [SettingsUISlider(min = 1, max = 12, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveMidBlockCrossingRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultMidBlockCrossingRepeatWindowMonths
        {
            get => m_DefaultMidBlockCrossingRepeatWindowMonths;
            set
            {
                if (m_DefaultMidBlockCrossingRepeatWindowMonths == value)
                {
                    return;
                }

                int previous = m_DefaultMidBlockCrossingRepeatWindowMonths;
                m_DefaultMidBlockCrossingRepeatWindowMonths = value;
                LogEnforcementValueChange("newSaveDefaults", "midBlockCrossingRepeatWindowMonths", previous, value);
            }
        }

        [SettingsUISlider(min = 2, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveMidBlockCrossingRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultMidBlockCrossingRepeatThreshold
        {
            get => m_DefaultMidBlockCrossingRepeatThreshold;
            set
            {
                if (m_DefaultMidBlockCrossingRepeatThreshold == value)
                {
                    return;
                }

                int previous = m_DefaultMidBlockCrossingRepeatThreshold;
                m_DefaultMidBlockCrossingRepeatThreshold = value;
                LogEnforcementValueChange("newSaveDefaults", "midBlockCrossingRepeatThreshold", previous, value);
            }
        }

        [SettingsUISlider(min = 100, max = 500, step = 25, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveMidBlockCrossingRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultMidBlockCrossingRepeatMultiplierPercent
        {
            get => m_DefaultMidBlockCrossingRepeatMultiplierPercent;
            set
            {
                if (m_DefaultMidBlockCrossingRepeatMultiplierPercent == value)
                {
                    return;
                }

                int previous = m_DefaultMidBlockCrossingRepeatMultiplierPercent;
                m_DefaultMidBlockCrossingRepeatMultiplierPercent = value;
                LogEnforcementValueChange("newSaveDefaults", "midBlockCrossingRepeatMultiplierPercent", previous, value);
            }
        }

        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveIntersectionMovementPolicySettingsDisabled))]
        public bool DefaultEnableIntersectionMovementRepeatPenalty
        {
            get => m_DefaultEnableIntersectionMovementRepeatPenalty;
            set
            {
                if (m_DefaultEnableIntersectionMovementRepeatPenalty == value)
                {
                    return;
                }

                bool previous = m_DefaultEnableIntersectionMovementRepeatPenalty;
                m_DefaultEnableIntersectionMovementRepeatPenalty = value;
                LogEnforcementToggleChange("newSaveDefaults", "intersectionMovementRepeatPenalty", previous, value);
            }
        }

        [SettingsUISlider(min = 1, max = 12, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveIntersectionMovementRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultIntersectionMovementRepeatWindowMonths
        {
            get => m_DefaultIntersectionMovementRepeatWindowMonths;
            set
            {
                if (m_DefaultIntersectionMovementRepeatWindowMonths == value)
                {
                    return;
                }

                int previous = m_DefaultIntersectionMovementRepeatWindowMonths;
                m_DefaultIntersectionMovementRepeatWindowMonths = value;
                LogEnforcementValueChange("newSaveDefaults", "intersectionMovementRepeatWindowMonths", previous, value);
            }
        }

        [SettingsUISlider(min = 2, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveIntersectionMovementRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultIntersectionMovementRepeatThreshold
        {
            get => m_DefaultIntersectionMovementRepeatThreshold;
            set
            {
                if (m_DefaultIntersectionMovementRepeatThreshold == value)
                {
                    return;
                }

                int previous = m_DefaultIntersectionMovementRepeatThreshold;
                m_DefaultIntersectionMovementRepeatThreshold = value;
                LogEnforcementValueChange("newSaveDefaults", "intersectionMovementRepeatThreshold", previous, value);
            }
        }

        [SettingsUISlider(min = 100, max = 500, step = 25, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNewSaveIntersectionMovementRepeatSettingsDisabled))]
        [SettingsUISection(kNewSaveDefaultsTab, kRepeatOffenderGroup)]
        public int DefaultIntersectionMovementRepeatMultiplierPercent
        {
            get => m_DefaultIntersectionMovementRepeatMultiplierPercent;
            set
            {
                if (m_DefaultIntersectionMovementRepeatMultiplierPercent == value)
                {
                    return;
                }

                int previous = m_DefaultIntersectionMovementRepeatMultiplierPercent;
                m_DefaultIntersectionMovementRepeatMultiplierPercent = value;
                LogEnforcementValueChange("newSaveDefaults", "intersectionMovementRepeatMultiplierPercent", previous, value);
            }
        }

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

                EnforcementGameplaySettingsState previous = EnforcementGameplaySettingsService.Current;
                EnforcementGameplaySettingsService.ResetToCodeDefaults();
                EnforcementGameplaySettingsState current = EnforcementGameplaySettingsService.Current;
                LogTrackedEnforcementSettingChanges(
                    "currentSave",
                    previous,
                    current,
                    "ResetCurrentSaveSettingsToCodeDefaults");
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
        public bool EnableEstimatedRerouteLogging
        {
            get => m_EnableEstimatedRerouteLogging;
            set => SetDebugLoggingToggle(
                ref m_EnableEstimatedRerouteLogging,
                value,
                nameof(EnableEstimatedRerouteLogging));
        }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnableEnforcementEventLogging
        {
            get => m_EnableEnforcementEventLogging;
            set => SetDebugLoggingToggle(
                ref m_EnableEnforcementEventLogging,
                value,
                nameof(EnableEnforcementEventLogging));
        }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnablePolicyImpactSummaryLogging
        {
            get => m_EnablePolicyImpactSummaryLogging;
            set => SetDebugLoggingToggle(
                ref m_EnablePolicyImpactSummaryLogging,
                value,
                nameof(EnablePolicyImpactSummaryLogging));
        }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnableFineIncomeLogging
        {
            get => m_EnableFineIncomeLogging;
            set => SetDebugLoggingToggle(
                ref m_EnableFineIncomeLogging,
                value,
                nameof(EnableFineIncomeLogging));
        }


        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnablePathfindingPenaltyDiagnosticLogging
        {
            get => m_EnablePathfindingPenaltyDiagnosticLogging;
            set => SetDebugLoggingToggle(
                ref m_EnablePathfindingPenaltyDiagnosticLogging,
                value,
                nameof(EnablePathfindingPenaltyDiagnosticLogging));
        }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnableType2PublicTransportLaneUsageLogging
        {
            get => m_EnableType2PublicTransportLaneUsageLogging;
            set => SetDebugLoggingToggle(
                ref m_EnableType2PublicTransportLaneUsageLogging,
                value,
                nameof(EnableType2PublicTransportLaneUsageLogging));
        }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnableType3PublicTransportLaneUsageLogging
        {
            get => m_EnableType3PublicTransportLaneUsageLogging;
            set => SetDebugLoggingToggle(
                ref m_EnableType3PublicTransportLaneUsageLogging,
                value,
                nameof(EnableType3PublicTransportLaneUsageLogging));
        }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnableType4PublicTransportLaneUsageLogging
        {
            get => m_EnableType4PublicTransportLaneUsageLogging;
            set => SetDebugLoggingToggle(
                ref m_EnableType4PublicTransportLaneUsageLogging,
                value,
                nameof(EnableType4PublicTransportLaneUsageLogging));
        }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnablePathObsoleteSourceLogging
        {
            get => m_EnablePathObsoleteSourceLogging;
            set => SetDebugLoggingToggle(
                ref m_EnablePathObsoleteSourceLogging,
                value,
                nameof(EnablePathObsoleteSourceLogging));
        }

        [Exclude]
        [SettingsUISection(kDebugTab, kDebugLoggingGroup)]
        public bool EnableAllVehicleRouteSelectionChangeLogging
        {
            get => m_EnableAllVehicleRouteSelectionChangeLogging;
            set => SetDebugLoggingToggle(
                ref m_EnableAllVehicleRouteSelectionChangeLogging,
                value,
                nameof(EnableAllVehicleRouteSelectionChangeLogging));
        }

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

                bool previous = m_EnableFocusedRouteRebuildDiagnosticsLogging;
                m_EnableFocusedRouteRebuildDiagnosticsLogging = value;
                FocusedRouteDiagnosticsPatchController.Sync(value);
                LogDebugLoggingToggleChange(
                    nameof(EnableFocusedRouteRebuildDiagnosticsLogging),
                    previous,
                    value);
            }
        }

        [Exclude]
        [SettingsUISection(kDebugTab, kFocusedLoggingGroup)]
        public bool EnableFocusedVehicleOnlyRouteLogging
        {
            get => m_EnableFocusedVehicleOnlyRouteLogging;
            set => SetDebugLoggingToggle(
                ref m_EnableFocusedVehicleOnlyRouteLogging,
                value,
                nameof(EnableFocusedVehicleOnlyRouteLogging));
        }

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
        [SettingsUISection(kDebugTab, kChirperGroup)]
        public bool EnableChirperLifecycleLogging
        {
            get => m_EnableChirperLifecycleLogging;
            set => SetDebugLoggingToggle(
                ref m_EnableChirperLifecycleLogging,
                value,
                nameof(EnableChirperLifecycleLogging));
        }

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
            EnableChirperLifecycleLogging = false;
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

        internal void EnableSettingChangeLogging()
        {
            m_EnableSettingChangeLogging = true;
        }

        internal void LogDebugLoggingSettingsSnapshot(string source = null)
        {
            if (!m_EnableSettingChangeLogging)
            {
                return;
            }

            string resolvedSource =
                string.IsNullOrWhiteSpace(source) ? "unknown" : source;
            Mod.log.Info(
                "[LOGGING_SETTINGS_SNAPSHOT] " +
                $"source={resolvedSource}, " +
                $"estimatedReroute={EnableEstimatedRerouteLogging}, " +
                $"enforcementEvents={EnableEnforcementEventLogging}, " +
                $"policyImpactSummary={EnablePolicyImpactSummaryLogging}, " +
                $"fineIncome={EnableFineIncomeLogging}, " +
                $"pathfindingPenaltyDiagnostics={EnablePathfindingPenaltyDiagnosticLogging}, " +
                $"type2PtLaneUsage={EnableType2PublicTransportLaneUsageLogging}, " +
                $"type3PtLaneUsage={EnableType3PublicTransportLaneUsageLogging}, " +
                $"type4PtLaneUsage={EnableType4PublicTransportLaneUsageLogging}, " +
                $"pathObsoleteSource={EnablePathObsoleteSourceLogging}, " +
                $"allVehicleRouteSelectionChanges={EnableAllVehicleRouteSelectionChangeLogging}, " +
                $"focusedRouteRebuildDiagnostics={EnableFocusedRouteRebuildDiagnosticsLogging}, " +
                $"focusedVehicleOnlyRouteLogging={EnableFocusedVehicleOnlyRouteLogging}, " +
                $"chirperLifecycle={EnableChirperLifecycleLogging}");
        }

        internal void LogTrackedEnforcementSettingChanges(
            string scope,
            EnforcementGameplaySettingsState previous,
            EnforcementGameplaySettingsState current,
            string source = null)
        {
            LogEnforcementToggleChange(
                scope,
                "publicTransportLane",
                previous.EnablePublicTransportLaneEnforcement,
                current.EnablePublicTransportLaneEnforcement,
                source);
            LogEnforcementToggleChange(
                scope,
                "midBlockCrossing",
                previous.EnableMidBlockCrossingEnforcement,
                current.EnableMidBlockCrossingEnforcement,
                source);
            LogEnforcementToggleChange(
                scope,
                "intersectionMovement",
                previous.EnableIntersectionMovementEnforcement,
                current.EnableIntersectionMovementEnforcement,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowRoadPublicTransportVehicles",
                previous.AllowRoadPublicTransportVehicles,
                current.AllowRoadPublicTransportVehicles,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowTaxis",
                previous.AllowTaxis,
                current.AllowTaxis,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowPoliceCars",
                previous.AllowPoliceCars,
                current.AllowPoliceCars,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowFireEngines",
                previous.AllowFireEngines,
                current.AllowFireEngines,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowAmbulances",
                previous.AllowAmbulances,
                current.AllowAmbulances,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowGarbageTrucks",
                previous.AllowGarbageTrucks,
                current.AllowGarbageTrucks,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowPostVans",
                previous.AllowPostVans,
                current.AllowPostVans,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowRoadMaintenanceVehicles",
                previous.AllowRoadMaintenanceVehicles,
                current.AllowRoadMaintenanceVehicles,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowSnowplows",
                previous.AllowSnowplows,
                current.AllowSnowplows,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowVehicleMaintenanceVehicles",
                previous.AllowVehicleMaintenanceVehicles,
                current.AllowVehicleMaintenanceVehicles,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowPersonalCars",
                previous.AllowPersonalCars,
                current.AllowPersonalCars,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowDeliveryTrucks",
                previous.AllowDeliveryTrucks,
                current.AllowDeliveryTrucks,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowCargoTransportVehicles",
                previous.AllowCargoTransportVehicles,
                current.AllowCargoTransportVehicles,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowHearses",
                previous.AllowHearses,
                current.AllowHearses,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowPrisonerTransports",
                previous.AllowPrisonerTransports,
                current.AllowPrisonerTransports,
                source);
            LogEnforcementToggleChange(
                scope,
                "allowParkMaintenanceVehicles",
                previous.AllowParkMaintenanceVehicles,
                current.AllowParkMaintenanceVehicles,
                source);
            LogEnforcementValueChange(
                scope,
                "publicTransportLaneFineAmount",
                previous.PublicTransportLaneFineAmount,
                current.PublicTransportLaneFineAmount,
                source);
            LogEnforcementValueChange(
                scope,
                "midBlockCrossingFineAmount",
                previous.MidBlockCrossingFineAmount,
                current.MidBlockCrossingFineAmount,
                source);
            LogEnforcementValueChange(
                scope,
                "intersectionMovementFineAmount",
                previous.IntersectionMovementFineAmount,
                current.IntersectionMovementFineAmount,
                source);
            LogEnforcementToggleChange(
                scope,
                "publicTransportLaneRepeatPenalty",
                previous.EnablePublicTransportLaneRepeatPenalty,
                current.EnablePublicTransportLaneRepeatPenalty,
                source);
            LogEnforcementValueChange(
                scope,
                "publicTransportLaneRepeatWindowMonths",
                previous.PublicTransportLaneRepeatWindowMonths,
                current.PublicTransportLaneRepeatWindowMonths,
                source);
            LogEnforcementValueChange(
                scope,
                "publicTransportLaneRepeatThreshold",
                previous.PublicTransportLaneRepeatThreshold,
                current.PublicTransportLaneRepeatThreshold,
                source);
            LogEnforcementValueChange(
                scope,
                "publicTransportLaneRepeatMultiplierPercent",
                previous.PublicTransportLaneRepeatMultiplierPercent,
                current.PublicTransportLaneRepeatMultiplierPercent,
                source);
            LogEnforcementToggleChange(
                scope,
                "midBlockCrossingRepeatPenalty",
                previous.EnableMidBlockCrossingRepeatPenalty,
                current.EnableMidBlockCrossingRepeatPenalty,
                source);
            LogEnforcementValueChange(
                scope,
                "midBlockCrossingRepeatWindowMonths",
                previous.MidBlockCrossingRepeatWindowMonths,
                current.MidBlockCrossingRepeatWindowMonths,
                source);
            LogEnforcementValueChange(
                scope,
                "midBlockCrossingRepeatThreshold",
                previous.MidBlockCrossingRepeatThreshold,
                current.MidBlockCrossingRepeatThreshold,
                source);
            LogEnforcementValueChange(
                scope,
                "midBlockCrossingRepeatMultiplierPercent",
                previous.MidBlockCrossingRepeatMultiplierPercent,
                current.MidBlockCrossingRepeatMultiplierPercent,
                source);
            LogEnforcementToggleChange(
                scope,
                "intersectionMovementRepeatPenalty",
                previous.EnableIntersectionMovementRepeatPenalty,
                current.EnableIntersectionMovementRepeatPenalty,
                source);
            LogEnforcementValueChange(
                scope,
                "intersectionMovementRepeatWindowMonths",
                previous.IntersectionMovementRepeatWindowMonths,
                current.IntersectionMovementRepeatWindowMonths,
                source);
            LogEnforcementValueChange(
                scope,
                "intersectionMovementRepeatThreshold",
                previous.IntersectionMovementRepeatThreshold,
                current.IntersectionMovementRepeatThreshold,
                source);
            LogEnforcementValueChange(
                scope,
                "intersectionMovementRepeatMultiplierPercent",
                previous.IntersectionMovementRepeatMultiplierPercent,
                current.IntersectionMovementRepeatMultiplierPercent,
                source);
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

        private void LogEnforcementToggleChange(
            string scope,
            string option,
            bool previous,
            bool current,
            string source = null)
        {
            if (!m_EnableSettingChangeLogging || previous == current)
            {
                return;
            }

            string sourceSuffix =
                string.IsNullOrWhiteSpace(source) ? string.Empty : $", source={source}";
            Mod.log.Info(
                $"[ENFORCEMENT_SETTINGS] scope={scope}, option={option}, enabled={current}, previous={previous}{sourceSuffix}");
        }

        private void LogEnforcementValueChange(
            string scope,
            string option,
            int previous,
            int current,
            string source = null)
        {
            if (!m_EnableSettingChangeLogging || previous == current)
            {
                return;
            }

            string sourceSuffix =
                string.IsNullOrWhiteSpace(source) ? string.Empty : $", source={source}";
            Mod.log.Info(
                $"[ENFORCEMENT_SETTINGS] scope={scope}, option={option}, value={current}, previous={previous}{sourceSuffix}");
        }

        private void SetDebugLoggingToggle(
            ref bool field,
            bool value,
            string optionName)
        {
            if (field == value)
            {
                return;
            }

            bool previous = field;
            field = value;
            LogDebugLoggingToggleChange(optionName, previous, value);
        }

        private void LogDebugLoggingToggleChange(
            string option,
            bool previous,
            bool current,
            string source = null)
        {
            if (!m_EnableSettingChangeLogging || previous == current)
            {
                return;
            }

            string sourceSuffix =
                string.IsNullOrWhiteSpace(source) ? string.Empty : $", source={source}";
            Mod.log.Info(
                $"[LOGGING_SETTINGS] option={option}, enabled={current}, previous={previous}{sourceSuffix}");
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
