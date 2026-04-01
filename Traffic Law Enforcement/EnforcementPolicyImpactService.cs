using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Unity.Entities;
using Game.Vehicles;
using Game;
using Game.SceneFlow;

namespace Traffic_Law_Enforcement
{
    public struct EnforcementPolicyImpactTrackingState
    {
        public long m_MonthIndex;
        public int m_TotalPathRequestCount;
        public int m_TotalActualPathCount;
        public int m_TotalAvoidedPathCount;
        public int m_TotalFineAmount;
        public int m_PublicTransportLaneActualCount;
        public int m_MidBlockCrossingActualCount;
        public int m_IntersectionMovementActualCount;
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

        public EnforcementPolicyImpactTrackingState(
            long monthIndex,
            int totalPathRequestCount,
            int totalActualPathCount,
            int totalAvoidedPathCount,
            int totalFineAmount,
            int publicTransportLaneActualCount,
            int midBlockCrossingActualCount,
            int intersectionMovementActualCount,
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
            m_TotalAvoidedPathCount = totalAvoidedPathCount;
            m_TotalFineAmount = totalFineAmount;
            m_PublicTransportLaneActualCount = publicTransportLaneActualCount;
            m_MidBlockCrossingActualCount = midBlockCrossingActualCount;
            m_IntersectionMovementActualCount = intersectionMovementActualCount;
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

    public readonly struct PathRequestEvent
    {
        public readonly long TimestampMonthTicks;
        public readonly long PathContextSequence;

        public PathRequestEvent(long timestampMonthTicks, long pathContextSequence)
        {
            TimestampMonthTicks = timestampMonthTicks;
            PathContextSequence = pathContextSequence;
        }
    }

    public readonly struct ActualViolationEvent
    {
        public readonly long TimestampMonthTicks;
        public readonly long PathContextSequence;
        public readonly string Kind;
        public readonly int FineAmount;

        public ActualViolationEvent(
            long timestampMonthTicks,
            long pathContextSequence,
            string kind,
            int fineAmount)
        {
            TimestampMonthTicks = timestampMonthTicks;
            PathContextSequence = pathContextSequence;
            Kind = kind;
            FineAmount = fineAmount;
        }
    }

    public readonly struct AvoidedRerouteEvent
    {
        public readonly long TimestampMonthTicks;
        public readonly long PathContextSequence;
        public readonly bool AvoidedPublicTransportLanePenalty;
        public readonly bool AvoidedMidBlockPenalty;
        public readonly bool AvoidedIntersectionPenalty;

        public AvoidedRerouteEvent(
            long timestampMonthTicks,
            long pathContextSequence,
            bool avoidedPublicTransportLanePenalty,
            bool avoidedMidBlockPenalty,
            bool avoidedIntersectionPenalty)
        {
            TimestampMonthTicks = timestampMonthTicks;
            PathContextSequence = pathContextSequence;
            AvoidedPublicTransportLanePenalty = avoidedPublicTransportLanePenalty;
            AvoidedMidBlockPenalty = avoidedMidBlockPenalty;
            AvoidedIntersectionPenalty = avoidedIntersectionPenalty;
        }
    }

    public readonly struct RollingWindowSnapshot
    {
        public readonly int TotalPathRequestCount;
        public readonly int TotalActualPathCount;
        public readonly int TotalAvoidedPathCount;
        public readonly int TotalActualOrAvoidedPathCount;
        public readonly int TotalFineAmount;

        public readonly int PublicTransportLaneActualCount;
        public readonly int MidBlockCrossingActualCount;
        public readonly int IntersectionMovementActualCount;

        public readonly int PublicTransportLaneFineAmount;
        public readonly int MidBlockCrossingFineAmount;
        public readonly int IntersectionMovementFineAmount;

        public readonly int PublicTransportLaneAvoidedEventCount;
        public readonly int MidBlockCrossingAvoidedEventCount;
        public readonly int IntersectionMovementAvoidedEventCount;

        public readonly int PublicTransportLaneActualOrAvoidedPathCount;
        public readonly int MidBlockCrossingActualOrAvoidedPathCount;
        public readonly int IntersectionMovementActualOrAvoidedPathCount;

        public RollingWindowSnapshot(
            int totalPathRequestCount,
            int totalActualPathCount,
            int totalAvoidedPathCount,
            int totalActualOrAvoidedPathCount,
            int totalFineAmount,
            int publicTransportLaneActualCount,
            int midBlockCrossingActualCount,
            int intersectionMovementActualCount,
            int publicTransportLaneFineAmount,
            int midBlockCrossingFineAmount,
            int intersectionMovementFineAmount,
            int publicTransportLaneAvoidedEventCount,
            int midBlockCrossingAvoidedEventCount,
            int intersectionMovementAvoidedEventCount,
            int publicTransportLaneActualOrAvoidedPathCount,
            int midBlockCrossingActualOrAvoidedPathCount,
            int intersectionMovementActualOrAvoidedPathCount)
        {
            TotalPathRequestCount = totalPathRequestCount;
            TotalActualPathCount = totalActualPathCount;
            TotalAvoidedPathCount = totalAvoidedPathCount;
            TotalActualOrAvoidedPathCount = totalActualOrAvoidedPathCount;
            TotalFineAmount = totalFineAmount;

            PublicTransportLaneActualCount = publicTransportLaneActualCount;
            MidBlockCrossingActualCount = midBlockCrossingActualCount;
            IntersectionMovementActualCount = intersectionMovementActualCount;

            PublicTransportLaneFineAmount = publicTransportLaneFineAmount;
            MidBlockCrossingFineAmount = midBlockCrossingFineAmount;
            IntersectionMovementFineAmount = intersectionMovementFineAmount;

            PublicTransportLaneAvoidedEventCount = publicTransportLaneAvoidedEventCount;
            MidBlockCrossingAvoidedEventCount = midBlockCrossingAvoidedEventCount;
            IntersectionMovementAvoidedEventCount = intersectionMovementAvoidedEventCount;

            PublicTransportLaneActualOrAvoidedPathCount = publicTransportLaneActualOrAvoidedPathCount;
            MidBlockCrossingActualOrAvoidedPathCount = midBlockCrossingActualOrAvoidedPathCount;
            IntersectionMovementActualOrAvoidedPathCount = intersectionMovementActualOrAvoidedPathCount;
        }
    }

    internal readonly struct EnforcementSummaryLogSnapshot : IEquatable<EnforcementSummaryLogSnapshot>
    {
        public readonly int Routes;
        public readonly int ViolationTotal;
        public readonly int AvoidanceTotal;
        public readonly int PublicTransportViolationCount;
        public readonly int MidBlockViolationCount;
        public readonly int IntersectionViolationCount;
        public readonly int PublicTransportAvoidanceCount;
        public readonly int MidBlockAvoidanceCount;
        public readonly int IntersectionAvoidanceCount;

        public EnforcementSummaryLogSnapshot(
            int routes,
            int violationTotal,
            int avoidanceTotal,
            int publicTransportViolationCount,
            int midBlockViolationCount,
            int intersectionViolationCount,
            int publicTransportAvoidanceCount,
            int midBlockAvoidanceCount,
            int intersectionAvoidanceCount)
        {
            Routes = routes;
            ViolationTotal = violationTotal;
            AvoidanceTotal = avoidanceTotal;
            PublicTransportViolationCount = publicTransportViolationCount;
            MidBlockViolationCount = midBlockViolationCount;
            IntersectionViolationCount = intersectionViolationCount;
            PublicTransportAvoidanceCount = publicTransportAvoidanceCount;
            MidBlockAvoidanceCount = midBlockAvoidanceCount;
            IntersectionAvoidanceCount = intersectionAvoidanceCount;
        }

        public bool Equals(EnforcementSummaryLogSnapshot other)
        {
            return Routes == other.Routes &&
                ViolationTotal == other.ViolationTotal &&
                AvoidanceTotal == other.AvoidanceTotal &&
                PublicTransportViolationCount == other.PublicTransportViolationCount &&
                MidBlockViolationCount == other.MidBlockViolationCount &&
                IntersectionViolationCount == other.IntersectionViolationCount &&
                PublicTransportAvoidanceCount == other.PublicTransportAvoidanceCount &&
                MidBlockAvoidanceCount == other.MidBlockAvoidanceCount &&
                IntersectionAvoidanceCount == other.IntersectionAvoidanceCount;
        }

        public override bool Equals(object obj)
        {
            return obj is EnforcementSummaryLogSnapshot other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = Routes;
            hash = (hash * 397) ^ ViolationTotal;
            hash = (hash * 397) ^ AvoidanceTotal;
            hash = (hash * 397) ^ PublicTransportViolationCount;
            hash = (hash * 397) ^ MidBlockViolationCount;
            hash = (hash * 397) ^ IntersectionViolationCount;
            hash = (hash * 397) ^ PublicTransportAvoidanceCount;
            hash = (hash * 397) ^ MidBlockAvoidanceCount;
            hash = (hash * 397) ^ IntersectionAvoidanceCount;
            return hash;
        }
    }

    public static class EnforcementPolicyImpactService
    {
        private static readonly long SummaryLogIntervalStopwatchTicks =
            (long)System.Diagnostics.Stopwatch.Frequency;

        public static int GetActiveVehicleRouteCount()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                return 0;
            }

            ActiveVehicleRouteCountSystem system =
                world.GetExistingSystemManaged<ActiveVehicleRouteCountSystem>() ??
                world.GetOrCreateSystemManaged<ActiveVehicleRouteCountSystem>();

            return system.GetActiveVehicleRouteCount();
        }
        public const string kLoadedSaveOnlyLocaleId = "TrafficLawEnforcement.PolicyImpact.Text.LoadedSaveOnly";
        public const string kWaitingForTimeLocaleId = "TrafficLawEnforcement.PolicyImpact.Text.WaitingForTime";
        public const string kNoDataLocaleId = "TrafficLawEnforcement.PolicyImpact.Text.NoData";
        public const string kNoteLocaleId = "TrafficLawEnforcement.PolicyImpact.Text.Note";
        public const string kStatisticsLineFormat = "TrafficLawEnforcement.PolicyImpact.Text.StatisticsLineFormat";
        public const string kTotalLabelLocaleId = "TrafficLawEnforcement.PolicyImpact.Label.Total";
        public const string kPublicTransportLaneLabelLocaleId = "TrafficLawEnforcement.PolicyImpact.Label.PublicTransportLane";
        public const string kMidBlockLabelLocaleId = "TrafficLawEnforcement.PolicyImpact.Label.MidBlock";
        public const string kIntersectionLabelLocaleId = "TrafficLawEnforcement.PolicyImpact.Label.Intersection";

