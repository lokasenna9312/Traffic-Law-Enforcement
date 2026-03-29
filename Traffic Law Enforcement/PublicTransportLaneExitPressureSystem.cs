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
        private ComponentLookup<CarCurrentLane> m_CurrentLaneData;
        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;
        private ComponentLookup<Car> m_CarData;
        private ComponentLookup<PathOwner> m_PathOwnerData;
        private ComponentLookup<PublicTransportLaneViolation> m_ViolationData;
        protected override void OnCreate()
        {
            base.OnCreate();
            m_ViolationQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadWrite<PathOwner>(),
                ComponentType.ReadWrite<PublicTransportLaneViolation>());
            RequireForUpdate(m_ViolationQuery);
            m_CurrentLaneData = GetComponentLookup<CarCurrentLane>(true);
            m_CarData = GetComponentLookup<Car>(true);
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

            m_CurrentLaneData.Update(this);
            m_CarData.Update(this);
            m_PathOwnerData.Update(this);
            m_ViolationData.Update(this);
            m_TypeLookups.Update(this);

            long thresholdDayTicks = (long)System.Math.Round(System.Math.Max(0f, EnforcementGameplaySettingsService.Current.PublicTransportLaneExitPressureThresholdDays) * EnforcementGameTime.DayTicksPerDay);
            long currentDayTicks = EnforcementGameTime.CurrentTimestampDayTicks;

            NativeArray<Entity> vehicles = m_ViolationQuery.ToEntityArray(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index += 1)
                {
                    Entity vehicle = vehicles[index];

                    if (!m_CarData.TryGetComponent(vehicle, out Car car))
                    {
                        continue;
                    }

                    if (EmergencyVehiclePolicy.IsEmergencyVehicle(car))
                    {
                        continue;
                    }

                    if (!m_ViolationData.TryGetComponent(vehicle, out PublicTransportLaneViolation violation))
                    {
                        continue;
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

                        Entity currentLane = m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLaneData)
                            ? currentLaneData.m_Lane
                            : violation.m_Lane;

                        string role = PublicTransportLanePolicy.DescribeVehicleRole(vehicle, ref m_TypeLookups);
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
            finally
            {
                vehicles.Dispose();
            }
        }
    }
}
