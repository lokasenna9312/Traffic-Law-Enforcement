using Game;
using Game.Common;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    [BurstCompile]
    public partial struct VehicleLaneHistorySystem : ISystem
    {
        // Queries are now cached via system state
        private EntityQuery m_NewCarQuery;
        private EntityQuery m_ChangedLaneQuery;

        public void OnCreate(ref SystemState state)
        {
            m_NewCarQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Car>(),
                    ComponentType.ReadOnly<CarCurrentLane>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<VehicleLaneHistory>(),
                },
            });
            m_ChangedLaneQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Car>(),
                    ComponentType.ReadOnly<CarCurrentLane>(),
                    ComponentType.ReadWrite<VehicleLaneHistory>(),
                },
            });
            m_ChangedLaneQuery.SetChangedVersionFilter(ComponentType.ReadOnly<CarCurrentLane>());
            state.RequireForUpdate(m_NewCarQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var ownerLookup = state.GetComponentLookup<Owner>(true);
            // Initialization: Add VehicleLaneHistory to new cars
            new InitLaneHistoryJob
            {
                OwnerLookup = ownerLookup,
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(m_NewCarQuery);

            // Update: Update lane history for changed lanes
            new UpdateLaneHistoryJob
            {
                OwnerLookup = ownerLookup
            }.ScheduleParallel(m_ChangedLaneQuery);

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private struct InitLaneHistoryJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<Owner> OwnerLookup;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute([EntityIndexInQuery] int index, Entity entity, in Car car, in CarCurrentLane currentLane)
            {
                Entity lane = currentLane.m_Lane;
                Entity owner = Entity.Null;
                if (lane != Entity.Null && OwnerLookup.TryGetComponent(lane, out Owner laneOwner))
                    owner = laneOwner.m_Owner;

                var history = new VehicleLaneHistory
                {
                    m_PreviousLane = Entity.Null,
                    m_CurrentLane = lane,
                    m_PreviousLaneOwner = Entity.Null,
                    m_CurrentLaneOwner = owner,
                    m_LaneChangeCount = 0,
                };
                ECB.AddComponent(index, entity, history);
            }
        }

        [BurstCompile]
        private struct UpdateLaneHistoryJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<Owner> OwnerLookup;

            public void Execute(Entity entity, ref VehicleLaneHistory history, in CarCurrentLane currentLane)
            {
                Entity lane = currentLane.m_Lane;
                Entity owner = Entity.Null;
                if (lane != Entity.Null && OwnerLookup.TryGetComponent(lane, out Owner laneOwner))
                    owner = laneOwner.m_Owner;

                if (history.m_CurrentLane == lane)
                {
                    if (history.m_CurrentLaneOwner != owner)
                    {
                        history.m_CurrentLaneOwner = owner;
                    }
                    return;
                }

                history.m_PreviousLane = history.m_CurrentLane;
                history.m_PreviousLaneOwner = history.m_CurrentLaneOwner;
                history.m_CurrentLane = lane;
                history.m_CurrentLaneOwner = owner;
                history.m_LaneChangeCount += 1;
            }
        }
    }
}
