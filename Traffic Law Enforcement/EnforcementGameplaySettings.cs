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

        public int GetEffectivePublicTransportLaneFineAmount()
        {
            return EnablePublicTransportLaneEnforcement ? PublicTransportLaneFineAmount : 0;
        }

        public int GetEffectiveMidBlockCrossingFineAmount()
        {
            return EnableMidBlockCrossingEnforcement ? MidBlockCrossingFineAmount : 0;
        }

        public int GetEffectiveIntersectionMovementFineAmount()
        {
            return EnableIntersectionMovementEnforcement ? IntersectionMovementFineAmount : 0;
        }

        public bool IsPublicTransportLaneRepeatPenaltyEffectivelyEnabled()
        {
            return EnablePublicTransportLaneEnforcement && EnablePublicTransportLaneRepeatPenalty;
        }

        public bool IsMidBlockCrossingRepeatPenaltyEffectivelyEnabled()
        {
            return EnableMidBlockCrossingEnforcement && EnableMidBlockCrossingRepeatPenalty;
        }

        public bool IsIntersectionMovementRepeatPenaltyEffectivelyEnabled()
        {
            return EnableIntersectionMovementEnforcement && EnableIntersectionMovementRepeatPenalty;
        }

        public PublicTransportLaneVehicleCategory GetEnabledPublicTransportLaneCategories()
        {
            if (!EnablePublicTransportLaneEnforcement)
            {
                return PublicTransportLaneVehicleCategory.None;
            }

            PublicTransportLaneVehicleCategory categories = PublicTransportLaneVehicleCategory.None;
            if (AllowRoadPublicTransportVehicles) categories |= PublicTransportLaneVehicleCategory.RoadPublicTransportVehicle;
            if (AllowTaxis) categories |= PublicTransportLaneVehicleCategory.Taxi;
            if (AllowPoliceCars) categories |= PublicTransportLaneVehicleCategory.PoliceCar;
            if (AllowFireEngines) categories |= PublicTransportLaneVehicleCategory.FireEngine;
            if (AllowAmbulances) categories |= PublicTransportLaneVehicleCategory.Ambulance;
            if (AllowGarbageTrucks) categories |= PublicTransportLaneVehicleCategory.GarbageTruck;
            if (AllowPostVans) categories |= PublicTransportLaneVehicleCategory.PostVan;
            if (AllowRoadMaintenanceVehicles) categories |= PublicTransportLaneVehicleCategory.RoadMaintenanceVehicle;
            if (AllowSnowplows) categories |= PublicTransportLaneVehicleCategory.Snowplow;
            if (AllowVehicleMaintenanceVehicles) categories |= PublicTransportLaneVehicleCategory.VehicleMaintenanceVehicle;
            return categories;
        }

        public bool AllowsPublicTransportLaneCategories(PublicTransportLaneVehicleCategory categories)
        {
            return (GetEnabledPublicTransportLaneCategories() & categories) != PublicTransportLaneVehicleCategory.None;
        }

        public bool AllowsAdditionalPublicTransportLaneRole(PublicTransportLaneFlagGrantExperimentRole role)
        {
            if (!EnablePublicTransportLaneEnforcement)
            {
                return false;
            }

            switch (role)
            {
                case PublicTransportLaneFlagGrantExperimentRole.PersonalCar:
                    return AllowPersonalCars;
                case PublicTransportLaneFlagGrantExperimentRole.DeliveryTruck:
                    return AllowDeliveryTrucks;
                case PublicTransportLaneFlagGrantExperimentRole.CargoTransport:
                    return AllowCargoTransportVehicles;
                case PublicTransportLaneFlagGrantExperimentRole.Hearse:
                    return AllowHearses;
                case PublicTransportLaneFlagGrantExperimentRole.PrisonerTransport:
                    return AllowPrisonerTransports;
                case PublicTransportLaneFlagGrantExperimentRole.ParkMaintenanceVehicle:
                    return AllowParkMaintenanceVehicles;
                default:
                    return false;
            }
        }

        public int GetPermissionSettingsMask()
        {
            if (!EnablePublicTransportLaneEnforcement)
            {
                return 0;
            }

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
