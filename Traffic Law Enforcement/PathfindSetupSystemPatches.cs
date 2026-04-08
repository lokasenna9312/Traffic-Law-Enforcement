using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Simulation;
using Game.Vehicles;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Traffic_Law_Enforcement
{
    internal static class PathfindSetupSystemPatches
    {
        private const string HarmonyId = "Traffic_Law_Enforcement.PathfindSetupSystemPatches";

        private static readonly MethodInfo s_CompleteSetupMethod =
            AccessTools.Method(typeof(PathfindSetupSystem), nameof(PathfindSetupSystem.CompleteSetup));

        private static readonly FieldInfo s_SetupListField =
            AccessTools.Field(typeof(PathfindSetupSystem), "m_SetupList");

        private static readonly Dictionary<int, PathfindSetupSystem.SetupListItem> s_StartItems =
            new Dictionary<int, PathfindSetupSystem.SetupListItem>();
        private static readonly Dictionary<int, PathfindSetupSystem.SetupListItem> s_EndItems =
            new Dictionary<int, PathfindSetupSystem.SetupListItem>();

        private static Harmony s_Harmony;
        private static bool s_LoggedFirstCompleteSetupInvocation;

        internal static bool IsApplied => s_Harmony != null;

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            try
            {
                if (s_CompleteSetupMethod == null || s_SetupListField == null)
                {
                    Mod.log.Warn("PathfindSetupSystem patch skipped: CompleteSetup or m_SetupList not found.");
                    return;
                }

                s_Harmony = new Harmony(HarmonyId);
                HarmonyMethod prefix = new HarmonyMethod(typeof(PathfindSetupSystemPatches), nameof(CompleteSetupPrefix));
                s_Harmony.Patch(s_CompleteSetupMethod, prefix: prefix);
                Mod.log.Info("PathfindSetupSystem.CompleteSetup patch applied.");
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                Mod.log.Error(ex, "Failed to apply PathfindSetupSystem patches.");
            }
        }

        public static void Remove()
        {
            if (s_Harmony == null)
            {
                return;
            }

            s_Harmony.UnpatchAll(HarmonyId);
            s_Harmony = null;
            s_LoggedFirstCompleteSetupInvocation = false;
        }

        private static void CompleteSetupPrefix(PathfindSetupSystem __instance)
        {
            if (!s_LoggedFirstCompleteSetupInvocation)
            {
                s_LoggedFirstCompleteSetupInvocation = true;
                Mod.log.Info(
                    $"PathfindSetupSystem.CompleteSetup prefix invoked: " +
                    $"focusedDiagnostics={EnforcementLoggingPolicy.EnableFocusedRouteRebuildDiagnosticsLogging}, " +
                    $"watchedCount={FocusedLoggingService.WatchedVehicleCount}");
            }

            if (!EnforcementLoggingPolicy.ShouldLogFocusedRouteRebuildDiagnostics() ||
                !FocusedLoggingService.HasWatchedVehicles)
            {
                return;
            }

            object boxedSetupList = s_SetupListField.GetValue(__instance);
            if (boxedSetupList is not NativeList<PathfindSetupSystem.SetupListItem> setupList ||
                setupList.Length == 0)
            {
                return;
            }

            s_StartItems.Clear();
            s_EndItems.Clear();

            for (int index = 0; index < setupList.Length; index++)
            {
                PathfindSetupSystem.SetupListItem item = setupList[index];
                if (!FocusedLoggingService.IsWatched(item.m_Owner))
                {
                    continue;
                }

                if (item.m_ActionStart)
                {
                    s_StartItems[item.m_ActionIndex] = item;
                }
                else
                {
                    s_EndItems[item.m_ActionIndex] = item;
                }
            }

            if (s_StartItems.Count == 0 && s_EndItems.Count == 0)
            {
                return;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            EntityManager entityManager = world?.EntityManager ?? default;

            foreach (KeyValuePair<int, PathfindSetupSystem.SetupListItem> pair in s_StartItems)
            {
                PathfindSetupSystem.SetupListItem startItem = pair.Value;
                s_EndItems.TryGetValue(pair.Key, out PathfindSetupSystem.SetupListItem endItem);

                bool hasProfile =
                    world != null &&
                    startItem.m_Owner != Entity.Null &&
                    entityManager.HasComponent<VehicleTrafficLawProfile>(startItem.m_Owner);

                VehicleTrafficLawProfile profile = hasProfile
                    ? entityManager.GetComponentData<VehicleTrafficLawProfile>(startItem.m_Owner)
                    : default;

                bool allowOnPublicTransportLane = hasProfile &&
                    PublicTransportLanePolicy.CanUsePublicTransportLane(profile);

                bool pendingExitActive = false;
                if (hasProfile &&
                    !allowOnPublicTransportLane &&
                    entityManager.HasComponent<PublicTransportLanePendingExit>(startItem.m_Owner))
                {
                    PublicTransportLanePendingExit pendingExit =
                    entityManager.GetComponentData<PublicTransportLanePendingExit>(startItem.m_Owner);
                    pendingExitActive = pendingExit.m_HasLeftPublicTransportLane == 0;
                    allowOnPublicTransportLane = pendingExitActive;
                }

                string obsoleteAttemptId =
                    ObsoleteAttemptCorrelationService.GetAttemptId(startItem.m_Owner);
                string elapsedSinceObsolete =
                    ObsoleteAttemptCorrelationService.GetElapsedSinceObsolete(startItem.m_Owner);
                string targetKindNormalized =
                    world != null
                        ? RouteDebugNormalization.NormalizeTargetKind(
                            entityManager,
                            endItem.m_Owner != Entity.Null ? endItem.m_Target : startItem.m_Target,
                            startItem.m_Parameters.m_ParkingTarget)
                        : RouteDebugNormalization.UnknownTargetKind;
                targetKindNormalized =
                    ObsoleteAttemptCorrelationService.ResolveTargetKindNormalized(
                        startItem.m_Owner,
                        targetKindNormalized);

                string liveState = BuildLiveStateSuffix(startItem.m_Owner, entityManager);
                string message =
                    $"FOCUSED_SETUP_PATHFIND: source=PathfindSetupSystem.CompleteSetup, " +
                    $"vehicle={startItem.m_Owner}, " +
                    $"vehicleEntity={startItem.m_Owner}, " +
                    $"actionIndex={pair.Key}, " +
                    $"obsoleteAttemptId={obsoleteAttemptId}, " +
                    $"elapsedSinceObsolete={elapsedSinceObsolete}, " +
                    $"targetKindNormalized={targetKindNormalized}, " +
                    $"hasProfile={hasProfile}, " +
                    $"shouldTrack={(hasProfile ? profile.m_ShouldTrack.ToString() : "n/a")}, " +
                    $"emergency={(hasProfile ? (profile.m_EmergencyVehicle != 0).ToString() : "n/a")}, " +
                    $"accessBits={(hasProfile ? FormatAccessBits(profile.m_PublicTransportLaneAccessBits) : "none")}, " +
                    $"allowOnPTLane={(hasProfile ? allowOnPublicTransportLane.ToString() : "unavailable")}, " +
                    $"pendingExitActive={(hasProfile ? pendingExitActive.ToString() : "n/a")}, " +
                    $"ignoredRules={FormatRuleFlags(startItem.m_Parameters.m_IgnoredRules)}, " +
                    $"taxiIgnoredRules={FormatRuleFlags(startItem.m_Parameters.m_TaxiIgnoredRules)}, " +
                    $"pathfindFlags={FormatPathfindFlags(startItem.m_Parameters.m_PathfindFlags)}, " +
                    $"methods={FormatPathMethods(startItem.m_Parameters.m_Methods)}, " +
                    $"weights={FormatWeights(startItem.m_Parameters.m_Weights)}, " +
                    $"parkingTarget={startItem.m_Parameters.m_ParkingTarget}, " +
                    $"origin={FormatTarget(startItem.m_Target)}, " +
                    $"destination={(endItem.m_Owner != Entity.Null ? FormatTarget(endItem.m_Target) : "missing")}";

                if (!string.IsNullOrWhiteSpace(liveState))
                {
                    message += $", {liveState}";
                }

                Mod.log.Info(message);

            }
        }

        private static string FormatTarget(SetupQueueTarget target)
        {
            return
                $"type={target.m_Type}, " +
                $"entity={target.m_Entity}, " +
                $"entity2={target.m_Entity2}, " +
                $"methods={FormatPathMethods(target.m_Methods)}, " +
                $"roadTypes={target.m_RoadTypes}, " +
                $"flags={target.m_Flags}, " +
                $"trackTypes={target.m_TrackTypes}, " +
                $"randomCost={target.m_RandomCost:0.###}";
        }

        private static string FormatWeights(PathfindWeights weights)
        {
            return $"time={weights.m_Value.x},behaviour={weights.m_Value.y},money={weights.m_Value.z},comfort={weights.m_Value.w}";
        }

        private static string BuildLiveStateSuffix(Entity vehicle, EntityManager entityManager)
        {
            if (vehicle == Entity.Null || !entityManager.Exists(vehicle))
            {
                return string.Empty;
            }

            string currentLaneState = string.Empty;
            if (entityManager.HasComponent<CarCurrentLane>(vehicle))
            {
                CarCurrentLane currentLane = entityManager.GetComponentData<CarCurrentLane>(vehicle);
                Entity normalizedCurrentLane = NormalizeLane(entityManager, currentLane.m_Lane);
                Entity normalizedChangeLane = NormalizeLane(entityManager, currentLane.m_ChangeLane);
                currentLaneState =
                    $"liveCurrentLane={FormatEntityOrNone(currentLane.m_Lane)}, " +
                    $"liveNormalizedCurrentLane={FormatEntityOrNone(normalizedCurrentLane)}, " +
                    $"liveChangeLane={FormatEntityOrNone(currentLane.m_ChangeLane)}, " +
                    $"liveNormalizedChangeLane={FormatEntityOrNone(normalizedChangeLane)}, " +
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

        private static string FormatEntityOrNone(Entity entity)
        {
            return entity == Entity.Null
                ? "none"
                : entity.ToString();
        }

        private static string FormatFloat3(float3 value)
        {
            return $"({value.x:0.###},{value.y:0.###},{value.z:0.###})";
        }

        private static string FormatRuleFlags(RuleFlags flags)
        {
            return flags == 0 ? "none" : flags.ToString();
        }

        private static string FormatAccessBits(PublicTransportLaneAccessBits bits)
        {
            return bits == 0 ? "none" : bits.ToString();
        }

        private static string FormatPathfindFlags(PathfindFlags flags)
        {
            return flags == 0 ? "none" : flags.ToString();
        }

        private static string FormatPathMethods(PathMethod methods)
        {
            return methods == 0 ? "none" : methods.ToString();
        }
    }
}

