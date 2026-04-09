using Game.Common;
using Game.Buildings;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Unity.Entities;
using Entity = Unity.Entities.Entity;
using CarLane = Game.Net.CarLane;
using ParkingLane = Game.Net.ParkingLane;
using ConnectionLane = Game.Net.ConnectionLane;
using PrefabRef = Game.Prefabs.PrefabRef;
using PrefabSpawnLocationData = Game.Prefabs.SpawnLocationData;
using RouteConnectionType = Game.Prefabs.RouteConnectionType;
using SpawnLocation = Game.Objects.SpawnLocation;

namespace Traffic_Law_Enforcement
{
    internal enum AccessEndpointKind : byte
    {
        None = 0,
        ParkingLane = 1,
        GarageLane = 2,
        ParkingConnection = 3,
        BuildingService = 4,
    }

    internal struct AccessEndpointLookupContext
    {
        public ComponentLookup<CarLane> CarLaneData;
        public ComponentLookup<EdgeLane> EdgeLaneData;
        public ComponentLookup<ParkingLane> ParkingLaneData;
        public ComponentLookup<GarageLane> GarageLaneData;
        public ComponentLookup<ConnectionLane> ConnectionLaneData;
        public ComponentLookup<Owner> OwnerData;
        public ComponentLookup<SpawnLocation> SpawnLocationData;
        public ComponentLookup<PrefabRef> PrefabRefData;
        public ComponentLookup<PrefabSpawnLocationData> PrefabSpawnLocationData;
        public ComponentLookup<Building> BuildingData;
        public ComponentLookup<ServiceUpgrade> ServiceUpgradeData;
    }

    internal static class AccessEndpointClassifier
    {
        public static AccessEndpointKind Classify(
            EntityManager entityManager,
            Entity lane,
            PathMethod pathMethodsHint = 0)
        {
            if (lane == Entity.Null)
            {
                return AccessEndpointKind.None;
            }

            bool hasParkingLane = entityManager.HasComponent<ParkingLane>(lane);
            bool hasGarageLane = entityManager.HasComponent<GarageLane>(lane);
            bool hasCarLane = entityManager.HasComponent<CarLane>(lane);
            bool isRoadLane = entityManager.HasComponent<EdgeLane>(lane) && hasCarLane;
            bool hasConnectionLane = TryGetConnectionLane(entityManager, lane, out ConnectionLane connectionLane);
            bool hasDirectBuildingOwnerAnchor =
                HasDirectBuildingServiceOwnerAnchor(entityManager, lane);
            bool hasCargoSpawnAnchor =
                TryGetSpawnLocationConnectionType(entityManager, lane, out RouteConnectionType connectionType) &&
                connectionType == RouteConnectionType.Cargo;
            bool hasCargoLoadingHint =
                (pathMethodsHint & PathMethod.CargoLoading) != 0;

            return ClassifyCore(
                hasParkingLane,
                hasGarageLane,
                hasCarLane,
                isRoadLane,
                hasConnectionLane,
                connectionLane,
                hasDirectBuildingOwnerAnchor,
                hasCargoSpawnAnchor,
                hasCargoLoadingHint);
        }

        public static AccessEndpointKind Classify(
            Entity lane,
            ref AccessEndpointLookupContext context,
            PathMethod pathMethodsHint = 0)
        {
            if (lane == Entity.Null)
            {
                return AccessEndpointKind.None;
            }

            bool hasParkingLane = context.ParkingLaneData.HasComponent(lane);
            bool hasGarageLane = context.GarageLaneData.HasComponent(lane);
            bool hasCarLane = context.CarLaneData.HasComponent(lane);
            bool isRoadLane = context.EdgeLaneData.HasComponent(lane) && hasCarLane;
            bool hasConnectionLane =
                context.ConnectionLaneData.TryGetComponent(lane, out ConnectionLane connectionLane);
            bool hasDirectBuildingOwnerAnchor =
                HasDirectBuildingServiceOwnerAnchor(lane, ref context);
            bool hasCargoSpawnAnchor =
                TryGetSpawnLocationConnectionType(lane, ref context, out RouteConnectionType connectionType) &&
                connectionType == RouteConnectionType.Cargo;
            bool hasCargoLoadingHint =
                (pathMethodsHint & PathMethod.CargoLoading) != 0;

            return ClassifyCore(
                hasParkingLane,
                hasGarageLane,
                hasCarLane,
                isRoadLane,
                hasConnectionLane,
                connectionLane,
                hasDirectBuildingOwnerAnchor,
                hasCargoSpawnAnchor,
                hasCargoLoadingHint);
        }

