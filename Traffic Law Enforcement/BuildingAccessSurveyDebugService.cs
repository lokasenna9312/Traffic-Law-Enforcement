using System.Collections.Generic;
using System.Text;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.UI.InGame;
using Unity.Entities;
using Unity.Collections;
using Entity = Unity.Entities.Entity;
using CarLane = Game.Net.CarLane;
using ConnectionLane = Game.Net.ConnectionLane;
using ParkingLane = Game.Net.ParkingLane;
using PrefabRef = Game.Prefabs.PrefabRef;
using PrefabSpawnLocationData = Game.Prefabs.SpawnLocationData;
using RouteConnectionType = Game.Prefabs.RouteConnectionType;
using SpawnLocation = Game.Objects.SpawnLocation;
using ServiceUpgrade = Game.Buildings.ServiceUpgrade;

namespace Traffic_Law_Enforcement
{
    internal static class BuildingAccessSurveyDebugService
    {
        private struct SurveyEntry
        {
            public Entity Entity;
            public string Text;
        }

        internal static void DumpSelectedBuildingAccessSurvey()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Mod.log.Info("[BUILDING_ACCESS_SURVEY] world unavailable");
                return;
            }

            SelectedInfoUISystem selectedInfoSystem =
                world.GetExistingSystemManaged<SelectedInfoUISystem>();
            if (selectedInfoSystem == null)
            {
                Mod.log.Info("[BUILDING_ACCESS_SURVEY] SelectedInfoUISystem unavailable");
                return;
            }

            EntityManager entityManager = world.EntityManager;
            Entity selected = selectedInfoSystem.selectedEntity;
            if (selected == Entity.Null)
            {
                Mod.log.Info("[BUILDING_ACCESS_SURVEY] no selected entity");
                return;
            }

            Entity surveyRoot = ResolveSurveyRoot(entityManager, selected);
            if (surveyRoot == Entity.Null)
            {
                Mod.log.Info(
                    $"[BUILDING_ACCESS_SURVEY] selected={FormatEntity(selected)} has no building/service root in owner chain");
                return;
            }

