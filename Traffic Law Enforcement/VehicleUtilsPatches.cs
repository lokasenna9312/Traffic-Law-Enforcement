using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
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
            public bool AllowOnPublicTransportLane;
            public bool PendingExitActive;
            public VehicleTrafficLawProfile Profile;
        }

        private static readonly Type s_PathfindExecutorType =
            AccessTools.Inner(typeof(PathfindJobs), "PathfindExecutor");

        private static readonly MethodInfo[] s_SetupPathfindMethods = GetSetupPathfindMethods();

        private static readonly MethodInfo s_CalculateCostMethod = AccessTools.FirstMethod(
            s_PathfindExecutorType,
            method => method.Name == "CalculateCost" && method.ReturnType == typeof(float) && method.GetParameters().Length == 4);

        private static Harmony s_Harmony;
        private static int s_CachedPublicTransportLaneFine;
        private static bool s_HasCachedPenaltyValues;
        private static bool s_CachedPublicTransportLaneEnforcementEnabled;
        private static int s_CachedConfiguredPublicTransportLaneFine;
        private static bool s_LoggedFirstSetupPathfindInvocation;
        private const int MaxFocusedPublicTransportLaneCostLogsPerVehicle = 6;
        private static readonly Dictionary<Entity, int> s_FocusedPublicTransportLaneCostLogCounts = new Dictionary<Entity, int>();

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

                    StringBuilder patchedMethods = new StringBuilder(s_SetupPathfindMethods.Length * 48);
                    for (int index = 0; index < s_SetupPathfindMethods.Length; index += 1)
                    {
                        if (index > 0)
                        {
                            patchedMethods.Append("; ");
                        }

                        patchedMethods.Append(FormatMethodSignature(s_SetupPathfindMethods[index]));
                    }

                    Mod.log.Info(
                        $"VehicleUtils.SetupPathfind patch applied to {s_SetupPathfindMethods.Length} overload(s): " +
                        patchedMethods);
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
            s_FocusedPublicTransportLaneCostLogCounts.Clear();
        }

        internal static void InvalidateCachedPenaltyValues()
        {
            s_HasCachedPenaltyValues = false;
        }

        private static MethodInfo[] GetSetupPathfindMethods()
        {
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(VehicleUtils)))
            {
                if (IsSetupPathfindCandidate(method))
                {
                    methods.Add(method);
                }
            }

            if (methods.Count == 0)
            {
                return Array.Empty<MethodInfo>();
            }

            MethodInfo[] result = new MethodInfo[methods.Count];
            for (int index = 0; index < methods.Count; index += 1)
            {
                result[index] = methods[index];
            }

            return result;
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

            bool focusedRouteDiagnosticsEnabled =
                EnforcementLoggingPolicy.ShouldLogFocusedRouteRebuildDiagnostics();

            if (focusedRouteDiagnosticsEnabled && !s_LoggedFirstSetupPathfindInvocation)
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

            float addedPenalty = publicTransportPenalty * moneyWeight * math.abs(delta.y - delta.x);

            Entity focusedVehicle = ResolveFocusedVehicle(___m_Parameters);
            if (ShouldLogFocusedPublicTransportLaneCost(focusedVehicle))
            {
                LogFocusedPublicTransportLaneCost(
                    focusedVehicle,
                    rules,
                    delta,
                    ___m_Parameters,
                    publicTransportPenalty,
                    moneyWeight,
                    addedPenalty);
            }

            __result += addedPenalty;
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

            if (!Mod.IsPublicTransportLaneEnforcementEnabled)
            {
                return result;
            }

            bool allowOnPublicTransportLane = PublicTransportLanePolicy.CanUsePublicTransportLane(profile);

            if (!allowOnPublicTransportLane && entityManager.HasComponent<PublicTransportLanePendingExit>(owner))
            {
                PublicTransportLanePendingExit pendingExit = entityManager.GetComponentData<PublicTransportLanePendingExit>(owner);
                allowOnPublicTransportLane = pendingExit.m_HasLeftPublicTransportLane == 0;
                result.PendingExitActive = pendingExit.m_HasLeftPublicTransportLane == 0;
            }

            result.AllowOnPublicTransportLane = allowOnPublicTransportLane;

            SetRuleFlag(ref item.m_Parameters.m_IgnoredRules, RuleFlags.ForbidPrivateTraffic, allowOnPublicTransportLane);

            SetRuleFlag(ref item.m_Parameters.m_TaxiIgnoredRules, RuleFlags.ForbidPrivateTraffic, allowOnPublicTransportLane);

            return result;
        }

        private static Entity ResolveFocusedVehicle(PathfindParameters parameters)
        {
            Entity parkingTarget = parameters.m_ParkingTarget;
            return parkingTarget != Entity.Null ? parkingTarget : Entity.Null;
        }

        private static bool ShouldLogFocusedPublicTransportLaneCost(Entity vehicle)
        {
            if (vehicle == Entity.Null ||
                !EnforcementLoggingPolicy.ShouldLogFocusedRouteRebuildDiagnostics() ||
                !FocusedLoggingService.IsWatched(vehicle))
            {
                return false;
            }

            if (s_FocusedPublicTransportLaneCostLogCounts.TryGetValue(vehicle, out int count) &&
                count >= MaxFocusedPublicTransportLaneCostLogsPerVehicle)
            {
                return false;
            }

            return true;
        }

        private static void LogFocusedPublicTransportLaneCost(
            Entity vehicle,
            RuleFlags rules,
            float2 delta,
            PathfindParameters parameters,
            int configuredFine,
            float moneyWeight,
            float addedPenalty)
        {
            int nextCount = 1;
            if (s_FocusedPublicTransportLaneCostLogCounts.TryGetValue(vehicle, out int currentCount))
            {
                nextCount = currentCount + 1;
            }

            s_FocusedPublicTransportLaneCostLogCounts[vehicle] = nextCount;

            Mod.log.Info(
                $"FOCUSED_PT_LANE_COST: vehicle={vehicle}, " +
                $"vehicleEntity={FocusedLoggingService.FormatEntity(vehicle)}, " +
                $"logIndex={nextCount}, " +
                $"rules={FormatRuleFlags(rules)}, " +
                $"ignoredRules={FormatRuleFlags(parameters.m_IgnoredRules)}, " +
                $"taxiIgnoredRules={FormatRuleFlags(parameters.m_TaxiIgnoredRules)}, " +
                $"methods={(parameters.m_Methods == 0 ? "none" : parameters.m_Methods.ToString())}, " +
                $"pathfindFlags={(parameters.m_PathfindFlags == 0 ? "none" : parameters.m_PathfindFlags.ToString())}, " +
                $"delta=({delta.x:0.###},{delta.y:0.###}), " +
                $"moneyWeight={moneyWeight:0.###}, " +
                $"configuredFine={configuredFine}, " +
                $"addedPenalty={addedPenalty:0.###}, " +
                $"parkingTarget={FocusedLoggingService.FormatEntity(parameters.m_ParkingTarget)}");
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
            string accessBits = syncResult.HasProfile
                ? FormatAccessBits(syncResult.Profile.m_PublicTransportLaneAccessBits)
                : "none";

            string allowOnPublicTransportLane = syncResult.HasProfile
                ? syncResult.AllowOnPublicTransportLane.ToString()
                : "unavailable";

            Mod.log.Info(
                $"FOCUSED_SETUP_PATHFIND: vehicle={owner}, " +
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
            StringBuilder signature = new StringBuilder(64);
            signature.Append(method.DeclaringType?.Name);
            signature.Append('.');
            signature.Append(method.Name);
            signature.Append('(');
            for (int index = 0; index < parameters.Length; index += 1)
            {
                if (index > 0)
                {
                    signature.Append(", ");
                }

                signature.Append(parameters[index].ParameterType.Name);
            }

            signature.Append(')');
            return signature.ToString();
        }
    }
}
