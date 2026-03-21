using Game;
using Game.Prefabs;
using Game.Simulation;
using Game.Vehicles;
using System.Collections.Generic;
using Unity.Entities;
using VehicleAmbulance = Game.Vehicles.Ambulance;
using VehicleCargoTransport = Game.Vehicles.CargoTransport;
using VehicleDeliveryTruck = Game.Vehicles.DeliveryTruck;
using VehicleFireEngine = Game.Vehicles.FireEngine;
using VehicleGarbageTruck = Game.Vehicles.GarbageTruck;
using VehicleHearse = Game.Vehicles.Hearse;
using VehicleMaintenanceVehicle = Game.Vehicles.MaintenanceVehicle;
using VehicleParkMaintenanceVehicle = Game.Vehicles.ParkMaintenanceVehicle;
using VehiclePersonalCar = Game.Vehicles.PersonalCar;
using VehiclePoliceCar = Game.Vehicles.PoliceCar;
using VehiclePostVan = Game.Vehicles.PostVan;
using VehiclePrisonerTransport = Game.Vehicles.PrisonerTransport;
using VehiclePublicTransport = Game.Vehicles.PublicTransport;
using VehicleTaxi = Game.Vehicles.Taxi;
using Game.SceneFlow;

namespace Traffic_Law_Enforcement
{
    public struct PublicTransportLaneVehicleTypeLookups // TODO: Rename to PublicTransportLaneVehicleTypeLookups in a later refactor
    {
        public ComponentLookup<Car> CarData;
        public ComponentLookup<VehiclePublicTransport> PublicTransportData;
        public ComponentLookup<VehicleTaxi> TaxiData;
        public ComponentLookup<VehiclePoliceCar> PoliceCarData;
        public ComponentLookup<VehicleFireEngine> FireEngineData;
        public ComponentLookup<VehicleAmbulance> AmbulanceData;
        public ComponentLookup<VehicleGarbageTruck> GarbageTruckData;
        public ComponentLookup<VehiclePostVan> PostVanData;
        public ComponentLookup<VehicleMaintenanceVehicle> MaintenanceVehicleData;
        public ComponentLookup<VehiclePersonalCar> PersonalCarData;
        public ComponentLookup<VehicleDeliveryTruck> DeliveryTruckData;
        public ComponentLookup<VehicleCargoTransport> CargoTransportData;
        public ComponentLookup<VehicleHearse> HearseData;
        public ComponentLookup<VehiclePrisonerTransport> PrisonerTransportData;
        public ComponentLookup<VehicleParkMaintenanceVehicle> ParkMaintenanceVehicleData;
        public ComponentLookup<PrefabRef> PrefabRefData;
        public ComponentLookup<MaintenanceVehicleData> PrefabMaintenanceVehicleData;

        public static PublicTransportLaneVehicleTypeLookups Create(ref SystemState state)
        {
            return new PublicTransportLaneVehicleTypeLookups
            {
                CarData = state.GetComponentLookup<Car>(true),
                PublicTransportData = state.GetComponentLookup<VehiclePublicTransport>(true),
                TaxiData = state.GetComponentLookup<VehicleTaxi>(true),
                PoliceCarData = state.GetComponentLookup<VehiclePoliceCar>(true),
                FireEngineData = state.GetComponentLookup<VehicleFireEngine>(true),
                AmbulanceData = state.GetComponentLookup<VehicleAmbulance>(true),
                GarbageTruckData = state.GetComponentLookup<VehicleGarbageTruck>(true),
                PostVanData = state.GetComponentLookup<VehiclePostVan>(true),
                MaintenanceVehicleData = state.GetComponentLookup<VehicleMaintenanceVehicle>(true),
                PersonalCarData = state.GetComponentLookup<VehiclePersonalCar>(true),
                DeliveryTruckData = state.GetComponentLookup<VehicleDeliveryTruck>(true),
                CargoTransportData = state.GetComponentLookup<VehicleCargoTransport>(true),
                HearseData = state.GetComponentLookup<VehicleHearse>(true),
                PrisonerTransportData = state.GetComponentLookup<VehiclePrisonerTransport>(true),
                ParkMaintenanceVehicleData = state.GetComponentLookup<VehicleParkMaintenanceVehicle>(true),
                PrefabRefData = state.GetComponentLookup<PrefabRef>(true),
                PrefabMaintenanceVehicleData = state.GetComponentLookup<MaintenanceVehicleData>(true),
            };
        }

