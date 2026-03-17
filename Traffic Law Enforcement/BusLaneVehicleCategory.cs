using System;

namespace Traffic_Law_Enforcement
{
    [Flags]
    public enum BusLaneVehicleCategory : ushort
    {
        None = 0,
        RoadPublicTransportVehicle = 1 << 0,
        Taxi = 1 << 1,
        PoliceCar = 1 << 2,
        FireEngine = 1 << 3,
        Ambulance = 1 << 4,
        GarbageTruck = 1 << 5,
        PostVan = 1 << 6,
        RoadMaintenanceVehicle = 1 << 7,
        Snowplow = 1 << 8,
        VehicleMaintenanceVehicle = 1 << 9,
    }
}