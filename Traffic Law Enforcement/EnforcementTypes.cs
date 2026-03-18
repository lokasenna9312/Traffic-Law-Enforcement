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

    public enum BusLaneFlagGrantExperimentRole : byte
    {
        None = 0,
        PersonalCar = 1,
        DeliveryTruck = 2,
        CargoTransport = 3,
        Hearse = 4,
        PrisonerTransport = 5,
        ParkMaintenanceVehicle = 6,
    }

    public static class BusLaneFlagGrantExperimentRoleInfo
    {
        public static BusLaneFlagGrantExperimentRole Clamp(int value)
        {
            if (value < (int)BusLaneFlagGrantExperimentRole.None || value > (int)BusLaneFlagGrantExperimentRole.ParkMaintenanceVehicle)
            {
                return BusLaneFlagGrantExperimentRole.None;
            }

            return (BusLaneFlagGrantExperimentRole)value;
        }

        public static string ToDisplayName(BusLaneFlagGrantExperimentRole role, bool korean = false)
        {
            switch (role)
            {
                case BusLaneFlagGrantExperimentRole.PersonalCar:
                    return korean ? "\uac1c\uc778 \uc2b9\uc6a9\ucc28" : "Personal cars";
                case BusLaneFlagGrantExperimentRole.DeliveryTruck:
                    return korean ? "\ubc30\ub2ec \ud2b8\ub7ed" : "Delivery trucks";
                case BusLaneFlagGrantExperimentRole.CargoTransport:
                    return korean ? "\ud654\ubb3c \uc6b4\uc1a1 \ucc28\ub7c9" : "Cargo transport vehicles";
                case BusLaneFlagGrantExperimentRole.Hearse:
                    return korean ? "\uc601\uad6c\ucc28" : "Hearses";
                case BusLaneFlagGrantExperimentRole.PrisonerTransport:
                    return korean ? "\uc8c4\uc218 \uc774\uc1a1\ucc28" : "Prisoner transports";
                case BusLaneFlagGrantExperimentRole.ParkMaintenanceVehicle:
                    return korean ? "\uacf5\uc6d0 \uc815\ube44 \ucc28\ub7c9" : "Park maintenance vehicles";
                default:
                    return korean ? "\uc5c6\uc74c" : "None";
            }
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
