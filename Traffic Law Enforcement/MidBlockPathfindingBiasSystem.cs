using Game;
using Game.Prefabs;
using Game.Pathfind;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public static class MidBlockPathfindingBiasTelemetry
    {
        public static int ModifiedPrefabCount { get; private set; }
        public static string OverrideSummary { get; private set; } = "Waiting for car pathfinding prefabs.";

        public static void SetState(
            int modifiedPrefabCount,
            bool enabled,
            int midBlockMoneyPenalty)
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

            OverrideSummary =
                $"UnsafeUTurn money +{midBlockMoneyPenalty:0}; " +
                "illegal access ingress/egress penalties are applied per transition; " +
                "PT-lane route penalties are still handled per route rather than through shared PathfindCarData prefabs";
        }
    }

    public partial class MidBlockPathfindingBiasSystem : GameSystemBase
    {
        private EntityQuery m_PathfindCarDataQuery;
        private EntityQuery m_UncachedPathfindCarDataQuery;
        private EntityTypeHandle m_EntityTypeHandle;
        private ComponentLookup<OriginalPathfindCarData> m_OriginalPathfindCarDataLookup;
        private int m_LastMidBlockPenalty = int.MinValue;
        private bool m_LastEnforcementEnabled;
        private int m_LastObservedRuntimeWorldGeneration = -1;
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
            RequireForUpdate(m_PathfindCarDataQuery);
        }

        protected override void OnUpdate()
        {
            int currentGeneration = EnforcementSaveDataSystem.RuntimeWorldGeneration;
            bool runtimeReloaded = currentGeneration != m_LastObservedRuntimeWorldGeneration;
            if (runtimeReloaded)
            {
                m_LastObservedRuntimeWorldGeneration = currentGeneration;
                m_HasApplied = false;
            }

            bool enforcementEnabled = Mod.IsMidBlockCrossingEnforcementEnabled;
            int midBlockPenalty = enforcementEnabled
                ? EnforcementPenaltyService.GetMidBlockCrossingFine()
                : 0;
            bool hasUncachedPrefabs = !m_UncachedPathfindCarDataQuery.IsEmptyIgnoreFilter;

            bool needsApply =
                !m_HasApplied ||
                runtimeReloaded ||
                hasUncachedPrefabs ||
                enforcementEnabled != m_LastEnforcementEnabled ||
                midBlockPenalty != m_LastMidBlockPenalty;

            if (!needsApply)
            {
                return;
            }

            int prefabCount = m_PathfindCarDataQuery.CalculateEntityCount();
            m_OriginalPathfindCarDataLookup.Update(this);
            m_EntityTypeHandle = GetEntityTypeHandle();

            m_LastMidBlockPenalty = midBlockPenalty;
            m_LastEnforcementEnabled = enforcementEnabled;
            m_HasApplied = true;

            ApplyPathfindCarDataOverrides(midBlockPenalty);
            PublishOverrideTelemetry(
                prefabCount,
                enforcementEnabled,
                midBlockPenalty);
            TryLogOverrideSummary(prefabCount, enforcementEnabled);
        }

        private void ApplyPathfindCarDataOverrides(int midBlockPenalty)
        {
            NativeArray<ArchetypeChunk> chunks = m_PathfindCarDataQuery.ToArchetypeChunkArray(Allocator.Temp);
            try
            {
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex += 1)
                {
                    NativeArray<Entity> prefabs = chunks[chunkIndex].GetNativeArray(m_EntityTypeHandle);
                    for (int index = 0; index < prefabs.Length; index += 1)
                    {
                        ApplyPathfindCarDataOverride(prefabs[index], midBlockPenalty);
                    }
                }
            }
            finally
            {
                chunks.Dispose();
            }
        }

        private void ApplyPathfindCarDataOverride(
            Entity prefab,
            int midBlockMoneyPenalty)
        {
            PathfindCarData currentData = EntityManager.GetComponentData<PathfindCarData>(prefab);
            PathfindCarData originalData =
                GetOrCacheOriginalPathfindCarData(prefab, currentData);
            PathfindCarData overriddenData =
                BuildPathfindCarDataOverride(originalData, midBlockMoneyPenalty);
            WritePathfindCarDataOverride(prefab, currentData, overriddenData);
        }

        private static void PublishOverrideTelemetry(
            int prefabCount,
            bool enforcementEnabled,
            int midBlockPenalty)
        {
            MidBlockPathfindingBiasTelemetry.SetState(
                prefabCount,
                enforcementEnabled,
                midBlockPenalty);
        }

        private static void TryLogOverrideSummary(
            int prefabCount,
            bool enforcementEnabled)
        {
            if (!EnforcementLoggingPolicy.ShouldLogPathfindingPenaltyDiagnostics())
            {
                return;
            }

            Mod.log.Info(
                $"Applied mid-block pathfinding bias overrides: prefabs={prefabCount}, " +
                $"enabled={enforcementEnabled}, " +
                $"{MidBlockPathfindingBiasTelemetry.OverrideSummary}");
        }

        private PathfindCarData GetOrCacheOriginalPathfindCarData(
            Entity prefab,
            PathfindCarData currentData)
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

        private static PathfindCarData BuildPathfindCarDataOverride(
            PathfindCarData originalData,
            int midBlockMoneyPenalty)
        {
            PathfindCarData overriddenData = originalData;
            ApplyMidBlockMoneyBias(ref overriddenData, midBlockMoneyPenalty);
            return overriddenData;
        }

        private void WritePathfindCarDataOverride(
            Entity prefab,
            PathfindCarData currentData,
            PathfindCarData overriddenData)
        {
            if (!PathfindCarDataEquals(currentData, overriddenData))
            {
                EntityManager.SetComponentData(prefab, overriddenData);
            }
        }

        private static void ApplyMidBlockMoneyBias(
            ref PathfindCarData pathfindCarData,
            int midBlockMoneyPenalty)
        {
            AddMoneyPenalty(
                ref pathfindCarData.m_UnsafeUTurnCost,
                midBlockMoneyPenalty);
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
