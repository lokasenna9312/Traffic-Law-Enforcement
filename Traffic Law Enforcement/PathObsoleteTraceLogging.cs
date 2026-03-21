using System;
using Game.Pathfind;
using Game.Vehicles;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public static class PathObsoleteTraceLogging
    {
        public static void Record(
            string sourceSystem,
            Entity vehicle,
            Entity currentLane,
            PathFlags stateBefore,
            PathFlags stateAfter,
            string reason,
            Car car,
            string role,
            string extra = null)
        {
            if (!EnforcementLoggingPolicy.ShouldLogPathObsoleteSources())
            {
                return;
            }

            bool emergency = EmergencyVehiclePolicy.IsEmergencyVehicle(car);
            bool usePublicTransportLanes = (car.m_Flags & CarFlags.UsePublicTransportLanes) != 0;
            string suffix = string.IsNullOrWhiteSpace(extra) ? string.Empty : ", " + extra;

            Mod.log.Info(
                $"[OBSOLETE_TRACE] by={sourceSystem}, vehicle={vehicle}, currentLane={currentLane}, " +
                $"pathStateBefore={stateBefore}, pathStateAfter={stateAfter}, reason={reason}, " +
                $"emergency={emergency}, usePTFlag={usePublicTransportLanes}, role={role}, carFlags={car.m_Flags}{suffix}");
        }
    }
}