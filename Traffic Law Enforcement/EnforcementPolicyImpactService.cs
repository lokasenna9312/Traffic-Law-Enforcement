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

        public PathRequestEvent(long timestampMonthTicks)
        {
            TimestampMonthTicks = timestampMonthTicks;
        }
    }

    public readonly struct ActualViolationEvent
    {
        public readonly long TimestampMonthTicks;
        public readonly string Kind;
        public readonly int FineAmount;

        public ActualViolationEvent(long timestampMonthTicks, string kind, int fineAmount)
        {
            TimestampMonthTicks = timestampMonthTicks;
            Kind = kind;
            FineAmount = fineAmount;
        }
    }

    public readonly struct AvoidedRerouteEvent
    {
        public readonly long TimestampMonthTicks;
        public readonly bool AvoidedPublicTransportLanePenalty;
        public readonly bool AvoidedMidBlockPenalty;
        public readonly bool AvoidedIntersectionPenalty;

        public AvoidedRerouteEvent(long timestampMonthTicks, bool avoidedPublicTransportLanePenalty, bool avoidedMidBlockPenalty, bool avoidedIntersectionPenalty)
        {
            TimestampMonthTicks = timestampMonthTicks;
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

        public RollingWindowSnapshot(
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
        public const string kSummaryLineFormatLocaleId = "TrafficLawEnforcement.PolicyImpact.Text.SummaryLineFormat";
        public const string kDetailLineFormatLocaleId = "TrafficLawEnforcement.PolicyImpact.Text.DetailLineFormat";
        public const string kNoteLocaleId = "TrafficLawEnforcement.PolicyImpact.Text.Note";
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
        private static int s_PendingPathRequestsUntilTimeInitialization;
        private static readonly List<PathRequestEvent> s_PathRequestEvents = new List<PathRequestEvent>();
        private static readonly List<ActualViolationEvent> s_ActualViolationEvents = new List<ActualViolationEvent>();
        private static readonly List<AvoidedRerouteEvent> s_AvoidedRerouteEvents = new List<AvoidedRerouteEvent>();

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
            s_PendingPathRequestsUntilTimeInitialization = 0;
            s_PathRequestEvents.Clear();
            s_ActualViolationEvents.Clear();
            s_AvoidedRerouteEvents.Clear();
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
                    if (entry.TimestampMonthTicks >= 0L && !string.IsNullOrWhiteSpace(entry.Kind) && entry.FineAmount >= 0)
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

        public static void RecordPathRequest()
        {
            s_TotalPathRequestCount += 1;
            if (!EnforcementGameTime.IsInitialized)
            {
                s_PendingPathRequestsUntilTimeInitialization += 1;
                return;
            }
            if (EnforcementGameTime.CurrentTimestampMonthTicks < 0)
            {
                s_PendingPathRequestsUntilTimeInitialization += 1;
                return;
            }
            s_PathRequestEvents.Add(new PathRequestEvent(EnforcementGameTime.CurrentTimestampMonthTicks));
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
            int violationTotal = snapshot.PublicTransportLaneActualCount + snapshot.MidBlockCrossingActualCount + snapshot.IntersectionMovementActualCount;
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

        public static void RecordActualViolation(string kind, int fineAmount)
        {
            s_TotalActualPathCount += 1;
            s_TotalFineAmount += fineAmount;

            if (EnforcementGameTime.IsInitialized && EnforcementGameTime.CurrentTimestampMonthTicks >= 0L && !string.IsNullOrWhiteSpace(kind))
            {
                s_ActualViolationEvents.Add(new ActualViolationEvent(EnforcementGameTime.CurrentTimestampMonthTicks, kind, fineAmount));
            }

            switch (kind)
            {
                case EnforcementKinds.PublicTransportLane:
                    s_PublicTransportLaneActualCount += 1;
                    s_PublicTransportLaneFineAmount += fineAmount;
                    break;
                case EnforcementKinds.MidBlockCrossing:
                    s_MidBlockCrossingActualCount += 1;
                    s_MidBlockCrossingFineAmount += fineAmount;
                    break;
                case EnforcementKinds.IntersectionMovement:
                    s_IntersectionMovementActualCount += 1;
                    s_IntersectionMovementFineAmount += fineAmount;
                    break;
            }
        }

        public static void RecordAvoidedReroute(bool avoidedPublicTransportLanePenalty, bool avoidedMidBlockPenalty, bool avoidedIntersectionPenalty)
        {
            if (!avoidedPublicTransportLanePenalty && !avoidedMidBlockPenalty && !avoidedIntersectionPenalty)
            {
                return;
            }

            s_TotalAvoidedPathCount += 1;

            if (EnforcementGameTime.IsInitialized && EnforcementGameTime.CurrentTimestampMonthTicks >= 0L)
            {
                s_AvoidedRerouteEvents.Add(new AvoidedRerouteEvent(EnforcementGameTime.CurrentTimestampMonthTicks, avoidedPublicTransportLanePenalty, avoidedMidBlockPenalty, avoidedIntersectionPenalty));
            }

            if (avoidedPublicTransportLanePenalty)
            {
                s_PublicTransportLaneAvoidedEventCount += 1;
            }

            if (avoidedMidBlockPenalty)
            {
                s_MidBlockCrossingAvoidedEventCount += 1;
            }

            if (avoidedIntersectionPenalty)
            {
                s_IntersectionMovementAvoidedEventCount += 1;
            }

        }

        public static string GetCurrentPeriodSummaryText()
        {
            if (!IsGameplayContextAvailable())
            {
                return LocalizeText(kLoadedSaveOnlyLocaleId, "Available only in a loaded save.");
            }

            if (!EnforcementGameTime.IsInitialized)
            {
                return LocalizeText(kWaitingForTimeLocaleId, "Waiting for in-game time initialization.");
            }

            RollingWindowSnapshot snapshot = GetRollingWindowSnapshot();
            // Use the aggregated value of all vehicle entity routes as denominator
            int violationNumerator = snapshot.PublicTransportLaneActualCount + snapshot.MidBlockCrossingActualCount + snapshot.IntersectionMovementActualCount;
            int vehicleRouteDenominator = snapshot.TotalPathRequestCount;
            int suppressionFailureDenominator = violationNumerator + snapshot.TotalAvoidedPathCount;
            if (vehicleRouteDenominator <= 0 && suppressionFailureDenominator <= 0)
            {
                return LocalizeText(kNoDataLocaleId, "No pathfinding requests, fined violations, or rerouted pathfinding outcomes that avoided penalized routes have been recorded yet.");
            }

            string violationRate = FormatRatio(violationNumerator, vehicleRouteDenominator);
            string suppressionFailureRate = FormatRatio(violationNumerator, suppressionFailureDenominator);
            string fines = FormatMoney(snapshot.TotalFineAmount);
            string totalLabel = LocalizeText(kTotalLabelLocaleId, "Total");
            return FormatLocalizedText(kSummaryLineFormatLocaleId, "{0}: violation rate {1}, suppression failure rate {2}, fines {3}", totalLabel, violationRate, suppressionFailureRate, fines);
        }

        public static string GetCurrentPeriodDetailsText()
        {
            if (!IsGameplayContextAvailable())
            {
                return LocalizeText(kLoadedSaveOnlyLocaleId, "Available only in a loaded save.");
            }

            if (!EnforcementGameTime.IsInitialized)
            {
                return LocalizeText(kWaitingForTimeLocaleId, "Waiting for in-game time initialization.");
            }

            RollingWindowSnapshot snapshot = GetRollingWindowSnapshot();
            StringBuilder builder = new StringBuilder(640);
            int vehicleRouteDenominator = snapshot.TotalPathRequestCount;
            AppendRateAndFineLine(builder, LocalizeText(kTotalLabelLocaleId, "Total"), snapshot.TotalActualPathCount, vehicleRouteDenominator, snapshot.TotalAvoidedPathCount, snapshot.TotalFineAmount);
            AppendRateAndFineLine(builder, LocalizeText(kPublicTransportLaneLabelLocaleId, "PT-lane"), snapshot.PublicTransportLaneActualCount, vehicleRouteDenominator, snapshot.PublicTransportLaneAvoidedEventCount, snapshot.PublicTransportLaneFineAmount);
            AppendRateAndFineLine(builder, LocalizeText(kMidBlockLabelLocaleId, "Mid-block"), snapshot.MidBlockCrossingActualCount, vehicleRouteDenominator, snapshot.MidBlockCrossingAvoidedEventCount, snapshot.MidBlockCrossingFineAmount);
            AppendRateAndFineLine(builder, LocalizeText(kIntersectionLabelLocaleId, "Intersection"), snapshot.IntersectionMovementActualCount, vehicleRouteDenominator, snapshot.IntersectionMovementAvoidedEventCount, snapshot.IntersectionMovementFineAmount);
            builder.AppendLine();
            builder.Append(LocalizeText(kNoteLocaleId, "Note: A counts pathfinding requests, not unique trips. D counts estimated rerouted pathfinding outcomes that gave up a penalized route. Per-type D counts can overlap when one reroute avoids multiple penalty types."));
            return builder.ToString();
        }

        public static string GetCurrentPeriodPublicTransportLaneText()
        {
            return GetCurrentPeriodLineText(kPublicTransportLaneLabelLocaleId, "PT-lane", snapshot => snapshot.PublicTransportLaneActualCount, snapshot => snapshot.PublicTransportLaneAvoidedEventCount, snapshot => snapshot.PublicTransportLaneFineAmount);
        }

        public static string GetCurrentPeriodMidBlockText()
        {
            return GetCurrentPeriodLineText(kMidBlockLabelLocaleId, "Mid-block", snapshot => snapshot.MidBlockCrossingActualCount, snapshot => snapshot.MidBlockCrossingAvoidedEventCount, snapshot => snapshot.MidBlockCrossingFineAmount);
        }

        public static string GetCurrentPeriodIntersectionText()
        {
            return GetCurrentPeriodLineText(kIntersectionLabelLocaleId, "Intersection", snapshot => snapshot.IntersectionMovementActualCount, snapshot => snapshot.IntersectionMovementAvoidedEventCount, snapshot => snapshot.IntersectionMovementFineAmount);
        }

        public static RollingWindowSnapshot GetRollingWindowSnapshot()
        {
            if (!EnforcementGameTime.IsInitialized)
            {
                return default;
            }

            UpdateRollingWindowData();

            int totalPathRequestCount = s_PathRequestEvents.Count;
            int totalActualPathCount = 0;
            int totalAvoidedPathCount = s_AvoidedRerouteEvents.Count;
            int totalFineAmount = 0;
            int publicTransportLaneActualCount = 0;
            int midBlockCrossingActualCount = 0;
            int intersectionMovementActualCount = 0;
            int publicTransportLaneFineAmount = 0;
            int midBlockCrossingFineAmount = 0;
            int intersectionMovementFineAmount = 0;
            int publicTransportLaneAvoidedEventCount = 0;
            int midBlockCrossingAvoidedEventCount = 0;
            int intersectionMovementAvoidedEventCount = 0;

            for (int index = 0; index < s_ActualViolationEvents.Count; index += 1)
            {
                ActualViolationEvent entry = s_ActualViolationEvents[index];
                totalActualPathCount += 1;
                totalFineAmount += entry.FineAmount;

                switch (entry.Kind)
                {
                    case EnforcementKinds.PublicTransportLane:
                        publicTransportLaneActualCount += 1;
                        publicTransportLaneFineAmount += entry.FineAmount;
                        break;
                    case EnforcementKinds.MidBlockCrossing:
                        midBlockCrossingActualCount += 1;
                        midBlockCrossingFineAmount += entry.FineAmount;
                        break;
                    case EnforcementKinds.IntersectionMovement:
                        intersectionMovementActualCount += 1;
                        intersectionMovementFineAmount += entry.FineAmount;
                        break;
                }
            }

            for (int index = 0; index < s_AvoidedRerouteEvents.Count; index += 1)
            {
                AvoidedRerouteEvent entry = s_AvoidedRerouteEvents[index];
                if (entry.AvoidedPublicTransportLanePenalty)
                {
                    publicTransportLaneAvoidedEventCount += 1;
                }

                if (entry.AvoidedMidBlockPenalty)
                {
                    midBlockCrossingAvoidedEventCount += 1;
                }

                if (entry.AvoidedIntersectionPenalty)
                {
                    intersectionMovementAvoidedEventCount += 1;
                }
            }

            return new RollingWindowSnapshot(
                totalPathRequestCount,
                totalActualPathCount,
                totalAvoidedPathCount,
                totalFineAmount,
                publicTransportLaneActualCount,
                midBlockCrossingActualCount,
                intersectionMovementActualCount,
                publicTransportLaneFineAmount,
                midBlockCrossingFineAmount,
                intersectionMovementFineAmount,
                publicTransportLaneAvoidedEventCount,
                midBlockCrossingAvoidedEventCount,
                intersectionMovementAvoidedEventCount);
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
            if (s_PendingPathRequestsUntilTimeInitialization <= 0 ||
                !EnforcementGameTime.IsInitialized ||
                EnforcementGameTime.CurrentTimestampMonthTicks < 0L)
            {
                return;
            }

            long timestampMonthTicks = EnforcementGameTime.CurrentTimestampMonthTicks;
            for (int index = 0; index < s_PendingPathRequestsUntilTimeInitialization; index += 1)
            {
                s_PathRequestEvents.Add(new PathRequestEvent(timestampMonthTicks));
            }

            s_PendingPathRequestsUntilTimeInitialization = 0;
        }

        private static string GetCurrentPeriodLineText(string labelLocaleId, string fallbackLabel, Func<RollingWindowSnapshot, int> actualCountSelector, Func<RollingWindowSnapshot, int> avoidedCountSelector, Func<RollingWindowSnapshot, int> fineAmountSelector)
        {
            if (!IsGameplayContextAvailable())
            {
                return LocalizeText(kLoadedSaveOnlyLocaleId, "Available only in a loaded save.");
            }

            if (!EnforcementGameTime.IsInitialized)
            {
                return LocalizeText(kWaitingForTimeLocaleId, "Waiting for in-game time initialization.");
            }

            RollingWindowSnapshot snapshot = GetRollingWindowSnapshot();
            string label = LocalizeText(labelLocaleId, fallbackLabel);
            int actualCount = actualCountSelector(snapshot);
            int avoidedCount = avoidedCountSelector(snapshot);
            int fineAmount = fineAmountSelector(snapshot);
            int vehicleRouteDenominator = snapshot.TotalPathRequestCount; // Use 1-month aggregated denominator
            string violationRate = FormatRatio(actualCount, vehicleRouteDenominator);
            string suppressionFailureRate = FormatRatio(actualCount, actualCount + avoidedCount);
            string fines = FormatMoney(fineAmount);
            return FormatLocalizedText(kDetailLineFormatLocaleId, "{0}: violation rate {1}, suppression failure rate {2}, fines {3}", label, violationRate, suppressionFailureRate, fines);
        }

        private static int ClampToNonNegative(int value)
        {
            return value < 0 ? 0 : value;
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

        private static string FormatLocalizedText(string localeId, string fallbackFormat, params object[] args)
        {
            string format = LocalizeText(localeId, fallbackFormat);
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

        private static void AppendRateAndFineLine(StringBuilder builder, string label, int actualCount, int totalPathRequestCount, int avoidedCount, int fineAmount)
        {
            string violationRate = FormatRatio(actualCount, totalPathRequestCount);
            string suppressionFailureRate = FormatRatio(actualCount, actualCount + avoidedCount);
            string fines = FormatMoney(fineAmount);
            builder.AppendLine(FormatLocalizedText(kDetailLineFormatLocaleId, "{0}: violation rate {1}, suppression failure rate {2}, fines {3}", label, violationRate, suppressionFailureRate, fines));
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
