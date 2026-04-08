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

        private static readonly Type s_WorkerActionType =
            AccessTools.Inner(typeof(PathfindQueueSystem), "WorkerAction");

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

        private static readonly FieldInfo s_WorkerActionsListField =
            AccessTools.DeclaredField(s_WorkerActionsType, "m_Actions");

        private static readonly PropertyInfo s_WorkerActionsListLengthProperty =
            AccessTools.Property(s_WorkerActionsListField?.FieldType, "Length");

        private static readonly PropertyInfo s_WorkerActionsListIndexerProperty =
            AccessTools.Property(s_WorkerActionsListField?.FieldType, "Item");

        private static readonly FieldInfo s_WorkerActionTypeField =
            AccessTools.DeclaredField(s_WorkerActionType, "m_Type");

        private static Harmony s_Harmony;
        private static MethodInfo s_ScheduleWorkerJobsTarget;
        private static int s_LastLoggedScheduleRuntimeWorldGeneration = int.MinValue;

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
                if (s_ScheduleWorkerJobsTarget == null)
                {
                    Mod.log.Info(
                        "Schedule-handoff discovery skipped: ScheduleWorkerJobs target not found.");
                    return;
                }

                s_Harmony = new Harmony(HarmonyId);
                s_LastLoggedScheduleRuntimeWorldGeneration = int.MinValue;
                s_Harmony.Patch(
                    s_ScheduleWorkerJobsTarget,
                    transpiler: new HarmonyMethod(
                        typeof(PathfindRuntimeDiscoveryPatches),
                        nameof(ScheduleWorkerJobsTranspiler)));
                Mod.log.Info(
                    $"[SCHED-RAW] install target={DescribeMethod(s_ScheduleWorkerJobsTarget)}");
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                s_ScheduleWorkerJobsTarget = null;
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
            s_LastLoggedScheduleRuntimeWorldGeneration = int.MinValue;
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

        private static IEnumerable<CodeInstruction> ScheduleWorkerJobsTranspiler(
            IEnumerable<CodeInstruction> instructions)
        {
            bool inserted = false;

            foreach (CodeInstruction instruction in instructions)
            {
                if (!inserted && IsScheduleCall(instruction))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldind_Ref);
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

        private static void LogScheduleHandoff(object currentActions)
        {
            int runtimeWorldGeneration =
                EnforcementSaveDataSystem.RuntimeWorldGeneration;
            if (s_LastLoggedScheduleRuntimeWorldGeneration == runtimeWorldGeneration)
            {
                return;
            }

            s_LastLoggedScheduleRuntimeWorldGeneration = runtimeWorldGeneration;
            (bool hasAnyActions, bool hasPathfindWork, int workerActionCount, int pathfindActionCount) =
                InspectWorkerActions(currentActions);
            Mod.log.Info(
                $"[SCHED-RAW] runtimeWorldGeneration={runtimeWorldGeneration} firstHit=true " +
                $"hasPathfindWork={hasPathfindWork} hasAnyActions={hasAnyActions} workerActionCount={workerActionCount} " +
                $"pathfindActionCount={pathfindActionCount}");
        }

        private static (bool HasAnyActions, bool HasPathfindWork, int WorkerActionCount, int PathfindActionCount)
            InspectWorkerActions(object currentActions)
        {
            if (currentActions == null || s_WorkerActionsListField == null)
            {
                return (false, false, 0, 0);
            }

            object workerActionsList = s_WorkerActionsListField.GetValue(currentActions);
            int workerActionCount = GetWorkerActionCount(workerActionsList);
            bool hasAnyActions = workerActionCount > 0;
            int pathfindActionCount = hasAnyActions
                ? CountPathfindWork(workerActionsList, workerActionCount)
                : 0;
            bool hasPathfindWork = pathfindActionCount > 0;
            return (hasAnyActions, hasPathfindWork, workerActionCount, pathfindActionCount);
        }

        private static int GetWorkerActionCount(object workerActionsList)
        {
            if (workerActionsList == null || s_WorkerActionsListLengthProperty == null)
            {
                return 0;
            }

            object value = s_WorkerActionsListLengthProperty.GetValue(workerActionsList);
            return value is int count ? count : 0;
        }

        private static int CountPathfindWork(object workerActionsList, int workerActionCount)
        {
            if (workerActionsList == null ||
                workerActionCount <= 0 ||
                s_WorkerActionsListIndexerProperty == null ||
                s_WorkerActionTypeField == null)
            {
                return 0;
            }

            int pathfindActionCount = 0;
            for (int index = 0; index < workerActionCount; index += 1)
            {
                object workerAction =
                    s_WorkerActionsListIndexerProperty.GetValue(workerActionsList, new object[] { index });
                object actionType = workerAction == null ? null : s_WorkerActionTypeField.GetValue(workerAction);
                if (string.Equals(actionType?.ToString(), nameof(PathfindQueueSystem.ActionType.Pathfind), StringComparison.Ordinal))
                {
                    pathfindActionCount += 1;
                }
            }

            return pathfindActionCount;
        }
    }
}
