using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public struct VehicleLaneHistory : IComponentData
    {
        public Entity m_PreviousLane;
        public Entity m_CurrentLane;
        public Entity m_PreviousLaneOwner;
        public Entity m_CurrentLaneOwner;
        public byte m_LaneChangeCount;
    }
}