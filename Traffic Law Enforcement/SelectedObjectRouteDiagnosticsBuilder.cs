using System.Collections.Generic;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.UI.InGame;
using Game.Vehicles;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    internal struct SelectedObjectRouteDiagnosticsContext
    {
        public PublicTransportLaneVehicleTypeLookups TypeLookups;
        public ComponentLookup<Target> TargetData;
        public ComponentLookup<CurrentRoute> CurrentRouteData;
        public ComponentLookup<PathOwner> PathOwnerData;
        public ComponentLookup<RouteLane> RouteLaneData;
        public ComponentLookup<Waypoint> WaypointData;
        public ComponentLookup<Connected> ConnectedData;
        public BufferLookup<CarNavigationLane> NavigationLaneData;
        public BufferLookup<PathElement> PathElementData;
        public SelectedObjectDisplayFormatterContext Formatter;
        public RoutePenaltyInspectionContext InspectionContext;
    }

    internal readonly struct SelectedObjectRouteDiagnosticsData
    {
        public readonly bool HasDiagnostics;
        public readonly bool HasPathOwner;
        public readonly bool HasCurrentTarget;
        public readonly bool HasCurrentRoute;
        public readonly Entity CurrentRouteEntity;
        public readonly PathFlags CurrentPathFlags;
        public readonly string CurrentTargetText;
        public readonly string CurrentRouteText;
        public readonly string TargetRoadText;
        public readonly string StartOwnerRoadText;
        public readonly string EndOwnerRoadText;
        public readonly string DirectConnectText;
        public readonly string FullPathToTargetStartText;
        public readonly string NavigationLanesText;
        public readonly string PlannedPenaltiesText;
        public readonly string PenaltyTagsText;
        public readonly string ExplanationText;
        public readonly string WaypointRouteLaneText;
        public readonly string ConnectedStopText;

        public SelectedObjectRouteDiagnosticsData(
            bool hasDiagnostics,
            bool hasPathOwner,
            bool hasCurrentTarget,
            bool hasCurrentRoute,
            Entity currentRouteEntity,
            PathFlags currentPathFlags,
            string currentTargetText,
            string currentRouteText,
            string targetRoadText,
            string startOwnerRoadText,
            string endOwnerRoadText,
            string directConnectText,
            string fullPathToTargetStartText,
            string navigationLanesText,
            string plannedPenaltiesText,
            string penaltyTagsText,
            string explanationText,
            string waypointRouteLaneText,
            string connectedStopText)
        {
            HasDiagnostics = hasDiagnostics;
            HasPathOwner = hasPathOwner;
            HasCurrentTarget = hasCurrentTarget;
            HasCurrentRoute = hasCurrentRoute;
            CurrentRouteEntity = currentRouteEntity;
            CurrentPathFlags = currentPathFlags;
            CurrentTargetText = currentTargetText;
            CurrentRouteText = currentRouteText;
            TargetRoadText = targetRoadText;
            StartOwnerRoadText = startOwnerRoadText;
            EndOwnerRoadText = endOwnerRoadText;
            DirectConnectText = directConnectText;
            FullPathToTargetStartText = fullPathToTargetStartText;
            NavigationLanesText = navigationLanesText;
            PlannedPenaltiesText = plannedPenaltiesText;
            PenaltyTagsText = penaltyTagsText;
            ExplanationText = explanationText;
            WaypointRouteLaneText = waypointRouteLaneText;
            ConnectedStopText = connectedStopText;
        }
    }

    internal static class SelectedObjectRouteDiagnosticsBuilder
    {
        internal static SelectedObjectRouteDiagnosticsData Build(
            SelectedObjectResolveResult resolveResult,
            bool tleReady,
            Entity vehicle,
            Entity currentLaneEntity,
            ref SelectedObjectRouteDiagnosticsContext context)
        {
            if (!tleReady ||
                resolveResult.VehicleKind != SelectedObjectKind.RoadCar ||
                vehicle == Entity.Null)
            {
                return default;
            }

            bool hasCurrentTarget =
                context.TargetData.TryGetComponent(vehicle, out Target targetData) &&
                targetData.m_Target != Entity.Null;
            Entity targetEntity = hasCurrentTarget ? targetData.m_Target : Entity.Null;

            bool hasCurrentRoute =
                context.CurrentRouteData.TryGetComponent(vehicle, out CurrentRoute currentRouteData) &&
                currentRouteData.m_Route != Entity.Null;
            Entity currentRouteEntity = hasCurrentRoute ? currentRouteData.m_Route : Entity.Null;
            Entity selectableCurrentRouteEntity =
                ResolveSelectableCurrentRouteEntity(currentRouteEntity, ref context);

            bool hasPathOwner =
                context.PathOwnerData.TryGetComponent(vehicle, out PathOwner pathOwner);
            PathFlags currentPathFlags = hasPathOwner ? pathOwner.m_State : default;

            bool hasNavigationLanes =
                context.NavigationLaneData.TryGetBuffer(vehicle, out DynamicBuffer<CarNavigationLane> navigationLanes);
            bool hasPathElements =
                context.PathElementData.TryGetBuffer(vehicle, out DynamicBuffer<PathElement> pathElements);

            RoutePenaltyInspectionResult inspection =
                RoutePenaltyInspection.InspectCurrentRoute(
                    vehicle,
                    currentLaneEntity,
                    navigationLanes,
                    hasNavigationLanes,
                    ref context.InspectionContext,
                    captureDebugStrings: true);

            return new SelectedObjectRouteDiagnosticsData(
                hasDiagnostics: true,
                hasPathOwner: hasPathOwner,
                hasCurrentTarget: hasCurrentTarget,
                hasCurrentRoute: hasCurrentRoute,
                currentRouteEntity: selectableCurrentRouteEntity,
                currentPathFlags: currentPathFlags,
                currentTargetText: BuildCurrentTargetText(targetEntity, ref context),
                currentRouteText: BuildCurrentRouteText(selectableCurrentRouteEntity, currentRouteEntity, ref context),
                targetRoadText: BuildTargetRoadText(targetEntity, ref context),
                startOwnerRoadText: BuildRouteLaneOwnerRoadText(targetEntity, useStartLane: true, ref context),
                endOwnerRoadText: BuildRouteLaneOwnerRoadText(targetEntity, useStartLane: false, ref context),
                directConnectText: BuildCurrentToTargetStartDirectConnectText(currentLaneEntity, targetEntity, navigationLanes, hasNavigationLanes, ref context),
                fullPathToTargetStartText: BuildFullPathToTargetStartText(currentLaneEntity, targetEntity, navigationLanes, hasNavigationLanes, pathElements, hasPathElements, ref context),
                navigationLanesText: BuildNavigationLanesText(currentLaneEntity, navigationLanes, hasNavigationLanes, ref context),
                plannedPenaltiesText: NormalizeInspectionText(inspection.Breakdown),
                penaltyTagsText: NormalizeInspectionText(inspection.Tags),
                explanationText: BuildRouteDecisionExplanation(vehicle, currentLaneEntity, hasCurrentTarget, targetEntity, hasCurrentRoute, inspection, ref context),
                waypointRouteLaneText: BuildWaypointRouteLaneText(targetEntity, ref context),
                connectedStopText: BuildConnectedStopText(targetEntity, ref context));
        }

        private static string BuildNavigationLanesText(Entity currentLaneEntity, DynamicBuffer<CarNavigationLane> navigationLanes, bool hasNavigationLanes, ref SelectedObjectRouteDiagnosticsContext context, int maxPreviewLanes = 4)
        {
            if (!hasNavigationLanes || navigationLanes.Length == 0)
            {
                return "none";
            }

            List<string> lines = new List<string>(maxPreviewLanes + 2);
            int totalUpcoming = 0;

            for (int index = 0; index < navigationLanes.Length; index++)
            {
                Entity lane = navigationLanes[index].m_Lane;
                if (lane == Entity.Null)
                {
                    continue;
                }

                if (index == 0 && lane == currentLaneEntity)
                {
                    continue;
                }

                totalUpcoming += 1;
                if (lines.Count >= maxPreviewLanes)
                {
                    continue;
                }

                lines.Add($"{lines.Count + 1}. {SelectedObjectDisplayFormatter.BuildLaneDisplayText(lane, ref context.Formatter)}");
            }

            if (totalUpcoming == 0)
            {
                return "none";
            }

            List<string> summary = new List<string>(lines.Count + 2) { $"{totalUpcoming} total" };
            summary.AddRange(lines);
            if (totalUpcoming > lines.Count)
            {
                summary.Add($"(+{totalUpcoming - lines.Count} more)");
            }

            return string.Join("\n", summary.ToArray());
        }

        private static string BuildCurrentTargetText(Entity targetEntity, ref SelectedObjectRouteDiagnosticsContext context)
        {
            if (targetEntity == Entity.Null)
            {
                return SelectedObjectDisplayFormatter.FormatEntityOrNone(Entity.Null);
            }

            string text = SelectedObjectDisplayFormatter.FormatEntityOrNone(targetEntity);
            string targetName = TryGetCurrentTargetName(targetEntity, ref context);
            if (context.WaypointData.TryGetComponent(targetEntity, out Waypoint waypoint))
            {
                text += $" [waypoint {waypoint.m_Index}]";
            }

            if (!string.IsNullOrWhiteSpace(targetName))
            {
                text += $" \"{targetName}\"";
            }

            return text;
        }

        private static string BuildCurrentRouteText(Entity displayRouteEntity, Entity rawCurrentRouteEntity, ref SelectedObjectRouteDiagnosticsContext context)
        {
            string customName = TryGetCurrentRouteCustomName(displayRouteEntity, rawCurrentRouteEntity, ref context);
            if (!string.IsNullOrWhiteSpace(customName))
            {
                return customName;
            }

            string builtName = TryGetCurrentRouteBuiltName(displayRouteEntity, rawCurrentRouteEntity, ref context);
            if (!string.IsNullOrWhiteSpace(builtName))
            {
                return builtName;
            }

            string renderedName = TryGetCurrentRouteRenderedName(displayRouteEntity, rawCurrentRouteEntity, ref context);
            if (!string.IsNullOrWhiteSpace(renderedName))
            {
                return renderedName;
            }

            Entity routeEntity =
                displayRouteEntity != Entity.Null
                    ? displayRouteEntity
                    : rawCurrentRouteEntity;
            if (routeEntity == Entity.Null)
            {
                return SelectedObjectDisplayFormatter.FormatEntityOrNone(Entity.Null);
            }
            return SelectedObjectDisplayFormatter.FormatEntityOrNone(routeEntity);
        }

        private static Entity ResolveSelectableCurrentRouteEntity(Entity currentRouteEntity, ref SelectedObjectRouteDiagnosticsContext context)
        {
            if (currentRouteEntity == Entity.Null)
            {
                return Entity.Null;
            }

            EntityManager entityManager = context.Formatter.EntityManager;
            Entity candidate = currentRouteEntity;

            for (int depth = 0; depth < 16 && candidate != Entity.Null; depth++)
            {
                if (IsSelectableLineEntity(candidate, entityManager))
                {
                    return candidate;
                }

                if (!context.Formatter.OwnerData.TryGetComponent(candidate, out Owner owner) ||
                    owner.m_Owner == Entity.Null ||
                    owner.m_Owner == candidate)
                {
                    break;
                }

                candidate = owner.m_Owner;
            }

            return Entity.Null;
        }

        private static bool IsSelectableLineEntity(Entity entity, EntityManager entityManager)
        {
            if (entity == Entity.Null ||
                !entityManager.Exists(entity) ||
                !entityManager.HasComponent<Route>(entity) ||
                !entityManager.HasComponent<PrefabRef>(entity) ||
                !entityManager.HasBuffer<RouteWaypoint>(entity) ||
                !entityManager.HasBuffer<RouteSegment>(entity) ||
                !entityManager.HasBuffer<RouteVehicle>(entity) ||
                (!entityManager.HasComponent<TransportLine>(entity) &&
                 !entityManager.HasComponent<WorkRoute>(entity)))
            {
                return false;
            }

            int elementIndex = 0;
            return SelectedInfoUISystem.TryGetPosition(
                entity,
                entityManager,
                ref elementIndex,
                out _,
                out _,
                out _,
                out _,
                reinterpolate: true);
        }

        private static string TryGetCurrentRouteCustomName(Entity displayRouteEntity, Entity rawCurrentRouteEntity, ref SelectedObjectRouteDiagnosticsContext context)
        {
            string displayName = SelectedObjectDisplayFormatter.TryGetCustomName(displayRouteEntity, ref context.Formatter);
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            if (rawCurrentRouteEntity != Entity.Null &&
                rawCurrentRouteEntity != displayRouteEntity)
            {
                return SelectedObjectDisplayFormatter.TryGetCustomName(rawCurrentRouteEntity, ref context.Formatter);
            }

            return string.Empty;
        }

        private static string TryGetCurrentRouteBuiltName(Entity displayRouteEntity, Entity rawCurrentRouteEntity, ref SelectedObjectRouteDiagnosticsContext context)
        {
            string displayName = SelectedObjectDisplayFormatter.TryBuildRouteName(displayRouteEntity, ref context.Formatter);
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            if (rawCurrentRouteEntity != Entity.Null &&
                rawCurrentRouteEntity != displayRouteEntity)
            {
                return SelectedObjectDisplayFormatter.TryBuildRouteName(rawCurrentRouteEntity, ref context.Formatter);
            }

            return string.Empty;
        }

        private static string TryGetCurrentRouteRenderedName(Entity displayRouteEntity, Entity rawCurrentRouteEntity, ref SelectedObjectRouteDiagnosticsContext context)
        {
            string displayName = SelectedObjectDisplayFormatter.TryGetRenderedName(displayRouteEntity, ref context.Formatter);
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            if (rawCurrentRouteEntity != Entity.Null &&
                rawCurrentRouteEntity != displayRouteEntity)
            {
                return SelectedObjectDisplayFormatter.TryGetRenderedName(rawCurrentRouteEntity, ref context.Formatter);
            }

            return string.Empty;
        }

        private static string BuildWaypointRouteLaneText(Entity targetEntity, ref SelectedObjectRouteDiagnosticsContext context)
        {
            if (targetEntity == Entity.Null ||
                !context.RouteLaneData.TryGetComponent(targetEntity, out RouteLane routeLane))
            {
                return string.Empty;
            }

            return
                $"start: {SelectedObjectDisplayFormatter.BuildLaneDisplayText(routeLane.m_StartLane, ref context.Formatter)}\n" +
                $"end: {SelectedObjectDisplayFormatter.BuildLaneDisplayText(routeLane.m_EndLane, ref context.Formatter)}\n" +
                $"curve: {routeLane.m_StartCurvePos:0.###} -> {routeLane.m_EndCurvePos:0.###}";
        }

        private static string BuildConnectedStopText(Entity targetEntity, ref SelectedObjectRouteDiagnosticsContext context)
        {
            if (targetEntity == Entity.Null ||
                !context.ConnectedData.TryGetComponent(targetEntity, out Connected connected))
            {
                return string.Empty;
            }

            return SelectedObjectDisplayFormatter.FormatNamedEntity(connected.m_Connected, ref context.Formatter);
        }

        private static string BuildTargetRoadText(Entity targetEntity, ref SelectedObjectRouteDiagnosticsContext context)
        {
            if (targetEntity == Entity.Null)
            {
                return string.Empty;
            }

            if (context.ConnectedData.TryGetComponent(targetEntity, out Connected connected) &&
                SelectedObjectDisplayFormatter.TryGetRoadEntityFromAddressable(connected.m_Connected, out Entity stopRoad, ref context.Formatter))
            {
                return SelectedObjectDisplayFormatter.FormatRoadName(stopRoad, ref context.Formatter);
            }

            if (context.RouteLaneData.TryGetComponent(targetEntity, out RouteLane routeLane))
            {
                Entity roadEntity = SelectedObjectDisplayFormatter.ResolveRoadEntityFromLane(routeLane.m_StartLane, ref context.Formatter);
                if (roadEntity != Entity.Null)
                {
                    return SelectedObjectDisplayFormatter.FormatRoadName(roadEntity, ref context.Formatter);
                }
            }

            return string.Empty;
        }

        private static string TryGetCurrentTargetName(Entity targetEntity, ref SelectedObjectRouteDiagnosticsContext context)
        {
            string renderedName = SelectedObjectDisplayFormatter.TryGetRenderedName(targetEntity, ref context.Formatter);
            if (!string.IsNullOrWhiteSpace(renderedName))
            {
                return renderedName;
            }

            if (context.ConnectedData.TryGetComponent(targetEntity, out Connected connected))
            {
                renderedName = SelectedObjectDisplayFormatter.TryGetRenderedName(connected.m_Connected, ref context.Formatter);
                if (!string.IsNullOrWhiteSpace(renderedName))
                {
                    return renderedName;
                }
            }

            Entity roadEntity = Entity.Null;
            if (context.RouteLaneData.TryGetComponent(targetEntity, out RouteLane routeLane))
            {
                roadEntity = SelectedObjectDisplayFormatter.ResolveRoadEntityFromLane(routeLane.m_StartLane, ref context.Formatter);
            }

            return roadEntity == Entity.Null
                ? string.Empty
                : SelectedObjectDisplayFormatter.TryGetRenderedName(roadEntity, ref context.Formatter);
        }

        private static string BuildRouteLaneOwnerRoadText(Entity targetEntity, bool useStartLane, ref SelectedObjectRouteDiagnosticsContext context)
        {
            if (targetEntity == Entity.Null ||
                !context.RouteLaneData.TryGetComponent(targetEntity, out RouteLane routeLane))
            {
                return string.Empty;
            }

            Entity lane = useStartLane ? routeLane.m_StartLane : routeLane.m_EndLane;
            if (lane == Entity.Null)
            {
                return string.Empty;
            }

            Entity roadEntity = SelectedObjectDisplayFormatter.ResolveRoadEntityFromLane(lane, ref context.Formatter);
            if (roadEntity != Entity.Null)
            {
                return SelectedObjectDisplayFormatter.FormatRoadName(roadEntity, ref context.Formatter);
            }

            string laneOwnerName = SelectedObjectDisplayFormatter.TryGetLaneOwnerName(lane, ref context.Formatter);
            return string.IsNullOrWhiteSpace(laneOwnerName) ? string.Empty : laneOwnerName;
        }

        private static string BuildCurrentToTargetStartDirectConnectText(Entity currentLaneEntity, Entity targetEntity, DynamicBuffer<CarNavigationLane> navigationLanes, bool hasNavigationLanes, ref SelectedObjectRouteDiagnosticsContext context)
        {
            if (targetEntity == Entity.Null ||
                !context.RouteLaneData.TryGetComponent(targetEntity, out RouteLane routeLane) ||
                routeLane.m_StartLane == Entity.Null)
            {
                return SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteDirectConnectMissingStartLocaleId, "Target start lane unavailable.");
            }

            if (currentLaneEntity == routeLane.m_StartLane)
            {
                return SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteDirectConnectAlreadyOnStartLocaleId, "Yes, already on the target start lane.");
            }

            if (!hasNavigationLanes || navigationLanes.Length == 0)
            {
                return SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteDirectConnectNoPreviewLocaleId, "No, no current navigation preview reaches the target start lane.");
            }

            List<Entity> upcomingLanes = new List<Entity>(navigationLanes.Length);
            bool skippedLeadingCurrentLane = false;

            for (int index = 0; index < navigationLanes.Length; index++)
            {
                Entity lane = navigationLanes[index].m_Lane;
                if (lane == Entity.Null)
                {
                    continue;
                }

                if (!skippedLeadingCurrentLane &&
                    currentLaneEntity != Entity.Null &&
                    lane == currentLaneEntity)
                {
                    skippedLeadingCurrentLane = true;
                    continue;
                }

                upcomingLanes.Add(lane);
            }

            if (upcomingLanes.Count == 0)
            {
                return SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteDirectConnectNoPreviewLocaleId, "No, no current navigation preview reaches the target start lane.");
            }

            if (upcomingLanes[0] == routeLane.m_StartLane)
            {
                return SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteDirectConnectNextHopLocaleId, "Yes, the next hop reaches the target start lane.");
            }

            for (int index = 1; index < upcomingLanes.Count; index++)
            {
                if (upcomingLanes[index] == routeLane.m_StartLane)
                {
                    return string.Format(
                        SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteDirectConnectViaFormatLocaleId, "No, reaches the target start lane after {0} intermediate lane(s) via {1}."),
                        index,
                        SelectedObjectDisplayFormatter.BuildLaneDisplayText(upcomingLanes[0], ref context.Formatter));
                }
            }

            return SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteDirectConnectNoPreviewLocaleId, "No, no current navigation preview reaches the target start lane.");
        }

        private static string BuildFullPathToTargetStartText(Entity currentLaneEntity, Entity targetEntity, DynamicBuffer<CarNavigationLane> navigationLanes, bool hasNavigationLanes, DynamicBuffer<PathElement> pathElements, bool hasPathElements, ref SelectedObjectRouteDiagnosticsContext context)
        {
            if (targetEntity == Entity.Null ||
                !context.RouteLaneData.TryGetComponent(targetEntity, out RouteLane routeLane) ||
                routeLane.m_StartLane == Entity.Null)
            {
                return SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteFullPathMissingStartLocaleId, "Target start lane unavailable.");
            }

            if (currentLaneEntity == routeLane.m_StartLane)
            {
                return SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteFullPathContainsStartLocaleId, "Yes, current full path contains the target start lane.");
            }

            if (hasNavigationLanes)
            {
                for (int index = 0; index < navigationLanes.Length; index++)
                {
                    if (navigationLanes[index].m_Lane == routeLane.m_StartLane)
                    {
                        return SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteFullPathContainsStartLocaleId, "Yes, current full path contains the target start lane.");
                    }
                }
            }

            if (hasPathElements)
            {
                for (int index = 0; index < pathElements.Length; index++)
                {
                    if (pathElements[index].m_Target == routeLane.m_StartLane)
                    {
                        return SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteFullPathContainsStartLocaleId, "Yes, current full path contains the target start lane.");
                    }
                }
            }

            return SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteFullPathMissingLocaleId, "No, current full path does not contain the target start lane.");
        }

        private static string BuildRouteDecisionExplanation(Entity vehicle, Entity currentLaneEntity, bool hasCurrentTarget, Entity targetEntity, bool hasCurrentRoute, RoutePenaltyInspectionResult inspection, ref SelectedObjectRouteDiagnosticsContext context)
        {
            if (!hasCurrentRoute)
            {
                return SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteExplanationNoCurrentRouteLocaleId, "No current route is attached to this vehicle.");
            }

            if (!hasCurrentTarget)
            {
                return SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteExplanationNoCurrentTargetLocaleId, "No current target is attached to this vehicle.");
            }

            List<string> parts = new List<string>(3);
            bool hasPrimaryExplanation = false;

            if (TryHasWaypointRouteLaneMismatch(targetEntity, currentLaneEntity, ref context))
            {
                parts.Add(SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteExplanationWaypointAlignmentLocaleId, "Vehicle is aligning for the next waypoint / stop approach lane."));
                hasPrimaryExplanation = true;
            }

            if (inspection.Profile.HasAnyPenalty)
            {
                string tagSummary = NormalizeInspectionText(inspection.Tags);
                string format =
                    parts.Count == 0
                        ? SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteExplanationPenaltyPrimaryFormatLocaleId, "Current planned route contains deterrence tags: {0}.")
                        : SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteExplanationPenaltyModifierFormatLocaleId, "Current planned route also contains deterrence tags: {0}.");
                parts.Add(string.Format(format, tagSummary));
                hasPrimaryExplanation = true;
            }

            if (IsRoadPublicTransportVehicle(vehicle, ref context) &&
                inspection.PublicTransportLanePolicyResolved &&
                inspection.AllowedOnPublicTransportLane)
            {
                parts.Add(SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteExplanationPtPermissiveLocaleId, "PT-lane policy is currently permissive for this vehicle."));
            }

            if (!hasPrimaryExplanation)
            {
                parts.Add(SelectedObjectBridgeSystem.LocalizeText(SelectedObjectBridgeSystem.kRouteExplanationGenericFallbackLocaleId, "No route-target mismatch or current TLE penalty tag was identified; current behavior is most likely vanilla lane-group alignment."));
            }

            return string.Join(" ", parts.ToArray());
        }

        private static bool TryHasWaypointRouteLaneMismatch(Entity targetEntity, Entity currentLaneEntity, ref SelectedObjectRouteDiagnosticsContext context)
        {
            if (targetEntity == Entity.Null ||
                currentLaneEntity == Entity.Null ||
                !context.RouteLaneData.TryGetComponent(targetEntity, out RouteLane routeLane) ||
                routeLane.m_StartLane == Entity.Null)
            {
                return false;
            }

            return routeLane.m_StartLane != currentLaneEntity;
        }

        private static bool IsRoadPublicTransportVehicle(Entity vehicle, ref SelectedObjectRouteDiagnosticsContext context)
        {
            PublicTransportLaneVehicleCategory categories =
                PublicTransportLanePolicy.GetVanillaAuthorizedCategories(vehicle, ref context.TypeLookups);
            return (categories & PublicTransportLaneVehicleCategory.RoadPublicTransportVehicle) != 0;
        }

        private static string NormalizeInspectionText(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text == "none")
            {
                return SelectedObjectDisplayFormatter.FormatEntityOrNone(Entity.Null);
            }

            return text.Trim();
        }
    }
}