        public readonly struct PersistentTotalsSnapshot
        {
            public readonly int TotalPathRequestCount;
            public readonly int TotalActualPathCount;
            public readonly int TotalAvoidedPathCount;
            public readonly int TotalFineAmount;
            public readonly int PublicTransportLaneActualCount;
            public readonly int MidBlockCrossingActualCount;
            public readonly int IntersectionMovementActualCount;
            public readonly int PublicTransportLaneFineAmount;
            public readonly int MidBlockCrossingFineAmount;
            public readonly int IntersectionMovementFineAmount;
            public readonly int PublicTransportLaneAvoidedEventCount;
            public readonly int MidBlockCrossingAvoidedEventCount;
            public readonly int IntersectionMovementAvoidedEventCount;
            public readonly int TotalActualOrAvoidedPathCount;
            public readonly int PublicTransportLaneActualOrAvoidedPathCount;
            public readonly int MidBlockCrossingActualOrAvoidedPathCount;
            public readonly int IntersectionMovementActualOrAvoidedPathCount;

            public PersistentTotalsSnapshot(
                int totalPathRequestCount,
                int totalActualPathCount,
                int totalAvoidedPathCount,
                int totalFineAmount,
                int publicTransportLaneActualCount,
                int midBlockCrossingActualCount,
                int intersectionMovementActualCount,
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
                TotalPathRequestCount = totalPathRequestCount;
                TotalActualPathCount = totalActualPathCount;
                TotalAvoidedPathCount = totalAvoidedPathCount;
                TotalFineAmount = totalFineAmount;
                PublicTransportLaneActualCount = publicTransportLaneActualCount;
                MidBlockCrossingActualCount = midBlockCrossingActualCount;
                IntersectionMovementActualCount = intersectionMovementActualCount;
                PublicTransportLaneFineAmount = publicTransportLaneFineAmount;
                MidBlockCrossingFineAmount = midBlockCrossingFineAmount;
                IntersectionMovementFineAmount = intersectionMovementFineAmount;
                PublicTransportLaneAvoidedEventCount = publicTransportLaneAvoidedEventCount;
                MidBlockCrossingAvoidedEventCount = midBlockCrossingAvoidedEventCount;
                IntersectionMovementAvoidedEventCount = intersectionMovementAvoidedEventCount;
                TotalActualOrAvoidedPathCount = totalActualOrAvoidedPathCount;
                PublicTransportLaneActualOrAvoidedPathCount = publicTransportLaneActualOrAvoidedPathCount;
                MidBlockCrossingActualOrAvoidedPathCount = midBlockCrossingActualOrAvoidedPathCount;
                IntersectionMovementActualOrAvoidedPathCount = intersectionMovementActualOrAvoidedPathCount;
            }
        }

        public readonly struct PersistentDataSnapshot
        {
            public readonly EnforcementPolicyImpactTrackingState? TrackingState;
            public readonly PersistentTotalsSnapshot Totals;
            public readonly IEnumerable<PathRequestEvent> PathRequestEvents;
            public readonly IEnumerable<ActualViolationEvent> ActualViolationEvents;
            public readonly IEnumerable<AvoidedRerouteEvent> AvoidedRerouteEvents;

            public PersistentDataSnapshot(
                EnforcementPolicyImpactTrackingState? trackingState,
                PersistentTotalsSnapshot totals,
                IEnumerable<PathRequestEvent> pathRequestEvents,
                IEnumerable<ActualViolationEvent> actualViolationEvents,
                IEnumerable<AvoidedRerouteEvent> avoidedRerouteEvents)
            {
                TrackingState = trackingState;
                Totals = totals;
                PathRequestEvents = pathRequestEvents;
                ActualViolationEvents = actualViolationEvents;
                AvoidedRerouteEvents = avoidedRerouteEvents;
            }
        }

        private static bool s_HasTrackingState;
        private static EnforcementPolicyImpactTrackingState s_TrackingState;
        private static int s_TotalPathRequestCount;
        private static int s_TotalActualPathCount;
        private static int s_TotalAvoidedPathCount;
        private static int s_TotalFineAmount;
        private static int s_PublicTransportLaneActualCount;
        private static int s_MidBlockCrossingActualCount;
        private static int s_IntersectionMovementActualCount;
        private static int s_PublicTransportLaneFineAmount;
        private static int s_MidBlockCrossingFineAmount;
        private static int s_IntersectionMovementFineAmount;
        private static int s_PublicTransportLaneAvoidedEventCount;
        private static int s_MidBlockCrossingAvoidedEventCount;
        private static int s_IntersectionMovementAvoidedEventCount;
        private static readonly List<long> s_PendingPathRequestSequencesUntilTimeInitialization = new List<long>();
        private static long s_NextPathContextSequence = 1L;
        private static readonly List<PathRequestEvent> s_PathRequestEvents = new List<PathRequestEvent>();
        private static readonly List<ActualViolationEvent> s_ActualViolationEvents = new List<ActualViolationEvent>();
        private static readonly List<AvoidedRerouteEvent> s_AvoidedRerouteEvents = new List<AvoidedRerouteEvent>();
        private static readonly ReadOnlyCollection<PathRequestEvent> s_PathRequestEventsView =
            s_PathRequestEvents.AsReadOnly();
        private static readonly ReadOnlyCollection<ActualViolationEvent> s_ActualViolationEventsView =
            s_ActualViolationEvents.AsReadOnly();
        private static readonly ReadOnlyCollection<AvoidedRerouteEvent> s_AvoidedRerouteEventsView =
            s_AvoidedRerouteEvents.AsReadOnly();
        private static readonly HashSet<long> s_RequestPathsBuffer = new HashSet<long>();
        private static readonly HashSet<long> s_TotalActualPathsBuffer = new HashSet<long>();
        private static readonly HashSet<long> s_TotalAvoidedPathsBuffer = new HashSet<long>();
        private static readonly HashSet<long> s_TotalActualOrAvoidedPathsBuffer = new HashSet<long>();
        private static readonly HashSet<long> s_PublicTransportActualPathsBuffer = new HashSet<long>();
        private static readonly HashSet<long> s_MidBlockActualPathsBuffer = new HashSet<long>();
        private static readonly HashSet<long> s_IntersectionActualPathsBuffer = new HashSet<long>();
        private static readonly HashSet<long> s_PublicTransportAvoidedPathsBuffer = new HashSet<long>();
        private static readonly HashSet<long> s_MidBlockAvoidedPathsBuffer = new HashSet<long>();
        private static readonly HashSet<long> s_IntersectionAvoidedPathsBuffer = new HashSet<long>();
        private static readonly HashSet<long> s_PublicTransportActualOrAvoidedPathsBuffer = new HashSet<long>();
        private static readonly HashSet<long> s_MidBlockActualOrAvoidedPathsBuffer = new HashSet<long>();
        private static readonly HashSet<long> s_IntersectionActualOrAvoidedPathsBuffer = new HashSet<long>();
        private static readonly Dictionary<int, long> s_ActivePathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_TotalActualPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_PublicTransportLaneActualPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_MidBlockCrossingActualPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_IntersectionMovementActualPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_TotalAvoidedPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_PublicTransportLaneAvoidedPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_MidBlockCrossingAvoidedPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_IntersectionMovementAvoidedPathContextByVehicle = new Dictionary<int, long>();
        private static int s_TotalActualOrAvoidedPathCount;
        private static int s_PublicTransportLaneActualOrAvoidedPathCount;
        private static int s_MidBlockCrossingActualOrAvoidedPathCount;
        private static int s_IntersectionMovementActualOrAvoidedPathCount;
        private static readonly Dictionary<int, long> s_TotalActualOrAvoidedPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_PublicTransportLaneActualOrAvoidedPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_MidBlockCrossingActualOrAvoidedPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_IntersectionMovementActualOrAvoidedPathContextByVehicle = new Dictionary<int, long>();
        private static bool s_RollingWindowSnapshotDirty = true;
        private static long s_LastSnapshotTimestampMonthTicks = -1L;
        private static long s_NextRollingWindowPruneTimestampMonthTicks = long.MaxValue;
        private static RollingWindowSnapshot s_CachedRollingWindowSnapshot;
        private static bool s_HasStatisticsLineCache;
        private static string s_CachedStatisticsLocaleId = string.Empty;
        private static StatisticsAvailabilityState s_CachedStatisticsAvailabilityState;
        private static long s_CachedStatisticsTimestampMonthTicks = -1L;
        private static string s_CachedTotalStatisticsLine = string.Empty;
        private static string s_CachedPublicTransportLaneStatisticsLine = string.Empty;
        private static string s_CachedMidBlockCrossingStatisticsLine = string.Empty;
        private static string s_CachedIntersectionMovementStatisticsLine = string.Empty;
        private static bool s_HasLastEmittedSummaryLogSnapshot;
        private static EnforcementSummaryLogSnapshot s_LastEmittedSummaryLogSnapshot;
        private static long s_LastSummaryLogTimestampStopwatchTicks = long.MinValue;

        private enum StatisticsAvailabilityState : byte
        {
            LoadedSaveOnly = 0,
            WaitingForTimeInitialization = 1,
            Ready = 2,
        }

        public static bool TryGetTrackingState(out EnforcementPolicyImpactTrackingState trackingState)
        {
            trackingState = s_TrackingState;
            return s_HasTrackingState;
        }

        public static IReadOnlyCollection<PathRequestEvent> GetPathRequestEventSnapshot()
        {
            return s_PathRequestEventsView;
        }

        public static IReadOnlyCollection<ActualViolationEvent> GetActualViolationEventSnapshot()
        {
            return s_ActualViolationEventsView;
        }

        public static IReadOnlyCollection<AvoidedRerouteEvent> GetAvoidedRerouteEventSnapshot()
        {
            return s_AvoidedRerouteEventsView;
        }

        private static bool RemoveExpiredPrefix<T>(List<T> entries, long cutoffTimestamp, Func<T, long> getTimestamp)
        {
            int removeCount = 0;

            for (int index = 0; index < entries.Count; index += 1)
            {
                if (getTimestamp(entries[index]) >= cutoffTimestamp)
                {
                    break;
                }

                removeCount += 1;
            }

            if (removeCount <= 0)
            {
                return false;
            }

            entries.RemoveRange(0, removeCount);
            return true;
        }

        private static void GetTimestampRange<T>(
            List<T> entries,
            Func<T, long> getTimestamp,
            out long minTimestamp,
            out long maxTimestamp)
        {
            minTimestamp = long.MaxValue;
            maxTimestamp = long.MinValue;

            for (int index = 0; index < entries.Count; index += 1)
            {
                long timestamp = getTimestamp(entries[index]);
                if (timestamp < minTimestamp)
                {
                    minTimestamp = timestamp;
                }

                if (timestamp > maxTimestamp)
                {
                    maxTimestamp = timestamp;
                }
            }

            if (entries.Count == 0)
            {
                minTimestamp = long.MinValue;
                maxTimestamp = long.MinValue;
            }
        }

