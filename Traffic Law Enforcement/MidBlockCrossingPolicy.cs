using Game.Net;
using Game.Common;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public static class MidBlockCrossingPolicy
    {
        public static bool TryGetIllegalTransition(
            EntityManager entityManager,
            Entity sourceLane,
            Entity targetLane,
            out LaneTransitionViolationReasonCode reasonCode)
        {
            reasonCode = LaneTransitionViolationReasonCode.None;

            if (sourceLane == Entity.Null || targetLane == Entity.Null || sourceLane == targetLane)
            {
                return false;
            }

            if (TryDetectOppositeFlowSameRoadSegment(
                    entityManager,
                    sourceLane,
                    targetLane,
                    out reasonCode))
            {
                return true;
            }

            if (TryDetectIllegalIngress(
                    entityManager,
                    sourceLane,
                    targetLane,
                    out reasonCode))
            {
                return true;
            }

            if (TryDetectIllegalEgress(
                    entityManager,
                    sourceLane,
                    targetLane,
                    out reasonCode))
            {
                return true;
            }

            return false;
        }

        public static bool TryGetIllegalAccessTransition(
            EntityManager entityManager,
            Entity sourceLane,
            Entity targetLane,
            out LaneTransitionViolationReasonCode reasonCode)
        {
            reasonCode = LaneTransitionViolationReasonCode.None;

            if (sourceLane == Entity.Null || targetLane == Entity.Null || sourceLane == targetLane)
            {
                return false;
            }

            if (TryDetectIllegalIngress(
                    entityManager,
                    sourceLane,
                    targetLane,
                    out reasonCode))
            {
                return true;
            }

            return TryDetectIllegalEgress(
                entityManager,
                sourceLane,
                targetLane,
                out reasonCode);
        }

        public static bool IsAccessTransitionReason(
            LaneTransitionViolationReasonCode reasonCode)
        {
            switch (reasonCode)
            {
                case LaneTransitionViolationReasonCode.EnteredGarageAccessWithoutSideAccess:
                case LaneTransitionViolationReasonCode.EnteredParkingAccessWithoutSideAccess:
                case LaneTransitionViolationReasonCode.EnteredParkingConnectionWithoutSideAccess:
                case LaneTransitionViolationReasonCode.EnteredBuildingAccessConnectionWithoutSideAccess:
                case LaneTransitionViolationReasonCode.ExitedParkingAccessWithoutSideAccess:
                case LaneTransitionViolationReasonCode.ExitedGarageAccessWithoutSideAccess:
                case LaneTransitionViolationReasonCode.ExitedParkingConnectionWithoutSideAccess:
                case LaneTransitionViolationReasonCode.ExitedBuildingAccessConnectionWithoutSideAccess:
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryDetectOppositeFlowSameRoadSegment(
            EntityManager entityManager,
            Entity sourceLane,
            Entity targetLane,
            out LaneTransitionViolationReasonCode reasonCode)
        {
            reasonCode = LaneTransitionViolationReasonCode.None;

            if (!entityManager.HasComponent<EdgeLane>(sourceLane) ||
                !entityManager.HasComponent<EdgeLane>(targetLane) ||
                !entityManager.HasComponent<CarLane>(sourceLane) ||
                !entityManager.HasComponent<CarLane>(targetLane))
            {
                return false;
            }

            EdgeLane sourceEdgeLane = entityManager.GetComponentData<EdgeLane>(sourceLane);
            EdgeLane targetEdgeLane = entityManager.GetComponentData<EdgeLane>(targetLane);
            CarLane sourceCarLane = entityManager.GetComponentData<CarLane>(sourceLane);
            CarLane targetCarLane = entityManager.GetComponentData<CarLane>(targetLane);

            Entity sourceOwner = GetOwner(entityManager, sourceLane);
            Entity targetOwner = GetOwner(entityManager, targetLane);

            bool sameOwner = sourceOwner != Entity.Null && sourceOwner == targetOwner;
            bool oppositeDirections = IsOppositeDirection(sourceEdgeLane, targetEdgeLane);
            bool sameCarriageway = sourceCarLane.m_CarriagewayGroup == targetCarLane.m_CarriagewayGroup;

            if (!sameOwner || !oppositeDirections || !sameCarriageway)
            {
                return false;
            }

            reasonCode = LaneTransitionViolationReasonCode.OppositeFlowSameRoadSegment;
            return true;
        }

        private static bool TryDetectIllegalIngress(
            EntityManager entityManager,
            Entity sourceLane,
            Entity targetLane,
            out LaneTransitionViolationReasonCode reasonCode)
        {
            reasonCode = LaneTransitionViolationReasonCode.None;

            if (!entityManager.HasComponent<EdgeLane>(sourceLane) ||
                !entityManager.HasComponent<CarLane>(sourceLane))
            {
                return false;
            }

            CarLane sourceCarLane = entityManager.GetComponentData<CarLane>(sourceLane);
            if (LaneAllowsSideAccess(sourceCarLane))
            {
                return false;
            }

            if (entityManager.HasComponent<GarageLane>(targetLane))
            {
                reasonCode = LaneTransitionViolationReasonCode.EnteredGarageAccessWithoutSideAccess;
                return true;
            }

            if (entityManager.HasComponent<ParkingLane>(targetLane))
            {
                reasonCode = LaneTransitionViolationReasonCode.EnteredParkingAccessWithoutSideAccess;
                return true;
            }

            if (!entityManager.HasComponent<ConnectionLane>(targetLane))
            {
                return false;
            }

            ConnectionLane connectionLane = entityManager.GetComponentData<ConnectionLane>(targetLane);

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
            {
                reasonCode = LaneTransitionViolationReasonCode.EnteredParkingConnectionWithoutSideAccess;
                return true;
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0)
            {
                reasonCode = LaneTransitionViolationReasonCode.EnteredBuildingAccessConnectionWithoutSideAccess;
                return true;
            }

            return false;
        }

        private static bool TryDetectIllegalEgress(
            EntityManager entityManager,
            Entity sourceLane,
            Entity targetLane,
            out LaneTransitionViolationReasonCode reasonCode)
        {
            reasonCode = LaneTransitionViolationReasonCode.None;

            if (!IsAccessOrigin(entityManager, sourceLane) ||
                !entityManager.HasComponent<EdgeLane>(targetLane) ||
                !entityManager.HasComponent<CarLane>(targetLane))
            {
                return false;
            }

            CarLane targetCarLane = entityManager.GetComponentData<CarLane>(targetLane);
            if (LaneAllowsSideAccess(targetCarLane))
            {
                return false;
            }

            if (entityManager.HasComponent<ParkingLane>(sourceLane))
            {
                reasonCode = LaneTransitionViolationReasonCode.ExitedParkingAccessWithoutSideAccess;
                return true;
            }

            if (entityManager.HasComponent<GarageLane>(sourceLane))
            {
                reasonCode = LaneTransitionViolationReasonCode.ExitedGarageAccessWithoutSideAccess;
                return true;
            }

            if (!entityManager.HasComponent<ConnectionLane>(sourceLane))
            {
                return false;
            }

            ConnectionLane connectionLane = entityManager.GetComponentData<ConnectionLane>(sourceLane);

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
            {
                reasonCode = LaneTransitionViolationReasonCode.ExitedParkingConnectionWithoutSideAccess;
                return true;
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0)
            {
                reasonCode = LaneTransitionViolationReasonCode.ExitedBuildingAccessConnectionWithoutSideAccess;
                return true;
            }

            return false;
        }

        private static bool IsAccessOrigin(EntityManager entityManager, Entity lane)
        {
            if (entityManager.HasComponent<ParkingLane>(lane) ||
                entityManager.HasComponent<GarageLane>(lane))
            {
                return true;
            }

            if (!entityManager.HasComponent<ConnectionLane>(lane))
            {
                return false;
            }

            ConnectionLane connectionLane = entityManager.GetComponentData<ConnectionLane>(lane);
            bool parkingAccess = (connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0;
            bool roadConnection = (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0;
            return parkingAccess || !roadConnection;
        }

        private static Entity GetOwner(EntityManager entityManager, Entity lane)
        {
            if (entityManager.HasComponent<Owner>(lane))
            {
                return entityManager.GetComponentData<Owner>(lane).m_Owner;
            }

            return Entity.Null;
        }

        private static bool IsOppositeDirection(EdgeLane sourceLane, EdgeLane targetLane)
        {
            float sourceDirection = sourceLane.m_EdgeDelta.y - sourceLane.m_EdgeDelta.x;
            float targetDirection = targetLane.m_EdgeDelta.y - targetLane.m_EdgeDelta.x;
            return sourceDirection * targetDirection < 0f;
        }

        private static bool LaneAllowsSideAccess(CarLane lane)
        {
            return (lane.m_Flags &
                (CarLaneFlags.SideConnection |
                 CarLaneFlags.ParkingLeft |
                 CarLaneFlags.ParkingRight)) != 0;
        }
    }
}
