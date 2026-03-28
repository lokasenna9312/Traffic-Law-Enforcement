using Game.Pathfind;
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
        public readonly string CurrentLaneText;
        public readonly string PreviousLaneText;
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
        public readonly string RouteDiagnosticsTargetRoadText;
        public readonly string RouteDiagnosticsStartOwnerRoadText;
        public readonly string RouteDiagnosticsEndOwnerRoadText;
        public readonly string RouteDiagnosticsDirectConnectText;
        public readonly string RouteDiagnosticsFullPathToTargetStartText;
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
            string currentLaneText,
            string previousLaneText,
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
            string routeDiagnosticsTargetRoadText,
            string routeDiagnosticsStartOwnerRoadText,
            string routeDiagnosticsEndOwnerRoadText,
            string routeDiagnosticsDirectConnectText,
            string routeDiagnosticsFullPathToTargetStartText,
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
            CurrentLaneText = currentLaneText;
            PreviousLaneText = previousLaneText;
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
            RouteDiagnosticsTargetRoadText = routeDiagnosticsTargetRoadText;
            RouteDiagnosticsStartOwnerRoadText = routeDiagnosticsStartOwnerRoadText;
            RouteDiagnosticsEndOwnerRoadText = routeDiagnosticsEndOwnerRoadText;
            RouteDiagnosticsDirectConnectText = routeDiagnosticsDirectConnectText;
            RouteDiagnosticsFullPathToTargetStartText = routeDiagnosticsFullPathToTargetStartText;
            RouteDiagnosticsNavigationLanesText = routeDiagnosticsNavigationLanesText;
            RouteDiagnosticsPlannedPenaltiesText = routeDiagnosticsPlannedPenaltiesText;
            RouteDiagnosticsPenaltyTagsText = routeDiagnosticsPenaltyTagsText;
            RouteDiagnosticsExplanationText = routeDiagnosticsExplanationText;
            RouteDiagnosticsWaypointRouteLaneText = routeDiagnosticsWaypointRouteLaneText;
            RouteDiagnosticsConnectedStopText = routeDiagnosticsConnectedStopText;
        }
    }
}
