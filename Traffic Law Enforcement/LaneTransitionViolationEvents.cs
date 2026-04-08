using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public enum LaneTransitionViolationKind : byte
    {
        None = 0,
        MidBlockCrossing = 1,
        IntersectionMovement = 2,
    }

    public enum LaneTransitionViolationReasonCode : byte
    {
        None = 0,
        OppositeFlowSameRoadSegment = 1,
        EnteredGarageAccessWithoutSideAccess = 2,
        EnteredParkingAccessWithoutSideAccess = 3,
        EnteredParkingConnectionWithoutSideAccess = 4,
        EnteredBuildingAccessConnectionWithoutSideAccess = 5,
        ExitedParkingAccessWithoutSideAccess = 6,
        ExitedGarageAccessWithoutSideAccess = 7,
        ExitedParkingConnectionWithoutSideAccess = 8,
        ExitedBuildingAccessConnectionWithoutSideAccess = 9,
    }

    public struct DetectedLaneTransitionViolation : IBufferElementData
    {
        public Entity Vehicle;
        public Entity Lane;
        public Entity PreviousLane;
        public Entity PreviousOwner;
        public Entity CurrentOwner;
        public LaneTransitionViolationKind Kind;
        public LaneTransitionViolationReasonCode ReasonCode;
        public IllegalEgressApplyMode IllegalEgressMode;
        public Entity IllegalEgressOriginLane;
        public Entity IllegalEgressRoadLane;
        public LaneMovement ActualMovement;
        public LaneMovement AllowedMovement;
    }
    public struct LaneTransitionViolationEventBufferTag : IComponentData
    {
    }
}
