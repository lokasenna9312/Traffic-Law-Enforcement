namespace Traffic_Law_Enforcement
{
    public static class EnforcementLoggingPolicy
    {
        public static bool EnableEstimatedRerouteLogging => Mod.Settings?.EnableEstimatedRerouteLogging ?? false;

        public static bool EnableEnforcementEventLogging => Mod.Settings?.EnableEnforcementEventLogging ?? false;

        public static bool EnableType2PublicTransportLaneUsageLogging => Mod.Settings?.EnableType2PublicTransportLaneUsageLogging ?? false;

        public static bool EnableType3PublicTransportLaneUsageLogging => Mod.Settings?.EnableType3PublicTransportLaneUsageLogging ?? false;

        public static bool EnableType4PublicTransportLaneUsageLogging => Mod.Settings?.EnableType4PublicTransportLaneUsageLogging ?? false;

        public static bool EnablePathfindingPenaltyDiagnosticLogging => Mod.Settings?.EnablePathfindingPenaltyDiagnosticLogging ?? false;

        public static bool EnablePathObsoleteSourceLogging => Mod.Settings?.EnablePathObsoleteSourceLogging ?? false;

        public static bool ShouldLogEstimatedReroutes()
        {
            return Mod.IsEnforcementEnabled && EnableEstimatedRerouteLogging;
        }

        public static bool ShouldLogEnforcementEvents()
        {
            return EnableEnforcementEventLogging;
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

        public static void RecordEnforcementEvent(string message)
        {
            if (!ShouldLogEnforcementEvents() || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            EnforcementTelemetry.RecordEvent(message);
            Mod.log.Info(message);
        }

        public static void RecordType2Usage(string message)
        {
            if (!ShouldLogType2Usage() || string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            EnforcementTelemetry.RecordEvent(message);
            Mod.log.Info(message);
        }

        public static void RecordType3Usage(string message)
        {
            if (!ShouldLogType3Usage() || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            EnforcementTelemetry.RecordEvent(message);
            Mod.log.Info(message);
        }

        public static void RecordType4Usage(string message)
        {
            if (!ShouldLogType4Usage() || string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            EnforcementTelemetry.RecordEvent(message);
            Mod.log.Info(message);
        }
    }
}
