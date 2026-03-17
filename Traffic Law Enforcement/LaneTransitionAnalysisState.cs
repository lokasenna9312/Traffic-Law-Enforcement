using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public struct LaneTransitionAnalysisState : IComponentData
    {
        public byte m_LastProcessedLaneChangeCount;
    }
}