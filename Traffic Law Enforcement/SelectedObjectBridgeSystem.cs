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
    public enum SelectedObjectTleApplicability
    {
        NotApplicable,
        ApplicableNoLiveLaneData,
        ApplicableReady,
    }

    public readonly struct SelectedObjectDebugSnapshot
    {
        public readonly SelectedObjectResolveState ResolveState;
        public readonly SelectedObjectKind VehicleKind;
        public readonly SelectedObjectTleApplicability TleApplicability;
        public readonly Entity SourceSelectedEntity;
        public readonly Entity ResolvedVehicleEntity;
        public readonly Entity PrefabEntity;
        public readonly bool HasPrefabRef;
        public readonly bool IsTrailerChild;
        public readonly bool IsVehicle;
        public readonly bool IsCar;
        public readonly bool IsTrain;
        public readonly bool IsParked;
        public readonly bool HasCarCurrentLane;
        public readonly bool HasTrainCurrentLane;
        public readonly bool HasLiveLaneData;
        public readonly bool HasPublicTransportVehicleData;
        public readonly bool HasTrainData;
        public readonly string RuntimeFamilyText;
        public readonly string RawTransportTypeText;
        public readonly string RawTrackTypeText;
        public readonly string RailSubtypeSourceText;
        public readonly string SummaryClassificationText;
        public readonly string SummaryTleStatusText;
        public readonly string CompactLastReasonText;
        public readonly string CompactRepeatPenaltyText;

        public readonly int VehicleIndex;
        public readonly string RoleText;
        public readonly string PublicTransportLanePolicyText;
        public readonly bool HasTrafficLawProfile;

        public readonly Entity CurrentLaneEntity;
        public readonly Entity PreviousLaneEntity;
        public readonly int LaneChangeCount;

        public readonly bool PublicTransportLaneViolationActive;
        public readonly bool PendingExitActive;
        public readonly string PermissionStateSummary;

        public readonly int TotalFines;
        public readonly int TotalViolations;
        public readonly string LastReason;
        public readonly bool HasPathOwner;
        public readonly bool HasCurrentTarget;
        public readonly bool HasCurrentRoute;
        public readonly PathFlags CurrentPathFlags;
        public readonly bool HasRouteDiagnostics;
        public readonly string RouteDiagnosticsCurrentTargetText;
        public readonly string RouteDiagnosticsCurrentRouteText;
        public readonly string RouteDiagnosticsNavigationLanesText;
        public readonly string RouteDiagnosticsPlannedPenaltiesText;
        public readonly string RouteDiagnosticsPenaltyTagsText;
        public readonly string RouteDiagnosticsExplanationText;
        public readonly string RouteDiagnosticsWaypointRouteLaneText;
        public readonly string RouteDiagnosticsConnectedStopText;

        public SelectedObjectDebugSnapshot(
            SelectedObjectResolveState resolveState,
            SelectedObjectKind vehicleKind,
            SelectedObjectTleApplicability tleApplicability,
            Entity sourceSelectedEntity,
            Entity resolvedVehicleEntity,
            Entity prefabEntity,
            bool hasPrefabRef,
            bool isTrailerChild,
            bool isVehicle,
            bool isCar,
            bool isTrain,
            bool isParked,
            bool hasCarCurrentLane,
            bool hasTrainCurrentLane,
            bool hasLiveLaneData,
            bool hasPublicTransportVehicleData,
            bool hasTrainData,
            string runtimeFamilyText,
            string rawTransportTypeText,
            string rawTrackTypeText,
            string railSubtypeSourceText,
            string summaryClassificationText,
            string summaryTleStatusText,
            string compactLastReasonText,
            string compactRepeatPenaltyText,
            int vehicleIndex,
            string roleText,
            string publicTransportLanePolicyText,
            bool hasTrafficLawProfile,
            Entity currentLaneEntity,
            Entity previousLaneEntity,
            int laneChangeCount,
            bool ptLaneViolationActive,
            bool pendingExitActive,
            string permissionStateSummary,
            int totalFines,
            int totalViolations,
            string lastReason,
            bool hasPathOwner,
            bool hasCurrentTarget,
            bool hasCurrentRoute,
            PathFlags currentPathFlags,
            bool hasRouteDiagnostics,
            string routeDiagnosticsCurrentTargetText,
            string routeDiagnosticsCurrentRouteText,
            string routeDiagnosticsNavigationLanesText,
            string routeDiagnosticsPlannedPenaltiesText,
            string routeDiagnosticsPenaltyTagsText,
            string routeDiagnosticsExplanationText,
            string routeDiagnosticsWaypointRouteLaneText,
            string routeDiagnosticsConnectedStopText)
        {
            ResolveState = resolveState;
            VehicleKind = vehicleKind;
            TleApplicability = tleApplicability;
            SourceSelectedEntity = sourceSelectedEntity;
            ResolvedVehicleEntity = resolvedVehicleEntity;
            PrefabEntity = prefabEntity;
            HasPrefabRef = hasPrefabRef;
            IsTrailerChild = isTrailerChild;
            IsVehicle = isVehicle;
            IsCar = isCar;
            IsTrain = isTrain;
            IsParked = isParked;
            HasCarCurrentLane = hasCarCurrentLane;
            HasTrainCurrentLane = hasTrainCurrentLane;
            HasLiveLaneData = hasLiveLaneData;
            HasPublicTransportVehicleData = hasPublicTransportVehicleData;
            HasTrainData = hasTrainData;
            RuntimeFamilyText = runtimeFamilyText;
            RawTransportTypeText = rawTransportTypeText;
            RawTrackTypeText = rawTrackTypeText;
            RailSubtypeSourceText = railSubtypeSourceText;
            SummaryClassificationText = summaryClassificationText;
            SummaryTleStatusText = summaryTleStatusText;
            CompactLastReasonText = compactLastReasonText;
            CompactRepeatPenaltyText = compactRepeatPenaltyText;
            VehicleIndex = vehicleIndex;
            RoleText = roleText;
            PublicTransportLanePolicyText = publicTransportLanePolicyText;
            HasTrafficLawProfile = hasTrafficLawProfile;
            CurrentLaneEntity = currentLaneEntity;
            PreviousLaneEntity = previousLaneEntity;
            LaneChangeCount = laneChangeCount;
            PublicTransportLaneViolationActive = ptLaneViolationActive;
            PendingExitActive = pendingExitActive;
            PermissionStateSummary = permissionStateSummary;
            TotalFines = totalFines;
            TotalViolations = totalViolations;
            LastReason = lastReason;
            HasPathOwner = hasPathOwner;
            HasCurrentTarget = hasCurrentTarget;
            HasCurrentRoute = hasCurrentRoute;
            CurrentPathFlags = currentPathFlags;
            HasRouteDiagnostics = hasRouteDiagnostics;
            RouteDiagnosticsCurrentTargetText = routeDiagnosticsCurrentTargetText;
            RouteDiagnosticsCurrentRouteText = routeDiagnosticsCurrentRouteText;
            RouteDiagnosticsNavigationLanesText = routeDiagnosticsNavigationLanesText;
            RouteDiagnosticsPlannedPenaltiesText = routeDiagnosticsPlannedPenaltiesText;
            RouteDiagnosticsPenaltyTagsText = routeDiagnosticsPenaltyTagsText;
            RouteDiagnosticsExplanationText = routeDiagnosticsExplanationText;
            RouteDiagnosticsWaypointRouteLaneText = routeDiagnosticsWaypointRouteLaneText;
            RouteDiagnosticsConnectedStopText = routeDiagnosticsConnectedStopText;
        }
    }

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

        private SelectedObjectResolver m_SelectedObjectResolver;
        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;

        private ComponentLookup<Target> m_TargetData;
        private ComponentLookup<CurrentRoute> m_CurrentRouteData;
        private ComponentLookup<PathOwner> m_PathOwnerData;
        private ComponentLookup<RouteLane> m_RouteLaneData;
        private ComponentLookup<Waypoint> m_WaypointData;
        private ComponentLookup<Connected> m_ConnectedData;
        private BufferLookup<CarNavigationLane> m_NavigationLaneData;
        private ComponentLookup<CarCurrentLane> m_CurrentLaneData;
        private ComponentLookup<VehicleLaneHistory> m_HistoryData;
        private ComponentLookup<VehicleTrafficLawProfile> m_ProfileData;
        private ComponentLookup<PublicTransportLaneViolation> m_PublicTransportLaneViolationData;
        private ComponentLookup<PublicTransportLanePendingExit> m_PendingExitData;
        private ComponentLookup<PublicTransportLanePermissionState> m_PermissionStateData;
        private ComponentLookup<Owner> m_OwnerData;
        private ComponentLookup<CarLane> m_CarLaneData;
        private ComponentLookup<EdgeLane> m_EdgeLaneData;
        private ComponentLookup<ParkingLane> m_ParkingLaneData;
        private ComponentLookup<GarageLane> m_GarageLaneData;
        private ComponentLookup<ConnectionLane> m_ConnectionLaneData;

        private SelectedObjectDebugSnapshot m_CurrentSnapshot;
        private bool m_HasSnapshot;

        public SelectedObjectDebugSnapshot CurrentSnapshot => m_CurrentSnapshot;
        public bool HasSnapshot => m_HasSnapshot;

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
            m_CurrentLaneData = GetComponentLookup<CarCurrentLane>(true);
            m_HistoryData = GetComponentLookup<VehicleLaneHistory>(true);
            m_ProfileData = GetComponentLookup<VehicleTrafficLawProfile>(true);
            m_PublicTransportLaneViolationData =
                GetComponentLookup<PublicTransportLaneViolation>(true);
            m_PendingExitData = GetComponentLookup<PublicTransportLanePendingExit>(true);
            m_PermissionStateData =
                GetComponentLookup<PublicTransportLanePermissionState>(true);
            m_OwnerData = GetComponentLookup<Owner>(true);
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
            m_CurrentLaneData.Update(this);
            m_HistoryData.Update(this);
            m_ProfileData.Update(this);
            m_PublicTransportLaneViolationData.Update(this);
            m_PendingExitData.Update(this);
            m_PermissionStateData.Update(this);
            m_OwnerData.Update(this);
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

            string permissionStateSummary = string.Empty;
            if (tleApplicable)
            {
                permissionStateSummary =
                    BuildPermissionStateSummary(vehicle, hasTrafficLawProfile);
            }

            int totalFines = 0;
            int totalViolations = 0;
            string lastReason = string.Empty;

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

            RouteDiagnosticsData routeDiagnostics =
                BuildRouteDiagnosticsData(
                    resolveResult,
                    tleReady,
                    vehicle,
                    currentLaneEntity);

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
                BuildCompactLastReasonText(tleApplicable, lastReason),
                BuildCompactRepeatPenaltyText(tleApplicable, lastReason),
                resolveResult.IsVehicle && hasVehicleEntity ? vehicle.Index : -1,
                BuildRoleText(resolveResult),
                BuildPublicTransportLanePolicyText(resolveResult, hasTrafficLawProfile),
                hasTrafficLawProfile,
                currentLaneEntity,
                previousLaneEntity,
                laneChangeCount,
                ptLaneViolationActive,
                pendingExitActive,
                permissionStateSummary,
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
                routeDiagnostics.NavigationLanesText,
                routeDiagnostics.PlannedPenaltiesText,
                routeDiagnostics.PenaltyTagsText,
                routeDiagnostics.ExplanationText,
                routeDiagnostics.WaypointRouteLaneText,
                routeDiagnostics.ConnectedStopText);
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

        private RouteDiagnosticsData BuildRouteDiagnosticsData(
            SelectedObjectResolveResult resolveResult,
            bool tleReady,
            Entity vehicle,
            Entity currentLaneEntity)
        {
            if (!tleReady ||
                resolveResult.VehicleKind != SelectedObjectKind.RoadCar ||
                vehicle == Entity.Null)
            {
                return default;
            }

            bool hasCurrentTarget =
                m_TargetData.TryGetComponent(vehicle, out Target targetData) &&
                targetData.m_Target != Entity.Null;
            Entity targetEntity =
                hasCurrentTarget
                    ? targetData.m_Target
                    : Entity.Null;

            bool hasCurrentRoute =
                m_CurrentRouteData.TryGetComponent(vehicle, out CurrentRoute currentRouteData) &&
                currentRouteData.m_Route != Entity.Null;
            Entity currentRouteEntity =
                hasCurrentRoute
                    ? currentRouteData.m_Route
                    : Entity.Null;

            bool hasPathOwner =
                m_PathOwnerData.TryGetComponent(vehicle, out PathOwner pathOwner);
            PathFlags currentPathFlags =
                hasPathOwner
                    ? pathOwner.m_State
                    : default;

            bool hasNavigationLanes =
                m_NavigationLaneData.TryGetBuffer(vehicle, out DynamicBuffer<CarNavigationLane> navigationLanes);

            RoutePenaltyInspectionContext inspectionContext =
                CreateRouteInspectionContext();
            RoutePenaltyInspectionResult inspection =
                RoutePenaltyInspection.InspectCurrentRoute(
                    vehicle,
                    currentLaneEntity,
                    navigationLanes,
                    hasNavigationLanes,
                    ref inspectionContext,
                    captureDebugStrings: true);

            return new RouteDiagnosticsData(
                hasDiagnostics: true,
                hasPathOwner: hasPathOwner,
                hasCurrentTarget: hasCurrentTarget,
                hasCurrentRoute: hasCurrentRoute,
                currentPathFlags: currentPathFlags,
                currentTargetText: BuildCurrentTargetText(targetEntity),
                currentRouteText: FormatEntityOrNone(currentRouteEntity),
                navigationLanesText: NormalizeInspectionText(
                    RoutePenaltyInspection.BuildNavigationPreview(
                        currentLaneEntity,
                        navigationLanes,
                        hasNavigationLanes,
                        ref inspectionContext)),
                plannedPenaltiesText: NormalizeInspectionText(inspection.Breakdown),
                penaltyTagsText: NormalizeInspectionText(inspection.Tags),
                explanationText: BuildRouteDecisionExplanation(
                    vehicle,
                    currentLaneEntity,
                    hasCurrentTarget,
                    targetEntity,
                    hasCurrentRoute,
                    inspection),
                waypointRouteLaneText: BuildWaypointRouteLaneText(targetEntity),
                connectedStopText: BuildConnectedStopText(targetEntity));
        }

        private string BuildCurrentTargetText(Entity targetEntity)
        {
            if (targetEntity == Entity.Null)
            {
                return FormatEntityOrNone(Entity.Null);
            }

            string text = FormatEntityOrNone(targetEntity);
            if (m_WaypointData.TryGetComponent(targetEntity, out Waypoint waypoint))
            {
                text += $" [waypoint {waypoint.m_Index}]";
            }

            return text;
        }

        private string BuildWaypointRouteLaneText(Entity targetEntity)
        {
            if (targetEntity == Entity.Null ||
                !m_RouteLaneData.TryGetComponent(targetEntity, out RouteLane routeLane))
            {
                return string.Empty;
            }

            return
                $"start {FormatEntityOrNone(routeLane.m_StartLane)} -> end {FormatEntityOrNone(routeLane.m_EndLane)} ({routeLane.m_StartCurvePos:0.###} -> {routeLane.m_EndCurvePos:0.###})";
        }

        private string BuildConnectedStopText(Entity targetEntity)
        {
            if (targetEntity == Entity.Null ||
                !m_ConnectedData.TryGetComponent(targetEntity, out Connected connected))
            {
                return string.Empty;
            }

            return FormatEntityOrNone(connected.m_Connected);
        }

        private string BuildRouteDecisionExplanation(
            Entity vehicle,
            Entity currentLaneEntity,
            bool hasCurrentTarget,
            Entity targetEntity,
            bool hasCurrentRoute,
            RoutePenaltyInspectionResult inspection)
        {
            if (!hasCurrentRoute)
            {
                return LocalizeText(
                    kRouteExplanationNoCurrentRouteLocaleId,
                    "No current route is attached to this vehicle.");
            }

            if (!hasCurrentTarget)
            {
                return LocalizeText(
                    kRouteExplanationNoCurrentTargetLocaleId,
                    "No current target is attached to this vehicle.");
            }

            List<string> parts = new List<string>(3);
            bool hasPrimaryExplanation = false;

            if (TryHasWaypointRouteLaneMismatch(targetEntity, currentLaneEntity))
            {
                parts.Add(
                    LocalizeText(
                        kRouteExplanationWaypointAlignmentLocaleId,
                        "Vehicle is aligning for the next waypoint / stop approach lane."));
                hasPrimaryExplanation = true;
            }

            if (inspection.Profile.HasAnyPenalty)
            {
                string tagSummary = NormalizeInspectionText(inspection.Tags);
                string format =
                    parts.Count == 0
                        ? LocalizeText(
                            kRouteExplanationPenaltyPrimaryFormatLocaleId,
                            "Current planned route contains deterrence tags: {0}.")
                        : LocalizeText(
                            kRouteExplanationPenaltyModifierFormatLocaleId,
                            "Current planned route also contains deterrence tags: {0}.");
                parts.Add(string.Format(format, tagSummary));
                hasPrimaryExplanation = true;
            }

            if (IsRoadPublicTransportVehicle(vehicle) &&
                inspection.PublicTransportLanePolicyResolved &&
                inspection.AllowedOnPublicTransportLane)
            {
                parts.Add(
                    LocalizeText(
                        kRouteExplanationPtPermissiveLocaleId,
                        "PT-lane policy is currently permissive for this vehicle."));
            }

            if (!hasPrimaryExplanation)
            {
                parts.Add(
                    LocalizeText(
                        kRouteExplanationGenericFallbackLocaleId,
                        "No route-target mismatch or current TLE penalty tag was identified; current behavior is most likely vanilla lane-group alignment."));
            }

            return string.Join(" ", parts.ToArray());
        }

        private bool TryHasWaypointRouteLaneMismatch(
            Entity targetEntity,
            Entity currentLaneEntity)
        {
            if (targetEntity == Entity.Null ||
                currentLaneEntity == Entity.Null ||
                !m_RouteLaneData.TryGetComponent(targetEntity, out RouteLane routeLane) ||
                routeLane.m_StartLane == Entity.Null)
            {
                return false;
            }

            return routeLane.m_StartLane != currentLaneEntity;
        }

        private bool IsRoadPublicTransportVehicle(Entity vehicle)
        {
            PublicTransportLaneVehicleCategory categories =
                PublicTransportLanePolicy.GetVanillaAuthorizedCategories(vehicle, ref m_TypeLookups);
            return (categories & PublicTransportLaneVehicleCategory.RoadPublicTransportVehicle) != 0;
        }

        private string NormalizeInspectionText(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text == "none")
            {
                return FormatEntityOrNone(Entity.Null);
            }

            return text.Trim();
        }

        private string FormatEntityOrNone(Entity entity)
        {
            return entity == Entity.Null
                ? LocalizeText(SelectedObjectPanelUISystem.kNoneLocaleId, "None")
                : $"#{entity.Index}:v{entity.Version}";
        }

        private readonly struct RouteDiagnosticsData
        {
            public readonly bool HasDiagnostics;
            public readonly bool HasPathOwner;
            public readonly bool HasCurrentTarget;
            public readonly bool HasCurrentRoute;
            public readonly PathFlags CurrentPathFlags;
            public readonly string CurrentTargetText;
            public readonly string CurrentRouteText;
            public readonly string NavigationLanesText;
            public readonly string PlannedPenaltiesText;
            public readonly string PenaltyTagsText;
            public readonly string ExplanationText;
            public readonly string WaypointRouteLaneText;
            public readonly string ConnectedStopText;

            public RouteDiagnosticsData(
                bool hasDiagnostics,
                bool hasPathOwner,
                bool hasCurrentTarget,
                bool hasCurrentRoute,
                PathFlags currentPathFlags,
                string currentTargetText,
                string currentRouteText,
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
                CurrentPathFlags = currentPathFlags;
                CurrentTargetText = currentTargetText;
                CurrentRouteText = currentRouteText;
                NavigationLanesText = navigationLanesText;
                PlannedPenaltiesText = plannedPenaltiesText;
                PenaltyTagsText = penaltyTagsText;
                ExplanationText = explanationText;
                WaypointRouteLaneText = waypointRouteLaneText;
                ConnectedStopText = connectedStopText;
            }
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

        private string BuildCompactLastReasonText(
            bool tleApplicable,
            string lastReason)
        {
            if (!tleApplicable)
            {
                return string.Empty;
            }

            SplitRepeatPenaltyReason(
                NormalizeReasonText(lastReason),
                out string reasonText,
                out _);

            string normalizedReason = string.IsNullOrWhiteSpace(reasonText)
                ? LocalizeText(kReasonNoneRecordedLocaleId, "None recorded")
                : SummarizeReasonText(reasonText);

            const int maxLength = 56;
            if (normalizedReason.Length <= maxLength)
            {
                return normalizedReason;
            }

            return normalizedReason.Substring(0, maxLength - 3) + "...";
        }

        private string BuildCompactRepeatPenaltyText(
            bool tleApplicable,
            string lastReason)
        {
            if (!tleApplicable)
            {
                return string.Empty;
            }

            SplitRepeatPenaltyReason(
                NormalizeReasonText(lastReason),
                out _,
                out string repeatPenaltyText);

            return string.IsNullOrWhiteSpace(repeatPenaltyText)
                ? LocalizeText(
                    kReasonRepeatPenaltyNotAppliedLocaleId,
                    "Not applied")
                : repeatPenaltyText;
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

        private string BuildPublicTransportLanePolicyText(
            SelectedObjectResolveResult resolveResult,
            bool hasTrafficLawProfile)
        {
            if (!hasTrafficLawProfile ||
                !resolveResult.IsVehicle ||
                (resolveResult.VehicleKind != SelectedObjectKind.RoadCar &&
                 resolveResult.VehicleKind != SelectedObjectKind.ParkedRoadCar))
            {
                return string.Empty;
            }

            Entity vehicle = resolveResult.ResolvedVehicleEntity;
            if (vehicle == Entity.Null ||
                !m_ProfileData.TryGetComponent(vehicle, out VehicleTrafficLawProfile profile))
            {
                return string.Empty;
            }

            PublicTransportLaneAccessBits accessBits = profile.m_PublicTransportLaneAccessBits;
            string type = PublicTransportLanePolicy.DescribeType(accessBits);
            bool vanillaAllow = PublicTransportLanePolicy.VanillaAllowsAccess(accessBits);
            bool modAllow = PublicTransportLanePolicy.ModAllowsAccess(accessBits);
            string meaningFormat = LocalizeText(
                kPublicTransportLanePolicyMeaningFormatLocaleId,
                "{0} ({1}, {2})");
            string vanillaMeaning = LocalizeText(
                vanillaAllow
                    ? kPublicTransportLanePolicyVanillaAllowLocaleId
                    : kPublicTransportLanePolicyVanillaDenyLocaleId,
                vanillaAllow ? "Vanilla Allowed" : "Vanilla Denied");
            string tleMeaning = LocalizeText(
                modAllow
                    ? kPublicTransportLanePolicyTleAllowLocaleId
                    : kPublicTransportLanePolicyTleDenyLocaleId,
                modAllow ? "TLE Allowed" : "TLE Denied");
            string meaning = string.Format(meaningFormat, type, vanillaMeaning, tleMeaning);

            List<string> qualifiers = null;
            if (PublicTransportLanePolicy.ModPrefersLanes(accessBits))
            {
                (qualifiers ??= new List<string>()).Add(
                    LocalizeText(
                        kPublicTransportLanePolicyQualifierPublicTransportLocaleId,
                        "PT"));
            }

            if (profile.m_EmergencyVehicle != 0)
            {
                (qualifiers ??= new List<string>()).Add(
                    LocalizeText(
                        kPublicTransportLanePolicyQualifierEmergencyLocaleId,
                        "Emergency"));
            }

            return qualifiers == null || qualifiers.Count == 0
                ? meaning
                : $"{meaning} [{string.Join(", ", qualifiers)}]";
        }

        private static string LocalizeText(string localeId, string fallback)
        {
            if (GameManager.instance?.localizationManager?.activeDictionary != null &&
                GameManager.instance.localizationManager.activeDictionary.TryGetValue(localeId, out string value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return fallback;
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

        private string SummarizeReasonText(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return LocalizeText(kReasonNoneRecordedLocaleId, "None recorded");
            }

            const string ptLaneRevokedPrefix = "PT-lane flags revoked by mod setting: ";
            const string ptLaneMissingVanillaPrefix = "PT-lane flags missing for vanilla-authorized categories: ";
            const string ptLaneMissingGrantedRolePrefix = "PT-lane flags missing for granted role: ";
            const string ptLaneNotGrantedRolePrefix = "PT-lane flags not granted for role: ";
            const string actualMovementPrefix = "actual ";
            const string allowedMovementDelimiter = ", allowed ";

            if (reason.StartsWith(ptLaneRevokedPrefix))
            {
                return string.Format(
                    LocalizeText(
                        kReasonPublicTransportLaneRevokedByModFormatLocaleId,
                        "PT-lane access revoked by mod: {0}"),
                    reason.Substring(ptLaneRevokedPrefix.Length));
            }

            if (reason.StartsWith(ptLaneMissingVanillaPrefix))
            {
                return string.Format(
                    LocalizeText(
                        kReasonPublicTransportLaneMissingVanillaCategoriesFormatLocaleId,
                        "Vanilla-authorized PT-lane flags missing: {0}"),
                    reason.Substring(ptLaneMissingVanillaPrefix.Length));
            }

            if (reason.StartsWith(ptLaneMissingGrantedRolePrefix))
            {
                return string.Format(
                    LocalizeText(
                        kReasonPublicTransportLaneMissingGrantedRoleFormatLocaleId,
                        "Granted role missing PT-lane flags: {0}"),
                    reason.Substring(ptLaneMissingGrantedRolePrefix.Length));
            }

            if (reason.StartsWith(ptLaneNotGrantedRolePrefix))
            {
                return string.Format(
                    LocalizeText(
                        kReasonPublicTransportLaneNotGrantedRoleFormatLocaleId,
                        "PT-lane not granted for role: {0}"),
                    reason.Substring(ptLaneNotGrantedRolePrefix.Length));
            }

            if (reason == "vehicle has no PT-lane permission flags")
            {
                return LocalizeText(
                    kReasonNoPublicTransportLanePermissionFlagsLocaleId,
                    "Vehicle has no PT-lane permission flags");
            }

            switch (reason)
            {
                case "vehicle switched to the opposite flow on the same road segment":
                    return LocalizeText(
                        kReasonOppositeFlowSameSegmentLocaleId,
                        "Switched to opposite flow on the same segment");
                case "vehicle entered garage access from a lane without side-access permission":
                    return LocalizeText(
                        kReasonEnteredGarageAccessNoSideAccessLocaleId,
                        "Entered garage access without side access");
                case "vehicle entered parking access from a lane without side-access permission":
                    return LocalizeText(
                        kReasonEnteredParkingAccessNoSideAccessLocaleId,
                        "Entered parking access without side access");
                case "vehicle crossed into parking connection from a lane without side-access permission":
                    return LocalizeText(
                        kReasonEnteredParkingConnectionNoSideAccessLocaleId,
                        "Entered parking connection without side access");
                case "vehicle crossed into building/service access connection from a lane without side-access permission":
                    return LocalizeText(
                        kReasonEnteredBuildingAccessNoSideAccessLocaleId,
                        "Entered building/service access without side access");
                case "vehicle exited parking access into a lane without side-access permission":
                    return LocalizeText(
                        kReasonExitedParkingAccessNoSideAccessLocaleId,
                        "Exited parking access without side access");
                case "vehicle exited garage access into a lane without side-access permission":
                    return LocalizeText(
                        kReasonExitedGarageAccessNoSideAccessLocaleId,
                        "Exited garage access without side access");
                case "vehicle exited parking connection into a lane without side-access permission":
                    return LocalizeText(
                        kReasonExitedParkingConnectionNoSideAccessLocaleId,
                        "Exited parking connection without side access");
                case "vehicle exited building/service access connection into a lane without side-access permission":
                    return LocalizeText(
                        kReasonExitedBuildingAccessNoSideAccessLocaleId,
                        "Exited building/service access without side access");
            }

            if (reason.StartsWith(actualMovementPrefix) &&
                reason.Contains(allowedMovementDelimiter))
            {
                int delimiterIndex = reason.IndexOf(allowedMovementDelimiter);
                string actualMovement = reason.Substring(
                    actualMovementPrefix.Length,
                    delimiterIndex - actualMovementPrefix.Length);
                string allowedMovement = reason.Substring(
                    delimiterIndex + allowedMovementDelimiter.Length);
                return string.Format(
                    LocalizeText(
                        kReasonIntersectionMovementFormatLocaleId,
                        "Actual {0}, allowed {1}"),
                    actualMovement,
                    allowedMovement);
            }

            return reason;
        }

        private static string NormalizeReasonText(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return string.Empty;
            }

            return reason
                .Replace("public-transport-lane", "PT-lane")
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();
        }

        private string BuildRepeatPenaltySummaryText(int baseFine, int adjustedFine)
        {
            return string.Format(
                LocalizeText(
                    kReasonRepeatPenaltyAppliedFormatLocaleId,
                    "Repeat offender {0} -> {1}"),
                baseFine,
                adjustedFine);
        }

        private string BuildRepeatPenaltySummaryText()
        {
            return LocalizeText(
                kReasonRepeatPenaltyAppliedLocaleId,
                "Repeat offender multiplier applied");
        }

        private void SplitRepeatPenaltyReason(
            string normalizedReason,
            out string baseReason,
            out string repeatPenaltyText)
        {
            const string marker = " Repeat offender multiplier applied: ";

            repeatPenaltyText = string.Empty;
            baseReason = normalizedReason;

            if (string.IsNullOrWhiteSpace(normalizedReason))
            {
                return;
            }

            int markerIndex = normalizedReason.IndexOf(marker);
            if (markerIndex < 0)
            {
                return;
            }

            baseReason = normalizedReason.Substring(0, markerIndex).Trim();

            string repeatPayload = normalizedReason.Substring(markerIndex + marker.Length).Trim();
            repeatPayload = repeatPayload.TrimEnd('.');

            string[] parts = repeatPayload.Split(new[] { " -> " }, System.StringSplitOptions.None);
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int baseFine) &&
                int.TryParse(parts[1], out int adjustedFine))
            {
                repeatPenaltyText = BuildRepeatPenaltySummaryText(baseFine, adjustedFine);
                return;
            }

            repeatPenaltyText = BuildRepeatPenaltySummaryText();
        }

        private string BuildPermissionStateSummary(
            Entity vehicle,
            bool hasTrafficLawProfile)
        {
            if (vehicle == Entity.Null)
            {
                return string.Empty;
            }

            if (!m_PermissionStateData.TryGetComponent(
                    vehicle,
                    out PublicTransportLanePermissionState permissionState))
            {
                return hasTrafficLawProfile
                    ? "No active permission state"
                    : "Not tracked by Traffic Law Enforcement";
            }

            PublicTransportLaneAccessBits accessBits =
                permissionState.m_PublicTransportLaneAccessBits;

            bool emergencyActive =
                permissionState.m_EmergencyActive != 0;

            bool emergencyOverrideActive =
                PublicTransportLanePolicy.HasEmergencyPublicTransportLaneOverride(
                    accessBits,
                    emergencyActive);

            return
                $"type={PublicTransportLanePolicy.DescribeType(accessBits)}, " +
                $"vanillaAllow={PublicTransportLanePolicy.VanillaAllowsAccess(accessBits)}, " +
                $"modAllow={PublicTransportLanePolicy.ModAllowsAccess(accessBits)}, " +
                $"canUsePublicTransportLane={PublicTransportLanePolicy.CanUsePublicTransportLane(accessBits, emergencyActive)}, " +
                $"vanillaPrefer={PublicTransportLanePolicy.VanillaPrefersLanes(accessBits)}, " +
                $"modPrefer={PublicTransportLanePolicy.ModPrefersLanes(accessBits)}, " +
                $"changedByMod={PublicTransportLanePolicy.PermissionChangedByMod(accessBits)}, " +
                $"emergency={emergencyActive}, " +
                $"emergencyOverride={emergencyOverrideActive}, " +
                $"graceConsumed={permissionState.m_ImmediateEntryGraceConsumed != 0}";
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

