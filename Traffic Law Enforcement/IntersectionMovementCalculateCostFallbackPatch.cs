using System;
using System.Collections.Generic;
using System.Reflection;
using Game;
using Game.Pathfind;
using HarmonyLib;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    [HarmonyPatch]
    internal static class IntersectionMovementCalculateCostFallbackPatch
    {
        private static int s_LogCount;
        private static readonly HashSet<string> s_ActivatedMethods = new HashSet<string>();

        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type executorType = AccessTools.Inner(typeof(PathfindJobs), "PathfindExecutor");
            if (executorType == null)
            {
                Mod.log.Info("Intersection fallback hook skipped: PathfindExecutor type not found.");
                yield break;
            }

            var methods = AccessTools.GetDeclaredMethods(executorType);
            for (int index = 0; index < methods.Count; index += 1)
            {
                MethodInfo method = methods[index];
                if (method.IsStatic)
                {
                    continue;
                }

                if (!string.Equals(method.Name, "CalculateCost", StringComparison.Ordinal))
                {
                    continue;
                }

                Mod.log.Info($"Intersection fallback hook target selected: {method}");
                yield return method;
            }
        }

        private static void Postfix(object[] __args, ref float __result, PathfindParameters ___m_Parameters, MethodBase __originalMethod)
        {
            string methodKey = __originalMethod?.ToString() ?? "(null)";
            if (s_ActivatedMethods.Add(methodKey))
            {
                Mod.log.Info($"Intersection fallback hook active: method={methodKey}, argCount={__args?.Length ?? 0}");
            }

            if (!Mod.IsIntersectionMovementEnforcementEnabled)
            {
                return;
            }

            if (__args == null || __args.Length < 2)
            {
                return;
            }

            float moneyWeight = ___m_Parameters.m_Weights.money;
            if (moneyWeight <= 0f)
            {
                return;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                return;
            }

            EntityManager entityManager = world.EntityManager;
            for (int sourceIndex = 0; sourceIndex < __args.Length - 1; sourceIndex += 1)
            {
                if (!(__args[sourceIndex] is Entity sourceLane) || sourceLane == Entity.Null)
                {
                    continue;
                }

                for (int targetIndex = sourceIndex + 1; targetIndex < __args.Length; targetIndex += 1)
                {
                    if (!(__args[targetIndex] is Entity targetLane) || targetLane == Entity.Null)
                    {
                        continue;
                    }

                    if (!IntersectionMovementPolicy.TryGetIllegalIntersectionMovement(
                            entityManager,
                            sourceLane,
                            targetLane,
                            out LaneMovement actualMovement,
                            out LaneMovement allowedMovement))
                    {
                        continue;
                    }

                    int penalty = EnforcementPenaltyService.GetIntersectionMovementFine();
                    if (penalty <= 0)
                    {
                        return;
                    }

                    float addedCost = penalty * moneyWeight;
                    __result += addedCost;

                    if (EnforcementLoggingPolicy.ShouldLogPathfindingPenaltyDiagnostics() && s_LogCount < 16)
                    {
                        s_LogCount += 1;
                        Mod.log.Info(
                            $"Intersection fallback pre-penalty applied: method={__originalMethod?.Name}, " +
                            $"sourceLane={sourceLane}, " +
                            $"targetLane={targetLane}, " +
                            $"actual={IntersectionMovementPolicy.FormatMovement(actualMovement)}, " +
                            $"allowed={IntersectionMovementPolicy.FormatMovement(allowedMovement)}, " +
                            $"moneyWeight={moneyWeight:0.###}, addedCost={addedCost:0.###}");
                    }

                    return;
                }
            }
        }
    }
}

