using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public static class EnforcementLoggingPolicy
    {
        public static bool IsBurstLoggingActive => BurstLoggingService.IsActive;

        public static bool EnableEstimatedRerouteLogging => Mod.Settings?.EnableEstimatedRerouteLogging ?? false;

        public static bool EnableEnforcementEventLogging => Mod.Settings?.EnableEnforcementEventLogging ?? false;

        public static bool EnablePolicyImpactSummaryLogging =>
            Mod.Settings?.EnablePolicyImpactSummaryLogging ?? false;

        public static bool EnableFineIncomeLogging =>
            Mod.Settings?.EnableFineIncomeLogging ?? false;

        public static bool EnableChirperLifecycleLogging =>
            Mod.Settings?.EnableChirperLifecycleLogging ?? false;

        public static bool EnableType2PublicTransportLaneUsageLogging => Mod.Settings?.EnableType2PublicTransportLaneUsageLogging ?? false;

        public static bool EnableType3PublicTransportLaneUsageLogging => Mod.Settings?.EnableType3PublicTransportLaneUsageLogging ?? false;

        public static bool EnableType4PublicTransportLaneUsageLogging => Mod.Settings?.EnableType4PublicTransportLaneUsageLogging ?? false;

        public static bool EnablePathfindingPenaltyDiagnosticLogging => Mod.Settings?.EnablePathfindingPenaltyDiagnosticLogging ?? false;

        public static bool EnablePathObsoleteSourceLogging => Mod.Settings?.EnablePathObsoleteSourceLogging ?? false;

        public static bool EnableAllVehicleRouteSelectionChangeLogging =>
            Mod.Settings?.EnableAllVehicleRouteSelectionChangeLogging ?? false;

        public static bool EnableFocusedRouteRebuildDiagnosticsLogging =>
            Mod.Settings?.EnableFocusedRouteRebuildDiagnosticsLogging ?? false;

        public static bool EnableFocusedVehicleOnlyRouteLogging =>
            Mod.Settings?.EnableFocusedVehicleOnlyRouteLogging ?? false;

        public static bool ShouldLogEstimatedReroutes()
        {
            return EnableEstimatedRerouteLogging;
        }

        public static bool ShouldLogEnforcementEvents()
        {
            return EnableEnforcementEventLogging;
        }

        public static bool ShouldLogVehicleSpecificEnforcementEvents()
        {
            return EnableEnforcementEventLogging;
        }

        public static bool ShouldLogPolicyImpactSummary()
        {
            return EnablePolicyImpactSummaryLogging;
        }

        public static bool ShouldLogFineIncomeSummary()
        {
            return EnableFineIncomeLogging;
        }

        public static bool ShouldLogType2Usage()
        {
            return EnableType2PublicTransportLaneUsageLogging;
        }

        public static bool ShouldLogType3Usage()
        {
            return EnableType3PublicTransportLaneUsageLogging;
        }


        public static bool ShouldLogType4Usage()
        {
            return EnableType4PublicTransportLaneUsageLogging;
        }

        public static bool ShouldLogPathfindingPenaltyDiagnostics()
        {
            return EnablePathfindingPenaltyDiagnosticLogging;
        }

        public static bool ShouldLogPathObsoleteSources()
        {
            return EnablePathObsoleteSourceLogging;
        }

        public static bool ShouldLogVehicleSpecificPathObsoleteSource(Entity vehicle)
        {
            return ShouldLogVehicleSpecificVisibleLog(
                ShouldLogPathObsoleteSources(),
                vehicle);
        }

        public static bool ShouldLogAllVehicleRouteSelectionChanges()
        {
            return EnableAllVehicleRouteSelectionChangeLogging;
        }

        public static bool ShouldLogRouteSelectionChangeSummary(Entity vehicle)
        {
            return ShouldLogVehicleSpecificVisibleLog(
                ShouldLogAllVehicleRouteSelectionChanges(),
                vehicle);
        }

        public static bool ShouldLogFocusedRouteRebuildDiagnostics()
        {
            return EnableFocusedRouteRebuildDiagnosticsLogging;
        }

        public static bool ShouldLogPolicyDiagnostics()
        {
            return EnablePolicyImpactSummaryLogging;
        }

        public static bool ShouldLogFineDiagnostics()
        {
            return EnableFineIncomeLogging;
        }

        public static bool ShouldLogRerouteDiagnostics()
        {
            return ShouldObserveRouteDebugState();
        }

        public static bool ShouldLogChirperDiagnostics()
        {
            return EnableChirperLifecycleLogging;
        }

        public static bool ShouldObserveRouteDebugState()
        {
            return ShouldLogEstimatedReroutes() ||
                ShouldLogAllVehicleRouteSelectionChanges() ||
                ShouldLogFocusedRouteRebuildDiagnostics() ||
                ShouldLogPathfindingPenaltyDiagnostics();
        }

        public static bool ShouldLogFocusedPathfindSetup(Entity vehicle)
        {
            return ShouldLogFocusedRouteRebuildDiagnostics() &&
                   FocusedLoggingService.IsWatched(vehicle);
        }

        public static bool ShouldRestrictVehicleSpecificRouteDebugLogsToWatchedVehicles()
        {
            return EnableFocusedVehicleOnlyRouteLogging;
        }

        public static bool ShouldLogVehicleSpecificPathfindingPenaltyDiagnostics(Entity vehicle)
        {
            return ShouldLogVehicleSpecificVisibleLog(
                ShouldLogPathfindingPenaltyDiagnostics(),
                vehicle);
        }

        public static bool ShouldLogVehicleSpecificVisibleLog(bool baseEnabled, Entity vehicle)
        {
            if (!baseEnabled || vehicle == Entity.Null)
            {
                return false;
            }

            return !ShouldRestrictVehicleSpecificRouteDebugLogsToWatchedVehicles() ||
                FocusedLoggingService.IsWatched(vehicle);
        }

        public static bool ShouldLogVehicleSpecificEnforcementEvent(Entity vehicle)
        {
            return ShouldLogVehicleSpecificVisibleLog(
                ShouldLogVehicleSpecificEnforcementEvents(),
                vehicle);
        }

        public static bool ShouldLogVehicleSpecificType2Usage(Entity vehicle)
        {
            return ShouldLogVehicleSpecificVisibleLog(ShouldLogType2Usage(), vehicle);
        }

        public static bool ShouldLogVehicleSpecificType3Usage(Entity vehicle)
        {
            return ShouldLogVehicleSpecificVisibleLog(ShouldLogType3Usage(), vehicle);
        }

        public static bool ShouldLogVehicleSpecificType4Usage(Entity vehicle)
        {
            return ShouldLogVehicleSpecificVisibleLog(ShouldLogType4Usage(), vehicle);
        }

        public static void RecordEnforcementEvent(string message, Entity vehicle)
        {
            if (!ShouldLogVehicleSpecificEnforcementEvent(vehicle) || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            EnforcementTelemetry.RecordEvent(message);
            Mod.log.Info(message);
        }

        public static void RecordType2Usage(string message, Entity vehicle)
        {
            if (!ShouldLogVehicleSpecificType2Usage(vehicle) || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            EnforcementTelemetry.RecordEvent(message);
            Mod.log.Info(message);
        }

        public static void RecordType3Usage(string message, Entity vehicle)
        {
            if (!ShouldLogVehicleSpecificType3Usage(vehicle) || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            EnforcementTelemetry.RecordEvent(message);
            Mod.log.Info(message);
        }

        public static void RecordType4Usage(string message, Entity vehicle)
        {
            if (!ShouldLogVehicleSpecificType4Usage(vehicle) || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            EnforcementTelemetry.RecordEvent(message);
            Mod.log.Info(message);
        }

    }
}
