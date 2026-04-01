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
        private const int MinimumBindingScore = 100;
        private const int MinimumBindingScoreGap = 15;

        private static readonly Type s_PathfindExecutorType =
            AccessTools.Inner(typeof(PathfindJobs), "PathfindExecutor");

        private static Harmony s_Harmony;
        private static MethodInfo s_TargetMethod;
        private static int s_SourceLaneArgIndex = -1;
        private static int s_TargetLaneArgIndex = -1;
        private static int s_LogCount;

        internal static bool IsApplied => s_Harmony != null && s_TargetMethod != null;

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
                    Mod.log.Info("Mid-block access pathfind hook skipped: no sufficiently confident transition-cost method found.");
                    return;
                }

                s_Harmony = new Harmony(HarmonyId);
                s_LogCount = 0;
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
            s_LogCount = 0;
        }

        private static MethodInfo FindBestTransitionCostMethod(
            out int sourceLaneArgIndex,
            out int targetLaneArgIndex)
        {
            sourceLaneArgIndex = -1;
            targetLaneArgIndex = -1;

            MethodInfo bestMethod = null;
            int bestScore = int.MinValue;
            int secondBestScore = int.MinValue;
            int bestSourceIndex = -1;
            int bestTargetIndex = -1;
            bool bestSourceNameMatch = false;
            bool bestTargetNameMatch = false;

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

                bool sourceNameMatch = IsSourceNameMatch(entityParams, sourceIndex);
                bool targetNameMatch = IsTargetNameMatch(entityParams, targetIndex);
                int score =
                    ScoreMethod(
                        method,
                        entityParams,
                        sourceIndex,
                        targetIndex,
                        sourceNameMatch,
                        targetNameMatch);
                if (score > bestScore)
                {
                    secondBestScore = bestScore;
                    bestScore = score;
                    bestMethod = method;
                    bestSourceIndex = sourceIndex;
                    bestTargetIndex = targetIndex;
                    bestSourceNameMatch = sourceNameMatch;
                    bestTargetNameMatch = targetNameMatch;
                }
                else if (score > secondBestScore)
                {
                    secondBestScore = score;
                }
            }

            if (!HasHighConfidenceBinding(
                    bestMethod,
                    bestScore,
                    secondBestScore,
                    bestSourceNameMatch,
                    bestTargetNameMatch))
            {
                sourceLaneArgIndex = -1;
                targetLaneArgIndex = -1;
                return null;
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
            int targetIndex,
            bool sourceNameMatch,
            bool targetNameMatch)
        {
            int score = 0;
            string methodName = method.Name.ToLowerInvariant();

            if (string.Equals(method.Name, "CalculateCost", StringComparison.Ordinal))
            {
                score += 60;
            }

            if (methodName.Contains("cost"))
            {
                score += 25;
            }

            if (methodName.Contains("transition"))
            {
                score += 20;
            }

            if (methodName.Contains("connection"))
            {
                score += 15;
            }

            if (methodName.Contains("lane"))
            {
                score += 10;
            }

            if (entityParams.Count == 2)
            {
                score += 20;
            }

            if (sourceNameMatch)
            {
                score += 25;
            }

            if (targetNameMatch)
            {
                score += 25;
            }

            return score;
        }

        private static bool HasHighConfidenceBinding(
            MethodInfo bestMethod,
            int bestScore,
            int secondBestScore,
            bool sourceNameMatch,
            bool targetNameMatch)
        {
            if (bestMethod == null)
            {
                return false;
            }

            if (!sourceNameMatch || !targetNameMatch)
            {
                Mod.log.Info(
                    $"Mid-block access pathfind hook skipped: weak binding confidence for {DescribeMethod(bestMethod)} (sourceNameMatch={sourceNameMatch}, targetNameMatch={targetNameMatch}).");
                return false;
            }

            if (bestScore < MinimumBindingScore)
            {
                Mod.log.Info(
                    $"Mid-block access pathfind hook skipped: low binding score {bestScore} for {DescribeMethod(bestMethod)}.");
                return false;
            }

            if (secondBestScore != int.MinValue &&
                bestScore - secondBestScore < MinimumBindingScoreGap)
            {
                Mod.log.Info(
                    $"Mid-block access pathfind hook skipped: ambiguous binding between candidates (bestScore={bestScore}, secondBestScore={secondBestScore}).");
                return false;
            }

            return true;
        }

        private static bool IsSourceNameMatch(
            List<(int Index, string Name)> entityParams,
            int sourceIndex)
        {
            for (int index = 0; index < entityParams.Count; index += 1)
            {
                (int Index, string Name) entry = entityParams[index];
                if (entry.Index == sourceIndex)
                {
                    string name = entry.Name.ToLowerInvariant();
                    return name.Contains("source") ||
                        name.Contains("from") ||
                        name.Contains("previous") ||
                        name.Contains("prev");
                }
            }

            return false;
        }

        private static bool IsTargetNameMatch(
            List<(int Index, string Name)> entityParams,
            int targetIndex)
        {
            for (int index = 0; index < entityParams.Count; index += 1)
            {
                (int Index, string Name) entry = entityParams[index];
                if (entry.Index == targetIndex)
                {
                    string name = entry.Name.ToLowerInvariant();
                    return name.Contains("target") ||
                        name.Contains("to") ||
                        name.Contains("next") ||
                        name.Contains("connection");
                }
            }

            return false;
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
