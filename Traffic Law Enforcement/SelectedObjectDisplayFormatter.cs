using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.UI;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    internal struct SelectedObjectDisplayFormatterContext
    {
        public EntityManager EntityManager;
        public NameSystem NameSystem;
        public ComponentLookup<Owner> OwnerData;
        public ComponentLookup<Aggregated> AggregatedData;
        public ComponentLookup<SlaveLane> SlaveLaneData;
        public ComponentLookup<CarLane> CarLaneData;
        public ComponentLookup<ParkingLane> ParkingLaneData;
        public ComponentLookup<GarageLane> GarageLaneData;
        public ComponentLookup<ConnectionLane> ConnectionLaneData;
    }

    internal static class SelectedObjectDisplayFormatter
    {
        internal static string BuildLaneDisplayText(Entity lane, ref SelectedObjectDisplayFormatterContext context)
        {
            if (lane == Entity.Null)
            {
                return FormatEntityOrNone(Entity.Null);
            }

            string roadName = BuildRoadNameFromLane(lane, ref context);
            string ptSuffix = IsPublicTransportOnlyLane(lane, ref context)
                ? " [PT]"
                : string.Empty;
            string prefix = string.IsNullOrWhiteSpace(roadName)
                ? FormatEntityOrNone(lane)
                : roadName;

            if (context.ConnectionLaneData.HasComponent(lane))
            {
                return $"{prefix}, connection{ptSuffix}";
            }

            if (context.ParkingLaneData.HasComponent(lane))
            {
                return $"{prefix}, parking{ptSuffix}";
            }

            if (context.GarageLaneData.HasComponent(lane))
            {
                return $"{prefix}, garage{ptSuffix}";
            }

            if (TryBuildLaneOrdinal(lane, out int laneNumber, out int laneCount, ref context))
            {
                return $"{prefix}, {laneNumber}/{laneCount}{ptSuffix}";
            }

            return prefix + ptSuffix;
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
            string renderedName = TryGetRenderedName(roadEntity, ref context);
            return string.IsNullOrWhiteSpace(renderedName)
                ? FormatEntityOrNone(roadEntity)
                : renderedName;
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
            string entityText = FormatEntityOrNone(entity);
            string renderedName = TryGetRenderedName(entity, ref context);
            return string.IsNullOrWhiteSpace(renderedName)
                ? entityText
                : $"{entityText} \"{renderedName}\"";
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

            string renderedName = context.NameSystem.GetRenderedLabelName(entity);
            return string.IsNullOrWhiteSpace(renderedName)
                ? string.Empty
                : renderedName.Trim();
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
