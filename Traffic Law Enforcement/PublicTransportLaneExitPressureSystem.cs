using Game;
using Game.Pathfind;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public class PublicTransportLaneExitPressureSystem : GameSystemBase
    {
        private EntityQuery m_ViolationQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_ViolationQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadWrite<PathOwner>(),
                ComponentType.ReadWrite<PublicTransportLaneViolation>());
            RequireForUpdate(m_ViolationQuery);
        }

        protected override void OnUpdate()
        {
            if (!Mod.IsPublicTransportLaneEnforcementEnabled || !EnforcementGameTime.IsInitialized)
            {
                return;
            }

            long thresholdDayTicks = (long)System.Math.Round(System.Math.Max(0f, EnforcementGameplaySettingsService.Current.PublicTransportLaneExitPressureThresholdDays) * EnforcementGameTime.DayTicksPerDay);
            long currentDayTicks = EnforcementGameTime.CurrentTimestampDayTicks;

            NativeArray<Entity> vehicles = m_ViolationQuery.ToEntityArray(Allocator.Temp);
            NativeArray<Car> cars = m_ViolationQuery.ToComponentDataArray<Car>(Allocator.Temp);
            NativeArray<PathOwner> pathOwners = m_ViolationQuery.ToComponentDataArray<PathOwner>(Allocator.Temp);
            NativeArray<PublicTransportLaneViolation> violations = m_ViolationQuery.ToComponentDataArray<PublicTransportLaneViolation>(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index++)
                {
                    if (EmergencyVehiclePolicy.IsEmergencyVehicle(cars[index]))
                    {
                        continue;
                    }

                    PublicTransportLaneViolation violation = violations[index];
                    if (violation.m_ExitPressureApplied)
                    {
                        continue;
                    }

                    long elapsedDayTicks = currentDayTicks - violation.m_StartDayTicks;
                    if (elapsedDayTicks < thresholdDayTicks)
                    {
                        continue;
                    }

                    PathOwner pathOwner = pathOwners[index];
                    if ((pathOwner.m_State & PathFlags.Pending) != 0)
                    {
                        continue;
                    }

                    if ((pathOwner.m_State & PathFlags.Obsolete) == 0)
                    {
                        pathOwner.m_State |= PathFlags.Obsolete;
                        EntityManager.SetComponentData(vehicles[index], pathOwner);
                        if (EnforcementLoggingPolicy.ShouldLogEnforcementEvents())
                        {
                            Mod.log.Info($"Applied bus-lane exit pressure: vehicle={vehicles[index]}, lane={violation.m_Lane}, elapsedDayTicks={elapsedDayTicks}, thresholdDayTicks={thresholdDayTicks}");
                        }
                    }

                    violation.m_ExitPressureApplied = true;
                    EntityManager.SetComponentData(vehicles[index], violation);
                }
            }
            finally
            {
                vehicles.Dispose();
                cars.Dispose();
                pathOwners.Dispose();
                violations.Dispose();
            }
        }
    }
}
