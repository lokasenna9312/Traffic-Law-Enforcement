using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Game;
using Game.Pathfind;
using HarmonyLib;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    [HarmonyPatch]
    internal static class IntersectionMovementPathfindingPenaltyReflectionPatches
    {
        private const int MaxDiagnosticLogs = 32;
        private static readonly Type s_PathfindExecutorType = AccessTools.Inner(typeof(PathfindJobs), "PathfindExecutor");
        private static int s_DiagnosticLogCount;
        private static readonly HashSet<string> s_ActivatedMethods = new HashSet<string>();
        private const string HarmonyId = "Traffic_Law_Enforcement.IntersectionMovementPathfindingPenaltyReflectionPatches";
        private static Harmony s_Harmony;

        private readonly struct NamedEntity
        {
            public readonly string Name;
            public readonly Entity Value;

            public NamedEntity(string name, Entity value)
            {
                Name = name ?? string.Empty;
                Value = value;
            }
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            if (s_PathfindExecutorType == null)
            {
                yield break;
            }

            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(s_PathfindExecutorType))
            {
                if (method.IsStatic || method.ReturnType != typeof(float))
                {
                    continue;
                }

                if (string.Equals(method.Name, "CalculateCost", StringComparison.Ordinal) ||
                    method.Name.IndexOf("cost", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    yield return method;
                }
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
                s_Harmony.CreateClassProcessor(typeof(IntersectionMovementPathfindingPenaltyReflectionPatches)).Patch();
                Mod.log.Info("Intersection movement reflection fallback patches applied.");
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                Mod.log.Error(ex, "Failed to apply intersection movement reflection fallback patches.");
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
        }

        [HarmonyPostfix]
        private static void Postfix(object[] __args, ref float __result, PathfindParameters ___m_Parameters, MethodBase __originalMethod)
        {
            string methodKey = __originalMethod?.ToString() ?? "(null)";
            if (s_ActivatedMethods.Add(methodKey))
            {
                Mod.log.Info($"Intersection reflection fallback active: method={methodKey}, argCount={__args?.Length ?? 0}");
            }
            
            if (!Mod.IsIntersectionMovementEnforcementEnabled)
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
            if (!TryFindIllegalIntersectionPair(entityManager, __args, out Entity sourceLane, out Entity targetLane, out LaneMovement actualMovement, out LaneMovement allowedMovement))
            {
                return;
            }

            int penalty = EnforcementPenaltyService.GetIntersectionMovementFine();
            if (penalty <= 0)
            {
                return;
            }

            float addedCost = penalty * moneyWeight;
            __result += addedCost;

            if (EnforcementLoggingPolicy.ShouldLogPathfindingPenaltyDiagnostics() && s_DiagnosticLogCount < MaxDiagnosticLogs)
            {
                s_DiagnosticLogCount += 1;
                Mod.log.Info(
                    $"Intersection pre-penalty applied (reflection fallback): method={DescribeMethod(__originalMethod as MethodInfo)}, " +
                    $"sourceLane={sourceLane}, targetLane={targetLane}, " +
                    $"actual={IntersectionMovementPolicy.FormatMovement(actualMovement)}, " +
                    $"allowed={IntersectionMovementPolicy.FormatMovement(allowedMovement)}, " +
                    $"moneyWeight={moneyWeight:0.###}, addedCost={addedCost:0.###}");
            }
        }

        private static bool TryFindIllegalIntersectionPair(EntityManager entityManager, object[] args, out Entity sourceLane, out Entity targetLane, out LaneMovement actualMovement, out LaneMovement allowedMovement)
        {
            sourceLane = Entity.Null;
            targetLane = Entity.Null;
            actualMovement = LaneMovement.None;
            allowedMovement = LaneMovement.None;

            List<NamedEntity> entities = new List<NamedEntity>(16);
            HashSet<object> visited = new HashSet<object>();
            if (args != null)
            {
                for (int index = 0; index < args.Length; index += 1)
                {
                    visited.Clear();
                    CollectNamedEntities(args[index], $"arg{index}", 0, entities, visited);
                }
            }

            HashSet<ulong> seenPairs = new HashSet<ulong>();
            for (int sourceIndex = 0; sourceIndex < entities.Count; sourceIndex += 1)
            {
                if (!LooksLikeSource(entities[sourceIndex].Name))
                {
                    continue;
                }

                if (TryResolveIllegalPair(entityManager, entities, sourceIndex, seenPairs, true, out sourceLane, out targetLane, out actualMovement, out allowedMovement))
                {
                    return true;
                }
            }

            for (int sourceIndex = 0; sourceIndex < entities.Count; sourceIndex += 1)
            {
                if (TryResolveIllegalPair(entityManager, entities, sourceIndex, seenPairs, false, out sourceLane, out targetLane, out actualMovement, out allowedMovement))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeSource(string name)
        {
            string value = (name ?? string.Empty).ToLowerInvariant();
            return value.Contains("source") || value.Contains("from") || value.Contains("previous") || value.Contains("prev") || value.Contains("start");
        }

        private static bool LooksLikeTarget(string name)
        {
            string value = (name ?? string.Empty).ToLowerInvariant();
            return value.Contains("target") || value.Contains("to") || value.Contains("next") || value.Contains("connection") || value.Contains("end");
        }

        private static bool TryResolveIllegalPair(
            EntityManager entityManager,
            List<NamedEntity> entities,
            int sourceIndex,
            HashSet<ulong> seenPairs,
            bool requireTargetNameMatch,
            out Entity sourceLane,
            out Entity targetLane,
            out LaneMovement actualMovement,
            out LaneMovement allowedMovement)
        {
            sourceLane = entities[sourceIndex].Value;
            targetLane = Entity.Null;
            actualMovement = LaneMovement.None;
            allowedMovement = LaneMovement.None;

            if (sourceLane == Entity.Null)
            {
                return false;
            }

            for (int targetIndex = 0; targetIndex < entities.Count; targetIndex += 1)
            {
                if (sourceIndex == targetIndex)
                {
                    continue;
                }

                if (requireTargetNameMatch && !LooksLikeTarget(entities[targetIndex].Name))
                {
                    continue;
                }

                Entity candidateTarget = entities[targetIndex].Value;
                if (candidateTarget == Entity.Null || candidateTarget == sourceLane)
                {
                    continue;
                }

                ulong pairKey = (((ulong)(uint)sourceLane.Index) << 32) ^ (uint)candidateTarget.Index;
                if (!seenPairs.Add(pairKey))
                {
                    continue;
                }

                if (IntersectionMovementPolicy.TryGetIllegalIntersectionMovement(
                        entityManager,
                        sourceLane,
                        candidateTarget,
                        out actualMovement,
                        out allowedMovement))
                {
                    targetLane = candidateTarget;
                    return true;
                }
            }

            return false;
        }

        private static void CollectNamedEntities(object value, string path, int depth, List<NamedEntity> entities, HashSet<object> visited)
        {
            if (value == null || depth > 4)
            {
                return;
            }

            if (value is Entity entityValue)
            {
                if (entityValue != Entity.Null)
                {
                    entities.Add(new NamedEntity(path, entityValue));
                }

                return;
            }

            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(string))
            {
                return;
            }

            if (!type.IsValueType)
            {
                if (!visited.Add(value))
                {
                    return;
                }
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object fieldValue;
                try
                {
                    fieldValue = field.GetValue(value);
                }
                catch
                {
                    continue;
                }

                string fieldPath = string.IsNullOrEmpty(path) ? field.Name : path + "." + field.Name;
                CollectNamedEntities(fieldValue, fieldPath, depth + 1, entities, visited);
            }
        }

        private static string DescribeMethod(MethodInfo method)
        {
            if (method == null)
            {
                return "null";
            }

            ParameterInfo[] parameters = method.GetParameters();
            StringBuilder signature = new StringBuilder(96);
            signature.Append(method.DeclaringType?.FullName);
            signature.Append('.');
            signature.Append(method.Name);
            signature.Append('(');
            for (int index = 0; index < parameters.Length; index += 1)
            {
                if (index > 0)
                {
                    signature.Append(", ");
                }

                signature.Append(parameters[index].ParameterType.Name);
                signature.Append(' ');
                signature.Append(parameters[index].Name);
            }

            signature.Append(')');
            return signature.ToString();
        }
    }
}
