using Game.Vehicles;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public static class EmergencyVehiclePolicy
    {
        public static bool IsEmergencyVehicle(Car car)
        {
            return (car.m_Flags & CarFlags.Emergency) != 0;
        }

        public static bool IsEmergencyVehicle(Entity vehicle, ref BusLaneVehicleTypeLookups lookups)
        {
            return lookups.CarData.TryGetComponent(vehicle, out Car car) && IsEmergencyVehicle(car);
        }
    }
}
