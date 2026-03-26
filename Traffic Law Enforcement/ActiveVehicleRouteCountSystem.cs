using Game;
using Game.Vehicles;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public partial class ActiveVehicleRouteCountSystem : GameSystemBase
    {
        private EntityQuery m_ActiveVehicleRouteQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ActiveVehicleRouteQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>());
        }

        protected override void OnUpdate()
        {
        }

        public int GetActiveVehicleRouteCount()
        {
            return m_ActiveVehicleRouteQuery.CalculateEntityCount();
        }
    }
}