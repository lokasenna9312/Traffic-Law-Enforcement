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
        private ComponentLookup<Car> m_CarData;
        private ComponentLookup<VehicleTrafficLawProfile> m_ProfileData;
        private ComponentLookup<PersistedPublicTransportLaneAccessState> m_PersistedAccessStateData;
        private EntityQuery m_PersistedWithoutProfileQuery;
        private EntityQuery m_PersistedStateQuery;
        private NativeList<Entity> m_PendingRefreshVehicles;
        private const int kVehiclesPerFrame = 512;
        private int m_RefreshCursor;
        private bool m_HasEvaluated;
        private int m_LastPermissionSettingsMask;
        private int m_LastObservedRuntimeWorldGeneration = -1;
        private int m_LastLoggedPersistedCount = -1;
        private int m_LastLoggedPersistedWithoutProfileCount = -1;
        private int m_LastLoggedSeededCount = -1;
        private int m_LastLoggedSeedPermissionSettingsMask = -1;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_AllCarsQuery = GetEntityQuery(ComponentType.ReadOnly<Car>());
            m_ChangedCarQuery = GetEntityQuery(ComponentType.ReadOnly<Car>());
            m_ChangedCarQuery.SetChangedVersionFilter(ComponentType.ReadOnly<Car>());
            m_TypeLookups = PublicTransportLaneVehicleTypeLookups.Create(this);
            m_ProfileData = GetComponentLookup<VehicleTrafficLawProfile>(true);
            m_CarData = GetComponentLookup<Car>(true);
            m_PendingRefreshVehicles = new NativeList<Entity>(Allocator.Persistent);
            m_PersistedAccessStateData = GetComponentLookup<PersistedPublicTransportLaneAccessState>(true);
            m_PersistedWithoutProfileQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Car>(),
                    ComponentType.ReadOnly<PersistedPublicTransportLaneAccessState>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<VehicleTrafficLawProfile>(),
                },
            });
            m_PersistedStateQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Car>(),
                    ComponentType.ReadOnly<PersistedPublicTransportLaneAccessState>(),
                },
            });
            RequireForUpdate(m_AllCarsQuery);
        }

        protected override void OnUpdate()
        {
            HandleRuntimeWorldReload();
            m_TypeLookups.Update(this);
            m_PersistedAccessStateData.Update(this);
            m_ProfileData.Update(this);
            m_CarData.Update(this);
            EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;

            int permissionSettingsMask = PublicTransportLanePolicy.GetPermissionSettingsMask(settings);
            SeedProfilesFromPersistedState(permissionSettingsMask);
            bool fullRefresh =
                !m_HasEvaluated ||
                permissionSettingsMask != m_LastPermissionSettingsMask;

            if (fullRefresh && m_PendingRefreshVehicles.Length == 0)
            {
                BuildPendingRefreshList();
            }

            if (m_PendingRefreshVehicles.Length > 0)
            {
                ProcessRefreshBatch(settings, permissionSettingsMask);

                if (m_PendingRefreshVehicles.Length == 0)
                {
                    m_HasEvaluated = true;
                    m_LastPermissionSettingsMask = permissionSettingsMask;
                }

                return;
            }

            EvaluateQuery(
                m_ChangedCarQuery,
                settings,
                permissionSettingsMask);

            m_HasEvaluated = true;
            m_LastPermissionSettingsMask = permissionSettingsMask;
        }

        protected override void OnDestroy()
        {
            if (m_PendingRefreshVehicles.IsCreated)
            {
                m_PendingRefreshVehicles.Dispose();
            }

            base.OnDestroy();
        }

        private void HandleRuntimeWorldReload()
        {
            int currentGeneration = EnforcementSaveDataSystem.RuntimeWorldGeneration;
            if (m_LastObservedRuntimeWorldGeneration == currentGeneration)
            {
                return;
            }

            m_LastObservedRuntimeWorldGeneration = currentGeneration;

            ClearPendingRefresh();
            m_HasEvaluated = false;
            m_LastPermissionSettingsMask = 0;

            m_LastLoggedPersistedCount = -1;
            m_LastLoggedPersistedWithoutProfileCount = -1;
            m_LastLoggedSeededCount = -1;
            m_LastLoggedSeedPermissionSettingsMask = -1;

            Mod.log.Info(
                $"[SAVELOAD] VehicleTrafficLawProfileSystem runtime reset: generation={currentGeneration}");
        }

        private void SeedProfilesFromPersistedState(int permissionSettingsMask)
        {
            int persistedCount = m_PersistedStateQuery.CalculateEntityCount();
            int persistedWithoutProfileCount = m_PersistedWithoutProfileQuery.CalculateEntityCount();
            int seededCount = 0;

            if (persistedWithoutProfileCount > 0)
            {
                NativeArray<Entity> vehicles =
                    m_PersistedWithoutProfileQuery.ToEntityArray(Allocator.Temp);

                try
                {
                    for (int index = 0; index < vehicles.Length; index += 1)
                    {
                        Entity vehicle = vehicles[index];

                        if (!m_CarData.TryGetComponent(vehicle, out Car car))
                        {
                            continue;
                        }

                        if (!m_PersistedAccessStateData.TryGetComponent(
                                vehicle,
                                out PersistedPublicTransportLaneAccessState persisted))
                        {
                            continue;
                        }

                        VehicleTrafficLawProfile seededProfile =
                            new VehicleTrafficLawProfile
                            {
                                m_ShouldTrack = persisted.m_ShouldTrack,
                                m_EmergencyVehicle = persisted.m_EmergencyVehicle,
                                m_PublicTransportLaneAccessBits = persisted.m_AccessBits,
                                m_VanillaAuthorizedCategories =
                                    PublicTransportLanePolicy.GetVanillaAuthorizedCategories(
                                        vehicle,
                                        ref m_TypeLookups),
                                m_AdditionalRole =
                                    PublicTransportLanePolicy.GetFlagGrantExperimentRole(
                                        vehicle,
                                        ref m_TypeLookups),
                                m_PermissionSettingsMask = permissionSettingsMask,
                            };

                        EntityManager.AddComponentData(vehicle, seededProfile);
                        EntityManager.RemoveComponent<PersistedPublicTransportLaneAccessState>(vehicle);
                        seededCount += 1;
                    }
                }
                finally
                {
                    vehicles.Dispose();
                }
            }

            if (seededCount != m_LastLoggedSeededCount ||
                persistedCount != m_LastLoggedPersistedCount ||
                persistedWithoutProfileCount != m_LastLoggedPersistedWithoutProfileCount ||
                permissionSettingsMask != m_LastLoggedSeedPermissionSettingsMask)
            {
                Mod.log.Info(
                    $"[SAVELOAD] SeedProfilesFromPersistedState: seededProfiles={seededCount}, " +
                    $"persistedStates={persistedCount}, " +
                    $"persistedWithoutProfile={persistedWithoutProfileCount}, " +
                    $"permissionSettingsMask={permissionSettingsMask}");

                m_LastLoggedSeededCount = seededCount;
                m_LastLoggedPersistedCount = persistedCount;
                m_LastLoggedPersistedWithoutProfileCount = persistedWithoutProfileCount;
                m_LastLoggedSeedPermissionSettingsMask = permissionSettingsMask;
            }
        }

        private void BuildPendingRefreshList()
        {
            ClearPendingRefresh();

            NativeArray<Entity> vehicles = m_AllCarsQuery.ToEntityArray(Allocator.Temp);
            try
            {
                for (int index = 0; index < vehicles.Length; index += 1)
                {
                    m_PendingRefreshVehicles.Add(vehicles[index]);
                }
            }
            finally
            {
                vehicles.Dispose();
            }
        }

        private void ClearPendingRefresh()
        {
            if (m_PendingRefreshVehicles.IsCreated)
            {
                m_PendingRefreshVehicles.Clear();
            }

            m_RefreshCursor = 0;
        }

        private void ProcessRefreshBatch(
            EnforcementGameplaySettingsState settings,
            int permissionSettingsMask)
        {
            int end = System.Math.Min(
                m_RefreshCursor + kVehiclesPerFrame,
                m_PendingRefreshVehicles.Length);

            for (int index = m_RefreshCursor; index < end; index += 1)
            {
                Entity vehicle = m_PendingRefreshVehicles[index];
                if (!m_CarData.TryGetComponent(vehicle, out Car car))
                {
                    continue;
                }

                EvaluateVehicle(vehicle, car, settings, permissionSettingsMask);
            }

            m_RefreshCursor = end;

            if (m_RefreshCursor >= m_PendingRefreshVehicles.Length)
            {
                ClearPendingRefresh();
            }
        }

        private void EvaluateQuery(
            EntityQuery query,
            EnforcementGameplaySettingsState settings,
            int permissionSettingsMask)
        {
            if (query.IsEmptyIgnoreFilter)
            {
                return;
            }

            NativeArray<Entity> vehicles = query.ToEntityArray(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index += 1)
                {
                    Entity vehicle = vehicles[index];
                    if (!m_CarData.TryGetComponent(vehicle, out Car car))
                    {
                        continue;
                    }

                    EvaluateVehicle(
                        vehicle,
                        car,
                        settings,
                        permissionSettingsMask);
                }
            }
            finally
            {
                vehicles.Dispose();
            }
        }

        private void EvaluateVehicle(Entity vehicle, Car car, EnforcementGameplaySettingsState settings, int permissionSettingsMask)
        {
            bool hasProfile = m_ProfileData.TryGetComponent(vehicle, out VehicleTrafficLawProfile currentProfile);
            bool shouldTrack;
            PublicTransportLaneAccessBits accessBits;

            bool success = PublicTransportLanePolicy.TryGetDesiredPermissionState(
                vehicle,
                car,
                settings,
                ref m_TypeLookups,
                out shouldTrack,
                out accessBits);

            if (!success || !shouldTrack)
            {
                if (hasProfile)
                {
                    EntityManager.RemoveComponent<VehicleTrafficLawProfile>(vehicle);
                }

                if (m_PersistedAccessStateData.HasComponent(vehicle))
                {
                    EntityManager.RemoveComponent<PersistedPublicTransportLaneAccessState>(vehicle);
                }

                return;
            }

            VehicleTrafficLawProfile updatedProfile = new VehicleTrafficLawProfile
            {
                m_ShouldTrack = 1,
                m_EmergencyVehicle =
                    (byte)(EmergencyVehiclePolicy.IsEmergencyVehicle(car) ? 1 : 0),
                m_PublicTransportLaneAccessBits = accessBits,
                m_VanillaAuthorizedCategories =
                    PublicTransportLanePolicy.GetVanillaAuthorizedCategories(vehicle, ref m_TypeLookups),
                m_AdditionalRole =
                    PublicTransportLanePolicy.GetFlagGrantExperimentRole(vehicle, ref m_TypeLookups),
                m_PermissionSettingsMask = permissionSettingsMask,
            };

            if (!hasProfile)
            {
                EntityManager.AddComponentData(vehicle, updatedProfile);
            }
            else if (!ProfilesEqual(currentProfile, updatedProfile))
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
                   left.m_VanillaAuthorizedCategories == right.m_VanillaAuthorizedCategories &&
                   left.m_AdditionalRole == right.m_AdditionalRole &&
                   left.m_PermissionSettingsMask == right.m_PermissionSettingsMask &&
                   left.m_PublicTransportLaneAccessBits == right.m_PublicTransportLaneAccessBits;
        }
    }
}