        public static PublicTransportLaneVehicleTypeLookups Create(GameSystemBase system)
        {
            return new PublicTransportLaneVehicleTypeLookups
            {
                CarData = system.GetComponentLookup<Car>(true),
                PublicTransportData = system.GetComponentLookup<VehiclePublicTransport>(true),
                TaxiData = system.GetComponentLookup<VehicleTaxi>(true),
                PoliceCarData = system.GetComponentLookup<VehiclePoliceCar>(true),
                FireEngineData = system.GetComponentLookup<VehicleFireEngine>(true),
                AmbulanceData = system.GetComponentLookup<VehicleAmbulance>(true),
                GarbageTruckData = system.GetComponentLookup<VehicleGarbageTruck>(true),
                PostVanData = system.GetComponentLookup<VehiclePostVan>(true),
                MaintenanceVehicleData = system.GetComponentLookup<VehicleMaintenanceVehicle>(true),
                PersonalCarData = system.GetComponentLookup<VehiclePersonalCar>(true),
                DeliveryTruckData = system.GetComponentLookup<VehicleDeliveryTruck>(true),
                CargoTransportData = system.GetComponentLookup<VehicleCargoTransport>(true),
                HearseData = system.GetComponentLookup<VehicleHearse>(true),
                PrisonerTransportData = system.GetComponentLookup<VehiclePrisonerTransport>(true),
                ParkMaintenanceVehicleData = system.GetComponentLookup<VehicleParkMaintenanceVehicle>(true),
                PrefabRefData = system.GetComponentLookup<PrefabRef>(true),
                PrefabMaintenanceVehicleData = system.GetComponentLookup<MaintenanceVehicleData>(true),
            };
        }

        public void Update(ref SystemState state)
        {
            CarData.Update(ref state);
            PublicTransportData.Update(ref state);
            TaxiData.Update(ref state);
            PoliceCarData.Update(ref state);
            FireEngineData.Update(ref state);
            AmbulanceData.Update(ref state);
            GarbageTruckData.Update(ref state);
            PostVanData.Update(ref state);
            MaintenanceVehicleData.Update(ref state);
            PersonalCarData.Update(ref state);
            DeliveryTruckData.Update(ref state);
            CargoTransportData.Update(ref state);
            HearseData.Update(ref state);
            PrisonerTransportData.Update(ref state);
            ParkMaintenanceVehicleData.Update(ref state);
            PrefabRefData.Update(ref state);
            PrefabMaintenanceVehicleData.Update(ref state);
        }

        public void Update(GameSystemBase system)
        {
            CarData.Update(system);
            PublicTransportData.Update(system);
            TaxiData.Update(system);
            PoliceCarData.Update(system);
            FireEngineData.Update(system);
            AmbulanceData.Update(system);
            GarbageTruckData.Update(system);
            PostVanData.Update(system);
            MaintenanceVehicleData.Update(system);
            PersonalCarData.Update(system);
            DeliveryTruckData.Update(system);
            CargoTransportData.Update(system);
            HearseData.Update(system);
            PrisonerTransportData.Update(system);
            ParkMaintenanceVehicleData.Update(system);
            PrefabRefData.Update(system);
            PrefabMaintenanceVehicleData.Update(system);
        }
    }

    public static class EmergencyVehiclePolicy
    {
        public static bool IsEmergencyVehicle(Car car)
        {
            return (car.m_Flags & CarFlags.Emergency) != 0;
        }

        public static bool IsEmergencyVehicle(Entity vehicle, ref PublicTransportLaneVehicleTypeLookups lookups)
        {
            return lookups.CarData.TryGetComponent(vehicle, out Car car) && IsEmergencyVehicle(car);
        }
    }

    public static class PublicTransportLanePolicy
    {
        public const CarFlags PublicTransportLanePermissionMask = CarFlags.UsePublicTransportLanes | CarFlags.PreferPublicTransportLanes;

        public static bool IsAllowedOnPublicTransportLane(Entity vehicle, ref PublicTransportLaneVehicleTypeLookups lookups)
        {
            if (EmergencyVehiclePolicy.IsEmergencyVehicle(vehicle, ref lookups))
            {
                return true;
            }

            return HasPublicTransportLanePermissionFlag(vehicle, ref lookups);
        }

