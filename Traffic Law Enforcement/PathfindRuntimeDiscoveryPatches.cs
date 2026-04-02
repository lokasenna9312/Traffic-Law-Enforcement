using System;
using System.Reflection;
using System.Text;
using Game.Pathfind;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    internal static class PathfindRuntimeDiscoveryPatches
    {
        private const string HarmonyId =
            "Traffic_Law_Enforcement.PathfindRuntimeDiscoveryPatches";

        private static readonly Type s_PathfindWorkerJobType =
            AccessTools.Inner(typeof(PathfindQueueSystem), "PathfindWorkerJob");

        private static Harmony s_Harmony;
        private static MethodInfo s_ExecuteTarget;
        private static bool s_LoggedExecuteFirstHit;

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            try
            {
                if (s_PathfindWorkerJobType == null)
                {
                    Mod.log.Info("Runtime-path discovery skipped: PathfindWorkerJob type not found.");
                    return;
                }

                s_ExecuteTarget = FindExecuteTarget();
                if (s_ExecuteTarget == null)
                {
                    Mod.log.Info(
                        "Runtime-path discovery skipped: Execute target not found.");
                    return;
                }

                s_Harmony = new Harmony(HarmonyId);
                s_LoggedExecuteFirstHit = false;
                s_Harmony.Patch(
                    s_ExecuteTarget,
                    prefix: new HarmonyMethod(
                        typeof(PathfindRuntimeDiscoveryPatches),
                        nameof(ExecutePrefix)));
                Mod.log.Info($"[EXEC-RAW] install target={DescribeMethod(s_ExecuteTarget)}");
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                s_ExecuteTarget = null;
                Mod.log.Error(ex, "Failed to apply runtime-path discovery patches.");
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
            s_ExecuteTarget = null;
            s_LoggedExecuteFirstHit = false;
        }

        private static MethodInfo FindExecuteTarget()
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(s_PathfindWorkerJobType))
            {
                if (IsExecuteCandidate(method))
                {
                    return method;
                }
            }

            return null;
        }

        private static bool IsExecuteCandidate(MethodInfo method)
        {
            if (method == null ||
                method.IsStatic ||
                method.ReturnType != typeof(void) ||
                !string.Equals(method.Name, "Execute", StringComparison.Ordinal))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 3)
            {
                return false;
            }

            return parameters[0].ParameterType == typeof(PathfindActionData).MakeByRefType() &&
                parameters[1].ParameterType == typeof(int) &&
                parameters[2].ParameterType == typeof(Allocator);
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

        private static void ExecutePrefix()
        {
            if (s_LoggedExecuteFirstHit)
            {
                return;
            }

            s_LoggedExecuteFirstHit = true;
            Mod.log.Info("[EXEC-RAW] firstHit=true");
        }
    }
}
