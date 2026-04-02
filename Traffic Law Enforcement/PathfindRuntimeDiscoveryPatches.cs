using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Game.Pathfind;
using HarmonyLib;
using Unity.Jobs;

namespace Traffic_Law_Enforcement
{
    internal static class PathfindRuntimeDiscoveryPatches
    {
        private const string HarmonyId =
            "Traffic_Law_Enforcement.PathfindRuntimeDiscoveryPatches";

        private static readonly Type s_WorkerActionsType =
            AccessTools.Inner(typeof(PathfindQueueSystem), "WorkerActions");

        private static readonly Type s_PathfindWorkerJobType =
            AccessTools.Inner(typeof(PathfindQueueSystem), "PathfindWorkerJob");

        private static readonly MethodInfo s_IJobScheduleGenericMethod =
            AccessTools.FirstMethod(
                typeof(IJobExtensions),
                method =>
                    method.IsGenericMethodDefinition &&
                    method.Name == nameof(IJobExtensions.Schedule) &&
                    method.GetParameters().Length == 2);

        private static readonly MethodInfo s_LogScheduleHandoffMethod =
            AccessTools.DeclaredMethod(
                typeof(PathfindRuntimeDiscoveryPatches),
                nameof(LogScheduleHandoff));

        private static Harmony s_Harmony;
        private static MethodInfo s_ScheduleWorkerJobsTarget;
        private static MethodInfo s_PublicExecuteTarget;
        private static bool s_LoggedScheduleFirstHit;
        private static bool s_LoggedExecuteFirstHit;

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            try
            {
                if (s_WorkerActionsType == null)
                {
                    Mod.log.Info(
                        "Schedule-handoff discovery skipped: WorkerActions type not found.");
                    return;
                }

                s_ScheduleWorkerJobsTarget = FindScheduleWorkerJobsTarget();
                s_PublicExecuteTarget = FindPublicExecuteTarget();
                if (s_ScheduleWorkerJobsTarget == null)
                {
                    Mod.log.Info(
                        "Schedule-handoff discovery skipped: ScheduleWorkerJobs target not found.");
                    return;
                }

                if (s_PublicExecuteTarget == null)
                {
                    Mod.log.Info(
                        "Execute A/B discovery skipped: public Execute target not found.");
                    return;
                }

                s_Harmony = new Harmony(HarmonyId);
                s_LoggedScheduleFirstHit = false;
                s_LoggedExecuteFirstHit = false;
                s_Harmony.Patch(
                    s_ScheduleWorkerJobsTarget,
                    transpiler: new HarmonyMethod(
                        typeof(PathfindRuntimeDiscoveryPatches),
                        nameof(ScheduleWorkerJobsTranspiler)));
                s_Harmony.Patch(
                    s_PublicExecuteTarget,
                    prefix: new HarmonyMethod(
                        typeof(PathfindRuntimeDiscoveryPatches),
                        nameof(PublicExecutePrefix)));
                Mod.log.Info(
                    $"[SCHED-RAW] install target={DescribeMethod(s_ScheduleWorkerJobsTarget)}");
                Mod.log.Info(
                    $"[EXEC-AB] install target={DescribeMethod(s_PublicExecuteTarget)}");
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                s_ScheduleWorkerJobsTarget = null;
                s_PublicExecuteTarget = null;
                Mod.log.Error(ex, "Failed to apply schedule-handoff discovery patches.");
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
            s_ScheduleWorkerJobsTarget = null;
            s_PublicExecuteTarget = null;
            s_LoggedScheduleFirstHit = false;
            s_LoggedExecuteFirstHit = false;
        }

        private static MethodInfo FindScheduleWorkerJobsTarget()
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(PathfindQueueSystem)))
            {
                if (IsScheduleWorkerJobsCandidate(method))
                {
                    return method;
                }
            }

            return null;
        }

        private static MethodInfo FindPublicExecuteTarget()
        {
            if (s_PathfindWorkerJobType == null)
            {
                return null;
            }

            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(s_PathfindWorkerJobType))
            {
                if (IsPublicExecuteCandidate(method))
                {
                    return method;
                }
            }

            return null;
        }

        private static bool IsScheduleWorkerJobsCandidate(MethodInfo method)
        {
            if (method == null ||
                method.IsStatic ||
                method.ReturnType != typeof(void) ||
                !string.Equals(method.Name, "ScheduleWorkerJobs", StringComparison.Ordinal))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 1 &&
                parameters[0].ParameterType == s_WorkerActionsType?.MakeByRefType();
        }

        private static bool IsPublicExecuteCandidate(MethodInfo method)
        {
            if (method == null ||
                method.IsStatic ||
                method.ReturnType != typeof(void) ||
                !string.Equals(method.Name, "Execute", StringComparison.Ordinal))
            {
                return false;
            }

            return method.GetParameters().Length == 0;
        }

        private static IEnumerable<CodeInstruction> ScheduleWorkerJobsTranspiler(
            IEnumerable<CodeInstruction> instructions)
        {
            bool inserted = false;

            foreach (CodeInstruction instruction in instructions)
            {
                if (!inserted && IsScheduleCall(instruction))
                {
                    yield return new CodeInstruction(OpCodes.Call, s_LogScheduleHandoffMethod);
                    inserted = true;
                }

                yield return instruction;
            }

            if (!inserted)
            {
                throw new InvalidOperationException(
                    "Failed to locate IJobExtensions.Schedule(jobData, jobHandle) handoff.");
            }
        }

        private static bool IsScheduleCall(CodeInstruction instruction)
        {
            if (instruction == null || instruction.operand is not MethodInfo method)
            {
                return false;
            }

            if (!string.Equals(method.Name, nameof(IJobExtensions.Schedule), StringComparison.Ordinal) ||
                method.DeclaringType != typeof(IJobExtensions) ||
                !method.IsGenericMethod)
            {
                return false;
            }

            return method.GetGenericMethodDefinition() == s_IJobScheduleGenericMethod;
        }

        private static string DescribeMethod(MethodInfo method)
        {
            if (method == null)
            {
                return "null";
            }

            ParameterInfo[] parameters = method.GetParameters();
            StringBuilder parameterList = new StringBuilder(parameters.Length * 24);
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

            return $"{method.DeclaringType?.FullName}.{method.Name}({parameterList})";
        }

        private static void LogScheduleHandoff()
        {
            if (s_LoggedScheduleFirstHit)
            {
                return;
            }

            s_LoggedScheduleFirstHit = true;
            Mod.log.Info("[SCHED-RAW] firstHit=true");
        }

        private static void PublicExecutePrefix()
        {
            if (s_LoggedExecuteFirstHit)
            {
                return;
            }

            s_LoggedExecuteFirstHit = true;
            Mod.log.Info("[EXEC-AB] firstHit=true");
        }
    }
}
