using System.Collections.Generic;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Routes;
using Game.UI;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    internal struct SelectedObjectDisplayFormatterContext
    {
        public EntityManager EntityManager;
        public NameSystem NameSystem;
        public Game.Prefabs.PrefabSystem PrefabSystem;
        public ComponentLookup<Owner> OwnerData;
        public ComponentLookup<Aggregated> AggregatedData;
        public ComponentLookup<SlaveLane> SlaveLaneData;
        public ComponentLookup<CarLane> CarLaneData;
        public ComponentLookup<ParkingLane> ParkingLaneData;
        public ComponentLookup<GarageLane> GarageLaneData;
        public ComponentLookup<ConnectionLane> ConnectionLaneData;
        public Dictionary<Entity, string> LaneDisplayTextCache;
        public Dictionary<Entity, string> RoadNameCache;
        public Dictionary<Entity, string> NamedEntityCache;
        public Dictionary<Entity, string> RenderedNameCache;
        public Dictionary<Entity, string> CustomNameCache;
        public Dictionary<Entity, string> RouteNameCache;
    }

    internal static class SelectedObjectDisplayFormatter
    {
        internal static string BuildLaneDisplayText(Entity lane, ref SelectedObjectDisplayFormatterContext context)
        {
            if (lane == Entity.Null)
            {
                return FormatEntityOrNone(Entity.Null);
            }

            if (context.LaneDisplayTextCache != null &&
                context.LaneDisplayTextCache.TryGetValue(lane, out string cachedLaneText))
            {
                return cachedLaneText;
            }

            string roadName = BuildRoadNameFromLane(lane, ref context);
            string ptSuffix = IsPublicTransportOnlyLane(lane, ref context)
                ? " [PT]"
                : string.Empty;
            string prefix = string.IsNullOrWhiteSpace(roadName)
                ? FormatEntityOrNone(lane)
                : roadName;

            string laneText;
            if (context.ConnectionLaneData.HasComponent(lane))
            {
                laneText = $"{prefix}, connection{ptSuffix}";
            }
            else if (context.ParkingLaneData.HasComponent(lane))
            {
                laneText = $"{prefix}, parking{ptSuffix}";
            }
            else if (context.GarageLaneData.HasComponent(lane))
            {
                laneText = $"{prefix}, garage{ptSuffix}";
            }
            else if (TryBuildLaneOrdinal(lane, out int laneNumber, out int laneCount, ref context))
            {
                laneText = $"{prefix}, {laneNumber}/{laneCount}{ptSuffix}";
            }
            else
            {
                laneText = prefix + ptSuffix;
            }

            EnsureCache(ref context.LaneDisplayTextCache).TryAdd(lane, laneText);
            return laneText;
        }

        internal static bool TryBuildLaneOrdinal(Entity lane, out int laneNumber, out int laneCount, ref SelectedObjectDisplayFormatterContext context)
        {
            if (context.SlaveLaneData.TryGetComponent(lane, out SlaveLane slaveLane) &&
                slaveLane.m_MaxIndex >= slaveLane.m_MinIndex &&
                slaveLane.m_SubIndex >= slaveLane.m_MinIndex &&
                slaveLane.m_SubIndex <= slaveLane.m_MaxIndex)
            {
                laneNumber = slaveLane.m_SubIndex - slaveLane.m_MinIndex + 1;
                laneCount = slaveLane.m_MaxIndex - slaveLane.m_MinIndex + 1;
                return laneCount > 0;
            }

            laneNumber = 0;
            laneCount = 0;
            return false;
        }

        internal static bool IsPublicTransportOnlyLane(Entity lane, ref SelectedObjectDisplayFormatterContext context)
        {
            return lane != Entity.Null &&
                context.CarLaneData.TryGetComponent(lane, out CarLane carLane) &&
                (carLane.m_Flags & Game.Net.CarLaneFlags.PublicOnly) != 0;
        }

        internal static string BuildRoadNameFromLane(Entity lane, ref SelectedObjectDisplayFormatterContext context)
        {
            Entity roadEntity = ResolveRoadEntityFromLane(lane, ref context);
            return roadEntity == Entity.Null
                ? string.Empty
                : FormatRoadName(roadEntity, ref context);
        }

        internal static string FormatRoadName(Entity roadEntity, ref SelectedObjectDisplayFormatterContext context)
        {
            if (roadEntity == Entity.Null)
            {
                return FormatEntityOrNone(Entity.Null);
            }

            if (context.RoadNameCache != null &&
                context.RoadNameCache.TryGetValue(roadEntity, out string cachedRoadName))
            {
                return cachedRoadName;
            }

            string renderedName = TryGetRenderedName(roadEntity, ref context);
            string roadName = string.IsNullOrWhiteSpace(renderedName)
                ? FormatEntityOrNone(roadEntity)
                : renderedName;
            EnsureCache(ref context.RoadNameCache).TryAdd(roadEntity, roadName);
            return roadName;
        }

        internal static string TryGetLaneOwnerName(Entity lane, ref SelectedObjectDisplayFormatterContext context)
        {
            if (lane == Entity.Null ||
                !context.OwnerData.TryGetComponent(lane, out Owner owner) ||
                owner.m_Owner == Entity.Null)
            {
                return string.Empty;
            }

            string renderedName = TryGetRenderedName(owner.m_Owner, ref context);
            if (!string.IsNullOrWhiteSpace(renderedName))
            {
                return renderedName;
            }

            if (context.AggregatedData.TryGetComponent(owner.m_Owner, out Aggregated aggregated) &&
                aggregated.m_Aggregate != Entity.Null)
            {
                return TryGetRenderedName(aggregated.m_Aggregate, ref context);
            }

            return string.Empty;
        }

        internal static string FormatNamedEntity(Entity entity, ref SelectedObjectDisplayFormatterContext context)
        {
            if (context.NamedEntityCache != null &&
                context.NamedEntityCache.TryGetValue(entity, out string cachedNamedEntity))
            {
                return cachedNamedEntity;
            }

            string entityText = FormatEntityOrNone(entity);
            string renderedName = TryGetRenderedName(entity, ref context);
            string namedEntity = string.IsNullOrWhiteSpace(renderedName)
                ? entityText
                : $"{entityText} \"{renderedName}\"";
            EnsureCache(ref context.NamedEntityCache).TryAdd(entity, namedEntity);
            return namedEntity;
        }

        internal static string FormatEntityOrNone(Entity entity)
        {
            return entity == Entity.Null
                ? SelectedObjectLocalization.LocalizeText(SelectedObjectPanelUISystem.kNoneLocaleId, "None")
                : $"#{entity.Index}:v{entity.Version}";
        }

        internal static string TryGetRenderedName(Entity entity, ref SelectedObjectDisplayFormatterContext context)
        {
            if (entity == Entity.Null || context.NameSystem == null)
            {
                return string.Empty;
            }

            if (context.RenderedNameCache != null &&
                context.RenderedNameCache.TryGetValue(entity, out string cachedRenderedName))
            {
                return cachedRenderedName;
            }

            string renderedName = context.NameSystem.GetRenderedLabelName(entity);
            string normalizedRenderedName = string.IsNullOrWhiteSpace(renderedName)
                ? string.Empty
                : renderedName.Trim();
            EnsureCache(ref context.RenderedNameCache).TryAdd(entity, normalizedRenderedName);
            return normalizedRenderedName;
        }

        internal static string TryGetCustomName(Entity entity, ref SelectedObjectDisplayFormatterContext context)
        {
            if (entity == Entity.Null || context.NameSystem == null)
            {
                return string.Empty;
            }

            if (context.CustomNameCache != null &&
                context.CustomNameCache.TryGetValue(entity, out string cachedCustomName))
            {
                return cachedCustomName;
            }

            string normalizedCustomName =
                context.NameSystem.TryGetCustomName(entity, out string customName) &&
                !string.IsNullOrWhiteSpace(customName)
                    ? customName.Trim()
                    : string.Empty;
            EnsureCache(ref context.CustomNameCache).TryAdd(entity, normalizedCustomName);
            return normalizedCustomName;
        }

        internal static string TryBuildRouteName(Entity routeEntity, ref SelectedObjectDisplayFormatterContext context)
        {
            if (routeEntity == Entity.Null ||
                context.PrefabSystem == null ||
                !context.EntityManager.HasComponent<Game.Prefabs.PrefabRef>(routeEntity))
            {
                return string.Empty;
            }

            if (context.RouteNameCache != null &&
                context.RouteNameCache.TryGetValue(routeEntity, out string cachedRouteName))
            {
                return cachedRouteName;
            }

            Game.Prefabs.PrefabRef prefabRef = context.EntityManager.GetComponentData<Game.Prefabs.PrefabRef>(routeEntity);
            if (!context.PrefabSystem.TryGetPrefab<Game.Prefabs.RoutePrefab>(prefabRef.m_Prefab, out Game.Prefabs.RoutePrefab routePrefab))
            {
                return string.Empty;
            }

            string routeNumberText =
                context.EntityManager.HasComponent<RouteNumber>(routeEntity)
                    ? context.EntityManager.GetComponentData<RouteNumber>(routeEntity).m_Number.ToString()
                    : string.Empty;

            if (routeNumberText == "0")
            {
                routeNumberText = string.Empty;
            }

            string routeName = string.IsNullOrWhiteSpace(routeNumberText)
                ? routePrefab.name
                : $"{routePrefab.name} {routeNumberText}";
            EnsureCache(ref context.RouteNameCache).TryAdd(routeEntity, routeName);
            return routeName;
        }

        private static Dictionary<Entity, string> EnsureCache(ref Dictionary<Entity, string> cache)
        {
            if (cache == null)
            {
                cache = new Dictionary<Entity, string>();
            }

            return cache;
        }

        internal static bool TryGetRoadEntityFromAddressable(Entity entity, out Entity road, ref SelectedObjectDisplayFormatterContext context)
        {
            if (entity != Entity.Null &&
                BuildingUtils.GetAddress(context.EntityManager, entity, out road, out _))
            {
                return true;
            }

            road = Entity.Null;
            return false;
        }

        internal static Entity ResolveRoadEntityFromLane(Entity lane, ref SelectedObjectDisplayFormatterContext context)
        {
            if (lane == Entity.Null ||
                !context.OwnerData.TryGetComponent(lane, out Owner owner))
            {
                return Entity.Null;
            }

            Entity laneOwner = owner.m_Owner;
            if (laneOwner == Entity.Null)
            {
                return Entity.Null;
            }

            if (context.AggregatedData.TryGetComponent(laneOwner, out Aggregated aggregated) &&
                aggregated.m_Aggregate != Entity.Null)
            {
                return aggregated.m_Aggregate;
            }

            return laneOwner;
        }
    }
}