        private static void TryLogPolicyImpactSummary()
        {
            if (!EnforcementLoggingPolicy.ShouldLogPolicyImpactSummary())
            {
                return;
            }

            RollingWindowSnapshot rollingWindowSnapshot = GetRollingWindowSnapshot();
            EnforcementSummaryLogSnapshot summarySnapshot =
                new EnforcementSummaryLogSnapshot(
                    rollingWindowSnapshot.TotalPathRequestCount,
                    rollingWindowSnapshot.TotalActualPathCount,
                    rollingWindowSnapshot.TotalAvoidedPathCount,
                    rollingWindowSnapshot.PublicTransportLaneActualCount,
                    rollingWindowSnapshot.MidBlockCrossingActualCount,
                    rollingWindowSnapshot.IntersectionMovementActualCount,
                    rollingWindowSnapshot.PublicTransportLaneAvoidedEventCount,
                    rollingWindowSnapshot.MidBlockCrossingAvoidedEventCount,
                    rollingWindowSnapshot.IntersectionMovementAvoidedEventCount);

            if (s_HasLastEmittedSummaryLogSnapshot &&
                summarySnapshot.Equals(s_LastEmittedSummaryLogSnapshot))
            {
                return;
            }

            long nowStopwatchTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            if (s_LastSummaryLogTimestampStopwatchTicks != long.MinValue &&
                nowStopwatchTicks - s_LastSummaryLogTimestampStopwatchTicks <
                SummaryLogIntervalStopwatchTicks)
            {
                return;
            }

            Mod.log.Info(
                $"[ENFORCEMENT_SUMMARY] routes={summarySnapshot.Routes}, " +
                $"violations={summarySnapshot.ViolationTotal} " +
                $"(PT={summarySnapshot.PublicTransportViolationCount}, " +
                $"MidBlock={summarySnapshot.MidBlockViolationCount}, " +
                $"Intersection={summarySnapshot.IntersectionViolationCount}), " +
                $"avoidances={summarySnapshot.AvoidanceTotal} " +
                $"(PT={summarySnapshot.PublicTransportAvoidanceCount}, " +
                $"MidBlock={summarySnapshot.MidBlockAvoidanceCount}, " +
                $"Intersection={summarySnapshot.IntersectionAvoidanceCount})");

            s_HasLastEmittedSummaryLogSnapshot = true;
            s_LastEmittedSummaryLogSnapshot = summarySnapshot;
            s_LastSummaryLogTimestampStopwatchTicks = nowStopwatchTicks;
        }

        public static void ResetPersistentData()
        {
            if (EnforcementLoggingPolicy.ShouldLogPolicyDiagnostics())
            {
                Mod.log.Info(
                    "[ENFORCEMENT_POLICY_STATE] phase=ResetPersistentData, " +
                    $"tracking={s_HasTrackingState}, totals=path:{s_TotalPathRequestCount},actual:{s_TotalActualPathCount},avoided:{s_TotalAvoidedPathCount},decision:{s_TotalActualOrAvoidedPathCount}, " +
                    $"events=path:{s_PathRequestEvents.Count},actual:{s_ActualViolationEvents.Count},avoided:{s_AvoidedRerouteEvents.Count}, " +
                    $"activeContexts={s_ActivePathContextByVehicle.Count}, nextPathContext={s_NextPathContextSequence}, " +
                    $"runtimeWorldGeneration={EnforcementSaveDataSystem.RuntimeWorldGeneration}, monthTicks={EnforcementGameTime.CurrentTimestampMonthTicks}");
            }

            s_HasTrackingState = false;
            s_TrackingState = default;
            s_TotalPathRequestCount = 0;
            s_TotalActualPathCount = 0;
            s_TotalAvoidedPathCount = 0;
            s_TotalFineAmount = 0;
            s_PublicTransportLaneActualCount = 0;
            s_MidBlockCrossingActualCount = 0;
            s_IntersectionMovementActualCount = 0;
            s_PublicTransportLaneFineAmount = 0;
            s_MidBlockCrossingFineAmount = 0;
            s_IntersectionMovementFineAmount = 0;
            s_PublicTransportLaneAvoidedEventCount = 0;
            s_MidBlockCrossingAvoidedEventCount = 0;
            s_IntersectionMovementAvoidedEventCount = 0;
            s_TotalActualOrAvoidedPathCount = 0;
            s_PublicTransportLaneActualOrAvoidedPathCount = 0;
            s_MidBlockCrossingActualOrAvoidedPathCount = 0;
            s_IntersectionMovementActualOrAvoidedPathCount = 0;
            s_PendingPathRequestSequencesUntilTimeInitialization.Clear();
            s_NextPathContextSequence = 1L;
            s_PathRequestEvents.Clear();
            s_ActualViolationEvents.Clear();
            s_AvoidedRerouteEvents.Clear();
            s_ActivePathContextByVehicle.Clear();
            s_TotalActualPathContextByVehicle.Clear();
            s_PublicTransportLaneActualPathContextByVehicle.Clear();
            s_MidBlockCrossingActualPathContextByVehicle.Clear();
            s_IntersectionMovementActualPathContextByVehicle.Clear();
            s_TotalAvoidedPathContextByVehicle.Clear();
            s_PublicTransportLaneAvoidedPathContextByVehicle.Clear();
            s_MidBlockCrossingAvoidedPathContextByVehicle.Clear();
            s_IntersectionMovementAvoidedPathContextByVehicle.Clear();
            s_TotalActualOrAvoidedPathContextByVehicle.Clear();
            s_PublicTransportLaneActualOrAvoidedPathContextByVehicle.Clear();
            s_MidBlockCrossingActualOrAvoidedPathContextByVehicle.Clear();
            s_IntersectionMovementActualOrAvoidedPathContextByVehicle.Clear();
            s_CachedRollingWindowSnapshot = default;
            s_LastSnapshotTimestampMonthTicks = -1L;
            s_NextRollingWindowPruneTimestampMonthTicks = long.MaxValue;
            s_RollingWindowSnapshotDirty = true;
            s_HasLastEmittedSummaryLogSnapshot = false;
            s_LastEmittedSummaryLogSnapshot = default;
            s_LastSummaryLogTimestampStopwatchTicks = long.MinValue;
        }

        public static void LoadPersistentData(PersistentDataSnapshot data)
        {
            ResetPersistentData();

            if (data.TrackingState.HasValue)
            {
                s_TrackingState = data.TrackingState.Value;
                s_HasTrackingState = true;
            }

            s_TotalPathRequestCount = data.Totals.TotalPathRequestCount;
            s_TotalActualPathCount = data.Totals.TotalActualPathCount;
            s_TotalAvoidedPathCount = data.Totals.TotalAvoidedPathCount;
            s_TotalFineAmount = data.Totals.TotalFineAmount;
            s_PublicTransportLaneActualCount = data.Totals.PublicTransportLaneActualCount;
            s_MidBlockCrossingActualCount = data.Totals.MidBlockCrossingActualCount;
            s_IntersectionMovementActualCount = data.Totals.IntersectionMovementActualCount;
            s_PublicTransportLaneFineAmount = data.Totals.PublicTransportLaneFineAmount;
            s_MidBlockCrossingFineAmount = data.Totals.MidBlockCrossingFineAmount;
            s_IntersectionMovementFineAmount = data.Totals.IntersectionMovementFineAmount;
            s_PublicTransportLaneAvoidedEventCount = data.Totals.PublicTransportLaneAvoidedEventCount;
            s_MidBlockCrossingAvoidedEventCount = data.Totals.MidBlockCrossingAvoidedEventCount;
            s_IntersectionMovementAvoidedEventCount = data.Totals.IntersectionMovementAvoidedEventCount;
            s_TotalActualOrAvoidedPathCount = data.Totals.TotalActualOrAvoidedPathCount;
            s_PublicTransportLaneActualOrAvoidedPathCount = data.Totals.PublicTransportLaneActualOrAvoidedPathCount;
            s_MidBlockCrossingActualOrAvoidedPathCount = data.Totals.MidBlockCrossingActualOrAvoidedPathCount;
            s_IntersectionMovementActualOrAvoidedPathCount = data.Totals.IntersectionMovementActualOrAvoidedPathCount;

            if (data.PathRequestEvents != null)
            {
                foreach (PathRequestEvent entry in data.PathRequestEvents)
                {
                    if (entry.TimestampMonthTicks >= 0L)
                    {
                        s_PathRequestEvents.Add(entry);
                    }
                }
            }

            if (data.ActualViolationEvents != null)
            {
                foreach (ActualViolationEvent entry in data.ActualViolationEvents)
                {
                    if (entry.TimestampMonthTicks >= 0L &&
                        !string.IsNullOrWhiteSpace(entry.Kind) &&
                        entry.FineAmount >= 0)
                    {
                        s_ActualViolationEvents.Add(entry);
                    }
                }
            }

            if (data.AvoidedRerouteEvents != null)
            {
                foreach (AvoidedRerouteEvent entry in data.AvoidedRerouteEvents)
                {
                    if (entry.TimestampMonthTicks >= 0L &&
                        (entry.AvoidedPublicTransportLanePenalty ||
                        entry.AvoidedMidBlockPenalty ||
                        entry.AvoidedIntersectionPenalty))
                    {
                        s_AvoidedRerouteEvents.Add(entry);
                    }
                }
            }

            long maxLoadedPathContextSequence = 0L;

            for (int index = 0; index < s_PathRequestEvents.Count; index += 1)
            {
                long sequence = s_PathRequestEvents[index].PathContextSequence;
                if (sequence > maxLoadedPathContextSequence)
                {
                    maxLoadedPathContextSequence = sequence;
                }
            }

            for (int index = 0; index < s_ActualViolationEvents.Count; index += 1)
            {
                long sequence = s_ActualViolationEvents[index].PathContextSequence;
                if (sequence > maxLoadedPathContextSequence)
                {
                    maxLoadedPathContextSequence = sequence;
                }
            }

            for (int index = 0; index < s_AvoidedRerouteEvents.Count; index += 1)
            {
                long sequence = s_AvoidedRerouteEvents[index].PathContextSequence;
                if (sequence > maxLoadedPathContextSequence)
                {
                    maxLoadedPathContextSequence = sequence;
                }
            }

            s_NextPathContextSequence = System.Math.Max(1L, maxLoadedPathContextSequence + 1L);

            UpdateRollingWindowData();
            s_CachedRollingWindowSnapshot = default;
            s_LastSnapshotTimestampMonthTicks = -1L;
            s_RollingWindowSnapshotDirty = true;
            s_HasLastEmittedSummaryLogSnapshot = false;
            s_LastEmittedSummaryLogSnapshot = default;
            s_LastSummaryLogTimestampStopwatchTicks = long.MinValue;

            if (EnforcementLoggingPolicy.ShouldLogPolicyDiagnostics())
            {
                string trackingSummary =
                    data.TrackingState.HasValue
                        ? $"tracking=True, trackingMonth={data.TrackingState.Value.m_MonthIndex}, trackingTotals=path:{data.TrackingState.Value.m_TotalPathRequestCount},actual:{data.TrackingState.Value.m_TotalActualPathCount},avoided:{data.TrackingState.Value.m_TotalAvoidedPathCount},decision:{data.TrackingState.Value.m_TotalActualOrAvoidedPathCount},fine:{data.TrackingState.Value.m_TotalFineAmount}"
                        : "tracking=False, trackingMonth=null";
                bool suspiciousEmptyTrackingBaseline =
                    data.TrackingState.HasValue &&
                    data.TrackingState.Value.m_TotalPathRequestCount == 0 &&
                    data.TrackingState.Value.m_TotalActualPathCount == 0 &&
                    data.TrackingState.Value.m_TotalAvoidedPathCount == 0 &&
                    data.TrackingState.Value.m_TotalActualOrAvoidedPathCount == 0 &&
                    (data.Totals.TotalPathRequestCount > 0 ||
                    data.Totals.TotalActualPathCount > 0 ||
                    data.Totals.TotalAvoidedPathCount > 0 ||
                    data.Totals.TotalActualOrAvoidedPathCount > 0 ||
                    data.Totals.TotalFineAmount > 0);

                Mod.log.Info(
                    "[ENFORCEMENT_POLICY_STATE] phase=LoadPersistentData, " +
                    $"{trackingSummary}, suspiciousEmptyTrackingBaseline={suspiciousEmptyTrackingBaseline}, totals=path:{s_TotalPathRequestCount},actual:{s_TotalActualPathCount},avoided:{s_TotalAvoidedPathCount},decision:{s_TotalActualOrAvoidedPathCount}, " +
                    $"events=path:{s_PathRequestEvents.Count},actual:{s_ActualViolationEvents.Count},avoided:{s_AvoidedRerouteEvents.Count}, " +
                    $"activeContexts={s_ActivePathContextByVehicle.Count}, nextPathContext={s_NextPathContextSequence}, " +
                    $"runtimeWorldGeneration={EnforcementSaveDataSystem.RuntimeWorldGeneration}, monthTicks={EnforcementGameTime.CurrentTimestampMonthTicks}");
            }
        }

