using Game;
using Game.Pathfind;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public partial class PublicTransportLaneExitPressureSystem : GameSystemBase
    {
        private EntityQuery m_ViolationQuery;
        private EntityQuery m_ChangedViolationQuery;
        private EntityTypeHandle m_EntityTypeHandle;
        private ComponentTypeHandle<Car> m_CarTypeHandle;
        private ComponentLookup<CarCurrentLane> m_CurrentLaneData;
        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;
        private ComponentLookup<PathOwner> m_PathOwnerData;
        private ComponentLookup<PublicTransportLaneViolation> m_ViolationData;
        private long m_NextPressureEvaluationDayTicks = long.MaxValue;
        private long m_LastThresholdDayTicks = long.MinValue;
        protected override void OnCreate()
        {
            base.OnCreate();
            m_ViolationQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadWrite<PathOwner>(),
                ComponentType.ReadWrite<PublicTransportLaneViolation>());
            m_ChangedViolationQuery = GetEntityQuery(
                ComponentType.ReadOnly<PublicTransportLaneViolation>());
            m_ChangedViolationQuery.SetChangedVersionFilter(
                ComponentType.ReadOnly<PublicTransportLaneViolation>());
            RequireForUpdate(m_ViolationQuery);
            m_CurrentLaneData = GetComponentLookup<CarCurrentLane>(true);
            m_PathOwnerData = GetComponentLookup<PathOwner>();
            m_ViolationData = GetComponentLookup<PublicTransportLaneViolation>();
            m_TypeLookups = PublicTransportLaneVehicleTypeLookups.Create(this);
        }

        protected override void OnUpdate()
        {
            if (!Mod.IsPublicTransportLaneEnforcementEnabled || !EnforcementGameTime.IsInitialized)
            {
                return;
            }

            long thresholdDayTicks = (long)System.Math.Round(System.Math.Max(0f, EnforcementGameplaySettingsService.Current.PublicTransportLaneExitPressureThresholdDays) * EnforcementGameTime.DayTicksPerDay);
            long currentDayTicks = EnforcementGameTime.CurrentTimestampDayTicks;

            if (thresholdDayTicks == m_LastThresholdDayTicks &&
                currentDayTicks < m_NextPressureEvaluationDayTicks &&
                m_ChangedViolationQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            m_PathOwnerData.Update(this);
            m_ViolationData.Update(this);
            m_EntityTypeHandle = GetEntityTypeHandle();
            m_CarTypeHandle = GetComponentTypeHandle<Car>(true);

            NativeArray<ArchetypeChunk> chunks = m_ViolationQuery.ToArchetypeChunkArray(Allocator.Temp);
            long nextPressureEvaluationDayTicks = long.MaxValue;
            bool currentLaneDataReady = false;
            bool typeLookupsReady = false;
            try
            {
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex += 1)
                {
                    ArchetypeChunk chunk = chunks[chunkIndex];
                    NativeArray<Entity> vehicles = chunk.GetNativeArray(m_EntityTypeHandle);
                    NativeArray<Car> cars = chunk.GetNativeArray(ref m_CarTypeHandle);

                    for (int index = 0; index < vehicles.Length; index += 1)
                    {
                        Entity vehicle = vehicles[index];
                        Car car = cars[index];

                        if (EmergencyVehiclePolicy.IsEmergencyVehicle(car))
                        {
                            continue;
                        }

                        if (!m_ViolationData.TryGetComponent(vehicle, out PublicTransportLaneViolation violation))
                        {
                            continue;
                        }

                        if (!violation.m_ExitPressureApplied)
                        {
                            long vehiclePressureEvaluationDayTicks =
                                violation.m_StartDayTicks + thresholdDayTicks;
                            if (vehiclePressureEvaluationDayTicks < nextPressureEvaluationDayTicks)
                            {
                                nextPressureEvaluationDayTicks = vehiclePressureEvaluationDayTicks;
                            }
                        }

                        if (violation.m_ExitPressureApplied)
                        {
                            continue;
                        }

                        long elapsedDayTicks = currentDayTicks - violation.m_StartDayTicks;
                        if (elapsedDayTicks < thresholdDayTicks)
                        {
                            continue;
                        }

                        if (!m_PathOwnerData.TryGetComponent(vehicle, out PathOwner pathOwner))
                        {
                            continue;
                        }

                        if ((pathOwner.m_State & PathFlags.Pending) != 0)
                        {
                            continue;
                        }

                        if ((pathOwner.m_State & PathFlags.Obsolete) == 0)
                        {
                            PathFlags stateBefore = pathOwner.m_State;
                            pathOwner.m_State |= PathFlags.Obsolete;
                            EntityManager.SetComponentData(vehicle, pathOwner);
                            bool shouldLogPathObsoleteTrace =
                                EnforcementLoggingPolicy
                                    .ShouldLogVehicleSpecificPathObsoleteSource(
                                        vehicle);

                            if (!currentLaneDataReady)
                            {
                                m_CurrentLaneData.Update(this);
                                currentLaneDataReady = true;
                            }

                            Entity currentLane = m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLaneData)
                                ? currentLaneData.m_Lane
                                : violation.m_Lane;

                            if (shouldLogPathObsoleteTrace &&
                                !typeLookupsReady)
                            {
                                m_TypeLookups.Update(this);
                                typeLookupsReady = true;
                            }

                            string role =
                                shouldLogPathObsoleteTrace
                                    ? PublicTransportLanePolicy.DescribeVehicleRole(vehicle, ref m_TypeLookups)
                                    : null;
                            string reason = "PT-lane-exit-pressure-threshold-reached";
                            string extra =
                                $"violationLane={violation.m_Lane}, elapsedDayTicks={elapsedDayTicks}, " +
                                $"thresholdDayTicks={thresholdDayTicks}, exitPressureAppliedBefore={violation.m_ExitPressureApplied}";

                            PathObsoleteTraceLogging.Record(
                                "PT_EXIT_PRESSURE",
                                vehicle,
                                currentLane,
                                stateBefore,
                                pathOwner.m_State,
                                reason,
                                car,
                                role,
                                extra);

                            if (EnforcementLoggingPolicy.ShouldLogVehicleSpecificEnforcementEvent(vehicle))
                            {
                                Mod.log.Info($"Applied PT-lane exit pressure: vehicle={vehicle}, lane={violation.m_Lane}, elapsedDayTicks={elapsedDayTicks}, thresholdDayTicks={thresholdDayTicks}");
                            }
                        }

                        violation.m_ExitPressureApplied = true;
                        EntityManager.SetComponentData(vehicle, violation);
                    }
                }
            }
            finally
            {
                chunks.Dispose();
            }

            m_LastThresholdDayTicks = thresholdDayTicks;
            m_NextPressureEvaluationDayTicks = nextPressureEvaluationDayTicks;
        }
    }
}
