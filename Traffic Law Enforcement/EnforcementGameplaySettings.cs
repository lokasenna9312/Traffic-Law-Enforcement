namespace Traffic_Law_Enforcement
{
    public struct EnforcementGameplaySettingsState
    {
        public bool EnablePublicTransportLaneEnforcement;
        public bool EnableMidBlockCrossingEnforcement;
        public bool EnableIntersectionMovementEnforcement;

        public bool AllowRoadPublicTransportVehicles;
        public bool AllowTaxis;
        public bool AllowPoliceCars;
        public bool AllowFireEngines;
        public bool AllowAmbulances;
        public bool AllowGarbageTrucks;
        public bool AllowPostVans;
        public bool AllowRoadMaintenanceVehicles;
        public bool AllowSnowplows;
        public bool AllowVehicleMaintenanceVehicles;
        public bool AllowPersonalCars;
        public bool AllowDeliveryTrucks;
        public bool AllowCargoTransportVehicles;
        public bool AllowHearses;
        public bool AllowPrisonerTransports;
        public bool AllowParkMaintenanceVehicles;

        public float PublicTransportLaneExitPressureThresholdDays;

        public int PublicTransportLaneFineAmount;
        public int MidBlockCrossingFineAmount;
        public int IntersectionMovementFineAmount;

        public bool EnablePublicTransportLaneRepeatPenalty;
        public int PublicTransportLaneRepeatWindowMonths;
        public int PublicTransportLaneRepeatThreshold;
        public int PublicTransportLaneRepeatMultiplierPercent;

        public bool EnableMidBlockCrossingRepeatPenalty;
        public int MidBlockCrossingRepeatWindowMonths;
        public int MidBlockCrossingRepeatThreshold;
        public int MidBlockCrossingRepeatMultiplierPercent;

        public bool EnableIntersectionMovementRepeatPenalty;
        public int IntersectionMovementRepeatWindowMonths;
        public int IntersectionMovementRepeatThreshold;
        public int IntersectionMovementRepeatMultiplierPercent;

        public static EnforcementGameplaySettingsState CreateCodeDefaults()
        {
            return new EnforcementGameplaySettingsState
            {
                EnablePublicTransportLaneEnforcement = true,
                EnableMidBlockCrossingEnforcement = true,
                EnableIntersectionMovementEnforcement = true,
                AllowRoadPublicTransportVehicles = true,
                AllowTaxis = true,
                AllowPoliceCars = true,
                AllowFireEngines = true,
                AllowAmbulances = true,
                AllowGarbageTrucks = true,
                AllowPostVans = true,
                AllowRoadMaintenanceVehicles = true,
                AllowSnowplows = true,
                AllowVehicleMaintenanceVehicles = true,
                AllowPersonalCars = false,
                AllowDeliveryTrucks = false,
                AllowCargoTransportVehicles = false,
                AllowHearses = false,
                AllowPrisonerTransports = false,
                AllowParkMaintenanceVehicles = false,
                PublicTransportLaneExitPressureThresholdDays = 0.01f,
                PublicTransportLaneFineAmount = EnforcementPenaltyService.DefaultPublicTransportLaneFine,
                MidBlockCrossingFineAmount = EnforcementPenaltyService.DefaultMidBlockCrossingFine,
                IntersectionMovementFineAmount = EnforcementPenaltyService.DefaultIntersectionMovementFine,
                EnablePublicTransportLaneRepeatPenalty = true,
                PublicTransportLaneRepeatWindowMonths = 1,
                PublicTransportLaneRepeatThreshold = 2,
                PublicTransportLaneRepeatMultiplierPercent = 150,
                EnableMidBlockCrossingRepeatPenalty = true,
                MidBlockCrossingRepeatWindowMonths = 1,
                MidBlockCrossingRepeatThreshold = 2,
                MidBlockCrossingRepeatMultiplierPercent = 150,
                EnableIntersectionMovementRepeatPenalty = true,
                IntersectionMovementRepeatWindowMonths = 1,
                IntersectionMovementRepeatThreshold = 2,
                IntersectionMovementRepeatMultiplierPercent = 150,
            };
        }

        public bool HasAnyEnforcementEnabled()
        {
            return EnablePublicTransportLaneEnforcement || EnableMidBlockCrossingEnforcement || EnableIntersectionMovementEnforcement;
        }

        public BusLaneVehicleCategory GetEnabledBusLaneCategories()
        {
            BusLaneVehicleCategory categories = BusLaneVehicleCategory.None;
            if (AllowRoadPublicTransportVehicles) categories |= BusLaneVehicleCategory.RoadPublicTransportVehicle;
            if (AllowTaxis) categories |= BusLaneVehicleCategory.Taxi;
            if (AllowPoliceCars) categories |= BusLaneVehicleCategory.PoliceCar;
            if (AllowFireEngines) categories |= BusLaneVehicleCategory.FireEngine;
            if (AllowAmbulances) categories |= BusLaneVehicleCategory.Ambulance;
            if (AllowGarbageTrucks) categories |= BusLaneVehicleCategory.GarbageTruck;
            if (AllowPostVans) categories |= BusLaneVehicleCategory.PostVan;
            if (AllowRoadMaintenanceVehicles) categories |= BusLaneVehicleCategory.RoadMaintenanceVehicle;
            if (AllowSnowplows) categories |= BusLaneVehicleCategory.Snowplow;
            if (AllowVehicleMaintenanceVehicles) categories |= BusLaneVehicleCategory.VehicleMaintenanceVehicle;
            return categories;
        }

        public bool AllowsBusLaneCategories(BusLaneVehicleCategory categories)
        {
            return (GetEnabledBusLaneCategories() & categories) != BusLaneVehicleCategory.None;
        }

        public bool AllowsAdditionalBusLaneRole(BusLaneFlagGrantExperimentRole role)
        {
            switch (role)
            {
                case BusLaneFlagGrantExperimentRole.PersonalCar:
                    return AllowPersonalCars;
                case BusLaneFlagGrantExperimentRole.DeliveryTruck:
                    return AllowDeliveryTrucks;
                case BusLaneFlagGrantExperimentRole.CargoTransport:
                    return AllowCargoTransportVehicles;
                case BusLaneFlagGrantExperimentRole.Hearse:
                    return AllowHearses;
                case BusLaneFlagGrantExperimentRole.PrisonerTransport:
                    return AllowPrisonerTransports;
                case BusLaneFlagGrantExperimentRole.ParkMaintenanceVehicle:
                    return AllowParkMaintenanceVehicles;
                default:
                    return false;
            }
        }

        public int GetPermissionSettingsMask()
        {
            int mask = 0;
            if (AllowRoadPublicTransportVehicles) mask |= 1 << 0;
            if (AllowTaxis) mask |= 1 << 1;
            if (AllowPoliceCars) mask |= 1 << 2;
            if (AllowFireEngines) mask |= 1 << 3;
            if (AllowAmbulances) mask |= 1 << 4;
            if (AllowGarbageTrucks) mask |= 1 << 5;
            if (AllowPostVans) mask |= 1 << 6;
            if (AllowRoadMaintenanceVehicles) mask |= 1 << 7;
            if (AllowSnowplows) mask |= 1 << 8;
            if (AllowVehicleMaintenanceVehicles) mask |= 1 << 9;
            if (AllowPersonalCars) mask |= 1 << 10;
            if (AllowDeliveryTrucks) mask |= 1 << 11;
            if (AllowCargoTransportVehicles) mask |= 1 << 12;
            if (AllowHearses) mask |= 1 << 13;
            if (AllowPrisonerTransports) mask |= 1 << 14;
            if (AllowParkMaintenanceVehicles) mask |= 1 << 15;
            return mask;
        }
    }

    public static class EnforcementGameplaySettingsService
    {
        private static EnforcementGameplaySettingsState s_Current = EnforcementGameplaySettingsState.CreateCodeDefaults();

        public static EnforcementGameplaySettingsState Current => s_Current;

        public static void Apply(EnforcementGameplaySettingsState state)
        {
            s_Current = state;
        }

        public static void ResetToCodeDefaults()
        {
            s_Current = EnforcementGameplaySettingsState.CreateCodeDefaults();
        }
    }
}
