using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Game;
using Game.Pathfind;
using HarmonyLib;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    internal static class MidBlockAccessPathfindingPenaltyPatches
    {
        private const string HarmonyId =
            "Traffic_Law_Enforcement.MidBlockAccessPathfindingPenaltyPatches";

        private static readonly Type s_PathfindExecutorType =
            AccessTools.Inner(typeof(PathfindJobs), "PathfindExecutor");

        private static Harmony s_Harmony;
        private static MethodInfo s_TargetMethod;
        private static int s_SourceLaneArgIndex = -1;
        private static int s_TargetLaneArgIndex = -1;
        private static int s_LogCount;

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            try
            {
                if (s_PathfindExecutorType == null)
                {
                    Mod.log.Info("Mid-block access pathfind hook skipped: PathfindExecutor type not found.");
                    return;
                }

                s_TargetMethod =
                    FindBestTransitionCostMethod(
                        out s_SourceLaneArgIndex,
                        out s_TargetLaneArgIndex);
                if (s_TargetMethod == null ||
                    s_SourceLaneArgIndex < 0 ||
                    s_TargetLaneArgIndex < 0)
                {
                    LogCandidateMethods();
                    Mod.log.Info("Mid-block access pathfind hook skipped: no suitable transition-cost method found.");
                    return;
                }

                s_Harmony = new Harmony(HarmonyId);
                HarmonyMethod postfix =
                    new HarmonyMethod(
                        typeof(MidBlockAccessPathfindingPenaltyPatches),
                        nameof(TransitionCostPostfix));
                s_Harmony.Patch(s_TargetMethod, postfix: postfix);

                Mod.log.Info(
                    $"Mid-block access pathfind hook patched: {DescribeMethod(s_TargetMethod)}, " +
                    $"sourceArgIndex={s_SourceLaneArgIndex}, targetArgIndex={s_TargetLaneArgIndex}");
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                Mod.log.Error(ex, "Failed to apply mid-block access pathfind hook.");
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
            s_TargetMethod = null;
            s_SourceLaneArgIndex = -1;
            s_TargetLaneArgIndex = -1;
        }

        private static MethodInfo FindBestTransitionCostMethod(
            out int sourceLaneArgIndex,
            out int targetLaneArgIndex)
        {
            sourceLaneArgIndex = -1;
            targetLaneArgIndex = -1;

            MethodInfo bestMethod = null;
            int bestScore = int.MinValue;
            int bestSourceIndex = -1;
            int bestTargetIndex = -1;

            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(s_PathfindExecutorType))
            {
                if (method.IsStatic || method.ReturnType != typeof(float))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                List<(int Index, string Name)> entityParams =
                    new List<(int, string)>();
                for (int index = 0; index < parameters.Length; index += 1)
                {
                    if (parameters[index].ParameterType == typeof(Entity))
                    {
                        entityParams.Add((index, parameters[index].Name ?? string.Empty));
                    }
                }

                if (entityParams.Count < 2)
                {
                    continue;
                }

                int sourceIndex = FindBestSourceIndex(entityParams);
                int targetIndex = FindBestTargetIndex(entityParams, sourceIndex);
                if (sourceIndex < 0 ||
                    targetIndex < 0 ||
                    sourceIndex == targetIndex)
                {
                    continue;
                }

                int score =
                    ScoreMethod(
                        method,
                        entityParams,
                        sourceIndex,
                        targetIndex);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMethod = method;
                    bestSourceIndex = sourceIndex;
                    bestTargetIndex = targetIndex;
                }
            }

            sourceLaneArgIndex = bestSourceIndex;
            targetLaneArgIndex = bestTargetIndex;
            return bestMethod;
        }

        private static int FindBestSourceIndex(
            List<(int Index, string Name)> entityParams)
        {
            foreach ((int Index, string Name) entry in entityParams)
            {
                string name = entry.Name.ToLowerInvariant();
                if (name.Contains("source") ||
                    name.Contains("from") ||
                    name.Contains("previous") ||
                    name.Contains("prev"))
                {
                    return entry.Index;
                }
            }

            return entityParams[0].Index;
        }

        private static int FindBestTargetIndex(
            List<(int Index, string Name)> entityParams,
            int sourceIndex)
        {
            foreach ((int Index, string Name) entry in entityParams)
            {
                if (entry.Index == sourceIndex)
                {
                    continue;
                }

                string name = entry.Name.ToLowerInvariant();
                if (name.Contains("target") ||
                    name.Contains("to") ||
                    name.Contains("next") ||
                    name.Contains("connection"))
                {
                    return entry.Index;
                }
            }

            foreach ((int Index, string Name) entry in entityParams)
            {
                if (entry.Index != sourceIndex)
                {
                    return entry.Index;
                }
            }

            return -1;
        }

        private static int ScoreMethod(
            MethodInfo method,
            List<(int Index, string Name)> entityParams,
            int sourceIndex,
            int targetIndex)
        {
            int score = 0;
            string methodName = method.Name.ToLowerInvariant();

            if (methodName.Contains("cost"))
            {
                score += 40;
            }

            if (methodName.Contains("transition"))
            {
                score += 30;
            }

            if (methodName.Contains("connection"))
            {
                score += 20;
            }

            if (methodName.Contains("lane"))
            {
                score += 10;
            }

            foreach ((int Index, string Name) entry in entityParams)
            {
                string name = entry.Name.ToLowerInvariant();
                if (entry.Index == sourceIndex &&
                    (name.Contains("source") ||
                     name.Contains("from") ||
                     name.Contains("previous") ||
                     name.Contains("prev")))
                {
                    score += 25;
                }

                if (entry.Index == targetIndex &&
                    (name.Contains("target") ||
                     name.Contains("to") ||
                     name.Contains("next") ||
                     name.Contains("connection")))
                {
                    score += 25;
                }
            }

            return score;
        }

        private static void LogCandidateMethods()
        {
            if (s_PathfindExecutorType == null)
            {
                return;
            }

            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(s_PathfindExecutorType))
            {
                if (method.IsStatic || method.ReturnType != typeof(float))
                {
                    continue;
                }

                Mod.log.Info(
                    $"Mid-block access hook candidate: {DescribeMethod(method)}");
            }
        }

        private static string DescribeMethod(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            StringBuilder parameterList =
                new StringBuilder(parameters.Length * 24);
            for (int index = 0; index < parameters.Length; index += 1)
            {
                if (index > 0)
                {
                    parameterList.Append(", ");
                }

                ParameterInfo parameter = parameters[index];
                parameterList.Append(parameter.ParameterType.Name);
                parameterList.Append(' ');
                parameterList.Append(parameter.Name);
            }

            return
                $"{method.DeclaringType?.FullName}.{method.Name}({parameterList})";
        }

        private static void TransitionCostPostfix(
            object[] __args,
            ref float __result,
            PathfindParameters ___m_Parameters,
            MethodBase __originalMethod)
        {
            if (!Mod.IsMidBlockCrossingEnforcementEnabled)
            {
                return;
            }

            if (s_SourceLaneArgIndex < 0 || s_TargetLaneArgIndex < 0)
            {
                return;
            }

            if (__args == null ||
                __args.Length <= s_SourceLaneArgIndex ||
                __args.Length <= s_TargetLaneArgIndex)
            {
                return;
            }

            if (!(__args[s_SourceLaneArgIndex] is Entity sourceLane) ||
                !(__args[s_TargetLaneArgIndex] is Entity targetLane))
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
            if (!MidBlockCrossingPolicy.TryGetIllegalAccessTransition(
                    entityManager,
                    sourceLane,
                    targetLane,
                    out LaneTransitionViolationReasonCode reasonCode))
            {
                return;
            }

            int penalty = EnforcementPenaltyService.GetMidBlockCrossingFine();
            if (penalty <= 0)
            {
                return;
            }

            float addedCost = penalty * moneyWeight;
            __result += addedCost;

            if (EnforcementLoggingPolicy.ShouldLogPathfindingPenaltyDiagnostics() &&
                s_LogCount < 16)
            {
                s_LogCount += 1;
                Mod.log.Info(
                    $"Mid-block access pre-penalty applied: method={__originalMethod?.Name}, " +
                    $"sourceLane={sourceLane}, targetLane={targetLane}, " +
                    $"reason={RoutePenaltyInspection.FormatMidBlockReasonTag(reasonCode)}, " +
                    $"moneyWeight={moneyWeight:0.###}, addedCost={addedCost:0.###}");
            }
        }
    }
}
