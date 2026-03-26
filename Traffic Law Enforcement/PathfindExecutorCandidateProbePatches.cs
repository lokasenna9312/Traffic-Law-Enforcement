using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game;
using Game.Pathfind;
using HarmonyLib;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    [HarmonyPatch]
    internal static class PathfindExecutorCandidateProbePatches
    {
        private const string HarmonyId =
            "Traffic_Law_Enforcement.PathfindExecutorCandidateProbePatches";

        private static readonly Type s_PathfindExecutorType =
            AccessTools.Inner(typeof(PathfindJobs), "PathfindExecutor");

        private static readonly HashSet<string> s_SeenMethods =
            new HashSet<string>(StringComparer.Ordinal);

        private static Harmony s_Harmony;

        private static IEnumerable<MethodBase> TargetMethods()
        {
            if (s_PathfindExecutorType == null)
            {
                yield break;
            }

            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(s_PathfindExecutorType))
            {
                if (method.IsStatic)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                bool hasEntityParameter = parameters.Any(p => p.ParameterType == typeof(Entity));
                bool looksInteresting =
                    method.Name.IndexOf("cost", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    method.Name.IndexOf("transition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    method.Name.IndexOf("connection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    hasEntityParameter;

                if (!looksInteresting)
                {
                    continue;
                }

                yield return method;
            }
        }

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            try
            {
                s_Harmony = new Harmony(HarmonyId);
                s_Harmony.PatchAll(typeof(PathfindExecutorCandidateProbePatches).Assembly);

                if (EnforcementLoggingPolicy.ShouldLogPathfindingPenaltyDiagnostics())
                {
                    Mod.log.Info("[PATHFIND_PROBE] Candidate probe patches applied.");
                }
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                Mod.log.Error(ex, "[PATHFIND_PROBE] Failed to apply candidate probe patches.");
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
            s_SeenMethods.Clear();
        }

        [HarmonyPrefix]
        private static void Prefix(object[] __args, MethodBase __originalMethod)
        {
            if (!EnforcementLoggingPolicy.ShouldLogPathfindingPenaltyDiagnostics())
            {
                return;
            }

            string key = __originalMethod?.ToString() ?? "(null)";
            if (!s_SeenMethods.Add(key))
            {
                return;
            }

            Mod.log.Info(
                "[PATHFIND_PROBE] Entered method: " +
                $"{DescribeMethod(__originalMethod as MethodInfo)}, " +
                $"argSummary={SummarizeArgs(__args)}");
        }

        private static string SummarizeArgs(object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return "none";
            }

            int entityCount = 0;
            int nonNullCount = 0;
            List<string> samples = new List<string>(4);

            for (int i = 0; i < args.Length; i += 1)
            {
                object value = args[i];
                if (value != null)
                {
                    nonNullCount += 1;
                }

                if (value is Entity entityValue)
                {
                    entityCount += 1;
                    if (samples.Count < 4)
                    {
                        samples.Add($"arg{i}=Entity({entityValue})");
                    }
                }
                else if (value != null && samples.Count < 4)
                {
                    samples.Add($"arg{i}={value.GetType().Name}");
                }
            }

            return
                $"count={args.Length}, nonNull={nonNullCount}, entityArgs={entityCount}, " +
                $"samples=[{string.Join(", ", samples)}]";
        }

        private static string DescribeMethod(MethodInfo method)
        {
            if (method == null)
            {
                return "null";
            }

            ParameterInfo[] parameters = method.GetParameters();
            string parameterList = string.Join(
                ", ",
                parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));

            return $"{method.DeclaringType?.FullName}.{method.Name}({parameterList})";
        }
    }
}