using Game;
using Game.Net;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public class PublicTransportLaneViolationSystem : GameSystemBase
    {
        private EntityQuery m_CarQuery;
        private EntityQuery m_ChangedLaneQuery;
        private EntityQuery m_ChangedCarQuery;
        private EntityQuery m_ViolationQuery;
        private EntityQuery m_StatisticsQuery;
        private Entity m_StatisticsEntity;
        private ComponentLookup<Car> m_CarData;
        private ComponentLookup<CarLane> m_CarLaneData;
        private ComponentLookup<PublicTransportLaneViolation> m_ViolationData;
        private ComponentLookup<PublicTransportLaneType3UsageState> m_Type3UsageData;
        private BusLaneVehicleTypeLookups m_TypeLookups;
        private bool m_HasEvaluated;
        private bool m_LastEnforcementEnabled;

        public void OnCreate(ref SystemState state)
        {
            m_CarQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>());
            m_ChangedLaneQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>());
            m_ChangedLaneQuery.SetChangedVersionFilter(ComponentType.ReadOnly<CarCurrentLane>());
            m_ChangedCarQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>());
            m_ChangedCarQuery.SetChangedVersionFilter(ComponentType.ReadOnly<Car>());
            m_ViolationQuery = state.GetEntityQuery(ComponentType.ReadOnly<PublicTransportLaneViolation>());
            m_StatisticsQuery = state.GetEntityQuery(ComponentType.ReadWrite<TrafficLawEnforcementStatistics>());
            if (m_StatisticsQuery.IsEmptyIgnoreFilter)
            {
                m_StatisticsEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(m_StatisticsEntity, default(TrafficLawEnforcementStatistics));
            }
            else
            {
                m_StatisticsEntity = m_StatisticsQuery.GetSingletonEntity();
            }

            m_CarData = state.GetComponentLookup<Car>(true);
            m_CarLaneData = state.GetComponentLookup<CarLane>(true);
            m_ViolationData = state.GetComponentLookup<PublicTransportLaneViolation>();
            m_Type3UsageData = state.GetComponentLookup<PublicTransportLaneType3UsageState>();
            m_TypeLookups = BusLaneVehicleTypeLookups.Create(state);
            state.RequireForUpdate(m_CarQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;

            bool enforcementEnabled = Mod.IsPublicTransportLaneEnforcementEnabled;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            if (!enforcementEnabled)
            {
                if (!m_ViolationQuery.IsEmptyIgnoreFilter)
                {
                    ecb.RemoveComponent<PublicTransportLaneViolation>(m_ViolationQuery);
                }

                TrafficLawEnforcementStatistics disabledStats = state.EntityManager.GetComponentData(m_StatisticsEntity);
                if (disabledStats.m_ActivePublicTransportLaneViolatorCount != 0)
                {
                    disabledStats.m_ActivePublicTransportLaneViolatorCount = 0;
                    state.EntityManager.SetComponentData(m_StatisticsEntity, disabledStats);
                }
                EnforcementTelemetry.SetStatistics(disabledStats);

                state.Dependency.Complete();
                ecb.Playback(state.EntityManager);
                ecb.Dispose();
                m_HasEvaluated = false;
                m_LastEnforcementEnabled = false;
                return;
            }

            m_CarData.Update(state);
            m_CarLaneData.Update(state);
            m_ViolationData.Update(state);
            m_Type3UsageData.Update(state);
            m_TypeLookups.Update(state);

            TrafficLawEnforcementStatistics statistics = state.EntityManager.GetComponentData(m_StatisticsEntity);
            bool statisticsChanged = false;
            bool fullRefresh = !m_HasEvaluated || !m_LastEnforcementEnabled;

            var eventBuffer = new NativeList<ViolationEvent>(Allocator.Temp);
            if (fullRefresh)
            {
                new ViolationDetectionJob
                {
                    Settings = settings,
                    TypeLookups = m_TypeLookups,
                    ECB = ecb.AsParallelWriter(),
                    StatisticsEntity = m_StatisticsEntity,
                    EventBuffer = eventBuffer.AsParallelWriter()
                }.ScheduleParallel(m_CarQuery);
            }
            else
            {
                new ViolationDetectionJob
                {
                    Settings = settings,
                    TypeLookups = m_TypeLookups,
                    ECB = ecb.AsParallelWriter(),
                    StatisticsEntity = m_StatisticsEntity,
                    EventBuffer = eventBuffer.AsParallelWriter()
                }.ScheduleParallel(m_ChangedLaneQuery);
                new ViolationDetectionJob
                {
                    Settings = settings,
                    TypeLookups = m_TypeLookups,
                    ECB = ecb.AsParallelWriter(),
                    StatisticsEntity = m_StatisticsEntity,
                    EventBuffer = eventBuffer.AsParallelWriter()
                }.ScheduleParallel(m_ChangedCarQuery);
            }

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            // Managed follow-up: process violation events for logging/penalty
            for (int i = 0; i < eventBuffer.Length; i++)
            {
                var ev = eventBuffer[i];
                // Logging, penalty, string creation (managed)
                string reason = BusLanePolicy.DescribeMissingPermissionReason(ev.Vehicle, settings, ref m_TypeLookups);
                EnforcementPenaltyService.RecordPublicTransportLaneViolation(ev.Vehicle, ev.Lane, reason);
                EnforcementLoggingPolicy.RecordEnforcementEvent($"Public transport lane violation: vehicle={ev.Vehicle}, lane={ev.Lane}, reason={reason}");
            }
            eventBuffer.Dispose();

            int activeViolatorCount = m_ViolationQuery.CalculateEntityCount();
            if (statistics.m_ActivePublicTransportLaneViolatorCount != activeViolatorCount)
            {
                statistics.m_ActivePublicTransportLaneViolatorCount = activeViolatorCount;
                statisticsChanged = true;
            }

            if (statisticsChanged)
            {
                state.EntityManager.SetComponentData(m_StatisticsEntity, statistics);
            }
            EnforcementTelemetry.SetStatistics(statistics);

            m_HasEvaluated = true;
            m_LastEnforcementEnabled = true;
        }

        [BurstCompile]
        private struct ViolationDetectionJob : IJobEntity
        {
            public EnforcementGameplaySettingsState Settings;
            public BusLaneVehicleTypeLookups TypeLookups;
            public EntityCommandBuffer.ParallelWriter ECB;
            public Entity StatisticsEntity;
            public NativeList<ViolationEvent>.ParallelWriter EventBuffer;

            public void Execute([EntityIndexInQuery] int index, Entity entity, in Car car, in CarCurrentLane currentLane)
            {
                Entity laneEntity = currentLane.m_Lane;
                bool isViolation = false;
                if (laneEntity != Entity.Null && SystemAPI.TryGetComponent(laneEntity, out CarLane laneData) && (laneData.m_Flags & Game.Net.CarLaneFlags.PublicOnly) != 0)
                {
                    if (!EmergencyVehiclePolicy.IsEmergencyVehicle(car))
                    {
                        bool hasPermission = BusLanePolicy.HasPublicTransportLanePermissionFlag(entity, ref TypeLookups);
                        isViolation = !hasPermission;
                    }
                }

                bool hasViolation = SystemAPI.HasComponent<PublicTransportLaneViolation>(entity);
                if (!isViolation)
                {
                    if (hasViolation)
                    {
                        ECB.RemoveComponent<PublicTransportLaneViolation>(index, entity);
                    }
                    return;
                }

                PublicTransportLaneViolation violation = new PublicTransportLaneViolation
                {
                    m_Lane = laneEntity,
                    m_StartDayTicks = EnforcementGameTime.CurrentTimestampDayTicks,
                    m_ExitPressureApplied = false,
                };

                if (!hasViolation)
                {
                    ECB.AddComponent(index, entity, violation);
                    EventBuffer.Add(index, new ViolationEvent { Vehicle = entity, Lane = laneEntity });
                }
                else if (SystemAPI.TryGetComponent(entity, out PublicTransportLaneViolation existingViolation))
                {
                    if (existingViolation.m_Lane != violation.m_Lane)
                    {
                        ECB.SetComponent(index, entity, violation);
                        EventBuffer.Add(index, new ViolationEvent { Vehicle = entity, Lane = laneEntity });
                    }
                }
                else
                {
                    ECB.SetComponent(index, entity, violation);
                }
            }
        }

        private struct ViolationEvent
        {
            public Entity Vehicle;
            public Entity Lane;
        }

        private void EvaluateQuery(EntityQuery query, EnforcementGameplaySettingsState settings, ref TrafficLawEnforcementStatistics statistics, ref bool statisticsChanged)
        {
            NativeArray<Entity> vehicles = query.ToEntityArray(Allocator.Temp);
            NativeArray<CarCurrentLane> currentLanes = query.ToComponentDataArray<CarCurrentLane>(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index++)
                {
                    EvaluateVehicle(vehicles[index], currentLanes[index], settings, ref statistics, ref statisticsChanged);
                }
            }
            finally
            {
                vehicles.Dispose();
                currentLanes.Dispose();
            }
        }

        private void EvaluateVehicle(Entity vehicle, CarCurrentLane currentLane, EnforcementGameplaySettingsState settings, ref TrafficLawEnforcementStatistics statistics, ref bool statisticsChanged)
        {
            Entity laneEntity = currentLane.m_Lane;
            bool isViolation = false;
            bool shouldLogType3Usage = false;
            BusLaneFlagGrantExperimentRole type3Role = BusLaneFlagGrantExperimentRole.None;

            if (laneEntity != Entity.Null &&
                m_CarLaneData.TryGetComponent(laneEntity, out CarLane laneData) &&
                (laneData.m_Flags & Game.Net.CarLaneFlags.PublicOnly) != 0)
            {
                if (!IsEmergencyVehicle(vehicle))
                {
                    bool hasPermission = BusLanePolicy.HasPublicTransportLanePermissionFlag(vehicle, ref m_TypeLookups);
                    isViolation = !hasPermission;
                    shouldLogType3Usage = hasPermission && BusLanePolicy.TryGetAllowedType3Role(vehicle, settings, ref m_TypeLookups, out type3Role);
                }
            }

            UpdateType3UsageObservation(vehicle, laneEntity, shouldLogType3Usage, type3Role);

            bool hasViolation = m_ViolationData.HasComponent(vehicle);
            if (!isViolation)
            {
                if (hasViolation)
                {
                    EntityManager.RemoveComponent<PublicTransportLaneViolation>(vehicle);
                }

                return;
            }

            PublicTransportLaneViolation violation = new PublicTransportLaneViolation
            {
                m_Lane = laneEntity,
                m_StartDayTicks = EnforcementGameTime.CurrentTimestampDayTicks,
                m_ExitPressureApplied = false,
            };

            if (!hasViolation)
            {
                EntityManager.AddComponentData(vehicle, violation);
                statistics.m_PublicTransportLaneViolationCount += 1;
                statisticsChanged = true;
                LogViolation(vehicle, settings, violation, statistics.m_PublicTransportLaneViolationCount);
            }
            else if (m_ViolationData.TryGetComponent(vehicle, out PublicTransportLaneViolation existingViolation))
            {
                if (existingViolation.m_Lane != violation.m_Lane)
                {
                    violation.m_StartDayTicks = EnforcementGameTime.CurrentTimestampDayTicks;
                    violation.m_ExitPressureApplied = false;
                    EntityManager.SetComponentData(vehicle, violation);
                }
            }
            else
            {
                EntityManager.SetComponentData(vehicle, violation);
            }
        }

        private bool IsEmergencyVehicle(Entity vehicle)
        {
            return m_CarData.TryGetComponent(vehicle, out Car car) && EmergencyVehiclePolicy.IsEmergencyVehicle(car);
        }

        private void LogViolation(Entity vehicle, EnforcementGameplaySettingsState settings, PublicTransportLaneViolation violation, int totalCount)
        {
            string reason = BusLanePolicy.DescribeMissingPermissionReason(vehicle, settings, ref m_TypeLookups);
            string message = $"Public transport lane violation #{totalCount}: vehicle={vehicle}, lane={violation.m_Lane}, reason={reason}";
            EnforcementPenaltyService.RecordPublicTransportLaneViolation(vehicle, violation.m_Lane, reason);
            EnforcementLoggingPolicy.RecordEnforcementEvent(message);
        }

        private void UpdateType3UsageObservation(Entity vehicle, Entity laneEntity, bool shouldLogType3Usage, BusLaneFlagGrantExperimentRole type3Role)
        {
            bool hasUsageState = m_Type3UsageData.HasComponent(vehicle);
            if (!shouldLogType3Usage || type3Role == BusLaneFlagGrantExperimentRole.None || laneEntity == Entity.Null)
            {
                if (hasUsageState)
                {
                    EntityManager.RemoveComponent<PublicTransportLaneType3UsageState>(vehicle);
                }

                return;
            }

            if (!hasUsageState)
            {
                EntityManager.AddComponentData(vehicle, new PublicTransportLaneType3UsageState
                {
                    m_Lane = laneEntity,
                });
                LogType3Usage(vehicle, laneEntity, type3Role);
                return;
            }

            PublicTransportLaneType3UsageState state = m_Type3UsageData[vehicle];
            if (state.m_Lane == laneEntity)
            {
                return;
            }

            state.m_Lane = laneEntity;
            EntityManager.SetComponentData(vehicle, state);
            LogType3Usage(vehicle, laneEntity, type3Role);
        }

        private static void LogType3Usage(Entity vehicle, Entity laneEntity, BusLaneFlagGrantExperimentRole type3Role)
        {
            string roleName = BusLaneFlagGrantExperimentRoleInfo.ToDisplayName(type3Role);
            string message = $"PT-lane usage by non-public vehicles allowed to use PT lanes: vehicle={vehicle}, lane={laneEntity}, role={roleName}";
            EnforcementLoggingPolicy.RecordAllowedType3Usage(message);
        }
    }
}
