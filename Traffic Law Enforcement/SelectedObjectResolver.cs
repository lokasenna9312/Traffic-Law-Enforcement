using Game;
using Game.Net;
using Game.Prefabs;
using Game.UI.InGame;
using Game.Vehicles;
using Unity.Entities;
using Entity = Unity.Entities.Entity;
using PrefabRef = Game.Prefabs.PrefabRef;

namespace Traffic_Law_Enforcement
{
    public enum SelectedObjectResolveState
    {
        None,
        NotVehicle,
        Vehicle,
    }

    public enum SelectedObjectKind
    {
        None,
        RoadCar,
        ParkedRoadCar,
        RailVehicle,
        ParkedRailVehicle,
        Tram,
        ParkedTram,
        Train,
        ParkedTrain,
        Subway,
        ParkedSubway,
        OtherVehicle,
    }

    public enum SelectedObjectRuntimeFamily
    {
        None,
        Car,
        Train,
        Other,
    }

    public enum SelectedObjectRailSubtypeSource
    {
        None,
        TransportType,
        TrackType,
        Fallback,
    }

    public readonly struct SelectedObjectResolveResult
    {
        public readonly SelectedObjectResolveState ResolveState;
        public readonly SelectedObjectKind VehicleKind;
        public readonly SelectedObjectRuntimeFamily RuntimeFamily;
        public readonly Entity SourceSelectedEntity;
        public readonly Entity ResolvedVehicleEntity;
        public readonly Entity PrefabEntity;
        public readonly bool HasSelection;
        public readonly bool HasPrefabRef;
        public readonly bool IsVehicle;
        public readonly bool IsCar;
        public readonly bool IsTrain;
        public readonly bool IsParked;
        public readonly bool IsTrailerChild;
        public readonly bool HasCarCurrentLane;
        public readonly bool HasTrainCurrentLane;
        public readonly bool HasLiveLaneData;
        public readonly bool HasPublicTransportVehicleData;
        public readonly bool HasTrainData;
        public readonly TransportType RawTransportType;
        public readonly TrackTypes RawTrackType;
        public readonly SelectedObjectRailSubtypeSource RailSubtypeSource;

        public SelectedObjectResolveResult(
            SelectedObjectResolveState resolveState,
            SelectedObjectKind vehicleKind,
            SelectedObjectRuntimeFamily runtimeFamily,
            Entity sourceSelectedEntity,
            Entity resolvedVehicleEntity,
            Entity prefabEntity,
            bool hasSelection,
            bool hasPrefabRef,
            bool isVehicle,
            bool isCar,
            bool isTrain,
            bool isParked,
            bool isTrailerChild,
            bool hasCarCurrentLane,
            bool hasTrainCurrentLane,
            bool hasLiveLaneData,
            bool hasPublicTransportVehicleData,
            bool hasTrainData,
            TransportType rawTransportType,
            TrackTypes rawTrackType,
            SelectedObjectRailSubtypeSource railSubtypeSource)
        {
            ResolveState = resolveState;
            VehicleKind = vehicleKind;
            RuntimeFamily = runtimeFamily;
            SourceSelectedEntity = sourceSelectedEntity;
            ResolvedVehicleEntity = resolvedVehicleEntity;
            PrefabEntity = prefabEntity;
            HasSelection = hasSelection;
            HasPrefabRef = hasPrefabRef;
            IsVehicle = isVehicle;
            IsCar = isCar;
            IsTrain = isTrain;
            IsParked = isParked;
            IsTrailerChild = isTrailerChild;
            HasCarCurrentLane = hasCarCurrentLane;
            HasTrainCurrentLane = hasTrainCurrentLane;
            HasLiveLaneData = hasLiveLaneData;
            HasPublicTransportVehicleData = hasPublicTransportVehicleData;
            HasTrainData = hasTrainData;
            RawTransportType = rawTransportType;
            RawTrackType = rawTrackType;
            RailSubtypeSource = railSubtypeSource;
        }
    }

    internal sealed class SelectedObjectResolver
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
        private ComponentLookup<PrefabRef> m_PrefabRefData;
        private ComponentLookup<PublicTransportVehicleData> m_PublicTransportVehicleData;
        private ComponentLookup<TrainData> m_TrainPrefabData;

