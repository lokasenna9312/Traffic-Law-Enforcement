using System;
using System.Reflection;
using System.Text;
using Game.Pathfind;
using HarmonyLib;
using Unity.Entities;
using Unity.Mathematics;

namespace Traffic_Law_Enforcement
{
    internal static class PathfindRuntimeDiscoveryPatches
    {
        private const string HarmonyId =
            "Traffic_Law_Enforcement.PathfindRuntimeDiscoveryPatches";

        private static readonly Type s_PathfindExecutorType =
            AccessTools.Inner(typeof(PathfindJobs), "PathfindExecutor");

        private static Harmony s_Harmony;
        private static MethodInfo s_AddConnectionsTarget;
        private static MethodInfo s_DisallowConnectionTarget;
        private static bool s_LoggedAddConnectionsFirstHit;
        private static bool s_LoggedDisallowConnectionFirstHit;

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
                    Mod.log.Info("Runtime-path discovery skipped: PathfindExecutor type not found.");
                    return;
                }

                s_AddConnectionsTarget = FindAddConnectionsTarget();
                s_DisallowConnectionTarget = FindDisallowConnectionTarget();
                if (s_AddConnectionsTarget == null && s_DisallowConnectionTarget == null)
                {
                    Mod.log.Info(
                        "Runtime-path discovery skipped: AddConnections and DisallowConnection targets not found.");
                    return;
                }

                s_Harmony = new Harmony(HarmonyId);
                s_LoggedAddConnectionsFirstHit = false;
                s_LoggedDisallowConnectionFirstHit = false;

                if (s_AddConnectionsTarget != null)
                {
                    s_Harmony.Patch(
                        s_AddConnectionsTarget,
                        prefix: new HarmonyMethod(
                            typeof(PathfindRuntimeDiscoveryPatches),
                            nameof(AddConnectionsPrefix)));
                    Mod.log.Info($"[AC-RAW] install target={DescribeMethod(s_AddConnectionsTarget)}");
                }

                if (s_DisallowConnectionTarget != null)
                {
                    s_Harmony.Patch(
                        s_DisallowConnectionTarget,
                        prefix: new HarmonyMethod(
                            typeof(PathfindRuntimeDiscoveryPatches),
                            nameof(DisallowConnectionPrefix)));
                    Mod.log.Info($"[DC-RAW] install target={DescribeMethod(s_DisallowConnectionTarget)}");
                }
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                s_AddConnectionsTarget = null;
                s_DisallowConnectionTarget = null;
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
            s_AddConnectionsTarget = null;
            s_DisallowConnectionTarget = null;
            s_LoggedAddConnectionsFirstHit = false;
            s_LoggedDisallowConnectionFirstHit = false;
        }

        private static MethodInfo FindAddConnectionsTarget()
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(s_PathfindExecutorType))
            {
                if (IsAddConnectionsCandidate(method))
                {
                    return method;
                }
            }

            return null;
        }

        private static MethodInfo FindDisallowConnectionTarget()
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(s_PathfindExecutorType))
            {
                if (IsDisallowConnectionCandidate(method))
                {
                    return method;
                }
            }

            return null;
        }

        private static bool IsAddConnectionsCandidate(MethodInfo method)
        {
            if (method == null ||
                method.IsStatic ||
                method.ReturnType != typeof(void) ||
                !string.Equals(method.Name, "AddConnections", StringComparison.Ordinal))
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
                parameters[2].ParameterType == typeof(Edge).MakeByRefType() &&
                parameters[3].ParameterType == typeof(EdgeFlags) &&
                parameters[4].ParameterType == typeof(RuleFlags) &&
                parameters[5].ParameterType == typeof(float) &&
                parameters[6].ParameterType == typeof(float) &&
                parameters[7].ParameterType == typeof(bool3) &&
                parameters[8].ParameterType == typeof(bool) &&
                parameters[9].ParameterType == typeof(bool) &&
                parameters[10].ParameterType == typeof(bool);
        }

        private static bool IsDisallowConnectionCandidate(MethodInfo method)
        {
            if (method == null ||
                method.IsStatic ||
                method.ReturnType != typeof(bool) ||
                !string.Equals(method.Name, "DisallowConnection", StringComparison.Ordinal))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 5)
            {
                return false;
            }

            return parameters[0].ParameterType == typeof(PathMethod) &&
                parameters[1].ParameterType.Name == "PathfindItemFlags" &&
                parameters[2].ParameterType == typeof(PathSpecification).MakeByRefType() &&
                parameters[3].ParameterType == typeof(EdgeFlags).MakeByRefType() &&
                parameters[4].ParameterType == typeof(Entity);
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

        private static void AddConnectionsPrefix()
        {
            if (s_LoggedAddConnectionsFirstHit)
            {
                return;
            }

            s_LoggedAddConnectionsFirstHit = true;
            Mod.log.Info("[AC-RAW] firstHit=true");
        }

        private static void DisallowConnectionPrefix()
        {
            if (s_LoggedDisallowConnectionFirstHit)
            {
                return;
            }

            s_LoggedDisallowConnectionFirstHit = true;
            Mod.log.Info("[DC-RAW] firstHit=true");
        }
    }
}