        public static bool HasPublicTransportLanePermissionFlag(Entity vehicle, ref PublicTransportLaneVehicleTypeLookups lookups)
        {
            return lookups.CarData.TryGetComponent(vehicle, out Car car) && HasPublicTransportLanePermissionFlag(car);
        }

        public static bool HasPublicTransportLanePermissionFlag(Car car)
        {
            return (car.m_Flags & CarFlags.UsePublicTransportLanes) != 0;
        }

        public static int GetPermissionSettingsMask(EnforcementGameplaySettingsState settings)
        {
            return settings.GetPermissionSettingsMask();
        }

        public static bool TryGetDesiredPermissionState(Entity vehicle, Car car, EnforcementGameplaySettingsState settings, ref PublicTransportLaneVehicleTypeLookups lookups, out bool shouldTrack, out CarFlags desiredMask)
        {
            shouldTrack = false;
            desiredMask = 0;

            bool emergency = EmergencyVehiclePolicy.IsEmergencyVehicle(car);
            PublicTransportLaneVehicleCategory authorizedCategories = GetVanillaAuthorizedCategories(vehicle, ref lookups);
            PublicTransportLaneFlagGrantExperimentRole additionalRole = GetFlagGrantExperimentRole(vehicle, ref lookups);
            bool recognizedRole = authorizedCategories != PublicTransportLaneVehicleCategory.None || additionalRole != PublicTransportLaneFlagGrantExperimentRole.None;

            if (!recognizedRole && !emergency)
            {
                return false;
            }

            shouldTrack = true;
            bool allowAuthorized = settings.AllowsPublicTransportLaneCategories(authorizedCategories);
            bool allowAdditional = settings.AllowsAdditionalPublicTransportLaneRole(additionalRole);
            bool allow = emergency || allowAuthorized || allowAdditional;
            desiredMask = GetDesiredPermissionMask(emergency, authorizedCategories, additionalRole, allowAdditional, allow);
            return true;
        }

        private static CarFlags GetDesiredPermissionMask(
            bool emergency,
            PublicTransportLaneVehicleCategory authorizedCategories,
            PublicTransportLaneFlagGrantExperimentRole additionalRole,
            bool allowAdditional,
            bool allow)
        {
            if (!allow)
            {
                return 0;
            }

            if (emergency)
            {
                return PublicTransportLanePermissionMask;
            }

            bool isRoadPublicTransport =
                (authorizedCategories & PublicTransportLaneVehicleCategory.RoadPublicTransportVehicle) != 0;

            if (isRoadPublicTransport)
            {
                return PublicTransportLanePermissionMask;
            }

            if (authorizedCategories != PublicTransportLaneVehicleCategory.None)
            {
                return CarFlags.UsePublicTransportLanes;
            }

            if (additionalRole != PublicTransportLaneFlagGrantExperimentRole.None && allowAdditional)
            {
                return CarFlags.UsePublicTransportLanes;
            }

            return 0;
        }

        private static string GetRoleDisplayNameEnglish(PublicTransportLaneFlagGrantExperimentRole role)
        {
            switch (role)
            {
                case PublicTransportLaneFlagGrantExperimentRole.PersonalCar: return "Personal cars";
                case PublicTransportLaneFlagGrantExperimentRole.DeliveryTruck: return "Delivery trucks";
                case PublicTransportLaneFlagGrantExperimentRole.CargoTransport: return "Cargo transport vehicles";
                case PublicTransportLaneFlagGrantExperimentRole.Hearse: return "Hearses";
                case PublicTransportLaneFlagGrantExperimentRole.PrisonerTransport: return "Prisoner transports";
                case PublicTransportLaneFlagGrantExperimentRole.ParkMaintenanceVehicle: return "Park maintenance vehicles";
                default: return "None";
            }
        }