        public static PersistentTotalsSnapshot GetPersistentTotalsSnapshot()
        {
            return new PersistentTotalsSnapshot(
                s_TotalPathRequestCount,
                s_TotalActualPathCount,
                s_TotalAvoidedPathCount,
                s_TotalFineAmount,
                s_PublicTransportLaneActualCount,
                s_MidBlockCrossingActualCount,
                s_IntersectionMovementActualCount,
                s_PublicTransportLaneFineAmount,
                s_MidBlockCrossingFineAmount,
                s_IntersectionMovementFineAmount,
                s_PublicTransportLaneAvoidedEventCount,
                s_MidBlockCrossingAvoidedEventCount,
                s_IntersectionMovementAvoidedEventCount,
                s_TotalActualOrAvoidedPathCount,
                s_PublicTransportLaneActualOrAvoidedPathCount,
                s_MidBlockCrossingActualOrAvoidedPathCount,
                s_IntersectionMovementActualOrAvoidedPathCount);
        }

        public static bool ResetTrackingToCurrentMonth(long currentMonthIndex)
        {
            EnforcementPolicyImpactTrackingState nextState =
                CaptureCurrentState(currentMonthIndex);
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

        public static bool TryAdvanceMonth(
            long currentMonthIndex,
            out MonthlyEnforcementReport report)
        {
            report = default;

            if (!s_HasTrackingState)
            {
                EnsureTrackingInitialized(currentMonthIndex);
                return false;
            }

            _ = TryRepairSuspiciousTrackingBaseline(currentMonthIndex);

            if (currentMonthIndex <= s_TrackingState.m_MonthIndex)
            {
                if (currentMonthIndex < s_TrackingState.m_MonthIndex)
                {
                    s_TrackingState = CaptureCurrentState(currentMonthIndex);
                }

                return false;
            }

            if (EnforcementLoggingPolicy.ShouldLogPolicyDiagnostics() &&
                IsSuspiciousEmptyTrackingBaseline(s_TrackingState))
            {
                Mod.log.Info(
                    "[ENFORCEMENT_POLICY_STATE] " +
                    $"phase=AdvanceMonthUsingSuspiciousTrackingBaseline, trackingMonth={s_TrackingState.m_MonthIndex}, currentMonth={currentMonthIndex}, " +
                    $"trackingTotals=path:{s_TrackingState.m_TotalPathRequestCount},actual:{s_TrackingState.m_TotalActualPathCount},avoided:{s_TrackingState.m_TotalAvoidedPathCount},decision:{s_TrackingState.m_TotalActualOrAvoidedPathCount}, " +
                    $"currentTotals=path:{s_TotalPathRequestCount},actual:{s_TotalActualPathCount},avoided:{s_TotalAvoidedPathCount},decision:{s_TotalActualOrAvoidedPathCount}, fine={s_TotalFineAmount}");
            }

            report = CreateCompletedMonthReport(s_TrackingState);
            s_TrackingState = CaptureCurrentState(currentMonthIndex);
            s_HasTrackingState = true;
            return true;
        }

        public static void RecordPathRequest(int vehicleId)
        {
            RecordPathRequestInternal(vehicleId);
        }

        public static void UpdateTrackingForCurrentMonth()
        {
            if (!EnforcementGameTime.IsInitialized)
            {
                return;
            }

            UpdateRollingWindowData();
            TryLogPolicyImpactSummary();

            long currentMonthIndex = EnforcementGameTime.GetMonthIndex(EnforcementGameTime.CurrentTimestampMonthTicks);
            _ = TryRepairSuspiciousTrackingBaseline(currentMonthIndex);
            _ = EnsureTrackingInitialized(currentMonthIndex);
        }

        public static void PrepareTrackingStateForPersistence()
        {
            if (!EnforcementGameTime.IsInitialized)
            {
                return;
            }

            UpdateRollingWindowData();

            long currentMonthIndex =
                EnforcementGameTime.GetMonthIndex(
                    EnforcementGameTime.CurrentTimestampMonthTicks);
            _ = TryRepairSuspiciousTrackingBaseline(currentMonthIndex);
            _ = EnsureTrackingInitialized(currentMonthIndex);
        }

        public static void RecordActualViolation(string kind, int fineAmount, int vehicleId)
        {
            s_TotalFineAmount += fineAmount;

            bool countsTowardTotalPath = false;
            bool countsTowardKindPath = false;
            bool countsTowardTotalActualOrAvoidedPath = false;
            bool countsTowardKindActualOrAvoidedPath = false;
            long pathContextSequence = 0L;

            if (!string.IsNullOrWhiteSpace(kind) &&
                TryGetActivePathContext(vehicleId, out pathContextSequence))
            {
                countsTowardTotalPath =
                    TryMarkPathContextAsCounted(
                        s_TotalActualPathContextByVehicle,
                        vehicleId,
                        pathContextSequence);

                Dictionary<int, long> kindPathContextMap = GetKindActualPathContextMap(kind);
                countsTowardKindPath =
                    kindPathContextMap != null &&
                    TryMarkPathContextAsCounted(
                        kindPathContextMap,
                        vehicleId,
                        pathContextSequence);

                countsTowardTotalActualOrAvoidedPath =
                    TryMarkPathContextAsCounted(
                        s_TotalActualOrAvoidedPathContextByVehicle,
                        vehicleId,
                        pathContextSequence);

                Dictionary<int, long> kindActualOrAvoidedPathContextMap =
                    GetKindActualOrAvoidedPathContextMap(kind);

                countsTowardKindActualOrAvoidedPath =
                    kindActualOrAvoidedPathContextMap != null &&
                    TryMarkPathContextAsCounted(
                        kindActualOrAvoidedPathContextMap,
                        vehicleId,
                        pathContextSequence);
            }

            if (countsTowardTotalPath)
            {
                s_TotalActualPathCount += 1;
            }

            if (countsTowardTotalActualOrAvoidedPath)
            {
                s_TotalActualOrAvoidedPathCount += 1;
            }

            if (EnforcementGameTime.IsInitialized &&
                EnforcementGameTime.CurrentTimestampMonthTicks >= 0L &&
                !string.IsNullOrWhiteSpace(kind) &&
                pathContextSequence > 0L)
            {
                s_ActualViolationEvents.Add(
                    new ActualViolationEvent(
                        EnforcementGameTime.CurrentTimestampMonthTicks,
                        pathContextSequence,
                        kind,
                        fineAmount));

                s_RollingWindowSnapshotDirty = true;
            }

            switch (kind)
            {
                case EnforcementKinds.PublicTransportLane:
                    if (countsTowardKindPath)
                    {
                        s_PublicTransportLaneActualCount += 1;
                    }
                    if (countsTowardKindActualOrAvoidedPath)
                    {
                        s_PublicTransportLaneActualOrAvoidedPathCount += 1;
                    }
                    s_PublicTransportLaneFineAmount += fineAmount;
                    break;

                case EnforcementKinds.MidBlockCrossing:
                    if (countsTowardKindPath)
                    {
                        s_MidBlockCrossingActualCount += 1;
                    }
                    if (countsTowardKindActualOrAvoidedPath)
                    {
                        s_MidBlockCrossingActualOrAvoidedPathCount += 1;
                    }
                    s_MidBlockCrossingFineAmount += fineAmount;
                    break;

                case EnforcementKinds.IntersectionMovement:
                    if (countsTowardKindPath)
                    {
                        s_IntersectionMovementActualCount += 1;
                    }
                    if (countsTowardKindActualOrAvoidedPath)
                    {
                        s_IntersectionMovementActualOrAvoidedPathCount += 1;
                    }
                    s_IntersectionMovementFineAmount += fineAmount;
                    break;
            }
        }

        public static void RecordAvoidedReroute(
            int vehicleId,
            bool avoidedPublicTransportLanePenalty,
            bool avoidedMidBlockPenalty,
            bool avoidedIntersectionPenalty)
        {
            if (!avoidedPublicTransportLanePenalty &&
                !avoidedMidBlockPenalty &&
                !avoidedIntersectionPenalty)
            {
                return;
            }

            if (!TryGetActivePathContext(vehicleId, out long pathContextSequence))
            {
                return;
            }

            bool countsTowardTotalPath =
                TryMarkPathContextAsCounted(
                    s_TotalAvoidedPathContextByVehicle,
                    vehicleId,
                    pathContextSequence);

            bool countsTowardPublicTransportLanePath =
                avoidedPublicTransportLanePenalty &&
                TryMarkPathContextAsCounted(
                    s_PublicTransportLaneAvoidedPathContextByVehicle,
                    vehicleId,
                    pathContextSequence);

            bool countsTowardMidBlockPath =
                avoidedMidBlockPenalty &&
                TryMarkPathContextAsCounted(
                    s_MidBlockCrossingAvoidedPathContextByVehicle,
                    vehicleId,
                    pathContextSequence);

            bool countsTowardIntersectionPath =
                avoidedIntersectionPenalty &&
                TryMarkPathContextAsCounted(
                    s_IntersectionMovementAvoidedPathContextByVehicle,
                    vehicleId,
                    pathContextSequence);

            bool countsTowardTotalActualOrAvoidedPath =
                TryMarkPathContextAsCounted(
                    s_TotalActualOrAvoidedPathContextByVehicle,
                    vehicleId,
                    pathContextSequence);

            bool countsTowardPublicTransportLaneActualOrAvoidedPath =
                avoidedPublicTransportLanePenalty &&
                TryMarkPathContextAsCounted(
                    s_PublicTransportLaneActualOrAvoidedPathContextByVehicle,
                    vehicleId,
                    pathContextSequence);

            bool countsTowardMidBlockActualOrAvoidedPath =
                avoidedMidBlockPenalty &&
                TryMarkPathContextAsCounted(
                    s_MidBlockCrossingActualOrAvoidedPathContextByVehicle,
                    vehicleId,
                    pathContextSequence);

            bool countsTowardIntersectionActualOrAvoidedPath =
                avoidedIntersectionPenalty &&
                TryMarkPathContextAsCounted(
                    s_IntersectionMovementActualOrAvoidedPathContextByVehicle,
                    vehicleId,
                    pathContextSequence);

            if (countsTowardTotalPath)
            {
                s_TotalAvoidedPathCount += 1;
            }

            if (countsTowardTotalActualOrAvoidedPath)
            {
                s_TotalActualOrAvoidedPathCount += 1;
            }

            if (EnforcementGameTime.IsInitialized &&
                EnforcementGameTime.CurrentTimestampMonthTicks >= 0L &&
                pathContextSequence > 0L)
            {
                s_AvoidedRerouteEvents.Add(
                    new AvoidedRerouteEvent(
                        EnforcementGameTime.CurrentTimestampMonthTicks,
                        pathContextSequence,
                        avoidedPublicTransportLanePenalty,
                        avoidedMidBlockPenalty,
                        avoidedIntersectionPenalty));

                s_RollingWindowSnapshotDirty = true;
            }

            if (countsTowardPublicTransportLanePath)
            {
                s_PublicTransportLaneAvoidedEventCount += 1;
            }

            if (countsTowardPublicTransportLaneActualOrAvoidedPath)
            {
                s_PublicTransportLaneActualOrAvoidedPathCount += 1;
            }

            if (countsTowardMidBlockPath)
            {
                s_MidBlockCrossingAvoidedEventCount += 1;
            }

            if (countsTowardMidBlockActualOrAvoidedPath)
            {
                s_MidBlockCrossingActualOrAvoidedPathCount += 1;
            }

            if (countsTowardIntersectionPath)
            {
                s_IntersectionMovementAvoidedEventCount += 1;
            }

            if (countsTowardIntersectionActualOrAvoidedPath)
            {
                s_IntersectionMovementActualOrAvoidedPathCount += 1;
            }
        }

        public static string GetTotalStatisticsLine()
        {
            EnsureStatisticsLineCache();
            return s_CachedTotalStatisticsLine;
        }

        public static string GetPublicTransportLaneStatisticsLine()
        {
            EnsureStatisticsLineCache();
            return s_CachedPublicTransportLaneStatisticsLine;
        }

        public static string GetMidBlockCrossingStatisticsLine()
        {
            EnsureStatisticsLineCache();
            return s_CachedMidBlockCrossingStatisticsLine;
        }
        public static string GetIntersectionMovementStatisticsLine()
        {
            EnsureStatisticsLineCache();
            return s_CachedIntersectionMovementStatisticsLine;
        }

        private static void EnsureStatisticsLineCache()
        {
            string activeLocaleId = GetActiveLocaleId();
            StatisticsAvailabilityState availabilityState =
                GetStatisticsAvailabilityState();
            long timestampMonthTicks =
                availabilityState == StatisticsAvailabilityState.Ready
                    ? EnforcementGameTime.CurrentTimestampMonthTicks
                    : -1L;
            bool rollingWindowCacheInvalid =
                availabilityState == StatisticsAvailabilityState.Ready &&
                s_RollingWindowSnapshotDirty;

            if (s_HasStatisticsLineCache &&
                !rollingWindowCacheInvalid &&
                activeLocaleId == s_CachedStatisticsLocaleId &&
                availabilityState == s_CachedStatisticsAvailabilityState &&
                timestampMonthTicks == s_CachedStatisticsTimestampMonthTicks)
            {
                return;
            }

            if (availabilityState != StatisticsAvailabilityState.Ready)
            {
                string unavailableText =
                    availabilityState == StatisticsAvailabilityState.LoadedSaveOnly
                        ? LocalizeText(
                            kLoadedSaveOnlyLocaleId,
                            "Available only in a loaded save.")
                        : LocalizeText(
                            kWaitingForTimeLocaleId,
                            "Waiting for in-game time initialization.");

                s_CachedTotalStatisticsLine = unavailableText;
                s_CachedPublicTransportLaneStatisticsLine = unavailableText;
                s_CachedMidBlockCrossingStatisticsLine = unavailableText;
                s_CachedIntersectionMovementStatisticsLine = unavailableText;
            }
            else
            {
                RollingWindowSnapshot snapshot = GetRollingWindowSnapshot();

                s_CachedTotalStatisticsLine =
                    BuildStatisticsLine(
                        snapshot,
                        snapshot.TotalActualPathCount,
                        snapshot.TotalActualOrAvoidedPathCount,
                        snapshot.TotalFineAmount);
                s_CachedPublicTransportLaneStatisticsLine =
                    BuildStatisticsLine(
                        snapshot,
                        snapshot.PublicTransportLaneActualCount,
                        snapshot.PublicTransportLaneActualOrAvoidedPathCount,
                        snapshot.PublicTransportLaneFineAmount);
                s_CachedMidBlockCrossingStatisticsLine =
                    BuildStatisticsLine(
                        snapshot,
                        snapshot.MidBlockCrossingActualCount,
                        snapshot.MidBlockCrossingActualOrAvoidedPathCount,
                        snapshot.MidBlockCrossingFineAmount);
                s_CachedIntersectionMovementStatisticsLine =
                    BuildStatisticsLine(
                        snapshot,
                        snapshot.IntersectionMovementActualCount,
                        snapshot.IntersectionMovementActualOrAvoidedPathCount,
                        snapshot.IntersectionMovementFineAmount);
            }

            s_HasStatisticsLineCache = true;
            s_CachedStatisticsLocaleId = activeLocaleId;
            s_CachedStatisticsAvailabilityState = availabilityState;
            s_CachedStatisticsTimestampMonthTicks = timestampMonthTicks;
        }

        private static StatisticsAvailabilityState GetStatisticsAvailabilityState()
        {
            if (!IsGameplayContextAvailable())
            {
                return StatisticsAvailabilityState.LoadedSaveOnly;
            }

            return EnforcementGameTime.IsInitialized
                ? StatisticsAvailabilityState.Ready
                : StatisticsAvailabilityState.WaitingForTimeInitialization;
        }

        private static string BuildStatisticsLine(
            RollingWindowSnapshot snapshot,
            int actualCount,
            int actualOrAvoidedPathCount,
            int fineAmount)
        {
            string violationRate =
                FormatRatio(actualCount, snapshot.TotalPathRequestCount);

            string suppressionFailureRate =
                FormatRatio(actualCount, actualOrAvoidedPathCount);

            string fines = FormatMoney(fineAmount);

            string lineFormat = LocalizeText(
                kStatisticsLineFormat,
                "violation rate {0}, suppression failure rate {1}, fines {2}₡.");

            return string.Format(
                lineFormat,
                violationRate,
                suppressionFailureRate,
                fines);
        }

        public static RollingWindowSnapshot GetRollingWindowSnapshot()
        {
            if (!EnforcementGameTime.IsInitialized)
            {
                return default;
            }

            UpdateRollingWindowData();

            long currentTimestampMonthTicks = EnforcementGameTime.CurrentTimestampMonthTicks;
            if (!s_RollingWindowSnapshotDirty &&
                s_LastSnapshotTimestampMonthTicks == currentTimestampMonthTicks)
            {
                return s_CachedRollingWindowSnapshot;
            }

            s_CachedRollingWindowSnapshot = BuildRollingWindowSnapshot();
            s_LastSnapshotTimestampMonthTicks = currentTimestampMonthTicks;
            s_RollingWindowSnapshotDirty = false;
            return s_CachedRollingWindowSnapshot;
        }

        private static RollingWindowSnapshot BuildRollingWindowSnapshot()
        {
            if (!EnforcementGameTime.IsInitialized)
            {
                return default;
            }

            s_NextRollingWindowPruneTimestampMonthTicks =
                GetNextRollingWindowPruneTimestampMonthTicks();

            return BuildSnapshotForEventWindow(
                long.MinValue,
                long.MaxValue);
        }

        private static RollingWindowSnapshot BuildSnapshotForEventWindow(
            long startInclusiveTimestampMonthTicks,
            long endExclusiveTimestampMonthTicks)
        {
            bool hasEndExclusive = endExclusiveTimestampMonthTicks != long.MaxValue;

            int requestEventCount = s_PathRequestEvents.Count;
            int actualViolationEventCount = s_ActualViolationEvents.Count;
            int avoidedRerouteEventCount = s_AvoidedRerouteEvents.Count;
            int combinedEventCapacity = actualViolationEventCount + avoidedRerouteEventCount;

            HashSet<long> requestPaths = PreparePathSet(s_RequestPathsBuffer);
            HashSet<long> totalActualPaths = PreparePathSet(s_TotalActualPathsBuffer);
            HashSet<long> totalAvoidedPaths = PreparePathSet(s_TotalAvoidedPathsBuffer);
            HashSet<long> totalActualOrAvoidedPaths = PreparePathSet(s_TotalActualOrAvoidedPathsBuffer);

            HashSet<long> publicTransportActualPaths = PreparePathSet(s_PublicTransportActualPathsBuffer);
            HashSet<long> midBlockActualPaths = PreparePathSet(s_MidBlockActualPathsBuffer);
            HashSet<long> intersectionActualPaths = PreparePathSet(s_IntersectionActualPathsBuffer);

            HashSet<long> publicTransportAvoidedPaths = PreparePathSet(s_PublicTransportAvoidedPathsBuffer);
            HashSet<long> midBlockAvoidedPaths = PreparePathSet(s_MidBlockAvoidedPathsBuffer);
            HashSet<long> intersectionAvoidedPaths = PreparePathSet(s_IntersectionAvoidedPathsBuffer);

            HashSet<long> publicTransportActualOrAvoidedPaths = PreparePathSet(s_PublicTransportActualOrAvoidedPathsBuffer);
            HashSet<long> midBlockActualOrAvoidedPaths = PreparePathSet(s_MidBlockActualOrAvoidedPathsBuffer);
            HashSet<long> intersectionActualOrAvoidedPaths = PreparePathSet(s_IntersectionActualOrAvoidedPathsBuffer);

            int legacyRequestPathCount = 0;
            int legacyTotalActualPathCount = 0;
            int legacyTotalAvoidedPathCount = 0;
            int legacyPublicTransportActualCount = 0;
            int legacyMidBlockActualCount = 0;
            int legacyIntersectionActualCount = 0;
            int legacyPublicTransportAvoidedCount = 0;
            int legacyMidBlockAvoidedCount = 0;
            int legacyIntersectionAvoidedCount = 0;

            int totalFineAmount = 0;
            int publicTransportLaneFineAmount = 0;
            int midBlockCrossingFineAmount = 0;
            int intersectionMovementFineAmount = 0;

            for (int index = 0; index < s_PathRequestEvents.Count; index += 1)
            {
                PathRequestEvent entry = s_PathRequestEvents[index];
                if (entry.TimestampMonthTicks < startInclusiveTimestampMonthTicks ||
                    (hasEndExclusive &&
                    entry.TimestampMonthTicks >= endExclusiveTimestampMonthTicks))
                {
                    continue;
                }

                if (entry.PathContextSequence > 0L)
                {
                    requestPaths.Add(entry.PathContextSequence);
                }
                else
                {
                    legacyRequestPathCount += 1;
                }
            }

            for (int index = 0; index < s_ActualViolationEvents.Count; index += 1)
            {
                ActualViolationEvent entry = s_ActualViolationEvents[index];
                if (entry.TimestampMonthTicks < startInclusiveTimestampMonthTicks ||
                    (hasEndExclusive &&
                    entry.TimestampMonthTicks >= endExclusiveTimestampMonthTicks))
                {
                    continue;
                }

                totalFineAmount += entry.FineAmount;

                if (entry.PathContextSequence <= 0L)
                {
                    legacyTotalActualPathCount += 1;

                    switch (entry.Kind)
                    {
                        case EnforcementKinds.PublicTransportLane:
                            legacyPublicTransportActualCount += 1;
                            publicTransportLaneFineAmount += entry.FineAmount;
                            break;

                        case EnforcementKinds.MidBlockCrossing:
                            legacyMidBlockActualCount += 1;
                            midBlockCrossingFineAmount += entry.FineAmount;
                            break;

                        case EnforcementKinds.IntersectionMovement:
                            legacyIntersectionActualCount += 1;
                            intersectionMovementFineAmount += entry.FineAmount;
                            break;
                    }

                    continue;
                }

                long pathContextSequence = entry.PathContextSequence;
                totalActualPaths.Add(pathContextSequence);
                totalActualOrAvoidedPaths.Add(pathContextSequence);

                switch (entry.Kind)
                {
                    case EnforcementKinds.PublicTransportLane:
                        publicTransportActualPaths.Add(pathContextSequence);
                        publicTransportActualOrAvoidedPaths.Add(pathContextSequence);
                        publicTransportLaneFineAmount += entry.FineAmount;
                        break;

                    case EnforcementKinds.MidBlockCrossing:
                        midBlockActualPaths.Add(pathContextSequence);
                        midBlockActualOrAvoidedPaths.Add(pathContextSequence);
                        midBlockCrossingFineAmount += entry.FineAmount;
                        break;

                    case EnforcementKinds.IntersectionMovement:
                        intersectionActualPaths.Add(pathContextSequence);
                        intersectionActualOrAvoidedPaths.Add(pathContextSequence);
                        intersectionMovementFineAmount += entry.FineAmount;
                        break;
                }
            }

            for (int index = 0; index < s_AvoidedRerouteEvents.Count; index += 1)
            {
                AvoidedRerouteEvent entry = s_AvoidedRerouteEvents[index];
                if (entry.TimestampMonthTicks < startInclusiveTimestampMonthTicks ||
                    (hasEndExclusive &&
                    entry.TimestampMonthTicks >= endExclusiveTimestampMonthTicks))
                {
                    continue;
                }

                if (entry.PathContextSequence <= 0L)
                {
                    legacyTotalAvoidedPathCount += 1;

                    if (entry.AvoidedPublicTransportLanePenalty)
                    {
                        legacyPublicTransportAvoidedCount += 1;
                    }

                    if (entry.AvoidedMidBlockPenalty)
                    {
                        legacyMidBlockAvoidedCount += 1;
                    }

                    if (entry.AvoidedIntersectionPenalty)
                    {
                        legacyIntersectionAvoidedCount += 1;
                    }

                    continue;
                }

                long pathContextSequence = entry.PathContextSequence;
                totalAvoidedPaths.Add(pathContextSequence);
                totalActualOrAvoidedPaths.Add(pathContextSequence);

                if (entry.AvoidedPublicTransportLanePenalty)
                {
                    publicTransportAvoidedPaths.Add(pathContextSequence);
                    publicTransportActualOrAvoidedPaths.Add(pathContextSequence);
                }

                if (entry.AvoidedMidBlockPenalty)
                {
                    midBlockAvoidedPaths.Add(pathContextSequence);
                    midBlockActualOrAvoidedPaths.Add(pathContextSequence);
                }

                if (entry.AvoidedIntersectionPenalty)
                {
                    intersectionAvoidedPaths.Add(pathContextSequence);
                    intersectionActualOrAvoidedPaths.Add(pathContextSequence);
                }
            }

            int totalPathRequestCount = requestPaths.Count + legacyRequestPathCount;
            int totalActualPathCount = totalActualPaths.Count + legacyTotalActualPathCount;
            int totalAvoidedPathCount = totalAvoidedPaths.Count + legacyTotalAvoidedPathCount;
            int totalActualOrAvoidedPathCount =
                totalActualOrAvoidedPaths.Count +
                legacyTotalActualPathCount +
                legacyTotalAvoidedPathCount;

            int publicTransportLaneActualCount =
                publicTransportActualPaths.Count + legacyPublicTransportActualCount;
            int midBlockCrossingActualCount =
                midBlockActualPaths.Count + legacyMidBlockActualCount;
            int intersectionMovementActualCount =
                intersectionActualPaths.Count + legacyIntersectionActualCount;

            int publicTransportLaneAvoidedEventCount =
                publicTransportAvoidedPaths.Count + legacyPublicTransportAvoidedCount;
            int midBlockCrossingAvoidedEventCount =
                midBlockAvoidedPaths.Count + legacyMidBlockAvoidedCount;
            int intersectionMovementAvoidedEventCount =
                intersectionAvoidedPaths.Count + legacyIntersectionAvoidedCount;

            int publicTransportLaneActualOrAvoidedPathCount =
                publicTransportActualOrAvoidedPaths.Count +
                legacyPublicTransportActualCount +
                legacyPublicTransportAvoidedCount;

            int midBlockCrossingActualOrAvoidedPathCount =
                midBlockActualOrAvoidedPaths.Count +
                legacyMidBlockActualCount +
                legacyMidBlockAvoidedCount;

            int intersectionMovementActualOrAvoidedPathCount =
                intersectionActualOrAvoidedPaths.Count +
                legacyIntersectionActualCount +
                legacyIntersectionAvoidedCount;

            return new RollingWindowSnapshot(
                totalPathRequestCount,
                totalActualPathCount,
                totalAvoidedPathCount,
                totalActualOrAvoidedPathCount,
                totalFineAmount,
                publicTransportLaneActualCount,
                midBlockCrossingActualCount,
                intersectionMovementActualCount,
                publicTransportLaneFineAmount,
                midBlockCrossingFineAmount,
                intersectionMovementFineAmount,
                publicTransportLaneAvoidedEventCount,
                midBlockCrossingAvoidedEventCount,
                intersectionMovementAvoidedEventCount,
                publicTransportLaneActualOrAvoidedPathCount,
                midBlockCrossingActualOrAvoidedPathCount,
                intersectionMovementActualOrAvoidedPathCount);
        }

        private static bool TryRepairSuspiciousTrackingBaseline(long currentMonthIndex)
        {
            if (!s_HasTrackingState ||
                !EnforcementGameTime.IsInitialized ||
                !IsSuspiciousEmptyTrackingBaseline(s_TrackingState))
            {
                return false;
            }

            if (currentMonthIndex < s_TrackingState.m_MonthIndex)
            {
                return false;
            }

            long monthOffset = currentMonthIndex - s_TrackingState.m_MonthIndex;
            if (monthOffset > 1L)
            {
                if (EnforcementLoggingPolicy.ShouldLogPolicyDiagnostics())
                {
                    Mod.log.Info(
                        "[ENFORCEMENT_POLICY_STATE] " +
                        $"phase=RepairSuspiciousTrackingBaselineSkipped, reason=trackingMonthTooOld, trackingMonth={s_TrackingState.m_MonthIndex}, currentMonth={currentMonthIndex}");
                }

                return false;
            }

            long trackingMonthStartTimestamp =
                EnforcementGameTime.GetMonthTickAtMonthIndex(
                    s_TrackingState.m_MonthIndex);
            long trackingMonthEndTimestamp =
                EnforcementGameTime.GetMonthTickAtMonthIndex(
                    s_TrackingState.m_MonthIndex + 1L);
            long currentTimestampMonthTicks =
                EnforcementGameTime.CurrentTimestampMonthTicks;

            if (monthOffset == 1L &&
                currentTimestampMonthTicks > trackingMonthEndTimestamp)
            {
                if (EnforcementLoggingPolicy.ShouldLogPolicyDiagnostics())
                {
                    Mod.log.Info(
                        "[ENFORCEMENT_POLICY_STATE] " +
                        $"phase=RepairSuspiciousTrackingBaselineSkipped, reason=trackedMonthPartiallyPruned, trackingMonth={s_TrackingState.m_MonthIndex}, currentMonth={currentMonthIndex}, " +
                        $"currentMonthTicks={currentTimestampMonthTicks}, trackedMonthEnd={trackingMonthEndTimestamp}");
                }

                return false;
            }

            RollingWindowSnapshot trackedMonthSnapshot =
                BuildSnapshotForEventWindow(
                    trackingMonthStartTimestamp,
                    trackingMonthEndTimestamp);
            EnforcementPolicyImpactTrackingState repairedTrackingState =
                CreateTrackingStateFromTrackedMonthDelta(
                    s_TrackingState.m_MonthIndex,
                    trackedMonthSnapshot);

            if (TrackingStatesEqual(s_TrackingState, repairedTrackingState))
            {
                return false;
            }

            s_TrackingState = repairedTrackingState;
            s_HasTrackingState = true;

            if (EnforcementLoggingPolicy.ShouldLogPolicyDiagnostics())
            {
                Mod.log.Info(
                    "[ENFORCEMENT_POLICY_STATE] " +
                    $"phase=RepairSuspiciousTrackingBaseline, trackingMonth={s_TrackingState.m_MonthIndex}, currentMonth={currentMonthIndex}, " +
                    $"windowStart={trackingMonthStartTimestamp}, windowEnd={trackingMonthEndTimestamp}, " +
                    $"monthDelta=path:{trackedMonthSnapshot.TotalPathRequestCount},actual:{trackedMonthSnapshot.TotalActualPathCount},avoided:{trackedMonthSnapshot.TotalAvoidedPathCount},decision:{trackedMonthSnapshot.TotalActualOrAvoidedPathCount},fine:{trackedMonthSnapshot.TotalFineAmount}, " +
                    $"repairedTrackingTotals=path:{s_TrackingState.m_TotalPathRequestCount},actual:{s_TrackingState.m_TotalActualPathCount},avoided:{s_TrackingState.m_TotalAvoidedPathCount},decision:{s_TrackingState.m_TotalActualOrAvoidedPathCount},fine:{s_TrackingState.m_TotalFineAmount}");
            }

            return true;
        }

        private static HashSet<long> PreparePathSet(HashSet<long> buffer)
        {
            buffer.Clear();
            return buffer;
        }

        private static EnforcementPolicyImpactTrackingState CaptureCurrentState(long monthIndex)
        {
            return new EnforcementPolicyImpactTrackingState(
                monthIndex,
                s_TotalPathRequestCount,
                s_TotalActualPathCount,
                s_TotalAvoidedPathCount,
                s_TotalFineAmount,
                s_PublicTransportLaneActualCount,
                s_MidBlockCrossingActualCount,
                s_IntersectionMovementActualCount,
                s_PublicTransportLaneFineAmount,
                s_MidBlockCrossingFineAmount,
                s_IntersectionMovementFineAmount,
                s_PublicTransportLaneAvoidedEventCount,
                s_MidBlockCrossingAvoidedEventCount,
                s_IntersectionMovementAvoidedEventCount,
                s_TotalActualOrAvoidedPathCount,
                s_PublicTransportLaneActualOrAvoidedPathCount,
                s_MidBlockCrossingActualOrAvoidedPathCount,
                s_IntersectionMovementActualOrAvoidedPathCount);
        }

        private static EnforcementPolicyImpactTrackingState CreateTrackingStateFromTrackedMonthDelta(
            long monthIndex,
            RollingWindowSnapshot trackedMonthSnapshot)
        {
            return new EnforcementPolicyImpactTrackingState(
                monthIndex,
                ClampToNonNegative(
                    s_TotalPathRequestCount -
                    trackedMonthSnapshot.TotalPathRequestCount),
                ClampToNonNegative(
                    s_TotalActualPathCount -
                    trackedMonthSnapshot.TotalActualPathCount),
                ClampToNonNegative(
                    s_TotalAvoidedPathCount -
                    trackedMonthSnapshot.TotalAvoidedPathCount),
                ClampToNonNegative(
                    s_TotalFineAmount -
                    trackedMonthSnapshot.TotalFineAmount),
                ClampToNonNegative(
                    s_PublicTransportLaneActualCount -
                    trackedMonthSnapshot.PublicTransportLaneActualCount),
                ClampToNonNegative(
                    s_MidBlockCrossingActualCount -
                    trackedMonthSnapshot.MidBlockCrossingActualCount),
                ClampToNonNegative(
                    s_IntersectionMovementActualCount -
                    trackedMonthSnapshot.IntersectionMovementActualCount),
                ClampToNonNegative(
                    s_PublicTransportLaneFineAmount -
                    trackedMonthSnapshot.PublicTransportLaneFineAmount),
                ClampToNonNegative(
                    s_MidBlockCrossingFineAmount -
                    trackedMonthSnapshot.MidBlockCrossingFineAmount),
                ClampToNonNegative(
                    s_IntersectionMovementFineAmount -
                    trackedMonthSnapshot.IntersectionMovementFineAmount),
                ClampToNonNegative(
                    s_PublicTransportLaneAvoidedEventCount -
                    trackedMonthSnapshot.PublicTransportLaneAvoidedEventCount),
                ClampToNonNegative(
                    s_MidBlockCrossingAvoidedEventCount -
                    trackedMonthSnapshot.MidBlockCrossingAvoidedEventCount),
                ClampToNonNegative(
                    s_IntersectionMovementAvoidedEventCount -
                    trackedMonthSnapshot.IntersectionMovementAvoidedEventCount),
                ClampToNonNegative(
                    s_TotalActualOrAvoidedPathCount -
                    trackedMonthSnapshot.TotalActualOrAvoidedPathCount),
                ClampToNonNegative(
                    s_PublicTransportLaneActualOrAvoidedPathCount -
                    trackedMonthSnapshot.PublicTransportLaneActualOrAvoidedPathCount),
                ClampToNonNegative(
                    s_MidBlockCrossingActualOrAvoidedPathCount -
                    trackedMonthSnapshot.MidBlockCrossingActualOrAvoidedPathCount),
                ClampToNonNegative(
                    s_IntersectionMovementActualOrAvoidedPathCount -
                    trackedMonthSnapshot.IntersectionMovementActualOrAvoidedPathCount));
        }

        private static MonthlyEnforcementReport CreateCompletedMonthReport(
            EnforcementPolicyImpactTrackingState trackingState)
        {
            return new MonthlyEnforcementReport(
                trackingState.m_MonthIndex,
                ClampToNonNegative(s_TotalPathRequestCount - trackingState.m_TotalPathRequestCount),
                ClampToNonNegative(s_TotalActualPathCount - trackingState.m_TotalActualPathCount),
                ClampToNonNegative(s_PublicTransportLaneActualCount - trackingState.m_PublicTransportLaneActualCount),
                ClampToNonNegative(s_MidBlockCrossingActualCount - trackingState.m_MidBlockCrossingActualCount),
                ClampToNonNegative(s_IntersectionMovementActualCount - trackingState.m_IntersectionMovementActualCount),
                ClampToNonNegative(s_TotalFineAmount - trackingState.m_TotalFineAmount),
                ClampToNonNegative(s_TotalAvoidedPathCount - trackingState.m_TotalAvoidedPathCount),
                ClampToNonNegative(s_PublicTransportLaneFineAmount - trackingState.m_PublicTransportLaneFineAmount),
                ClampToNonNegative(s_MidBlockCrossingFineAmount - trackingState.m_MidBlockCrossingFineAmount),
                ClampToNonNegative(s_IntersectionMovementFineAmount - trackingState.m_IntersectionMovementFineAmount),
                ClampToNonNegative(s_PublicTransportLaneAvoidedEventCount - trackingState.m_PublicTransportLaneAvoidedEventCount),
                ClampToNonNegative(s_MidBlockCrossingAvoidedEventCount - trackingState.m_MidBlockCrossingAvoidedEventCount),
                ClampToNonNegative(s_IntersectionMovementAvoidedEventCount - trackingState.m_IntersectionMovementAvoidedEventCount),
                ClampToNonNegative(s_TotalActualOrAvoidedPathCount - trackingState.m_TotalActualOrAvoidedPathCount),
                ClampToNonNegative(s_PublicTransportLaneActualOrAvoidedPathCount - trackingState.m_PublicTransportLaneActualOrAvoidedPathCount),
                ClampToNonNegative(s_MidBlockCrossingActualOrAvoidedPathCount - trackingState.m_MidBlockCrossingActualOrAvoidedPathCount),
                ClampToNonNegative(s_IntersectionMovementActualOrAvoidedPathCount - trackingState.m_IntersectionMovementActualOrAvoidedPathCount));
        }

        private static bool TrackingStatesEqual(
            EnforcementPolicyImpactTrackingState left,
            EnforcementPolicyImpactTrackingState right)
        {
            return left.m_MonthIndex == right.m_MonthIndex
                && left.m_TotalPathRequestCount == right.m_TotalPathRequestCount
                && left.m_TotalActualPathCount == right.m_TotalActualPathCount
                && left.m_TotalAvoidedPathCount == right.m_TotalAvoidedPathCount
                && left.m_TotalFineAmount == right.m_TotalFineAmount
                && left.m_PublicTransportLaneActualCount == right.m_PublicTransportLaneActualCount
                && left.m_MidBlockCrossingActualCount == right.m_MidBlockCrossingActualCount
                && left.m_IntersectionMovementActualCount == right.m_IntersectionMovementActualCount
                && left.m_PublicTransportLaneFineAmount == right.m_PublicTransportLaneFineAmount
                && left.m_MidBlockCrossingFineAmount == right.m_MidBlockCrossingFineAmount
                && left.m_IntersectionMovementFineAmount == right.m_IntersectionMovementFineAmount
                && left.m_PublicTransportLaneAvoidedEventCount == right.m_PublicTransportLaneAvoidedEventCount
                && left.m_MidBlockCrossingAvoidedEventCount == right.m_MidBlockCrossingAvoidedEventCount
                && left.m_IntersectionMovementAvoidedEventCount == right.m_IntersectionMovementAvoidedEventCount
                && left.m_TotalActualOrAvoidedPathCount == right.m_TotalActualOrAvoidedPathCount
                && left.m_PublicTransportLaneActualOrAvoidedPathCount == right.m_PublicTransportLaneActualOrAvoidedPathCount
                && left.m_MidBlockCrossingActualOrAvoidedPathCount == right.m_MidBlockCrossingActualOrAvoidedPathCount
                && left.m_IntersectionMovementActualOrAvoidedPathCount == right.m_IntersectionMovementActualOrAvoidedPathCount;
        }

        private static bool IsSuspiciousEmptyTrackingBaseline(
            EnforcementPolicyImpactTrackingState trackingState)
        {
            if (trackingState.m_TotalPathRequestCount != 0 ||
                trackingState.m_TotalActualPathCount != 0 ||
                trackingState.m_TotalAvoidedPathCount != 0 ||
                trackingState.m_TotalActualOrAvoidedPathCount != 0 ||
                trackingState.m_TotalFineAmount != 0)
            {
                return false;
            }

            return s_TotalPathRequestCount > 0 ||
                s_TotalActualPathCount > 0 ||
                s_TotalAvoidedPathCount > 0 ||
                s_TotalActualOrAvoidedPathCount > 0 ||
                s_TotalFineAmount > 0;
        }

        private static int ClampToNonNegative(int value)
        {
            return value < 0 ? 0 : value;
        }

        private static bool IsGameplayContextAvailable()
        {
            return GameManager.instance != null && GameManager.instance.gameMode.IsGameOrEditor();
        }

        private static string LocalizeText(string localeId, string fallback)
        {
            if (GameManager.instance?.localizationManager?.activeDictionary != null &&
                GameManager.instance.localizationManager.activeDictionary.TryGetValue(localeId, out string value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return fallback;
        }

        private static string GetActiveLocaleId()
        {
            return GameManager.instance?.localizationManager?.activeLocaleId ?? string.Empty;
        }

        public static void UpdateRollingWindowData()
        {
            if (!EnforcementGameTime.IsInitialized)
            {
                return;
            }

            FlushPendingPathRequests();

            long currentTimestampMonthTicks = EnforcementGameTime.CurrentTimestampMonthTicks;
            if (s_NextRollingWindowPruneTimestampMonthTicks != long.MaxValue &&
                currentTimestampMonthTicks > 0L &&
                currentTimestampMonthTicks < s_NextRollingWindowPruneTimestampMonthTicks)
            {
                return;
            }

            long cutoffTimestamp = System.Math.Max(0L, currentTimestampMonthTicks - EnforcementGameTime.CurrentMonthTicksPerMonth);
            int pathRequestCountBefore = s_PathRequestEvents.Count;
            int actualViolationCountBefore = s_ActualViolationEvents.Count;
            int avoidedRerouteCountBefore = s_AvoidedRerouteEvents.Count;
            GetTimestampRange(
                s_PathRequestEvents,
                static entry => entry.TimestampMonthTicks,
                out long pathRequestMinTimestamp,
                out long pathRequestMaxTimestamp);
            GetTimestampRange(
                s_ActualViolationEvents,
                static entry => entry.TimestampMonthTicks,
                out long actualViolationMinTimestamp,
                out long actualViolationMaxTimestamp);
            GetTimestampRange(
                s_AvoidedRerouteEvents,
                static entry => entry.TimestampMonthTicks,
                out long avoidedRerouteMinTimestamp,
                out long avoidedRerouteMaxTimestamp);

            bool removedAny = false;
            removedAny |= PrunePathRequestEvents(cutoffTimestamp);
            removedAny |= PruneActualViolationEvents(cutoffTimestamp);
            removedAny |= PruneAvoidedRerouteEvents(cutoffTimestamp);

            if (removedAny)
            {
                s_RollingWindowSnapshotDirty = true;

                if (EnforcementLoggingPolicy.ShouldLogPolicyDiagnostics())
                {
                    Mod.log.Info(
                        "[ENFORCEMENT_POLICY_PRUNE] " +
                        $"cutoff={cutoffTimestamp}, monthTicks={currentTimestampMonthTicks}, monthWindow={EnforcementGameTime.CurrentMonthTicksPerMonth}, " +
                        $"events=path:{pathRequestCountBefore}->{s_PathRequestEvents.Count}, " +
                        $"actual:{actualViolationCountBefore}->{s_ActualViolationEvents.Count}, " +
                        $"avoided:{avoidedRerouteCountBefore}->{s_AvoidedRerouteEvents.Count}, " +
                        $"pathRange={pathRequestMinTimestamp}..{pathRequestMaxTimestamp}, " +
                        $"actualRange={actualViolationMinTimestamp}..{actualViolationMaxTimestamp}, " +
                        $"avoidedRange={avoidedRerouteMinTimestamp}..{avoidedRerouteMaxTimestamp}, " +
                        $"daysPerYear={EnforcementGameTime.CurrentDaysPerYear}");
                }
            }

            s_NextRollingWindowPruneTimestampMonthTicks =
                GetNextRollingWindowPruneTimestampMonthTicks();
        }


        private static void FlushPendingPathRequests()
        {
            if (s_PendingPathRequestSequencesUntilTimeInitialization.Count == 0 ||
                !EnforcementGameTime.IsInitialized ||
                EnforcementGameTime.CurrentTimestampMonthTicks < 0L)
            {
                return;
            }

            long timestampMonthTicks = EnforcementGameTime.CurrentTimestampMonthTicks;
            bool addedAny = false;

            for (int index = 0; index < s_PendingPathRequestSequencesUntilTimeInitialization.Count; index += 1)
            {
                long pathContextSequence =
                    s_PendingPathRequestSequencesUntilTimeInitialization[index];

                if (pathContextSequence > 0L)
                {
                    s_PathRequestEvents.Add(
                        new PathRequestEvent(
                            timestampMonthTicks,
                            pathContextSequence));
                    addedAny = true;
                }
            }

            s_PendingPathRequestSequencesUntilTimeInitialization.Clear();

            if (addedAny)
            {
                s_RollingWindowSnapshotDirty = true;
            }
        }

        private static long RecordPathRequestInternal(int vehicleId)
        {
            s_TotalPathRequestCount += 1;

            long pathContextSequence = 0L;
            if (vehicleId > 0)
            {
                pathContextSequence = s_NextPathContextSequence;
                s_NextPathContextSequence += 1L;
                s_ActivePathContextByVehicle[vehicleId] = pathContextSequence;
            }

            if (!EnforcementGameTime.IsInitialized ||
                EnforcementGameTime.CurrentTimestampMonthTicks < 0L)
            {
                if (pathContextSequence > 0L)
                {
                    s_PendingPathRequestSequencesUntilTimeInitialization.Add(pathContextSequence);
                }

                return pathContextSequence;
            }

            if (pathContextSequence > 0L)
            {
                s_PathRequestEvents.Add(
                    new PathRequestEvent(
                        EnforcementGameTime.CurrentTimestampMonthTicks,
                        pathContextSequence));

                s_RollingWindowSnapshotDirty = true;
            }

            return pathContextSequence;
        }

        public static void EnsureActivePathContext(int vehicleId)
        {
            if (vehicleId <= 0)
            {
                return;
            }

            if (s_ActivePathContextByVehicle.TryGetValue(vehicleId, out long existingSequence) &&
                existingSequence > 0L)
            {
                return;
            }

            long pathContextSequence = s_NextPathContextSequence;
            s_NextPathContextSequence += 1L;
            s_ActivePathContextByVehicle[vehicleId] = pathContextSequence;
        }

        private static bool TryGetActivePathContext(int vehicleId, out long pathContextSequence)
        {
            if (vehicleId > 0 &&
                s_ActivePathContextByVehicle.TryGetValue(vehicleId, out pathContextSequence) &&
                pathContextSequence > 0L)
            {
                return true;
            }

            pathContextSequence = 0L;
            return false;
        }

        private static bool TryMarkPathContextAsCounted(
            Dictionary<int, long> countedPathContexts,
            int vehicleId,
            long pathContextSequence)
        {
            if (vehicleId <= 0 || pathContextSequence <= 0L)
            {
                return true;
            }

            if (countedPathContexts.TryGetValue(vehicleId, out long previousSequence) &&
                previousSequence == pathContextSequence)
            {
                return false;
            }

            countedPathContexts[vehicleId] = pathContextSequence;
            return true;
        }

        private static Dictionary<int, long> GetKindActualPathContextMap(string kind)
        {
            switch (kind)
            {
                case EnforcementKinds.PublicTransportLane:
                    return s_PublicTransportLaneActualPathContextByVehicle;
                case EnforcementKinds.MidBlockCrossing:
                    return s_MidBlockCrossingActualPathContextByVehicle;
                case EnforcementKinds.IntersectionMovement:
                    return s_IntersectionMovementActualPathContextByVehicle;
                default:
                    return null;
            }
        }

        private static Dictionary<int, long> GetKindActualOrAvoidedPathContextMap(string kind)
        {
            switch (kind)
            {
                case EnforcementKinds.PublicTransportLane:
                    return s_PublicTransportLaneActualOrAvoidedPathContextByVehicle;

                case EnforcementKinds.MidBlockCrossing:
                    return s_MidBlockCrossingActualOrAvoidedPathContextByVehicle;

                case EnforcementKinds.IntersectionMovement:
                    return s_IntersectionMovementActualOrAvoidedPathContextByVehicle;

                default:
                    return null;
            }
        }

        private static string FormatRatio(int numerator, int denominator)
        {
            if (denominator <= 0)
            {
                return "n/a";
            }

            return (100d * numerator / denominator).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatMoney(int amount)
        {
            return amount.ToString("N0", CultureInfo.InvariantCulture);
        }

        private static bool PrunePathRequestEvents(long cutoffTimestamp)
        {
            return RemoveExpiredPrefix(
                s_PathRequestEvents,
                cutoffTimestamp,
                entry => entry.TimestampMonthTicks);
        }

        private static bool PruneActualViolationEvents(long cutoffTimestamp)
        {
            return RemoveExpiredPrefix(
                s_ActualViolationEvents,
                cutoffTimestamp,
                entry => entry.TimestampMonthTicks);
        }

        private static bool PruneAvoidedRerouteEvents(long cutoffTimestamp)
        {
            return RemoveExpiredPrefix(
                s_AvoidedRerouteEvents,
                cutoffTimestamp,
                entry => entry.TimestampMonthTicks);
        }

        private static long GetNextRollingWindowPruneTimestampMonthTicks()
        {
            if (!EnforcementGameTime.IsInitialized)
            {
                return long.MaxValue;
            }

            long monthTicksPerMonth = EnforcementGameTime.CurrentMonthTicksPerMonth;
            long earliestTimestamp = long.MaxValue;

            if (s_PathRequestEvents.Count > 0)
            {
                earliestTimestamp = System.Math.Min(
                    earliestTimestamp,
                    s_PathRequestEvents[0].TimestampMonthTicks);
            }

            if (s_ActualViolationEvents.Count > 0)
            {
                earliestTimestamp = System.Math.Min(
                    earliestTimestamp,
                    s_ActualViolationEvents[0].TimestampMonthTicks);
            }

            if (s_AvoidedRerouteEvents.Count > 0)
            {
                earliestTimestamp = System.Math.Min(
                    earliestTimestamp,
                    s_AvoidedRerouteEvents[0].TimestampMonthTicks);
            }

            return earliestTimestamp == long.MaxValue
                ? long.MaxValue
                : earliestTimestamp + monthTicksPerMonth;
        }
    }
}
