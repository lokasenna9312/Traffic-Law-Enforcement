using System;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;
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
            string extra = null,
            bool force = false)
        {
            if (!force && !EnforcementLoggingPolicy.ShouldLogPathObsoleteSources())
            {
                return;
            }

            if (!force &&
                EnforcementLoggingPolicy.ShouldRestrictVehicleSpecificRouteDebugLogsToWatchedVehicles() &&
                !FocusedLoggingService.IsWatched(vehicle))
            {
                return;
            }

            bool emergency = EmergencyVehiclePolicy.IsEmergencyVehicle(car);
            bool usePublicTransportLanes = (car.m_Flags & CarFlags.UsePublicTransportLanes) != 0;
            Entity currentTarget = Entity.Null;
            string targetKindNormalized = RouteDebugNormalization.UnknownTargetKind;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                EntityManager entityManager = world.EntityManager;
                if (vehicle != Entity.Null && entityManager.Exists(vehicle))
                {
                    if (entityManager.HasComponent<Target>(vehicle))
                    {
                        Target target = entityManager.GetComponentData<Target>(vehicle);
                        currentTarget = target.m_Target;
                    }

                    targetKindNormalized =
                        RouteDebugNormalization.NormalizeTargetKind(
                            entityManager,
                            currentTarget);
                }
            }

            string obsoleteAttemptId =
                ObsoleteAttemptCorrelationService.RegisterAttempt(
                    vehicle,
                    currentTarget,
                    targetKindNormalized);
            string liveState = BuildLiveStateSuffix(vehicle);
            string suffix = string.Empty;
            suffix +=
                $", obsoleteAttemptId={obsoleteAttemptId}, targetKindNormalized={targetKindNormalized}";
            if (!string.IsNullOrWhiteSpace(liveState))
            {
                suffix += ", " + liveState;
            }

            if (!string.IsNullOrWhiteSpace(extra))
            {
                suffix += ", " + extra;
            }

            Mod.log.Info(
                $"[OBSOLETE_TRACE] by={sourceSystem}, vehicle={vehicle}, currentLane={currentLane}, " +
                $"pathStateBefore={stateBefore}, pathStateAfter={stateAfter}, reason={reason}, " +
                $"emergency={emergency}, usePTFlag={usePublicTransportLanes}, role={role}, carFlags={car.m_Flags}{suffix}");
        }

        private static string BuildLiveStateSuffix(Entity vehicle)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                return string.Empty;
            }

            EntityManager entityManager = world.EntityManager;
            if (!entityManager.Exists(vehicle))
            {
                return string.Empty;
            }

            string currentLaneState = string.Empty;
            if (entityManager.HasComponent<CarCurrentLane>(vehicle))
            {
                CarCurrentLane currentLane = entityManager.GetComponentData<CarCurrentLane>(vehicle);
                currentLaneState =
                    $"liveCurrentLane={currentLane.m_Lane}, " +
                    $"liveChangeLane={currentLane.m_ChangeLane}, " +
                    $"liveCurve={FormatFloat3(currentLane.m_CurvePosition)}, " +
                    $"liveChangeProgress={currentLane.m_ChangeProgress:0.###}, " +
                    $"liveLanePosition={currentLane.m_LanePosition:0.###}, " +
                    $"liveLaneDistance={currentLane.m_Distance:0.###}, " +
                    $"liveLaneDuration={currentLane.m_Duration:0.###}, " +
                    $"liveLaneFlags={(currentLane.m_LaneFlags == 0 ? "none" : currentLane.m_LaneFlags.ToString())}";
            }

            string pathOwnerState = string.Empty;
            if (entityManager.HasComponent<PathOwner>(vehicle))
            {
                PathOwner pathOwner = entityManager.GetComponentData<PathOwner>(vehicle);
                int pathElementCount =
                    entityManager.HasBuffer<PathElement>(vehicle)
                        ? entityManager.GetBuffer<PathElement>(vehicle).Length
                        : 0;
                int remainingElements =
                    pathElementCount > 0
                        ? math.max(0, pathElementCount - pathOwner.m_ElementIndex)
                        : 0;
                pathOwnerState =
                    $"livePathElementIndex={pathOwner.m_ElementIndex}, " +
                    $"livePathElementCount={pathElementCount}, " +
                    $"liveRemainingElements={remainingElements}";
            }

            string transformState = string.Empty;
            if (entityManager.HasComponent<Transform>(vehicle))
            {
                Transform transform = entityManager.GetComponentData<Transform>(vehicle);
                transformState = $"liveWorldPos={FormatFloat3(transform.m_Position)}";
            }

            return JoinNonEmpty(currentLaneState, pathOwnerState, transformState);
        }

        private static string JoinNonEmpty(params string[] parts)
        {
            string result = string.Empty;
            for (int index = 0; index < parts.Length; index += 1)
            {
                if (string.IsNullOrWhiteSpace(parts[index]))
                {
                    continue;
                }

                result = string.IsNullOrEmpty(result)
                    ? parts[index]
                    : result + ", " + parts[index];
            }

            return result;
        }

        private static string FormatFloat3(float3 value)
        {
            return $"({value.x:0.###},{value.y:0.###},{value.z:0.###})";
        }
    }
}
