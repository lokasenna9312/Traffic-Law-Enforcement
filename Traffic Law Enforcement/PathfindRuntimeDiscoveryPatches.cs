using System;
using System.Reflection;
using System.Text;
using Game.Pathfind;
using HarmonyLib;
using Unity.Burst;
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
        private static MethodInfo s_PublicExecuteTarget;
        private static MethodInfo s_HelperExecuteTarget;
        private static bool s_LoggedPublicExecuteFirstHit;

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
                    Mod.log.Info("Execution-model validation skipped: PathfindWorkerJob type not found.");
                    return;
                }

                s_PublicExecuteTarget = FindPublicExecuteTarget();
                s_HelperExecuteTarget = FindHelperExecuteTarget();
                if (s_PublicExecuteTarget == null)
                {
                    Mod.log.Info(
                        "Execution-model validation skipped: public Execute target not found.");
                    return;
                }

                s_Harmony = new Harmony(HarmonyId);
                s_LoggedPublicExecuteFirstHit = false;
                s_Harmony.Patch(
                    s_PublicExecuteTarget,
                    prefix: new HarmonyMethod(
                        typeof(PathfindRuntimeDiscoveryPatches),
                        nameof(PublicExecutePrefix)));
                Mod.log.Info(
                    $"[EXEC-MODEL] install target={DescribeMethod(s_PublicExecuteTarget)} " +
                    $"helperTarget={DescribeMethod(s_HelperExecuteTarget)} " +
                    $"workerBurstCompile={HasBurstCompile(s_PathfindWorkerJobType)} " +
                    $"burstEnabled={BurstCompiler.IsEnabled}");
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                s_PublicExecuteTarget = null;
                s_HelperExecuteTarget = null;
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
            s_PublicExecuteTarget = null;
            s_HelperExecuteTarget = null;
            s_LoggedPublicExecuteFirstHit = false;
        }

        private static MethodInfo FindPublicExecuteTarget()
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(s_PathfindWorkerJobType))
            {
                if (IsPublicExecuteCandidate(method))
                {
                    return method;
                }
            }

            return null;
        }

        private static MethodInfo FindHelperExecuteTarget()
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(s_PathfindWorkerJobType))
            {
                if (IsHelperExecuteCandidate(method))
                {
                    return method;
                }
            }

            return null;
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

            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 0;
        }

        private static bool IsHelperExecuteCandidate(MethodInfo method)
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

        private static bool HasBurstCompile(MemberInfo member)
        {
            return member != null &&
                member.GetCustomAttributes(typeof(BurstCompileAttribute), inherit: false).Length != 0;
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

        private static void PublicExecutePrefix()
        {
            if (s_LoggedPublicExecuteFirstHit)
            {
                return;
            }

            s_LoggedPublicExecuteFirstHit = true;
            Mod.log.Info("[EXEC-MODEL] firstHit=true");
        }
    }
}
