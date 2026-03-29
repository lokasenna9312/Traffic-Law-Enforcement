using System;
using System.Linq;
using System.Reflection;
using Game;
using Game.Pathfind;
using Game.Vehicles;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Traffic_Law_Enforcement
{
    internal static class VehicleUtilsPatches
    {
        private const string HarmonyId = "Traffic_Law_Enforcement.VehicleUtilsPatches";

        private struct PathfindRuleSyncResult
        {
            public bool HasProfile;
            public bool ExperimentSkipped;
            public bool AllowOnPublicTransportLane;
            public bool PendingExitActive;
            public VehicleTrafficLawProfile Profile;
        }

        private static readonly Type s_PathfindExecutorType =
            AccessTools.Inner(typeof(PathfindJobs), "PathfindExecutor");

        private static readonly MethodInfo[] s_SetupPathfindMethods = AccessTools
            .GetDeclaredMethods(typeof(VehicleUtils))
            .Where(IsSetupPathfindCandidate)
            .ToArray();

        private static readonly MethodInfo s_CalculateCostMethod = AccessTools.FirstMethod(
            s_PathfindExecutorType,
            method => method.Name == "CalculateCost" && method.ReturnType == typeof(float) && method.GetParameters().Length == 4);

        private static Harmony s_Harmony;
        private static int s_CachedPublicTransportLaneFine;
        private static bool s_HasCachedPenaltyValues;
        private static bool s_CachedPublicTransportLaneEnforcementEnabled;
        private static int s_CachedConfiguredPublicTransportLaneFine;
        private static bool s_LoggedFirstSetupPathfindInvocation;

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            try
            {
                s_Harmony = new Harmony(HarmonyId);

                InvalidateCachedPenaltyValues();

                HarmonyMethod prefix = new HarmonyMethod(typeof(VehicleUtilsPatches), nameof(SetupPathfindPrefix));
                if (s_SetupPathfindMethods.Length == 0)
                {
                    Mod.log.Warn("VehicleUtils.SetupPathfind patch skipped: no matching overloads found.");
                }
                else
                {
                    foreach (MethodInfo setupPathfindMethod in s_SetupPathfindMethods)
                    {
                        s_Harmony.Patch(setupPathfindMethod, prefix: prefix);
                    }

                    Mod.log.Info(
                        $"VehicleUtils.SetupPathfind patch applied to {s_SetupPathfindMethods.Length} overload(s): " +
                        string.Join("; ", s_SetupPathfindMethods.Select(FormatMethodSignature)));
                }

                HarmonyMethod calculateCostPostfix = new HarmonyMethod(typeof(VehicleUtilsPatches), nameof(CalculateCostPostfix));
                s_Harmony.Patch(s_CalculateCostMethod, postfix: calculateCostPostfix);
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                Mod.log.Error(ex, "Failed to apply VehicleUtils pathfinding patches.");
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
            s_CachedPublicTransportLaneFine = 0;
            s_HasCachedPenaltyValues = false;
            s_CachedPublicTransportLaneEnforcementEnabled = false;
            s_CachedConfiguredPublicTransportLaneFine = 0;
            s_LoggedFirstSetupPathfindInvocation = false;
        }

        internal static void InvalidateCachedPenaltyValues()
        {
            s_HasCachedPenaltyValues = false;
        }

        private static void SetupPathfindPrefix(ref SetupQueueItem item)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                return;
            }

            EntityManager entityManager = world.EntityManager;
            Entity owner = item.m_Owner;
            if (owner == Entity.Null || !entityManager.HasComponent<Car>(owner))
            {
                return;
            }

            if (!s_LoggedFirstSetupPathfindInvocation)
            {
                s_LoggedFirstSetupPathfindInvocation = true;
                Mod.log.Info(
                    $"VehicleUtils.SetupPathfind prefix invoked: vehicle={owner}, " +
                    $"watched={FocusedLoggingService.IsWatched(owner)}, " +
                    $"focusedDiagnostics={EnforcementLoggingPolicy.EnableFocusedRouteRebuildDiagnosticsLogging}");
            }

            if (!s_HasCachedPenaltyValues)
            {
                RefreshCachedPenaltyValues();
            }

            RuleFlags ignoredRulesBefore = item.m_Parameters.m_IgnoredRules;
            RuleFlags taxiIgnoredRulesBefore = item.m_Parameters.m_TaxiIgnoredRules;

            PathfindRuleSyncResult syncResult =
                SyncPrivateTrafficIgnoredRules(entityManager, owner, ref item);

            if (EnforcementLoggingPolicy.ShouldLogFocusedPathfindSetup(owner))
            {
                LogFocusedPathfindSetup(
                    owner,
                    ignoredRulesBefore,
                    taxiIgnoredRulesBefore,
                    item,
                    syncResult);
            }
        }

        private static void CalculateCostPostfix(ref float __result, RuleFlags rules, float2 delta, PathfindParameters ___m_Parameters)
        {
            if (!s_CachedPublicTransportLaneEnforcementEnabled)
            {
                return;
            }

            if ((rules & RuleFlags.ForbidPrivateTraffic) == 0)
            {
                return;
            }

            int publicTransportPenalty = s_CachedPublicTransportLaneFine;
            if (publicTransportPenalty <= 0)
            {
                return;
            }

            float moneyWeight = ___m_Parameters.m_Weights.money;
            if (moneyWeight <= 0f)
            {
                return;
            }

            __result += publicTransportPenalty * moneyWeight * math.abs(delta.y - delta.x);
        }

        private static void RefreshCachedPenaltyValues()
        {
            bool enforcementEnabled = Mod.IsPublicTransportLaneEnforcementEnabled;
            int configuredFineAmount = enforcementEnabled
                ? EnforcementGameplaySettingsService.Current.PublicTransportLaneFineAmount
                : 0;

            if (s_HasCachedPenaltyValues && s_CachedPublicTransportLaneEnforcementEnabled == enforcementEnabled && s_CachedConfiguredPublicTransportLaneFine == configuredFineAmount)
            {
                return;
            }

            s_HasCachedPenaltyValues = true;
            s_CachedPublicTransportLaneEnforcementEnabled = enforcementEnabled;
            s_CachedConfiguredPublicTransportLaneFine = configuredFineAmount;
            s_CachedPublicTransportLaneFine = configuredFineAmount;
        }

        private static PathfindRuleSyncResult SyncPrivateTrafficIgnoredRules(EntityManager entityManager, Entity owner, ref SetupQueueItem item)
        {
            if (!entityManager.HasComponent<VehicleTrafficLawProfile>(owner))
            {
                return default;
            }

            VehicleTrafficLawProfile profile = entityManager.GetComponentData<VehicleTrafficLawProfile>(owner);
            PathfindRuleSyncResult result = new PathfindRuleSyncResult
            {
                HasProfile = true,
                Profile = profile,
            };

            bool allowOnPublicTransportLane = PublicTransportLanePolicy.CanUsePublicTransportLane(profile);

            if (!allowOnPublicTransportLane && entityManager.HasComponent<PublicTransportLanePendingExit>(owner))
            {
                PublicTransportLanePendingExit pendingExit = entityManager.GetComponentData<PublicTransportLanePendingExit>(owner);
                allowOnPublicTransportLane = pendingExit.m_HasLeftPublicTransportLane == 0;
                result.PendingExitActive = pendingExit.m_HasLeftPublicTransportLane == 0;
            }

            result.AllowOnPublicTransportLane = allowOnPublicTransportLane;

            if (ShouldUseVanillaPathfindRulesForTrackedVehicle(profile))
            {
                result.ExperimentSkipped = true;
                return result;
            }

            SetRuleFlag(ref item.m_Parameters.m_IgnoredRules, RuleFlags.ForbidPrivateTraffic, allowOnPublicTransportLane);

            SetRuleFlag(ref item.m_Parameters.m_TaxiIgnoredRules, RuleFlags.ForbidPrivateTraffic, allowOnPublicTransportLane);

            return result;
        }

        private static bool ShouldUseVanillaPathfindRulesForTrackedVehicle(VehicleTrafficLawProfile profile)
        {
            return Mod.Settings?.EnablePolicyTrackedVehicleVanillaPathfindRulesExperiment == true &&
                   profile.m_ShouldTrack != 0;
        }

        private static void SetRuleFlag(ref RuleFlags rules, RuleFlags flag, bool enabled)
        {
            if (enabled)
            {
                rules |= flag;
            }
            else
            {
                rules &= ~flag;
            }
        }

        private static void LogFocusedPathfindSetup(
            Entity owner,
            RuleFlags ignoredRulesBefore,
            RuleFlags taxiIgnoredRulesBefore,
            SetupQueueItem item,
            PathfindRuleSyncResult syncResult)
        {
            bool experimentEnabled =
                Mod.Settings?.EnablePolicyTrackedVehicleVanillaPathfindRulesExperiment == true;

            string accessBits = syncResult.HasProfile
                ? FormatAccessBits(syncResult.Profile.m_PublicTransportLaneAccessBits)
                : "none";

            string allowOnPublicTransportLane = syncResult.HasProfile
                ? syncResult.AllowOnPublicTransportLane.ToString()
                : "unavailable";

            Mod.log.Info(
                $"FOCUSED_SETUP_PATHFIND: vehicle={owner}, " +
                $"experimentEnabled={experimentEnabled}, " +
                $"experimentSkip={syncResult.ExperimentSkipped}, " +
                $"hasProfile={syncResult.HasProfile}, " +
                $"shouldTrack={(syncResult.HasProfile ? syncResult.Profile.m_ShouldTrack.ToString() : "n/a")}, " +
                $"emergency={(syncResult.HasProfile ? (syncResult.Profile.m_EmergencyVehicle != 0).ToString() : "n/a")}, " +
                $"accessBits={accessBits}, " +
                $"allowOnPTLane={allowOnPublicTransportLane}, " +
                $"pendingExitActive={(syncResult.HasProfile ? syncResult.PendingExitActive.ToString() : "n/a")}, " +
                $"ignoredRulesBefore={FormatRuleFlags(ignoredRulesBefore)}, " +
                $"ignoredRulesAfter={FormatRuleFlags(item.m_Parameters.m_IgnoredRules)}, " +
                $"taxiIgnoredRulesBefore={FormatRuleFlags(taxiIgnoredRulesBefore)}, " +
                $"taxiIgnoredRulesAfter={FormatRuleFlags(item.m_Parameters.m_TaxiIgnoredRules)}");
        }

        private static string FormatRuleFlags(RuleFlags flags)
        {
            return flags == 0 ? "none" : flags.ToString();
        }

        private static string FormatAccessBits(PublicTransportLaneAccessBits bits)
        {
            return bits == 0 ? "none" : bits.ToString();
        }

        private static bool IsSetupPathfindCandidate(MethodInfo method)
        {
            if (method == null || method.Name != nameof(VehicleUtils.SetupPathfind) || method.ReturnType != typeof(void))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 4 &&
                   parameters[1].ParameterType == typeof(PathOwner).MakeByRefType() &&
                   parameters[2].ParameterType == typeof(NativeQueue<SetupQueueItem>.ParallelWriter) &&
                   parameters[3].ParameterType == typeof(SetupQueueItem);
        }

        private static string FormatMethodSignature(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            return $"{method.DeclaringType?.Name}.{method.Name}(" +
                   string.Join(", ", parameters.Select(parameter => parameter.ParameterType.Name)) +
                   ")";
        }
    }
}
