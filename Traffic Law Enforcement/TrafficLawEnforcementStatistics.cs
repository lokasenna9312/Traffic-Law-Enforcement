using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public struct TrafficLawEnforcementStatistics : IComponentData
    {
        public int m_PublicTransportLaneViolationCount;
        public int m_ActivePublicTransportLaneViolatorCount;
        public int m_MidBlockCrossingViolationCount;
        public int m_IntersectionMovementViolationCount;
    }
}