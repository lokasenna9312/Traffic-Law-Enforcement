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
        Vehicle,
    }

    public enum SelectedVehicleKind
    {
        None,
        RoadCar,
        ParkedRoadCar,
        Train,
        ParkedTrain,
        OtherVehicle,
    }

    public readonly struct SelectedVehicleResolveResult
    {
        public readonly SelectedVehicleResolveState ResolveState;
        public readonly SelectedVehicleKind VehicleKind;
        public readonly Entity SourceSelectedEntity;
        public readonly Entity ResolvedVehicleEntity;
        public readonly bool HasSelection;
        public readonly bool IsVehicle;
        public readonly bool IsCar;
        public readonly bool IsTrain;
        public readonly bool IsParked;
        public readonly bool IsTrailerChild;
        public readonly bool HasCarCurrentLane;
        public readonly bool HasTrainCurrentLane;
        public readonly bool HasLiveLaneData;

        public SelectedVehicleResolveResult(
            SelectedVehicleResolveState resolveState,
            SelectedVehicleKind vehicleKind,
            Entity sourceSelectedEntity,
            Entity resolvedVehicleEntity,
            bool hasSelection,
            bool isVehicle,
            bool isCar,
            bool isTrain,
            bool isParked,
            bool isTrailerChild,
            bool hasCarCurrentLane,
            bool hasTrainCurrentLane,
            bool hasLiveLaneData)
        {
            ResolveState = resolveState;
            VehicleKind = vehicleKind;
            SourceSelectedEntity = sourceSelectedEntity;
            ResolvedVehicleEntity = resolvedVehicleEntity;
            HasSelection = hasSelection;
            IsVehicle = isVehicle;
            IsCar = isCar;
            IsTrain = isTrain;
            IsParked = isParked;
            IsTrailerChild = isTrailerChild;
            HasCarCurrentLane = hasCarCurrentLane;
            HasTrainCurrentLane = hasTrainCurrentLane;
            HasLiveLaneData = hasLiveLaneData;
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
        private ComponentLookup<Train> m_TrainData;
        private ComponentLookup<ParkedCar> m_ParkedCarData;
        private ComponentLookup<ParkedTrain> m_ParkedTrainData;
        private ComponentLookup<CarCurrentLane> m_CarCurrentLaneData;
        private ComponentLookup<TrainCurrentLane> m_TrainCurrentLaneData;

        public SelectedVehicleResolver(GameSystemBase system)
        {
            m_EntityManager = system.EntityManager;
            m_World = system.World;
            m_SelectedInfoSystem =
                system.World.GetExistingSystemManaged<SelectedInfoUISystem>();

            m_ControllerData = system.GetComponentLookup<Controller>(true);
            m_VehicleData = system.GetComponentLookup<Vehicle>(true);
            m_CarData = system.GetComponentLookup<Car>(true);
            m_TrainData = system.GetComponentLookup<Train>(true);
            m_ParkedCarData = system.GetComponentLookup<ParkedCar>(true);
            m_ParkedTrainData = system.GetComponentLookup<ParkedTrain>(true);
            m_CarCurrentLaneData = system.GetComponentLookup<CarCurrentLane>(true);
            m_TrainCurrentLaneData = system.GetComponentLookup<TrainCurrentLane>(true);
        }

        internal void Update(GameSystemBase system)
        {
            m_ControllerData.Update(system);
            m_VehicleData.Update(system);
            m_CarData.Update(system);
            m_TrainData.Update(system);
            m_ParkedCarData.Update(system);
            m_ParkedTrainData.Update(system);
            m_CarCurrentLaneData.Update(system);
            m_TrainCurrentLaneData.Update(system);
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
                    SelectedVehicleKind.None,
                    sourceSelectedEntity,
                    Entity.Null,
                    hasSelection: false,
                    isVehicle: false,
                    isCar: false,
                    isTrain: false,
                    isParked: false,
                    isTrailerChild: false,
                    hasCarCurrentLane: false,
                    hasTrainCurrentLane: false,
                    hasLiveLaneData: false);
            }

            Entity resolvedVehicleEntity = ResolveControllerRoot(sourceSelectedEntity);
            bool isTrailerChild =
                resolvedVehicleEntity != Entity.Null &&
                resolvedVehicleEntity != sourceSelectedEntity;

            bool hasVehicleMarker =
                resolvedVehicleEntity != Entity.Null &&
                EntityExists(resolvedVehicleEntity) &&
                m_VehicleData.HasComponent(resolvedVehicleEntity);

            bool isCar =
                resolvedVehicleEntity != Entity.Null &&
                EntityExists(resolvedVehicleEntity) &&
                m_CarData.HasComponent(resolvedVehicleEntity);

            bool isTrain =
                resolvedVehicleEntity != Entity.Null &&
                EntityExists(resolvedVehicleEntity) &&
                m_TrainData.HasComponent(resolvedVehicleEntity);

            bool hasParkedRoadCar =
                resolvedVehicleEntity != Entity.Null &&
                EntityExists(resolvedVehicleEntity) &&
                m_ParkedCarData.HasComponent(resolvedVehicleEntity);

            bool hasParkedTrain =
                resolvedVehicleEntity != Entity.Null &&
                EntityExists(resolvedVehicleEntity) &&
                m_ParkedTrainData.HasComponent(resolvedVehicleEntity);

            bool isParked = hasParkedRoadCar || hasParkedTrain;

            bool isVehicle =
                hasVehicleMarker ||
                isCar ||
                isTrain ||
                hasParkedRoadCar ||
                hasParkedTrain;

            bool hasCarCurrentLane =
                isCar &&
                m_CarCurrentLaneData.HasComponent(resolvedVehicleEntity);

            bool hasTrainCurrentLane =
                isTrain &&
                m_TrainCurrentLaneData.HasComponent(resolvedVehicleEntity);

            bool hasLiveLaneData = hasCarCurrentLane || hasTrainCurrentLane;

            SelectedVehicleKind vehicleKind;
            if (!isVehicle)
            {
                vehicleKind = SelectedVehicleKind.None;
            }
            else if (hasParkedTrain)
            {
                vehicleKind = SelectedVehicleKind.ParkedTrain;
            }
            else if (isTrain)
            {
                vehicleKind = SelectedVehicleKind.Train;
            }
            else if (hasParkedRoadCar)
            {
                vehicleKind = SelectedVehicleKind.ParkedRoadCar;
            }
            else if (isCar)
            {
                vehicleKind = SelectedVehicleKind.RoadCar;
            }
            else
            {
                vehicleKind = SelectedVehicleKind.OtherVehicle;
            }

            SelectedVehicleResolveState resolveState =
                isVehicle
                    ? SelectedVehicleResolveState.Vehicle
                    : SelectedVehicleResolveState.NotVehicle;

            return new SelectedVehicleResolveResult(
                resolveState,
                vehicleKind,
                sourceSelectedEntity,
                resolvedVehicleEntity,
                hasSelection: true,
                isVehicle,
                isCar,
                isTrain,
                isParked,
                isTrailerChild,
                hasCarCurrentLane,
                hasTrainCurrentLane,
                hasLiveLaneData);
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
