using System;
using System.Collections.Generic;
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
            int intersectionMovementAvoidedEventCount)
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

    public static class EnforcementPolicyImpactService
    {
        // Active vehicle route aggregation (ECS query based)
        public static int GetActiveVehicleRouteCount()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return 0;
            var entityManager = world.EntityManager;
            var vehicleQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>());
            return vehicleQuery.CalculateEntityCount();
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
                int intersectionMovementAvoidedEventCount)
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
        private static readonly Dictionary<int, long> s_ActivePathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_TotalActualPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_PublicTransportLaneActualPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_MidBlockCrossingActualPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_IntersectionMovementActualPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_TotalAvoidedPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_PublicTransportLaneAvoidedPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_MidBlockCrossingAvoidedPathContextByVehicle = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> s_IntersectionMovementAvoidedPathContextByVehicle = new Dictionary<int, long>();

        public static bool TryGetTrackingState(out EnforcementPolicyImpactTrackingState trackingState)
        {
            trackingState = s_TrackingState;
            return s_HasTrackingState;
        }

        public static IReadOnlyCollection<PathRequestEvent> GetPathRequestEventSnapshot()
        {
            return s_PathRequestEvents.ToArray();
        }

        public static IReadOnlyCollection<ActualViolationEvent> GetActualViolationEventSnapshot()
        {
            return s_ActualViolationEvents.ToArray();
        }

        public static IReadOnlyCollection<AvoidedRerouteEvent> GetAvoidedRerouteEventSnapshot()
        {
            return s_AvoidedRerouteEvents.ToArray();
        }

        public static void ResetPersistentData()
        {
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
        }

        public static void LoadPersistentData(
            EnforcementPolicyImpactTrackingState? trackingState,
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
            IEnumerable<PathRequestEvent> pathRequestEvents,
            IEnumerable<ActualViolationEvent> actualViolationEvents,
            IEnumerable<AvoidedRerouteEvent> avoidedRerouteEvents)
        {
            ResetPersistentData();

            if (trackingState.HasValue)
            {
                s_TrackingState = trackingState.Value;
                s_HasTrackingState = true;
            }

            s_TotalPathRequestCount = totalPathRequestCount;
            s_TotalActualPathCount = totalActualPathCount;
            s_TotalAvoidedPathCount = totalAvoidedPathCount;
            s_TotalFineAmount = totalFineAmount;
            s_PublicTransportLaneActualCount = publicTransportLaneActualCount;
            s_MidBlockCrossingActualCount = midBlockCrossingActualCount;
            s_IntersectionMovementActualCount = intersectionMovementActualCount;
            s_PublicTransportLaneFineAmount = publicTransportLaneFineAmount;
            s_MidBlockCrossingFineAmount = midBlockCrossingFineAmount;
            s_IntersectionMovementFineAmount = intersectionMovementFineAmount;
            s_PublicTransportLaneAvoidedEventCount = publicTransportLaneAvoidedEventCount;
            s_MidBlockCrossingAvoidedEventCount = midBlockCrossingAvoidedEventCount;
            s_IntersectionMovementAvoidedEventCount = intersectionMovementAvoidedEventCount;

            if (pathRequestEvents != null)
            {
                foreach (PathRequestEvent entry in pathRequestEvents)
                {
                    if (entry.TimestampMonthTicks >= 0L)
                    {
                        s_PathRequestEvents.Add(entry);
                    }
                }
            }

            if (actualViolationEvents != null)
            {
                foreach (ActualViolationEvent entry in actualViolationEvents)
                {
                    if (entry.TimestampMonthTicks >= 0L &&
                        !string.IsNullOrWhiteSpace(entry.Kind) &&
                        entry.FineAmount >= 0)
                    {
                        s_ActualViolationEvents.Add(entry);
                    }
                }
            }

            if (avoidedRerouteEvents != null)
            {
                foreach (AvoidedRerouteEvent entry in avoidedRerouteEvents)
                {
                    if (entry.TimestampMonthTicks >= 0L && (entry.AvoidedPublicTransportLanePenalty || entry.AvoidedMidBlockPenalty || entry.AvoidedIntersectionPenalty))
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
                s_IntersectionMovementAvoidedEventCount);
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

            // Real-time route count and category logs (recent 1 month)
            var snapshot = GetRollingWindowSnapshot();
            int violationTotal = snapshot.TotalActualPathCount;
            int avoidanceTotal = snapshot.TotalAvoidedPathCount;
            int vehicleRouteDenominator = snapshot.TotalPathRequestCount; // Unified denominator for all statistics
            if (EnforcementLoggingPolicy.EnableEnforcementEventLogging)
            {
                Mod.log.Info($"[RouteCount] vehicleRouteDenominator={vehicleRouteDenominator}, violationTotal={violationTotal}, avoidanceTotal={avoidanceTotal}");
                Mod.log.Info($"[ViolationCount] PublicTransport={snapshot.PublicTransportLaneActualCount}, MidBlock={snapshot.MidBlockCrossingActualCount}, Intersection={snapshot.IntersectionMovementActualCount}");
                Mod.log.Info($"[AvoidanceCount] PublicTransport={snapshot.PublicTransportLaneAvoidedEventCount}, MidBlock={snapshot.MidBlockCrossingAvoidedEventCount}, Intersection={snapshot.IntersectionMovementAvoidedEventCount}");
            }

            long currentMonthIndex = EnforcementGameTime.GetMonthIndex(EnforcementGameTime.CurrentTimestampMonthTicks);
            if (!s_HasTrackingState || currentMonthIndex != s_TrackingState.m_MonthIndex)
            {
                s_TrackingState = CaptureCurrentState(currentMonthIndex);
                s_HasTrackingState = true;
            }
        }

        public static void RecordActualViolation(string kind, int fineAmount, int vehicleId)
        {
            s_TotalFineAmount += fineAmount;

            bool countsTowardTotalPath = false;
            bool countsTowardKindPath = false;
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
            }

            if (countsTowardTotalPath)
            {
                s_TotalActualPathCount += 1;
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
            }

            switch (kind)
            {
                case EnforcementKinds.PublicTransportLane:
                    if (countsTowardKindPath)
                    {
                        s_PublicTransportLaneActualCount += 1;
                    }

                    s_PublicTransportLaneFineAmount += fineAmount;
                    break;

                case EnforcementKinds.MidBlockCrossing:
                    if (countsTowardKindPath)
                    {
                        s_MidBlockCrossingActualCount += 1;
                    }

                    s_MidBlockCrossingFineAmount += fineAmount;
                    break;

                case EnforcementKinds.IntersectionMovement:
                    if (countsTowardKindPath)
                    {
                        s_IntersectionMovementActualCount += 1;
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

            if (countsTowardTotalPath)
            {
                s_TotalAvoidedPathCount += 1;
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
            }

            if (countsTowardPublicTransportLanePath)
            {
                s_PublicTransportLaneAvoidedEventCount += 1;
            }

            if (countsTowardMidBlockPath)
            {
                s_MidBlockCrossingAvoidedEventCount += 1;
            }

            if (countsTowardIntersectionPath)
            {
                s_IntersectionMovementAvoidedEventCount += 1;
            }
        }

        public static string GetTotalStatisticsLine()
        {
            if (!IsGameplayContextAvailable())
                return LocalizeText(kLoadedSaveOnlyLocaleId, "Available only in a loaded save.");
            if (!EnforcementGameTime.IsInitialized)
                return LocalizeText(kWaitingForTimeLocaleId, "Waiting for in-game time initialization.");
            var snapshot = GetRollingWindowSnapshot();
            int violationNumeratorTotal = snapshot.TotalActualPathCount;
            int vehicleRouteDenominatorTotal = snapshot.TotalPathRequestCount;
            int suppressionFailureDenominatorTotal = snapshot.TotalActualOrAvoidedPathCount;
            string violationRateTotal = FormatRatio(violationNumeratorTotal, vehicleRouteDenominatorTotal);
            string suppressionFailureRateTotal = FormatRatio(violationNumeratorTotal, suppressionFailureDenominatorTotal);
            string finesTotal = FormatMoney(snapshot.TotalFineAmount);
            string lineFormat = LocalizeText(kStatisticsLineFormat, "violation rate {0}, suppression failure rate {1}, fines {2}₡.");
            return string.Format(lineFormat, violationRateTotal, suppressionFailureRateTotal, finesTotal);
        }

        public static string GetPublicTransportLaneStatisticsLine()
        {
            if (!IsGameplayContextAvailable())
                return LocalizeText(kLoadedSaveOnlyLocaleId, "Available only in a loaded save.");
            if (!EnforcementGameTime.IsInitialized)
                return LocalizeText(kWaitingForTimeLocaleId, "Waiting for in-game time initialization.");
            var snapshot = GetRollingWindowSnapshot();
            int vehicleRouteDenominatorTotal = snapshot.TotalPathRequestCount;
            int violationNumerator1 = snapshot.PublicTransportLaneActualCount;
            int suppressionFailureDenominator1 = snapshot.PublicTransportLaneActualOrAvoidedPathCount;
            string violationRate1 = FormatRatio(violationNumerator1, vehicleRouteDenominatorTotal);
            string suppressionFailureRate1 = FormatRatio(violationNumerator1, suppressionFailureDenominator1);
            string fines1 = FormatMoney(snapshot.PublicTransportLaneFineAmount);
            string lineFormat = LocalizeText(kStatisticsLineFormat, "violation rate {0}, suppression failure rate {1}, fines {2}₡.");
            return string.Format(lineFormat, violationRate1, suppressionFailureRate1, fines1);
        }

        public static string GetMidBlockCrossingStatisticsLine()
        {
            if (!IsGameplayContextAvailable())
                return LocalizeText(kLoadedSaveOnlyLocaleId, "Available only in a loaded save.");
            if (!EnforcementGameTime.IsInitialized)
                return LocalizeText(kWaitingForTimeLocaleId, "Waiting for in-game time initialization.");
            var snapshot = GetRollingWindowSnapshot();
            int vehicleRouteDenominatorTotal = snapshot.TotalPathRequestCount;
            int violationNumerator2 = snapshot.MidBlockCrossingActualCount;
            int suppressionFailureDenominator2 = snapshot.MidBlockCrossingActualOrAvoidedPathCount;
            string violationRate2 = FormatRatio(violationNumerator2, vehicleRouteDenominatorTotal);
            string suppressionFailureRate2 = FormatRatio(violationNumerator2, suppressionFailureDenominator2);
            string fines2 = FormatMoney(snapshot.MidBlockCrossingFineAmount);
            string lineFormat = LocalizeText(kStatisticsLineFormat, "violation rate {0}, suppression failure rate {1}, fines {2}₡.");
            return string.Format(lineFormat, violationRate2, suppressionFailureRate2, fines2);
        }

        public static string GetIntersectionMovementStatisticsLine()
        {
            if (!IsGameplayContextAvailable())
                return LocalizeText(kLoadedSaveOnlyLocaleId, "Available only in a loaded save.");
            if (!EnforcementGameTime.IsInitialized)
                return LocalizeText(kWaitingForTimeLocaleId, "Waiting for in-game time initialization.");
            var snapshot = GetRollingWindowSnapshot();
            int vehicleRouteDenominatorTotal = snapshot.TotalPathRequestCount;
            int violationNumerator3 = snapshot.IntersectionMovementActualCount;
            int suppressionFailureDenominator3 = snapshot.IntersectionMovementActualOrAvoidedPathCount;
            string violationRate3 = FormatRatio(violationNumerator3, vehicleRouteDenominatorTotal);
            string suppressionFailureRate3 = FormatRatio(violationNumerator3, suppressionFailureDenominator3);
            string fines3 = FormatMoney(snapshot.IntersectionMovementFineAmount);
            string lineFormat = LocalizeText(kStatisticsLineFormat, "violation rate {0}, suppression failure rate {1}, fines {2}₡.");
            return string.Format(lineFormat, violationRate3, suppressionFailureRate3, fines3);
        }

        public static RollingWindowSnapshot GetRollingWindowSnapshot()
        {
            if (!EnforcementGameTime.IsInitialized)
            {
                return default;
            }

            UpdateRollingWindowData();

            HashSet<long> requestPaths = new HashSet<long>();
            HashSet<long> totalActualPaths = new HashSet<long>();
            HashSet<long> totalAvoidedPaths = new HashSet<long>();

            HashSet<long> publicTransportActualPaths = new HashSet<long>();
            HashSet<long> midBlockActualPaths = new HashSet<long>();
            HashSet<long> intersectionActualPaths = new HashSet<long>();

            HashSet<long> publicTransportAvoidedPaths = new HashSet<long>();
            HashSet<long> midBlockAvoidedPaths = new HashSet<long>();
            HashSet<long> intersectionAvoidedPaths = new HashSet<long>();

            int totalFineAmount = 0;
            int publicTransportLaneFineAmount = 0;
            int midBlockCrossingFineAmount = 0;
            int intersectionMovementFineAmount = 0;

            for (int index = 0; index < s_PathRequestEvents.Count; index += 1)
            {
                PathRequestEvent entry = s_PathRequestEvents[index];
                if (entry.PathContextSequence > 0L)
                {
                    requestPaths.Add(entry.PathContextSequence);
                }
            }

            for (int index = 0; index < s_ActualViolationEvents.Count; index += 1)
            {
                ActualViolationEvent entry = s_ActualViolationEvents[index];
                totalFineAmount += entry.FineAmount;

                if (entry.PathContextSequence <= 0L)
                {
                    continue;
                }

                totalActualPaths.Add(entry.PathContextSequence);

                switch (entry.Kind)
                {
                    case EnforcementKinds.PublicTransportLane:
                        publicTransportActualPaths.Add(entry.PathContextSequence);
                        publicTransportLaneFineAmount += entry.FineAmount;
                        break;

                    case EnforcementKinds.MidBlockCrossing:
                        midBlockActualPaths.Add(entry.PathContextSequence);
                        midBlockCrossingFineAmount += entry.FineAmount;
                        break;

                    case EnforcementKinds.IntersectionMovement:
                        intersectionActualPaths.Add(entry.PathContextSequence);
                        intersectionMovementFineAmount += entry.FineAmount;
                        break;
                }
            }

            for (int index = 0; index < s_AvoidedRerouteEvents.Count; index += 1)
            {
                AvoidedRerouteEvent entry = s_AvoidedRerouteEvents[index];

                if (entry.PathContextSequence <= 0L)
                {
                    continue;
                }

                totalAvoidedPaths.Add(entry.PathContextSequence);

                if (entry.AvoidedPublicTransportLanePenalty)
                {
                    publicTransportAvoidedPaths.Add(entry.PathContextSequence);
                }

                if (entry.AvoidedMidBlockPenalty)
                {
                    midBlockAvoidedPaths.Add(entry.PathContextSequence);
                }

                if (entry.AvoidedIntersectionPenalty)
                {
                    intersectionAvoidedPaths.Add(entry.PathContextSequence);
                }
            }

            HashSet<long> totalActualOrAvoidedPaths = new HashSet<long>(totalActualPaths);
            totalActualOrAvoidedPaths.UnionWith(totalAvoidedPaths);

            HashSet<long> publicTransportActualOrAvoidedPaths =
                new HashSet<long>(publicTransportActualPaths);
            publicTransportActualOrAvoidedPaths.UnionWith(publicTransportAvoidedPaths);

            HashSet<long> midBlockActualOrAvoidedPaths =
                new HashSet<long>(midBlockActualPaths);
            midBlockActualOrAvoidedPaths.UnionWith(midBlockAvoidedPaths);

            HashSet<long> intersectionActualOrAvoidedPaths =
                new HashSet<long>(intersectionActualPaths);
            intersectionActualOrAvoidedPaths.UnionWith(intersectionAvoidedPaths);

            return new RollingWindowSnapshot(
                requestPaths.Count,
                totalActualPaths.Count,
                totalAvoidedPaths.Count,
                totalActualOrAvoidedPaths.Count,
                totalFineAmount,
                publicTransportActualPaths.Count,
                midBlockActualPaths.Count,
                intersectionActualPaths.Count,
                publicTransportLaneFineAmount,
                midBlockCrossingFineAmount,
                intersectionMovementFineAmount,
                publicTransportAvoidedPaths.Count,
                midBlockAvoidedPaths.Count,
                intersectionAvoidedPaths.Count,
                publicTransportActualOrAvoidedPaths.Count,
                midBlockActualOrAvoidedPaths.Count,
                intersectionActualOrAvoidedPaths.Count);
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
                s_IntersectionMovementAvoidedEventCount);
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

        public static void UpdateRollingWindowData()
        {
            if (!EnforcementGameTime.IsInitialized)
            {
                return;
            }

            FlushPendingPathRequests();

            long currentTimestampMonthTicks = EnforcementGameTime.CurrentTimestampMonthTicks;
            long cutoffTimestamp = System.Math.Max(0L, currentTimestampMonthTicks - EnforcementGameTime.CurrentMonthTicksPerMonth);
            PrunePathRequestEvents(cutoffTimestamp);
            PruneActualViolationEvents(cutoffTimestamp);
            PruneAvoidedRerouteEvents(cutoffTimestamp);
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
                }
            }

            s_PendingPathRequestSequencesUntilTimeInitialization.Clear();
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
            }

            return pathContextSequence;
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

        private static void PrunePathRequestEvents(long cutoffTimestamp)
        {
            for (int index = s_PathRequestEvents.Count - 1; index >= 0; index -= 1)
            {
                if (s_PathRequestEvents[index].TimestampMonthTicks < cutoffTimestamp)
                {
                    s_PathRequestEvents.RemoveAt(index);
                }
            }
        }

        private static void PruneActualViolationEvents(long cutoffTimestamp)
        {
            for (int index = s_ActualViolationEvents.Count - 1; index >= 0; index -= 1)
            {
                if (s_ActualViolationEvents[index].TimestampMonthTicks < cutoffTimestamp)
                {
                    s_ActualViolationEvents.RemoveAt(index);
                }
            }
        }

        private static void PruneAvoidedRerouteEvents(long cutoffTimestamp)
        {
            for (int index = s_AvoidedRerouteEvents.Count - 1; index >= 0; index -= 1)
            {
                if (s_AvoidedRerouteEvents[index].TimestampMonthTicks < cutoffTimestamp)
                {
                    s_AvoidedRerouteEvents.RemoveAt(index);
                }
            }
        }
    }
}
