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
        private static long s_CachedStatusTenths = long.MinValue;
        private static string s_CachedStatusText = "Inactive";

        internal static bool IsActive => s_EndTimestamp > Stopwatch.GetTimestamp();

        internal static void Reset()
        {
            s_EndTimestamp = 0;
            InvalidateStatusCache();
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
            InvalidateStatusCache();
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
            InvalidateStatusCache();

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
            if (remainingSeconds <= 0d)
            {
                s_CachedStatusTenths = 0;
                s_CachedStatusText = "Inactive";
                return s_CachedStatusText;
            }

            long remainingTenths = (long)System.Math.Ceiling(remainingSeconds * 10d);
            if (s_CachedStatusTenths == remainingTenths)
            {
                return s_CachedStatusText;
            }

            s_CachedStatusTenths = remainingTenths;
            s_CachedStatusText =
                $"Active ({remainingTenths / 10d:0.0}s remaining)";
            return s_CachedStatusText;
        }

        private static void InvalidateStatusCache()
        {
            s_CachedStatusTenths = long.MinValue;
        }
    }
}
