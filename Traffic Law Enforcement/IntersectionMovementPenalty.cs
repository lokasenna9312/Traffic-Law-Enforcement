using System.Text;
using Game.Net;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    internal static class IntersectionMovementPolicy
    {
        public static bool TryGetIllegalIntersectionMovement(
            ComponentLookup<ConnectionLane> connectionLaneData,
            ComponentLookup<CarLane> carLaneData,
            Entity sourceLane,
            Entity targetLane,
            out LaneMovement actualMovement,
            out LaneMovement allowedMovement)
        {
            actualMovement = LaneMovement.None;
            allowedMovement = LaneMovement.None;

            if (!connectionLaneData.TryGetComponent(targetLane, out ConnectionLane connectionLane))
            {
                return false;
            }

            bool isRoadIntersectionConnection =
                (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0 &&
                (connectionLane.m_Flags & ConnectionLaneFlags.Parking) == 0;

            if (!isRoadIntersectionConnection)
            {
                return false;
            }

            if (!carLaneData.TryGetComponent(sourceLane, out CarLane sourceCarLane) ||
                !carLaneData.TryGetComponent(targetLane, out CarLane targetCarLane))
            {
                return false;
            }

            actualMovement = GetMovement(targetCarLane.m_Flags);
            allowedMovement = GetMovement(sourceCarLane.m_Flags);

            if (actualMovement == LaneMovement.None || allowedMovement == LaneMovement.None)
            {
                return false;
            }

            return (allowedMovement & actualMovement) == LaneMovement.None;
        }

        public static bool TryGetIllegalIntersectionMovement(
            EntityManager entityManager,
            Entity sourceLane,
            Entity targetLane,
            out LaneMovement actualMovement,
            out LaneMovement allowedMovement)
        {
            actualMovement = LaneMovement.None;
            allowedMovement = LaneMovement.None;

            if (sourceLane == Entity.Null || targetLane == Entity.Null)
            {
                return false;
            }

            if (!entityManager.HasComponent<ConnectionLane>(targetLane))
            {
                return false;
            }

            ConnectionLane connectionLane = entityManager.GetComponentData<ConnectionLane>(targetLane);
            bool isRoadIntersectionConnection =
                (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0 &&
                (connectionLane.m_Flags & ConnectionLaneFlags.Parking) == 0;

            if (!isRoadIntersectionConnection)
            {
                return false;
            }

            if (!entityManager.HasComponent<CarLane>(sourceLane) || !entityManager.HasComponent<CarLane>(targetLane))
            {
                return false;
            }

            CarLane sourceCarLane = entityManager.GetComponentData<CarLane>(sourceLane);
            CarLane targetCarLane = entityManager.GetComponentData<CarLane>(targetLane);

            actualMovement = GetMovement(targetCarLane.m_Flags);
            allowedMovement = GetMovement(sourceCarLane.m_Flags);

            if (actualMovement == LaneMovement.None || allowedMovement == LaneMovement.None)
            {
                return false;
            }

            return (allowedMovement & actualMovement) == LaneMovement.None;
        }

        public static LaneMovement GetMovement(CarLaneFlags flags)
        {
            LaneMovement movement = LaneMovement.None;

            if ((flags & CarLaneFlags.Forward) != 0)
            {
                movement |= LaneMovement.Forward;
            }

            if ((flags & (CarLaneFlags.TurnLeft | CarLaneFlags.GentleTurnLeft)) != 0)
            {
                movement |= LaneMovement.Left;
            }

            if ((flags & (CarLaneFlags.TurnRight | CarLaneFlags.GentleTurnRight)) != 0)
            {
                movement |= LaneMovement.Right;
            }

            if ((flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.UTurnRight)) != 0)
            {
                movement |= LaneMovement.UTurn;
            }

            return movement;
        }

        public static string FormatMovement(LaneMovement movement)
        {
            StringBuilder parts = new StringBuilder(24);
            bool hasParts = false;

            if ((movement & LaneMovement.Forward) != 0)
            {
                parts.Append("forward");
                hasParts = true;
            }

            if ((movement & LaneMovement.Left) != 0)
            {
                if (hasParts)
                {
                    parts.Append('+');
                }

                parts.Append("left");
                hasParts = true;
            }

            if ((movement & LaneMovement.Right) != 0)
            {
                if (hasParts)
                {
                    parts.Append('+');
                }

                parts.Append("right");
                hasParts = true;
            }

            if ((movement & LaneMovement.UTurn) != 0)
            {
                if (hasParts)
                {
                    parts.Append('+');
                }

                parts.Append("u-turn");
                hasParts = true;
            }

            return hasParts ? parts.ToString() : "none";
        }
    }
}
