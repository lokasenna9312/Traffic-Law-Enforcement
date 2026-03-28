using System;
using System.Globalization;
using System.Text;
using Game;
using Game.Simulation;
using Unity.Entities;
using UnityEngine;

namespace Traffic_Law_Enforcement
{
    public partial class SettingsChangeLoggingSystem : GameSystemBase
    {
        private TimeSystem m_TimeSystem;
        private bool m_HasSnapshot;
        private LoggedSettingsSnapshot m_LastSnapshot;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
        }

        protected override void OnUpdate()
        {
            LoggedSettingsSnapshot snapshot = LoggedSettingsSnapshot.Capture();

            if (!m_HasSnapshot)
            {
                string inGameTimeLabel = GetInGameTimeLabel();
                Mod.log.Info($"[Settings] Initial snapshot at {inGameTimeLabel}: {snapshot.ToLogString()}");
                m_LastSnapshot = snapshot;
                m_HasSnapshot = true;
                return;
            }

            LogChanges(m_LastSnapshot, snapshot);
            m_LastSnapshot = snapshot;
        }

        private void LogChanges(LoggedSettingsSnapshot previous, LoggedSettingsSnapshot current)
        {
            bool invalidateVehicleUtilsPenaltyCache =
                previous.Gameplay.EnablePublicTransportLaneEnforcement != current.Gameplay.EnablePublicTransportLaneEnforcement ||
                previous.Gameplay.PublicTransportLaneFineAmount != current.Gameplay.PublicTransportLaneFineAmount;

            if (invalidateVehicleUtilsPenaltyCache)
            {
                VehicleUtilsPatches.InvalidateCachedPenaltyValues();
            }
            
            LogChange(nameof(Setting.EnablePublicTransportLaneEnforcement), previous.Gameplay.EnablePublicTransportLaneEnforcement, current.Gameplay.EnablePublicTransportLaneEnforcement);
            LogChange(nameof(Setting.EnableMidBlockCrossingEnforcement), previous.Gameplay.EnableMidBlockCrossingEnforcement, current.Gameplay.EnableMidBlockCrossingEnforcement);
            LogChange(nameof(Setting.EnableIntersectionMovementEnforcement), previous.Gameplay.EnableIntersectionMovementEnforcement, current.Gameplay.EnableIntersectionMovementEnforcement);
            LogChange(nameof(Setting.EnableEstimatedRerouteLogging), previous.EnableEstimatedRerouteLogging, current.EnableEstimatedRerouteLogging);
            LogChange(nameof(Setting.EnableEnforcementEventLogging), previous.EnableEnforcementEventLogging, current.EnableEnforcementEventLogging);
            LogChange(nameof(Setting.EnableType2PublicTransportLaneUsageLogging), previous.EnableType2PublicTransportLaneUsageLogging, current.EnableType2PublicTransportLaneUsageLogging);
            LogChange(nameof(Setting.EnableType3PublicTransportLaneUsageLogging), previous.EnableType3PublicTransportLaneUsageLogging, current.EnableType3PublicTransportLaneUsageLogging);
            LogChange(nameof(Setting.EnableType4PublicTransportLaneUsageLogging), previous.EnableType4PublicTransportLaneUsageLogging, current.EnableType4PublicTransportLaneUsageLogging);
            LogChange(nameof(Setting.EnablePathfindingPenaltyDiagnosticLogging), previous.EnablePathfindingPenaltyDiagnosticLogging, current.EnablePathfindingPenaltyDiagnosticLogging);
            LogChange(nameof(Setting.EnablePathObsoleteSourceLogging), previous.EnablePathObsoleteSourceLogging, current.EnablePathObsoleteSourceLogging);
            LogChange(nameof(Setting.EnableFocusedVehicleOnlyRouteLogging), previous.EnableFocusedVehicleOnlyRouteLogging, current.EnableFocusedVehicleOnlyRouteLogging);
            LogChange(nameof(Setting.AllowRoadPublicTransportVehicles), previous.Gameplay.AllowRoadPublicTransportVehicles, current.Gameplay.AllowRoadPublicTransportVehicles);
            LogChange(nameof(Setting.AllowTaxis), previous.Gameplay.AllowTaxis, current.Gameplay.AllowTaxis);
            LogChange(nameof(Setting.AllowPoliceCars), previous.Gameplay.AllowPoliceCars, current.Gameplay.AllowPoliceCars);
            LogChange(nameof(Setting.AllowFireEngines), previous.Gameplay.AllowFireEngines, current.Gameplay.AllowFireEngines);
            LogChange(nameof(Setting.AllowAmbulances), previous.Gameplay.AllowAmbulances, current.Gameplay.AllowAmbulances);
            LogChange(nameof(Setting.AllowGarbageTrucks), previous.Gameplay.AllowGarbageTrucks, current.Gameplay.AllowGarbageTrucks);
            LogChange(nameof(Setting.AllowPostVans), previous.Gameplay.AllowPostVans, current.Gameplay.AllowPostVans);
            LogChange(nameof(Setting.AllowRoadMaintenanceVehicles), previous.Gameplay.AllowRoadMaintenanceVehicles, current.Gameplay.AllowRoadMaintenanceVehicles);
            LogChange(nameof(Setting.AllowSnowplows), previous.Gameplay.AllowSnowplows, current.Gameplay.AllowSnowplows);
            LogChange(nameof(Setting.AllowVehicleMaintenanceVehicles), previous.Gameplay.AllowVehicleMaintenanceVehicles, current.Gameplay.AllowVehicleMaintenanceVehicles);
            LogChange(nameof(Setting.AllowPersonalCars), previous.Gameplay.AllowPersonalCars, current.Gameplay.AllowPersonalCars);
            LogChange(nameof(Setting.AllowDeliveryTrucks), previous.Gameplay.AllowDeliveryTrucks, current.Gameplay.AllowDeliveryTrucks);
            LogChange(nameof(Setting.AllowCargoTransportVehicles), previous.Gameplay.AllowCargoTransportVehicles, current.Gameplay.AllowCargoTransportVehicles);
            LogChange(nameof(Setting.AllowHearses), previous.Gameplay.AllowHearses, current.Gameplay.AllowHearses);
            LogChange(nameof(Setting.AllowPrisonerTransports), previous.Gameplay.AllowPrisonerTransports, current.Gameplay.AllowPrisonerTransports);
            LogChange(nameof(Setting.AllowParkMaintenanceVehicles), previous.Gameplay.AllowParkMaintenanceVehicles, current.Gameplay.AllowParkMaintenanceVehicles);

            LogChange(nameof(Setting.PublicTransportLaneExitPressureThresholdDays), previous.Gameplay.PublicTransportLaneExitPressureThresholdDays, current.Gameplay.PublicTransportLaneExitPressureThresholdDays);
            LogChange(nameof(Setting.PublicTransportLaneFineAmount), previous.Gameplay.PublicTransportLaneFineAmount, current.Gameplay.PublicTransportLaneFineAmount);
            LogChange(nameof(Setting.MidBlockCrossingFineAmount), previous.Gameplay.MidBlockCrossingFineAmount, current.Gameplay.MidBlockCrossingFineAmount);
            LogChange(nameof(Setting.IntersectionMovementFineAmount), previous.Gameplay.IntersectionMovementFineAmount, current.Gameplay.IntersectionMovementFineAmount);

            LogChange(nameof(Setting.EnablePublicTransportLaneRepeatPenalty), previous.Gameplay.EnablePublicTransportLaneRepeatPenalty, current.Gameplay.EnablePublicTransportLaneRepeatPenalty);
            LogChange(nameof(Setting.PublicTransportLaneRepeatWindowMonths), previous.Gameplay.PublicTransportLaneRepeatWindowMonths, current.Gameplay.PublicTransportLaneRepeatWindowMonths);
            LogChange(nameof(Setting.PublicTransportLaneRepeatThreshold), previous.Gameplay.PublicTransportLaneRepeatThreshold, current.Gameplay.PublicTransportLaneRepeatThreshold);
            LogChange(nameof(Setting.PublicTransportLaneRepeatMultiplierPercent), previous.Gameplay.PublicTransportLaneRepeatMultiplierPercent, current.Gameplay.PublicTransportLaneRepeatMultiplierPercent);

            LogChange(nameof(Setting.EnableMidBlockCrossingRepeatPenalty), previous.Gameplay.EnableMidBlockCrossingRepeatPenalty, current.Gameplay.EnableMidBlockCrossingRepeatPenalty);
            LogChange(nameof(Setting.MidBlockCrossingRepeatWindowMonths), previous.Gameplay.MidBlockCrossingRepeatWindowMonths, current.Gameplay.MidBlockCrossingRepeatWindowMonths);
            LogChange(nameof(Setting.MidBlockCrossingRepeatThreshold), previous.Gameplay.MidBlockCrossingRepeatThreshold, current.Gameplay.MidBlockCrossingRepeatThreshold);
            LogChange(nameof(Setting.MidBlockCrossingRepeatMultiplierPercent), previous.Gameplay.MidBlockCrossingRepeatMultiplierPercent, current.Gameplay.MidBlockCrossingRepeatMultiplierPercent);

            LogChange(nameof(Setting.EnableIntersectionMovementRepeatPenalty), previous.Gameplay.EnableIntersectionMovementRepeatPenalty, current.Gameplay.EnableIntersectionMovementRepeatPenalty);
            LogChange(nameof(Setting.IntersectionMovementRepeatWindowMonths), previous.Gameplay.IntersectionMovementRepeatWindowMonths, current.Gameplay.IntersectionMovementRepeatWindowMonths);
            LogChange(nameof(Setting.IntersectionMovementRepeatThreshold), previous.Gameplay.IntersectionMovementRepeatThreshold, current.Gameplay.IntersectionMovementRepeatThreshold);
            LogChange(nameof(Setting.IntersectionMovementRepeatMultiplierPercent), previous.Gameplay.IntersectionMovementRepeatMultiplierPercent, current.Gameplay.IntersectionMovementRepeatMultiplierPercent);
        }

