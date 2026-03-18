using System.Collections.Generic;
using System.Linq;
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

        public MonthlyEnforcementTrackingState(
            long monthIndex,
            int totalPathRequestCount,
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
            int intersectionMovementAvoidedEventCount)
        {
            m_MonthIndex = monthIndex;
            m_TotalPathRequestCount = totalPathRequestCount;
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
        }
    }

    public struct MonthlyEnforcementReport
    {
        public long m_MonthIndex;
        public int m_TotalPathRequestCount;
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

        public MonthlyEnforcementReport(
            long monthIndex,
            int totalPathRequestCount,
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
            int intersectionMovementAvoidedEventCount)
        {
            m_MonthIndex = monthIndex;
            m_TotalPathRequestCount = totalPathRequestCount;
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
        }

        public int TotalViolationCount => m_PublicTransportLaneCount + m_MidBlockCrossingCount + m_IntersectionMovementCount;
        public int TotalDecisionCount => TotalViolationCount + m_TotalAvoidedPathCount;
    }

    public static class MonthlyEnforcementChirperService
    {
        private static bool s_HasTrackingState;
        private static MonthlyEnforcementTrackingState s_TrackingState;
        private static readonly List<MonthlyEnforcementReport> s_ReportHistory = new List<MonthlyEnforcementReport>();
        private static int s_ManualPreviewRequestCount;

        public static bool TryGetTrackingState(out MonthlyEnforcementTrackingState trackingState)
        {
            trackingState = s_TrackingState;
            return s_HasTrackingState;
        }

        public static IReadOnlyCollection<MonthlyEnforcementReport> GetReportHistorySnapshot()
        {
            return s_ReportHistory.ToArray();
        }

        public static void ResetPersistentData()
        {
            s_HasTrackingState = false;
            s_TrackingState = default;
            s_ReportHistory.Clear();
            s_ManualPreviewRequestCount = 0;
        }

        public static void LoadPersistentData(MonthlyEnforcementTrackingState? trackingState, IEnumerable<MonthlyEnforcementReport> reports)
        {
            ResetPersistentData();

            if (trackingState.HasValue)
            {
                s_TrackingState = trackingState.Value;
                s_HasTrackingState = true;
            }

            if (reports == null)
            {
                return;
            }

            foreach (MonthlyEnforcementReport report in reports.OrderBy(entry => entry.m_MonthIndex))
            {
                s_ReportHistory.Add(report);
            }
        }

        public static bool ResetTrackingToCurrentMonth(long currentMonthIndex)
        {
            MonthlyEnforcementTrackingState nextState = CaptureCurrentState(currentMonthIndex);
            if (s_HasTrackingState && TrackingStatesEqual(s_TrackingState, nextState))
            {
                return false;
            }

            s_TrackingState = nextState;
            s_HasTrackingState = true;
            return true;
        }

        public static bool EnsureTrackingInitialized(long currentMonthIndex)
        {
            if (!s_HasTrackingState)
            {
                s_TrackingState = CaptureCurrentState(currentMonthIndex);
                s_HasTrackingState = true;
                return true;
            }

            if (currentMonthIndex < s_TrackingState.m_MonthIndex)
            {
                s_TrackingState = CaptureCurrentState(currentMonthIndex);
                return true;
            }

            return false;
        }

        public static bool TryAdvanceMonth(long currentMonthIndex, out MonthlyEnforcementReport report)
        {
            report = default;

            if (!s_HasTrackingState)
            {
                EnsureTrackingInitialized(currentMonthIndex);
                return false;
            }

            if (currentMonthIndex <= s_TrackingState.m_MonthIndex)
            {
                if (currentMonthIndex < s_TrackingState.m_MonthIndex)
                {
                    s_TrackingState = CaptureCurrentState(currentMonthIndex);
                }

                return false;
            }

            EnforcementPolicyImpactService.PersistentTotalsSnapshot totals = EnforcementPolicyImpactService.GetPersistentTotalsSnapshot();

            report = new MonthlyEnforcementReport(
                s_TrackingState.m_MonthIndex,
                ClampToNonNegative(totals.TotalPathRequestCount - s_TrackingState.m_TotalPathRequestCount),
                ClampToNonNegative(EnforcementTelemetry.PublicTransportLaneViolationCount - s_TrackingState.m_PublicTransportLaneCount),
                ClampToNonNegative(EnforcementTelemetry.MidBlockCrossingViolationCount - s_TrackingState.m_MidBlockCrossingCount),
                ClampToNonNegative(EnforcementTelemetry.IntersectionMovementViolationCount - s_TrackingState.m_IntersectionMovementCount),
                ClampToNonNegative(EnforcementTelemetry.TotalFineAmount - s_TrackingState.m_TotalFineAmount),
                ClampToNonNegative(totals.TotalAvoidedPathCount - s_TrackingState.m_TotalAvoidedPathCount),
                ClampToNonNegative(totals.PublicTransportLaneFineAmount - s_TrackingState.m_PublicTransportLaneFineAmount),
                ClampToNonNegative(totals.MidBlockCrossingFineAmount - s_TrackingState.m_MidBlockCrossingFineAmount),
                ClampToNonNegative(totals.IntersectionMovementFineAmount - s_TrackingState.m_IntersectionMovementFineAmount),
                ClampToNonNegative(totals.PublicTransportLaneAvoidedEventCount - s_TrackingState.m_PublicTransportLaneAvoidedEventCount),
                ClampToNonNegative(totals.MidBlockCrossingAvoidedEventCount - s_TrackingState.m_MidBlockCrossingAvoidedEventCount),
                ClampToNonNegative(totals.IntersectionMovementAvoidedEventCount - s_TrackingState.m_IntersectionMovementAvoidedEventCount));

            UpsertReport(report);
            s_TrackingState = CaptureCurrentState(currentMonthIndex);
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
                snapshot.IntersectionMovementAvoidedEventCount);
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
            Mod.log.Info($"Monthly chirper manual preview direct execution unavailable. Queued request for next simulation update. reason={failureReason}");
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

        private static MonthlyEnforcementTrackingState CaptureCurrentState(long currentMonthIndex)
        {
            EnforcementPolicyImpactService.PersistentTotalsSnapshot totals = EnforcementPolicyImpactService.GetPersistentTotalsSnapshot();

            return new MonthlyEnforcementTrackingState(
                currentMonthIndex,
                totals.TotalPathRequestCount,
                EnforcementTelemetry.PublicTransportLaneViolationCount,
                EnforcementTelemetry.MidBlockCrossingViolationCount,
                EnforcementTelemetry.IntersectionMovementViolationCount,
                EnforcementTelemetry.TotalFineAmount,
                totals.TotalAvoidedPathCount,
                totals.PublicTransportLaneFineAmount,
                totals.MidBlockCrossingFineAmount,
                totals.IntersectionMovementFineAmount,
                totals.PublicTransportLaneAvoidedEventCount,
                totals.MidBlockCrossingAvoidedEventCount,
                totals.IntersectionMovementAvoidedEventCount);
        }

        private static MonthlyEnforcementReport CreateDeltaReport(MonthlyEnforcementTrackingState trackingState)
        {
            EnforcementPolicyImpactService.PersistentTotalsSnapshot totals = EnforcementPolicyImpactService.GetPersistentTotalsSnapshot();

            return new MonthlyEnforcementReport(
                trackingState.m_MonthIndex,
                ClampToNonNegative(totals.TotalPathRequestCount - trackingState.m_TotalPathRequestCount),
                ClampToNonNegative(EnforcementTelemetry.PublicTransportLaneViolationCount - trackingState.m_PublicTransportLaneCount),
                ClampToNonNegative(EnforcementTelemetry.MidBlockCrossingViolationCount - trackingState.m_MidBlockCrossingCount),
                ClampToNonNegative(EnforcementTelemetry.IntersectionMovementViolationCount - trackingState.m_IntersectionMovementCount),
                ClampToNonNegative(EnforcementTelemetry.TotalFineAmount - trackingState.m_TotalFineAmount),
                ClampToNonNegative(totals.TotalAvoidedPathCount - trackingState.m_TotalAvoidedPathCount),
                ClampToNonNegative(totals.PublicTransportLaneFineAmount - trackingState.m_PublicTransportLaneFineAmount),
                ClampToNonNegative(totals.MidBlockCrossingFineAmount - trackingState.m_MidBlockCrossingFineAmount),
                ClampToNonNegative(totals.IntersectionMovementFineAmount - trackingState.m_IntersectionMovementFineAmount),
                ClampToNonNegative(totals.PublicTransportLaneAvoidedEventCount - trackingState.m_PublicTransportLaneAvoidedEventCount),
                ClampToNonNegative(totals.MidBlockCrossingAvoidedEventCount - trackingState.m_MidBlockCrossingAvoidedEventCount),
                ClampToNonNegative(totals.IntersectionMovementAvoidedEventCount - trackingState.m_IntersectionMovementAvoidedEventCount));
        }

        private static void UpsertReport(MonthlyEnforcementReport report)
        {
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

        private static bool TrackingStatesEqual(MonthlyEnforcementTrackingState left, MonthlyEnforcementTrackingState right)
        {
            return left.m_MonthIndex == right.m_MonthIndex
                && left.m_TotalPathRequestCount == right.m_TotalPathRequestCount
                && left.m_PublicTransportLaneCount == right.m_PublicTransportLaneCount
                && left.m_MidBlockCrossingCount == right.m_MidBlockCrossingCount
                && left.m_IntersectionMovementCount == right.m_IntersectionMovementCount
                && left.m_TotalFineAmount == right.m_TotalFineAmount
                && left.m_TotalAvoidedPathCount == right.m_TotalAvoidedPathCount
                && left.m_PublicTransportLaneFineAmount == right.m_PublicTransportLaneFineAmount
                && left.m_MidBlockCrossingFineAmount == right.m_MidBlockCrossingFineAmount
                && left.m_IntersectionMovementFineAmount == right.m_IntersectionMovementFineAmount
                && left.m_PublicTransportLaneAvoidedEventCount == right.m_PublicTransportLaneAvoidedEventCount
                && left.m_MidBlockCrossingAvoidedEventCount == right.m_MidBlockCrossingAvoidedEventCount
                && left.m_IntersectionMovementAvoidedEventCount == right.m_IntersectionMovementAvoidedEventCount;
        }

        private static int ClampToNonNegative(int value)
        {
            return value < 0 ? 0 : value;
        }
    }
}
