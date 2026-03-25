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

        private ComponentLookup<PathOwner> m_PathOwnerData;
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

            m_PathOwnerData = GetComponentLookup<PathOwner>(true);

            RequireForUpdate(m_PathOwnerQuery);
        }

        protected override void OnUpdate()
        {
            m_SuppressPendingBackfillForCurrentUpdate = false;
            HandleRuntimeWorldReload();
            m_PathOwnerData.Update(this);

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

            Mod.log.Info(
                $"[SAVELOAD] RouteAttemptTrackingSystem runtime reset: generation={currentGeneration}");
        }

        private void SeedMissingTrackingState()
        {
            if (m_MissingStateQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            NativeArray<Entity> vehicles =
                m_MissingStateQuery.ToEntityArray(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index += 1)
                {
                    Entity vehicle = vehicles[index];

                    if (!m_PathOwnerData.TryGetComponent(vehicle, out PathOwner pathOwner))
                    {
                        continue;
                    }

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
            finally
            {
                vehicles.Dispose();
            }
        }

        private void ProcessChangedPathOwners()
        {
            if (m_ChangedPathOwnerQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            NativeArray<Entity> vehicles =
                m_ChangedPathOwnerQuery.ToEntityArray(Allocator.Temp);
            NativeArray<RouteAttemptTrackingState> states =
                m_ChangedPathOwnerQuery.ToComponentDataArray<RouteAttemptTrackingState>(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index += 1)
                {
                    Entity vehicle = vehicles[index];
                    RouteAttemptTrackingState state = states[index];

                    if (!m_PathOwnerData.TryGetComponent(vehicle, out PathOwner pathOwner))
                    {
                        continue;
                    }

                    bool wasPending = state.m_WasPending != 0;
                    bool isPending = (pathOwner.m_State & PathFlags.Pending) != 0;

                    if (!wasPending && isPending)
                    {
                        EnforcementPolicyImpactService.RecordPathRequest(vehicle.Index);
                    }

                    byte updatedWasPending = (byte)(isPending ? 1 : 0);
                    if (state.m_WasPending != updatedWasPending)
                    {
                        state.m_WasPending = updatedWasPending;
                        EntityManager.SetComponentData(vehicle, state);
                    }
                }
            }
            finally
            {
                vehicles.Dispose();
                states.Dispose();
            }
        }
    }
}