        private void LogChange(string settingName, bool previous, bool current)
        {
            if (previous != current)
            {
                string inGameTimeLabel = GetInGameTimeLabel();
                Mod.log.Info($"[Settings] {settingName} changed: {previous} -> {current} at {inGameTimeLabel}");
            }
        }

        private void LogChange(string settingName, int previous, int current)
        {
            if (previous != current)
            {
                string inGameTimeLabel = GetInGameTimeLabel();
                Mod.log.Info($"[Settings] {settingName} changed: {previous} -> {current} at {inGameTimeLabel}");
            }
        }

        private void LogChange(string settingName, float previous, float current)
        {
            if (!Mathf.Approximately(previous, current))
            {
                string inGameTimeLabel = GetInGameTimeLabel();
                Mod.log.Info($"[Settings] {settingName} changed: {previous} -> {current} at {inGameTimeLabel}");
            }
        }

        private string GetInGameTimeLabel()
        {
            if (m_TimeSystem == null)
            {
                return "unavailable";
            }

            DateTime now = m_TimeSystem.GetCurrentDateTime();
            return FormattableString.Invariant(
                $"Y{now.Year} day={now.DayOfYear} time={now.Hour:00}:{now.Minute:00}");
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.000", CultureInfo.InvariantCulture);
        }

