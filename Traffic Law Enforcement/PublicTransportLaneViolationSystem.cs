using Game;
using Game.Net;
using Game.Vehicles;
using Game.SceneFlow;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;
using System.Collections.Generic;

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
        private ComponentLookup<Car> m_CarData;
        private ComponentLookup<CarLane> m_CarLaneData;
        private ComponentLookup<PublicTransportLaneViolation> m_ViolationData;
        private ComponentLookup<VehicleTrafficLawProfile> m_ProfileData;
        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;
        private EntityTypeHandle m_EntityTypeHandle;
        private ComponentTypeHandle<CarCurrentLane> m_CurrentLaneTypeHandle;
        private HashSet<Entity> m_ProcessedThisFrame;
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
            m_ProfileData = GetComponentLookup<VehicleTrafficLawProfile>(true);
            m_Type2UsageQuery = GetEntityQuery(ComponentType.ReadOnly<PublicTransportLaneType2UsageState>());
            m_Type3UsageQuery = GetEntityQuery(ComponentType.ReadOnly<PublicTransportLaneType3UsageState>());
            m_Type4UsageQuery = GetEntityQuery(ComponentType.ReadOnly<PublicTransportLaneType4UsageState>());
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
            m_TypeLookups = PublicTransportLaneVehicleTypeLookups.Create(this);
            m_PendingRefreshVehicles = new NativeList<Entity>(Allocator.Persistent);    
            m_ProcessedThisFrame = new HashSet<Entity>();
            RequireForUpdate(m_CarQuery);
        }

        protected override void OnUpdate()
        {
            EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;
            bool observeType2Usage = EnforcementLoggingPolicy.ShouldLogType2Usage();
            bool observeType3Usage = EnforcementLoggingPolicy.ShouldLogType3Usage();
            bool observeType4Usage = EnforcementLoggingPolicy.ShouldLogType4Usage();

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

                ClearPendingRefresh();
                m_HasEvaluated = false;
                m_LastEnforcementEnabled = false;
                return;
            }

            if (!observeType2Usage && !m_Type2UsageQuery.IsEmptyIgnoreFilter)
            {
                EntityManager.RemoveComponent<PublicTransportLaneType2UsageState>(m_Type2UsageQuery);
            }

            if (!observeType3Usage && !m_Type3UsageQuery.IsEmptyIgnoreFilter)
            {
                EntityManager.RemoveComponent<PublicTransportLaneType3UsageState>(m_Type3UsageQuery);
            }

            if (!observeType4Usage && !m_Type4UsageQuery.IsEmptyIgnoreFilter)
            {
                EntityManager.RemoveComponent<PublicTransportLaneType4UsageState>(m_Type4UsageQuery);
            }

            if (m_EventEntity == Entity.Null || !EntityManager.Exists(m_EventEntity))
            {
                m_EventEntity = m_EventBufferQuery.GetSingletonEntity();
            }

            DynamicBuffer<DetectedPublicTransportLaneEvent> events =
                EntityManager.GetBuffer<DetectedPublicTransportLaneEvent>(m_EventEntity);
            if (events.Length > 0)
            {
                events.Clear();
            }

            bool fullRefresh = !m_HasEvaluated || !m_LastEnforcementEnabled;

            if (!fullRefresh &&
                m_PendingRefreshVehicles.Length == 0 &&
                m_ChangedLaneQuery.IsEmptyIgnoreFilter &&
                m_ChangedCarQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            m_CarData.Update(this);
            m_CarLaneData.Update(this);
            m_ProfileData.Update(this);
            m_ViolationData.Update(this);
            m_TypeLookups.Update(this);
            m_EntityTypeHandle = GetEntityTypeHandle();
            m_CurrentLaneTypeHandle = GetComponentTypeHandle<CarCurrentLane>(true);

            if (fullRefresh && m_PendingRefreshVehicles.Length == 0)
            {
                BuildPendingRefreshList();
            }

            if (m_PendingRefreshVehicles.Length > 0)
            {
                ProcessRefreshBatch(
                    settings,
                    events,
                    observeType2Usage,
                    observeType3Usage,
                    observeType4Usage);

                if (m_PendingRefreshVehicles.Length == 0)
                {
                    m_HasEvaluated = true;
                    m_LastEnforcementEnabled = true;
                }

                return;
            }

            BeginSteadyStateEvaluation();
            EvaluateQueryDeduplicated(
                m_ChangedLaneQuery,
                settings,
                events,
                observeType2Usage,
                observeType3Usage,
                observeType4Usage);
            EvaluateQueryDeduplicated(
                m_ChangedCarQuery,
                settings,
                events,
                observeType2Usage,
                observeType3Usage,
                observeType4Usage);

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

        private void BuildPendingRefreshList()
        {
            ClearPendingRefresh();

            NativeArray<ArchetypeChunk> chunks = m_CarQuery.ToArchetypeChunkArray(Allocator.Temp);
            try
            {
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex += 1)
                {
                    NativeArray<Entity> vehicles = chunks[chunkIndex].GetNativeArray(m_EntityTypeHandle);
                    for (int index = 0; index < vehicles.Length; index += 1)
                    {
                        m_PendingRefreshVehicles.Add(vehicles[index]);
                    }
                }
            }
            finally
            {
                chunks.Dispose();
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
            DynamicBuffer<DetectedPublicTransportLaneEvent> events,
            bool observeType2Usage,
            bool observeType3Usage,
            bool observeType4Usage)
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
                EvaluateVehicle(
                    vehicle,
                    currentLane,
                    settings,
                    events,
                    observeType2Usage,
                    observeType3Usage,
                    observeType4Usage);
            }

            m_RefreshCursor = end;

            if (m_RefreshCursor >= m_PendingRefreshVehicles.Length)
            {
                ClearPendingRefresh();
            }
        }

        private void BeginSteadyStateEvaluation()
        {
            m_ProcessedThisFrame.Clear();
        }

        private void EvaluateQueryDeduplicated(
            EntityQuery query,
            EnforcementGameplaySettingsState settings,
            DynamicBuffer<DetectedPublicTransportLaneEvent> events,
            bool observeType2Usage,
            bool observeType3Usage,
            bool observeType4Usage)
        {
            NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            try
            {
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex += 1)
                {
                    ArchetypeChunk chunk = chunks[chunkIndex];
                    NativeArray<Entity> vehicles = chunk.GetNativeArray(m_EntityTypeHandle);
                    NativeArray<CarCurrentLane> currentLanes = chunk.GetNativeArray(ref m_CurrentLaneTypeHandle);

                    for (int index = 0; index < vehicles.Length; index += 1)
                    {
                        Entity vehicle = vehicles[index];
                        if (!m_ProcessedThisFrame.Add(vehicle))
                        {
                            continue;
                        }

                        EvaluateVehicle(
                            vehicle,
                            currentLanes[index],
                            settings,
                            events,
                            observeType2Usage,
                            observeType3Usage,
                            observeType4Usage);
                    }
                }
            }
            finally
            {
                chunks.Dispose();
            }
        }

        private void EvaluateVehicle(
            Entity vehicle,
            CarCurrentLane currentLane,
            EnforcementGameplaySettingsState settings,
            DynamicBuffer<DetectedPublicTransportLaneEvent> events,
            bool observeType2Usage,
            bool observeType3Usage,
            bool observeType4Usage)
        {
            Entity laneEntity = currentLane.m_Lane;
            bool isViolation = false;
            bool shouldTrackType2Usage = false;
            bool shouldTrackType3Usage = false;
            bool shouldTrackType4Usage = false;

            if (laneEntity != Entity.Null &&
                m_CarLaneData.TryGetComponent(laneEntity, out CarLane laneData) &&
                (laneData.m_Flags & Game.Net.CarLaneFlags.PublicOnly) != 0)
            {
                if (!IsEmergencyVehicle(vehicle))
                {
                    if (!m_ProfileData.TryGetComponent(vehicle, out VehicleTrafficLawProfile profile))
                    {
                        return;
                    }

                    PublicTransportLaneAccessBits bits = profile.m_PublicTransportLaneAccessBits;

                    bool configuredModAllowsAccess =
                        PublicTransportLanePolicy.ModAllowsAccess(bits);

                    isViolation = !configuredModAllowsAccess;

                    if (observeType2Usage)
                    {
                        shouldTrackType2Usage = PublicTransportLanePolicy.IsType2(bits);
                    }

                    if (observeType3Usage)
                    {
                        shouldTrackType3Usage = PublicTransportLanePolicy.IsType3(bits);
                    }

                    if (observeType4Usage)
                    {
                        shouldTrackType4Usage = PublicTransportLanePolicy.IsType4(bits);
                    }
                }
            }

            UpdateType2UsageObservation(vehicle, laneEntity, shouldTrackType2Usage, events);
            UpdateType3UsageObservation(vehicle, laneEntity, shouldTrackType3Usage, events);
            UpdateType4UsageObservation(vehicle, laneEntity, shouldTrackType4Usage, events);

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
