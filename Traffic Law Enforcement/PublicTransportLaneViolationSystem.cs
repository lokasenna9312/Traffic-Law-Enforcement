using Game;
using Game.Net;
using Game.Vehicles;
using Game.SceneFlow;
using Traffic_Law_Enforcement;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public partial class PublicTransportLaneViolationSystem : GameSystemBase
    {
        private EntityQuery m_CarQuery;
        private EntityQuery m_ChangedLaneQuery;
        private EntityQuery m_ChangedCarQuery;
        private EntityQuery m_ViolationQuery;
        private EntityQuery m_EventBufferQuery;
        private Entity m_EventEntity;
        private EntityQuery m_Type2UsageQuery;
        private EntityQuery m_Type3UsageQuery;
        private EntityQuery m_Type4UsageQuery;
        private EntityQuery m_StatisticsQuery;
        private Entity m_StatisticsEntity;
        private ComponentLookup<Car> m_CarData;
        private ComponentLookup<CarLane> m_CarLaneData;
        private ComponentLookup<PublicTransportLaneViolation> m_ViolationData;
        private ComponentLookup<PublicTransportLaneType3UsageState> m_Type3UsageData;
        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;
        private bool m_HasEvaluated;
        private bool m_LastEnforcementEnabled;
        private const int kVehiclesPerFrame = 512;
        private NativeList<Entity> m_PendingRefreshVehicles;
        private int m_RefreshCursor;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CarQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>());
            m_ChangedLaneQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>());
            m_ChangedLaneQuery.SetChangedVersionFilter(ComponentType.ReadOnly<CarCurrentLane>());
            m_ChangedCarQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>());
            m_ChangedCarQuery.SetChangedVersionFilter(ComponentType.ReadOnly<Car>());
            m_ViolationQuery = GetEntityQuery(ComponentType.ReadOnly<PublicTransportLaneViolation>());
            m_Type2UsageQuery = GetEntityQuery(ComponentType.ReadOnly<PublicTransportLaneType2UsageState>());
            m_Type3UsageQuery = GetEntityQuery(ComponentType.ReadOnly<PublicTransportLaneType3UsageState>());
            m_Type4UsageQuery = GetEntityQuery(ComponentType.ReadOnly<PublicTransportLaneType4UsageState>());
            m_StatisticsQuery = GetEntityQuery(ComponentType.ReadWrite<TrafficLawEnforcementStatistics>());
            if (m_StatisticsQuery.IsEmptyIgnoreFilter)
            {
                m_StatisticsEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(m_StatisticsEntity, default(TrafficLawEnforcementStatistics));
            }
            else
            {
                m_StatisticsEntity = m_StatisticsQuery.GetSingletonEntity();
            }
            m_EventBufferQuery = GetEntityQuery(
                ComponentType.ReadOnly<PublicTransportLaneEventBufferTag>(),
                ComponentType.ReadWrite<DetectedPublicTransportLaneEvent>());

            if (m_EventBufferQuery.IsEmptyIgnoreFilter)
            {
                m_EventEntity = EntityManager.CreateEntity();
                EntityManager.AddComponent<PublicTransportLaneEventBufferTag>(m_EventEntity);
                EntityManager.AddBuffer<DetectedPublicTransportLaneEvent>(m_EventEntity);
            }
            else
            {
                m_EventEntity = m_EventBufferQuery.GetSingletonEntity();
            }
            m_CarData = GetComponentLookup<Car>(true);
            m_CarLaneData = GetComponentLookup<CarLane>(true);
            m_ViolationData = GetComponentLookup<PublicTransportLaneViolation>();
            m_Type3UsageData = GetComponentLookup<PublicTransportLaneType3UsageState>();
            m_TypeLookups = PublicTransportLaneVehicleTypeLookups.Create(this);
            m_PendingRefreshVehicles = new NativeList<Entity>(Allocator.Persistent);    
            RequireForUpdate(m_CarQuery);
        }

        protected override void OnUpdate()
        {
            EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;

            bool enforcementEnabled = Mod.IsPublicTransportLaneEnforcementEnabled;
            if (!enforcementEnabled)
            {
                if (!m_ViolationQuery.IsEmptyIgnoreFilter)
                {
                    EntityManager.RemoveComponent<PublicTransportLaneViolation>(m_ViolationQuery);
                }

                if (!m_Type2UsageQuery.IsEmptyIgnoreFilter)
                {
                    EntityManager.RemoveComponent<PublicTransportLaneType2UsageState>(m_Type2UsageQuery);
                }

                if (!m_Type3UsageQuery.IsEmptyIgnoreFilter)
                {
                    EntityManager.RemoveComponent<PublicTransportLaneType3UsageState>(m_Type3UsageQuery);
                }

                if (!m_Type4UsageQuery.IsEmptyIgnoreFilter)
                {
                    EntityManager.RemoveComponent<PublicTransportLaneType4UsageState>(m_Type4UsageQuery);
                }

                TrafficLawEnforcementStatistics disabledStats = EntityManager.GetComponentData<TrafficLawEnforcementStatistics>(m_StatisticsEntity);
                if (disabledStats.m_ActivePublicTransportLaneViolatorCount != 0)
                {
                    disabledStats.m_ActivePublicTransportLaneViolatorCount = 0;
                    EntityManager.SetComponentData(m_StatisticsEntity, disabledStats);
                }
                EnforcementTelemetry.SetStatistics(disabledStats);

                ClearPendingRefresh();
                m_HasEvaluated = false;
                m_LastEnforcementEnabled = false;
                return;
            }

            m_CarData.Update(this);
            m_CarLaneData.Update(this);
            m_ViolationData.Update(this);
            m_Type3UsageData.Update(this);
            m_TypeLookups.Update(this);

            if (m_EventEntity == Entity.Null || !EntityManager.Exists(m_EventEntity))
            {
                m_EventEntity = m_EventBufferQuery.GetSingletonEntity();
            }

            DynamicBuffer<DetectedPublicTransportLaneEvent> events =
                EntityManager.GetBuffer<DetectedPublicTransportLaneEvent>(m_EventEntity);
            events.Clear();

            TrafficLawEnforcementStatistics statistics = EntityManager.GetComponentData<TrafficLawEnforcementStatistics>(m_StatisticsEntity);
            bool statisticsChanged = false;
            bool fullRefresh = !m_HasEvaluated || !m_LastEnforcementEnabled;

            if (fullRefresh && m_PendingRefreshVehicles.Length == 0)
            {
                BuildPendingRefreshList();
            }

            if (m_PendingRefreshVehicles.Length > 0)
            {
                ProcessRefreshBatch(settings, events);

                UpdateActiveViolatorStatistics(ref statistics, ref statisticsChanged);

                if (statisticsChanged)
                {
                    EntityManager.SetComponentData(m_StatisticsEntity, statistics);
                }

                EnforcementTelemetry.SetStatistics(statistics);

                if (m_PendingRefreshVehicles.Length == 0)
                {
                    m_HasEvaluated = true;
                    m_LastEnforcementEnabled = true;
                }

                return;
            }

            EvaluateQuery(m_ChangedLaneQuery, settings, events);
            EvaluateQuery(m_ChangedCarQuery, settings, events);

            UpdateActiveViolatorStatistics(ref statistics, ref statisticsChanged);

            if (statisticsChanged)
            {
                EntityManager.SetComponentData(m_StatisticsEntity, statistics);
            }
            EnforcementTelemetry.SetStatistics(statistics);

            m_HasEvaluated = true;
            m_LastEnforcementEnabled = true;
        }

        protected override void OnDestroy()
        {
            if (m_PendingRefreshVehicles.IsCreated)
            {
                m_PendingRefreshVehicles.Dispose();
            }

            base.OnDestroy();
        }

        private void UpdateActiveViolatorStatistics(
            ref TrafficLawEnforcementStatistics statistics,
            ref bool statisticsChanged)
        {
            int activeViolatorCount = m_ViolationQuery.CalculateEntityCount();
            if (statistics.m_ActivePublicTransportLaneViolatorCount != activeViolatorCount)
            {
                statistics.m_ActivePublicTransportLaneViolatorCount = activeViolatorCount;
                statisticsChanged = true;
            }
        }

        private void BuildPendingRefreshList()
        {
            ClearPendingRefresh();

            NativeArray<Entity> vehicles = m_CarQuery.ToEntityArray(Allocator.Temp);
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
            DynamicBuffer<DetectedPublicTransportLaneEvent> events)
        {
            int end = System.Math.Min(
                m_RefreshCursor + kVehiclesPerFrame,
                m_PendingRefreshVehicles.Length);

            for (int index = m_RefreshCursor; index < end; index += 1)
            {
                Entity vehicle = m_PendingRefreshVehicles[index];
                if (!EntityManager.Exists(vehicle) ||
                    !EntityManager.HasComponent<Car>(vehicle) ||
                    !EntityManager.HasComponent<CarCurrentLane>(vehicle))
                {
                    continue;
                }

                CarCurrentLane currentLane = EntityManager.GetComponentData<CarCurrentLane>(vehicle);
                EvaluateVehicle(vehicle, currentLane, settings, events);
            }

            m_RefreshCursor = end;

            if (m_RefreshCursor >= m_PendingRefreshVehicles.Length)
            {
                ClearPendingRefresh();
            }
        }

        private void EvaluateQuery(EntityQuery query, EnforcementGameplaySettingsState settings, DynamicBuffer<DetectedPublicTransportLaneEvent> events)
        {
            NativeArray<Entity> vehicles = query.ToEntityArray(Allocator.Temp);
            NativeArray<CarCurrentLane> currentLanes = query.ToComponentDataArray<CarCurrentLane>(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index++)
                {
                    EvaluateVehicle(vehicles[index], currentLanes[index], settings, events);
                }
            }
            finally
            {
                vehicles.Dispose();
                currentLanes.Dispose();
            }
        }

        private void EvaluateVehicle(Entity vehicle, CarCurrentLane currentLane, EnforcementGameplaySettingsState settings, DynamicBuffer<DetectedPublicTransportLaneEvent> events)
        {
            Entity laneEntity = currentLane.m_Lane;
            bool isViolation = false;
            bool shouldLogType2Usage = false;
            bool shouldLogType3Usage = false;
            bool shouldLogType4Usage = false;

            if (laneEntity != Entity.Null &&
                m_CarLaneData.TryGetComponent(laneEntity, out CarLane laneData) &&
                (laneData.m_Flags & Game.Net.CarLaneFlags.PublicOnly) != 0)
            {
                if (!IsEmergencyVehicle(vehicle))
                {
                    bool hasPermission = PublicTransportLanePolicy.HasPublicTransportLanePermissionFlag(vehicle, ref m_TypeLookups);
                    isViolation = !hasPermission;
                    var authorizedCategories = PublicTransportLanePolicy.GetVanillaAuthorizedCategories(vehicle, ref m_TypeLookups);
                    var additionalRole = PublicTransportLanePolicy.GetFlagGrantExperimentRole(vehicle, ref m_TypeLookups);

                    // --- Type 2: Vanilla allowed, Mod denied ---
                    bool isType2 = !hasPermission && authorizedCategories != PublicTransportLaneVehicleCategory.None && !settings.AllowsPublicTransportLaneCategories(authorizedCategories);
                    shouldLogType2Usage = isType2;

                    // --- Type 3: Vanilla denied, Mod allowed ---
                    bool isType3 = hasPermission && PublicTransportLanePolicy.TryGetAllowedType3Role(vehicle, settings, ref m_TypeLookups, out _);
                    shouldLogType3Usage = isType3;

                    // --- Type 4: Vanilla denied, Mod denied ---
                    bool isType4 = !hasPermission && authorizedCategories == PublicTransportLaneVehicleCategory.None && additionalRole == PublicTransportLaneFlagGrantExperimentRole.None;
                    shouldLogType4Usage = isType4;
                }
            }

            UpdateType2UsageObservation(vehicle, laneEntity, shouldLogType2Usage, events);
            UpdateType3UsageObservation(vehicle, laneEntity, shouldLogType3Usage, events);
            UpdateType4UsageObservation(vehicle, laneEntity, shouldLogType4Usage, events);

            // --- Violation tracing and logging ---
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
                events.Add(new DetectedPublicTransportLaneEvent
                {
                    Vehicle = vehicle,
                    Lane = laneEntity,
                    Kind = PublicTransportLaneEventKind.ViolationStart,
                });
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
        // --- Trace and Log Type 2 ---
        private void UpdateType2UsageObservation(Entity vehicle, Entity laneEntity, bool shouldLogType2Usage, DynamicBuffer<DetectedPublicTransportLaneEvent> events)
        {
            bool hasUsageState = EntityManager.HasComponent<PublicTransportLaneType2UsageState>(vehicle);
            if (!shouldLogType2Usage || laneEntity == Entity.Null)
            {
                if (hasUsageState)
                {
                    EntityManager.RemoveComponent<PublicTransportLaneType2UsageState>(vehicle);
                }
                return;
            }

            if (!hasUsageState)
            {
                EntityManager.AddComponentData(vehicle, new PublicTransportLaneType2UsageState
                {
                    m_Lane = laneEntity,
                });
                events.Add(new DetectedPublicTransportLaneEvent
                {
                    Vehicle = vehicle,
                    Lane = laneEntity,
                    Kind = PublicTransportLaneEventKind.UsageType2,
                });
                return;
            }

            var state = EntityManager.GetComponentData<PublicTransportLaneType2UsageState>(vehicle);
            if (state.m_Lane == laneEntity)
            {
                return;
            }

            state.m_Lane = laneEntity;
            EntityManager.SetComponentData(vehicle, state);
            events.Add(new DetectedPublicTransportLaneEvent
            {
                Vehicle = vehicle,
                Lane = laneEntity,
                Kind = PublicTransportLaneEventKind.UsageType2,
            });
        }

        // --- Trace and Log Type 3 ---
        private void UpdateType3UsageObservation(Entity vehicle, Entity laneEntity, bool shouldLogType3Usage, DynamicBuffer<DetectedPublicTransportLaneEvent> events)
        {
            bool hasUsageState = EntityManager.HasComponent<PublicTransportLaneType3UsageState>(vehicle);
            if (!shouldLogType3Usage || laneEntity == Entity.Null)
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
                events.Add(new DetectedPublicTransportLaneEvent
                {
                    Vehicle = vehicle,
                    Lane = laneEntity,
                    Kind = PublicTransportLaneEventKind.UsageType3,
                });
                return;
            }

            var state = EntityManager.GetComponentData<PublicTransportLaneType3UsageState>(vehicle);
            if (state.m_Lane == laneEntity)
            {
                return;
            }

            state.m_Lane = laneEntity;
            EntityManager.SetComponentData(vehicle, state);
            events.Add(new DetectedPublicTransportLaneEvent
            {
                Vehicle = vehicle,
                Lane = laneEntity,
                Kind = PublicTransportLaneEventKind.UsageType3,
            });
            return;
        }

        // --- Trace and Log Type 4 ---
        private void UpdateType4UsageObservation(Entity vehicle, Entity laneEntity, bool shouldLogType4Usage, DynamicBuffer<DetectedPublicTransportLaneEvent> events)
        {
            bool hasUsageState = EntityManager.HasComponent<PublicTransportLaneType4UsageState>(vehicle);
            if (!shouldLogType4Usage || laneEntity == Entity.Null)
            {
                if (hasUsageState)
                {
                    EntityManager.RemoveComponent<PublicTransportLaneType4UsageState>(vehicle);
                }
                return;
            }

            if (!hasUsageState)
            {
                EntityManager.AddComponentData(vehicle, new PublicTransportLaneType4UsageState
                {
                    m_Lane = laneEntity,
                });
                events.Add(new DetectedPublicTransportLaneEvent
                {
                    Vehicle = vehicle,
                    Lane = laneEntity,
                    Kind = PublicTransportLaneEventKind.UsageType4,
                });
                return;
            }

            var state = EntityManager.GetComponentData<PublicTransportLaneType4UsageState>(vehicle);
            if (state.m_Lane == laneEntity)
            {
                return;
            }

            state.m_Lane = laneEntity;
            EntityManager.SetComponentData(vehicle, state);
            events.Add(new DetectedPublicTransportLaneEvent
            {
                Vehicle = vehicle,
                Lane = laneEntity,
                Kind = PublicTransportLaneEventKind.UsageType4,
            });
            return;
        }
        
        private bool IsEmergencyVehicle(Entity vehicle)
        {
            return m_CarData.TryGetComponent(vehicle, out Car car) && EmergencyVehiclePolicy.IsEmergencyVehicle(car);
        }
    }
}
