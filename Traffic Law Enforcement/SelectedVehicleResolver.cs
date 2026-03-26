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

    public enum SelectedVehicleRuntimeFamily
    {
        None,
        Car,
        Train,
        Other,
    }

    public enum SelectedVehicleRailSubtypeSource
    {
        None,
        TransportType,
        TrackType,
        Fallback,
    }

    public readonly struct SelectedVehicleResolveResult
    {
        public readonly SelectedVehicleResolveState ResolveState;
        public readonly SelectedVehicleKind VehicleKind;
        public readonly SelectedVehicleRuntimeFamily RuntimeFamily;
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
        public readonly SelectedVehicleRailSubtypeSource RailSubtypeSource;

        public SelectedVehicleResolveResult(
            SelectedVehicleResolveState resolveState,
            SelectedVehicleKind vehicleKind,
            SelectedVehicleRuntimeFamily runtimeFamily,
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
            SelectedVehicleRailSubtypeSource railSubtypeSource)
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
        private ComponentLookup<PrefabRef> m_PrefabRefData;
        private ComponentLookup<PublicTransportVehicleData> m_PublicTransportVehicleData;
        private ComponentLookup<TrainData> m_TrainPrefabData;

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
                    SelectedVehicleRuntimeFamily.None,
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
                    railSubtypeSource: SelectedVehicleRailSubtypeSource.None);
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

            SelectedVehicleRuntimeFamily runtimeFamily = GetRuntimeFamily(
                isCar,
                isTrain,
                hasParkedRoadCar,
                isVehicle);

            SelectedVehicleKind vehicleKind;
            SelectedVehicleRailSubtypeSource railSubtypeSource =
                SelectedVehicleRailSubtypeSource.None;
            if (!isVehicle)
            {
                vehicleKind = SelectedVehicleKind.None;
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

        private static SelectedVehicleKind ResolveRailVehicleKind(
            bool isParked,
            bool hasPublicTransportVehicleData,
            TransportType rawTransportType,
            bool hasTrainData,
            TrackTypes rawTrackType,
            out SelectedVehicleRailSubtypeSource railSubtypeSource)
        {
            if (hasPublicTransportVehicleData &&
                TryMapTransportTypeToRailVehicleKind(
                    rawTransportType,
                    out SelectedVehicleKind vehicleKind))
            {
                railSubtypeSource = SelectedVehicleRailSubtypeSource.TransportType;
                return isParked
                    ? ToParkedRailVehicleKind(vehicleKind)
                    : vehicleKind;
            }

            if (hasTrainData &&
                TryMapTrackTypeToRailVehicleKind(rawTrackType, out SelectedVehicleKind trackVehicleKind))
            {
                railSubtypeSource = SelectedVehicleRailSubtypeSource.TrackType;
                return isParked
                    ? ToParkedRailVehicleKind(trackVehicleKind)
                    : trackVehicleKind;
            }

            railSubtypeSource = SelectedVehicleRailSubtypeSource.Fallback;
            return isParked
                ? SelectedVehicleKind.ParkedRailVehicle
                : SelectedVehicleKind.RailVehicle;
        }

        private static bool TryMapTransportTypeToRailVehicleKind(
            TransportType transportType,
            out SelectedVehicleKind vehicleKind)
        {
            vehicleKind = SelectedVehicleKind.RailVehicle;

            switch (transportType)
            {
                case TransportType.Tram:
                    vehicleKind = SelectedVehicleKind.Tram;
                    return true;

                case TransportType.Train:
                    vehicleKind = SelectedVehicleKind.Train;
                    return true;

                case TransportType.Subway:
                    vehicleKind = SelectedVehicleKind.Subway;
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
            out SelectedVehicleKind vehicleKind)
        {
            vehicleKind = SelectedVehicleKind.RailVehicle;

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
                vehicleKind = SelectedVehicleKind.Tram;
                return true;
            }

            if (isSubwayTrack)
            {
                vehicleKind = SelectedVehicleKind.Subway;
                return true;
            }

            vehicleKind = SelectedVehicleKind.Train;
            return true;
        }

        private static SelectedVehicleKind ToParkedRailVehicleKind(
            SelectedVehicleKind vehicleKind)
        {
            switch (vehicleKind)
            {
                case SelectedVehicleKind.Tram:
                    return SelectedVehicleKind.ParkedTram;

                case SelectedVehicleKind.Train:
                    return SelectedVehicleKind.ParkedTrain;

                case SelectedVehicleKind.Subway:
                    return SelectedVehicleKind.ParkedSubway;

                default:
                    return SelectedVehicleKind.ParkedRailVehicle;
            }
        }

        private static SelectedVehicleRuntimeFamily GetRuntimeFamily(
            bool isCar,
            bool isTrain,
            bool hasParkedRoadCar,
            bool isVehicle)
        {
            if (isCar || hasParkedRoadCar)
            {
                return SelectedVehicleRuntimeFamily.Car;
            }

            if (isTrain)
            {
                return SelectedVehicleRuntimeFamily.Train;
            }

            return isVehicle
                ? SelectedVehicleRuntimeFamily.Other
                : SelectedVehicleRuntimeFamily.None;
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
