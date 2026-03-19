using Game;
using Game.Pathfind;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    [BurstCompile]
    public partial struct PublicTransportLanePermissionSystem : ISystem
    {
        private EntityQuery m_AllCarsQuery;
        private EntityQuery m_ChangedCarQuery;
        private EntityQuery m_TrackedQuery;
        private ComponentLookup<PathOwner> m_PathOwnerData;
        private BusLaneVehicleTypeLookups m_TypeLookups;
        private bool m_HasEvaluated;
        private bool m_LastEnforcementEnabled;
        private int m_LastSettingsMask;

        public void OnCreate(ref SystemState state)
        {
            m_AllCarsQuery = state.GetEntityQuery(ComponentType.ReadWrite<Car>());
            m_ChangedCarQuery = state.GetEntityQuery(ComponentType.ReadWrite<Car>());
            m_ChangedCarQuery.SetChangedVersionFilter(ComponentType.ReadOnly<Car>());
            m_TrackedQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<Car>(),
                ComponentType.ReadWrite<PublicTransportLanePermissionState>());
            m_PathOwnerData = state.GetComponentLookup<PathOwner>();
            m_TypeLookups = BusLaneVehicleTypeLookups.Create(state);
            state.RequireForUpdate(m_AllCarsQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_PathOwnerData.Update(state);
            m_TypeLookups.Update(state);
            EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;

            bool enforcementEnabled = Mod.IsPublicTransportLaneEnforcementEnabled;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            if (!enforcementEnabled)
            {
                new RestoreTrackedVehiclesJob
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(m_TrackedQuery);
                state.Dependency.Complete();
                ecb.Playback(state.EntityManager);
                ecb.Dispose();
                m_HasEvaluated = false;
                m_LastEnforcementEnabled = false;
                return;
            }

            int settingsMask = BusLanePolicy.GetPermissionSettingsMask(settings);
            bool fullRefresh = !m_HasEvaluated || !m_LastEnforcementEnabled || settingsMask != m_LastSettingsMask;
            var query = fullRefresh ? m_AllCarsQuery : m_ChangedCarQuery;
            new EvaluatePermissionJob
            {
                Settings = settings,
                TypeLookups = m_TypeLookups,
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(query);

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            m_HasEvaluated = true;
            m_LastEnforcementEnabled = true;
            m_LastSettingsMask = settingsMask;
        }

        [BurstCompile]
        private struct EvaluatePermissionJob : IJobEntity
        {
            public EnforcementGameplaySettingsState Settings;
            public BusLaneVehicleTypeLookups TypeLookups;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute([EntityIndexInQuery] int index, Entity entity, ref Car car)
            {
                bool hasState = SystemAPI.HasComponent<PublicTransportLanePermissionState>(entity);
                PublicTransportLanePermissionState state = hasState
                    ? SystemAPI.GetComponent<PublicTransportLanePermissionState>(entity)
                    : default;
                CarFlags originalMask = hasState
                    ? state.m_OriginalPublicTransportLaneFlags
                    : (car.m_Flags & BusLanePolicy.PublicTransportLanePermissionMask);

                if (!BusLanePolicy.TryGetDesiredPermissionState(entity, car, Settings, ref TypeLookups, out _, out CarFlags desiredMask))
                {
                    if (hasState)
                    {
                        ECB.AddComponent(index, entity, state);
                        ECB.RemoveComponent<PublicTransportLanePermissionState>(index, entity);
                    }
                    return;
                }

                CarFlags currentMask = car.m_Flags & BusLanePolicy.PublicTransportLanePermissionMask;
                bool emergencyActive = EmergencyVehiclePolicy.IsEmergencyVehicle(car);
                bool emergencyTransition = hasState && state.m_EmergencyActive != (emergencyActive ? (byte)1 : (byte)0);
                bool flagsChanged = currentMask != desiredMask;

                if (flagsChanged)
                {
                    car.m_Flags = (car.m_Flags & ~BusLanePolicy.PublicTransportLanePermissionMask) | desiredMask;
                }

                PublicTransportLanePermissionState updatedState = new PublicTransportLanePermissionState
                {
                    m_OriginalPublicTransportLaneFlags = originalMask,
                    m_EmergencyActive = emergencyActive ? (byte)1 : (byte)0,
                };

                if (!hasState)
                {
                    ECB.AddComponent(index, entity, updatedState);
                }
                else if (!StatesEqual(state, updatedState))
                {
                    ECB.SetComponent(index, entity, updatedState);
                }

                if (flagsChanged || emergencyTransition)
                {
                    ECB.AddComponent<PathObsoleteTag>(index, entity);
                }
            }
        }

        [BurstCompile]
        private struct RestoreTrackedVehiclesJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute([EntityIndexInQuery] int index, Entity entity, ref Car car, ref PublicTransportLanePermissionState state)
            {
                CarFlags currentMask = car.m_Flags & BusLanePolicy.PublicTransportLanePermissionMask;
                bool flagsChanged = currentMask != state.m_OriginalPublicTransportLaneFlags;
                if (flagsChanged)
                {
                    car.m_Flags = (car.m_Flags & ~BusLanePolicy.PublicTransportLanePermissionMask) | state.m_OriginalPublicTransportLaneFlags;
                }

                if (flagsChanged || state.m_EmergencyActive != 0)
                {
                    ECB.AddComponent<PathObsoleteTag>(index, entity);
                }

                ECB.RemoveComponent<PublicTransportLanePermissionState>(index, entity);
            }
        }

        // PathObsoleteTag: marker for managed follow-up system to obsolete path
        public struct PathObsoleteTag : IComponentData {}
        {
            return left.m_OriginalPublicTransportLaneFlags == right.m_OriginalPublicTransportLaneFlags &&
                left.m_EmergencyActive == right.m_EmergencyActive;
        }
    }
}
