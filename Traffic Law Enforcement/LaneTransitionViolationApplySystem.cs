using Game;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public partial class LaneTransitionViolationApplySystem : GameSystemBase
    {
        private EntityQuery m_EventBufferQuery;
        private EntityQuery m_StatisticsQuery;

        private Entity m_EventEntity;
        private Entity m_StatisticsEntity;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EventBufferQuery = GetEntityQuery(
                ComponentType.ReadOnly<LaneTransitionViolationEventBufferTag>(),
                ComponentType.ReadWrite<DetectedLaneTransitionViolation>());

            if (m_EventBufferQuery.IsEmptyIgnoreFilter)
            {
                m_EventEntity = EntityManager.CreateEntity();
                EntityManager.AddComponent<LaneTransitionViolationEventBufferTag>(m_EventEntity);
                EntityManager.AddBuffer<DetectedLaneTransitionViolation>(m_EventEntity);
            }
            else
            {
                m_EventEntity = m_EventBufferQuery.GetSingletonEntity();
            }

            m_StatisticsQuery = GetEntityQuery(
                ComponentType.ReadWrite<TrafficLawEnforcementStatistics>());

            if (m_StatisticsQuery.IsEmptyIgnoreFilter)
            {
                m_StatisticsEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(
                    m_StatisticsEntity,
                    default(TrafficLawEnforcementStatistics));
            }
            else
            {
                m_StatisticsEntity = m_StatisticsQuery.GetSingletonEntity();
            }
        }

        protected override void OnUpdate()
        {
            if (m_EventEntity == Entity.Null || !EntityManager.Exists(m_EventEntity))
            {
                if (m_EventBufferQuery.IsEmptyIgnoreFilter)
                {
                    return;
                }

                m_EventEntity = m_EventBufferQuery.GetSingletonEntity();
            }

            DynamicBuffer<DetectedLaneTransitionViolation> events =
                EntityManager.GetBuffer<DetectedLaneTransitionViolation>(m_EventEntity);

            if (events.Length == 0)
            {
                return;
            }

            TrafficLawEnforcementStatistics statistics =
                EntityManager.GetComponentData<TrafficLawEnforcementStatistics>(m_StatisticsEntity);

            bool statisticsChanged = false;

            for (int index = 0; index < events.Length; index += 1)
            {
                DetectedLaneTransitionViolation evt = events[index];

                switch (evt.Kind)
                {
                    case LaneTransitionViolationKind.MidBlockCrossing:
                    {
                        int midBlockViolationCountBefore =
                            statistics.m_MidBlockCrossingViolationCount;
                        statistics.m_MidBlockCrossingViolationCount += 1;
                        statisticsChanged = true;

                        string reason = FormatMidBlockReason(evt.ReasonCode);

                        EnforcementPenaltyService.RecordMidBlockCrossingViolation(
                            evt.Vehicle,
                            evt.Lane,
                            reason);
                        bool illegalEgressMarkerWritten =
                            TryRecordAppliedIllegalEgressMarker(evt);
                        MaybeLogOrdinaryEgressApplyResult(
                            evt,
                            illegalEgressMarkerWritten);

                        MaybeLogRealizedOppositeFlowApply(
                            evt.Vehicle,
                            evt.ReasonCode,
                            midBlockViolationCountBefore,
                            statistics.m_MidBlockCrossingViolationCount);
                        MaybeLogRealizedAccessApply(
                            evt.Vehicle,
                            evt.ReasonCode,
                            midBlockViolationCountBefore,
                            statistics.m_MidBlockCrossingViolationCount);

                        if (EnforcementLoggingPolicy.ShouldLogVehicleSpecificEnforcementEvent(evt.Vehicle))
                        {
                            string message =
                                $"Mid-block crossing violation #{statistics.m_MidBlockCrossingViolationCount}: " +
                                $"vehicle={FocusedLoggingService.FormatEntity(evt.Vehicle)}, " +
                                $"lane={FocusedLoggingService.FormatEntity(evt.Lane)}, " +
                                $"reason={reason}";
                            EnforcementLoggingPolicy.RecordEnforcementEvent(message, evt.Vehicle);
                        }
                        break;
                    }

                    case LaneTransitionViolationKind.IntersectionMovement:
                    {
                        statistics.m_IntersectionMovementViolationCount += 1;
                        statisticsChanged = true;

                        string reason =
                            $"actual {evt.ActualMovement}, allowed {evt.AllowedMovement}";

                        EnforcementPenaltyService.RecordIntersectionMovementViolation(
                            evt.Vehicle,
                            evt.Lane,
                            reason);

                        if (EnforcementLoggingPolicy.ShouldLogVehicleSpecificEnforcementEvent(evt.Vehicle))
                        {
                            string message =
                                $"Intersection movement violation #{statistics.m_IntersectionMovementViolationCount}: " +
                                $"vehicle={FocusedLoggingService.FormatEntity(evt.Vehicle)}, " +
                                $"lane={FocusedLoggingService.FormatEntity(evt.Lane)}, " +
                                $"actual={evt.ActualMovement}, allowed={evt.AllowedMovement}";
                            EnforcementLoggingPolicy.RecordEnforcementEvent(message, evt.Vehicle);
                        }
                        break;
                    }
                }
            }

            if (statisticsChanged)
            {
                EntityManager.SetComponentData(m_StatisticsEntity, statistics);
            }

            EnforcementTelemetry.SetStatistics(statistics);
            events.Clear();
        }

        private static string FormatMidBlockReason(LaneTransitionViolationReasonCode reasonCode)
        {
            switch (reasonCode)
            {
                case LaneTransitionViolationReasonCode.OppositeFlowSameRoadSegment:
                    return "vehicle switched to the opposite flow on the same road segment";

                case LaneTransitionViolationReasonCode.EnteredGarageAccessWithoutSideAccess:
                    return "vehicle entered garage access from a lane without side-access permission";

                case LaneTransitionViolationReasonCode.EnteredParkingAccessWithoutSideAccess:
                    return "vehicle entered parking access from a lane without side-access permission";

                case LaneTransitionViolationReasonCode.EnteredParkingConnectionWithoutSideAccess:
                    return "vehicle crossed into parking connection from a lane without side-access permission";

                case LaneTransitionViolationReasonCode.EnteredBuildingAccessConnectionWithoutSideAccess:
                    return "vehicle crossed into building/service access connection from a lane without side-access permission";

                case LaneTransitionViolationReasonCode.ExitedParkingAccessWithoutSideAccess:
                    return "vehicle exited parking access into a lane without side-access permission";

                case LaneTransitionViolationReasonCode.ExitedGarageAccessWithoutSideAccess:
                    return "vehicle exited garage access into a lane without side-access permission";

                case LaneTransitionViolationReasonCode.ExitedParkingConnectionWithoutSideAccess:
                    return "vehicle exited parking connection into a lane without side-access permission";

                case LaneTransitionViolationReasonCode.ExitedBuildingAccessConnectionWithoutSideAccess:
                    return "vehicle exited building/service access connection into a lane without side-access permission";

                default:
                    return "lane transition violation";
            }
        }

        // Debug-only apply confirmation for realized OppositeFlowSameRoadSegment.
        // This does not alter violation application behavior.
        private static void MaybeLogRealizedOppositeFlowApply(
            Entity vehicle,
            LaneTransitionViolationReasonCode reasonCode,
            int violationCountBefore,
            int violationCountAfter)
        {
            if (reasonCode != LaneTransitionViolationReasonCode.OppositeFlowSameRoadSegment ||
                !EnforcementLoggingPolicy.ShouldLogVehicleSpecificEnforcementEvent(vehicle))
            {
                return;
            }

            string message =
                "[OPPFLOW_REALIZED_APPLY] " +
                $"vehicle={FocusedLoggingService.FormatEntity(vehicle)} " +
                $"reason={reasonCode} " +
                "applyHappened=true " +
                $"midBlockViolationCount={violationCountBefore}->{violationCountAfter}";

            EnforcementLoggingPolicy.RecordEnforcementEvent(message, vehicle);
        }

        // Debug-only apply confirmation for realized illegal ingress/egress.
        // This confirms that the buffered realized access event was later applied.
        // This does not alter violation application behavior.
        private static void MaybeLogRealizedAccessApply(
            Entity vehicle,
            LaneTransitionViolationReasonCode reasonCode,
            int violationCountBefore,
            int violationCountAfter)
        {
            if (!MidBlockCrossingPolicy.IsAccessTransitionReason(reasonCode) ||
                !EnforcementLoggingPolicy.ShouldLogVehicleSpecificEnforcementEvent(vehicle))
            {
                return;
            }

            string message =
                "[ACCESS_REALIZED_APPLY] " +
                $"vehicle={FocusedLoggingService.FormatEntity(vehicle)} " +
                $"reason={reasonCode} " +
                "applyHappened=true " +
                $"midBlockViolationCount={violationCountBefore}->{violationCountAfter}";

            EnforcementLoggingPolicy.RecordEnforcementEvent(message, vehicle);
        }

        private static bool TryRecordAppliedIllegalEgressMarker(
            DetectedLaneTransitionViolation evt)
        {
            if (!IsIllegalEgressReason(evt.ReasonCode) ||
                evt.IllegalEgressMode == IllegalEgressApplyMode.None ||
                evt.Vehicle == Entity.Null ||
                evt.IllegalEgressOriginLane == Entity.Null ||
                evt.IllegalEgressRoadLane == Entity.Null)
            {
                return false;
            }

            EnforcementTelemetry.RecordAppliedIllegalEgressMarker(
                evt.Vehicle.Index,
                evt.IllegalEgressMode,
                evt.IllegalEgressOriginLane.Index,
                evt.IllegalEgressRoadLane.Index);
            return true;
        }

        private static void MaybeLogOrdinaryEgressApplyResult(
            DetectedLaneTransitionViolation evt,
            bool markerWritten)
        {
            if (!IsIllegalEgressReason(evt.ReasonCode) ||
                !EnforcementLoggingPolicy.ShouldLogVehicleSpecificEnforcementEvent(evt.Vehicle))
            {
                return;
            }

            string branch = evt.IllegalEgressMode switch
            {
                IllegalEgressApplyMode.Direct => "Direct",
                IllegalEgressApplyMode.Carried => "Carried",
                _ => "None",
            };

            string message =
                "[ACCESS_EGRESS_APPLY_RESULT] " +
                $"vehicle={FocusedLoggingService.FormatEntity(evt.Vehicle)} " +
                $"branch={branch} " +
                $"storedOriginLane={FocusedLoggingService.FormatEntity(evt.IllegalEgressOriginLane)} " +
                $"previousLane={FocusedLoggingService.FormatEntity(evt.PreviousLane)} " +
                $"currentLane={FocusedLoggingService.FormatEntity(evt.Lane)} " +
                $"previousOwner={FocusedLoggingService.FormatEntity(evt.PreviousOwner)} " +
                $"currentOwner={FocusedLoggingService.FormatEntity(evt.CurrentOwner)} " +
                "candidateFormed=True " +
                "legalityEvaluated=True " +
                "legalityResult=Illegal " +
                "buffered=True " +
                "applied=True " +
                $"reasonCode={evt.ReasonCode} " +
                $"dropReason={(markerWritten ? "None" : "MarkerWriteSkipped")}";

            EnforcementLoggingPolicy.RecordEnforcementEvent(message, evt.Vehicle);
        }

        private static bool IsIllegalEgressReason(LaneTransitionViolationReasonCode reasonCode)
        {
            switch (reasonCode)
            {
                case LaneTransitionViolationReasonCode.ExitedParkingAccessWithoutSideAccess:
                case LaneTransitionViolationReasonCode.ExitedGarageAccessWithoutSideAccess:
                case LaneTransitionViolationReasonCode.ExitedParkingConnectionWithoutSideAccess:
                case LaneTransitionViolationReasonCode.ExitedBuildingAccessConnectionWithoutSideAccess:
                    return true;

                default:
                    return false;
            }
        }
    }
}
