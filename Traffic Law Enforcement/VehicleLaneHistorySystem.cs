using Game;
using Game.Common;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public partial class VehicleLaneHistorySystem : GameSystemBase
    {
        private EntityQuery m_CarQuery;
        private EntityQuery m_NewCarQuery;
        private EntityQuery m_ChangedLaneQuery;
        private ComponentLookup<Owner> m_OwnerData;
        private ComponentLookup<VehicleLaneHistory> m_HistoryData;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CarQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>());
            m_NewCarQuery = GetEntityQuery(new EntityQueryDesc
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
            m_ChangedLaneQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadWrite<VehicleLaneHistory>());
            m_ChangedLaneQuery.SetChangedVersionFilter(ComponentType.ReadOnly<CarCurrentLane>());
            m_OwnerData = GetComponentLookup<Owner>(true);
            m_HistoryData = GetComponentLookup<VehicleLaneHistory>();
            RequireForUpdate(m_CarQuery);
        }

        protected override void OnUpdate()
        {
            m_OwnerData.Update(this);
            m_HistoryData.Update(this);

            ProcessQuery(m_NewCarQuery);
            ProcessQuery(m_ChangedLaneQuery);
        }

        private void ProcessQuery(EntityQuery query)
        {
            NativeArray<Entity> vehicles = query.ToEntityArray(Allocator.Temp);
            NativeArray<CarCurrentLane> currentLanes = query.ToComponentDataArray<CarCurrentLane>(Allocator.Temp);

            try
            {
                for (int index = 0; index < vehicles.Length; index++)
                {
                    UpdateHistory(vehicles[index], currentLanes[index]);
                }
            }
            finally
            {
                vehicles.Dispose();
                currentLanes.Dispose();
            }
        }

        private void UpdateHistory(Entity vehicle, CarCurrentLane currentLane)
        {
            Entity currentLaneEntity = currentLane.m_Lane;
            Entity currentLaneOwner = GetOwner(currentLaneEntity);

            if (!m_HistoryData.TryGetComponent(vehicle, out VehicleLaneHistory history))
            {
                history = new VehicleLaneHistory
                {
                    m_PreviousLane = Entity.Null,
                    m_CurrentLane = currentLaneEntity,
                    m_PreviousLaneOwner = Entity.Null,
                    m_CurrentLaneOwner = currentLaneOwner,
                    m_LaneChangeCount = 0,
                };
                EntityManager.AddComponentData(vehicle, history);
                return;
            }

            if (history.m_CurrentLane == currentLaneEntity)
            {
                if (history.m_CurrentLaneOwner != currentLaneOwner)
                {
                    history.m_CurrentLaneOwner = currentLaneOwner;
                    EntityManager.SetComponentData(vehicle, history);
                }

                return;
            }

            history.m_PreviousLane = history.m_CurrentLane;
            history.m_PreviousLaneOwner = history.m_CurrentLaneOwner;
            history.m_CurrentLane = currentLaneEntity;
            history.m_CurrentLaneOwner = currentLaneOwner;
            history.m_LaneChangeCount += 1;
            EntityManager.SetComponentData(vehicle, history);
        }

        private Entity GetOwner(Entity lane)
        {
            if (lane != Entity.Null && m_OwnerData.TryGetComponent(lane, out Owner owner))
            {
                return owner.m_Owner;
            }

            return Entity.Null;
        }
    }
}
