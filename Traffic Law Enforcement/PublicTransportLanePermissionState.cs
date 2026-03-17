using Game.Vehicles;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public struct PublicTransportLanePermissionState : IComponentData
    {
        public CarFlags m_OriginalPublicTransportLaneFlags;
        public byte m_EmergencyActive;
    }
}
