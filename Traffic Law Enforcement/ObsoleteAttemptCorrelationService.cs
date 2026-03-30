using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Routes;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    internal static class ObsoleteAttemptCorrelationService
    {
        private readonly struct ObsoleteAttemptInfo
        {
            internal readonly string AttemptId;
            internal readonly Entity Target;
            internal readonly string TargetKindNormalized;
            internal readonly bool HasGameTime;
            internal readonly long ObsoleteMonthTicks;
            internal readonly long ObsoleteWallClockMs;

            internal ObsoleteAttemptInfo(
                string attemptId,
                Entity target,
                string targetKindNormalized,
                bool hasGameTime,
                long obsoleteMonthTicks,
                long obsoleteWallClockMs)
            {
                AttemptId = attemptId;
                Target = target;
                TargetKindNormalized = targetKindNormalized;
                HasGameTime = hasGameTime;
                ObsoleteMonthTicks = obsoleteMonthTicks;
                ObsoleteWallClockMs = obsoleteWallClockMs;
            }
        }

        private static readonly Dictionary<Entity, ObsoleteAttemptInfo> s_AttemptsByVehicle =
            new Dictionary<Entity, ObsoleteAttemptInfo>();

        private static long s_AttemptSequence;
        private static int s_RuntimeWorldGeneration = int.MinValue;

        internal static void Reset()
        {
            s_AttemptsByVehicle.Clear();
            s_AttemptSequence = 0L;
            s_RuntimeWorldGeneration = EnforcementSaveDataSystem.RuntimeWorldGeneration;
        }

        internal static void ResetForRuntimeWorldGeneration(int runtimeWorldGeneration)
        {
            s_AttemptsByVehicle.Clear();
            s_RuntimeWorldGeneration = runtimeWorldGeneration;
        }

        internal static string RegisterAttempt(
            Entity vehicle,
            Entity target,
            string targetKindNormalized)
        {
            EnsureCurrentRuntimeWorldGeneration();

            if (vehicle == Entity.Null)
            {
                return "n/a";
            }

            s_AttemptSequence += 1L;
            string attemptId = $"{FocusedLoggingService.FormatEntity(vehicle)}-o{s_AttemptSequence}";
            string safeTargetKind =
                string.IsNullOrWhiteSpace(targetKindNormalized)
                    ? RouteDebugNormalization.UnknownTargetKind
                    : targetKindNormalized;

            s_AttemptsByVehicle[vehicle] = new ObsoleteAttemptInfo(
                attemptId,
                target,
                safeTargetKind,
                EnforcementGameTime.IsInitialized,
                EnforcementGameTime.CurrentTimestampMonthTicks,
                Stopwatch.GetTimestamp());

            return attemptId;
        }

        internal static string GetAttemptId(Entity vehicle)
        {
            return TryGetAttemptInfo(vehicle, out ObsoleteAttemptInfo info)
                ? info.AttemptId
                : "n/a";
        }

        internal static string GetElapsedSinceObsolete(Entity vehicle)
        {
            if (!TryGetAttemptInfo(vehicle, out ObsoleteAttemptInfo info))
            {
                return "n/a";
            }

            if (info.HasGameTime && EnforcementGameTime.IsInitialized)
            {
                long deltaMonthTicks =
                    EnforcementGameTime.CurrentTimestampMonthTicks - info.ObsoleteMonthTicks;
                if (deltaMonthTicks < 0L)
                {
                    return "n/a";
                }

                return $"{deltaMonthTicks}mt";
            }

            long deltaStopwatchTicks = Stopwatch.GetTimestamp() - info.ObsoleteWallClockMs;
            if (deltaStopwatchTicks < 0L)
            {
                return "n/a";
            }

            long elapsedMs =
                (long)Math.Round((double)deltaStopwatchTicks * 1000d / Stopwatch.Frequency);
            return $"{elapsedMs}ms";
        }

        internal static string ResolveTargetKindNormalized(
            Entity vehicle,
            string currentTargetKindNormalized)
        {
            if (!string.IsNullOrWhiteSpace(currentTargetKindNormalized) &&
                currentTargetKindNormalized != RouteDebugNormalization.UnknownTargetKind)
            {
                return currentTargetKindNormalized;
            }

            return TryGetAttemptInfo(vehicle, out ObsoleteAttemptInfo info) &&
                !string.IsNullOrWhiteSpace(info.TargetKindNormalized)
                    ? info.TargetKindNormalized
                    : RouteDebugNormalization.UnknownTargetKind;
        }

        internal static Entity GetLastKnownTarget(Entity vehicle)
        {
            return TryGetAttemptInfo(vehicle, out ObsoleteAttemptInfo info)
                ? info.Target
                : Entity.Null;
        }

        private static bool TryGetAttemptInfo(Entity vehicle, out ObsoleteAttemptInfo info)
        {
            EnsureCurrentRuntimeWorldGeneration();

            if (vehicle != Entity.Null && s_AttemptsByVehicle.TryGetValue(vehicle, out info))
            {
                return true;
            }

            info = default;
            return false;
        }

        private static void EnsureCurrentRuntimeWorldGeneration()
        {
            int currentGeneration = EnforcementSaveDataSystem.RuntimeWorldGeneration;
            if (s_RuntimeWorldGeneration == currentGeneration)
            {
                return;
            }

            ResetForRuntimeWorldGeneration(currentGeneration);
        }
    }

    internal static class RouteDebugNormalization
    {
        internal const string UnknownTargetKind = "unknown";

        internal static string NormalizeTargetKind(
            EntityManager entityManager,
            SetupQueueTarget target,
            Entity parkingTarget)
        {
            if (parkingTarget != Entity.Null)
            {
                return "parking";
            }

            if (target.m_Type == SetupTargetType.OutsideConnection)
            {
                return "connection";
            }

            Entity targetEntity =
                target.m_Entity != Entity.Null
                    ? target.m_Entity
                    : target.m_Entity2;
            return NormalizeTargetKind(entityManager, targetEntity);
        }

        internal static string NormalizeTargetKind(
            EntityManager entityManager,
            Entity targetEntity,
            Entity parkingTarget = default)
        {
            if (parkingTarget != Entity.Null)
            {
                return "parking";
            }

            if (targetEntity == Entity.Null || !entityManager.Exists(targetEntity))
            {
                return UnknownTargetKind;
            }

            if (entityManager.HasComponent<RouteLane>(targetEntity))
            {
                RouteLane routeLane = entityManager.GetComponentData<RouteLane>(targetEntity);
                Entity lane = routeLane.m_EndLane != Entity.Null
                    ? routeLane.m_EndLane
                    : routeLane.m_StartLane;
                string routeLaneKind = NormalizeLaneTargetKind(entityManager, lane);
                if (routeLaneKind != UnknownTargetKind)
                {
                    return routeLaneKind;
                }
            }

            return NormalizeLaneTargetKind(entityManager, targetEntity);
        }

        internal static string BuildAcceptedHeadFamilyKey(
            EntityManager entityManager,
            DynamicBuffer<PathElement> pathElements,
            int startIndex = 0,
            int maxElements = 3)
        {
            if (pathElements.Length == 0 || maxElements <= 0)
            {
                return "none";
            }

            List<string> tokens = new List<string>(maxElements);
            for (int index = startIndex; index < pathElements.Length && tokens.Count < maxElements; index += 1)
            {
                string token = BuildLaneFamilyToken(entityManager, pathElements[index].m_Target);
                if (string.IsNullOrWhiteSpace(token) || token == "none")
                {
                    continue;
                }

                tokens.Add(token);
            }

            return tokens.Count == 0
                ? "none"
                : string.Join(">", tokens.ToArray());
        }

        private static string NormalizeLaneTargetKind(EntityManager entityManager, Entity lane)
        {
            Entity normalizedLane = NormalizeLane(entityManager, lane);
            if (normalizedLane == Entity.Null || !entityManager.Exists(normalizedLane))
            {
                return UnknownTargetKind;
            }

            if (entityManager.HasComponent<ParkingLane>(normalizedLane) ||
                entityManager.HasComponent<GarageLane>(normalizedLane))
            {
                return "parking";
            }

            if (entityManager.HasComponent<ConnectionLane>(normalizedLane))
            {
                ConnectionLane connectionLane =
                    entityManager.GetComponentData<ConnectionLane>(normalizedLane);
                bool highwayLike =
                    (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0 &&
                    (connectionLane.m_Flags & ConnectionLaneFlags.Parking) == 0;
                return highwayLike ? "highway_like" : "connection";
            }

            if (entityManager.HasComponent<CarLane>(normalizedLane))
            {
                CarLane carLane = entityManager.GetComponentData<CarLane>(normalizedLane);
                if ((carLane.m_Flags & CarLaneFlags.Highway) != 0)
                {
                    return "highway_like";
                }

                if ((carLane.m_Flags & CarLaneFlags.PublicOnly) != 0)
                {
                    return "local_platform";
                }

                return "local_stop";
            }

            return entityManager.HasComponent<EdgeLane>(normalizedLane)
                ? "local_stop"
                : UnknownTargetKind;
        }

        private static string BuildLaneFamilyToken(EntityManager entityManager, Entity lane)
        {
            Entity normalizedLane = NormalizeLane(entityManager, lane);
            if (normalizedLane == Entity.Null || !entityManager.Exists(normalizedLane))
            {
                return "none";
            }

            string kind = DescribeFamilyKind(entityManager, normalizedLane);
            if (TryResolveAggregateRoadEntity(entityManager, normalizedLane, out Entity aggregateRoad))
            {
                return $"{kind}:agg#{FormatEntityKey(aggregateRoad)}";
            }

            if (TryResolveOwnerRoadEntity(entityManager, normalizedLane, out Entity ownerRoad))
            {
                return $"{kind}:owner#{FormatEntityKey(ownerRoad)}";
            }

            return $"{kind}:lane#{FormatEntityKey(normalizedLane)}";
        }

        private static string DescribeFamilyKind(EntityManager entityManager, Entity normalizedLane)
        {
            if (entityManager.HasComponent<ParkingLane>(normalizedLane))
            {
                return "parking";
            }

            if (entityManager.HasComponent<GarageLane>(normalizedLane))
            {
                return "garage";
            }

            if (entityManager.HasComponent<ConnectionLane>(normalizedLane))
            {
                ConnectionLane connectionLane =
                    entityManager.GetComponentData<ConnectionLane>(normalizedLane);
                bool isRoadConnection =
                    (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0 &&
                    (connectionLane.m_Flags & ConnectionLaneFlags.Parking) == 0;
                return isRoadConnection ? "connection" : "access";
            }

            return entityManager.HasComponent<EdgeLane>(normalizedLane)
                ? "road"
                : "lane";
        }

        private static bool TryResolveAggregateRoadEntity(
            EntityManager entityManager,
            Entity lane,
            out Entity aggregateRoad)
        {
            aggregateRoad = Entity.Null;

            if (!TryResolveOwnerRoadEntity(entityManager, lane, out Entity ownerRoad) ||
                ownerRoad == Entity.Null ||
                !entityManager.Exists(ownerRoad) ||
                !entityManager.HasComponent<Aggregated>(ownerRoad))
            {
                return false;
            }

            Aggregated aggregated = entityManager.GetComponentData<Aggregated>(ownerRoad);
            if (aggregated.m_Aggregate == Entity.Null)
            {
                return false;
            }

            aggregateRoad = aggregated.m_Aggregate;
            return true;
        }

        private static bool TryResolveOwnerRoadEntity(
            EntityManager entityManager,
            Entity lane,
            out Entity ownerRoad)
        {
            ownerRoad = Entity.Null;

            if (lane == Entity.Null ||
                !entityManager.Exists(lane) ||
                !entityManager.HasComponent<Owner>(lane))
            {
                return false;
            }

            Owner owner = entityManager.GetComponentData<Owner>(lane);
            if (owner.m_Owner == Entity.Null)
            {
                return false;
            }

            ownerRoad = owner.m_Owner;
            return true;
        }

        private static Entity NormalizeLane(EntityManager entityManager, Entity lane)
        {
            if (lane == Entity.Null || !entityManager.Exists(lane))
            {
                return Entity.Null;
            }

            Entity normalizedLane = lane;
            if (entityManager.HasComponent<SlaveLane>(lane) &&
                entityManager.HasComponent<Owner>(lane))
            {
                SlaveLane slaveLane = entityManager.GetComponentData<SlaveLane>(lane);
                Owner owner = entityManager.GetComponentData<Owner>(lane);
                if (owner.m_Owner != Entity.Null &&
                    entityManager.Exists(owner.m_Owner) &&
                    entityManager.HasBuffer<SubLane>(owner.m_Owner))
                {
                    DynamicBuffer<SubLane> subLanes = entityManager.GetBuffer<SubLane>(owner.m_Owner);
                    if (slaveLane.m_MasterIndex >= 0 &&
                        slaveLane.m_MasterIndex < subLanes.Length)
                    {
                        Entity masterLane = subLanes[slaveLane.m_MasterIndex].m_SubLane;
                        if (masterLane != Entity.Null)
                        {
                            normalizedLane = masterLane;
                        }
                    }
                }
            }

            return normalizedLane;
        }

        private static string FormatEntityKey(Entity entity)
        {
            return entity == Entity.Null
                ? "none"
                : $"{entity.Index}v{entity.Version}";
        }
    }
}