        public static string DescribeMissingPermissionReason(Entity vehicle, EnforcementGameplaySettingsState settings, ref PublicTransportLaneVehicleTypeLookups lookups)
        {
            PublicTransportLaneVehicleCategory authorizedCategories = GetVanillaAuthorizedCategories(vehicle, ref lookups);
            PublicTransportLaneFlagGrantExperimentRole additionalRole = GetFlagGrantExperimentRole(vehicle, ref lookups);


            if (authorizedCategories != PublicTransportLaneVehicleCategory.None)
            {
                if (!settings.AllowsPublicTransportLaneCategories(authorizedCategories))
                {
                    return $"public-transport-lane flags revoked by mod setting: {authorizedCategories}";
                }

                return $"public-transport-lane flags missing for vanilla-authorized categories: {authorizedCategories}";
            }

            if (additionalRole != PublicTransportLaneFlagGrantExperimentRole.None)
            {
                var displayName = GetRoleDisplayNameEnglish(additionalRole);
                if (settings.AllowsAdditionalPublicTransportLaneRole(additionalRole))
                {
                    return $"public-transport-lane flags missing for granted role: {displayName}";
                }

                return $"public-transport-lane flags not granted for role: {displayName}";
            }

            return "vehicle has no public-transport-lane permission flags";
        }

        public static bool TryGetAllowedType3Role(Entity vehicle, EnforcementGameplaySettingsState settings, ref PublicTransportLaneVehicleTypeLookups lookups, out PublicTransportLaneFlagGrantExperimentRole role)
        {
            role = GetFlagGrantExperimentRole(vehicle, ref lookups);
            if (role == PublicTransportLaneFlagGrantExperimentRole.None)
            {
                return false;
            }

            if (GetVanillaAuthorizedCategories(vehicle, ref lookups) != PublicTransportLaneVehicleCategory.None)
            {
                return false;
            }

            return settings.AllowsAdditionalPublicTransportLaneRole(role);
        }

        public static string DescribeVehicleRole(Entity vehicle, ref PublicTransportLaneVehicleTypeLookups lookups)
        {
            List<string> names = new List<string>(4);

            PublicTransportLaneVehicleCategory authorizedCategories = GetVanillaAuthorizedCategories(vehicle, ref lookups);
            AppendAuthorizedCategoryNames(authorizedCategories, names);

            PublicTransportLaneFlagGrantExperimentRole additionalRole = GetFlagGrantExperimentRole(vehicle, ref lookups);

            if (additionalRole != PublicTransportLaneFlagGrantExperimentRole.None)
            {
                var displayName = GetRoleDisplayNameEnglish(additionalRole);
                names.Add(displayName);
            }

            if (names.Count == 0)
            {
                names.Add("Unclassified road vehicle");
            }

            string description = string.Join(", ", names);
            if (EmergencyVehiclePolicy.IsEmergencyVehicle(vehicle, ref lookups))
            {
                description += " [emergency]";
            }

            return description;
        }

        public static PublicTransportLaneVehicleCategory GetVanillaAuthorizedCategories(Entity vehicle, ref PublicTransportLaneVehicleTypeLookups lookups)
        {
            PublicTransportLaneVehicleCategory categories = PublicTransportLaneVehicleCategory.None;

            if (lookups.PublicTransportData.HasComponent(vehicle))
            {
                categories |= PublicTransportLaneVehicleCategory.RoadPublicTransportVehicle;
            }

            if (lookups.TaxiData.HasComponent(vehicle))
            {
                categories |= PublicTransportLaneVehicleCategory.Taxi;
            }

            if (lookups.PoliceCarData.HasComponent(vehicle))
            {
                categories |= PublicTransportLaneVehicleCategory.PoliceCar;
            }

            if (lookups.FireEngineData.HasComponent(vehicle))
            {
                categories |= PublicTransportLaneVehicleCategory.FireEngine;
            }

            if (lookups.AmbulanceData.HasComponent(vehicle))
            {
                categories |= PublicTransportLaneVehicleCategory.Ambulance;
            }

            if (lookups.GarbageTruckData.HasComponent(vehicle))
            {
                categories |= PublicTransportLaneVehicleCategory.GarbageTruck;
            }

            if (lookups.PostVanData.HasComponent(vehicle))
            {
                categories |= PublicTransportLaneVehicleCategory.PostVan;
            }

            categories |= GetMaintenanceCategories(vehicle, ref lookups);
            return categories;
        }

