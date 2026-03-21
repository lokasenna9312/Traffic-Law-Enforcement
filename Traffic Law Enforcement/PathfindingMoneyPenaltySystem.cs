using Game;
using Game.Pathfind;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public static class PathfindingPenaltyTelemetry
    {
        public static int ModifiedPrefabCount { get; private set; }
        public static string OverrideSummary { get; private set; } = "Waiting for car pathfinding prefabs.";

        public static void SetState(int modifiedPrefabCount, bool enabled, float midBlockPenalty, float intersectionPenalty)
        {
            ModifiedPrefabCount = modifiedPrefabCount;

            if (modifiedPrefabCount == 0)
            {
                OverrideSummary = "No car pathfinding prefabs found.";
                return;
            }

            if (!enabled)
            {
                OverrideSummary = "Enforcement disabled; pathfinding costs restored to base values.";
                return;
            }

            OverrideSummary = $"UTurn +{midBlockPenalty:0}, UnsafeUTurn +{midBlockPenalty:0}, LaneCross +{midBlockPenalty:0}, UnsafeTurning +{intersectionPenalty:0}, Forbidden intersection +{intersectionPenalty:0}; bus-lane money-axis penalty is handled per route rather than through shared PathfindCarData prefabs";
        }
    }

    public partial class PathfindingMoneyPenaltySystem : GameSystemBase
    {
        private EntityQuery m_PathfindCarDataQuery;
        private EntityQuery m_UncachedPathfindCarDataQuery;
        private ComponentLookup<OriginalPathfindCarData> m_OriginalPathfindCarDataLookup;
        private PrefabSystem m_PrefabSystem;
        private int m_LastMidBlockPenalty = int.MinValue;
        private int m_LastIntersectionPenalty = int.MinValue;
        private int m_LastPrefabCount = -1;
        private bool m_LastEnforcementEnabled;
        private bool m_HasApplied;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_PathfindCarDataQuery = GetEntityQuery(ComponentType.ReadWrite<PathfindCarData>());
            m_UncachedPathfindCarDataQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<PathfindCarData>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<OriginalPathfindCarData>(),
                },
            });
            m_OriginalPathfindCarDataLookup = GetComponentLookup<OriginalPathfindCarData>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            RequireForUpdate(m_PathfindCarDataQuery);
        }

        protected override void OnUpdate()
        {
            m_OriginalPathfindCarDataLookup.Update(this);

            int prefabCount = m_PathfindCarDataQuery.CalculateEntityCount();
            bool enforcementEnabled = Mod.IsMidBlockCrossingEnforcementEnabled || Mod.IsIntersectionMovementEnforcementEnabled;
            int midBlockPenalty = Mod.IsMidBlockCrossingEnforcementEnabled ? EnforcementPenaltyService.GetMidBlockCrossingFine() : 0;
            int intersectionPenalty = Mod.IsIntersectionMovementEnforcementEnabled ? EnforcementPenaltyService.GetIntersectionMovementFine() : 0;

            bool needsApply =
                !m_HasApplied ||
                prefabCount != m_LastPrefabCount ||
                !m_UncachedPathfindCarDataQuery.IsEmptyIgnoreFilter ||
                enforcementEnabled != m_LastEnforcementEnabled ||
                midBlockPenalty != m_LastMidBlockPenalty ||
                intersectionPenalty != m_LastIntersectionPenalty;

            if (!needsApply)
            {
                return;
            }

            ApplyOverrides(midBlockPenalty, intersectionPenalty);

            m_LastMidBlockPenalty = midBlockPenalty;
            m_LastIntersectionPenalty = intersectionPenalty;
            m_LastPrefabCount = prefabCount;
            m_LastEnforcementEnabled = enforcementEnabled;
            m_HasApplied = true;

            PathfindingPenaltyTelemetry.SetState(prefabCount, enforcementEnabled, midBlockPenalty, intersectionPenalty);
            if (EnforcementLoggingPolicy.ShouldLogPathfindingPenaltyDiagnostics())
            {
                Mod.log.Info($"Applied pathfinding money-axis penalties: prefabs={prefabCount}, enabled={enforcementEnabled}, {PathfindingPenaltyTelemetry.OverrideSummary}");
                LogSharedPathfindPrefabDiagnostics();
            }
        }

        private void ApplyOverrides(float midBlockPenalty, float intersectionPenalty)
        {
            NativeArray<Entity> prefabs = m_PathfindCarDataQuery.ToEntityArray(Allocator.Temp);

            try
            {
                for (int index = 0; index < prefabs.Length; index++)
                {
                    ApplyOverrides(prefabs[index], midBlockPenalty, intersectionPenalty);
                }
            }
            finally
            {
                prefabs.Dispose();
            }
        }

        private void ApplyOverrides(Entity prefab, float midBlockPenalty, float intersectionPenalty)
        {
            PathfindCarData currentData = EntityManager.GetComponentData<PathfindCarData>(prefab);
            PathfindCarData originalData = GetOriginalData(prefab, currentData);
            PathfindCarData updatedData = originalData;

            AddMoneyPenalty(ref updatedData.m_UTurnCost, midBlockPenalty);
            AddMoneyPenalty(ref updatedData.m_UnsafeUTurnCost, midBlockPenalty);
            AddMoneyPenalty(ref updatedData.m_LaneCrossCost, midBlockPenalty);
            AddMoneyPenalty(ref updatedData.m_UnsafeTurningCost, intersectionPenalty);
            AddMoneyPenalty(ref updatedData.m_ForbiddenCost, intersectionPenalty);

            if (!PathfindCarDataEquals(currentData, updatedData))
            {
                EntityManager.SetComponentData(prefab, updatedData);
            }
        }

        private PathfindCarData GetOriginalData(Entity prefab, PathfindCarData currentData)
        {
            if (m_OriginalPathfindCarDataLookup.TryGetComponent(prefab, out OriginalPathfindCarData originalData))
            {
                return originalData.m_Value;
            }

            EntityManager.AddComponentData(prefab, new OriginalPathfindCarData
            {
                m_Value = currentData,
            });

            return currentData;
        }

        private void LogSharedPathfindPrefabDiagnostics()
        {
            NativeArray<Entity> prefabs = m_PathfindCarDataQuery.ToEntityArray(Allocator.Temp);

            try
            {
                for (int index = 0; index < prefabs.Length; index++)
                {
                    Entity prefab = prefabs[index];
                    PathfindCarData currentData = EntityManager.GetComponentData<PathfindCarData>(prefab);
                    PathfindCarData originalData = m_OriginalPathfindCarDataLookup.TryGetComponent(prefab, out OriginalPathfindCarData originalComponent)
                        ? originalComponent.m_Value
                        : currentData;

                    string prefabName = m_PrefabSystem != null ? m_PrefabSystem.GetPrefabName(prefab) : prefab.ToString();
                }
            }
            finally
            {
                prefabs.Dispose();
            }
        }

        private static void AddMoneyPenalty(ref PathfindCosts cost, float penalty)
        {
            cost.m_Value.z += penalty;
        }

        private static bool PathfindCarDataEquals(PathfindCarData left, PathfindCarData right)
        {
            return PathfindCostsEquals(left.m_DrivingCost, right.m_DrivingCost) &&
                PathfindCostsEquals(left.m_TurningCost, right.m_TurningCost) &&
                PathfindCostsEquals(left.m_UnsafeTurningCost, right.m_UnsafeTurningCost) &&
                PathfindCostsEquals(left.m_UTurnCost, right.m_UTurnCost) &&
                PathfindCostsEquals(left.m_UnsafeUTurnCost, right.m_UnsafeUTurnCost) &&
                PathfindCostsEquals(left.m_CurveAngleCost, right.m_CurveAngleCost) &&
                PathfindCostsEquals(left.m_LaneCrossCost, right.m_LaneCrossCost) &&
                PathfindCostsEquals(left.m_ParkingCost, right.m_ParkingCost) &&
                PathfindCostsEquals(left.m_SpawnCost, right.m_SpawnCost) &&
                PathfindCostsEquals(left.m_ForbiddenCost, right.m_ForbiddenCost);
        }

        private static bool PathfindCostsEquals(PathfindCosts left, PathfindCosts right)
        {
            return math.all(left.m_Value == right.m_Value);
        }
    }
}