            List<SurveyEntry> entries = new List<SurveyEntry>(256);
            EntityQuery ownerQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Owner>());
            using NativeArray<Entity> ownedEntities = ownerQuery.ToEntityArray(Allocator.Temp);
            for (int index = 0; index < ownedEntities.Length; index++)
            {
                Entity entity = ownedEntities[index];
                if (!OwnerChainContainsEntity(entityManager, entity, surveyRoot))
                {
                    continue;
                }

                if (!IsSurveyRelevant(entityManager, entity))
                {
                    continue;
                }

                entries.Add(new SurveyEntry
                {
                    Entity = entity,
                    Text = BuildEntryText(entityManager, entity, surveyRoot),
                });
            }

            entries.Sort(static (left, right) => left.Entity.Index.CompareTo(right.Entity.Index));

            Mod.log.Info(
                $"[BUILDING_ACCESS_SURVEY] begin selected={FormatEntity(selected)} root={FormatEntity(surveyRoot)} ownedRelevant={entries.Count}");

            Mod.log.Info(
                $"[BUILDING_ACCESS_SURVEY] root-summary root={FormatEntity(surveyRoot)} kind={DescribeRootKind(entityManager, surveyRoot)} ownerChain={BuildOwnerChainText(entityManager, surveyRoot)}");

            for (int index = 0; index < entries.Count; index++)
            {
                Mod.log.Info($"[BUILDING_ACCESS_SURVEY] {entries[index].Text}");
            }

            Mod.log.Info(
                $"[BUILDING_ACCESS_SURVEY] end root={FormatEntity(surveyRoot)} ownedRelevant={entries.Count}");
        }

        private static Entity ResolveSurveyRoot(EntityManager entityManager, Entity entity)
        {
            byte depth = 0;
            Entity current = entity;
            while (current != Entity.Null && depth < 16)
            {
                if (entityManager.HasComponent<Building>(current) ||
                    entityManager.HasComponent<ServiceUpgrade>(current))
                {
                    return current;
                }

                if (!entityManager.HasComponent<Owner>(current))
                {
                    break;
                }

                current = entityManager.GetComponentData<Owner>(current).m_Owner;
                depth += 1;
            }

            return Entity.Null;
        }

        private static bool OwnerChainContainsEntity(
            EntityManager entityManager,
            Entity entity,
            Entity candidate)
        {
            byte depth = 0;
            Entity current = entity;
            while (current != Entity.Null && depth < 16)
            {
                if (current == candidate)
                {
                    return true;
                }

                if (!entityManager.HasComponent<Owner>(current))
                {
                    break;
                }

                current = entityManager.GetComponentData<Owner>(current).m_Owner;
                depth += 1;
            }

            return false;
        }

        private static bool IsSurveyRelevant(EntityManager entityManager, Entity entity)
        {
            return entityManager.HasComponent<CarLane>(entity) ||
                entityManager.HasComponent<ConnectionLane>(entity) ||
                entityManager.HasComponent<ParkingLane>(entity) ||
                entityManager.HasComponent<GarageLane>(entity) ||
                entityManager.HasComponent<SpawnLocation>(entity) ||
                entityManager.HasComponent<EdgeLane>(entity);
        }

        private static string BuildEntryText(
            EntityManager entityManager,
            Entity entity,
            Entity surveyRoot)
        {
            StringBuilder builder = new StringBuilder(384);
            builder.Append("entity=").Append(FormatEntity(entity));
            builder.Append(" classify=").Append(AccessEndpointClassifier.Classify(entityManager, entity));
            builder.Append(" plainRoadLike=").Append(FormatBool(IsPlainRoadLike(entityManager, entity)));
            builder.Append(" buildingAnchor=").Append(FormatBool(AccessEndpointClassifier.HasBuildingServiceAnchor(entityManager, entity)));
            builder.Append(" roadAllowanceAnchor=").Append(FormatBool(AccessEndpointClassifier.HasBuildingServiceRoadAllowanceAnchor(entityManager, entity)));
            builder.Append(" targetMatchRoot=").Append(FormatBool(AccessEndpointClassifier.LaneMatchesBuildingServiceTarget(entityManager, entity, surveyRoot)));
            builder.Append(" ownerChain=").Append(BuildOwnerChainText(entityManager, entity));

            AppendCarLaneSummary(builder, entityManager, entity);
            AppendConnectionLaneSummary(builder, entityManager, entity);
            AppendParkingGarageSummary(builder, entityManager, entity);
            AppendSpawnLocationSummary(builder, entityManager, entity);

            return builder.ToString();
        }

        private static void AppendCarLaneSummary(
            StringBuilder builder,
            EntityManager entityManager,
            Entity entity)
        {
            if (!entityManager.HasComponent<CarLane>(entity))
            {
                return;
            }

            CarLane carLane = entityManager.GetComponentData<CarLane>(entity);
            bool hasEdgeLane = entityManager.HasComponent<EdgeLane>(entity);
            builder.Append(" carLaneFlags=").Append(FormatEnumFlags(carLane.m_Flags));
            builder.Append(" edgeLane=").Append(FormatBool(hasEdgeLane));
        }

        private static void AppendConnectionLaneSummary(
            StringBuilder builder,
            EntityManager entityManager,
            Entity entity)
        {
            if (!entityManager.HasComponent<ConnectionLane>(entity))
            {
                return;
            }

            ConnectionLane connectionLane = entityManager.GetComponentData<ConnectionLane>(entity);
            builder.Append(" connectionFlags=").Append(FormatEnumFlags(connectionLane.m_Flags));
        }

        private static void AppendParkingGarageSummary(
            StringBuilder builder,
            EntityManager entityManager,
            Entity entity)
        {
            if (entityManager.HasComponent<ParkingLane>(entity))
            {
                builder.Append(" parkingLane=true");
            }

            if (entityManager.HasComponent<GarageLane>(entity))
            {
                builder.Append(" garageLane=true");
            }
        }

        private static void AppendSpawnLocationSummary(
            StringBuilder builder,
            EntityManager entityManager,
            Entity entity)
        {
            if (!entityManager.HasComponent<SpawnLocation>(entity) ||
                !entityManager.HasComponent<PrefabRef>(entity))
            {
                return;
            }

            Entity prefab = entityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
            builder.Append(" spawnLocation=true");
            builder.Append(" prefab=").Append(FormatEntity(prefab));

            if (prefab != Entity.Null &&
                entityManager.HasComponent<PrefabSpawnLocationData>(prefab))
            {
                PrefabSpawnLocationData spawnLocationData =
                    entityManager.GetComponentData<PrefabSpawnLocationData>(prefab);
                builder.Append(" spawnConnectionType=").Append(spawnLocationData.m_ConnectionType);
            }
        }

        private static string DescribeRootKind(EntityManager entityManager, Entity entity)
        {
            if (entityManager.HasComponent<Building>(entity))
            {
                return "Building";
            }

            if (entityManager.HasComponent<ServiceUpgrade>(entity))
            {
                return "ServiceUpgrade";
            }

            return "Unknown";
        }

        private static string BuildOwnerChainText(EntityManager entityManager, Entity entity)
        {
            StringBuilder builder = new StringBuilder(96);
            byte depth = 0;
            Entity current = entity;
            while (current != Entity.Null && depth < 12)
            {
                if (depth > 0)
                {
                    builder.Append(">");
                }

                builder.Append(FormatEntity(current));

                if (!entityManager.HasComponent<Owner>(current))
                {
                    break;
                }

                current = entityManager.GetComponentData<Owner>(current).m_Owner;
                depth += 1;
            }

            return builder.ToString();
        }

        private static bool IsPlainRoadLike(EntityManager entityManager, Entity entity)
        {
            bool hasCarLane = entityManager.HasComponent<CarLane>(entity);
            if (entityManager.HasComponent<EdgeLane>(entity) && hasCarLane)
            {
                return true;
            }

            if (entityManager.HasComponent<ConnectionLane>(entity))
            {
                ConnectionLane connectionLane = entityManager.GetComponentData<ConnectionLane>(entity);
                bool parkingAccess = (connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0;
                bool roadConnection = (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0;
                bool cargoConnection = (connectionLane.m_Flags & ConnectionLaneFlags.AllowCargo) != 0;
                return roadConnection && !parkingAccess && !cargoConnection;
            }

            if (entityManager.HasComponent<SpawnLocation>(entity) &&
                entityManager.HasComponent<PrefabRef>(entity))
            {
                Entity prefab = entityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
                if (prefab != Entity.Null &&
                    entityManager.HasComponent<PrefabSpawnLocationData>(prefab))
                {
                    return entityManager.GetComponentData<PrefabSpawnLocationData>(prefab).m_ConnectionType ==
                        RouteConnectionType.Road;
                }
            }

            return false;
        }

        private static string FormatEntity(Entity entity)
        {
            return entity == Entity.Null ? "None" : $"#{entity.Index}:v{entity.Version}";
        }

        private static string FormatBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string FormatEnumFlags<TEnum>(TEnum value)
            where TEnum : struct
        {
            string text = value.ToString();
            return string.IsNullOrWhiteSpace(text) ? "None" : text;
        }
    }
}
