using System;
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

        private static readonly Type s_PathfindExecutorType =
            AccessTools.Inner(typeof(PathfindJobs), "PathfindExecutor");

        private static readonly MethodInfo s_SetupPathfindMethod = AccessTools.Method(
            typeof(VehicleUtils),
            nameof(VehicleUtils.SetupPathfind),
            new[]
            {
                typeof(CarCurrentLane).MakeByRefType(),
                typeof(PathOwner).MakeByRefType(),
                typeof(NativeQueue<SetupQueueItem>.ParallelWriter),
                typeof(SetupQueueItem)
            });

        private static readonly MethodInfo s_CalculateCostMethod = AccessTools.FirstMethod(
            s_PathfindExecutorType,
            method => method.Name == "CalculateCost" &&
                      method.ReturnType == typeof(float) &&
                      method.GetParameters().Length == 4);

        private static Harmony s_Harmony;
        private static int s_CachedPublicTransportLaneFine;
        private static bool s_HasCachedPenaltyValues;
        private static bool s_CachedPublicTransportLaneEnforcementEnabled;
        private static int s_CachedConfiguredPublicTransportLaneFine;

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

                HarmonyMethod prefix =
                    new HarmonyMethod(typeof(VehicleUtilsPatches), nameof(SetupPathfindPrefix));
                s_Harmony.Patch(s_SetupPathfindMethod, prefix: prefix);

                HarmonyMethod calculateCostPostfix =
                    new HarmonyMethod(typeof(VehicleUtilsPatches), nameof(CalculateCostPostfix));
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
        }

        internal static void InvalidateCachedPenaltyValues()
        {
            s_HasCachedPenaltyValues = false;
        }

        private static void SetupPathfindPrefix(ref SetupQueueItem item)
        {
            EnforcementPolicyImpactService.RecordPathRequest();

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

            if (!s_HasCachedPenaltyValues)
            {
                RefreshCachedPenaltyValues();
            }

            SyncPrivateTrafficIgnoredRules(entityManager, owner, ref item);

            item.m_Parameters.m_Weights.m_Value.z = 0f;
        }

        private static void CalculateCostPostfix(
            ref float __result,
            RuleFlags rules,
            float2 delta,
            PathfindParameters ___m_Parameters)
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

            if (s_HasCachedPenaltyValues &&
                s_CachedPublicTransportLaneEnforcementEnabled == enforcementEnabled &&
                s_CachedConfiguredPublicTransportLaneFine == configuredFineAmount)
            {
                return;
            }

            s_HasCachedPenaltyValues = true;
            s_CachedPublicTransportLaneEnforcementEnabled = enforcementEnabled;
            s_CachedConfiguredPublicTransportLaneFine = configuredFineAmount;
            s_CachedPublicTransportLaneFine = configuredFineAmount;
        }

        private static void SyncPrivateTrafficIgnoredRules(
            EntityManager entityManager,
            Entity owner,
            ref SetupQueueItem item)
        {
            if (!entityManager.HasComponent<VehicleTrafficLawProfile>(owner))
            {
                return;
            }

            VehicleTrafficLawProfile profile =
                entityManager.GetComponentData<VehicleTrafficLawProfile>(owner);

            bool allowOnPublicTransportLane =
                (profile.m_DesiredPublicTransportLaneMask & CarFlags.UsePublicTransportLanes) != 0;

            if (!allowOnPublicTransportLane &&
                entityManager.HasComponent<PublicTransportLanePendingExit>(owner))
            {
                allowOnPublicTransportLane = true;
            }

            SetRuleFlag(
                ref item.m_Parameters.m_IgnoredRules,
                RuleFlags.ForbidPrivateTraffic,
                allowOnPublicTransportLane);

            SetRuleFlag(
                ref item.m_Parameters.m_TaxiIgnoredRules,
                RuleFlags.ForbidPrivateTraffic,
                allowOnPublicTransportLane);
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
    }
}