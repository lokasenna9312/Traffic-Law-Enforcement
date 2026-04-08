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

                s_TargetMethod = FindTransitionExpansionMethod();
                if (s_TargetMethod == null)
                {
                    LogCandidateMethods();
                    Mod.log.Info(
                        "Mid-block access pathfind hook skipped: no exact connection-expansion seam found.");
                    return;
                }

                s_Harmony = new Harmony(HarmonyId);
                s_LogCount = 0;
                HarmonyMethod postfix =
                    new HarmonyMethod(
                        typeof(MidBlockAccessPathfindingPenaltyPatches),
                        nameof(AddHeapDataPrefix));
                s_Harmony.Patch(s_TargetMethod, prefix: postfix);

                Mod.log.Info(
                    $"Mid-block access pathfind hook patched: {DescribeMethod(s_TargetMethod)}");
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
            s_LogCount = 0;
        }

        private static MethodInfo FindTransitionExpansionMethod()
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(s_PathfindExecutorType))
            {
                if (!IsTransitionExpansionCandidate(method))
                {
                    continue;
                }

                return method;
            }

            return null;
        }

        private static bool IsTransitionExpansionCandidate(MethodInfo method)
        {
            if (method == null ||
                method.IsStatic ||
                method.ReturnType != typeof(void) ||
                !string.Equals(method.Name, "AddHeapData", StringComparison.Ordinal))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 11)
            {
                return false;
            }

            return parameters[0].ParameterType == typeof(int) &&
                parameters[1].ParameterType == typeof(EdgeID) &&
                parameters[2].ParameterType == typeof(EdgeID) &&
                parameters[3].ParameterType == typeof(Edge).MakeByRefType() &&
                parameters[4].ParameterType == typeof(EdgeFlags) &&
                parameters[5].ParameterType == typeof(RuleFlags) &&
                parameters[6].ParameterType == typeof(float) &&
                parameters[7].ParameterType == typeof(float);
        }

        private static void LogCandidateMethods()
        {
            if (s_PathfindExecutorType == null)
            {
                return;
            }

            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(s_PathfindExecutorType))
            {
                if (method.IsStatic)
                {
                    continue;
                }

                if (string.Equals(method.Name, "AddHeapData", StringComparison.Ordinal) ||
                    string.Equals(method.Name, "CalculateCost", StringComparison.Ordinal) ||
                    string.Equals(method.Name, "CalculateTotalCost", StringComparison.Ordinal))
                {
                    Mod.log.Info(
                        $"Mid-block access hook candidate: {DescribeMethod(method)}");
                }
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

        private static void AddHeapDataPrefix(
            EdgeID id,
            EdgeID id2,
            Edge edge,
            ref float baseCost,
            PathfindParameters ___m_Parameters,
            UnsafePathfindData ___m_PathfindData,
            MethodBase __originalMethod)
        {
            if (!Mod.IsMidBlockCrossingEnforcementEnabled)
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
            if (id2.m_Index < 0 || id2.m_Index >= ___m_PathfindData.m_Edges.Length)
            {
                return;
            }

            Entity sourceLane = edge.m_Owner;
            Entity targetLane = ___m_PathfindData.m_Edges[id2.m_Index].m_Owner;
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
            baseCost += addedCost;

            if (EnforcementLoggingPolicy.ShouldLogPathfindingPenaltyDiagnostics() &&
                s_LogCount < 16)
            {
                return;
            }

            string tag = $"mid-block({RoutePenaltyInspection.FormatMidBlockReasonTag(reasonCode)})";
            Mod.log.Info(
                $"{DirectAccessHitLogPrefix} firstHit=true " +
                $"reason={reasonCode} tag={tag} " +
                $"sourceLane={sourceLane} " +
                $"targetLane={targetLane}");
        }

        private static void MaybeLogDirectOppositeFlowHit(
            Entity sourceLane,
            Entity targetLane,
            LaneTransitionViolationReasonCode reasonCode)
        {
            if (reasonCode != LaneTransitionViolationReasonCode.OppositeFlowSameRoadSegment)
            {
                return;
            }

            if (System.Threading.Interlocked.Exchange(ref s_DirectOppositeFlowHitLogged, 1) != 0)
            {
                return;
            }

            string tag = $"mid-block({RoutePenaltyInspection.FormatMidBlockReasonTag(reasonCode)})";
            Mod.log.Info(
                $"{DirectOppositeFlowHitLogPrefix} firstHit=true " +
                $"reason={reasonCode} tag={tag} " +
                $"sourceLane={sourceLane} " +
                $"targetLane={targetLane}");
        }
    }
}

