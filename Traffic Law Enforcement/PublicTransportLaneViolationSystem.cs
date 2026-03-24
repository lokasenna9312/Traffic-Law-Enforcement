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

            m_CarData.Update(this);
            m_CarLaneData.Update(this);
            m_ProfileData.Update(this);
            m_ViolationData.Update(this);
            m_TypeLookups.Update(this);

            if (m_EventEntity == Entity.Null || !EntityManager.Exists(m_EventEntity))
            {
                m_EventEntity = m_EventBufferQuery.GetSingletonEntity();
            }

            DynamicBuffer<DetectedPublicTransportLaneEvent> events =
                EntityManager.GetBuffer<DetectedPublicTransportLaneEvent>(m_EventEntity);
            events.Clear();

            bool fullRefresh = !m_HasEvaluated || !m_LastEnforcementEnabled;

            if (fullRefresh && m_PendingRefreshVehicles.Length == 0)
            {
                BuildPendingRefreshList();
            }

            if (m_PendingRefreshVehicles.Length > 0)
            {
                ProcessRefreshBatch(settings, events);

                if (m_PendingRefreshVehicles.Length == 0)
                {
                    m_HasEvaluated = true;
                    m_LastEnforcementEnabled = true;
                }

                return;
            }

            BeginSteadyStateEvaluation();
            EvaluateQueryDeduplicated(m_ChangedLaneQuery, settings, events);
            EvaluateQueryDeduplicated(m_ChangedCarQuery, settings, events);

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

        private void BeginSteadyStateEvaluation()
        {
            m_ProcessedThisFrame.Clear();
        }

        private void EvaluateQueryDeduplicated(
            EntityQuery query,
            EnforcementGameplaySettingsState settings,
            DynamicBuffer<DetectedPublicTransportLaneEvent> events)
        {
            NativeArray<Entity> vehicles = query.ToEntityArray(Allocator.Temp);
            NativeArray<CarCurrentLane> currentLanes = query.ToComponentDataArray<CarCurrentLane>(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index += 1)
                {
                    Entity vehicle = vehicles[index];
                    if (!m_ProcessedThisFrame.Add(vehicle))
                    {
                        continue;
                    }

                    EvaluateVehicle(vehicle, currentLanes[index], settings, events);
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
                    if (!m_ProfileData.TryGetComponent(vehicle, out VehicleTrafficLawProfile profile))
                    {
                        return;
                    }

                    PublicTransportLaneAccessBits bits = profile.m_PublicTransportLaneAccessBits;

                    bool modAllowsAccess = PublicTransportLanePolicy.ModAllowsAccess(bits);
                    bool vanillaAllowsAccess = PublicTransportLanePolicy.VanillaAllowsAccess(bits);

                    isViolation = !modAllowsAccess;

                    bool isType2 = PublicTransportLanePolicy.IsType2(bits);
                    bool isType3 = PublicTransportLanePolicy.IsType3(bits);
                    bool isType4 = PublicTransportLanePolicy.IsType4(bits);

                    shouldLogType2Usage = isType2;
                    shouldLogType3Usage = isType3;
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
