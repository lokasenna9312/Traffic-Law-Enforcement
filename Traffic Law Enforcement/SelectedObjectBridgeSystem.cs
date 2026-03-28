using System.Collections.Generic;
using Game;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Routes;
using Game.SceneFlow;
using Game.Vehicles;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public partial class SelectedObjectBridgeSystem : GameSystemBase
    {
        internal const string kClassificationRoadCarLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Classification.RoadCar";
        internal const string kClassificationParkedRoadCarLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Classification.ParkedRoadCar";
        internal const string kClassificationRailVehicleLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Classification.RailVehicle";
        internal const string kClassificationParkedRailVehicleLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Classification.ParkedRailVehicle";
        internal const string kClassificationTramLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Classification.Tram";
        internal const string kClassificationParkedTramLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Classification.ParkedTram";
        internal const string kClassificationTrainLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Classification.Train";
        internal const string kClassificationParkedTrainLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Classification.ParkedTrain";
        internal const string kClassificationSubwayLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Classification.Subway";
        internal const string kClassificationParkedSubwayLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Classification.ParkedSubway";
        internal const string kClassificationOtherVehicleLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Classification.OtherVehicle";
        internal const string kRoleRoadPublicTransportLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.RoadPublicTransportVehicle";
        internal const string kRoleTaxiLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.Taxi";
        internal const string kRolePoliceCarLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.PoliceCar";
        internal const string kRoleFireEngineLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.FireEngine";
        internal const string kRoleAmbulanceLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.Ambulance";
        internal const string kRoleGarbageTruckLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.GarbageTruck";
        internal const string kRolePostVanLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.PostVan";
        internal const string kRoleRoadMaintenanceVehicleLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.RoadMaintenanceVehicle";
        internal const string kRoleSnowplowLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.Snowplow";
        internal const string kRoleVehicleMaintenanceVehicleLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.VehicleMaintenanceVehicle";
        internal const string kRolePersonalCarLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.PersonalCar";
        internal const string kRoleDeliveryTruckLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.DeliveryTruck";
        internal const string kRoleCargoTransportLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.CargoTransport";
        internal const string kRoleHearseLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.Hearse";
        internal const string kRolePrisonerTransportLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.PrisonerTransport";
        internal const string kRoleParkMaintenanceVehicleLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.ParkMaintenanceVehicle";
        internal const string kRoleUnclassifiedRoadVehicleLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.UnclassifiedRoadVehicle";
        internal const string kRoleEmergencyQualifierLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.EmergencyQualifier";
        internal const string kReasonNoneRecordedLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.NoneRecorded";
        internal const string kReasonPublicTransportLaneRevokedByModFormatLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.PublicTransportLaneRevokedByModFormat";
        internal const string kReasonPublicTransportLaneMissingVanillaCategoriesFormatLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.PublicTransportLaneMissingVanillaCategoriesFormat";
        internal const string kReasonPublicTransportLaneMissingGrantedRoleFormatLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.PublicTransportLaneMissingGrantedRoleFormat";
        internal const string kReasonPublicTransportLaneNotGrantedRoleFormatLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.PublicTransportLaneNotGrantedRoleFormat";
        internal const string kReasonNoPublicTransportLanePermissionFlagsLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.NoPublicTransportLanePermissionFlags";
        internal const string kReasonOppositeFlowSameSegmentLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.OppositeFlowSameSegment";
        internal const string kReasonEnteredGarageAccessNoSideAccessLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.EnteredGarageAccessNoSideAccess";
        internal const string kReasonEnteredParkingAccessNoSideAccessLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.EnteredParkingAccessNoSideAccess";
        internal const string kReasonEnteredParkingConnectionNoSideAccessLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.EnteredParkingConnectionNoSideAccess";
        internal const string kReasonEnteredBuildingAccessNoSideAccessLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.EnteredBuildingAccessNoSideAccess";
        internal const string kReasonExitedParkingAccessNoSideAccessLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.ExitedParkingAccessNoSideAccess";
        internal const string kReasonExitedGarageAccessNoSideAccessLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.ExitedGarageAccessNoSideAccess";
        internal const string kReasonExitedParkingConnectionNoSideAccessLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.ExitedParkingConnectionNoSideAccess";
        internal const string kReasonExitedBuildingAccessNoSideAccessLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.ExitedBuildingAccessNoSideAccess";
        internal const string kReasonIntersectionMovementFormatLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.IntersectionMovementFormat";
        internal const string kReasonRepeatPenaltyAppliedFormatLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.RepeatPenaltyAppliedFormat";
        internal const string kReasonRepeatPenaltyAppliedLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.RepeatPenaltyApplied";
        internal const string kReasonRepeatPenaltyNotAppliedLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Reason.RepeatPenaltyNotApplied";
        internal const string kPublicTransportLanePolicyQualifierPublicTransportLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.PublicTransportLanePolicyQualifier.PublicTransport";
        internal const string kPublicTransportLanePolicyQualifierEmergencyLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.PublicTransportLanePolicyQualifier.Emergency";
        internal const string kPublicTransportLanePolicyMeaningFormatLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.PublicTransportLanePolicyMeaningFormat";
        internal const string kPublicTransportLanePolicyVanillaAllowLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.PublicTransportLanePolicyVanillaAllow";
        internal const string kPublicTransportLanePolicyVanillaDenyLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.PublicTransportLanePolicyVanillaDeny";
        internal const string kPublicTransportLanePolicyTleAllowLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.PublicTransportLanePolicyTleAllow";
        internal const string kPublicTransportLanePolicyTleDenyLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.PublicTransportLanePolicyTleDeny";
        internal const string kRouteExplanationNoCurrentRouteLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteExplanation.NoCurrentRoute";
        internal const string kRouteExplanationNoCurrentTargetLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteExplanation.NoCurrentTarget";
        internal const string kRouteExplanationWaypointAlignmentLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteExplanation.WaypointAlignment";
        internal const string kRouteExplanationPenaltyPrimaryFormatLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteExplanation.PenaltyPrimaryFormat";
        internal const string kRouteExplanationPenaltyModifierFormatLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteExplanation.PenaltyModifierFormat";
        internal const string kRouteExplanationPtPermissiveLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteExplanation.PtPermissive";
        internal const string kRouteExplanationGenericFallbackLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteExplanation.GenericFallback";
        internal const string kRouteDirectConnectAlreadyOnStartLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteDirectConnect.AlreadyOnStart";
        internal const string kRouteDirectConnectNextHopLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteDirectConnect.NextHop";
        internal const string kRouteDirectConnectViaFormatLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteDirectConnect.ViaFormat";
        internal const string kRouteDirectConnectNoPreviewLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteDirectConnect.NoPreview";
        internal const string kRouteDirectConnectMissingStartLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteDirectConnect.MissingStart";
        internal const string kRouteFullPathContainsStartLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteFullPath.ContainsStart";
        internal const string kRouteFullPathMissingStartLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteFullPath.MissingStart";
        internal const string kRouteFullPathMissingLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteFullPath.Missing";

        private SelectedObjectResolver m_SelectedObjectResolver;
        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;

        private ComponentLookup<Target> m_TargetData;
        private ComponentLookup<CurrentRoute> m_CurrentRouteData;
        private ComponentLookup<PathOwner> m_PathOwnerData;
        private ComponentLookup<RouteLane> m_RouteLaneData;
        private ComponentLookup<Waypoint> m_WaypointData;
        private ComponentLookup<Connected> m_ConnectedData;
        private BufferLookup<CarNavigationLane> m_NavigationLaneData;
        private BufferLookup<PathElement> m_PathElementData;
        private ComponentLookup<CarCurrentLane> m_CurrentLaneData;
        private ComponentLookup<VehicleLaneHistory> m_HistoryData;
        private ComponentLookup<VehicleTrafficLawProfile> m_ProfileData;
        private ComponentLookup<PublicTransportLaneViolation> m_PublicTransportLaneViolationData;
        private ComponentLookup<PublicTransportLanePendingExit> m_PendingExitData;
        private ComponentLookup<PublicTransportLanePermissionState> m_PermissionStateData;
        private ComponentLookup<Owner> m_OwnerData;
        private ComponentLookup<Aggregated> m_AggregatedData;
        private ComponentLookup<SlaveLane> m_SlaveLaneData;
        private ComponentLookup<CarLane> m_CarLaneData;
        private ComponentLookup<EdgeLane> m_EdgeLaneData;
        private ComponentLookup<ParkingLane> m_ParkingLaneData;
        private ComponentLookup<GarageLane> m_GarageLaneData;
        private ComponentLookup<ConnectionLane> m_ConnectionLaneData;

        private SelectedObjectDebugSnapshot m_CurrentSnapshot;
        private bool m_HasSnapshot;
        private Entity m_CurrentRouteSelectionEntity;

        public SelectedObjectDebugSnapshot CurrentSnapshot => m_CurrentSnapshot;
        public bool HasSnapshot => m_HasSnapshot;
        public Entity CurrentRouteSelectionEntity => m_CurrentRouteSelectionEntity;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SelectedObjectResolver = new SelectedObjectResolver(this);
            m_TypeLookups = PublicTransportLaneVehicleTypeLookups.Create(this);
            m_TargetData = GetComponentLookup<Target>(true);
            m_CurrentRouteData = GetComponentLookup<CurrentRoute>(true);
            m_PathOwnerData = GetComponentLookup<PathOwner>(true);
            m_RouteLaneData = GetComponentLookup<RouteLane>(true);
            m_WaypointData = GetComponentLookup<Waypoint>(true);
            m_ConnectedData = GetComponentLookup<Connected>(true);
            m_NavigationLaneData = GetBufferLookup<CarNavigationLane>(true);
            m_PathElementData = GetBufferLookup<PathElement>(true);
            m_CurrentLaneData = GetComponentLookup<CarCurrentLane>(true);
            m_HistoryData = GetComponentLookup<VehicleLaneHistory>(true);
            m_ProfileData = GetComponentLookup<VehicleTrafficLawProfile>(true);
            m_PublicTransportLaneViolationData =
                GetComponentLookup<PublicTransportLaneViolation>(true);
            m_PendingExitData = GetComponentLookup<PublicTransportLanePendingExit>(true);
            m_PermissionStateData =
                GetComponentLookup<PublicTransportLanePermissionState>(true);
            m_OwnerData = GetComponentLookup<Owner>(true);
            m_AggregatedData = GetComponentLookup<Aggregated>(true);
            m_SlaveLaneData = GetComponentLookup<SlaveLane>(true);
            m_CarLaneData = GetComponentLookup<CarLane>(true);
            m_EdgeLaneData = GetComponentLookup<EdgeLane>(true);
            m_ParkingLaneData = GetComponentLookup<ParkingLane>(true);
            m_GarageLaneData = GetComponentLookup<GarageLane>(true);
            m_ConnectionLaneData = GetComponentLookup<ConnectionLane>(true);
        }

        protected override void OnUpdate()
        {
            m_SelectedObjectResolver.Update(this);
            m_TypeLookups.Update(this);
            m_TargetData.Update(this);
            m_CurrentRouteData.Update(this);
            m_PathOwnerData.Update(this);
            m_RouteLaneData.Update(this);
            m_WaypointData.Update(this);
            m_ConnectedData.Update(this);
            m_NavigationLaneData.Update(this);
            m_PathElementData.Update(this);
            m_CurrentLaneData.Update(this);
            m_HistoryData.Update(this);
            m_ProfileData.Update(this);
            m_PublicTransportLaneViolationData.Update(this);
            m_PendingExitData.Update(this);
            m_PermissionStateData.Update(this);
            m_OwnerData.Update(this);
            m_AggregatedData.Update(this);
            m_SlaveLaneData.Update(this);
            m_CarLaneData.Update(this);
            m_EdgeLaneData.Update(this);
            m_ParkingLaneData.Update(this);
            m_GarageLaneData.Update(this);
            m_ConnectionLaneData.Update(this);

            SelectedObjectResolveResult resolveResult =
                m_SelectedObjectResolver.ResolveCurrentSelection();

            m_CurrentSnapshot = BuildSnapshot(resolveResult);
            m_HasSnapshot = true;
        }

        private SelectedObjectDebugSnapshot BuildSnapshot(
            SelectedObjectResolveResult resolveResult)
        {
            Entity vehicle = resolveResult.ResolvedVehicleEntity;
            bool hasVehicleEntity = vehicle != Entity.Null;
            SelectedObjectTleApplicability tleApplicability =
                GetTleApplicability(resolveResult);
            bool tleApplicable =
                tleApplicability != SelectedObjectTleApplicability.NotApplicable;
            bool tleReady =
                tleApplicability == SelectedObjectTleApplicability.ApplicableReady;

            bool hasTrafficLawProfile =
                tleApplicable &&
                hasVehicleEntity &&
                m_ProfileData.HasComponent(vehicle);

            Entity currentLaneEntity = Entity.Null;
            if (tleReady &&
                resolveResult.HasCarCurrentLane &&
                m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLane))
            {
                currentLaneEntity = currentLane.m_Lane;
            }

            Entity previousLaneEntity = Entity.Null;
            int laneChangeCount = 0;
            if (tleReady &&
                hasVehicleEntity &&
                m_HistoryData.TryGetComponent(vehicle, out VehicleLaneHistory history))
            {
                previousLaneEntity = history.m_PreviousLane;
                laneChangeCount = history.m_LaneChangeCount;
            }

            bool ptLaneViolationActive =
                tleReady &&
                hasVehicleEntity &&
                m_PublicTransportLaneViolationData.HasComponent(vehicle);

            bool pendingExitActive =
                tleReady &&
                hasVehicleEntity &&
                m_PendingExitData.HasComponent(vehicle);

            int totalFines = 0;
            int totalViolations = 0;
            string lastReason = string.Empty;
            SelectedObjectDisplayFormatterContext formatterContext =
                CreateDisplayFormatterContext();

            if (tleApplicable && resolveResult.IsVehicle)
            {
                int vehicleIndex = vehicle.Index;
                if (EnforcementTelemetry.TryGetVehicleEnforcementRecord(
                        vehicleIndex,
                        out VehicleEnforcementRecord enforcementRecord))
                {
                    totalViolations = enforcementRecord.TotalViolations;
                    totalFines = enforcementRecord.TotalFines;
                    lastReason = enforcementRecord.LastReason ?? string.Empty;
                }
            }

            SelectedObjectRouteDiagnosticsContext routeDiagnosticsContext =
                CreateRouteDiagnosticsContext();
            SelectedObjectRouteDiagnosticsData routeDiagnostics =
                SelectedObjectRouteDiagnosticsBuilder.Build(
                    resolveResult,
                    tleReady,
                    vehicle,
                    currentLaneEntity,
                    ref routeDiagnosticsContext);
            m_CurrentRouteSelectionEntity = routeDiagnostics.CurrentRouteEntity;
            SelectedObjectEnforcementSummaryContext enforcementSummaryContext =
                CreateEnforcementSummaryContext();
            SelectedObjectEnforcementSummaryData enforcementSummary =
                SelectedObjectEnforcementSummaryBuilder.Build(
                    resolveResult,
                    tleApplicable,
                    hasTrafficLawProfile,
                    vehicle,
                    lastReason,
                    ref enforcementSummaryContext);

            return new SelectedObjectDebugSnapshot(
                resolveResult.ResolveState,
                resolveResult.VehicleKind,
                tleApplicability,
                resolveResult.SourceSelectedEntity,
                resolveResult.ResolvedVehicleEntity,
                resolveResult.PrefabEntity,
                resolveResult.HasPrefabRef,
                resolveResult.IsTrailerChild,
                resolveResult.IsVehicle,
                resolveResult.IsCar,
                resolveResult.IsTrain,
                resolveResult.IsParked,
                resolveResult.HasCarCurrentLane,
                resolveResult.HasTrainCurrentLane,
                resolveResult.HasLiveLaneData,
                resolveResult.HasPublicTransportVehicleData,
                resolveResult.HasTrainData,
                BuildRuntimeFamilyText(resolveResult),
                BuildRawTransportTypeText(resolveResult),
                BuildRawTrackTypeText(resolveResult),
                BuildRailSubtypeSourceText(resolveResult),
                BuildSummaryClassificationText(resolveResult),
                BuildSummaryTleStatusText(resolveResult, tleApplicability),
                enforcementSummary.CompactLastReasonText,
                enforcementSummary.CompactRepeatPenaltyText,
                resolveResult.IsVehicle && hasVehicleEntity ? vehicle.Index : -1,
                BuildRoleText(resolveResult),
                enforcementSummary.PublicTransportLanePolicyText,
                hasTrafficLawProfile,
                currentLaneEntity,
                previousLaneEntity,
                SelectedObjectDisplayFormatter.BuildLaneDisplayText(
                    currentLaneEntity,
                    ref formatterContext),
                SelectedObjectDisplayFormatter.BuildLaneDisplayText(
                    previousLaneEntity,
                    ref formatterContext),
                laneChangeCount,
                ptLaneViolationActive,
                pendingExitActive,
                enforcementSummary.PermissionStateSummary,
                totalFines,
                totalViolations,
                lastReason,
                routeDiagnostics.HasPathOwner,
                routeDiagnostics.HasCurrentTarget,
                routeDiagnostics.HasCurrentRoute,
                routeDiagnostics.CurrentPathFlags,
                routeDiagnostics.HasDiagnostics,
                routeDiagnostics.CurrentTargetText,
                routeDiagnostics.CurrentRouteText,
                routeDiagnostics.TargetRoadText,
                routeDiagnostics.StartOwnerRoadText,
                routeDiagnostics.EndOwnerRoadText,
                routeDiagnostics.DirectConnectText,
                routeDiagnostics.FullPathToTargetStartText,
                routeDiagnostics.NavigationLanesText,
                routeDiagnostics.PlannedPenaltiesText,
                routeDiagnostics.PenaltyTagsText,
                routeDiagnostics.ExplanationText,
                routeDiagnostics.WaypointRouteLaneText,
                routeDiagnostics.ConnectedStopText);
        }

        private SelectedObjectDisplayFormatterContext CreateDisplayFormatterContext()
        {
            return new SelectedObjectDisplayFormatterContext
            {
                EntityManager = EntityManager,
                NameSystem = World.GetOrCreateSystemManaged<Game.UI.NameSystem>(),
                OwnerData = m_OwnerData,
                AggregatedData = m_AggregatedData,
                SlaveLaneData = m_SlaveLaneData,
                CarLaneData = m_CarLaneData,
                ParkingLaneData = m_ParkingLaneData,
                GarageLaneData = m_GarageLaneData,
                ConnectionLaneData = m_ConnectionLaneData,
            };
        }

        private RoutePenaltyInspectionContext CreateRouteInspectionContext()
        {
            return new RoutePenaltyInspectionContext
            {
                EntityManager = EntityManager,
                OwnerData = m_OwnerData,
                CarLaneData = m_CarLaneData,
                EdgeLaneData = m_EdgeLaneData,
                ParkingLaneData = m_ParkingLaneData,
                GarageLaneData = m_GarageLaneData,
                ConnectionLaneData = m_ConnectionLaneData,
                ProfileData = m_ProfileData,
                TypeLookups = m_TypeLookups,
            };
        }

        private SelectedObjectEnforcementSummaryContext CreateEnforcementSummaryContext()
        {
            return new SelectedObjectEnforcementSummaryContext
            {
                ProfileData = m_ProfileData,
                PermissionStateData = m_PermissionStateData,
            };
        }

        private SelectedObjectRouteDiagnosticsContext CreateRouteDiagnosticsContext()
        {
            return new SelectedObjectRouteDiagnosticsContext
            {
                TypeLookups = m_TypeLookups,
                TargetData = m_TargetData,
                CurrentRouteData = m_CurrentRouteData,
                PathOwnerData = m_PathOwnerData,
                RouteLaneData = m_RouteLaneData,
                WaypointData = m_WaypointData,
                ConnectedData = m_ConnectedData,
                NavigationLaneData = m_NavigationLaneData,
                PathElementData = m_PathElementData,
                Formatter = CreateDisplayFormatterContext(),
                InspectionContext = CreateRouteInspectionContext(),
            };
        }

        private static string BuildRuntimeFamilyText(
            SelectedObjectResolveResult resolveResult)
        {
            if (!resolveResult.IsVehicle)
            {
                return "Unavailable";
            }

            switch (resolveResult.RuntimeFamily)
            {
                case SelectedObjectRuntimeFamily.Car:
                    return "Car";

                case SelectedObjectRuntimeFamily.Train:
                    return "Train";

                case SelectedObjectRuntimeFamily.Other:
                    return "Other";

                default:
                    return "Unavailable";
            }
        }

        private static string BuildRawTransportTypeText(
            SelectedObjectResolveResult resolveResult)
        {
            if (!resolveResult.IsVehicle || !resolveResult.HasPublicTransportVehicleData)
            {
                return "Unavailable";
            }

            return resolveResult.RawTransportType == Game.Prefabs.TransportType.None
                ? "None"
                : resolveResult.RawTransportType.ToString();
        }

        private static string BuildRawTrackTypeText(
            SelectedObjectResolveResult resolveResult)
        {
            if (!resolveResult.IsVehicle || !resolveResult.HasTrainData)
            {
                return "Unavailable";
            }

            Game.Net.TrackTypes trackType = resolveResult.RawTrackType;
            if (trackType == Game.Net.TrackTypes.None)
            {
                return "None";
            }

            bool isTrainTrack = (trackType & Game.Net.TrackTypes.Train) != Game.Net.TrackTypes.None;
            bool isTramTrack = (trackType & Game.Net.TrackTypes.Tram) != Game.Net.TrackTypes.None;
            bool isSubwayTrack = (trackType & Game.Net.TrackTypes.Subway) != Game.Net.TrackTypes.None;

            int matchedTypeCount =
                (isTrainTrack ? 1 : 0) +
                (isTramTrack ? 1 : 0) +
                (isSubwayTrack ? 1 : 0);

            if (matchedTypeCount != 1)
            {
                return "Mixed";
            }

            if (isTramTrack)
            {
                return "Tram";
            }

            if (isSubwayTrack)
            {
                return "Subway";
            }

            return "Train";
        }

        private static string BuildRailSubtypeSourceText(
            SelectedObjectResolveResult resolveResult)
        {
            switch (resolveResult.RailSubtypeSource)
            {
                case SelectedObjectRailSubtypeSource.TransportType:
                    return "TransportType";

                case SelectedObjectRailSubtypeSource.TrackType:
                    return "TrackType";

                case SelectedObjectRailSubtypeSource.Fallback:
                    return "Fallback";

                default:
                    return "None";
            }
        }

        private string BuildSummaryClassificationText(
            SelectedObjectResolveResult resolveResult)
        {
            switch (resolveResult.VehicleKind)
            {
                case SelectedObjectKind.RoadCar:
                    return LocalizeText(kClassificationRoadCarLocaleId, "Road car");

                case SelectedObjectKind.ParkedRoadCar:
                    return LocalizeText(kClassificationParkedRoadCarLocaleId, "Parked road car");

                case SelectedObjectKind.RailVehicle:
                    return LocalizeText(kClassificationRailVehicleLocaleId, "Rail vehicle");

                case SelectedObjectKind.ParkedRailVehicle:
                    return LocalizeText(kClassificationParkedRailVehicleLocaleId, "Parked rail vehicle");

                case SelectedObjectKind.Tram:
                    return LocalizeText(kClassificationTramLocaleId, "Tram");

                case SelectedObjectKind.ParkedTram:
                    return LocalizeText(kClassificationParkedTramLocaleId, "Parked tram");

                case SelectedObjectKind.Train:
                    return LocalizeText(kClassificationTrainLocaleId, "Train");

                case SelectedObjectKind.ParkedTrain:
                    return LocalizeText(kClassificationParkedTrainLocaleId, "Parked train");

                case SelectedObjectKind.Subway:
                    return LocalizeText(kClassificationSubwayLocaleId, "Subway");

                case SelectedObjectKind.ParkedSubway:
                    return LocalizeText(kClassificationParkedSubwayLocaleId, "Parked subway");

                case SelectedObjectKind.OtherVehicle:
                    return LocalizeText(kClassificationOtherVehicleLocaleId, "Other vehicle");

                default:
                    return string.Empty;
            }
        }

        private static string BuildSummaryTleStatusText(
            SelectedObjectResolveResult resolveResult,
            SelectedObjectTleApplicability tleApplicability)
        {
            if (resolveResult.ResolveState == SelectedObjectResolveState.None)
            {
                return string.Empty;
            }

            if (resolveResult.ResolveState == SelectedObjectResolveState.NotVehicle)
            {
                return "Selected object is not a vehicle";
            }

            switch (tleApplicability)
            {
                case SelectedObjectTleApplicability.NotApplicable:
                    return "Traffic Law Enforcement not applicable";

                case SelectedObjectTleApplicability.ApplicableNoLiveLaneData:
                    return resolveResult.VehicleKind == SelectedObjectKind.ParkedRoadCar
                        ? "Live lane unavailable for parked road car"
                        : "Live lane unavailable";

                case SelectedObjectTleApplicability.ApplicableReady:
                    return "Tracking selected road vehicle";

                default:
                    return string.Empty;
            }
        }

        private string BuildRoleText(SelectedObjectResolveResult resolveResult)
        {
            if (!resolveResult.HasSelection)
            {
                return string.Empty;
            }

            if (!resolveResult.IsVehicle)
            {
                return string.Empty;
            }

            switch (resolveResult.VehicleKind)
            {
                case SelectedObjectKind.RoadCar:
                    {
                        Entity vehicle = resolveResult.ResolvedVehicleEntity;
                        return BuildLocalizedRoadVehicleRoleText(vehicle);
                    }

                case SelectedObjectKind.ParkedRoadCar:
                    return LocalizeText(kClassificationParkedRoadCarLocaleId, "Parked road car");

                case SelectedObjectKind.RailVehicle:
                    return LocalizeText(kClassificationRailVehicleLocaleId, "Rail vehicle");

                case SelectedObjectKind.ParkedRailVehicle:
                    return LocalizeText(kClassificationParkedRailVehicleLocaleId, "Parked rail vehicle");

                case SelectedObjectKind.Tram:
                    return LocalizeText(kClassificationTramLocaleId, "Tram");

                case SelectedObjectKind.ParkedTram:
                    return LocalizeText(kClassificationParkedTramLocaleId, "Parked tram");

                case SelectedObjectKind.Train:
                    return LocalizeText(kClassificationTrainLocaleId, "Train");

                case SelectedObjectKind.ParkedTrain:
                    return LocalizeText(kClassificationParkedTrainLocaleId, "Parked train");

                case SelectedObjectKind.Subway:
                    return LocalizeText(kClassificationSubwayLocaleId, "Subway");

                case SelectedObjectKind.ParkedSubway:
                    return LocalizeText(kClassificationParkedSubwayLocaleId, "Parked subway");

                case SelectedObjectKind.OtherVehicle:
                    return LocalizeText(kClassificationOtherVehicleLocaleId, "Other vehicle");

                default:
                    return LocalizeText(kClassificationOtherVehicleLocaleId, "Other vehicle");
            }
        }

        internal static string LocalizeText(string localeId, string fallback)
        {
            return SelectedObjectLocalization.LocalizeText(localeId, fallback);
        }

        private string BuildLocalizedRoadVehicleRoleText(Entity vehicle)
        {
            List<string> names = new List<string>(4);

            PublicTransportLaneVehicleCategory authorizedCategories =
                PublicTransportLanePolicy.GetVanillaAuthorizedCategories(vehicle, ref m_TypeLookups);
            AppendAuthorizedCategoryNamesLocalized(authorizedCategories, names);

            PublicTransportLaneFlagGrantExperimentRole additionalRole =
                PublicTransportLanePolicy.GetFlagGrantExperimentRole(vehicle, ref m_TypeLookups);

            if (additionalRole != PublicTransportLaneFlagGrantExperimentRole.None)
            {
                names.Add(GetRoleDisplayNameLocalized(additionalRole));
            }

            if (names.Count == 0)
            {
                names.Add(LocalizeText(kRoleUnclassifiedRoadVehicleLocaleId, "Unclassified road vehicle"));
            }

            string description = string.Join(", ", names);
            if (EmergencyVehiclePolicy.IsEmergencyVehicle(vehicle, ref m_TypeLookups))
            {
                description += " [" + LocalizeText(kRoleEmergencyQualifierLocaleId, "emergency") + "]";
            }

            return description;
        }

        private string GetRoleDisplayNameLocalized(PublicTransportLaneFlagGrantExperimentRole role)
        {
            switch (role)
            {
                case PublicTransportLaneFlagGrantExperimentRole.PersonalCar:
                    return LocalizeText(kRolePersonalCarLocaleId, "Personal cars");
                case PublicTransportLaneFlagGrantExperimentRole.DeliveryTruck:
                    return LocalizeText(kRoleDeliveryTruckLocaleId, "Delivery trucks");
                case PublicTransportLaneFlagGrantExperimentRole.CargoTransport:
                    return LocalizeText(kRoleCargoTransportLocaleId, "Cargo transport vehicles");
                case PublicTransportLaneFlagGrantExperimentRole.Hearse:
                    return LocalizeText(kRoleHearseLocaleId, "Hearses");
                case PublicTransportLaneFlagGrantExperimentRole.PrisonerTransport:
                    return LocalizeText(kRolePrisonerTransportLocaleId, "Prisoner transports");
                case PublicTransportLaneFlagGrantExperimentRole.ParkMaintenanceVehicle:
                    return LocalizeText(kRoleParkMaintenanceVehicleLocaleId, "Park maintenance vehicles");
                default:
                    return LocalizeText(kRoleUnclassifiedRoadVehicleLocaleId, "Unclassified road vehicle");
            }
        }

        private void AppendAuthorizedCategoryNamesLocalized(
            PublicTransportLaneVehicleCategory categories,
            List<string> names)
        {
            if ((categories & PublicTransportLaneVehicleCategory.RoadPublicTransportVehicle) != 0)
            {
                names.Add(LocalizeText(kRoleRoadPublicTransportLocaleId, "Road public transport vehicles"));
            }

            if ((categories & PublicTransportLaneVehicleCategory.Taxi) != 0)
            {
                names.Add(LocalizeText(kRoleTaxiLocaleId, "Taxis"));
            }

            if ((categories & PublicTransportLaneVehicleCategory.PoliceCar) != 0)
            {
                names.Add(LocalizeText(kRolePoliceCarLocaleId, "Police cars"));
            }

            if ((categories & PublicTransportLaneVehicleCategory.FireEngine) != 0)
            {
                names.Add(LocalizeText(kRoleFireEngineLocaleId, "Fire engines"));
            }

            if ((categories & PublicTransportLaneVehicleCategory.Ambulance) != 0)
            {
                names.Add(LocalizeText(kRoleAmbulanceLocaleId, "Ambulances"));
            }

            if ((categories & PublicTransportLaneVehicleCategory.GarbageTruck) != 0)
            {
                names.Add(LocalizeText(kRoleGarbageTruckLocaleId, "Garbage trucks"));
            }

            if ((categories & PublicTransportLaneVehicleCategory.PostVan) != 0)
            {
                names.Add(LocalizeText(kRolePostVanLocaleId, "Post vans"));
            }

            if ((categories & PublicTransportLaneVehicleCategory.RoadMaintenanceVehicle) != 0)
            {
                names.Add(LocalizeText(kRoleRoadMaintenanceVehicleLocaleId, "Road maintenance vehicles"));
            }

            if ((categories & PublicTransportLaneVehicleCategory.Snowplow) != 0)
            {
                names.Add(LocalizeText(kRoleSnowplowLocaleId, "Snowplows"));
            }

            if ((categories & PublicTransportLaneVehicleCategory.VehicleMaintenanceVehicle) != 0)
            {
                names.Add(LocalizeText(kRoleVehicleMaintenanceVehicleLocaleId, "Vehicle maintenance vehicles"));
            }
        }

        private static SelectedObjectTleApplicability GetTleApplicability(
            SelectedObjectResolveResult resolveResult)
        {
            switch (resolveResult.VehicleKind)
            {
                case SelectedObjectKind.RoadCar:
                    return resolveResult.HasCarCurrentLane
                        ? SelectedObjectTleApplicability.ApplicableReady
                        : SelectedObjectTleApplicability.ApplicableNoLiveLaneData;

                case SelectedObjectKind.ParkedRoadCar:
                    return SelectedObjectTleApplicability.ApplicableNoLiveLaneData;

                default:
                    return SelectedObjectTleApplicability.NotApplicable;
            }
        }
    }
}

