using Game;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public partial class VehicleTrafficLawProfileSystem : GameSystemBase
    {
        private EntityQuery m_AllCarsQuery;
        private EntityQuery m_ChangedCarQuery;
        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;

        private bool m_HasEvaluated;
        private int m_LastPermissionSettingsMask;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_AllCarsQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>());

            m_ChangedCarQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>());
            m_ChangedCarQuery.SetChangedVersionFilter(ComponentType.ReadOnly<Car>());

            m_TypeLookups = PublicTransportLaneVehicleTypeLookups.Create(this);

            RequireForUpdate(m_AllCarsQuery);
        }

        protected override void OnUpdate()
        {
            m_TypeLookups.Update(this);

            EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;
            int permissionSettingsMask = PublicTransportLanePolicy.GetPermissionSettingsMask(settings);

            bool fullRefresh =
                !m_HasEvaluated ||
                permissionSettingsMask != m_LastPermissionSettingsMask;

            EvaluateQuery(
                fullRefresh ? m_AllCarsQuery : m_ChangedCarQuery,
                settings,
                permissionSettingsMask);

            m_HasEvaluated = true;
            m_LastPermissionSettingsMask = permissionSettingsMask;
        }

        private void EvaluateQuery(
            EntityQuery query,
            EnforcementGameplaySettingsState settings,
            int permissionSettingsMask)
        {
            NativeArray<Entity> vehicles = query.ToEntityArray(Allocator.Temp);
            NativeArray<Car> cars = query.ToComponentDataArray<Car>(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index += 1)
                {
                    EvaluateVehicle(
                        vehicles[index],
                        cars[index],
                        settings,
                        permissionSettingsMask);
                }
            }
            finally
            {
                vehicles.Dispose();
                cars.Dispose();
            }
        }

        private void EvaluateVehicle(
            Entity vehicle,
            Car car,
            EnforcementGameplaySettingsState settings,
            int permissionSettingsMask)
        {
            bool hasProfile = EntityManager.HasComponent<VehicleTrafficLawProfile>(vehicle);

            bool shouldTrack;
            CarFlags desiredMask;

            bool success = PublicTransportLanePolicy.TryGetDesiredPermissionState(
                vehicle,
                car,
                settings,
                ref m_TypeLookups,
                out shouldTrack,
                out desiredMask);

            if (!success || !shouldTrack)
            {
                if (hasProfile)
                {
                    EntityManager.RemoveComponent<VehicleTrafficLawProfile>(vehicle);
                }

                return;
            }

            VehicleTrafficLawProfile updatedProfile = new VehicleTrafficLawProfile
            {
                m_ShouldTrack = 1,
                m_EmergencyVehicle =
                    (byte)(EmergencyVehiclePolicy.IsEmergencyVehicle(car) ? 1 : 0),
                m_DesiredPublicTransportLaneMask =
                    desiredMask & PublicTransportLanePolicy.PublicTransportLanePermissionMask,
                m_VanillaAuthorizedCategories =
                    PublicTransportLanePolicy.GetVanillaAuthorizedCategories(vehicle, ref m_TypeLookups),
                m_AdditionalRole =
                    PublicTransportLanePolicy.GetFlagGrantExperimentRole(vehicle, ref m_TypeLookups),
                m_PermissionSettingsMask = permissionSettingsMask,
            };

            if (!hasProfile)
            {
                EntityManager.AddComponentData(vehicle, updatedProfile);
                return;
            }

            VehicleTrafficLawProfile currentProfile =
                EntityManager.GetComponentData<VehicleTrafficLawProfile>(vehicle);

            if (!ProfilesEqual(currentProfile, updatedProfile))
            {
                EntityManager.SetComponentData(vehicle, updatedProfile);
            }
        }

        private static bool ProfilesEqual(
            VehicleTrafficLawProfile left,
            VehicleTrafficLawProfile right)
        {
            return left.m_ShouldTrack == right.m_ShouldTrack &&
                   left.m_EmergencyVehicle == right.m_EmergencyVehicle &&
                   left.m_DesiredPublicTransportLaneMask == right.m_DesiredPublicTransportLaneMask &&
                   left.m_VanillaAuthorizedCategories == right.m_VanillaAuthorizedCategories &&
                   left.m_AdditionalRole == right.m_AdditionalRole &&
                   left.m_PermissionSettingsMask == right.m_PermissionSettingsMask;
        }
    }
}
