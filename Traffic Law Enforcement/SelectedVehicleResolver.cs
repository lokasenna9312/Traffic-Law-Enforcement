using Game;
using Game.UI.InGame;
using Game.Vehicles;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public enum SelectedVehicleResolveState
    {
        None,
        NotVehicle,
        VehicleNotSupported,
        ParkedVehicle,
        RoadCarNoLaneData,
        Ready,
    }

    public readonly struct SelectedVehicleResolveResult
    {
        public readonly SelectedVehicleResolveState ResolveState;
        public readonly Entity SourceSelectedEntity;
        public readonly Entity ResolvedVehicleEntity;
        public readonly bool HasSelection;
        public readonly bool IsVehicle;
        public readonly bool IsCar;
        public readonly bool IsParked;
        public readonly bool IsTrailerChild;
        public readonly bool HasCarCurrentLane;

        public SelectedVehicleResolveResult(
            SelectedVehicleResolveState resolveState,
            Entity sourceSelectedEntity,
            Entity resolvedVehicleEntity,
            bool hasSelection,
            bool isVehicle,
            bool isCar,
            bool isParked,
            bool isTrailerChild,
            bool hasCarCurrentLane)
        {
            ResolveState = resolveState;
            SourceSelectedEntity = sourceSelectedEntity;
            ResolvedVehicleEntity = resolvedVehicleEntity;
            HasSelection = hasSelection;
            IsVehicle = isVehicle;
            IsCar = isCar;
            IsParked = isParked;
            IsTrailerChild = isTrailerChild;
            HasCarCurrentLane = hasCarCurrentLane;
        }
    }

    internal sealed class SelectedVehicleResolver
    {
        private const int kMaxControllerDepth = 16;

        private readonly EntityManager m_EntityManager;
        private readonly World m_World;

        private SelectedInfoUISystem m_SelectedInfoSystem;

        private ComponentLookup<Controller> m_ControllerData;
        private ComponentLookup<Vehicle> m_VehicleData;
        private ComponentLookup<Car> m_CarData;
        private ComponentLookup<ParkedCar> m_ParkedCarData;
        private ComponentLookup<CarCurrentLane> m_CarCurrentLaneData;

        public SelectedVehicleResolver(GameSystemBase system)
        {
            m_EntityManager = system.EntityManager;
            m_World = system.World;
            m_SelectedInfoSystem =
                system.World.GetExistingSystemManaged<SelectedInfoUISystem>();

            m_ControllerData = system.GetComponentLookup<Controller>(true);
            m_VehicleData = system.GetComponentLookup<Vehicle>(true);
            m_CarData = system.GetComponentLookup<Car>(true);
            m_ParkedCarData = system.GetComponentLookup<ParkedCar>(true);
            m_CarCurrentLaneData = system.GetComponentLookup<CarCurrentLane>(true);
        }

        internal void Update(GameSystemBase system)
        {
            m_ControllerData.Update(system);
            m_VehicleData.Update(system);
            m_CarData.Update(system);
            m_ParkedCarData.Update(system);
            m_CarCurrentLaneData.Update(system);
        }

        public SelectedVehicleResolveResult ResolveCurrentSelection()
        {
            if (m_SelectedInfoSystem == null)
            {
                m_SelectedInfoSystem =
                    m_World.GetExistingSystemManaged<SelectedInfoUISystem>();
            }

            Entity sourceSelectedEntity = m_SelectedInfoSystem != null
                ? m_SelectedInfoSystem.selectedEntity
                : Entity.Null;

            if (sourceSelectedEntity == Entity.Null)
            {
                return new SelectedVehicleResolveResult(
                    SelectedVehicleResolveState.None,
                    sourceSelectedEntity,
                    Entity.Null,
                    hasSelection: false,
                    isVehicle: false,
                    isCar: false,
                    isParked: false,
                    isTrailerChild: false,
                    hasCarCurrentLane: false);
            }

            Entity resolvedVehicleEntity = ResolveControllerRoot(sourceSelectedEntity);
            bool isTrailerChild =
                resolvedVehicleEntity != Entity.Null &&
                resolvedVehicleEntity != sourceSelectedEntity;

            bool isVehicle =
                resolvedVehicleEntity != Entity.Null &&
                EntityExists(resolvedVehicleEntity) &&
                m_VehicleData.HasComponent(resolvedVehicleEntity);

            bool isCar =
                isVehicle &&
                m_CarData.HasComponent(resolvedVehicleEntity);

            bool isParked =
                isVehicle &&
                m_ParkedCarData.HasComponent(resolvedVehicleEntity);

            bool hasCarCurrentLane =
                isCar &&
                m_CarCurrentLaneData.HasComponent(resolvedVehicleEntity);

            SelectedVehicleResolveState resolveState;
            if (!isVehicle)
            {
                resolveState = SelectedVehicleResolveState.NotVehicle;
            }
            else if (isParked)
            {
                resolveState = SelectedVehicleResolveState.ParkedVehicle;
            }
            else if (!isCar)
            {
                resolveState = SelectedVehicleResolveState.VehicleNotSupported;
            }
            else if (!hasCarCurrentLane)
            {
                resolveState = SelectedVehicleResolveState.RoadCarNoLaneData;
            }
            else
            {
                resolveState = SelectedVehicleResolveState.Ready;
            }

            return new SelectedVehicleResolveResult(
                resolveState,
                sourceSelectedEntity,
                resolvedVehicleEntity,
                hasSelection: true,
                isVehicle,
                isCar,
                isParked,
                isTrailerChild,
                hasCarCurrentLane);
        }

        private Entity ResolveControllerRoot(Entity entity)
        {
            if (!EntityExists(entity))
            {
                return Entity.Null;
            }

            Entity current = entity;

            for (int depth = 0; depth < kMaxControllerDepth; depth += 1)
            {
                if (!m_ControllerData.TryGetComponent(current, out Controller controller) ||
                    controller.m_Controller == Entity.Null ||
                    controller.m_Controller == current ||
                    !EntityExists(controller.m_Controller))
                {
                    return current;
                }

                current = controller.m_Controller;
            }

            return current;
        }

        private bool EntityExists(Entity entity)
        {
            return entity != Entity.Null && m_EntityManager.Exists(entity);
        }
    }
}
