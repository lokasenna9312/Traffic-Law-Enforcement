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
            bool hasBuildingOwnerAnchor =
                HasBuildingServiceOwnerAnchor(entityManager, lane);
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
                hasBuildingOwnerAnchor,
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
            bool hasBuildingOwnerAnchor =
                HasBuildingServiceOwnerAnchor(lane, ref context);
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
                hasBuildingOwnerAnchor,
                hasCargoSpawnAnchor,
                hasCargoLoadingHint);
        }

        public static bool HasBuildingServiceAnchor(
            EntityManager entityManager,
            Entity lane)
        {
            if (lane == Entity.Null)
            {
                return false;
            }

            if (HasBuildingServiceOwnerAnchor(entityManager, lane))
            {
                return true;
            }

            return TryGetSpawnLocationConnectionType(entityManager, lane, out RouteConnectionType connectionType) &&
                connectionType == RouteConnectionType.Cargo;
        }

        public static bool HasBuildingServiceRoadAllowanceAnchor(
            EntityManager entityManager,
            Entity lane)
        {
            if (lane == Entity.Null)
            {
                return false;
            }

            return HasBuildingServiceOwnerAnchor(entityManager, lane);
        }

        public static bool LaneMatchesBuildingServiceTarget(
            EntityManager entityManager,
            Entity lane,
            Entity targetEntity)
        {
            if (lane == Entity.Null || targetEntity == Entity.Null)
            {
                return false;
            }

            Entity laneOwner = GetOwner(entityManager, lane);
            if (laneOwner == Entity.Null)
            {
                return false;
            }

            byte targetDepth = 0;
            Entity currentTarget = targetEntity;
            while (currentTarget != Entity.Null && targetDepth < 12)
            {
                if ((entityManager.HasComponent<Building>(currentTarget) ||
                        entityManager.HasComponent<ServiceUpgrade>(currentTarget)) &&
                    OwnerChainContainsEntity(entityManager, laneOwner, currentTarget))
                {
                    return true;
                }

                if (!entityManager.HasComponent<Owner>(currentTarget))
                {
                    break;
                }

                currentTarget = entityManager.GetComponentData<Owner>(currentTarget).m_Owner;
                targetDepth += 1;
            }

            return false;
        }

        public static bool HasBuildingServiceAnchor(
            Entity lane,
            ref AccessEndpointLookupContext context)
        {
            if (lane == Entity.Null)
            {
                return false;
            }

            if (HasBuildingServiceOwnerAnchor(lane, ref context))
            {
                return true;
            }

            return TryGetSpawnLocationConnectionType(lane, ref context, out RouteConnectionType connectionType) &&
                connectionType == RouteConnectionType.Cargo;
        }

        public static bool HasBuildingServiceRoadAllowanceAnchor(
            Entity lane,
            ref AccessEndpointLookupContext context)
        {
            if (lane == Entity.Null)
            {
                return false;
            }

            return HasBuildingServiceOwnerAnchor(lane, ref context);
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
            bool hasBuildingOwnerAnchor,
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
                        hasBuildingOwnerAnchor,
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
                !hasBuildingOwnerAnchor &&
                !hasCargoSpawnAnchor)
            {
                return AccessEndpointKind.None;
            }

            // Bare generic non-road lanes belong to the non-parking building/service
            // access family when their owner chain resolves to the serviced
            // building/facility or when they expose an explicit cargo anchor.
            return (hasBuildingOwnerAnchor || hasCargoSpawnAnchor)
                ? AccessEndpointKind.BuildingService
                : AccessEndpointKind.None;
        }

        private static bool IsAnchoredNonParkingBuildingServiceConnection(
            ConnectionLane connectionLane,
            bool hasBuildingOwnerAnchor,
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
                    hasBuildingOwnerAnchor ||
                    hasCargoLoadingHint ||
                    !pedestrianConnection;
            }

            if (pedestrianConnection)
            {
                return cargoConnection || hasCargoSpawnAnchor;
            }

            return cargoConnection ||
                hasCargoSpawnAnchor ||
                (hasCargoLoadingHint && hasBuildingOwnerAnchor);
        }

        private static bool HasBuildingServiceOwnerAnchor(
            EntityManager entityManager,
            Entity lane)
        {
            return HasBuildingServiceEntityInOwnerChain(
                entityManager,
                GetOwner(entityManager, lane));
        }

        private static bool HasBuildingServiceOwnerAnchor(
            Entity lane,
            ref AccessEndpointLookupContext context)
        {
            return HasBuildingServiceEntityInOwnerChain(
                GetOwner(lane, ref context),
                ref context);
        }

        private static bool HasBuildingServiceEntityInOwnerChain(
            EntityManager entityManager,
            Entity entity)
        {
            byte depth = 0;
            while (entity != Entity.Null && depth < 12)
            {
                if (entityManager.HasComponent<Building>(entity) ||
                    entityManager.HasComponent<ServiceUpgrade>(entity))
                {
                    return true;
                }

                if (!entityManager.HasComponent<Owner>(entity))
                {
                    break;
                }

                entity = entityManager.GetComponentData<Owner>(entity).m_Owner;
                depth += 1;
            }

            return false;
        }

        private static bool OwnerChainContainsEntity(
            EntityManager entityManager,
            Entity entity,
            Entity candidate)
        {
            byte depth = 0;
            while (entity != Entity.Null && depth < 12)
            {
                if (entity == candidate)
                {
                    return true;
                }

                if (!entityManager.HasComponent<Owner>(entity))
                {
                    break;
                }

                entity = entityManager.GetComponentData<Owner>(entity).m_Owner;
                depth += 1;
            }

            return false;
        }

        private static bool HasBuildingServiceEntityInOwnerChain(
            Entity entity,
            ref AccessEndpointLookupContext context)
        {
            byte depth = 0;
            while (entity != Entity.Null && depth < 12)
            {
                if (context.BuildingData.HasComponent(entity) ||
                    context.ServiceUpgradeData.HasComponent(entity))
                {
                    return true;
                }

                if (!context.OwnerData.TryGetComponent(entity, out Owner owner))
                {
                    break;
                }

                entity = owner.m_Owner;
                depth += 1;
            }

            return false;
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
