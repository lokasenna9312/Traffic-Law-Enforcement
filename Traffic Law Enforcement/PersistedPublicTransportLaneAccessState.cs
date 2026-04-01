using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public struct PersistedPublicTransportLaneAccessState : IComponentData
    {
        public byte m_ShouldTrack;
        public byte m_EmergencyVehicle;
        public PublicTransportLaneAccessBits m_AccessBits;
    }
}