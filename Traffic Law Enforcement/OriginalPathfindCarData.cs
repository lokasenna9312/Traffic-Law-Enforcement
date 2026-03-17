using Game.Prefabs;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public struct OriginalPathfindCarData : IComponentData
    {
        public PathfindCarData m_Value;
    }
}
