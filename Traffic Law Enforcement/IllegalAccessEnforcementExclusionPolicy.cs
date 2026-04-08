using Game.Vehicles;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    internal static class IllegalAccessEnforcementExclusionPolicy
    {
        public static bool IsExcluded(
            Entity vehicle,
            Car car,
            ref PublicTransportLaneVehicleTypeLookups lookups)
        {
            if (EmergencyVehiclePolicy.IsEmergencyVehicle(car))
            {
                return true;
            }

            PublicTransportLaneVehicleCategory categories =
                PublicTransportLanePolicy.GetVanillaAuthorizedCategories(
                    vehicle,
                    ref lookups);

            if ((categories & PublicTransportLaneVehicleCategory.RoadPublicTransportVehicle) != 0)
            {
                return true;
            }

            PublicTransportLaneVehicleCategory excludedMaintenanceCategories =
                PublicTransportLaneVehicleCategory.RoadMaintenanceVehicle |
                PublicTransportLaneVehicleCategory.Snowplow |
                PublicTransportLaneVehicleCategory.VehicleMaintenanceVehicle;

            if ((categories & excludedMaintenanceCategories) != 0)
            {
                return true;
            }

            PublicTransportLaneFlagGrantExperimentRole role =
                PublicTransportLanePolicy.GetFlagGrantExperimentRole(
                    vehicle,
                    ref lookups);

            switch (role)
            {
                case PublicTransportLaneFlagGrantExperimentRole.CargoTransport:
                case PublicTransportLaneFlagGrantExperimentRole.PrisonerTransport:
                    return true;

                default:
                    return false;
            }
        }
    }
}
