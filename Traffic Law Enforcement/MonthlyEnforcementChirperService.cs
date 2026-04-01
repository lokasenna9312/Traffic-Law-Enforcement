using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Game;
using Game.SceneFlow;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public struct MonthlyEnforcementTrackingState
    {
        public long m_MonthIndex;
        public int m_TotalPathRequestCount;
        public int m_TotalActualPathCount;
        public int m_PublicTransportLaneCount;
        public int m_MidBlockCrossingCount;
        public int m_IntersectionMovementCount;
        public int m_TotalFineAmount;
        public int m_TotalAvoidedPathCount;
        public int m_PublicTransportLaneFineAmount;
        public int m_MidBlockCrossingFineAmount;
        public int m_IntersectionMovementFineAmount;
        public int m_PublicTransportLaneAvoidedEventCount;
        public int m_MidBlockCrossingAvoidedEventCount;
        public int m_IntersectionMovementAvoidedEventCount;
        public int m_TotalActualOrAvoidedPathCount;
        public int m_PublicTransportLaneActualOrAvoidedPathCount;
        public int m_MidBlockCrossingActualOrAvoidedPathCount;
        public int m_IntersectionMovementActualOrAvoidedPathCount;

        public MonthlyEnforcementTrackingState(
            long monthIndex,
            int totalPathRequestCount,
            int totalActualPathCount,
            int publicTransportLaneCount,
            int midBlockCrossingCount,
            int intersectionMovementCount,
            int totalFineAmount,
            int totalAvoidedPathCount,
            int publicTransportLaneFineAmount,
            int midBlockCrossingFineAmount,
            int intersectionMovementFineAmount,
            int publicTransportLaneAvoidedEventCount,
            int midBlockCrossingAvoidedEventCount,
            int intersectionMovementAvoidedEventCount,
            int totalActualOrAvoidedPathCount,
            int publicTransportLaneActualOrAvoidedPathCount,
            int midBlockCrossingActualOrAvoidedPathCount,
            int intersectionMovementActualOrAvoidedPathCount)
        {
            m_MonthIndex = monthIndex;
            m_TotalPathRequestCount = totalPathRequestCount;
            m_TotalActualPathCount = totalActualPathCount;
            m_PublicTransportLaneCount = publicTransportLaneCount;
            m_MidBlockCrossingCount = midBlockCrossingCount;
            m_IntersectionMovementCount = intersectionMovementCount;
            m_TotalFineAmount = totalFineAmount;
            m_TotalAvoidedPathCount = totalAvoidedPathCount;
            m_PublicTransportLaneFineAmount = publicTransportLaneFineAmount;
            m_MidBlockCrossingFineAmount = midBlockCrossingFineAmount;
            m_IntersectionMovementFineAmount = intersectionMovementFineAmount;
            m_PublicTransportLaneAvoidedEventCount = publicTransportLaneAvoidedEventCount;
            m_MidBlockCrossingAvoidedEventCount = midBlockCrossingAvoidedEventCount;
            m_IntersectionMovementAvoidedEventCount = intersectionMovementAvoidedEventCount;
            m_TotalActualOrAvoidedPathCount = totalActualOrAvoidedPathCount;
            m_PublicTransportLaneActualOrAvoidedPathCount = publicTransportLaneActualOrAvoidedPathCount;
            m_MidBlockCrossingActualOrAvoidedPathCount = midBlockCrossingActualOrAvoidedPathCount;
            m_IntersectionMovementActualOrAvoidedPathCount = intersectionMovementActualOrAvoidedPathCount;
        }
    }

    public struct MonthlyEnforcementReport
    {
        public long m_MonthIndex;
        public int m_TotalPathRequestCount;
        public int m_TotalActualPathCount;
        public int m_PublicTransportLaneCount;
        public int m_MidBlockCrossingCount;
        public int m_IntersectionMovementCount;
        public int m_TotalFineAmount;
        public int m_TotalAvoidedPathCount;
        public int m_PublicTransportLaneFineAmount;
        public int m_MidBlockCrossingFineAmount;
        public int m_IntersectionMovementFineAmount;
        public int m_PublicTransportLaneAvoidedEventCount;
        public int m_MidBlockCrossingAvoidedEventCount;
        public int m_IntersectionMovementAvoidedEventCount;
        public int m_TotalActualOrAvoidedPathCount;
        public int m_PublicTransportLaneActualOrAvoidedPathCount;
        public int m_MidBlockCrossingActualOrAvoidedPathCount;
        public int m_IntersectionMovementActualOrAvoidedPathCount;

        public MonthlyEnforcementReport(
            long monthIndex,
            int totalPathRequestCount,
            int totalActualPathCount,
            int publicTransportLaneCount,
            int midBlockCrossingCount,
            int intersectionMovementCount,
            int totalFineAmount,
            int totalAvoidedPathCount,
            int publicTransportLaneFineAmount,
            int midBlockCrossingFineAmount,
            int intersectionMovementFineAmount,
            int publicTransportLaneAvoidedEventCount,
            int midBlockCrossingAvoidedEventCount,
            int intersectionMovementAvoidedEventCount,
            int totalActualOrAvoidedPathCount,
            int publicTransportLaneActualOrAvoidedPathCount,
            int midBlockCrossingActualOrAvoidedPathCount,
            int intersectionMovementActualOrAvoidedPathCount)
        {
            m_MonthIndex = monthIndex;
            m_TotalPathRequestCount = totalPathRequestCount;
            m_TotalActualPathCount = totalActualPathCount;
            m_PublicTransportLaneCount = publicTransportLaneCount;
            m_MidBlockCrossingCount = midBlockCrossingCount;
            m_IntersectionMovementCount = intersectionMovementCount;
            m_TotalFineAmount = totalFineAmount;
            m_TotalAvoidedPathCount = totalAvoidedPathCount;
            m_PublicTransportLaneFineAmount = publicTransportLaneFineAmount;
            m_MidBlockCrossingFineAmount = midBlockCrossingFineAmount;
            m_IntersectionMovementFineAmount = intersectionMovementFineAmount;
            m_PublicTransportLaneAvoidedEventCount = publicTransportLaneAvoidedEventCount;
            m_MidBlockCrossingAvoidedEventCount = midBlockCrossingAvoidedEventCount;
            m_IntersectionMovementAvoidedEventCount = intersectionMovementAvoidedEventCount;
            m_TotalActualOrAvoidedPathCount = totalActualOrAvoidedPathCount;
            m_PublicTransportLaneActualOrAvoidedPathCount = publicTransportLaneActualOrAvoidedPathCount;
            m_MidBlockCrossingActualOrAvoidedPathCount = midBlockCrossingActualOrAvoidedPathCount;
            m_IntersectionMovementActualOrAvoidedPathCount = intersectionMovementActualOrAvoidedPathCount;
        }

        public int TotalViolationCount => m_TotalActualPathCount;
        public int TotalDecisionCount => m_TotalActualOrAvoidedPathCount;
    }

    public static class MonthlyEnforcementChirperService
    {
        private static readonly List<MonthlyEnforcementReport> s_ReportHistory = new List<MonthlyEnforcementReport>();
        private static readonly ReadOnlyCollection<MonthlyEnforcementReport> s_ReportHistoryView =
            s_ReportHistory.AsReadOnly();
        private static int s_ManualPreviewRequestCount;

        public static bool TryGetTrackingState(out MonthlyEnforcementTrackingState trackingState)
        {
            if (EnforcementPolicyImpactService.TryGetTrackingState(
                    out EnforcementPolicyImpactTrackingState policyTrackingState))
            {
                trackingState =
                    CreateTrackingStateFromPolicyImpactTrackingState(policyTrackingState);
                return true;
            }

            trackingState = default;
            return false;
        }

        public static IReadOnlyCollection<MonthlyEnforcementReport> GetReportHistorySnapshot()
        {
            return s_ReportHistoryView;
        }

        public static void ResetPersistentData()
        {
            s_ReportHistory.Clear();
            s_ManualPreviewRequestCount = 0;
        }

        public static void LoadPersistentData(MonthlyEnforcementTrackingState? trackingState, IEnumerable<MonthlyEnforcementReport> reports)
        {
            ResetPersistentData();

            if (reports == null)
            {
                return;
            }

            List<MonthlyEnforcementReport> sortedReports = new List<MonthlyEnforcementReport>();
            foreach (MonthlyEnforcementReport report in reports)
            {
                sortedReports.Add(report);
            }

            sortedReports.Sort(static (left, right) => left.m_MonthIndex.CompareTo(right.m_MonthIndex));
            foreach (MonthlyEnforcementReport report in sortedReports)
            {
                s_ReportHistory.Add(report);
            }

            if (EnforcementLoggingPolicy.ShouldLogChirperDiagnostics())
            {
                string trackingSummary =
                    trackingState.HasValue
                        ? $"legacyTrackingMonth={trackingState.Value.m_MonthIndex}, totalActual={trackingState.Value.m_TotalActualPathCount}, totalAvoided={trackingState.Value.m_TotalAvoidedPathCount}, totalDecision={trackingState.Value.m_TotalActualOrAvoidedPathCount}"
                        : "legacyTrackingMonth=null";

                Mod.log.Info(
                    "[ENFORCEMENT_CHIRPER_STATE] " +
                    $"phase=LoadPersistentData, {trackingSummary}, reports={s_ReportHistory.Count}");
            }
        }

        public static bool ResetTrackingToCurrentMonth(long currentMonthIndex)
        {
            return EnforcementPolicyImpactService.ResetTrackingToCurrentMonth(currentMonthIndex);
        }

        public static bool EnsureTrackingInitialized(long currentMonthIndex)
        {
            return EnforcementPolicyImpactService.EnsureTrackingInitialized(currentMonthIndex);
        }

        public static bool TryAdvanceMonth(long currentMonthIndex, out MonthlyEnforcementReport report)
        {
            if (!EnforcementPolicyImpactService.TryAdvanceMonth(currentMonthIndex, out report))
            {
                return false;
            }

            UpsertReport(report);
            return true;
        }

        public static MonthlyEnforcementReport BuildCurrentPeriodPreview()
        {
            if (!EnforcementGameTime.IsInitialized)
            {
                return default;
            }

            RollingWindowSnapshot snapshot = EnforcementPolicyImpactService.GetRollingWindowSnapshot();
            return new MonthlyEnforcementReport(
                EnforcementGameTime.GetMonthIndex(GetCurrentPeriodStartMonthTicks(EnforcementGameTime.CurrentTimestampMonthTicks)),
                snapshot.TotalPathRequestCount,
                snapshot.TotalActualPathCount,
                snapshot.PublicTransportLaneActualCount,
                snapshot.MidBlockCrossingActualCount,
                snapshot.IntersectionMovementActualCount,
                snapshot.TotalFineAmount,
                snapshot.TotalAvoidedPathCount,
                snapshot.PublicTransportLaneFineAmount,
                snapshot.MidBlockCrossingFineAmount,
                snapshot.IntersectionMovementFineAmount,
                snapshot.PublicTransportLaneAvoidedEventCount,
                snapshot.MidBlockCrossingAvoidedEventCount,
                snapshot.IntersectionMovementAvoidedEventCount,
                snapshot.TotalActualOrAvoidedPathCount,
                snapshot.PublicTransportLaneActualOrAvoidedPathCount,
                snapshot.MidBlockCrossingActualOrAvoidedPathCount,
                snapshot.IntersectionMovementActualOrAvoidedPathCount);
        }

        public static long GetCurrentPeriodStartMonthTicks(long currentTimestampMonthTicks)
        {
            return currentTimestampMonthTicks > 0L
                ? System.Math.Max(0L, currentTimestampMonthTicks - EnforcementGameTime.CurrentMonthTicksPerMonth)
                : 0L;
        }

        public static long GetReportPeriodStartMonthTicks(MonthlyEnforcementReport report)
        {
            return EnforcementGameTime.GetMonthTickAtMonthIndex(report.m_MonthIndex);
        }

        public static long GetReportPeriodEndMonthTicks(MonthlyEnforcementReport report)
        {
            return EnforcementGameTime.GetMonthTickAtMonthIndex(report.m_MonthIndex + 1L);
        }

        public static bool TryPublishManualPreviewNow(out string failureReason)
        {
            failureReason = null;

            if (GameManager.instance?.gameMode == null || !GameManager.instance.gameMode.IsGameOrEditor())
            {
                failureReason = "game is not in a loaded city session";
                return false;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                failureReason = "default game world is unavailable";
                return false;
            }

            if (!EnforcementGameTime.TryUpdateFromWorld(world, logOnInitialization: true, out failureReason))
            {
                return false;
            }

            MonthlyEnforcementChirperSystem system = world.GetExistingSystemManaged<MonthlyEnforcementChirperSystem>() ?? world.GetOrCreateSystemManaged<MonthlyEnforcementChirperSystem>();
            if (system == null)
            {
                failureReason = "monthly chirper system is unavailable";
                return false;
            }

            return system.TryPublishManualPreviewNow(out failureReason);
        }

        public static void RequestManualPreview()
        {
            if (TryPublishManualPreviewNow(out string failureReason))
            {
                return;
            }
            Interlocked.Increment(ref s_ManualPreviewRequestCount);
        }

        public static bool TryConsumeManualPreviewRequest()
        {
            while (true)
            {
                int currentCount = Volatile.Read(ref s_ManualPreviewRequestCount);
                if (currentCount <= 0)
                {
                    return false;
                }

                if (Interlocked.CompareExchange(ref s_ManualPreviewRequestCount, currentCount - 1, currentCount) == currentCount)
                {
                    return true;
                }
            }
        }

        public static bool HasPendingManualPreviewRequests()
        {
            return Volatile.Read(ref s_ManualPreviewRequestCount) > 0;
        }

        private static void UpsertReport(MonthlyEnforcementReport report)
        {
            int count = s_ReportHistory.Count;
            if (count > 0)
            {
                int lastIndex = count - 1;
                MonthlyEnforcementReport lastReport = s_ReportHistory[lastIndex];

                if (lastReport.m_MonthIndex == report.m_MonthIndex)
                {
                    s_ReportHistory[lastIndex] = report;
                    return;
                }

                if (lastReport.m_MonthIndex < report.m_MonthIndex)
                {
                    s_ReportHistory.Add(report);
                    return;
                }
            }

            for (int index = 0; index < s_ReportHistory.Count; index += 1)
            {
                if (s_ReportHistory[index].m_MonthIndex == report.m_MonthIndex)
                {
                    s_ReportHistory[index] = report;
                    return;
                }
            }

            s_ReportHistory.Add(report);
            s_ReportHistory.Sort((left, right) => left.m_MonthIndex.CompareTo(right.m_MonthIndex));
        }

        private static MonthlyEnforcementTrackingState CreateTrackingStateFromPolicyImpactTrackingState(
            EnforcementPolicyImpactTrackingState trackingState)
        {
            return new MonthlyEnforcementTrackingState(
                trackingState.m_MonthIndex,
                trackingState.m_TotalPathRequestCount,
                trackingState.m_TotalActualPathCount,
                trackingState.m_PublicTransportLaneActualCount,
                trackingState.m_MidBlockCrossingActualCount,
                trackingState.m_IntersectionMovementActualCount,
                trackingState.m_TotalFineAmount,
                trackingState.m_TotalAvoidedPathCount,
                trackingState.m_PublicTransportLaneFineAmount,
                trackingState.m_MidBlockCrossingFineAmount,
                trackingState.m_IntersectionMovementFineAmount,
                trackingState.m_PublicTransportLaneAvoidedEventCount,
                trackingState.m_MidBlockCrossingAvoidedEventCount,
                trackingState.m_IntersectionMovementAvoidedEventCount,
                trackingState.m_TotalActualOrAvoidedPathCount,
                trackingState.m_PublicTransportLaneActualOrAvoidedPathCount,
                trackingState.m_MidBlockCrossingActualOrAvoidedPathCount,
                trackingState.m_IntersectionMovementActualOrAvoidedPathCount);
        }
    }
}
