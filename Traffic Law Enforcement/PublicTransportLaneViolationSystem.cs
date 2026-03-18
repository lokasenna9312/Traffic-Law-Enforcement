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

            m_CarData = GetComponentLookup<Car>(true);
            m_CarLaneData = GetComponentLookup<CarLane>(true);
            m_ViolationData = GetComponentLookup<PublicTransportLaneViolation>();
            m_Type3UsageData = GetComponentLookup<PublicTransportLaneType3UsageState>();
            m_TypeLookups = BusLaneVehicleTypeLookups.Create(this);
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

                TrafficLawEnforcementStatistics disabledStats = EntityManager.GetComponentData<TrafficLawEnforcementStatistics>(m_StatisticsEntity);
                if (disabledStats.m_ActivePublicTransportLaneViolatorCount != 0)
                {
                    disabledStats.m_ActivePublicTransportLaneViolatorCount = 0;
                    EntityManager.SetComponentData(m_StatisticsEntity, disabledStats);
                }
                EnforcementTelemetry.SetStatistics(disabledStats);

                m_HasEvaluated = false;
                m_LastEnforcementEnabled = false;
                return;
            }

            m_CarData.Update(this);
            m_CarLaneData.Update(this);
            m_ViolationData.Update(this);
            m_Type3UsageData.Update(this);
            m_TypeLookups.Update(this);

            TrafficLawEnforcementStatistics statistics = EntityManager.GetComponentData<TrafficLawEnforcementStatistics>(m_StatisticsEntity);
            bool statisticsChanged = false;
            bool fullRefresh = !m_HasEvaluated || !m_LastEnforcementEnabled;

            if (fullRefresh)
            {
                EvaluateQuery(m_CarQuery, settings, ref statistics, ref statisticsChanged);
            }
            else
            {
                EvaluateQuery(m_ChangedLaneQuery, settings, ref statistics, ref statisticsChanged);
                EvaluateQuery(m_ChangedCarQuery, settings, ref statistics, ref statisticsChanged);
            }

            int activeViolatorCount = m_ViolationQuery.CalculateEntityCount();
            if (statistics.m_ActivePublicTransportLaneViolatorCount != activeViolatorCount)
            {
                statistics.m_ActivePublicTransportLaneViolatorCount = activeViolatorCount;
                statisticsChanged = true;
            }

            if (statisticsChanged)
            {
                EntityManager.SetComponentData(m_StatisticsEntity, statistics);
            }
            EnforcementTelemetry.SetStatistics(statistics);

            m_HasEvaluated = true;
            m_LastEnforcementEnabled = true;
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
