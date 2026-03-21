using System;

namespace Traffic_Law_Enforcement
{
    public static class EnforcementKinds
    {
        public const string PublicTransportLane = "Bus lane violation";
        public const string MidBlockCrossing = "Mid-block crossing";
        public const string IntersectionMovement = "Intersection movement";
    }

    [Flags]
    public enum LaneMovement : byte
    {
        None = 0,
        Forward = 1 << 0,
        Left = 1 << 1,
        Right = 1 << 2,
        UTurn = 1 << 3,
    }

    [Flags]
    public enum PublicTransportLaneVehicleCategory : ushort
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

    public enum PublicTransportLaneFlagGrantExperimentRole : byte
    {
        None = 0,
        PersonalCar = 1,
        DeliveryTruck = 2,
        CargoTransport = 3,
        Hearse = 4,
        PrisonerTransport = 5,
        ParkMaintenanceVehicle = 6,
    }

    public static class PublicTransportLaneFlagGrantExperimentRoleInfo
    {
        public static PublicTransportLaneFlagGrantExperimentRole Clamp(int value)
        {
            if (value < (int)PublicTransportLaneFlagGrantExperimentRole.None || value > (int)PublicTransportLaneFlagGrantExperimentRole.ParkMaintenanceVehicle)
            {
                return PublicTransportLaneFlagGrantExperimentRole.None;
            }

            return (PublicTransportLaneFlagGrantExperimentRole)value;
        }

    }

    public readonly struct EnforcementRecord
    {
        public readonly string Kind;
        public readonly int VehicleId;
        public readonly int LaneId;
        public readonly int FineAmount;
        public readonly string Reason;

        public string kind => Kind;
        public int vehicleId => VehicleId;
        public int laneId => LaneId;
        public int fineAmount => FineAmount;
        public string reason => Reason;

        public EnforcementRecord(string kind, int vehicleId, int laneId, int fineAmount, string reason)
        {
            Kind = kind;
            VehicleId = vehicleId;
            LaneId = laneId;
            FineAmount = fineAmount;
            Reason = reason;
        }

        public override string ToString()
        {
            return $"{Kind} | vehicle {VehicleId} | lane {LaneId} | fine {FineAmount} | {Reason}";
        }
    }
}
