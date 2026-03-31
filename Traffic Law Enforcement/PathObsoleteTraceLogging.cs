using System;
using System.Text;
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
        private struct PathObsoleteTargetContext
        {
            public Entity CurrentTarget;
            public string TargetKindNormalized;
        }

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
            if (!ShouldRecordPathObsoleteTrace(force, vehicle))
            {
                return;
            }

            bool emergency = EmergencyVehiclePolicy.IsEmergencyVehicle(car);
            bool usePublicTransportLanes = (car.m_Flags & CarFlags.UsePublicTransportLanes) != 0;
            PathObsoleteTargetContext targetContext =
                ResolvePathObsoleteTargetContext(vehicle);

            string obsoleteAttemptId =
                ObsoleteAttemptCorrelationService.RegisterAttempt(
                    vehicle,
                    targetContext.CurrentTarget,
                    targetContext.TargetKindNormalized);
            string liveState = BuildLiveStateSuffix(vehicle);

            StringBuilder message = new StringBuilder(256);
            message.Append("[OBSOLETE_TRACE] by=");
            message.Append(sourceSystem);
            message.Append(", vehicle=");
            message.Append(vehicle);
            message.Append(", currentLane=");
            message.Append(currentLane);
            message.Append(", pathStateBefore=");
            message.Append(stateBefore);
            message.Append(", pathStateAfter=");
            message.Append(stateAfter);
            message.Append(", reason=");
            message.Append(reason);
            message.Append(", emergency=");
            message.Append(emergency);
            message.Append(", usePTFlag=");
            message.Append(usePublicTransportLanes);
            message.Append(", role=");
            message.Append(role);
            message.Append(", carFlags=");
            message.Append(car.m_Flags);
            message.Append(", obsoleteAttemptId=");
            message.Append(obsoleteAttemptId);
            message.Append(", targetKindNormalized=");
            message.Append(targetContext.TargetKindNormalized);
            AppendOptionalDetail(message, liveState);
            AppendOptionalDetail(message, extra);

            Mod.log.Info(message.ToString());
        }

        private static bool ShouldRecordPathObsoleteTrace(
            bool force,
            Entity vehicle)
        {
            if (!force && !EnforcementLoggingPolicy.ShouldLogPathObsoleteSources())
            {
                return false;
            }

            if (!force &&
                EnforcementLoggingPolicy.ShouldRestrictVehicleSpecificRouteDebugLogsToWatchedVehicles() &&
                !FocusedLoggingService.IsWatched(vehicle))
            {
                return false;
            }

            return true;
        }

        private static PathObsoleteTargetContext ResolvePathObsoleteTargetContext(
            Entity vehicle)
        {
            PathObsoleteTargetContext context = new PathObsoleteTargetContext
            {
                CurrentTarget = Entity.Null,
                TargetKindNormalized = RouteDebugNormalization.UnknownTargetKind,
            };

            if (!TryGetVehicleEntityManager(vehicle, out EntityManager entityManager))
            {
                return context;
            }

            if (entityManager.HasComponent<Target>(vehicle))
            {
                Target target = entityManager.GetComponentData<Target>(vehicle);
                context.CurrentTarget = target.m_Target;
            }

            context.TargetKindNormalized =
                RouteDebugNormalization.NormalizeTargetKind(
                    entityManager,
                    context.CurrentTarget);
            return context;
        }

        private static bool TryGetVehicleEntityManager(
            Entity vehicle,
            out EntityManager entityManager)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                entityManager = default;
                return false;
            }

            entityManager = world.EntityManager;
            return vehicle != Entity.Null && entityManager.Exists(vehicle);
        }

        private static string BuildLiveStateSuffix(Entity vehicle)
        {
            if (!TryGetVehicleEntityManager(vehicle, out EntityManager entityManager))
            {
                return string.Empty;
            }

            StringBuilder liveState = new StringBuilder(192);
            AppendLiveCurrentLaneState(liveState, entityManager, vehicle);
            AppendLivePathOwnerState(liveState, entityManager, vehicle);
            AppendLiveTransformState(liveState, entityManager, vehicle);
            return liveState.ToString();
        }

        private static void AppendLiveCurrentLaneState(
            StringBuilder builder,
            EntityManager entityManager,
            Entity vehicle)
        {
            if (!entityManager.HasComponent<CarCurrentLane>(vehicle))
            {
                return;
            }

            CarCurrentLane currentLane = entityManager.GetComponentData<CarCurrentLane>(vehicle);
            AppendOptionalDetail(
                builder,
                $"liveCurrentLane={currentLane.m_Lane}, " +
                $"liveChangeLane={currentLane.m_ChangeLane}, " +
                $"liveCurve={FormatFloat3(currentLane.m_CurvePosition)}, " +
                $"liveChangeProgress={currentLane.m_ChangeProgress:0.###}, " +
                $"liveLanePosition={currentLane.m_LanePosition:0.###}, " +
                $"liveLaneDistance={currentLane.m_Distance:0.###}, " +
                $"liveLaneDuration={currentLane.m_Duration:0.###}, " +
                $"liveLaneFlags={(currentLane.m_LaneFlags == 0 ? "none" : currentLane.m_LaneFlags.ToString())}");
        }

        private static void AppendLivePathOwnerState(
            StringBuilder builder,
            EntityManager entityManager,
            Entity vehicle)
        {
            if (!entityManager.HasComponent<PathOwner>(vehicle))
            {
                return;
            }

            PathOwner pathOwner = entityManager.GetComponentData<PathOwner>(vehicle);
            int pathElementCount =
                entityManager.HasBuffer<PathElement>(vehicle)
                    ? entityManager.GetBuffer<PathElement>(vehicle).Length
                    : 0;
            int remainingElements =
                pathElementCount > 0
                    ? math.max(0, pathElementCount - pathOwner.m_ElementIndex)
                    : 0;
            AppendOptionalDetail(
                builder,
                $"livePathElementIndex={pathOwner.m_ElementIndex}, " +
                $"livePathElementCount={pathElementCount}, " +
                $"liveRemainingElements={remainingElements}");
        }

        private static void AppendLiveTransformState(
            StringBuilder builder,
            EntityManager entityManager,
            Entity vehicle)
        {
            if (!entityManager.HasComponent<Transform>(vehicle))
            {
                return;
            }

            Transform transform = entityManager.GetComponentData<Transform>(vehicle);
            AppendOptionalDetail(
                builder,
                $"liveWorldPos={FormatFloat3(transform.m_Position)}");
        }

        private static void AppendOptionalDetail(
            StringBuilder builder,
            string detail)
        {
            if (string.IsNullOrWhiteSpace(detail))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(detail);
        }

        private static string FormatFloat3(float3 value)
        {
            return $"({value.x:0.###},{value.y:0.###},{value.z:0.###})";
        }
    }
}
