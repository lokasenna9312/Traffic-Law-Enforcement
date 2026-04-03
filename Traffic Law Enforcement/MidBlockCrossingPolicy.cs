using Game.Net;
using Game.Common;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public static class MidBlockCrossingPolicy
    {
        [System.Flags]
        private enum AccessSide : byte
        {
            None = 0,
            Left = 1 << 0,
            Right = 1 << 1,
        }

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

            if (!TryGetRoadCarLane(entityManager, sourceLane, out CarLane sourceCarLane))
            {
                return false;
            }

            if (entityManager.HasComponent<GarageLane>(targetLane))
            {
                if (TryGetConnectionLane(entityManager, targetLane, out ConnectionLane garageConnectionLane))
                {
                    if (RoadAllowsGarageAccess(sourceCarLane, garageConnectionLane))
                    {
                        return false;
                    }
                }
                else if (RoadHasGenericSideConnection(sourceCarLane))
                {
                    return false;
                }

                reasonCode = LaneTransitionViolationReasonCode.EnteredGarageAccessWithoutSideAccess;
                return true;
            }

            if (entityManager.HasComponent<ParkingLane>(targetLane))
            {
                ParkingLane parkingLane = entityManager.GetComponentData<ParkingLane>(targetLane);
                if (RoadAllowsParkingAccess(sourceCarLane, parkingLane))
                {
                    return false;
                }

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
                if (RoadAllowsParkingConnectionAccess(sourceCarLane, connectionLane))
                {
                    return false;
                }

                reasonCode = LaneTransitionViolationReasonCode.EnteredParkingConnectionWithoutSideAccess;
                return true;
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0)
            {
                if (!IsInsideAccessConnection(connectionLane))
                {
                    return false;
                }

                if (RoadAllowsBuildingAccess(sourceCarLane))
                {
                    return false;
                }

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
                !TryGetRoadCarLane(entityManager, targetLane, out CarLane targetCarLane))
            {
                return false;
            }

            if (entityManager.HasComponent<ParkingLane>(sourceLane))
            {
                ParkingLane parkingLane = entityManager.GetComponentData<ParkingLane>(sourceLane);
                if (RoadAllowsParkingAccess(targetCarLane, parkingLane))
                {
                    return false;
                }

                reasonCode = LaneTransitionViolationReasonCode.ExitedParkingAccessWithoutSideAccess;
                return true;
            }

            if (entityManager.HasComponent<GarageLane>(sourceLane))
            {
                if (TryGetConnectionLane(entityManager, sourceLane, out ConnectionLane garageConnectionLane))
                {
                    if (RoadAllowsGarageAccess(targetCarLane, garageConnectionLane))
                    {
                        return false;
                    }
                }
                else if (RoadHasGenericSideConnection(targetCarLane))
                {
                    return false;
                }

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
                if (RoadAllowsParkingConnectionAccess(targetCarLane, connectionLane))
                {
                    return false;
                }

                reasonCode = LaneTransitionViolationReasonCode.ExitedParkingConnectionWithoutSideAccess;
                return true;
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0)
            {
                if (!IsInsideAccessConnection(connectionLane))
                {
                    return false;
                }

                if (RoadAllowsBuildingAccess(targetCarLane))
                {
                    return false;
                }

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

        private static bool TryGetRoadCarLane(
            EntityManager entityManager,
            Entity lane,
            out CarLane carLane)
        {
            if (entityManager.HasComponent<EdgeLane>(lane) &&
                entityManager.HasComponent<CarLane>(lane))
            {
                carLane = entityManager.GetComponentData<CarLane>(lane);
                return true;
            }

            carLane = default;
            return false;
        }

        private static bool TryGetConnectionLane(
            EntityManager entityManager,
            Entity lane,
            out ConnectionLane connectionLane)
        {
            if (entityManager.HasComponent<ConnectionLane>(lane))
            {
                connectionLane = entityManager.GetComponentData<ConnectionLane>(lane);
                return true;
            }

            connectionLane = default;
            return false;
        }

        // Patch 1.5 keeps the classifier exact-pair based. It refines patch 1 by
        // treating ConnectionLaneFlags.Inside as the primary signal that a direct
        // access connection is internal-facing. Parking-side flags alone no longer
        // legalize those Inside connection pairs.
        private static bool RoadHasGenericSideConnection(CarLane lane)
        {
            return (lane.m_Flags & CarLaneFlags.SideConnection) != 0;
        }

        private static bool IsInsideAccessConnection(ConnectionLane connectionLane)
        {
            return (connectionLane.m_Flags & ConnectionLaneFlags.Inside) != 0;
        }

        private static bool RoadAllowsBuildingAccess(CarLane roadLane)
        {
            return RoadHasGenericSideConnection(roadLane);
        }

        private static bool RoadAllowsGarageAccess(CarLane roadLane, ConnectionLane connectionLane)
        {
            if (RoadHasGenericSideConnection(roadLane))
            {
                return true;
            }

            if (IsInsideAccessConnection(connectionLane))
            {
                return false;
            }

            return (connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0 &&
                GetRoadParkingSides(roadLane) != AccessSide.None;
        }

        private static bool RoadAllowsParkingConnectionAccess(
            CarLane roadLane,
            ConnectionLane connectionLane)
        {
            if (RoadHasGenericSideConnection(roadLane))
            {
                return true;
            }

            if (IsInsideAccessConnection(connectionLane))
            {
                return false;
            }

            return GetRoadParkingSides(roadLane) != AccessSide.None;
        }

        private static bool RoadAllowsParkingAccess(CarLane roadLane, ParkingLane parkingLane)
        {
            if (RoadHasGenericSideConnection(roadLane))
            {
                return true;
            }

            AccessSide roadSides = GetRoadParkingSides(roadLane);
            if (roadSides == AccessSide.None)
            {
                return false;
            }

            AccessSide parkingSides = GetParkingSides(parkingLane);
            return parkingSides == AccessSide.None ||
                (roadSides & parkingSides) != 0;
        }

        private static AccessSide GetRoadParkingSides(CarLane lane)
        {
            AccessSide sides = AccessSide.None;
            if ((lane.m_Flags & CarLaneFlags.ParkingLeft) != 0)
            {
                sides |= AccessSide.Left;
            }

            if ((lane.m_Flags & CarLaneFlags.ParkingRight) != 0)
            {
                sides |= AccessSide.Right;
            }

            return sides;
        }

        private static AccessSide GetParkingSides(ParkingLane lane)
        {
            AccessSide sides = AccessSide.None;
            if ((lane.m_Flags & (ParkingLaneFlags.LeftSide | ParkingLaneFlags.ParkingLeft)) != 0)
            {
                sides |= AccessSide.Left;
            }

            if ((lane.m_Flags & (ParkingLaneFlags.RightSide | ParkingLaneFlags.ParkingRight)) != 0)
            {
                sides |= AccessSide.Right;
            }

            return sides;
        }
    }
}
