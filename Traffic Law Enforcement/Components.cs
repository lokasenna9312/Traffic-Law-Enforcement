using Game.Prefabs;
using Game.Vehicles;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public struct PublicTransportLaneViolation : IComponentData
    {
        public Entity m_Lane;
        public long m_StartDayTicks;
        public bool m_ExitPressureApplied;
    }
    public struct PublicTransportLaneType2UsageState : IComponentData
    {
        public Entity m_Lane;
    }

    public struct PublicTransportLaneType4UsageState : IComponentData
    {
        public Entity m_Lane;
    }
    public struct LaneTransitionAnalysisState : IComponentData
    {
        public byte m_LastProcessedLaneChangeCount;
        public byte m_PendingOrdinaryEgressCorridorFailsafeBudget;
        public Entity m_PendingOrdinaryEgressOriginLane;
        public byte m_PendingBuildingServiceIngressCorridorFailsafeBudget;
        public Entity m_PendingBuildingServiceIngressRoadLane;
        public Entity m_PendingBuildingServiceIngressEntryLane;
        public Entity m_PendingBuildingServiceIngressTarget;
        public byte m_PendingBuildingServiceEgressCorridorFailsafeBudget;
        public Entity m_PendingBuildingServiceEgressOriginLane;
        public Entity m_PendingGarageConnectionEgressBridgeConnectionLane;
        public Entity m_PendingGarageConnectionEgressBridgeOriginLane;
    }

    public struct OriginalPathfindCarData : IComponentData
    {
        public PathfindCarData m_Value;
    }

    public struct PublicTransportLanePermissionState : IComponentData
    {
        public PublicTransportLaneAccessBits m_PublicTransportLaneAccessBits;
        public CarFlags m_OriginalPublicTransportLaneFlags;
        public byte m_EmergencyActive;
        public byte m_ImmediateEntryGraceConsumed;
    }

    public struct PublicTransportLaneType3UsageState : IComponentData
    {
        public Entity m_Lane;
    }


    public struct CenterlineAccessObsoleteState : IComponentData
    {
        public Entity m_LastCurrentLane;
        public Entity m_LastSourceLane;
        public Entity m_LastTargetLane;
        public int m_LastAccessIndex;
        public byte m_LastEvaluationResult;
        public byte m_LastTransitionFamily;
        public byte m_AwaitingPathRefresh;
    }

    public struct CenterlineAccessOriginWatch : IComponentData
    {
    }

    public struct TrafficLawEnforcementStatistics : IComponentData
    {
        public int m_PublicTransportLaneViolationCount;
        public int m_ActivePublicTransportLaneViolatorCount;
        public int m_MidBlockCrossingViolationCount;
        public int m_IntersectionMovementViolationCount;
    }


    public struct VehicleLaneHistory : IComponentData
    {
        public Entity m_PreviousLane;
        public Entity m_CurrentLane;
        public Entity m_PreviousLaneOwner;
        public Entity m_CurrentLaneOwner;
        public byte m_LaneChangeCount;
    }
}