        public SelectedObjectResolver(GameSystemBase system)
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
            m_PrefabRefData = system.GetComponentLookup<PrefabRef>(true);
            m_PublicTransportVehicleData =
                system.GetComponentLookup<PublicTransportVehicleData>(true);
            m_TrainPrefabData = system.GetComponentLookup<TrainData>(true);
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
            m_PrefabRefData.Update(system);
            m_PublicTransportVehicleData.Update(system);
            m_TrainPrefabData.Update(system);
        }

        public SelectedObjectResolveResult ResolveCurrentSelection()
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
                return new SelectedObjectResolveResult(
                    SelectedObjectResolveState.None,
                    SelectedObjectKind.None,
                    SelectedObjectRuntimeFamily.None,
                    sourceSelectedEntity,
                    Entity.Null,
                    Entity.Null,
                    hasSelection: false,
                    hasPrefabRef: false,
                    isVehicle: false,
                    isCar: false,
                    isTrain: false,
                    isParked: false,
                    isTrailerChild: false,
                    hasCarCurrentLane: false,
                    hasTrainCurrentLane: false,
                    hasLiveLaneData: false,
                    hasPublicTransportVehicleData: false,
                    hasTrainData: false,
                    rawTransportType: TransportType.None,
                    rawTrackType: TrackTypes.None,
                    railSubtypeSource: SelectedObjectRailSubtypeSource.None);
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

            bool hasTrainMarker =
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
            bool isTrain = hasTrainMarker || hasParkedTrain;

            bool isVehicle =
                hasVehicleMarker ||
                isCar ||
                hasTrainMarker ||
                hasParkedRoadCar ||
                hasParkedTrain;

            bool hasCarCurrentLane =
                isCar &&
                m_CarCurrentLaneData.HasComponent(resolvedVehicleEntity);

            bool hasTrainCurrentLane =
                hasTrainMarker &&
                m_TrainCurrentLaneData.HasComponent(resolvedVehicleEntity);

            bool hasLiveLaneData = hasCarCurrentLane || hasTrainCurrentLane;

            bool hasPrefabRef =
                TryGetPrefabEntity(resolvedVehicleEntity, out Entity prefabEntity);

            bool hasPublicTransportVehicleData = false;
            PublicTransportVehicleData publicTransportVehicleData = default;
            if (hasPrefabRef)
            {
                hasPublicTransportVehicleData =
                    m_PublicTransportVehicleData.TryGetComponent(
                        prefabEntity,
                        out publicTransportVehicleData);
            }

            TransportType rawTransportType =
                hasPublicTransportVehicleData
                    ? publicTransportVehicleData.m_TransportType
                    : TransportType.None;

            bool hasTrainData = false;
            TrainData trainData = default;
            if (hasPrefabRef)
            {
                hasTrainData =
                    m_TrainPrefabData.TryGetComponent(prefabEntity, out trainData);
            }

            TrackTypes rawTrackType =
                hasTrainData
                    ? trainData.m_TrackType
                    : TrackTypes.None;

            SelectedObjectRuntimeFamily runtimeFamily = GetRuntimeFamily(
                isCar,
                isTrain,
                hasParkedRoadCar,
                isVehicle);

            SelectedObjectKind vehicleKind;
            SelectedObjectRailSubtypeSource railSubtypeSource =
                SelectedObjectRailSubtypeSource.None;
            if (!isVehicle)
            {
                vehicleKind = SelectedObjectKind.None;
            }
            else if (hasParkedTrain)
            {
                vehicleKind = ResolveRailVehicleKind(
                    isParked: true,
                    hasPublicTransportVehicleData,
                    rawTransportType,
                    hasTrainData,
                    rawTrackType,
                    out railSubtypeSource);
            }
            else if (hasTrainMarker)
            {
                vehicleKind = ResolveRailVehicleKind(
                    isParked: false,
                    hasPublicTransportVehicleData,
                    rawTransportType,
                    hasTrainData,
                    rawTrackType,
                    out railSubtypeSource);
            }
            else if (hasParkedRoadCar)
            {
                vehicleKind = SelectedObjectKind.ParkedRoadCar;
            }
            else if (isCar)
            {
                vehicleKind = SelectedObjectKind.RoadCar;
            }
            else
            {
                vehicleKind = SelectedObjectKind.OtherVehicle;
            }

            SelectedObjectResolveState resolveState =
                isVehicle
                    ? SelectedObjectResolveState.Vehicle
                    : SelectedObjectResolveState.NotVehicle;

            return new SelectedObjectResolveResult(
                resolveState,
                vehicleKind,
                runtimeFamily,
                sourceSelectedEntity,
                resolvedVehicleEntity,
                prefabEntity,
                hasSelection: true,
                hasPrefabRef,
                isVehicle,
                isCar,
                isTrain,
                isParked,
                isTrailerChild,
                hasCarCurrentLane,
                hasTrainCurrentLane,
                hasLiveLaneData,
                hasPublicTransportVehicleData,
                hasTrainData,
                rawTransportType,
                rawTrackType,
                railSubtypeSource);
        }

        private static SelectedObjectKind ResolveRailVehicleKind(
            bool isParked,
            bool hasPublicTransportVehicleData,
            TransportType rawTransportType,
            bool hasTrainData,
            TrackTypes rawTrackType,
            out SelectedObjectRailSubtypeSource railSubtypeSource)
        {
            if (hasPublicTransportVehicleData &&
                TryMapTransportTypeToRailVehicleKind(
                    rawTransportType,
                    out SelectedObjectKind vehicleKind))
            {
                railSubtypeSource = SelectedObjectRailSubtypeSource.TransportType;
                return isParked
                    ? ToParkedRailVehicleKind(vehicleKind)
                    : vehicleKind;
            }

            if (hasTrainData &&
                TryMapTrackTypeToRailVehicleKind(rawTrackType, out SelectedObjectKind trackVehicleKind))
            {
                railSubtypeSource = SelectedObjectRailSubtypeSource.TrackType;
                return isParked
                    ? ToParkedRailVehicleKind(trackVehicleKind)
                    : trackVehicleKind;
            }

            railSubtypeSource = SelectedObjectRailSubtypeSource.Fallback;
            return isParked
                ? SelectedObjectKind.ParkedRailVehicle
                : SelectedObjectKind.RailVehicle;
        }

        private static bool TryMapTransportTypeToRailVehicleKind(
            TransportType transportType,
            out SelectedObjectKind vehicleKind)
        {
            vehicleKind = SelectedObjectKind.RailVehicle;

            switch (transportType)
            {
                case TransportType.Tram:
                    vehicleKind = SelectedObjectKind.Tram;
                    return true;

                case TransportType.Train:
                    vehicleKind = SelectedObjectKind.Train;
                    return true;

                case TransportType.Subway:
                    vehicleKind = SelectedObjectKind.Subway;
                    return true;

                default:
                    return false;
            }
        }

        private bool TryGetPrefabEntity(Entity vehicle, out Entity prefabEntity)
        {
            prefabEntity = Entity.Null;

            return vehicle != Entity.Null &&
                EntityExists(vehicle) &&
                m_PrefabRefData.TryGetComponent(vehicle, out PrefabRef prefabRef) &&
                prefabRef.m_Prefab != Entity.Null &&
                EntityExists(prefabRef.m_Prefab) &&
                (prefabEntity = prefabRef.m_Prefab) != Entity.Null;
        }

        private static bool TryMapTrackTypeToRailVehicleKind(
            TrackTypes trackType,
            out SelectedObjectKind vehicleKind)
        {
            vehicleKind = SelectedObjectKind.RailVehicle;

            bool isTrainTrack = (trackType & TrackTypes.Train) != TrackTypes.None;
            bool isTramTrack = (trackType & TrackTypes.Tram) != TrackTypes.None;
            bool isSubwayTrack = (trackType & TrackTypes.Subway) != TrackTypes.None;

            int matchedTypeCount =
                (isTrainTrack ? 1 : 0) +
                (isTramTrack ? 1 : 0) +
                (isSubwayTrack ? 1 : 0);

            if (matchedTypeCount != 1)
            {
                return false;
            }

            if (isTramTrack)
            {
                vehicleKind = SelectedObjectKind.Tram;
                return true;
            }

            if (isSubwayTrack)
            {
                vehicleKind = SelectedObjectKind.Subway;
                return true;
            }

            vehicleKind = SelectedObjectKind.Train;
            return true;
        }

        private static SelectedObjectKind ToParkedRailVehicleKind(
            SelectedObjectKind vehicleKind)
        {
            switch (vehicleKind)
            {
                case SelectedObjectKind.Tram:
                    return SelectedObjectKind.ParkedTram;

                case SelectedObjectKind.Train:
                    return SelectedObjectKind.ParkedTrain;

                case SelectedObjectKind.Subway:
                    return SelectedObjectKind.ParkedSubway;

                default:
                    return SelectedObjectKind.ParkedRailVehicle;
            }
        }

        private static SelectedObjectRuntimeFamily GetRuntimeFamily(
            bool isCar,
            bool isTrain,
            bool hasParkedRoadCar,
            bool isVehicle)
        {
            if (isCar || hasParkedRoadCar)
            {
                return SelectedObjectRuntimeFamily.Car;
            }

            if (isTrain)
            {
                return SelectedObjectRuntimeFamily.Train;
            }

            return isVehicle
                ? SelectedObjectRuntimeFamily.Other
                : SelectedObjectRuntimeFamily.None;
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