        public static bool IsAccessOrigin(
            EntityManager entityManager,
            Entity lane,
            PathMethod pathMethodsHint = 0)
        {
            return Classify(entityManager, lane, pathMethodsHint) != AccessEndpointKind.None;
        }

        public static bool IsAccessOrigin(
            Entity lane,
            ref AccessEndpointLookupContext context,
            PathMethod pathMethodsHint = 0)
        {
            return Classify(lane, ref context, pathMethodsHint) != AccessEndpointKind.None;
        }

        public static string Describe(AccessEndpointKind kind)
        {
            return kind switch
            {
                AccessEndpointKind.ParkingLane => "parking access",
                AccessEndpointKind.GarageLane => "garage access",
                AccessEndpointKind.ParkingConnection => "parking connection",
                AccessEndpointKind.BuildingService => "building/service access connection",
                _ => "building access",
            };
        }

        private static AccessEndpointKind ClassifyCore(
            bool hasParkingLane,
            bool hasGarageLane,
            bool hasCarLane,
            bool isRoadLane,
            bool hasConnectionLane,
            ConnectionLane connectionLane,
            bool hasDirectBuildingOwnerAnchor,
            bool hasCargoSpawnAnchor,
            bool hasCargoLoadingHint)
        {
            if (hasParkingLane)
            {
                return AccessEndpointKind.ParkingLane;
            }

            if (hasGarageLane)
            {
                return AccessEndpointKind.GarageLane;
            }

            if (hasConnectionLane)
            {
                if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
                {
                    return AccessEndpointKind.ParkingConnection;
                }

                if (IsAnchoredNonParkingBuildingServiceConnection(
                        connectionLane,
                        hasDirectBuildingOwnerAnchor,
                        hasCargoSpawnAnchor,
                        hasCargoLoadingHint))
                {
                    return AccessEndpointKind.BuildingService;
                }

                return AccessEndpointKind.None;
            }

            if (isRoadLane)
            {
                return AccessEndpointKind.None;
            }

            if (!hasCarLane &&
                !hasDirectBuildingOwnerAnchor &&
                !hasCargoSpawnAnchor)
            {
                return AccessEndpointKind.None;
            }

            // Bare generic non-road lanes qualify only when they are directly owned by
            // the terminal building/service entity or expose an explicit cargo anchor.
            // This keeps internal invisible-path/service-road segments out of the
            // access family while still recognizing the true terminal building lane.
            return (hasDirectBuildingOwnerAnchor || hasCargoSpawnAnchor)
                ? AccessEndpointKind.BuildingService
                : AccessEndpointKind.None;
        }

