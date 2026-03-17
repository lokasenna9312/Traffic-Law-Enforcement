namespace Traffic_Law_Enforcement
{
    public static class EnforcementLoggingPolicy
    {
        public static bool EnableEstimatedRerouteLogging => Mod.Settings?.EnableEstimatedRerouteLogging ?? false;

        public static bool EnableEnforcementEventLogging => Mod.Settings?.EnableEnforcementEventLogging ?? false;

        public static bool EnableAllowedType3PublicTransportLaneUsageLogging => Mod.Settings?.EnableAllowedType3PublicTransportLaneUsageLogging ?? false;

        public static bool EnablePathfindingPenaltyDiagnosticLogging => Mod.Settings?.EnablePathfindingPenaltyDiagnosticLogging ?? false;

        public static bool ShouldLogEstimatedReroutes()
        {
            return Mod.IsEnforcementEnabled && EnableEstimatedRerouteLogging;
        }

        public static bool ShouldLogEnforcementEvents()
        {
            return EnableEnforcementEventLogging;
        }

        public static bool ShouldLogAllowedType3Usage()
        {
            return EnableAllowedType3PublicTransportLaneUsageLogging;
        }

        public static bool ShouldLogPathfindingPenaltyDiagnostics()
        {
            return EnablePathfindingPenaltyDiagnosticLogging;
        }

        public static void RecordEnforcementEvent(string message)
        {
            if (!ShouldLogEnforcementEvents() || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            EnforcementTelemetry.RecordEvent(message);
            Mod.log.Info(message);
        }

        public static void RecordAllowedType3Usage(string message)
        {
            if (!ShouldLogAllowedType3Usage() || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            EnforcementTelemetry.RecordEvent(message);
            Mod.log.Info(message);
        }
    }
}
