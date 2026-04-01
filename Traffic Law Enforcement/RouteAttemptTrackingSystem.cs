using Game;
using Game.Pathfind;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public struct RouteAttemptTrackingState : IComponentData
    {
        public byte m_WasPending;
    }

    public partial class RouteAttemptTrackingSystem : GameSystemBase
    {
        private EntityQuery m_PathOwnerQuery;
        private EntityQuery m_MissingStateQuery;
        private EntityQuery m_ChangedPathOwnerQuery;
        private EntityQuery m_TrackingStateQuery;

        private EntityTypeHandle m_EntityTypeHandle;
        private ComponentTypeHandle<PathOwner> m_PathOwnerTypeHandle;
        private ComponentTypeHandle<RouteAttemptTrackingState> m_TrackingStateTypeHandle;
        private int m_LastObservedRuntimeWorldGeneration = -1;
        private bool m_SuppressPendingBackfillForCurrentUpdate;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_PathOwnerQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<PathOwner>());

            m_MissingStateQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Car>(),
                    ComponentType.ReadOnly<PathOwner>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<RouteAttemptTrackingState>(),
                },
            });

            m_ChangedPathOwnerQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<PathOwner>(),
                ComponentType.ReadWrite<RouteAttemptTrackingState>());

            m_ChangedPathOwnerQuery.SetChangedVersionFilter(
                ComponentType.ReadOnly<PathOwner>());

            m_TrackingStateQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<PathOwner>(),
                ComponentType.ReadOnly<RouteAttemptTrackingState>());

            RequireForUpdate(m_PathOwnerQuery);
        }

        protected override void OnUpdate()
        {
            m_SuppressPendingBackfillForCurrentUpdate = false;
            HandleRuntimeWorldReload();

            if (m_MissingStateQuery.IsEmptyIgnoreFilter &&
                m_ChangedPathOwnerQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            m_EntityTypeHandle = GetEntityTypeHandle();
            m_PathOwnerTypeHandle = GetComponentTypeHandle<PathOwner>(true);
            m_TrackingStateTypeHandle = GetComponentTypeHandle<RouteAttemptTrackingState>(true);

            SeedMissingTrackingState();
            ProcessChangedPathOwners();
        }

        private void HandleRuntimeWorldReload()
        {
            int currentGeneration = EnforcementSaveDataSystem.RuntimeWorldGeneration;
            if (m_LastObservedRuntimeWorldGeneration == currentGeneration)
            {
                return;
            }

            m_LastObservedRuntimeWorldGeneration = currentGeneration;
            m_SuppressPendingBackfillForCurrentUpdate = true;

            if (!m_TrackingStateQuery.IsEmptyIgnoreFilter)
            {
                EntityManager.RemoveComponent<RouteAttemptTrackingState>(m_TrackingStateQuery);
            }

        }

        private void SeedMissingTrackingState()
        {
            if (m_MissingStateQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            NativeArray<ArchetypeChunk> chunks =
                m_MissingStateQuery.ToArchetypeChunkArray(Allocator.Temp);
            try
            {
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex += 1)
                {
                    ArchetypeChunk chunk = chunks[chunkIndex];
                    NativeArray<Entity> vehicles = chunk.GetNativeArray(m_EntityTypeHandle);
                    NativeArray<PathOwner> pathOwners = chunk.GetNativeArray(ref m_PathOwnerTypeHandle);

                    for (int index = 0; index < vehicles.Length; index += 1)
                    {
                        Entity vehicle = vehicles[index];
                        PathOwner pathOwner = pathOwners[index];
                        bool isPending = (pathOwner.m_State & PathFlags.Pending) != 0;

                        if (isPending)
                        {
                            if (m_SuppressPendingBackfillForCurrentUpdate)
                            {
                                EnforcementPolicyImpactService.EnsureActivePathContext(vehicle.Index);
                            }
                            else
                            {
                                EnforcementPolicyImpactService.RecordPathRequest(vehicle.Index);
                                PublicTransportLaneExitPressureTelemetry.TryRecordSubsequentPathRequest(vehicle);
                            }
                        }

                        EntityManager.AddComponentData(
                            vehicle,
                            new RouteAttemptTrackingState
                            {
                                m_WasPending = (byte)(isPending ? 1 : 0),
                            });
                    }
                }
            }
            finally
            {
                chunks.Dispose();
            }
        }

        private void ProcessChangedPathOwners()
        {
            if (m_ChangedPathOwnerQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            NativeArray<ArchetypeChunk> chunks =
                m_ChangedPathOwnerQuery.ToArchetypeChunkArray(Allocator.Temp);
            try
            {
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex += 1)
                {
                    ArchetypeChunk chunk = chunks[chunkIndex];
                    NativeArray<Entity> vehicles = chunk.GetNativeArray(m_EntityTypeHandle);
                    NativeArray<PathOwner> pathOwners = chunk.GetNativeArray(ref m_PathOwnerTypeHandle);
                    NativeArray<RouteAttemptTrackingState> states =
                        chunk.GetNativeArray(ref m_TrackingStateTypeHandle);

                    for (int index = 0; index < vehicles.Length; index += 1)
                    {
                        Entity vehicle = vehicles[index];
                        PathOwner pathOwner = pathOwners[index];
                        RouteAttemptTrackingState state = states[index];
                        bool wasPending = state.m_WasPending != 0;
                        bool isPending = (pathOwner.m_State & PathFlags.Pending) != 0;

                        if (!wasPending && isPending)
                        {
                            EnforcementPolicyImpactService.RecordPathRequest(vehicle.Index);
                            PublicTransportLaneExitPressureTelemetry.TryRecordSubsequentPathRequest(vehicle);
                        }

                        byte updatedWasPending = (byte)(isPending ? 1 : 0);
                        if (state.m_WasPending != updatedWasPending)
                        {
                            state.m_WasPending = updatedWasPending;
                            EntityManager.SetComponentData(vehicle, state);
                        }
                    }
                }
            }
            finally
            {
                chunks.Dispose();
            }
        }
    }
}