        private static bool IsAnchoredNonParkingBuildingServiceConnection(
            ConnectionLane connectionLane,
            bool hasDirectBuildingOwnerAnchor,
            bool hasCargoSpawnAnchor,
            bool hasCargoLoadingHint)
        {
            bool roadConnection = (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0;
            bool pedestrianConnection = (connectionLane.m_Flags & ConnectionLaneFlags.Pedestrian) != 0;
            bool cargoConnection = (connectionLane.m_Flags & ConnectionLaneFlags.AllowCargo) != 0;
            bool insideConnection = (connectionLane.m_Flags & ConnectionLaneFlags.Inside) != 0;

            if (!roadConnection)
            {
                return insideConnection ||
                    cargoConnection ||
                    hasCargoSpawnAnchor ||
                    hasDirectBuildingOwnerAnchor ||
                    hasCargoLoadingHint ||
                    !pedestrianConnection;
            }

            if (pedestrianConnection)
            {
                return cargoConnection || hasCargoSpawnAnchor;
            }

            return cargoConnection ||
                hasCargoSpawnAnchor ||
                (hasCargoLoadingHint && hasDirectBuildingOwnerAnchor);
        }

        private static bool HasDirectBuildingServiceOwnerAnchor(
            EntityManager entityManager,
            Entity lane)
        {
            Entity ownerEntity = GetOwner(entityManager, lane);
            return ownerEntity != Entity.Null &&
                (entityManager.HasComponent<Building>(ownerEntity) ||
                entityManager.HasComponent<ServiceUpgrade>(ownerEntity));
        }

        private static bool HasDirectBuildingServiceOwnerAnchor(
            Entity lane,
            ref AccessEndpointLookupContext context)
        {
            Entity ownerEntity = GetOwner(lane, ref context);
            return ownerEntity != Entity.Null &&
                (context.BuildingData.HasComponent(ownerEntity) ||
                context.ServiceUpgradeData.HasComponent(ownerEntity));
        }

        private static bool IsPlainRoadLikeEntity(
            EntityManager entityManager,
            Entity entity)
        {
            if (entity == Entity.Null)
            {
                return false;
            }

            bool hasCarLane = entityManager.HasComponent<CarLane>(entity);
            if (entityManager.HasComponent<EdgeLane>(entity) && hasCarLane)
            {
                return true;
            }

            if (TryGetConnectionLane(entityManager, entity, out ConnectionLane connectionLane))
            {
                bool parkingAccess = (connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0;
                bool roadConnection = (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0;
                bool cargoConnection = (connectionLane.m_Flags & ConnectionLaneFlags.AllowCargo) != 0;
                return roadConnection && !parkingAccess && !cargoConnection;
            }

            if (TryGetSpawnLocationConnectionType(entityManager, entity, out RouteConnectionType routeConnectionType))
            {
                return routeConnectionType == RouteConnectionType.Road;
            }

            return false;
        }

        private static bool IsPlainRoadLikeEntity(
            Entity entity,
            ref AccessEndpointLookupContext context)
        {
            if (entity == Entity.Null)
            {
                return false;
            }

            bool hasCarLane = context.CarLaneData.HasComponent(entity);
            if (context.EdgeLaneData.HasComponent(entity) && hasCarLane)
            {
                return true;
            }

            if (context.ConnectionLaneData.TryGetComponent(entity, out ConnectionLane connectionLane))
            {
                bool parkingAccess = (connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0;
                bool roadConnection = (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0;
                bool cargoConnection = (connectionLane.m_Flags & ConnectionLaneFlags.AllowCargo) != 0;
                return roadConnection && !parkingAccess && !cargoConnection;
            }

            if (TryGetSpawnLocationConnectionType(entity, ref context, out RouteConnectionType routeConnectionType))
            {
                return routeConnectionType == RouteConnectionType.Road;
            }

            return false;
        }

        private static bool TryGetConnectionLane(
            EntityManager entityManager,
            Entity lane,
            out ConnectionLane connectionLane)
        {
            if (entityManager.HasComponent<ConnectionLane>(lane))
            {
                connectionLane = entityManager.GetComponentData<ConnectionLane>(lane);
                return true;
            }

            connectionLane = default;
            return false;
        }

        private static bool TryGetSpawnLocationConnectionType(
            EntityManager entityManager,
            Entity lane,
            out RouteConnectionType connectionType)
        {
            connectionType = RouteConnectionType.None;

            if (!entityManager.HasComponent<SpawnLocation>(lane) ||
                !entityManager.HasComponent<PrefabRef>(lane))
            {
                return false;
            }

            Entity prefab = entityManager.GetComponentData<PrefabRef>(lane).m_Prefab;
            if (prefab == Entity.Null ||
                !entityManager.HasComponent<PrefabSpawnLocationData>(prefab))
            {
                return false;
            }

            connectionType =
                entityManager.GetComponentData<PrefabSpawnLocationData>(prefab).m_ConnectionType;
            return true;
        }

        private static bool TryGetSpawnLocationConnectionType(
            Entity lane,
            ref AccessEndpointLookupContext context,
            out RouteConnectionType connectionType)
        {
            connectionType = RouteConnectionType.None;

            if (!context.SpawnLocationData.HasComponent(lane) ||
                !context.PrefabRefData.TryGetComponent(lane, out PrefabRef prefabRef))
            {
                return false;
            }

            Entity prefab = prefabRef.m_Prefab;
            if (prefab == Entity.Null ||
                !context.PrefabSpawnLocationData.TryGetComponent(prefab, out PrefabSpawnLocationData spawnLocationData))
            {
                return false;
            }

            connectionType = spawnLocationData.m_ConnectionType;
            return true;
        }

        private static Entity GetOwner(
            EntityManager entityManager,
            Entity lane)
        {
            if (entityManager.HasComponent<Owner>(lane))
            {
                return entityManager.GetComponentData<Owner>(lane).m_Owner;
            }

            return Entity.Null;
        }

        private static Entity GetOwner(
            Entity lane,
            ref AccessEndpointLookupContext context)
        {
            return context.OwnerData.TryGetComponent(lane, out Owner owner)
                ? owner.m_Owner
                : Entity.Null;
        }
    }
}
