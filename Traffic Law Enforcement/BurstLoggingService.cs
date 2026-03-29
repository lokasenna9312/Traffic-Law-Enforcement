using System;
using System.Diagnostics;

namespace Traffic_Law_Enforcement
{
    internal static class BurstLoggingService
    {
        internal const double DefaultDurationSeconds = 5d;
        internal const int BurstEstimatedRerouteLogsPerUpdate = 64;
        internal const int BurstRouteSelectionChangeLogsPerUpdate = 128;
        internal const int BurstPublicTransportLaneDecisionDiagnosticLogsPerUpdate = 64;

        private static long s_EndTimestamp;

        internal static bool IsActive => GetRemainingSeconds() > 0d;

        internal static void Reset()
        {
            s_EndTimestamp = 0;
        }

        internal static void RequestDefaultBurst()
        {
            RequestBurst(DefaultDurationSeconds);
        }

        internal static void CancelBurst()
        {
            if (!IsActive)
            {
                return;
            }

            s_EndTimestamp = 0;
            Mod.log.Info("[BurstLogging] Cancelled");
        }

        internal static void ToggleDefaultBurst()
        {
            if (IsActive)
            {
                CancelBurst();
                return;
            }

            RequestDefaultBurst();
        }

        internal static void RequestBurst(double durationSeconds)
        {
            if (durationSeconds <= 0d)
            {
                return;
            }

            long now = Stopwatch.GetTimestamp();
            long durationTicks = (long)(durationSeconds * Stopwatch.Frequency);
            long baseline = s_EndTimestamp > now
                ? s_EndTimestamp
                : now;

            s_EndTimestamp = baseline + durationTicks;

            Mod.log.Info(
                $"[BurstLogging] Started: durationSeconds={durationSeconds:0.###}, " +
                $"estimatedReroute={EnforcementLoggingPolicy.EnableEstimatedRerouteLogging}, " +
                $"pathfindingPenaltyDiagnostics={EnforcementLoggingPolicy.EnablePathfindingPenaltyDiagnosticLogging}, " +
                $"allVehicleRouteSelectionChanges={EnforcementLoggingPolicy.EnableAllVehicleRouteSelectionChangeLogging}");
        }

        internal static double GetRemainingSeconds()
        {
            long remainingTicks = s_EndTimestamp - Stopwatch.GetTimestamp();
            return remainingTicks > 0
                ? remainingTicks / (double)Stopwatch.Frequency
                : 0d;
        }

        internal static string DescribeStatus()
        {
            double remainingSeconds = GetRemainingSeconds();
            return remainingSeconds > 0d
                ? $"Active ({remainingSeconds:0.0}s remaining)"
                : "Inactive";
        }
    }
}
