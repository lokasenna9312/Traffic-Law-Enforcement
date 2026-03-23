using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public struct PublicTransportLanePendingExit : IComponentData
    {
        public Entity m_LaneWhenGraceGranted;
        public byte m_HasLeftPublicTransportLane;
    }
}