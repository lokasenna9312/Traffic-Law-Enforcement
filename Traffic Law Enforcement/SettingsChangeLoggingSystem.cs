using System;
using System.Globalization;
using System.Text;
using Game;
using Game.Simulation;
using Unity.Entities;
using UnityEngine;

namespace Traffic_Law_Enforcement
{
    public class SettingsChangeLoggingSystem : GameSystemBase
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
                Mod.log.Info($"Settings snapshot: inGameTime={GetInGameTimeLabel()}, {snapshot.ToLogString()}");
                m_LastSnapshot = snapshot;
                m_HasSnapshot = true;
                return;
            }

            string inGameTimeLabel = GetInGameTimeLabel();
            LogChanges(inGameTimeLabel, m_LastSnapshot, snapshot);
            m_LastSnapshot = snapshot;
        }

        private void LogChanges(string inGameTimeLabel, LoggedSettingsSnapshot previous, LoggedSettingsSnapshot current)
        {
            LogChange(inGameTimeLabel, nameof(Setting.EnablePublicTransportLaneEnforcement), previous.Gameplay.EnablePublicTransportLaneEnforcement, current.Gameplay.EnablePublicTransportLaneEnforcement);
            LogChange(inGameTimeLabel, nameof(Setting.EnableMidBlockCrossingEnforcement), previous.Gameplay.EnableMidBlockCrossingEnforcement, current.Gameplay.EnableMidBlockCrossingEnforcement);
            LogChange(inGameTimeLabel, nameof(Setting.EnableIntersectionMovementEnforcement), previous.Gameplay.EnableIntersectionMovementEnforcement, current.Gameplay.EnableIntersectionMovementEnforcement);
            LogChange(inGameTimeLabel, nameof(Setting.EnableEstimatedRerouteLogging), previous.EnableEstimatedRerouteLogging, current.EnableEstimatedRerouteLogging);
            LogChange(inGameTimeLabel, nameof(Setting.EnableEnforcementEventLogging), previous.EnableEnforcementEventLogging, current.EnableEnforcementEventLogging);
            LogChange(inGameTimeLabel, nameof(Setting.EnableAllowedType3PublicTransportLaneUsageLogging), previous.EnableAllowedType3PublicTransportLaneUsageLogging, current.EnableAllowedType3PublicTransportLaneUsageLogging);
            LogChange(inGameTimeLabel, nameof(Setting.EnablePathfindingPenaltyDiagnosticLogging), previous.EnablePathfindingPenaltyDiagnosticLogging, current.EnablePathfindingPenaltyDiagnosticLogging);
            LogChange(inGameTimeLabel, nameof(Setting.AllowRoadPublicTransportVehicles), previous.Gameplay.AllowRoadPublicTransportVehicles, current.Gameplay.AllowRoadPublicTransportVehicles);
            LogChange(inGameTimeLabel, nameof(Setting.AllowTaxis), previous.Gameplay.AllowTaxis, current.Gameplay.AllowTaxis);
            LogChange(inGameTimeLabel, nameof(Setting.AllowPoliceCars), previous.Gameplay.AllowPoliceCars, current.Gameplay.AllowPoliceCars);
            LogChange(inGameTimeLabel, nameof(Setting.AllowFireEngines), previous.Gameplay.AllowFireEngines, current.Gameplay.AllowFireEngines);
            LogChange(inGameTimeLabel, nameof(Setting.AllowAmbulances), previous.Gameplay.AllowAmbulances, current.Gameplay.AllowAmbulances);
            LogChange(inGameTimeLabel, nameof(Setting.AllowGarbageTrucks), previous.Gameplay.AllowGarbageTrucks, current.Gameplay.AllowGarbageTrucks);
            LogChange(inGameTimeLabel, nameof(Setting.AllowPostVans), previous.Gameplay.AllowPostVans, current.Gameplay.AllowPostVans);
            LogChange(inGameTimeLabel, nameof(Setting.AllowRoadMaintenanceVehicles), previous.Gameplay.AllowRoadMaintenanceVehicles, current.Gameplay.AllowRoadMaintenanceVehicles);
            LogChange(inGameTimeLabel, nameof(Setting.AllowSnowplows), previous.Gameplay.AllowSnowplows, current.Gameplay.AllowSnowplows);
            LogChange(inGameTimeLabel, nameof(Setting.AllowVehicleMaintenanceVehicles), previous.Gameplay.AllowVehicleMaintenanceVehicles, current.Gameplay.AllowVehicleMaintenanceVehicles);
            LogChange(inGameTimeLabel, nameof(Setting.AllowPersonalCars), previous.Gameplay.AllowPersonalCars, current.Gameplay.AllowPersonalCars);
            LogChange(inGameTimeLabel, nameof(Setting.AllowDeliveryTrucks), previous.Gameplay.AllowDeliveryTrucks, current.Gameplay.AllowDeliveryTrucks);
            LogChange(inGameTimeLabel, nameof(Setting.AllowCargoTransportVehicles), previous.Gameplay.AllowCargoTransportVehicles, current.Gameplay.AllowCargoTransportVehicles);
            LogChange(inGameTimeLabel, nameof(Setting.AllowHearses), previous.Gameplay.AllowHearses, current.Gameplay.AllowHearses);
            LogChange(inGameTimeLabel, nameof(Setting.AllowPrisonerTransports), previous.Gameplay.AllowPrisonerTransports, current.Gameplay.AllowPrisonerTransports);
            LogChange(inGameTimeLabel, nameof(Setting.AllowParkMaintenanceVehicles), previous.Gameplay.AllowParkMaintenanceVehicles, current.Gameplay.AllowParkMaintenanceVehicles);

            LogChange(inGameTimeLabel, nameof(Setting.PublicTransportLaneExitPressureThresholdDays), previous.Gameplay.PublicTransportLaneExitPressureThresholdDays, current.Gameplay.PublicTransportLaneExitPressureThresholdDays);
            LogChange(inGameTimeLabel, nameof(Setting.PublicTransportLaneFineAmount), previous.Gameplay.PublicTransportLaneFineAmount, current.Gameplay.PublicTransportLaneFineAmount);
            LogChange(inGameTimeLabel, nameof(Setting.MidBlockCrossingFineAmount), previous.Gameplay.MidBlockCrossingFineAmount, current.Gameplay.MidBlockCrossingFineAmount);
            LogChange(inGameTimeLabel, nameof(Setting.IntersectionMovementFineAmount), previous.Gameplay.IntersectionMovementFineAmount, current.Gameplay.IntersectionMovementFineAmount);

            LogChange(inGameTimeLabel, nameof(Setting.EnablePublicTransportLaneRepeatPenalty), previous.Gameplay.EnablePublicTransportLaneRepeatPenalty, current.Gameplay.EnablePublicTransportLaneRepeatPenalty);
            LogChange(inGameTimeLabel, nameof(Setting.PublicTransportLaneRepeatWindowMonths), previous.Gameplay.PublicTransportLaneRepeatWindowMonths, current.Gameplay.PublicTransportLaneRepeatWindowMonths);
            LogChange(inGameTimeLabel, nameof(Setting.PublicTransportLaneRepeatThreshold), previous.Gameplay.PublicTransportLaneRepeatThreshold, current.Gameplay.PublicTransportLaneRepeatThreshold);
            LogChange(inGameTimeLabel, nameof(Setting.PublicTransportLaneRepeatMultiplierPercent), previous.Gameplay.PublicTransportLaneRepeatMultiplierPercent, current.Gameplay.PublicTransportLaneRepeatMultiplierPercent);

            LogChange(inGameTimeLabel, nameof(Setting.EnableMidBlockCrossingRepeatPenalty), previous.Gameplay.EnableMidBlockCrossingRepeatPenalty, current.Gameplay.EnableMidBlockCrossingRepeatPenalty);
            LogChange(inGameTimeLabel, nameof(Setting.MidBlockCrossingRepeatWindowMonths), previous.Gameplay.MidBlockCrossingRepeatWindowMonths, current.Gameplay.MidBlockCrossingRepeatWindowMonths);
            LogChange(inGameTimeLabel, nameof(Setting.MidBlockCrossingRepeatThreshold), previous.Gameplay.MidBlockCrossingRepeatThreshold, current.Gameplay.MidBlockCrossingRepeatThreshold);
            LogChange(inGameTimeLabel, nameof(Setting.MidBlockCrossingRepeatMultiplierPercent), previous.Gameplay.MidBlockCrossingRepeatMultiplierPercent, current.Gameplay.MidBlockCrossingRepeatMultiplierPercent);

            LogChange(inGameTimeLabel, nameof(Setting.EnableIntersectionMovementRepeatPenalty), previous.Gameplay.EnableIntersectionMovementRepeatPenalty, current.Gameplay.EnableIntersectionMovementRepeatPenalty);
            LogChange(inGameTimeLabel, nameof(Setting.IntersectionMovementRepeatWindowMonths), previous.Gameplay.IntersectionMovementRepeatWindowMonths, current.Gameplay.IntersectionMovementRepeatWindowMonths);
            LogChange(inGameTimeLabel, nameof(Setting.IntersectionMovementRepeatThreshold), previous.Gameplay.IntersectionMovementRepeatThreshold, current.Gameplay.IntersectionMovementRepeatThreshold);
            LogChange(inGameTimeLabel, nameof(Setting.IntersectionMovementRepeatMultiplierPercent), previous.Gameplay.IntersectionMovementRepeatMultiplierPercent, current.Gameplay.IntersectionMovementRepeatMultiplierPercent);
        }

        private static void LogChange(string inGameTimeLabel, string settingName, bool previous, bool current)
        {
            if (previous == current)
            {
                return;
            }

            Mod.log.Info($"Setting changed: inGameTime={inGameTimeLabel}, {settingName} {previous.ToString().ToLowerInvariant()} -> {current.ToString().ToLowerInvariant()}");
        }

        private static void LogChange(string inGameTimeLabel, string settingName, int previous, int current)
        {
            if (previous == current)
            {
                return;
            }

            Mod.log.Info($"Setting changed: inGameTime={inGameTimeLabel}, {settingName} {previous} -> {current}");
        }

        private static void LogChange(string inGameTimeLabel, string settingName, float previous, float current)
        {
            if (Mathf.Approximately(previous, current))
            {
                return;
            }

            Mod.log.Info($"Setting changed: inGameTime={inGameTimeLabel}, {settingName} {FormatFloat(previous)} -> {FormatFloat(current)}");
        }

        private string GetInGameTimeLabel()
        {
            if (!EnforcementGameTime.IsInitialized || m_TimeSystem == null)
            {
                return "unavailable";
            }

            int daysPerYear = Mathf.Max(1, m_TimeSystem.daysPerYear);
            int day = Mathf.Clamp(Mathf.FloorToInt(m_TimeSystem.normalizedDate * daysPerYear), 0, daysPerYear - 1) + 1;
            float totalHours = Mathf.Clamp01(m_TimeSystem.normalizedTime) * 24f;
            int hour = Mathf.Clamp(Mathf.FloorToInt(totalHours), 0, 23);
            int minute = Mathf.Clamp(Mathf.FloorToInt((totalHours - hour) * 60f), 0, 59);
            return FormattableString.Invariant($"Y{m_TimeSystem.year} day={day} time={hour:00}:{minute:00}");
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
            public readonly bool EnableAllowedType3PublicTransportLaneUsageLogging;
            public readonly bool EnablePathfindingPenaltyDiagnosticLogging;

            private LoggedSettingsSnapshot(
                EnforcementGameplaySettingsState gameplay,
                bool enableEstimatedRerouteLogging,
                bool enableEnforcementEventLogging,
                bool enableAllowedType3PublicTransportLaneUsageLogging,
                bool enablePathfindingPenaltyDiagnosticLogging)
            {
                Gameplay = gameplay;
                EnableEstimatedRerouteLogging = enableEstimatedRerouteLogging;
                EnableEnforcementEventLogging = enableEnforcementEventLogging;
                EnableAllowedType3PublicTransportLaneUsageLogging = enableAllowedType3PublicTransportLaneUsageLogging;
                EnablePathfindingPenaltyDiagnosticLogging = enablePathfindingPenaltyDiagnosticLogging;
            }

            public static LoggedSettingsSnapshot Capture()
            {
                return new LoggedSettingsSnapshot(
                    EnforcementGameplaySettingsService.Current,
                    Mod.Settings?.EnableEstimatedRerouteLogging ?? false,
                    Mod.Settings?.EnableEnforcementEventLogging ?? false,
                    Mod.Settings?.EnableAllowedType3PublicTransportLaneUsageLogging ?? false,
                    Mod.Settings?.EnablePathfindingPenaltyDiagnosticLogging ?? false);
            }

            public string ToLogString()
            {
                StringBuilder builder = new StringBuilder(512);
                Append(builder, nameof(Setting.EnablePublicTransportLaneEnforcement), Gameplay.EnablePublicTransportLaneEnforcement);
                Append(builder, nameof(Setting.EnableMidBlockCrossingEnforcement), Gameplay.EnableMidBlockCrossingEnforcement);
                Append(builder, nameof(Setting.EnableIntersectionMovementEnforcement), Gameplay.EnableIntersectionMovementEnforcement);
                Append(builder, nameof(Setting.EnableEstimatedRerouteLogging), EnableEstimatedRerouteLogging);
                Append(builder, nameof(Setting.EnableEnforcementEventLogging), EnableEnforcementEventLogging);
                Append(builder, nameof(Setting.EnableAllowedType3PublicTransportLaneUsageLogging), EnableAllowedType3PublicTransportLaneUsageLogging);
                Append(builder, nameof(Setting.EnablePathfindingPenaltyDiagnosticLogging), EnablePathfindingPenaltyDiagnosticLogging);
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