        public static PublicTransportLaneFlagGrantExperimentRole GetFlagGrantExperimentRole(Entity vehicle, ref PublicTransportLaneVehicleTypeLookups lookups)
        {
            if (lookups.PersonalCarData.HasComponent(vehicle))
            {
                return PublicTransportLaneFlagGrantExperimentRole.PersonalCar;
            }

            if (lookups.DeliveryTruckData.HasComponent(vehicle))
            {
                return PublicTransportLaneFlagGrantExperimentRole.DeliveryTruck;
            }

            if (lookups.CargoTransportData.HasComponent(vehicle))
            {
                return PublicTransportLaneFlagGrantExperimentRole.CargoTransport;
            }

            if (lookups.HearseData.HasComponent(vehicle))
            {
                return PublicTransportLaneFlagGrantExperimentRole.Hearse;
            }

            if (lookups.PrisonerTransportData.HasComponent(vehicle))
            {
                return PublicTransportLaneFlagGrantExperimentRole.PrisonerTransport;
            }

            if (lookups.ParkMaintenanceVehicleData.HasComponent(vehicle))
            {
                return PublicTransportLaneFlagGrantExperimentRole.ParkMaintenanceVehicle;
            }

            return PublicTransportLaneFlagGrantExperimentRole.None;
        }

        private static PublicTransportLaneVehicleCategory GetMaintenanceCategories(Entity vehicle, ref PublicTransportLaneVehicleTypeLookups lookups)
        {
            if (!lookups.MaintenanceVehicleData.HasComponent(vehicle))
            {
                return PublicTransportLaneVehicleCategory.None;
            }

            MaintenanceVehicleData prefabData;
            if (lookups.PrefabMaintenanceVehicleData.TryGetComponent(vehicle, out prefabData))
            {
            }
            else if (lookups.PrefabRefData.TryGetComponent(vehicle, out PrefabRef prefabRef) &&
                lookups.PrefabMaintenanceVehicleData.TryGetComponent(prefabRef.m_Prefab, out prefabData))
            {
            }
            else
            {
                return PublicTransportLaneVehicleCategory.None;
            }

            PublicTransportLaneVehicleCategory categories = PublicTransportLaneVehicleCategory.None;
            MaintenanceType maintenanceType = prefabData.m_MaintenanceType;

            if ((maintenanceType & MaintenanceType.Road) != MaintenanceType.None)
            {
                categories |= PublicTransportLaneVehicleCategory.RoadMaintenanceVehicle;
            }

            if ((maintenanceType & MaintenanceType.Snow) != MaintenanceType.None)
            {
                categories |= PublicTransportLaneVehicleCategory.Snowplow;
            }

            if ((maintenanceType & MaintenanceType.Vehicle) != MaintenanceType.None)
            {
                categories |= PublicTransportLaneVehicleCategory.VehicleMaintenanceVehicle;
            }

            return categories;
        }

        private static void AppendAuthorizedCategoryNames(PublicTransportLaneVehicleCategory categories, List<string> names)
        {
            if ((categories & PublicTransportLaneVehicleCategory.RoadPublicTransportVehicle) != 0)
            {
                names.Add("Road public transport vehicles");
            }

            if ((categories & PublicTransportLaneVehicleCategory.Taxi) != 0)
            {
                names.Add("Taxis");
            }

            if ((categories & PublicTransportLaneVehicleCategory.PoliceCar) != 0)
            {
                names.Add("Police cars");
            }

            if ((categories & PublicTransportLaneVehicleCategory.FireEngine) != 0)
            {
                names.Add("Fire engines");
            }

            if ((categories & PublicTransportLaneVehicleCategory.Ambulance) != 0)
            {
                names.Add("Ambulances");
            }

            if ((categories & PublicTransportLaneVehicleCategory.GarbageTruck) != 0)
            {
                names.Add("Garbage trucks");
            }

            if ((categories & PublicTransportLaneVehicleCategory.PostVan) != 0)
            {
                names.Add("Post vans");
            }

            if ((categories & PublicTransportLaneVehicleCategory.RoadMaintenanceVehicle) != 0)
            {
                names.Add("Road maintenance vehicles");
            }

            if ((categories & PublicTransportLaneVehicleCategory.Snowplow) != 0)
            {
                names.Add("Snowplows");
            }

            if ((categories & PublicTransportLaneVehicleCategory.VehicleMaintenanceVehicle) != 0)
            {
                names.Add("Vehicle maintenance vehicles");
            }
        }
    }
}
