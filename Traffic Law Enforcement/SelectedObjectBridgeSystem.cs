using System.Collections.Generic;
using System.Text;
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
        [System.Flags]
        private enum MinimalSnapshotConsumerSource
        {
            None = 0,
            FocusedLoggingPanel = 1 << 0,
            SelectedObjectPanel = 1 << 1,
        }

        private static MinimalSnapshotConsumerSource s_MinimalSnapshotConsumers;
        private static bool s_DetailedSnapshotConsumerActive;
        private static bool s_SummaryRequested;
        private static bool s_DebugFieldsRequested;
        private static bool s_LaneDetailsRequested;
        private static bool s_RouteDiagnosticsRequested;
        private static bool s_LaneDetailsConsumerActive;
        private static bool s_RouteDiagnosticsConsumerActive;
        private static readonly Dictionary<int, string> s_LocalizedRoadVehicleRoleDescriptionCache =
            new Dictionary<int, string>();
        private static string s_LocalizedRoadVehicleRoleLocaleId = string.Empty;

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
        internal const string kRoleListSeparatorLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.ListSeparator";
        internal const string kRoleEmergencyFormatLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.Role.EmergencyFormat";
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
        internal const string kPublicTransportLanePolicyQualifierSeparatorLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.PublicTransportLanePolicyQualifierSeparator";
        internal const string kPublicTransportLanePolicyQualifiedFormatLocaleId =
            "TrafficLawEnforcement.SelectedObjectPanel.Text.PublicTransportLanePolicyQualifiedFormat";
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
        private Game.UI.NameSystem m_NameSystem;
        private Game.Prefabs.PrefabSystem m_PrefabSystem;

        private SelectedObjectDebugSnapshot m_CurrentSnapshot;
        private bool m_HasSnapshot;
        private int m_LastSnapshotSettingsVersion = -1;
        private Entity m_LastHydratedSelectionSourceEntity;
        private Entity m_LastHydratedResolvedVehicleEntity;
        private int m_SelectionHydrationPhase = 3;
        public SelectedObjectDebugSnapshot CurrentSnapshot => m_CurrentSnapshot;
        public bool HasSnapshot => m_HasSnapshot;
        internal bool AreLaneDetailsHydrated => m_SelectionHydrationPhase >= 1;
        internal bool AreRouteDiagnosticsHydrated => m_SelectionHydrationPhase >= 2;

        internal static void SetDetailedSnapshotConsumerActive(bool active)
        {
            s_DetailedSnapshotConsumerActive = active;
        }

        internal static void SetFocusedLoggingMinimalSnapshotConsumerActive(bool active)
        {
            SetMinimalSnapshotConsumerActive(
                MinimalSnapshotConsumerSource.FocusedLoggingPanel,
                active);
        }

        internal static void SetSelectedObjectPanelMinimalSnapshotConsumerActive(bool active)
        {
            SetMinimalSnapshotConsumerActive(
                MinimalSnapshotConsumerSource.SelectedObjectPanel,
                active);
        }

        internal static void SetLaneDetailsConsumerActive(bool active)
        {
            s_LaneDetailsConsumerActive = active;
        }

        internal static void SetRouteDiagnosticsConsumerActive(bool active)
        {
            s_RouteDiagnosticsConsumerActive = active;
        }

        internal static void RequestDebugFieldsSnapshot()
        {
            s_DebugFieldsRequested = true;
        }

        internal static void RequestSummarySnapshot()
        {
            s_SummaryRequested = true;
        }

        internal static void RequestLaneDetailsSnapshot()
        {
            s_LaneDetailsRequested = true;
        }

        internal static void RequestRouteDiagnosticsSnapshot()
        {
            s_RouteDiagnosticsRequested = true;
        }

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
            m_NameSystem = World.GetOrCreateSystemManaged<Game.UI.NameSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<Game.Prefabs.PrefabSystem>();
        }

        protected override void OnUpdate()
        {
            bool summaryRequested = ConsumeSummaryRequest();
            bool debugFieldsRequested = ConsumeDebugFieldsRequest();
            bool laneDetailsRequested = ConsumeLaneDetailsRequest();
            bool routeDiagnosticsRequested = ConsumeRouteDiagnosticsRequest();
            bool includeSummaryFields =
                s_DetailedSnapshotConsumerActive ||
                summaryRequested;
            bool includeGeneralDebugFields = debugFieldsRequested;
            bool includeRouteDiagnosticsDebugFields = routeDiagnosticsRequested;
            bool includeLaneDetailsFields =
                s_LaneDetailsConsumerActive ||
                laneDetailsRequested;
            bool includeRouteDiagnosticsDisplayFields =
                s_RouteDiagnosticsConsumerActive ||
                routeDiagnosticsRequested;
            bool includeDeferredRouteDiagnosticsFields =
                includeRouteDiagnosticsDisplayFields;
            bool includeFormatterFields =
                includeLaneDetailsFields ||
                includeRouteDiagnosticsDisplayFields;
            bool includeCurrentLaneFields =
                includeLaneDetailsFields ||
                includeRouteDiagnosticsDisplayFields ||
                summaryRequested;
            bool includePathStateFields =
                s_DetailedSnapshotConsumerActive ||
                includeRouteDiagnosticsDisplayFields ||
                summaryRequested;
            bool includeProfileData =
                includeSummaryFields ||
                includeRouteDiagnosticsDisplayFields;
            bool includeViolationStateFields = includeSummaryFields;
            bool includePermissionStateSummary = includeGeneralDebugFields;
            bool buildDetailedSnapshot =
                s_DetailedSnapshotConsumerActive ||
                summaryRequested ||
                debugFieldsRequested ||
                laneDetailsRequested ||
                routeDiagnosticsRequested;
            bool buildAnySnapshot =
                s_MinimalSnapshotConsumers != MinimalSnapshotConsumerSource.None ||
                buildDetailedSnapshot;

            if (!buildAnySnapshot)
            {
                m_HasSnapshot = false;
                return;
            }

            m_SelectedObjectResolver.Update(this);
            SelectedObjectResolveResult resolveResult =
                m_SelectedObjectResolver.ResolveCurrentSelection();
            SelectedObjectTleApplicability tleApplicability =
                GetTleApplicability(resolveResult);
            bool tleApplicable =
                tleApplicability != SelectedObjectTleApplicability.NotApplicable;
            bool tleReady =
                tleApplicability == SelectedObjectTleApplicability.ApplicableReady;
            bool selectionIdentityChanged =
                HasSelectionIdentityChanged(resolveResult);
            if (selectionIdentityChanged)
            {
                m_LastHydratedSelectionSourceEntity = resolveResult.SourceSelectedEntity;
                m_LastHydratedResolvedVehicleEntity = resolveResult.ResolvedVehicleEntity;
                m_SelectionHydrationPhase = 0;
            }

            bool panelSelectionHydrationActive =
                buildDetailedSnapshot &&
                s_DetailedSnapshotConsumerActive &&
                !summaryRequested &&
                !debugFieldsRequested &&
                !laneDetailsRequested &&
                !routeDiagnosticsRequested &&
                resolveResult.HasSelection &&
                resolveResult.IsVehicle;
            if (panelSelectionHydrationActive)
            {
                if (s_LaneDetailsConsumerActive && m_SelectionHydrationPhase < 1)
                {
                    includeLaneDetailsFields = false;
                }

                if (s_RouteDiagnosticsConsumerActive && m_SelectionHydrationPhase < 2)
                {
                    includeRouteDiagnosticsDisplayFields = false;
                }

                if (s_RouteDiagnosticsConsumerActive && m_SelectionHydrationPhase < 3)
                {
                    includeDeferredRouteDiagnosticsFields = false;
                }

                includeFormatterFields =
                    includeLaneDetailsFields ||
                    includeRouteDiagnosticsDisplayFields;
                includeCurrentLaneFields =
                    includeLaneDetailsFields ||
                    includeRouteDiagnosticsDisplayFields;
                includePathStateFields =
                    s_DetailedSnapshotConsumerActive ||
                    includeRouteDiagnosticsDisplayFields;
                includeProfileData =
                    includeSummaryFields ||
                    includeRouteDiagnosticsDisplayFields;
            }

            if (buildDetailedSnapshot &&
                (!resolveResult.HasSelection || !resolveResult.IsVehicle))
            {
                m_CurrentSnapshot = BuildSnapshot(
                    resolveResult,
                    buildDetailedSnapshot,
                    includeSummaryFields,
                    includeGeneralDebugFields,
                    includeRouteDiagnosticsDebugFields,
                    includePermissionStateSummary,
                    includeViolationStateFields,
                    includeLaneDetailsFields,
                    includeRouteDiagnosticsDisplayFields,
                    includeDeferredRouteDiagnosticsFields,
                    includeCurrentLaneFields,
                    includePathStateFields);
                m_LastSnapshotSettingsVersion = EnforcementGameplaySettingsService.Version;
                m_HasSnapshot = true;
                AdvanceSelectionHydrationPhase(panelSelectionHydrationActive);
                return;
            }

            if (buildDetailedSnapshot)
            {
                if (includeCurrentLaneFields && tleReady)
                {
                    m_TargetData.Update(this);
                    m_CurrentRouteData.Update(this);
                }
                if (includePathStateFields && tleReady)
                {
                    m_PathOwnerData.Update(this);
                }
                if (includeRouteDiagnosticsDisplayFields && tleReady)
                {
                    m_RouteLaneData.Update(this);
                    m_WaypointData.Update(this);
                    m_ConnectedData.Update(this);
                    m_NavigationLaneData.Update(this);
                }
                if (includeRouteDiagnosticsDebugFields && tleReady)
                {
                    m_PathElementData.Update(this);
                }
                if (includeCurrentLaneFields && tleReady)
                {
                    m_CurrentLaneData.Update(this);
                }
                if (includeLaneDetailsFields && tleReady)
                {
                    m_HistoryData.Update(this);
                }
                if (includeProfileData && tleApplicable)
                {
                    m_ProfileData.Update(this);
                }
                if (includeViolationStateFields && tleReady)
                {
                    m_PublicTransportLaneViolationData.Update(this);
                    m_PendingExitData.Update(this);
                }
                if (includePermissionStateSummary && tleApplicable)
                {
                    m_PermissionStateData.Update(this);
                }
                if (includeFormatterFields && tleReady)
                {
                    m_OwnerData.Update(this);
                    m_AggregatedData.Update(this);
                    m_SlaveLaneData.Update(this);
                    m_CarLaneData.Update(this);
                    m_EdgeLaneData.Update(this);
                    m_ParkingLaneData.Update(this);
                    m_GarageLaneData.Update(this);
                    m_ConnectionLaneData.Update(this);
                }
            }

            m_CurrentSnapshot = BuildSnapshot(
                resolveResult,
                buildDetailedSnapshot,
                includeSummaryFields,
                includeGeneralDebugFields,
                includeRouteDiagnosticsDebugFields,
                includePermissionStateSummary,
                includeViolationStateFields,
                includeLaneDetailsFields,
                includeRouteDiagnosticsDisplayFields,
                includeDeferredRouteDiagnosticsFields,
                includeCurrentLaneFields,
                includePathStateFields);
            m_LastSnapshotSettingsVersion = EnforcementGameplaySettingsService.Version;
            m_HasSnapshot = true;
            AdvanceSelectionHydrationPhase(panelSelectionHydrationActive);
        }

        private SelectedObjectDebugSnapshot BuildSnapshot(
            SelectedObjectResolveResult resolveResult,
            bool buildDetailedSnapshot,
            bool includeSummaryFields,
            bool includeGeneralDebugFields,
            bool includeRouteDiagnosticsDebugFields,
            bool includePermissionStateSummary,
            bool includeViolationStateFields,
            bool includeLaneDetailsFields,
            bool includeRouteDiagnosticsDisplayFields,
            bool includeDeferredRouteDiagnosticsFields,
            bool includeCurrentLaneFields,
            bool includePathStateFields)
        {
            Entity vehicle = resolveResult.ResolvedVehicleEntity;
            bool hasVehicleEntity = vehicle != Entity.Null;
            SelectedObjectTleApplicability tleApplicability =
                GetTleApplicability(resolveResult);
            bool includeRoleText =
                s_DetailedSnapshotConsumerActive ||
                s_MinimalSnapshotConsumers != MinimalSnapshotConsumerSource.None;
            bool needsTypeLookups =
                resolveResult.VehicleKind == SelectedObjectKind.RoadCar &&
                ((!buildDetailedSnapshot &&
                  tleApplicability == SelectedObjectTleApplicability.ApplicableReady) ||
                 (buildDetailedSnapshot &&
                  (includeRoleText || includeRouteDiagnosticsDisplayFields)));

            if (needsTypeLookups)
            {
                m_TypeLookups.Update(this);
            }

            if (!buildDetailedSnapshot)
            {
                return BuildMinimalSnapshot(
                    resolveResult,
                    tleApplicability,
                    vehicle,
                    hasVehicleEntity,
                    CanRetainDeferredSnapshotFields(
                        resolveResult,
                        tleApplicability));
            }

            bool tleApplicable =
                tleApplicability != SelectedObjectTleApplicability.NotApplicable;
            bool tleReady =
                tleApplicability == SelectedObjectTleApplicability.ApplicableReady;

            bool hasTrafficLawProfile =
                includeSummaryFields &&
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
            if (includeLaneDetailsFields &&
                tleReady &&
                hasVehicleEntity &&
                m_HistoryData.TryGetComponent(vehicle, out VehicleLaneHistory history))
            {
                previousLaneEntity = history.m_PreviousLane;
                laneChangeCount = history.m_LaneChangeCount;
            }

            Target currentTarget = default;
            bool hasCurrentTarget =
                includeCurrentLaneFields &&
                tleReady &&
                hasVehicleEntity &&
                m_TargetData.TryGetComponent(vehicle, out currentTarget) &&
                currentTarget.m_Target != Entity.Null;
            Entity currentTargetEntity =
                hasCurrentTarget
                    ? currentTarget.m_Target
                    : Entity.Null;
            CurrentRoute currentRoute = default;
            bool hasCurrentRoute =
                includeCurrentLaneFields &&
                tleReady &&
                hasVehicleEntity &&
                m_CurrentRouteData.TryGetComponent(vehicle, out currentRoute) &&
                currentRoute.m_Route != Entity.Null;
            Entity currentRouteEntity =
                hasCurrentRoute
                    ? currentRoute.m_Route
                    : Entity.Null;
            PathOwner pathOwner = default;
            bool hasPathOwner =
                includePathStateFields &&
                tleReady &&
                hasVehicleEntity &&
                m_PathOwnerData.TryGetComponent(vehicle, out pathOwner);
            PathFlags currentPathFlags = hasPathOwner ? pathOwner.m_State : default;

            bool ptLaneViolationActive =
                includeViolationStateFields &&
                tleReady &&
                hasVehicleEntity &&
                m_PublicTransportLaneViolationData.HasComponent(vehicle);

            bool pendingExitActive =
                includeViolationStateFields &&
                tleReady &&
                hasVehicleEntity &&
                m_PendingExitData.HasComponent(vehicle);

            int totalFines = 0;
            int totalViolations = 0;
            string lastReason = string.Empty;
            SelectedObjectDisplayFormatterContext formatterContext = default;
            bool hasFormatterContext = false;
            int settingsVersion = EnforcementGameplaySettingsService.Version;

            if (includeSummaryFields && tleApplicable && resolveResult.IsVehicle)
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

            bool canReuseLaneDisplayText =
                includeLaneDetailsFields &&
                CanReuseLaneDisplayText(
                    resolveResult,
                    tleApplicability,
                    currentLaneEntity,
                    previousLaneEntity);
            bool canReuseRouteDiagnostics =
                includeRouteDiagnosticsDisplayFields &&
                !includeRouteDiagnosticsDebugFields &&
                CanReuseRouteDiagnostics(
                    resolveResult,
                    tleApplicability,
                    currentLaneEntity,
                    currentTargetEntity,
                    currentRouteEntity,
                    currentPathFlags,
                    includeDeferredRouteDiagnosticsFields);
            bool canReuseSummaryPresentation =
                CanReuseSummaryPresentationFields(
                    resolveResult,
                    tleApplicability);
            bool canReuseEnforcementSummary =
                tleApplicable &&
                (includeSummaryFields || includePermissionStateSummary) &&
                CanReuseEnforcementSummary(
                    resolveResult,
                    tleApplicability,
                    hasTrafficLawProfile,
                    lastReason,
                    includePermissionStateSummary);

            if (CanReuseDetailedSnapshot(
                    resolveResult,
                    tleApplicability,
                    settingsVersion,
                    hasTrafficLawProfile,
                    currentLaneEntity,
                    previousLaneEntity,
                    laneChangeCount,
                    ptLaneViolationActive,
                    pendingExitActive,
                    totalFines,
                    totalViolations,
                    lastReason,
                    hasPathOwner,
                    hasCurrentTarget,
                    hasCurrentRoute,
                    currentPathFlags,
                    currentTargetEntity,
                    currentRouteEntity,
                    includeGeneralDebugFields,
                    includeRouteDiagnosticsDebugFields,
                    includePermissionStateSummary,
                    includeLaneDetailsFields,
                    canReuseLaneDisplayText,
                    includeRouteDiagnosticsDisplayFields,
                    canReuseRouteDiagnostics))
            {
                return m_CurrentSnapshot;
            }

            bool needsFormatterContext =
                (includeRouteDiagnosticsDisplayFields && !canReuseRouteDiagnostics) ||
                (includeLaneDetailsFields &&
                 !canReuseLaneDisplayText &&
                 (currentLaneEntity != Entity.Null || previousLaneEntity != Entity.Null));

            if (needsFormatterContext)
            {
                formatterContext = CreateDisplayFormatterContext();
                hasFormatterContext = true;
            }

            bool routeDiagnosticsAvailable =
                tleReady &&
                resolveResult.VehicleKind == SelectedObjectKind.RoadCar &&
                vehicle != Entity.Null;
            SelectedObjectRouteDiagnosticsData routeDiagnostics = default;
            if (routeDiagnosticsAvailable && includeRouteDiagnosticsDisplayFields)
            {
                if (canReuseRouteDiagnostics)
                {
                    routeDiagnostics = BuildRouteDiagnosticsFromSnapshot(m_CurrentSnapshot);
                }
                else
                {
                    if (!hasFormatterContext)
                    {
                        formatterContext = CreateDisplayFormatterContext();
                        hasFormatterContext = true;
                    }

                    SelectedObjectRouteDiagnosticsContext routeDiagnosticsContext =
                        CreateRouteDiagnosticsContext(formatterContext);
                    routeDiagnostics =
                        SelectedObjectRouteDiagnosticsBuilder.Build(
                            resolveResult,
                            tleReady,
                            vehicle,
                            currentLaneEntity,
                            includeRouteDiagnosticsDebugFields,
                            includeDeferredRouteDiagnosticsFields,
                            ref routeDiagnosticsContext);
                }
            }

            SelectedObjectEnforcementSummaryData enforcementSummary = default;
            if (tleApplicable && (includeSummaryFields || includePermissionStateSummary))
            {
                if (canReuseEnforcementSummary)
                {
                    enforcementSummary =
                        BuildEnforcementSummaryFromSnapshot(
                            m_CurrentSnapshot,
                            includePermissionStateSummary);
                }
                else
                {
                    SelectedObjectEnforcementSummaryContext enforcementSummaryContext =
                        CreateEnforcementSummaryContext();
                    enforcementSummary =
                        SelectedObjectEnforcementSummaryBuilder.Build(
                            resolveResult,
                            tleApplicable,
                            hasTrafficLawProfile,
                            vehicle,
                            lastReason,
                            includePermissionStateSummary,
                            ref enforcementSummaryContext);
                }
            }

            string summaryClassificationText =
                canReuseSummaryPresentation
                    ? m_CurrentSnapshot.SummaryClassificationText
                    : BuildSummaryClassificationText(resolveResult);
            string summaryTleStatusText =
                canReuseSummaryPresentation
                    ? m_CurrentSnapshot.SummaryTleStatusText
                    : BuildSummaryTleStatusText(resolveResult, tleApplicability);
            string roleText =
                !includeRoleText
                    ? string.Empty
                    : canReuseSummaryPresentation
                    ? m_CurrentSnapshot.RoleText
                    : BuildRoleText(resolveResult);

            string currentRouteColorText =
                includeRouteDiagnosticsDisplayFields &&
                canReuseRouteDiagnostics
                    ? m_CurrentSnapshot.CurrentRouteColorText
                    : string.Empty;
            if (includeRouteDiagnosticsDisplayFields &&
                !canReuseRouteDiagnostics &&
                currentRouteEntity != Entity.Null &&
                EntityManager.HasComponent<Game.Routes.Color>(currentRouteEntity))
            {
                UnityEngine.Color32 color =
                    EntityManager.GetComponentData<Game.Routes.Color>(currentRouteEntity).m_Color;
                currentRouteColorText = $"#{color.r:X2}{color.g:X2}{color.b:X2}";
            }

            string currentLaneText =
                !includeLaneDetailsFields
                    ? string.Empty
                    : canReuseLaneDisplayText
                    ? m_CurrentSnapshot.CurrentLaneText
                    : currentLaneEntity == Entity.Null
                    ? SelectedObjectDisplayFormatter.FormatEntityOrNone(Entity.Null)
                    : SelectedObjectDisplayFormatter.BuildLaneDisplayText(
                        currentLaneEntity,
                        ref formatterContext);
            string previousLaneText =
                !includeLaneDetailsFields
                    ? string.Empty
                    : canReuseLaneDisplayText
                    ? m_CurrentSnapshot.PreviousLaneText
                    : previousLaneEntity == Entity.Null
                    ? SelectedObjectDisplayFormatter.FormatEntityOrNone(Entity.Null)
                    : SelectedObjectDisplayFormatter.BuildLaneDisplayText(
                        previousLaneEntity,
                        ref formatterContext);

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
                includeGeneralDebugFields ? BuildRuntimeFamilyText(resolveResult) : string.Empty,
                includeGeneralDebugFields ? BuildRawTransportTypeText(resolveResult) : string.Empty,
                includeGeneralDebugFields ? BuildRawTrackTypeText(resolveResult) : string.Empty,
                includeGeneralDebugFields ? BuildRailSubtypeSourceText(resolveResult) : string.Empty,
                summaryClassificationText,
                summaryTleStatusText,
                enforcementSummary.CompactLastReasonText,
                enforcementSummary.CompactRepeatPenaltyText,
                resolveResult.IsVehicle && hasVehicleEntity ? vehicle.Index : -1,
                roleText,
                enforcementSummary.PublicTransportLanePolicyText,
                hasTrafficLawProfile,
                currentLaneEntity,
                previousLaneEntity,
                currentLaneText,
                previousLaneText,
                laneChangeCount,
                ptLaneViolationActive,
                pendingExitActive,
                enforcementSummary.PermissionStateSummary,
                totalFines,
                totalViolations,
                lastReason,
                hasPathOwner,
                hasCurrentTarget,
                hasCurrentRoute,
                currentPathFlags,
                includeRouteDiagnosticsDisplayFields ? currentTargetEntity : Entity.Null,
                includeRouteDiagnosticsDisplayFields ? currentRouteEntity : Entity.Null,
                currentRouteColorText,
                routeDiagnosticsAvailable,
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

        private bool CanReuseLaneDisplayText(
            SelectedObjectResolveResult resolveResult,
            SelectedObjectTleApplicability tleApplicability,
            Entity currentLaneEntity,
            Entity previousLaneEntity)
        {
            return m_HasSnapshot &&
                !string.IsNullOrEmpty(m_CurrentSnapshot.CurrentLaneText) &&
                !string.IsNullOrEmpty(m_CurrentSnapshot.PreviousLaneText) &&
                resolveResult.SourceSelectedEntity == m_CurrentSnapshot.SourceSelectedEntity &&
                resolveResult.ResolvedVehicleEntity == m_CurrentSnapshot.ResolvedVehicleEntity &&
                tleApplicability == m_CurrentSnapshot.TleApplicability &&
                currentLaneEntity == m_CurrentSnapshot.CurrentLaneEntity &&
                previousLaneEntity == m_CurrentSnapshot.PreviousLaneEntity;
        }

        private bool HasSelectionIdentityChanged(
            SelectedObjectResolveResult resolveResult)
        {
            return resolveResult.SourceSelectedEntity != m_LastHydratedSelectionSourceEntity ||
                resolveResult.ResolvedVehicleEntity != m_LastHydratedResolvedVehicleEntity;
        }

        private void AdvanceSelectionHydrationPhase(bool panelSelectionHydrationActive)
        {
            if (!panelSelectionHydrationActive || m_SelectionHydrationPhase >= 3)
            {
                return;
            }

            m_SelectionHydrationPhase++;
        }

        private static SelectedObjectEnforcementSummaryData BuildEnforcementSummaryFromSnapshot(
            SelectedObjectDebugSnapshot snapshot,
            bool includePermissionStateSummary)
        {
            return new SelectedObjectEnforcementSummaryData(
                snapshot.CompactLastReasonText,
                snapshot.CompactRepeatPenaltyText,
                snapshot.PublicTransportLanePolicyText,
                includePermissionStateSummary
                    ? snapshot.PermissionStateSummary
                    : string.Empty);
        }

        private bool CanReuseSummaryPresentationFields(
            SelectedObjectResolveResult resolveResult,
            SelectedObjectTleApplicability tleApplicability)
        {
            return m_HasSnapshot &&
                resolveResult.IsVehicle &&
                !string.IsNullOrEmpty(m_CurrentSnapshot.SummaryClassificationText) &&
                !string.IsNullOrEmpty(m_CurrentSnapshot.SummaryTleStatusText) &&
                resolveResult.ResolveState == m_CurrentSnapshot.ResolveState &&
                resolveResult.VehicleKind == m_CurrentSnapshot.VehicleKind &&
                tleApplicability == m_CurrentSnapshot.TleApplicability &&
                resolveResult.SourceSelectedEntity == m_CurrentSnapshot.SourceSelectedEntity &&
                resolveResult.ResolvedVehicleEntity == m_CurrentSnapshot.ResolvedVehicleEntity &&
                resolveResult.PrefabEntity == m_CurrentSnapshot.PrefabEntity &&
                resolveResult.HasPrefabRef == m_CurrentSnapshot.HasPrefabRef &&
                resolveResult.IsVehicle == m_CurrentSnapshot.IsVehicle &&
                resolveResult.IsCar == m_CurrentSnapshot.IsCar &&
                resolveResult.IsTrain == m_CurrentSnapshot.IsTrain &&
                resolveResult.IsParked == m_CurrentSnapshot.IsParked;
        }

        private bool CanReuseEnforcementSummary(
            SelectedObjectResolveResult resolveResult,
            SelectedObjectTleApplicability tleApplicability,
            bool hasTrafficLawProfile,
            string lastReason,
            bool includePermissionStateSummary)
        {
            return m_HasSnapshot &&
                !includePermissionStateSummary &&
                !string.IsNullOrEmpty(m_CurrentSnapshot.CompactLastReasonText) &&
                resolveResult.ResolveState == m_CurrentSnapshot.ResolveState &&
                resolveResult.VehicleKind == m_CurrentSnapshot.VehicleKind &&
                tleApplicability == m_CurrentSnapshot.TleApplicability &&
                resolveResult.SourceSelectedEntity == m_CurrentSnapshot.SourceSelectedEntity &&
                resolveResult.ResolvedVehicleEntity == m_CurrentSnapshot.ResolvedVehicleEntity &&
                resolveResult.IsVehicle == m_CurrentSnapshot.IsVehicle &&
                hasTrafficLawProfile == m_CurrentSnapshot.HasTrafficLawProfile &&
                string.Equals(
                    lastReason ?? string.Empty,
                    m_CurrentSnapshot.LastReason ?? string.Empty,
                    System.StringComparison.Ordinal);
        }

        private bool CanReuseDetailedSnapshot(
            SelectedObjectResolveResult resolveResult,
            SelectedObjectTleApplicability tleApplicability,
            int settingsVersion,
            bool hasTrafficLawProfile,
            Entity currentLaneEntity,
            Entity previousLaneEntity,
            int laneChangeCount,
            bool ptLaneViolationActive,
            bool pendingExitActive,
            int totalFines,
            int totalViolations,
            string lastReason,
            bool hasPathOwner,
            bool hasCurrentTarget,
            bool hasCurrentRoute,
            PathFlags currentPathFlags,
            Entity currentTargetEntity,
            Entity currentRouteEntity,
            bool includeGeneralDebugFields,
            bool includeRouteDiagnosticsDebugFields,
            bool includePermissionStateSummary,
            bool includeLaneDetailsFields,
            bool canReuseLaneDisplayText,
            bool includeRouteDiagnosticsDisplayFields,
            bool canReuseRouteDiagnostics)
        {
            bool laneDetailsStateMatches =
                !includeLaneDetailsFields ||
                (currentLaneEntity == m_CurrentSnapshot.CurrentLaneEntity &&
                 previousLaneEntity == m_CurrentSnapshot.PreviousLaneEntity &&
                 laneChangeCount == m_CurrentSnapshot.LaneChangeCount &&
                 hasCurrentTarget == m_CurrentSnapshot.HasCurrentTarget &&
                 hasCurrentRoute == m_CurrentSnapshot.HasCurrentRoute &&
                 canReuseLaneDisplayText);
            bool routeDiagnosticsStateMatches =
                !includeRouteDiagnosticsDisplayFields ||
                (currentTargetEntity == m_CurrentSnapshot.CurrentTargetEntity &&
                 currentRouteEntity == m_CurrentSnapshot.CurrentRouteEntity &&
                 canReuseRouteDiagnostics);

            return m_HasSnapshot &&
                !includeGeneralDebugFields &&
                !includeRouteDiagnosticsDebugFields &&
                !includePermissionStateSummary &&
                settingsVersion == m_LastSnapshotSettingsVersion &&
                resolveResult.ResolveState == m_CurrentSnapshot.ResolveState &&
                resolveResult.VehicleKind == m_CurrentSnapshot.VehicleKind &&
                tleApplicability == m_CurrentSnapshot.TleApplicability &&
                resolveResult.SourceSelectedEntity == m_CurrentSnapshot.SourceSelectedEntity &&
                resolveResult.ResolvedVehicleEntity == m_CurrentSnapshot.ResolvedVehicleEntity &&
                resolveResult.PrefabEntity == m_CurrentSnapshot.PrefabEntity &&
                resolveResult.HasPrefabRef == m_CurrentSnapshot.HasPrefabRef &&
                resolveResult.IsTrailerChild == m_CurrentSnapshot.IsTrailerChild &&
                resolveResult.IsVehicle == m_CurrentSnapshot.IsVehicle &&
                resolveResult.IsCar == m_CurrentSnapshot.IsCar &&
                resolveResult.IsTrain == m_CurrentSnapshot.IsTrain &&
                resolveResult.IsParked == m_CurrentSnapshot.IsParked &&
                resolveResult.HasCarCurrentLane == m_CurrentSnapshot.HasCarCurrentLane &&
                resolveResult.HasTrainCurrentLane == m_CurrentSnapshot.HasTrainCurrentLane &&
                resolveResult.HasLiveLaneData == m_CurrentSnapshot.HasLiveLaneData &&
                resolveResult.HasPublicTransportVehicleData == m_CurrentSnapshot.HasPublicTransportVehicleData &&
                resolveResult.HasTrainData == m_CurrentSnapshot.HasTrainData &&
                hasTrafficLawProfile == m_CurrentSnapshot.HasTrafficLawProfile &&
                ptLaneViolationActive == m_CurrentSnapshot.PublicTransportLaneViolationActive &&
                pendingExitActive == m_CurrentSnapshot.PendingExitActive &&
                totalFines == m_CurrentSnapshot.TotalFines &&
                totalViolations == m_CurrentSnapshot.TotalViolations &&
                string.Equals(
                    lastReason ?? string.Empty,
                    m_CurrentSnapshot.LastReason ?? string.Empty,
                    System.StringComparison.Ordinal) &&
                hasPathOwner == m_CurrentSnapshot.HasPathOwner &&
                currentPathFlags == m_CurrentSnapshot.CurrentPathFlags &&
                laneDetailsStateMatches &&
                routeDiagnosticsStateMatches;
        }

        private bool CanReuseRouteDiagnostics(
            SelectedObjectResolveResult resolveResult,
            SelectedObjectTleApplicability tleApplicability,
            Entity currentLaneEntity,
            Entity currentTargetEntity,
            Entity currentRouteEntity,
            PathFlags currentPathFlags,
            bool includeDeferredRouteDiagnosticsFields)
        {
            return m_HasSnapshot &&
                m_CurrentSnapshot.HasRouteDiagnostics &&
                (!includeDeferredRouteDiagnosticsFields ||
                 !string.IsNullOrEmpty(m_CurrentSnapshot.RouteDiagnosticsExplanationText)) &&
                resolveResult.SourceSelectedEntity == m_CurrentSnapshot.SourceSelectedEntity &&
                resolveResult.ResolvedVehicleEntity == m_CurrentSnapshot.ResolvedVehicleEntity &&
                tleApplicability == m_CurrentSnapshot.TleApplicability &&
                currentLaneEntity == m_CurrentSnapshot.CurrentLaneEntity &&
                currentTargetEntity == m_CurrentSnapshot.CurrentTargetEntity &&
                currentRouteEntity == m_CurrentSnapshot.CurrentRouteEntity &&
                currentPathFlags == m_CurrentSnapshot.CurrentPathFlags;
        }

        private static SelectedObjectRouteDiagnosticsData BuildRouteDiagnosticsFromSnapshot(
            SelectedObjectDebugSnapshot snapshot)
        {
            return new SelectedObjectRouteDiagnosticsData(
                snapshot.HasRouteDiagnostics,
                snapshot.HasPathOwner,
                snapshot.HasCurrentTarget,
                snapshot.HasCurrentRoute,
                snapshot.CurrentPathFlags,
                snapshot.RouteDiagnosticsCurrentTargetText,
                snapshot.RouteDiagnosticsCurrentRouteText,
                snapshot.RouteDiagnosticsTargetRoadText,
                snapshot.RouteDiagnosticsStartOwnerRoadText,
                snapshot.RouteDiagnosticsEndOwnerRoadText,
                snapshot.RouteDiagnosticsDirectConnectText,
                snapshot.RouteDiagnosticsFullPathToTargetStartText,
                snapshot.RouteDiagnosticsNavigationLanesText,
                snapshot.RouteDiagnosticsPlannedPenaltiesText,
                snapshot.RouteDiagnosticsPenaltyTagsText,
                snapshot.RouteDiagnosticsExplanationText,
                snapshot.RouteDiagnosticsWaypointRouteLaneText,
                snapshot.RouteDiagnosticsConnectedStopText);
        }

        private SelectedObjectDebugSnapshot BuildMinimalSnapshot(
            SelectedObjectResolveResult resolveResult,
            SelectedObjectTleApplicability tleApplicability,
            Entity vehicle,
            bool hasVehicleEntity,
            bool retainDeferredSnapshotFields)
        {
            bool routeDiagnosticsAvailable =
                tleApplicability == SelectedObjectTleApplicability.ApplicableReady &&
                resolveResult.VehicleKind == SelectedObjectKind.RoadCar &&
                hasVehicleEntity;
            string roleText =
                tleApplicability == SelectedObjectTleApplicability.ApplicableReady
                    ? BuildRoleText(resolveResult)
                    : string.Empty;

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
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.SummaryClassificationText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.SummaryTleStatusText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.CompactLastReasonText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.CompactRepeatPenaltyText
                    : string.Empty,
                resolveResult.IsVehicle && hasVehicleEntity ? vehicle.Index : -1,
                roleText,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.PublicTransportLanePolicyText
                    : string.Empty,
                retainDeferredSnapshotFields &&
                    m_CurrentSnapshot.HasTrafficLawProfile,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.CurrentLaneEntity
                    : Entity.Null,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.PreviousLaneEntity
                    : Entity.Null,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.CurrentLaneText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.PreviousLaneText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.LaneChangeCount
                    : 0,
                retainDeferredSnapshotFields &&
                    m_CurrentSnapshot.PublicTransportLaneViolationActive,
                retainDeferredSnapshotFields &&
                    m_CurrentSnapshot.PendingExitActive,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.PermissionStateSummary
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.TotalFines
                    : 0,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.TotalViolations
                    : 0,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.LastReason
                    : string.Empty,
                retainDeferredSnapshotFields &&
                    m_CurrentSnapshot.HasPathOwner,
                retainDeferredSnapshotFields &&
                    m_CurrentSnapshot.HasCurrentTarget,
                retainDeferredSnapshotFields &&
                    m_CurrentSnapshot.HasCurrentRoute,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.CurrentPathFlags
                    : default,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.CurrentTargetEntity
                    : Entity.Null,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.CurrentRouteEntity
                    : Entity.Null,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.CurrentRouteColorText
                    : string.Empty,
                routeDiagnosticsAvailable,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.RouteDiagnosticsCurrentTargetText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.RouteDiagnosticsCurrentRouteText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.RouteDiagnosticsTargetRoadText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.RouteDiagnosticsStartOwnerRoadText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.RouteDiagnosticsEndOwnerRoadText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.RouteDiagnosticsDirectConnectText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.RouteDiagnosticsFullPathToTargetStartText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.RouteDiagnosticsNavigationLanesText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.RouteDiagnosticsPlannedPenaltiesText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.RouteDiagnosticsPenaltyTagsText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.RouteDiagnosticsExplanationText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.RouteDiagnosticsWaypointRouteLaneText
                    : string.Empty,
                retainDeferredSnapshotFields
                    ? m_CurrentSnapshot.RouteDiagnosticsConnectedStopText
                    : string.Empty);
        }

        private bool CanRetainDeferredSnapshotFields(
            SelectedObjectResolveResult resolveResult,
            SelectedObjectTleApplicability tleApplicability)
        {
            return m_HasSnapshot &&
                resolveResult.ResolveState == m_CurrentSnapshot.ResolveState &&
                resolveResult.VehicleKind == m_CurrentSnapshot.VehicleKind &&
                tleApplicability == m_CurrentSnapshot.TleApplicability &&
                resolveResult.SourceSelectedEntity == m_CurrentSnapshot.SourceSelectedEntity &&
                resolveResult.ResolvedVehicleEntity == m_CurrentSnapshot.ResolvedVehicleEntity;
        }

        private SelectedObjectDisplayFormatterContext CreateDisplayFormatterContext()
        {
            return new SelectedObjectDisplayFormatterContext
            {
                EntityManager = EntityManager,
                NameSystem = m_NameSystem,
                PrefabSystem = m_PrefabSystem,
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

        private SelectedObjectRouteDiagnosticsContext CreateRouteDiagnosticsContext(
            SelectedObjectDisplayFormatterContext formatterContext)
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
                Formatter = formatterContext,
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
            string activeLocaleId = SelectedObjectLocalization.ActiveLocaleId;
            if (activeLocaleId != s_LocalizedRoadVehicleRoleLocaleId)
            {
                s_LocalizedRoadVehicleRoleLocaleId = activeLocaleId;
                s_LocalizedRoadVehicleRoleDescriptionCache.Clear();
            }

            PublicTransportLaneVehicleCategory authorizedCategories =
                PublicTransportLanePolicy.GetVanillaAuthorizedCategories(vehicle, ref m_TypeLookups);
            PublicTransportLaneFlagGrantExperimentRole additionalRole =
                PublicTransportLanePolicy.GetFlagGrantExperimentRole(vehicle, ref m_TypeLookups);
            bool isEmergencyVehicle =
                EmergencyVehiclePolicy.IsEmergencyVehicle(vehicle, ref m_TypeLookups);
            int cacheKey =
                CreateVehicleRoleDescriptionKey(
                    authorizedCategories,
                    additionalRole,
                    isEmergencyVehicle);

            if (s_LocalizedRoadVehicleRoleDescriptionCache.TryGetValue(cacheKey, out string cachedDescription))
            {
                return cachedDescription;
            }

            string description =
                BuildLocalizedRoadVehicleRoleDescription(
                    authorizedCategories,
                    additionalRole,
                    isEmergencyVehicle);
            s_LocalizedRoadVehicleRoleDescriptionCache[cacheKey] = description;
            return description;
        }

        private string BuildLocalizedRoadVehicleRoleDescription(
            PublicTransportLaneVehicleCategory authorizedCategories,
            PublicTransportLaneFlagGrantExperimentRole additionalRole,
            bool isEmergencyVehicle)
        {
            StringBuilder description = new StringBuilder(96);
            string roleListSeparator = LocalizeText(kRoleListSeparatorLocaleId, ", ");
            bool hasAnyName = false;

            AppendAuthorizedCategoryNamesLocalized(
                authorizedCategories,
                description,
                roleListSeparator,
                ref hasAnyName);

            if (additionalRole != PublicTransportLaneFlagGrantExperimentRole.None)
            {
                AppendRoleDescriptionPart(
                    description,
                    GetRoleDisplayNameLocalized(additionalRole),
                    roleListSeparator,
                    ref hasAnyName);
            }

            if (!hasAnyName)
            {
                AppendRoleDescriptionPart(
                    description,
                    LocalizeText(kRoleUnclassifiedRoadVehicleLocaleId, "Unclassified road vehicle"),
                    roleListSeparator,
                    ref hasAnyName);
            }

            string result = description.ToString();
            if (isEmergencyVehicle)
            {
                return string.Format(
                    LocalizeText(kRoleEmergencyFormatLocaleId, "{0} [{1}]"),
                    result,
                    LocalizeText(kRoleEmergencyQualifierLocaleId, "emergency"));
            }

            return result;
        }

        private static int CreateVehicleRoleDescriptionKey(
            PublicTransportLaneVehicleCategory categories,
            PublicTransportLaneFlagGrantExperimentRole role,
            bool isEmergencyVehicle)
        {
            return ((int)categories << 8) |
                ((int)role << 1) |
                (isEmergencyVehicle ? 1 : 0);
        }

        private static void AppendRoleDescriptionPart(
            StringBuilder description,
            string part,
            string separator,
            ref bool hasAnyName)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                return;
            }

            if (hasAnyName)
            {
                description.Append(separator);
            }

            description.Append(part);
            hasAnyName = true;
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
            StringBuilder description,
            string separator,
            ref bool hasAnyName)
        {
            if ((categories & PublicTransportLaneVehicleCategory.RoadPublicTransportVehicle) != 0)
            {
                AppendRoleDescriptionPart(
                    description,
                    LocalizeText(kRoleRoadPublicTransportLocaleId, "Road public transport vehicles"),
                    separator,
                    ref hasAnyName);
            }

            if ((categories & PublicTransportLaneVehicleCategory.Taxi) != 0)
            {
                AppendRoleDescriptionPart(
                    description,
                    LocalizeText(kRoleTaxiLocaleId, "Taxis"),
                    separator,
                    ref hasAnyName);
            }

            if ((categories & PublicTransportLaneVehicleCategory.PoliceCar) != 0)
            {
                AppendRoleDescriptionPart(
                    description,
                    LocalizeText(kRolePoliceCarLocaleId, "Police cars"),
                    separator,
                    ref hasAnyName);
            }

            if ((categories & PublicTransportLaneVehicleCategory.FireEngine) != 0)
            {
                AppendRoleDescriptionPart(
                    description,
                    LocalizeText(kRoleFireEngineLocaleId, "Fire engines"),
                    separator,
                    ref hasAnyName);
            }

            if ((categories & PublicTransportLaneVehicleCategory.Ambulance) != 0)
            {
                AppendRoleDescriptionPart(
                    description,
                    LocalizeText(kRoleAmbulanceLocaleId, "Ambulances"),
                    separator,
                    ref hasAnyName);
            }

            if ((categories & PublicTransportLaneVehicleCategory.GarbageTruck) != 0)
            {
                AppendRoleDescriptionPart(
                    description,
                    LocalizeText(kRoleGarbageTruckLocaleId, "Garbage trucks"),
                    separator,
                    ref hasAnyName);
            }

            if ((categories & PublicTransportLaneVehicleCategory.PostVan) != 0)
            {
                AppendRoleDescriptionPart(
                    description,
                    LocalizeText(kRolePostVanLocaleId, "Post vans"),
                    separator,
                    ref hasAnyName);
            }

            if ((categories & PublicTransportLaneVehicleCategory.RoadMaintenanceVehicle) != 0)
            {
                AppendRoleDescriptionPart(
                    description,
                    LocalizeText(kRoleRoadMaintenanceVehicleLocaleId, "Road maintenance vehicles"),
                    separator,
                    ref hasAnyName);
            }

            if ((categories & PublicTransportLaneVehicleCategory.Snowplow) != 0)
            {
                AppendRoleDescriptionPart(
                    description,
                    LocalizeText(kRoleSnowplowLocaleId, "Snowplows"),
                    separator,
                    ref hasAnyName);
            }

            if ((categories & PublicTransportLaneVehicleCategory.VehicleMaintenanceVehicle) != 0)
            {
                AppendRoleDescriptionPart(
                    description,
                    LocalizeText(kRoleVehicleMaintenanceVehicleLocaleId, "Vehicle maintenance vehicles"),
                    separator,
                    ref hasAnyName);
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

        private static bool ConsumeSummaryRequest()
        {
            bool requested = s_SummaryRequested;
            s_SummaryRequested = false;
            return requested;
        }

        private static bool ConsumeDebugFieldsRequest()
        {
            bool requested = s_DebugFieldsRequested;
            s_DebugFieldsRequested = false;
            return requested;
        }

        private static bool ConsumeLaneDetailsRequest()
        {
            bool requested = s_LaneDetailsRequested;
            s_LaneDetailsRequested = false;
            return requested;
        }

        private static bool ConsumeRouteDiagnosticsRequest()
        {
            bool requested = s_RouteDiagnosticsRequested;
            s_RouteDiagnosticsRequested = false;
            return requested;
        }

        private static void SetMinimalSnapshotConsumerActive(
            MinimalSnapshotConsumerSource source,
            bool active)
        {
            if (active)
            {
                s_MinimalSnapshotConsumers |= source;
                return;
            }

            s_MinimalSnapshotConsumers &= ~source;
        }

    }
}

