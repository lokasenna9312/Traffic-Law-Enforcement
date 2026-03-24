using Game.Vehicles;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public struct VehicleTrafficLawProfile : IComponentData
    {
        public byte m_ShouldTrack;
        public byte m_EmergencyVehicle;
        public PublicTransportLaneAccessBits m_PublicTransportLaneAccessBits;
        public PublicTransportLaneVehicleCategory m_VanillaAuthorizedCategories;
        public PublicTransportLaneFlagGrantExperimentRole m_AdditionalRole;
        public int m_PermissionSettingsMask;
    }
}
