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
        private static readonly Type s_PathfindExecutorType = AccessTools.Inner(typeof(PathfindJobs), "PathfindExecutor");
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
            method => method.Name == "CalculateCost" && method.ReturnType == typeof(float) && method.GetParameters().Length == 4);

        private static Harmony s_Harmony;

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            try
            {
                s_Harmony = new Harmony(HarmonyId);
                HarmonyMethod prefix = new HarmonyMethod(typeof(VehicleUtilsPatches), nameof(SetupPathfindPrefix));
                s_Harmony.Patch(s_SetupPathfindMethod, prefix: prefix);

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
        }

        private static void SetupPathfindPrefix(ref SetupQueueItem item)
        {
            EnforcementPolicyImpactService.RecordPathRequest();
            World world = World.DefaultGameObjectInjectionWorld;
            EntityManager entityManager = world.EntityManager;
            Entity owner = item.m_Owner;
            Car car = entityManager.GetComponentData<Car>(owner);
            item.m_Parameters.m_Weights.m_Value.z = 0f;
        }

        private static void CalculateCostPostfix(ref float __result, RuleFlags rules, float2 delta, PathfindParameters ___m_Parameters)
        {
            if (!Mod.IsPublicTransportLaneEnforcementEnabled || (rules & RuleFlags.ForbidPrivateTraffic) == 0)
            {
                return;
            }

            float moneyWeight = ___m_Parameters.m_Weights.money;
            if (moneyWeight <= 0f)
            {
                return;
            }

            int publicTransportPenalty = EnforcementPenaltyService.GetPublicTransportLaneFine();
            if (publicTransportPenalty <= 0)
            {
                return;
            }

            __result += publicTransportPenalty * moneyWeight * math.abs(delta.y - delta.x);
        }

        private static void SyncPrivateTrafficIgnoredRules(World world, Entity owner, Car car, ref SetupQueueItem item)
        {
            PathfindingMoneyPenaltySystem system = world.GetExistingSystemManaged<PathfindingMoneyPenaltySystem>();
            if (system == null)
            {
                return;
            }

            BusLaneVehicleTypeLookups typeLookups = BusLaneVehicleTypeLookups.Create(system);
            typeLookups.Update(system);

            if (!BusLanePolicy.TryGetDesiredPermissionState(owner, car, EnforcementGameplaySettingsService.Current, ref typeLookups, out bool shouldTrack, out CarFlags desiredMask) || !shouldTrack)
            {
                return;
            }

            bool allowOnPublicTransportLane = (desiredMask & CarFlags.UsePublicTransportLanes) != 0;
            SetRuleFlag(ref item.m_Parameters.m_IgnoredRules, RuleFlags.ForbidPrivateTraffic, allowOnPublicTransportLane);
            SetRuleFlag(ref item.m_Parameters.m_TaxiIgnoredRules, RuleFlags.ForbidPrivateTraffic, allowOnPublicTransportLane);
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