        private readonly struct LoggedSettingsSnapshot
        {
            public readonly EnforcementGameplaySettingsState Gameplay;
            public readonly bool EnableEstimatedRerouteLogging;
            public readonly bool EnableEnforcementEventLogging;
            public readonly bool EnableType2PublicTransportLaneUsageLogging;
            public readonly bool EnableType3PublicTransportLaneUsageLogging;
            public readonly bool EnableType4PublicTransportLaneUsageLogging;
            public readonly bool EnablePathfindingPenaltyDiagnosticLogging;
            public readonly bool EnablePathObsoleteSourceLogging;
            public readonly bool EnableFocusedVehicleOnlyRouteLogging;

            private LoggedSettingsSnapshot(
                EnforcementGameplaySettingsState gameplay,
                bool enableEstimatedRerouteLogging,
                bool enableEnforcementEventLogging,
                bool enableType2PublicTransportLaneUsageLogging,
                bool enableType3PublicTransportLaneUsageLogging,
                bool enableType4PublicTransportLaneUsageLogging,
                bool enablePathfindingPenaltyDiagnosticLogging,
                bool enablePathObsoleteSourceLogging,
                bool enableFocusedVehicleOnlyRouteLogging)
            {
                Gameplay = gameplay;
                EnableEstimatedRerouteLogging = enableEstimatedRerouteLogging;
                EnableEnforcementEventLogging = enableEnforcementEventLogging;
                EnableType2PublicTransportLaneUsageLogging = enableType2PublicTransportLaneUsageLogging;
                EnableType3PublicTransportLaneUsageLogging = enableType3PublicTransportLaneUsageLogging;
                EnableType4PublicTransportLaneUsageLogging = enableType4PublicTransportLaneUsageLogging;
                EnablePathfindingPenaltyDiagnosticLogging = enablePathfindingPenaltyDiagnosticLogging;
                EnablePathObsoleteSourceLogging = enablePathObsoleteSourceLogging;
                EnableFocusedVehicleOnlyRouteLogging = enableFocusedVehicleOnlyRouteLogging;
            }

            public static LoggedSettingsSnapshot Capture()
            {
                return new LoggedSettingsSnapshot(
                    EnforcementGameplaySettingsService.Current,
                    Mod.Settings?.EnableEstimatedRerouteLogging ?? false,
                    Mod.Settings?.EnableEnforcementEventLogging ?? false,
                    Mod.Settings?.EnableType2PublicTransportLaneUsageLogging ?? false,
                    Mod.Settings?.EnableType3PublicTransportLaneUsageLogging ?? false,
                    Mod.Settings?.EnableType4PublicTransportLaneUsageLogging ?? false,
                    Mod.Settings?.EnablePathfindingPenaltyDiagnosticLogging ?? false,
                    Mod.Settings?.EnablePathObsoleteSourceLogging ?? false,
                    Mod.Settings?.EnableFocusedVehicleOnlyRouteLogging ?? false);
            }

            public string ToLogString()
            {
                StringBuilder builder = new StringBuilder(512);
                Append(builder, nameof(Setting.EnablePublicTransportLaneEnforcement), Gameplay.EnablePublicTransportLaneEnforcement);
                Append(builder, nameof(Setting.EnableMidBlockCrossingEnforcement), Gameplay.EnableMidBlockCrossingEnforcement);
                Append(builder, nameof(Setting.EnableIntersectionMovementEnforcement), Gameplay.EnableIntersectionMovementEnforcement);
                Append(builder, nameof(Setting.EnableEstimatedRerouteLogging), EnableEstimatedRerouteLogging);
                Append(builder, nameof(Setting.EnableEnforcementEventLogging), EnableEnforcementEventLogging);
                Append(builder, nameof(Setting.EnableType2PublicTransportLaneUsageLogging), EnableType2PublicTransportLaneUsageLogging);
                Append(builder, nameof(Setting.EnableType3PublicTransportLaneUsageLogging), EnableType3PublicTransportLaneUsageLogging);
                Append(builder, nameof(Setting.EnableType4PublicTransportLaneUsageLogging), EnableType4PublicTransportLaneUsageLogging);
                Append(builder, nameof(Setting.EnablePathfindingPenaltyDiagnosticLogging), EnablePathfindingPenaltyDiagnosticLogging);
                Append(builder, nameof(Setting.EnablePathObsoleteSourceLogging), EnablePathObsoleteSourceLogging);
                Append(builder, nameof(Setting.EnableFocusedVehicleOnlyRouteLogging), EnableFocusedVehicleOnlyRouteLogging);
                Append(builder, nameof(Setting.AllowRoadPublicTransportVehicles), Gameplay.AllowRoadPublicTransportVehicles);
                Append(builder, nameof(Setting.AllowTaxis), Gameplay.AllowTaxis);
                Append(builder, nameof(Setting.AllowPoliceCars), Gameplay.AllowPoliceCars);
                Append(builder, nameof(Setting.AllowFireEngines), Gameplay.AllowFireEngines);
                Append(builder, nameof(Setting.AllowAmbulances), Gameplay.AllowAmbulances);
                Append(builder, nameof(Setting.AllowGarbageTrucks), Gameplay.AllowGarbageTrucks);
                Append(builder, nameof(Setting.AllowPostVans), Gameplay.AllowPostVans);
                Append(builder, nameof(Setting.AllowRoadMaintenanceVehicles), Gameplay.AllowRoadMaintenanceVehicles);
                Append(builder, nameof(Setting.AllowSnowplows), Gameplay.AllowSnowplows);
                Append(builder, nameof(Setting.AllowVehicleMaintenanceVehicles), Gameplay.AllowVehicleMaintenanceVehicles);
                Append(builder, nameof(Setting.AllowPersonalCars), Gameplay.AllowPersonalCars);
                Append(builder, nameof(Setting.AllowDeliveryTrucks), Gameplay.AllowDeliveryTrucks);
                Append(builder, nameof(Setting.AllowCargoTransportVehicles), Gameplay.AllowCargoTransportVehicles);
                Append(builder, nameof(Setting.AllowHearses), Gameplay.AllowHearses);
                Append(builder, nameof(Setting.AllowPrisonerTransports), Gameplay.AllowPrisonerTransports);
                Append(builder, nameof(Setting.AllowParkMaintenanceVehicles), Gameplay.AllowParkMaintenanceVehicles);
                Append(builder, nameof(Setting.PublicTransportLaneExitPressureThresholdDays), FormatFloat(Gameplay.PublicTransportLaneExitPressureThresholdDays));
                Append(builder, nameof(Setting.PublicTransportLaneFineAmount), Gameplay.PublicTransportLaneFineAmount);
                Append(builder, nameof(Setting.MidBlockCrossingFineAmount), Gameplay.MidBlockCrossingFineAmount);
                Append(builder, nameof(Setting.IntersectionMovementFineAmount), Gameplay.IntersectionMovementFineAmount);
                Append(builder, nameof(Setting.EnablePublicTransportLaneRepeatPenalty), Gameplay.EnablePublicTransportLaneRepeatPenalty);
                Append(builder, nameof(Setting.PublicTransportLaneRepeatWindowMonths), Gameplay.PublicTransportLaneRepeatWindowMonths);
                Append(builder, nameof(Setting.PublicTransportLaneRepeatThreshold), Gameplay.PublicTransportLaneRepeatThreshold);
                Append(builder, nameof(Setting.PublicTransportLaneRepeatMultiplierPercent), Gameplay.PublicTransportLaneRepeatMultiplierPercent);
                Append(builder, nameof(Setting.EnableMidBlockCrossingRepeatPenalty), Gameplay.EnableMidBlockCrossingRepeatPenalty);
                Append(builder, nameof(Setting.MidBlockCrossingRepeatWindowMonths), Gameplay.MidBlockCrossingRepeatWindowMonths);
                Append(builder, nameof(Setting.MidBlockCrossingRepeatThreshold), Gameplay.MidBlockCrossingRepeatThreshold);
                Append(builder, nameof(Setting.MidBlockCrossingRepeatMultiplierPercent), Gameplay.MidBlockCrossingRepeatMultiplierPercent);
                Append(builder, nameof(Setting.EnableIntersectionMovementRepeatPenalty), Gameplay.EnableIntersectionMovementRepeatPenalty);
                Append(builder, nameof(Setting.IntersectionMovementRepeatWindowMonths), Gameplay.IntersectionMovementRepeatWindowMonths);
                Append(builder, nameof(Setting.IntersectionMovementRepeatThreshold), Gameplay.IntersectionMovementRepeatThreshold);
                Append(builder, nameof(Setting.IntersectionMovementRepeatMultiplierPercent), Gameplay.IntersectionMovementRepeatMultiplierPercent);
                return builder.ToString();
            }

            private static void Append(StringBuilder builder, string name, bool value)
            {
                AppendSeparator(builder);
                builder.Append(name).Append('=').Append(value.ToString().ToLowerInvariant());
            }

            private static void Append(StringBuilder builder, string name, int value)
            {
                AppendSeparator(builder);
                builder.Append(name).Append('=').Append(value);
            }

            private static void Append(StringBuilder builder, string name, string value)
            {
                AppendSeparator(builder);
                builder.Append(name).Append('=').Append(value);
            }

            private static void AppendSeparator(StringBuilder builder)
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }
            }
        }
    }
}
