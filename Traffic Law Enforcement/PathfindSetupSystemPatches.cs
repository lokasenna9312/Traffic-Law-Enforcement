using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Pathfind;
using Game.Simulation;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    internal static class PathfindSetupSystemPatches
    {
        private const string HarmonyId = "Traffic_Law_Enforcement.PathfindSetupSystemPatches";

        private static readonly MethodInfo s_CompleteSetupMethod =
            AccessTools.Method(typeof(PathfindSetupSystem), nameof(PathfindSetupSystem.CompleteSetup));

        private static readonly FieldInfo s_SetupListField =
            AccessTools.Field(typeof(PathfindSetupSystem), "m_SetupList");

        private static Harmony s_Harmony;
        private static bool s_LoggedFirstCompleteSetupInvocation;

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

            Dictionary<int, PathfindSetupSystem.SetupListItem> startItems = new Dictionary<int, PathfindSetupSystem.SetupListItem>();
            Dictionary<int, PathfindSetupSystem.SetupListItem> endItems = new Dictionary<int, PathfindSetupSystem.SetupListItem>();

            for (int index = 0; index < setupList.Length; index++)
            {
                PathfindSetupSystem.SetupListItem item = setupList[index];
                if (!FocusedLoggingService.IsWatched(item.m_Owner))
                {
                    continue;
                }

                if (item.m_ActionStart)
                {
                    startItems[item.m_ActionIndex] = item;
                }
                else
                {
                    endItems[item.m_ActionIndex] = item;
                }
            }

            if (startItems.Count == 0 && endItems.Count == 0)
            {
                return;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            EntityManager entityManager = world?.EntityManager ?? default;

            foreach (KeyValuePair<int, PathfindSetupSystem.SetupListItem> pair in startItems)
            {
                PathfindSetupSystem.SetupListItem startItem = pair.Value;
                endItems.TryGetValue(pair.Key, out PathfindSetupSystem.SetupListItem endItem);

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

                Mod.log.Info(
                    $"FOCUSED_SETUP_PATHFIND: source=PathfindSetupSystem.CompleteSetup, " +
                    $"vehicle={startItem.m_Owner}, " +
                    $"vehicleEntity={FocusedLoggingService.FormatEntity(startItem.m_Owner)}, " +
                    $"actionIndex={pair.Key}, " +
                    $"experimentEnabled={(Mod.Settings?.EnablePolicyTrackedVehicleVanillaPathfindRulesExperiment == true)}, " +
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
                    $"parkingTarget={FocusedLoggingService.FormatEntity(startItem.m_Parameters.m_ParkingTarget)}, " +
                    $"origin={FormatTarget(startItem.m_Target)}, " +
                    $"destination={(endItem.m_Owner != Entity.Null ? FormatTarget(endItem.m_Target) : "missing")}");
            }
        }

        private static string FormatTarget(SetupQueueTarget target)
        {
            return
                $"type={target.m_Type}, " +
                $"entity={FocusedLoggingService.FormatEntity(target.m_Entity)}, " +
                $"entity2={FocusedLoggingService.FormatEntity(target.m_Entity2)}, " +
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
