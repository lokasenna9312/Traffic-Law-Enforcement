using Colossal.UI.Binding;
using Game;
using Game.Input;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.SceneFlow;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public partial class SelectedObjectPanelUISystem : UISystemBase
    {
        private const string kGroup = "selectedObjectPanel";
        private const int kSummaryRefreshIntervalFrames = 2;
        private const int kLaneDetailsRefreshIntervalFrames = 3;
        private const int kRouteDiagnosticsRefreshIntervalFrames = 6;
        internal const string kHeaderTextLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.Header";
        internal const string kSummaryTitleLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.SummaryTitle";
        internal const string kClassificationLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.Classification";
        internal const string kTleStatusLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.TleStatus";
        internal const string kRoleLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.RoleOrType";
        internal const string kActiveFlagsLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.ActiveFlags";
        internal const string kViolationsFinesLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.ViolationsFines";
        internal const string kLastReasonLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.LastReason";
        internal const string kRepeatPenaltyLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.RepeatPenalty";
        internal const string kPublicTransportLanePolicyLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.PublicTransportLanePolicy";
        internal const string kFocusLogStatusLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.FocusLogStatus";
        internal const string kActiveFlagsValueFormatLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.ActiveFlagsFormat";
        internal const string kActiveFlagsViolationNameLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.ActiveFlagsViolationName";
        internal const string kActiveFlagsPendingNameLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.ActiveFlagsPendingName";
        internal const string kFlagOnLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.FlagOn";
        internal const string kFlagOffLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.FlagOff";
        internal const string kTotalsValueFormatLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.TotalsFormat";
        internal const string kNoSelectionLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.NoSelection";
        internal const string kNotVehicleLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.NotVehicle";
        internal const string kNotApplicableLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.NotApplicable";
        internal const string kNoLiveLaneLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.NoLiveLane";
        internal const string kTrackingLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.Tracking";
        internal const string kFooterHintLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.FooterHint";
        internal const string kExpandSectionLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.ExpandSection";
        internal const string kCollapseSectionLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.CollapseSection";
        internal const string kLaneDetailsTitleLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LaneDetailsTitle";
        internal const string kCurrentLaneLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.CurrentLane";
        internal const string kPreviousLaneLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.PreviousLane";
        internal const string kLaneChangesLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.LaneChanges";
        internal const string kLiveLaneStateLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.LiveLaneState";
        internal const string kRouteDiagnosticsTitleLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.RouteDiagnosticsTitle";
        internal const string kCurrentTargetLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.CurrentTarget";
        internal const string kCurrentRouteLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.CurrentRoute";
        internal const string kTargetRoadLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.TargetRoad";
        internal const string kRouteExplanationLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.RouteExplanation";
        internal const string kConnectedStopLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.ConnectedStop";
        internal const string kEntitySelectionLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.EntitySelection";
        internal const string kEntitySelectionPlaceholderLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.EntitySelectionPlaceholder";
        internal const string kEntitySelectionSubmitLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.EntitySelectionSubmit";
        internal const string kEntitySelectionStatusInvalidFormatLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.EntitySelectionStatusInvalidFormat";
        internal const string kEntitySelectionStatusEntityNotFoundFormatLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.EntitySelectionStatusEntityNotFoundFormat";
        internal const string kEntitySelectionStatusSelectedFormatLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.EntitySelectionStatusSelectedFormat";
        internal const string kEntitySelectionStatusUnavailableLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.EntitySelectionStatusUnavailable";
        internal const string kPathActionLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.PathAction";
        internal const string kPathObsoleteButtonLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.PathObsoleteButton";
        internal const string kPathObsoleteStatusUnavailableLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.PathObsoleteStatusUnavailable";
        internal const string kPathObsoleteStatusNoPathOwnerLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.PathObsoleteStatusNoPathOwner";
        internal const string kPathObsoleteStatusPendingLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.PathObsoleteStatusPending";
        internal const string kPathObsoleteStatusAlreadyObsoleteLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.PathObsoleteStatusAlreadyObsolete";
        internal const string kPathObsoleteStatusMarkedFormatLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.PathObsoleteStatusMarkedFormat";
        internal const string kLiveLaneStateReadyLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStateReady";
        internal const string kLiveLaneStateNoLiveLaneLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStateNoLiveLane";
        internal const string kLiveLaneStateNotApplicableLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStateNotApplicable";
        internal const string kLiveLaneStateParkedRoadCarLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStateParkedRoadCar";
        internal const string kLiveLaneStateNoPathOwnerLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStateNoPathOwner";
        internal const string kLiveLaneStateNoCurrentRouteLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStateNoCurrentRoute";
        internal const string kLiveLaneStateNoCurrentTargetLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStateNoCurrentTarget";
        internal const string kLiveLaneStatePathPendingLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStatePathPending";
        internal const string kLiveLaneStatePathScheduledLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStatePathScheduled";
        internal const string kLiveLaneStatePathObsoleteLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStatePathObsolete";
        internal const string kLiveLaneStatePathFailedLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStatePathFailed";
        internal const string kLiveLaneStatePathStuckLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStatePathStuck";
        internal const string kLiveLaneStatePathUpdatedLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStatePathUpdated";
        internal const string kNoneLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.None";

        private SelectedObjectBridgeSystem m_SelectedObjectBridgeSystem;
        private SelectedInfoUISystem m_SelectedInfoSystem;
        private ProxyAction m_PanelToggleAction;

        private ValueBinding<bool> m_VisibleBinding;
        private ValueBinding<bool> m_CompactBinding;
        private ValueBinding<bool> m_CollapsedBinding;
        private ValueBinding<string> m_ClassificationBinding;
        private ValueBinding<string> m_ClassificationLabelBinding;
        private ValueBinding<string> m_MessageBinding;
        private ValueBinding<string> m_TleStatusBinding;
        private ValueBinding<string> m_RoleBinding;
        private ValueBinding<string> m_PublicTransportLanePolicyBinding;
        private ValueBinding<string> m_FocusLogStatusBinding;
        private ValueBinding<string> m_VehicleIndexBinding;
        private ValueBinding<string> m_ViolationPendingBinding;
        private ValueBinding<string> m_TotalsBinding;
        private ValueBinding<string> m_LastReasonBinding;
        private ValueBinding<string> m_RepeatPenaltyBinding;
        private ValueBinding<string> m_EntitySelectionLabelBinding;
        private ValueBinding<string> m_EntitySelectionPlaceholderBinding;
        private ValueBinding<string> m_EntitySelectionSubmitBinding;
        private ValueBinding<string> m_EntitySelectionSuggestedValueBinding;
        private ValueBinding<string> m_EntitySelectionStatusBinding;
        private ValueBinding<bool> m_EntitySelectionStatusIsErrorBinding;
        private ValueBinding<string> m_PathActionLabelBinding;
        private ValueBinding<string> m_PathObsoleteButtonBinding;
        private ValueBinding<bool> m_PathObsoleteButtonVisibleBinding;
        private ValueBinding<bool> m_PathObsoleteButtonEnabledBinding;
        private ValueBinding<string> m_PathObsoleteStatusBinding;
        private ValueBinding<bool> m_PathObsoleteStatusIsErrorBinding;
        private ValueBinding<string> m_HeaderTextBinding;
        private ValueBinding<string> m_SummaryTitleBinding;
        private ValueBinding<string> m_TleStatusLabelBinding;
        private ValueBinding<string> m_RoleLabelBinding;
        private ValueBinding<string> m_ActiveFlagsLabelBinding;
        private ValueBinding<string> m_ViolationsFinesLabelBinding;
        private ValueBinding<string> m_LastReasonLabelBinding;
        private ValueBinding<string> m_RepeatPenaltyLabelBinding;
        private ValueBinding<string> m_PublicTransportLanePolicyLabelBinding;
        private ValueBinding<string> m_FocusLogStatusLabelBinding;
        private ValueBinding<string> m_FooterTextBinding;
        private ValueBinding<string> m_ExpandSectionTooltipBinding;
        private ValueBinding<string> m_CollapseSectionTooltipBinding;
        private ValueBinding<string> m_LaneDetailsTitleBinding;
        private ValueBinding<string> m_CurrentLaneLabelBinding;
        private ValueBinding<string> m_PreviousLaneLabelBinding;
        private ValueBinding<string> m_LaneChangesLabelBinding;
        private ValueBinding<string> m_LiveLaneStateLabelBinding;
        private ValueBinding<string> m_RouteDiagnosticsTitleBinding;
        private ValueBinding<string> m_CurrentTargetLabelBinding;
        private ValueBinding<string> m_CurrentRouteLabelBinding;
        private ValueBinding<string> m_TargetRoadLabelBinding;
        private ValueBinding<string> m_RouteExplanationLabelBinding;
        private ValueBinding<string> m_ConnectedStopLabelBinding;
        private ValueBinding<string> m_CurrentLaneBinding;
        private ValueBinding<string> m_PreviousLaneBinding;
        private ValueBinding<string> m_LaneChangesBinding;
        private ValueBinding<string> m_LiveLaneStateBinding;
        private ValueBinding<bool> m_LaneDetailsVisibleBinding;
        private ValueBinding<bool> m_LaneDetailsCollapsedBinding;
        private ValueBinding<bool> m_RouteDiagnosticsVisibleBinding;
        private ValueBinding<bool> m_RouteDiagnosticsCollapsedBinding;
        private ValueBinding<string> m_CurrentTargetBinding;
        private ValueBinding<string> m_CurrentTargetEntityBinding;
        private ValueBinding<string> m_CurrentRouteBinding;
        private ValueBinding<string> m_CurrentRouteEntityTextBinding;
        private ValueBinding<string> m_CurrentRouteColorBinding;
        private ValueBinding<string> m_TargetRoadBinding;
        private ValueBinding<string> m_RouteExplanationBinding;
        private ValueBinding<string> m_ConnectedStopBinding;

        private bool m_IsPanelEnabled;
        private bool m_IsCollapsed;
        private bool m_IsLaneDetailsCollapsed = true;
        private bool m_IsRouteDiagnosticsCollapsed = true;
        private int m_LastSeenRuntimeWorldGeneration = -1;
        private string m_LastLocalizedLocaleId = string.Empty;
        private bool m_LocalizedBindingsInitialized;
        private string m_EntitySelectionStatus = string.Empty;
        private bool m_EntitySelectionStatusIsError;
        private string m_EntitySelectionStatusSelectedEntity = string.Empty;
        private string m_PathObsoleteStatus = string.Empty;
        private bool m_PathObsoleteStatusIsError;
        private string m_PathObsoleteStatusSelectedEntity = string.Empty;
        private string m_NoneText = "None";
        private string m_NotApplicableText = "Not applicable";
        private string m_FocusLogWatchedText = "Watched";
        private string m_FocusLogNotWatchedText = "Not watched";
        private string m_NoLiveLaneText = "No live lane";
        private string m_TrackingText = "Tracking";
        private string m_NoSelectionText = "No selection";
        private string m_PathObsoleteNoPathOwnerText = "Vehicle has no path owner.";
        private string m_PathObsoletePendingText = "Path is already pending rebuild.";
        private string m_PathObsoleteAlreadyObsoleteText = "Path is already obsolete.";
        private string m_LiveLaneStateReadyText = "Ready";
        private string m_LiveLaneStateNoLiveLaneText = "No live lane";
        private string m_LiveLaneStateNotApplicableText = "Not applicable";
        private string m_LiveLaneStateParkedRoadCarText = "Parked road car";
        private string m_LiveLaneStateNoPathOwnerText = "Ready, no path owner";
        private string m_LiveLaneStateNoCurrentRouteText = "Ready, no current route";
        private string m_LiveLaneStateNoCurrentTargetText = "Ready, no current target";
        private string m_LiveLaneStatePathPendingText = "Ready, path pending";
        private string m_LiveLaneStatePathScheduledText = "Ready, path scheduled";
        private string m_LiveLaneStatePathObsoleteText = "Ready, path obsolete";
        private string m_LiveLaneStatePathFailedText = "Ready, path failed";
        private string m_LiveLaneStatePathStuckText = "Ready, path stuck";
        private string m_LiveLaneStatePathUpdatedText = "Ready, path updated";
        private string m_ActiveFlagsFormatText = "{0} {1}, {2} {3}";
        private string m_ActiveFlagsViolationNameText = "Violation";
        private string m_ActiveFlagsPendingNameText = "Pending";
        private string m_FlagOnText = "On";
        private string m_FlagOffText = "Off";
        private string m_TotalsFormatText = "Violations {0}, Fines {1}";
        private bool m_HasCachedPanelState;
        private int m_LastProcessedBridgeSnapshotSerial = -1;
        private SelectedObjectDebugSnapshot m_LastPanelSnapshot;
        private SelectedObjectDebugSnapshot m_LastPanelSummarySnapshot;
        private SelectedObjectDebugSnapshot m_LastPanelLaneDetailsSnapshot;
        private SelectedObjectDebugSnapshot m_LastPanelRouteDiagnosticsSnapshot;
        private PanelState m_LastPanelState;
        private string m_LastPanelSuggestedEntitySelectionValue = string.Empty;
        private string m_LastPanelEntitySelectionStatus = string.Empty;
        private bool m_LastPanelEntitySelectionStatusIsError;
        private string m_LastPanelPathObsoleteStatus = string.Empty;
        private bool m_LastPanelPathObsoleteStatusIsError;
        private bool m_LastPanelSummaryReady = true;
        private bool m_LastPanelLaneDetailsCollapsed = true;
        private bool m_LastPanelRouteDiagnosticsCollapsed = true;
        private bool m_LastPanelLaneDetailsReady = true;
        private bool m_LastPanelRouteDiagnosticsReady = true;
        private bool m_CollapsedFastPathApplied;
        private bool m_HasCachedSummarySnapshot;
        private SelectedObjectDebugSnapshot m_CachedSummarySnapshot;
        private int m_LastSummaryRequestFrame = int.MinValue;
        private Entity m_LastSummaryRequestedSourceEntity;
        private Entity m_LastSummaryRequestedVehicleEntity;
        private bool m_HasCachedLaneDetailsSnapshot;
        private SelectedObjectDebugSnapshot m_CachedLaneDetailsSnapshot;
        private int m_LastLaneDetailsRequestFrame = int.MinValue;
        private Entity m_LastLaneDetailsRequestedSourceEntity;
        private Entity m_LastLaneDetailsRequestedVehicleEntity;
        private bool m_HasCachedRouteDiagnosticsSnapshot;
        private SelectedObjectDebugSnapshot m_CachedRouteDiagnosticsSnapshot;
        private int m_LastRouteDiagnosticsRequestFrame = int.MinValue;
        private Entity m_LastRouteDiagnosticsRequestedSourceEntity;
        private Entity m_LastRouteDiagnosticsRequestedVehicleEntity;

        public override GameMode gameMode => GameMode.Game;

        private struct PanelState
        {
            public bool Visible;
            public bool Compact;
            public string Classification;
            public string Message;
            public string TleStatus;
            public string Role;
            public string PublicTransportLanePolicy;
            public string FocusLogStatus;
            public string VehicleIndex;
            public string ViolationPending;
            public string Totals;
            public string LastReason;
            public string RepeatPenalty;
            public string EntitySelectionSuggestedValue;
            public string EntitySelectionStatus;
            public bool EntitySelectionStatusIsError;
            public bool PathObsoleteButtonVisible;
            public bool PathObsoleteButtonEnabled;
            public string PathObsoleteStatus;
            public bool PathObsoleteStatusIsError;
            public bool LaneDetailsVisible;
            public string CurrentLane;
            public string PreviousLane;
            public string LaneChanges;
            public string LiveLaneState;
            public bool RouteDiagnosticsVisible;
            public string CurrentTarget;
            public string CurrentTargetEntity;
            public string CurrentRoute;
            public string CurrentRouteEntityText;
            public string CurrentRouteColor;
            public string TargetRoad;
            public string RouteExplanation;
            public string ConnectedStop;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SelectedObjectBridgeSystem =
                World.GetOrCreateSystemManaged<SelectedObjectBridgeSystem>();
            m_SelectedInfoSystem =
                World.GetExistingSystemManaged<SelectedInfoUISystem>();

            AddBinding(m_VisibleBinding = new ValueBinding<bool>(kGroup, "visible", false));
            AddBinding(m_CompactBinding = new ValueBinding<bool>(kGroup, "compact", false));
            AddBinding(m_CollapsedBinding = new ValueBinding<bool>(kGroup, "collapsed", false));
            AddBinding(m_ClassificationBinding = new ValueBinding<string>(kGroup, "classification", string.Empty));
            AddBinding(m_ClassificationLabelBinding = new ValueBinding<string>(kGroup, "classificationLabelText", string.Empty));
            AddBinding(m_MessageBinding = new ValueBinding<string>(kGroup, "message", string.Empty));
            AddBinding(m_TleStatusBinding = new ValueBinding<string>(kGroup, "tleStatus", string.Empty));
            AddBinding(m_RoleBinding = new ValueBinding<string>(kGroup, "role", string.Empty));
            AddBinding(m_PublicTransportLanePolicyBinding = new ValueBinding<string>(kGroup, "publicTransportLanePolicy", string.Empty));
            AddBinding(m_FocusLogStatusBinding = new ValueBinding<string>(kGroup, "focusLogStatus", string.Empty));
            AddBinding(m_VehicleIndexBinding = new ValueBinding<string>(kGroup, "vehicleIndex", string.Empty));
            AddBinding(m_ViolationPendingBinding = new ValueBinding<string>(kGroup, "violationPending", string.Empty));
            AddBinding(m_TotalsBinding = new ValueBinding<string>(kGroup, "totals", string.Empty));
            AddBinding(m_LastReasonBinding = new ValueBinding<string>(kGroup, "lastReason", string.Empty));
            AddBinding(m_RepeatPenaltyBinding = new ValueBinding<string>(kGroup, "repeatPenalty", string.Empty));
            AddBinding(m_EntitySelectionLabelBinding = new ValueBinding<string>(kGroup, "entitySelectionLabelText", string.Empty));
            AddBinding(m_EntitySelectionPlaceholderBinding = new ValueBinding<string>(kGroup, "entitySelectionPlaceholderText", string.Empty));
            AddBinding(m_EntitySelectionSubmitBinding = new ValueBinding<string>(kGroup, "entitySelectionSubmitText", string.Empty));
            AddBinding(m_EntitySelectionSuggestedValueBinding = new ValueBinding<string>(kGroup, "entitySelectionSuggestedValue", string.Empty));
            AddBinding(m_EntitySelectionStatusBinding = new ValueBinding<string>(kGroup, "entitySelectionStatus", string.Empty));
            AddBinding(m_EntitySelectionStatusIsErrorBinding = new ValueBinding<bool>(kGroup, "entitySelectionStatusIsError", false));
            AddBinding(m_PathActionLabelBinding = new ValueBinding<string>(kGroup, "pathActionLabelText", string.Empty));
            AddBinding(m_PathObsoleteButtonBinding = new ValueBinding<string>(kGroup, "pathObsoleteButtonText", string.Empty));
            AddBinding(m_PathObsoleteButtonVisibleBinding = new ValueBinding<bool>(kGroup, "pathObsoleteButtonVisible", false));
            AddBinding(m_PathObsoleteButtonEnabledBinding = new ValueBinding<bool>(kGroup, "pathObsoleteButtonEnabled", false));
            AddBinding(m_PathObsoleteStatusBinding = new ValueBinding<string>(kGroup, "pathObsoleteStatus", string.Empty));
            AddBinding(m_PathObsoleteStatusIsErrorBinding = new ValueBinding<bool>(kGroup, "pathObsoleteStatusIsError", false));
            AddBinding(m_HeaderTextBinding = new ValueBinding<string>(kGroup, "headerText", string.Empty));
            AddBinding(m_SummaryTitleBinding = new ValueBinding<string>(kGroup, "summaryTitle", string.Empty));
            AddBinding(m_TleStatusLabelBinding = new ValueBinding<string>(kGroup, "tleStatusLabelText", string.Empty));
            AddBinding(m_RoleLabelBinding = new ValueBinding<string>(kGroup, "roleLabelText", string.Empty));
            AddBinding(m_ActiveFlagsLabelBinding = new ValueBinding<string>(kGroup, "activeFlagsLabelText", string.Empty));
            AddBinding(m_ViolationsFinesLabelBinding = new ValueBinding<string>(kGroup, "violationsFinesLabelText", string.Empty));
            AddBinding(m_LastReasonLabelBinding = new ValueBinding<string>(kGroup, "lastReasonLabelText", string.Empty));
            AddBinding(m_RepeatPenaltyLabelBinding = new ValueBinding<string>(kGroup, "repeatPenaltyLabelText", string.Empty));
            AddBinding(m_PublicTransportLanePolicyLabelBinding = new ValueBinding<string>(kGroup, "publicTransportLanePolicyLabelText", string.Empty));
            AddBinding(m_FocusLogStatusLabelBinding = new ValueBinding<string>(kGroup, "focusLogStatusLabelText", string.Empty));
            AddBinding(m_FooterTextBinding = new ValueBinding<string>(kGroup, "footerText", string.Empty));
            AddBinding(m_ExpandSectionTooltipBinding = new ValueBinding<string>(kGroup, "expandSectionTooltipText", string.Empty));
            AddBinding(m_CollapseSectionTooltipBinding = new ValueBinding<string>(kGroup, "collapseSectionTooltipText", string.Empty));
            AddBinding(m_LaneDetailsTitleBinding = new ValueBinding<string>(kGroup, "laneDetailsTitleText", string.Empty));
            AddBinding(m_CurrentLaneLabelBinding = new ValueBinding<string>(kGroup, "currentLaneLabelText", string.Empty));
            AddBinding(m_PreviousLaneLabelBinding = new ValueBinding<string>(kGroup, "previousLaneLabelText", string.Empty));
            AddBinding(m_LaneChangesLabelBinding = new ValueBinding<string>(kGroup, "laneChangesLabelText", string.Empty));
            AddBinding(m_LiveLaneStateLabelBinding = new ValueBinding<string>(kGroup, "liveLaneStateLabelText", string.Empty));
            AddBinding(m_RouteDiagnosticsTitleBinding = new ValueBinding<string>(kGroup, "routeDiagnosticsTitleText", string.Empty));
            AddBinding(m_CurrentTargetLabelBinding = new ValueBinding<string>(kGroup, "currentTargetLabelText", string.Empty));
            AddBinding(m_CurrentRouteLabelBinding = new ValueBinding<string>(kGroup, "currentRouteLabelText", string.Empty));
            AddBinding(m_TargetRoadLabelBinding = new ValueBinding<string>(kGroup, "targetRoadLabelText", string.Empty));
            AddBinding(m_RouteExplanationLabelBinding = new ValueBinding<string>(kGroup, "routeExplanationLabelText", string.Empty));
            AddBinding(m_ConnectedStopLabelBinding = new ValueBinding<string>(kGroup, "connectedStopLabelText", string.Empty));
            AddBinding(m_CurrentLaneBinding = new ValueBinding<string>(kGroup, "currentLane", string.Empty));
            AddBinding(m_PreviousLaneBinding = new ValueBinding<string>(kGroup, "previousLane", string.Empty));
            AddBinding(m_LaneChangesBinding = new ValueBinding<string>(kGroup, "laneChanges", string.Empty));
            AddBinding(m_LiveLaneStateBinding = new ValueBinding<string>(kGroup, "liveLaneState", string.Empty));
            AddBinding(m_LaneDetailsVisibleBinding = new ValueBinding<bool>(kGroup, "laneDetailsVisible", false));
            AddBinding(m_LaneDetailsCollapsedBinding = new ValueBinding<bool>(kGroup, "laneDetailsCollapsed", true));
            AddBinding(m_RouteDiagnosticsVisibleBinding = new ValueBinding<bool>(kGroup, "routeDiagnosticsVisible", false));
            AddBinding(m_RouteDiagnosticsCollapsedBinding = new ValueBinding<bool>(kGroup, "routeDiagnosticsCollapsed", true));
            AddBinding(m_CurrentTargetBinding = new ValueBinding<string>(kGroup, "currentTarget", string.Empty));
            AddBinding(m_CurrentTargetEntityBinding = new ValueBinding<string>(kGroup, "currentTargetEntity", string.Empty));
            AddBinding(m_CurrentRouteBinding = new ValueBinding<string>(kGroup, "currentRoute", string.Empty));
            AddBinding(m_CurrentRouteEntityTextBinding = new ValueBinding<string>(kGroup, "currentRouteEntityText", string.Empty));
            AddBinding(m_CurrentRouteColorBinding = new ValueBinding<string>(kGroup, "currentRouteColor", string.Empty));
            AddBinding(m_TargetRoadBinding = new ValueBinding<string>(kGroup, "targetRoad", string.Empty));
            AddBinding(m_RouteExplanationBinding = new ValueBinding<string>(kGroup, "routeExplanation", string.Empty));
            AddBinding(m_ConnectedStopBinding = new ValueBinding<string>(kGroup, "connectedStop", string.Empty));

            AddBinding(new TriggerBinding(kGroup, "close", HandleCloseRequested));
            AddBinding(new TriggerBinding(kGroup, "toggleCollapsed", ToggleCollapsed));
            AddBinding(new TriggerBinding(kGroup, "toggleLaneDetailsCollapsed", ToggleLaneDetailsCollapsed));
            AddBinding(new TriggerBinding(kGroup, "toggleRouteDiagnosticsCollapsed", ToggleRouteDiagnosticsCollapsed));
            AddBinding(new TriggerBinding<string>(kGroup, "submitEntitySelection", HandleSubmitEntitySelection));
            AddBinding(new TriggerBinding(kGroup, "markSelectedVehiclePathObsolete", HandleMarkSelectedVehiclePathObsolete));
        }

        protected override void OnDestroy()
        {
            SelectedObjectBridgeSystem.SetSelectedObjectPanelMinimalSnapshotConsumerActive(false);
            SelectedObjectBridgeSystem.SetDetailedSnapshotConsumerActive(false);
            SelectedObjectBridgeSystem.SetLaneDetailsConsumerActive(false);
            SelectedObjectBridgeSystem.SetRouteDiagnosticsConsumerActive(false);
            if (m_PanelToggleAction != null)
            {
                m_PanelToggleAction.shouldBeEnabled = false;
                m_PanelToggleAction = null;
            }

            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            UpdatePanelToggle();
            UpdateSaveScopedUiState();
            SyncSnapshotConsumers();

            if (!m_IsPanelEnabled)
            {
                m_HasCachedPanelState = false;
                m_LastProcessedBridgeSnapshotSerial = -1;
                m_CollapsedFastPathApplied = false;
                ClearDeferredSnapshotCaches();
                m_VisibleBinding.Update(false);
                return;
            }

            UpdateLocalizedTextBindingsIfNeeded();

            if (m_IsCollapsed)
            {
                m_HasCachedPanelState = false;
                m_LastProcessedBridgeSnapshotSerial = -1;
                ApplyCollapsedBindings();
                return;
            }

            m_CollapsedFastPathApplied = false;

            if (m_SelectedObjectBridgeSystem == null)
            {
                m_SelectedObjectBridgeSystem =
                    World.GetExistingSystemManaged<SelectedObjectBridgeSystem>();
            }

            string currentSuggestedEntitySelectionValue = string.Empty;
            if (m_SelectedObjectBridgeSystem == null || !m_SelectedObjectBridgeSystem.HasSnapshot)
            {
                m_HasCachedPanelState = false;
                m_LastProcessedBridgeSnapshotSerial = -1;
                ClearDeferredSnapshotCaches();
                RefreshEntitySelectionStatus(currentSuggestedEntitySelectionValue);
                RefreshPathObsoleteStatus(currentSuggestedEntitySelectionValue);
                PanelState noSnapshotState = BuildNoSelectionState();
                UpdateBindings(noSnapshotState);
                return;
            }

            currentSuggestedEntitySelectionValue =
                BuildSuggestedEntitySelectionValue(m_SelectedObjectBridgeSystem.CurrentSnapshot);
            RefreshEntitySelectionStatus(currentSuggestedEntitySelectionValue);
            RefreshPathObsoleteStatus(currentSuggestedEntitySelectionValue);
            SelectedObjectDebugSnapshot snapshot = m_SelectedObjectBridgeSystem.CurrentSnapshot;
            int currentBridgeSnapshotSerial = m_SelectedObjectBridgeSystem.SnapshotSerial;
            if (m_HasCachedPanelState &&
                currentBridgeSnapshotSerial == m_LastProcessedBridgeSnapshotSerial)
            {
                return;
            }

            if (snapshot.ResolveState == SelectedObjectResolveState.None ||
                snapshot.ResolveState == SelectedObjectResolveState.NotVehicle)
            {
                ClearDeferredSnapshotCaches();
            }

            bool summaryReady = true;
            SelectedObjectDebugSnapshot summarySnapshot = snapshot;
            bool laneDetailsReady =
                m_IsLaneDetailsCollapsed ||
                m_SelectedObjectBridgeSystem.AreLaneDetailsHydrated;
            SelectedObjectDebugSnapshot laneDetailsSnapshot = snapshot;
            RefreshRouteDiagnosticsCache(snapshot);
            ScheduleRouteDiagnosticsRefresh(snapshot);
            bool routeDiagnosticsReady =
                TryGetRouteDiagnosticsDisplaySnapshot(
                    snapshot,
                    out SelectedObjectDebugSnapshot routeDiagnosticsSnapshot);
            if (TryReusePanelState(
                    snapshot,
                    summarySnapshot,
                    laneDetailsSnapshot,
                    routeDiagnosticsSnapshot,
                    currentSuggestedEntitySelectionValue,
                    summaryReady,
                    laneDetailsReady,
                    routeDiagnosticsReady,
                    out _))
            {
                return;
            }

            PanelState state = BuildState(
                snapshot,
                summarySnapshot,
                laneDetailsSnapshot,
                routeDiagnosticsSnapshot,
                currentSuggestedEntitySelectionValue,
                summaryReady,
                laneDetailsReady,
                routeDiagnosticsReady);
            CachePanelState(
                snapshot,
                summarySnapshot,
                laneDetailsSnapshot,
                routeDiagnosticsSnapshot,
                currentSuggestedEntitySelectionValue,
                summaryReady,
                laneDetailsReady,
                routeDiagnosticsReady,
                state);
            m_LastProcessedBridgeSnapshotSerial = currentBridgeSnapshotSerial;
            UpdateBindings(state);
        }

        private PanelState BuildState(
            SelectedObjectDebugSnapshot snapshot,
            SelectedObjectDebugSnapshot summarySnapshot,
            SelectedObjectDebugSnapshot laneDetailsSnapshot,
            SelectedObjectDebugSnapshot routeDiagnosticsSnapshot,
            string suggestedEntitySelectionValue,
            bool summaryReady,
            bool laneDetailsReady,
            bool routeDiagnosticsReady)
        {
            if (snapshot.ResolveState == SelectedObjectResolveState.None)
            {
                return BuildNoSelectionState();
            }

            if (snapshot.ResolveState == SelectedObjectResolveState.NotVehicle)
            {
                return new PanelState
                {
                    Visible = true,
                    Compact = false,
                    Message = LocalizeText(kNotVehicleLocaleId, "Not a vehicle"),
                    EntitySelectionSuggestedValue = suggestedEntitySelectionValue,
                    EntitySelectionStatus = m_EntitySelectionStatus,
                    EntitySelectionStatusIsError = m_EntitySelectionStatusIsError,
                    PathObsoleteButtonVisible = false,
                    PathObsoleteButtonEnabled = false,
                    PathObsoleteStatus = string.Empty,
                    PathObsoleteStatusIsError = false
                };
            }

            bool tleReady =
                snapshot.TleApplicability == SelectedObjectTleApplicability.ApplicableReady;
            bool summaryContentReady = summaryReady;
            bool laneDetailsExpanded = !m_IsLaneDetailsCollapsed;
            bool laneDetailsContentReady = laneDetailsExpanded && laneDetailsReady;
            bool routeDiagnosticsVisible = CanShowRouteDiagnosticsSection(snapshot);
            bool routeDiagnosticsExpanded =
                routeDiagnosticsVisible &&
                !m_IsRouteDiagnosticsCollapsed &&
                routeDiagnosticsReady;

            return new PanelState
            {
                Visible = true,
                Compact = false,
                Classification = summaryContentReady
                    ? summarySnapshot.SummaryClassificationText
                    : string.Empty,
                TleStatus = BuildCompactTleStatusText(snapshot),
                Role = NormalizeText(snapshot.RoleText),
                PublicTransportLanePolicy = summaryContentReady
                    ? NormalizeText(summarySnapshot.PublicTransportLanePolicyText)
                    : string.Empty,
                FocusLogStatus = BuildFocusLogStatusText(snapshot),
                VehicleIndex = BuildDisplayedVehicleEntityText(snapshot),
                ViolationPending = tleReady && summaryContentReady
                    ? BuildActiveFlagsText(summarySnapshot)
                    : string.Empty,
                Totals = tleReady && summaryContentReady
                    ? BuildTotalsText(summarySnapshot)
                    : string.Empty,
                LastReason = tleReady && summaryContentReady
                    ? NormalizeText(summarySnapshot.CompactLastReasonText)
                    : string.Empty,
                RepeatPenalty = tleReady && summaryContentReady
                    ? NormalizeText(summarySnapshot.CompactRepeatPenaltyText)
                    : string.Empty,
                EntitySelectionSuggestedValue = suggestedEntitySelectionValue,
                EntitySelectionStatus = m_EntitySelectionStatus,
                EntitySelectionStatusIsError = m_EntitySelectionStatusIsError,
                PathObsoleteButtonVisible = true,
                PathObsoleteButtonEnabled =
                    summaryContentReady &&
                    CanMarkPathObsolete(summarySnapshot),
                PathObsoleteStatus = summaryContentReady
                    ? BuildPathObsoleteStatusText(summarySnapshot)
                    : m_PathObsoleteStatus,
                PathObsoleteStatusIsError = m_PathObsoleteStatusIsError,
                LaneDetailsVisible = true,
                CurrentLane = laneDetailsContentReady
                    ? NormalizeText(laneDetailsSnapshot.CurrentLaneText)
                    : string.Empty,
                PreviousLane = laneDetailsContentReady
                    ? NormalizeText(laneDetailsSnapshot.PreviousLaneText)
                    : string.Empty,
                LaneChanges = laneDetailsContentReady
                    ? laneDetailsSnapshot.LaneChangeCount.ToString()
                    : string.Empty,
                LiveLaneState = laneDetailsContentReady && summaryContentReady
                    ? BuildLiveLaneStateText(summarySnapshot)
                    : string.Empty,
                RouteDiagnosticsVisible = routeDiagnosticsVisible,
                CurrentTarget = routeDiagnosticsExpanded
                    ? BuildCurrentTargetDisplayText(routeDiagnosticsSnapshot)
                    : string.Empty,
                CurrentTargetEntity = routeDiagnosticsExpanded
                    ? BuildCurrentTargetEntityText(routeDiagnosticsSnapshot)
                    : string.Empty,
                CurrentRoute = routeDiagnosticsExpanded
                    ? NormalizeText(routeDiagnosticsSnapshot.RouteDiagnosticsCurrentRouteText)
                    : string.Empty,
                CurrentRouteEntityText = routeDiagnosticsExpanded
                    ? BuildCurrentRouteEntityText(routeDiagnosticsSnapshot)
                    : string.Empty,
                CurrentRouteColor = routeDiagnosticsExpanded
                    ? BuildCurrentRouteColorText(routeDiagnosticsSnapshot)
                    : string.Empty,
                TargetRoad = routeDiagnosticsExpanded
                    ? NormalizeText(routeDiagnosticsSnapshot.RouteDiagnosticsTargetRoadText)
                    : string.Empty,
                RouteExplanation = routeDiagnosticsExpanded
                    ? NormalizeText(routeDiagnosticsSnapshot.RouteDiagnosticsExplanationText)
                    : string.Empty,
                ConnectedStop = routeDiagnosticsExpanded
                    ? NormalizeText(routeDiagnosticsSnapshot.RouteDiagnosticsConnectedStopText)
                    : string.Empty
            };
        }

        private void UpdateBindings(PanelState state)
        {
            m_VisibleBinding.Update(state.Visible);
            m_CompactBinding.Update(state.Compact);
            m_CollapsedBinding.Update(state.Visible && !state.Compact && m_IsCollapsed);
            m_ClassificationBinding.Update(state.Classification ?? string.Empty);
            m_MessageBinding.Update(state.Message ?? string.Empty);
            m_TleStatusBinding.Update(state.TleStatus ?? string.Empty);
            m_RoleBinding.Update(state.Role ?? string.Empty);
            m_PublicTransportLanePolicyBinding.Update(state.PublicTransportLanePolicy ?? string.Empty);
            m_FocusLogStatusBinding.Update(state.FocusLogStatus ?? string.Empty);
            m_VehicleIndexBinding.Update(state.VehicleIndex ?? string.Empty);
            m_ViolationPendingBinding.Update(state.ViolationPending ?? string.Empty);
            m_TotalsBinding.Update(state.Totals ?? string.Empty);
            m_LastReasonBinding.Update(state.LastReason ?? string.Empty);
            m_RepeatPenaltyBinding.Update(state.RepeatPenalty ?? string.Empty);
            m_EntitySelectionSuggestedValueBinding.Update(state.EntitySelectionSuggestedValue ?? string.Empty);
            m_EntitySelectionStatusBinding.Update(state.EntitySelectionStatus ?? string.Empty);
            m_EntitySelectionStatusIsErrorBinding.Update(state.EntitySelectionStatusIsError);
            m_PathObsoleteButtonVisibleBinding.Update(state.PathObsoleteButtonVisible);
            m_PathObsoleteButtonEnabledBinding.Update(state.PathObsoleteButtonEnabled);
            m_PathObsoleteStatusBinding.Update(state.PathObsoleteStatus ?? string.Empty);
            m_PathObsoleteStatusIsErrorBinding.Update(state.PathObsoleteStatusIsError);
            m_CurrentLaneBinding.Update(state.CurrentLane ?? string.Empty);
            m_PreviousLaneBinding.Update(state.PreviousLane ?? string.Empty);
            m_LaneChangesBinding.Update(state.LaneChanges ?? string.Empty);
            m_LiveLaneStateBinding.Update(state.LiveLaneState ?? string.Empty);
            m_LaneDetailsVisibleBinding.Update(state.LaneDetailsVisible);
            m_LaneDetailsCollapsedBinding.Update(m_IsLaneDetailsCollapsed);
            m_RouteDiagnosticsVisibleBinding.Update(state.RouteDiagnosticsVisible);
            m_RouteDiagnosticsCollapsedBinding.Update(m_IsRouteDiagnosticsCollapsed);
            m_CurrentTargetBinding.Update(state.CurrentTarget ?? string.Empty);
            m_CurrentTargetEntityBinding.Update(state.CurrentTargetEntity ?? string.Empty);
            m_CurrentRouteBinding.Update(state.CurrentRoute ?? string.Empty);
            m_CurrentRouteEntityTextBinding.Update(state.CurrentRouteEntityText ?? string.Empty);
            m_CurrentRouteColorBinding.Update(state.CurrentRouteColor ?? string.Empty);
            m_TargetRoadBinding.Update(state.TargetRoad ?? string.Empty);
            m_RouteExplanationBinding.Update(state.RouteExplanation ?? string.Empty);
            m_ConnectedStopBinding.Update(state.ConnectedStop ?? string.Empty);
        }

        private void UpdateLocalizedTextBindings()
        {
            m_HasCachedPanelState = false;
            ClearDeferredSnapshotCaches();
            m_HeaderTextBinding.Update(LocalizeText(kHeaderTextLocaleId, "Selected Object"));
            m_SummaryTitleBinding.Update(LocalizeText(kSummaryTitleLocaleId, "Summary"));
            m_ClassificationLabelBinding.Update(LocalizeText(kClassificationLabelLocaleId, "Classification"));
            m_TleStatusLabelBinding.Update(LocalizeText(kTleStatusLabelLocaleId, "TLE status"));
            m_RoleLabelBinding.Update(LocalizeText(kRoleLabelLocaleId, "Role / type"));
            m_ActiveFlagsLabelBinding.Update(LocalizeText(kActiveFlagsLabelLocaleId, "Active flags"));
            m_ViolationsFinesLabelBinding.Update(LocalizeText(kViolationsFinesLabelLocaleId, "Violations / fines"));
            m_LastReasonLabelBinding.Update(LocalizeText(kLastReasonLabelLocaleId, "Last reason"));
            m_RepeatPenaltyLabelBinding.Update(LocalizeText(kRepeatPenaltyLabelLocaleId, "Repeat penalty"));
            m_PublicTransportLanePolicyLabelBinding.Update(LocalizeText(kPublicTransportLanePolicyLabelLocaleId, "PT lane policy"));
            m_FocusLogStatusLabelBinding.Update(LocalizeText(kFocusLogStatusLabelLocaleId, "Focus Log"));
            m_EntitySelectionLabelBinding.Update(LocalizeText(kEntitySelectionLabelLocaleId, "Entity number"));
            m_EntitySelectionPlaceholderBinding.Update(LocalizeText(kEntitySelectionPlaceholderLocaleId, "#154656:v1 or entity://154656/1"));
            m_EntitySelectionSubmitBinding.Update(LocalizeText(kEntitySelectionSubmitLocaleId, "Select"));
            m_PathActionLabelBinding.Update(LocalizeText(kPathActionLabelLocaleId, "Path action"));
            m_PathObsoleteButtonBinding.Update(LocalizeText(kPathObsoleteButtonLocaleId, "Mark path obsolete"));
            m_FooterTextBinding.Update(LocalizeText(kFooterHintLocaleId, "If Developer Mode is enabled, press Tab for more details."));
            m_ExpandSectionTooltipBinding.Update(LocalizeText(kExpandSectionLocaleId, "Expand section"));
            m_CollapseSectionTooltipBinding.Update(LocalizeText(kCollapseSectionLocaleId, "Collapse section"));
            m_LaneDetailsTitleBinding.Update(LocalizeText(kLaneDetailsTitleLocaleId, "Lane details"));
            m_CurrentLaneLabelBinding.Update(LocalizeText(kCurrentLaneLabelLocaleId, "Current lane"));
            m_PreviousLaneLabelBinding.Update(LocalizeText(kPreviousLaneLabelLocaleId, "Previous lane"));
            m_LaneChangesLabelBinding.Update(LocalizeText(kLaneChangesLabelLocaleId, "Lane changes"));
            m_LiveLaneStateLabelBinding.Update(LocalizeText(kLiveLaneStateLabelLocaleId, "Live routing state"));
            m_RouteDiagnosticsTitleBinding.Update(LocalizeText(kRouteDiagnosticsTitleLocaleId, "Route diagnostics"));
            m_CurrentTargetLabelBinding.Update(LocalizeText(kCurrentTargetLabelLocaleId, "Current target"));
            m_CurrentRouteLabelBinding.Update(LocalizeText(kCurrentRouteLabelLocaleId, "Current route"));
            m_TargetRoadLabelBinding.Update(LocalizeText(kTargetRoadLabelLocaleId, "Target road"));
            m_RouteExplanationLabelBinding.Update(LocalizeText(kRouteExplanationLabelLocaleId, "Route context"));
            m_ConnectedStopLabelBinding.Update(LocalizeText(kConnectedStopLabelLocaleId, "Connected stop"));
            m_NoneText = LocalizeText(kNoneLocaleId, "None");
            m_NotApplicableText = LocalizeText(kNotApplicableLocaleId, "Not applicable");
            m_FocusLogWatchedText = LocalizeText(FocusedLoggingPanelUISystem.kWatchedStatusLocaleId, "Watched");
            m_FocusLogNotWatchedText = LocalizeText(FocusedLoggingPanelUISystem.kNotWatchedStatusLocaleId, "Not watched");
            m_NoLiveLaneText = LocalizeText(kNoLiveLaneLocaleId, "No live lane");
            m_TrackingText = LocalizeText(kTrackingLocaleId, "Tracking");
            m_NoSelectionText = LocalizeText(kNoSelectionLocaleId, "No selection");
            m_PathObsoleteNoPathOwnerText = LocalizeText(
                kPathObsoleteStatusNoPathOwnerLocaleId,
                "Vehicle has no path owner.");
            m_PathObsoletePendingText = LocalizeText(
                kPathObsoleteStatusPendingLocaleId,
                "Path is already pending rebuild.");
            m_PathObsoleteAlreadyObsoleteText = LocalizeText(
                kPathObsoleteStatusAlreadyObsoleteLocaleId,
                "Path is already obsolete.");
            m_LiveLaneStateReadyText = LocalizeText(kLiveLaneStateReadyLocaleId, "Ready");
            m_LiveLaneStateNoLiveLaneText = LocalizeText(kLiveLaneStateNoLiveLaneLocaleId, "No live lane");
            m_LiveLaneStateNotApplicableText = LocalizeText(kLiveLaneStateNotApplicableLocaleId, "Not applicable");
            m_LiveLaneStateParkedRoadCarText = LocalizeText(
                kLiveLaneStateParkedRoadCarLocaleId,
                "Parked road car");
            m_LiveLaneStateNoPathOwnerText = LocalizeText(
                kLiveLaneStateNoPathOwnerLocaleId,
                "Ready, no path owner");
            m_LiveLaneStateNoCurrentRouteText = LocalizeText(
                kLiveLaneStateNoCurrentRouteLocaleId,
                "Ready, no current route");
            m_LiveLaneStateNoCurrentTargetText = LocalizeText(
                kLiveLaneStateNoCurrentTargetLocaleId,
                "Ready, no current target");
            m_LiveLaneStatePathPendingText = LocalizeText(
                kLiveLaneStatePathPendingLocaleId,
                "Ready, path pending");
            m_LiveLaneStatePathScheduledText = LocalizeText(
                kLiveLaneStatePathScheduledLocaleId,
                "Ready, path scheduled");
            m_LiveLaneStatePathObsoleteText = LocalizeText(
                kLiveLaneStatePathObsoleteLocaleId,
                "Ready, path obsolete");
            m_LiveLaneStatePathFailedText = LocalizeText(
                kLiveLaneStatePathFailedLocaleId,
                "Ready, path failed");
            m_LiveLaneStatePathStuckText = LocalizeText(
                kLiveLaneStatePathStuckLocaleId,
                "Ready, path stuck");
            m_LiveLaneStatePathUpdatedText = LocalizeText(
                kLiveLaneStatePathUpdatedLocaleId,
                "Ready, path updated");
            m_ActiveFlagsFormatText = LocalizeText(
                kActiveFlagsValueFormatLocaleId,
                "{0} {1}, {2} {3}");
            m_ActiveFlagsViolationNameText = LocalizeText(
                kActiveFlagsViolationNameLocaleId,
                "Violation");
            m_ActiveFlagsPendingNameText = LocalizeText(
                kActiveFlagsPendingNameLocaleId,
                "Pending");
            m_FlagOnText = LocalizeText(kFlagOnLocaleId, "On");
            m_FlagOffText = LocalizeText(kFlagOffLocaleId, "Off");
            m_TotalsFormatText = LocalizeText(
                kTotalsValueFormatLocaleId,
                "Violations {0}, Fines {1}");
        }

        private bool TryReusePanelState(
            SelectedObjectDebugSnapshot snapshot,
            SelectedObjectDebugSnapshot summarySnapshot,
            SelectedObjectDebugSnapshot laneDetailsSnapshot,
            SelectedObjectDebugSnapshot routeDiagnosticsSnapshot,
            string suggestedEntitySelectionValue,
            bool summaryReady,
            bool laneDetailsReady,
            bool routeDiagnosticsReady,
            out PanelState state)
        {
            if (!m_HasCachedPanelState ||
                !CanReusePanelState(
                    snapshot,
                    summarySnapshot,
                    laneDetailsSnapshot,
                    routeDiagnosticsSnapshot,
                    suggestedEntitySelectionValue,
                    summaryReady,
                    laneDetailsReady,
                    routeDiagnosticsReady))
            {
                state = default;
                return false;
            }

            state = m_LastPanelState;
            return true;
        }

        private bool CanReusePanelState(
            SelectedObjectDebugSnapshot snapshot,
            SelectedObjectDebugSnapshot summarySnapshot,
            SelectedObjectDebugSnapshot laneDetailsSnapshot,
            SelectedObjectDebugSnapshot routeDiagnosticsSnapshot,
            string suggestedEntitySelectionValue,
            bool summaryReady,
            bool laneDetailsReady,
            bool routeDiagnosticsReady)
        {
            bool summaryStateMatches =
                snapshot.SummaryClassificationText == m_LastPanelSnapshot.SummaryClassificationText &&
                snapshot.PublicTransportLanePolicyText == m_LastPanelSnapshot.PublicTransportLanePolicyText &&
                snapshot.PublicTransportLaneViolationActive == m_LastPanelSnapshot.PublicTransportLaneViolationActive &&
                snapshot.PendingExitActive == m_LastPanelSnapshot.PendingExitActive &&
                snapshot.TotalFines == m_LastPanelSnapshot.TotalFines &&
                snapshot.TotalViolations == m_LastPanelSnapshot.TotalViolations &&
                snapshot.CompactLastReasonText == m_LastPanelSnapshot.CompactLastReasonText &&
                snapshot.CompactRepeatPenaltyText == m_LastPanelSnapshot.CompactRepeatPenaltyText &&
                snapshot.HasPathOwner == m_LastPanelSnapshot.HasPathOwner &&
                snapshot.CurrentPathFlags == m_LastPanelSnapshot.CurrentPathFlags;
            bool laneDetailsStateMatches =
                m_IsLaneDetailsCollapsed ||
                (!laneDetailsReady && !m_LastPanelLaneDetailsReady) ||
                (laneDetailsReady == m_LastPanelLaneDetailsReady &&
                 snapshot.CurrentLaneText == m_LastPanelSnapshot.CurrentLaneText &&
                 snapshot.PreviousLaneText == m_LastPanelSnapshot.PreviousLaneText &&
                 snapshot.LaneChangeCount == m_LastPanelSnapshot.LaneChangeCount &&
                 snapshot.HasCurrentTarget == m_LastPanelSnapshot.HasCurrentTarget &&
                 snapshot.HasCurrentRoute == m_LastPanelSnapshot.HasCurrentRoute &&
                 snapshot.CurrentPathFlags == m_LastPanelSnapshot.CurrentPathFlags);
            bool routeDiagnosticsVisible = CanShowRouteDiagnosticsSection(snapshot);
            bool routeDiagnosticsRelevant =
                routeDiagnosticsVisible &&
                !m_IsRouteDiagnosticsCollapsed;
            bool routeDiagnosticsStateMatches =
                !routeDiagnosticsRelevant ||
                (!routeDiagnosticsReady && !m_LastPanelRouteDiagnosticsReady) ||
                (routeDiagnosticsReady == m_LastPanelRouteDiagnosticsReady &&
                 routeDiagnosticsSnapshot.CurrentTargetEntity == m_LastPanelRouteDiagnosticsSnapshot.CurrentTargetEntity &&
                 routeDiagnosticsSnapshot.CurrentRouteEntity == m_LastPanelRouteDiagnosticsSnapshot.CurrentRouteEntity &&
                 routeDiagnosticsSnapshot.CurrentRouteColorText == m_LastPanelRouteDiagnosticsSnapshot.CurrentRouteColorText &&
                 routeDiagnosticsSnapshot.RouteDiagnosticsCurrentTargetText == m_LastPanelRouteDiagnosticsSnapshot.RouteDiagnosticsCurrentTargetText &&
                 routeDiagnosticsSnapshot.RouteDiagnosticsCurrentRouteText == m_LastPanelRouteDiagnosticsSnapshot.RouteDiagnosticsCurrentRouteText &&
                 routeDiagnosticsSnapshot.RouteDiagnosticsTargetRoadText == m_LastPanelRouteDiagnosticsSnapshot.RouteDiagnosticsTargetRoadText &&
                 routeDiagnosticsSnapshot.RouteDiagnosticsExplanationText == m_LastPanelRouteDiagnosticsSnapshot.RouteDiagnosticsExplanationText &&
                 routeDiagnosticsSnapshot.RouteDiagnosticsConnectedStopText == m_LastPanelRouteDiagnosticsSnapshot.RouteDiagnosticsConnectedStopText);

            return
                snapshot.ResolveState == m_LastPanelSnapshot.ResolveState &&
                snapshot.TleApplicability == m_LastPanelSnapshot.TleApplicability &&
                snapshot.SourceSelectedEntity == m_LastPanelSnapshot.SourceSelectedEntity &&
                snapshot.ResolvedVehicleEntity == m_LastPanelSnapshot.ResolvedVehicleEntity &&
                snapshot.VehicleKind == m_LastPanelSnapshot.VehicleKind &&
                snapshot.RoleText == m_LastPanelSnapshot.RoleText &&
                BuildFocusLogStatusText(snapshot) == (m_LastPanelState.FocusLogStatus ?? string.Empty) &&
                suggestedEntitySelectionValue == m_LastPanelSuggestedEntitySelectionValue &&
                m_EntitySelectionStatus == m_LastPanelEntitySelectionStatus &&
                m_EntitySelectionStatusIsError == m_LastPanelEntitySelectionStatusIsError &&
                m_PathObsoleteStatus == m_LastPanelPathObsoleteStatus &&
                m_PathObsoleteStatusIsError == m_LastPanelPathObsoleteStatusIsError &&
                m_IsLaneDetailsCollapsed == m_LastPanelLaneDetailsCollapsed &&
                m_IsRouteDiagnosticsCollapsed == m_LastPanelRouteDiagnosticsCollapsed &&
                routeDiagnosticsVisible == m_LastPanelState.RouteDiagnosticsVisible &&
                summaryStateMatches &&
                laneDetailsStateMatches &&
                routeDiagnosticsStateMatches;
        }

        private void CachePanelState(
            SelectedObjectDebugSnapshot snapshot,
            SelectedObjectDebugSnapshot summarySnapshot,
            SelectedObjectDebugSnapshot laneDetailsSnapshot,
            SelectedObjectDebugSnapshot routeDiagnosticsSnapshot,
            string suggestedEntitySelectionValue,
            bool summaryReady,
            bool laneDetailsReady,
            bool routeDiagnosticsReady,
            PanelState state)
        {
            m_HasCachedPanelState = true;
            m_LastPanelSnapshot = snapshot;
            m_LastPanelSummarySnapshot = summarySnapshot;
            m_LastPanelLaneDetailsSnapshot = laneDetailsSnapshot;
            m_LastPanelRouteDiagnosticsSnapshot = routeDiagnosticsSnapshot;
            m_LastPanelState = state;
            m_LastPanelSuggestedEntitySelectionValue = suggestedEntitySelectionValue ?? string.Empty;
            m_LastPanelEntitySelectionStatus = m_EntitySelectionStatus ?? string.Empty;
            m_LastPanelEntitySelectionStatusIsError = m_EntitySelectionStatusIsError;
            m_LastPanelPathObsoleteStatus = m_PathObsoleteStatus ?? string.Empty;
            m_LastPanelPathObsoleteStatusIsError = m_PathObsoleteStatusIsError;
            m_LastPanelSummaryReady = summaryReady;
            m_LastPanelLaneDetailsCollapsed = m_IsLaneDetailsCollapsed;
            m_LastPanelRouteDiagnosticsCollapsed = m_IsRouteDiagnosticsCollapsed;
            m_LastPanelLaneDetailsReady = laneDetailsReady;
            m_LastPanelRouteDiagnosticsReady = routeDiagnosticsReady;
        }

        private void UpdateLocalizedTextBindingsIfNeeded()
        {
            string activeLocaleId = GetActiveLocaleId();
            if (m_LocalizedBindingsInitialized && activeLocaleId == m_LastLocalizedLocaleId)
            {
                return;
            }

            UpdateLocalizedTextBindings();
            m_LastLocalizedLocaleId = activeLocaleId;
            m_LocalizedBindingsInitialized = true;
        }

        private void UpdatePanelToggle()
        {
            ProxyAction toggleAction = GetPanelToggleAction();
            if (toggleAction != null && toggleAction.WasPressedThisFrame())
            {
                m_IsPanelEnabled = !m_IsPanelEnabled;
            }
        }

        private ProxyAction GetPanelToggleAction()
        {
            if (m_PanelToggleAction == null && Mod.Settings != null)
            {
                m_PanelToggleAction =
                    Mod.Settings.GetAction(KeybindingIds.SelectedObjectPanelToggleActionName);

                if (m_PanelToggleAction != null)
                {
                    m_PanelToggleAction.shouldBeEnabled = true;
                }
            }

            return m_PanelToggleAction;
        }

        private void HandleCloseRequested()
        {
            m_IsPanelEnabled = false;
            m_CollapsedFastPathApplied = false;
            m_LastProcessedBridgeSnapshotSerial = -1;
            SelectedObjectBridgeSystem.SetSelectedObjectPanelMinimalSnapshotConsumerActive(false);
            SelectedObjectBridgeSystem.SetDetailedSnapshotConsumerActive(false);
            SelectedObjectBridgeSystem.SetLaneDetailsConsumerActive(false);
            SelectedObjectBridgeSystem.SetRouteDiagnosticsConsumerActive(false);
            ClearEntitySelectionStatus();
            ClearPathObsoleteStatus();
            ClearDeferredSnapshotCaches();
        }

        private void ToggleCollapsed()
        {
            if (!m_IsPanelEnabled)
            {
                return;
            }

            m_IsCollapsed = !m_IsCollapsed;
            m_HasCachedPanelState = false;
            if (m_IsCollapsed)
            {
                m_LastProcessedBridgeSnapshotSerial = -1;
                ApplyCollapsedBindings();
            }
            else
            {
                m_CollapsedFastPathApplied = false;
                m_LastProcessedBridgeSnapshotSerial = -1;
                m_LastSummaryRequestFrame = int.MinValue;
                m_LastLaneDetailsRequestFrame = int.MinValue;
                m_LastRouteDiagnosticsRequestFrame = int.MinValue;
                m_CollapsedBinding.Update(false);
            }
        }

        private void ToggleLaneDetailsCollapsed()
        {
            m_IsLaneDetailsCollapsed = !m_IsLaneDetailsCollapsed;
            m_HasCachedPanelState = false;
            m_LastProcessedBridgeSnapshotSerial = -1;
            m_LastLaneDetailsRequestFrame = int.MinValue;
            m_LaneDetailsCollapsedBinding.Update(m_IsLaneDetailsCollapsed);
        }

        private void ToggleRouteDiagnosticsCollapsed()
        {
            m_IsRouteDiagnosticsCollapsed = !m_IsRouteDiagnosticsCollapsed;
            m_HasCachedPanelState = false;
            m_LastProcessedBridgeSnapshotSerial = -1;
            m_LastRouteDiagnosticsRequestFrame = int.MinValue;
            m_RouteDiagnosticsCollapsedBinding.Update(m_IsRouteDiagnosticsCollapsed);
        }

        private void HandleSubmitEntitySelection(string input)
        {
            if (!TryParseEntitySelectionInput(input, out Entity entity))
            {
                SetEntitySelectionStatus(
                    LocalizeText(
                        kEntitySelectionStatusInvalidFormatLocaleId,
                        "Enter #index:vversion or entity://index/version."),
                    isError: true);
                return;
            }

            if (!EntityManager.Exists(entity))
            {
                SetEntitySelectionStatus(
                    string.Format(
                        LocalizeText(
                            kEntitySelectionStatusEntityNotFoundFormatLocaleId,
                            "Entity not found: {0}"),
                        FormatEntity(entity)),
                    isError: true);
                return;
            }

            if (m_SelectedInfoSystem == null)
            {
                m_SelectedInfoSystem =
                    World.GetExistingSystemManaged<SelectedInfoUISystem>();
            }

            if (m_SelectedInfoSystem == null)
            {
                SetEntitySelectionStatus(
                    LocalizeText(
                        kEntitySelectionStatusUnavailableLocaleId,
                        "Selection system unavailable."),
                    isError: true);
                return;
            }

            m_SelectedInfoSystem.SetSelection(entity);
            SetEntitySelectionSuccessStatus(entity);
        }

        private void HandleMarkSelectedVehiclePathObsolete()
        {
            if (m_SelectedObjectBridgeSystem == null || !m_SelectedObjectBridgeSystem.HasSnapshot)
            {
                SetPathObsoleteStatus(
                    LocalizeText(
                        kPathObsoleteStatusUnavailableLocaleId,
                        "Selected vehicle unavailable."),
                    isError: true,
                    trackedEntityText: string.Empty);
                return;
            }

            SelectedObjectDebugSnapshot snapshot = m_SelectedObjectBridgeSystem.CurrentSnapshot;
            Entity vehicle = snapshot.ResolvedVehicleEntity;
            string trackedEntityText = BuildSuggestedEntitySelectionValue(snapshot);

            if (vehicle == Entity.Null || !EntityManager.Exists(vehicle))
            {
                SetPathObsoleteStatus(
                    LocalizeText(
                        kPathObsoleteStatusUnavailableLocaleId,
                        "Selected vehicle unavailable."),
                    isError: true,
                    trackedEntityText);
                return;
            }

            if (!EntityManager.HasComponent<PathOwner>(vehicle))
            {
                SetPathObsoleteStatus(
                    LocalizeText(
                        kPathObsoleteStatusNoPathOwnerLocaleId,
                        "Vehicle has no path owner."),
                    isError: false,
                    trackedEntityText);
                return;
            }

            PathOwner pathOwner = EntityManager.GetComponentData<PathOwner>(vehicle);
            if ((pathOwner.m_State & PathFlags.Pending) != 0)
            {
                SetPathObsoleteStatus(
                    LocalizeText(
                        kPathObsoleteStatusPendingLocaleId,
                        "Path is already pending rebuild."),
                    isError: false,
                    trackedEntityText);
                return;
            }

            if ((pathOwner.m_State & PathFlags.Obsolete) != 0)
            {
                SetPathObsoleteStatus(
                    LocalizeText(
                        kPathObsoleteStatusAlreadyObsoleteLocaleId,
                        "Path is already obsolete."),
                    isError: false,
                    trackedEntityText);
                return;
            }

            PathFlags stateBefore = pathOwner.m_State;
            pathOwner.m_State |= PathFlags.Obsolete;
            EntityManager.SetComponentData(vehicle, pathOwner);

            Car car = EntityManager.HasComponent<Car>(vehicle)
                ? EntityManager.GetComponentData<Car>(vehicle)
                : default;

            PathObsoleteTraceLogging.Record(
                "SELECTED_OBJECT_PANEL",
                vehicle,
                snapshot.CurrentLaneEntity,
                stateBefore,
                pathOwner.m_State,
                "manual-panel-action",
                car,
                NormalizeText(snapshot.RoleText),
                $"selectionEntity={FormatEntity(snapshot.SourceSelectedEntity)}",
                force: true);

            SetPathObsoleteStatus(
                string.Format(
                    LocalizeText(
                        kPathObsoleteStatusMarkedFormatLocaleId,
                        "Marked {0} path obsolete."),
                    FormatEntity(vehicle)),
                isError: false,
                trackedEntityText);
        }

        private static string NormalizeText(string text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : text.Trim();
        }

        private static string GetActiveLocaleId()
        {
            return GameManager.instance?.localizationManager?.activeLocaleId ?? string.Empty;
        }

        private void SyncSnapshotConsumers()
        {
            bool panelBodyVisible = m_IsPanelEnabled && !m_IsCollapsed;
            SelectedObjectBridgeSystem.SetSelectedObjectPanelMinimalSnapshotConsumerActive(false);
            SelectedObjectBridgeSystem.SetDetailedSnapshotConsumerActive(panelBodyVisible);
            SelectedObjectBridgeSystem.SetLaneDetailsConsumerActive(
                panelBodyVisible && !m_IsLaneDetailsCollapsed);
            SelectedObjectBridgeSystem.SetRouteDiagnosticsConsumerActive(false);
        }

        private void RefreshSummaryCache(SelectedObjectDebugSnapshot snapshot)
        {
            if (HasSummaryDisplayData(snapshot))
            {
                m_CachedSummarySnapshot = snapshot;
                m_HasCachedSummarySnapshot = true;
                return;
            }

            if (m_HasCachedSummarySnapshot &&
                !IsSameResolvedSelection(snapshot, m_CachedSummarySnapshot))
            {
                ClearSummaryCache();
            }
        }

        private void ScheduleSummaryRefresh(SelectedObjectDebugSnapshot snapshot)
        {
            if (snapshot.ResolveState == SelectedObjectResolveState.None ||
                snapshot.ResolveState == SelectedObjectResolveState.NotVehicle ||
                !snapshot.IsVehicle)
            {
                return;
            }

            bool selectionChangedSinceLastRequest =
                snapshot.SourceSelectedEntity != m_LastSummaryRequestedSourceEntity ||
                snapshot.ResolvedVehicleEntity != m_LastSummaryRequestedVehicleEntity;
            bool hasDisplaySnapshot =
                HasSummaryDisplayData(snapshot) ||
                HasCachedSummaryDisplayData(snapshot);
            int currentFrame = UnityEngine.Time.frameCount;
            bool refreshDue =
                m_LastSummaryRequestFrame == int.MinValue ||
                currentFrame - m_LastSummaryRequestFrame >=
                kSummaryRefreshIntervalFrames;

            if (!selectionChangedSinceLastRequest &&
                hasDisplaySnapshot &&
                !refreshDue)
            {
                return;
            }

            SelectedObjectBridgeSystem.RequestSummarySnapshot();
            m_LastSummaryRequestFrame = currentFrame;
            m_LastSummaryRequestedSourceEntity = snapshot.SourceSelectedEntity;
            m_LastSummaryRequestedVehicleEntity = snapshot.ResolvedVehicleEntity;
        }

        private bool TryGetSummaryDisplaySnapshot(
            SelectedObjectDebugSnapshot snapshot,
            out SelectedObjectDebugSnapshot summarySnapshot)
        {
            if (HasSummaryDisplayData(snapshot))
            {
                summarySnapshot = snapshot;
                return true;
            }

            if (HasCachedSummaryDisplayData(snapshot))
            {
                summarySnapshot = m_CachedSummarySnapshot;
                return true;
            }

            summarySnapshot = snapshot;
            return false;
        }

        private bool HasCachedSummaryDisplayData(
            SelectedObjectDebugSnapshot snapshot)
        {
            return m_HasCachedSummarySnapshot &&
                HasSummaryDisplayData(m_CachedSummarySnapshot) &&
                IsSameResolvedSelection(snapshot, m_CachedSummarySnapshot);
        }

        private static bool HasSummaryDisplayData(
            SelectedObjectDebugSnapshot snapshot)
        {
            return snapshot.IsVehicle &&
                !string.IsNullOrEmpty(snapshot.SummaryClassificationText) &&
                !string.IsNullOrEmpty(snapshot.SummaryTleStatusText);
        }

        private void RefreshLaneDetailsCache(SelectedObjectDebugSnapshot snapshot)
        {
            if (HasLaneDetailsDisplayData(snapshot))
            {
                m_CachedLaneDetailsSnapshot = snapshot;
                m_HasCachedLaneDetailsSnapshot = true;
                return;
            }

            if (m_HasCachedLaneDetailsSnapshot &&
                !IsSameResolvedSelection(snapshot, m_CachedLaneDetailsSnapshot))
            {
                ClearLaneDetailsCache();
            }
        }

        private void ScheduleLaneDetailsRefresh(SelectedObjectDebugSnapshot snapshot)
        {
            if (m_IsLaneDetailsCollapsed ||
                snapshot.TleApplicability != SelectedObjectTleApplicability.ApplicableReady ||
                snapshot.ResolvedVehicleEntity == Entity.Null)
            {
                return;
            }

            bool selectionChangedSinceLastRequest =
                snapshot.SourceSelectedEntity != m_LastLaneDetailsRequestedSourceEntity ||
                snapshot.ResolvedVehicleEntity != m_LastLaneDetailsRequestedVehicleEntity;
            bool hasDisplaySnapshot =
                HasLaneDetailsDisplayData(snapshot) ||
                HasCachedLaneDetailsDisplayData(snapshot);
            int currentFrame = UnityEngine.Time.frameCount;
            bool refreshDue =
                m_LastLaneDetailsRequestFrame == int.MinValue ||
                currentFrame - m_LastLaneDetailsRequestFrame >=
                kLaneDetailsRefreshIntervalFrames;

            if (!selectionChangedSinceLastRequest &&
                hasDisplaySnapshot &&
                !refreshDue)
            {
                return;
            }

            SelectedObjectBridgeSystem.RequestLaneDetailsSnapshot();
            m_LastLaneDetailsRequestFrame = currentFrame;
            m_LastLaneDetailsRequestedSourceEntity = snapshot.SourceSelectedEntity;
            m_LastLaneDetailsRequestedVehicleEntity = snapshot.ResolvedVehicleEntity;
        }

        private bool TryGetLaneDetailsDisplaySnapshot(
            SelectedObjectDebugSnapshot snapshot,
            out SelectedObjectDebugSnapshot laneDetailsSnapshot)
        {
            if (HasLaneDetailsDisplayData(snapshot))
            {
                laneDetailsSnapshot = snapshot;
                return true;
            }

            if (HasCachedLaneDetailsDisplayData(snapshot))
            {
                laneDetailsSnapshot = m_CachedLaneDetailsSnapshot;
                return true;
            }

            laneDetailsSnapshot = snapshot;
            return false;
        }

        private bool HasCachedLaneDetailsDisplayData(
            SelectedObjectDebugSnapshot snapshot)
        {
            return m_HasCachedLaneDetailsSnapshot &&
                HasLaneDetailsDisplayData(m_CachedLaneDetailsSnapshot) &&
                IsSameResolvedSelection(snapshot, m_CachedLaneDetailsSnapshot);
        }

        private static bool HasLaneDetailsDisplayData(
            SelectedObjectDebugSnapshot snapshot)
        {
            return snapshot.TleApplicability == SelectedObjectTleApplicability.ApplicableReady &&
                !string.IsNullOrEmpty(snapshot.CurrentLaneText) &&
                !string.IsNullOrEmpty(snapshot.PreviousLaneText);
        }

        private void RefreshRouteDiagnosticsCache(SelectedObjectDebugSnapshot snapshot)
        {
            if (HasRouteDiagnosticsDisplayData(snapshot))
            {
                m_CachedRouteDiagnosticsSnapshot = snapshot;
                m_HasCachedRouteDiagnosticsSnapshot = true;
                return;
            }

            if (m_HasCachedRouteDiagnosticsSnapshot &&
                !IsSameResolvedSelection(snapshot, m_CachedRouteDiagnosticsSnapshot))
            {
                m_HasCachedRouteDiagnosticsSnapshot = false;
                m_CachedRouteDiagnosticsSnapshot = default;
            }
        }

        private void ScheduleRouteDiagnosticsRefresh(SelectedObjectDebugSnapshot snapshot)
        {
            if (m_IsRouteDiagnosticsCollapsed ||
                !CanShowRouteDiagnosticsSection(snapshot) ||
                snapshot.TleApplicability != SelectedObjectTleApplicability.ApplicableReady ||
                snapshot.ResolvedVehicleEntity == Entity.Null)
            {
                return;
            }

            bool selectionChangedSinceLastRequest =
                snapshot.SourceSelectedEntity != m_LastRouteDiagnosticsRequestedSourceEntity ||
                snapshot.ResolvedVehicleEntity != m_LastRouteDiagnosticsRequestedVehicleEntity;
            bool hasDisplaySnapshot =
                HasRouteDiagnosticsDisplayData(snapshot) ||
                HasCachedRouteDiagnosticsDisplayData(snapshot);
            int currentFrame = UnityEngine.Time.frameCount;
            bool refreshDue =
                m_LastRouteDiagnosticsRequestFrame == int.MinValue ||
                currentFrame - m_LastRouteDiagnosticsRequestFrame >=
                kRouteDiagnosticsRefreshIntervalFrames;

            if (!selectionChangedSinceLastRequest &&
                hasDisplaySnapshot &&
                !refreshDue)
            {
                return;
            }

            SelectedObjectBridgeSystem.RequestRouteDiagnosticsSnapshot();
            m_LastRouteDiagnosticsRequestFrame = currentFrame;
            m_LastRouteDiagnosticsRequestedSourceEntity = snapshot.SourceSelectedEntity;
            m_LastRouteDiagnosticsRequestedVehicleEntity = snapshot.ResolvedVehicleEntity;
        }

        private bool TryGetRouteDiagnosticsDisplaySnapshot(
            SelectedObjectDebugSnapshot snapshot,
            out SelectedObjectDebugSnapshot routeDiagnosticsSnapshot)
        {
            if (HasRouteDiagnosticsDisplayData(snapshot))
            {
                routeDiagnosticsSnapshot = snapshot;
                return true;
            }

            if (HasCachedRouteDiagnosticsDisplayData(snapshot))
            {
                routeDiagnosticsSnapshot = m_CachedRouteDiagnosticsSnapshot;
                return true;
            }

            routeDiagnosticsSnapshot = snapshot;
            return false;
        }

        private bool HasCachedRouteDiagnosticsDisplayData(
            SelectedObjectDebugSnapshot snapshot)
        {
            return m_HasCachedRouteDiagnosticsSnapshot &&
                HasRouteDiagnosticsDisplayData(m_CachedRouteDiagnosticsSnapshot) &&
                IsSameResolvedSelection(snapshot, m_CachedRouteDiagnosticsSnapshot);
        }

        private static bool HasRouteDiagnosticsDisplayData(
            SelectedObjectDebugSnapshot snapshot)
        {
            return snapshot.HasRouteDiagnostics &&
                !string.IsNullOrEmpty(snapshot.RouteDiagnosticsCurrentTargetText) &&
                !string.IsNullOrEmpty(snapshot.RouteDiagnosticsCurrentRouteText);
        }

        private static bool CanShowRouteDiagnosticsSection(
            SelectedObjectDebugSnapshot snapshot)
        {
            return snapshot.TleApplicability == SelectedObjectTleApplicability.ApplicableReady &&
                snapshot.VehicleKind == SelectedObjectKind.RoadCar &&
                snapshot.ResolvedVehicleEntity != Entity.Null;
        }

        private static bool IsSameResolvedSelection(
            SelectedObjectDebugSnapshot left,
            SelectedObjectDebugSnapshot right)
        {
            return left.SourceSelectedEntity == right.SourceSelectedEntity &&
                left.ResolvedVehicleEntity == right.ResolvedVehicleEntity;
        }

        private void ClearRouteDiagnosticsCache()
        {
            m_HasCachedRouteDiagnosticsSnapshot = false;
            m_CachedRouteDiagnosticsSnapshot = default;
            m_LastRouteDiagnosticsRequestFrame = int.MinValue;
            m_LastRouteDiagnosticsRequestedSourceEntity = Entity.Null;
            m_LastRouteDiagnosticsRequestedVehicleEntity = Entity.Null;
        }

        private void ClearSummaryCache()
        {
            m_HasCachedSummarySnapshot = false;
            m_CachedSummarySnapshot = default;
            m_LastSummaryRequestFrame = int.MinValue;
            m_LastSummaryRequestedSourceEntity = Entity.Null;
            m_LastSummaryRequestedVehicleEntity = Entity.Null;
        }

        private void ClearLaneDetailsCache()
        {
            m_HasCachedLaneDetailsSnapshot = false;
            m_CachedLaneDetailsSnapshot = default;
            m_LastLaneDetailsRequestFrame = int.MinValue;
            m_LastLaneDetailsRequestedSourceEntity = Entity.Null;
            m_LastLaneDetailsRequestedVehicleEntity = Entity.Null;
        }

        private void ClearDeferredSnapshotCaches()
        {
            ClearSummaryCache();
            ClearLaneDetailsCache();
            ClearRouteDiagnosticsCache();
        }

        private void UpdateSaveScopedUiState()
        {
            int runtimeWorldGeneration = EnforcementSaveDataSystem.RuntimeWorldGeneration;
            if (runtimeWorldGeneration == m_LastSeenRuntimeWorldGeneration)
            {
                return;
            }

            if (m_LastSeenRuntimeWorldGeneration >= 0 &&
                runtimeWorldGeneration > m_LastSeenRuntimeWorldGeneration)
            {
                m_IsLaneDetailsCollapsed = true;
                m_LaneDetailsCollapsedBinding.Update(true);
                m_IsRouteDiagnosticsCollapsed = true;
                m_RouteDiagnosticsCollapsedBinding.Update(true);
                ClearDeferredSnapshotCaches();
            }

            m_LastSeenRuntimeWorldGeneration = runtimeWorldGeneration;
        }

        private string BuildCompactTleStatusText(SelectedObjectDebugSnapshot snapshot)
        {
            switch (snapshot.TleApplicability)
            {
                case SelectedObjectTleApplicability.NotApplicable:
                    return snapshot.ResolveState == SelectedObjectResolveState.NotVehicle
                        ? string.Empty
                        : m_NotApplicableText;

                case SelectedObjectTleApplicability.ApplicableNoLiveLaneData:
                    return m_NoLiveLaneText;

                case SelectedObjectTleApplicability.ApplicableReady:
                    return m_TrackingText;

                default:
                    return NormalizeText(snapshot.SummaryTleStatusText);
            }
        }

        private PanelState BuildNoSelectionState()
        {
            return new PanelState
            {
                Visible = true,
                Compact = false,
                Message = m_NoSelectionText,
                EntitySelectionSuggestedValue = string.Empty,
                EntitySelectionStatus = m_EntitySelectionStatus,
                EntitySelectionStatusIsError = m_EntitySelectionStatusIsError,
                PathObsoleteButtonVisible = false,
                PathObsoleteButtonEnabled = false,
                PathObsoleteStatus = string.Empty,
                PathObsoleteStatusIsError = false
            };
        }

        private PanelState BuildCollapsedState()
        {
            return new PanelState
            {
                Visible = true,
                Compact = false
            };
        }

        private void ApplyCollapsedBindings()
        {
            if (m_CollapsedFastPathApplied)
            {
                return;
            }

            m_VisibleBinding.Update(true);
            m_CompactBinding.Update(false);
            m_CollapsedBinding.Update(true);
            m_CollapsedFastPathApplied = true;
        }

        private bool CanMarkPathObsolete(SelectedObjectDebugSnapshot snapshot)
        {
            return snapshot.ResolveState != SelectedObjectResolveState.NotVehicle &&
                snapshot.ResolvedVehicleEntity != Entity.Null &&
                snapshot.HasPathOwner &&
                (snapshot.CurrentPathFlags & (PathFlags.Pending | PathFlags.Obsolete)) == 0;
        }

        private string BuildPathObsoleteStatusText(SelectedObjectDebugSnapshot snapshot)
        {
            if (!string.IsNullOrEmpty(m_PathObsoleteStatus))
            {
                return m_PathObsoleteStatus;
            }

            if (snapshot.ResolveState == SelectedObjectResolveState.NotVehicle ||
                snapshot.ResolvedVehicleEntity == Entity.Null)
            {
                return string.Empty;
            }

            if (!snapshot.HasPathOwner)
            {
                return m_PathObsoleteNoPathOwnerText;
            }

            if ((snapshot.CurrentPathFlags & PathFlags.Pending) != 0)
            {
                return m_PathObsoletePendingText;
            }

            if ((snapshot.CurrentPathFlags & PathFlags.Obsolete) != 0)
            {
                return m_PathObsoleteAlreadyObsoleteText;
            }

            return string.Empty;
        }

        private string BuildLiveLaneStateText(SelectedObjectDebugSnapshot snapshot)
        {
            switch (snapshot.TleApplicability)
            {
                case SelectedObjectTleApplicability.ApplicableReady:
                    if (!snapshot.HasPathOwner)
                    {
                        return m_LiveLaneStateNoPathOwnerText;
                    }

                    if ((snapshot.CurrentPathFlags & PathFlags.Failed) != 0)
                    {
                        return m_LiveLaneStatePathFailedText;
                    }

                    if ((snapshot.CurrentPathFlags & PathFlags.Stuck) != 0)
                    {
                        return m_LiveLaneStatePathStuckText;
                    }

                    if ((snapshot.CurrentPathFlags & PathFlags.Pending) != 0)
                    {
                        return m_LiveLaneStatePathPendingText;
                    }

                    if ((snapshot.CurrentPathFlags & PathFlags.Scheduled) != 0)
                    {
                        return m_LiveLaneStatePathScheduledText;
                    }

                    if (!snapshot.HasCurrentRoute)
                    {
                        return m_LiveLaneStateNoCurrentRouteText;
                    }

                    if (!snapshot.HasCurrentTarget)
                    {
                        return m_LiveLaneStateNoCurrentTargetText;
                    }

                    if ((snapshot.CurrentPathFlags & PathFlags.Obsolete) != 0)
                    {
                        return m_LiveLaneStatePathObsoleteText;
                    }

                    if ((snapshot.CurrentPathFlags & PathFlags.Updated) != 0)
                    {
                        return m_LiveLaneStatePathUpdatedText;
                    }

                    return m_LiveLaneStateReadyText;

                case SelectedObjectTleApplicability.ApplicableNoLiveLaneData:
                    return snapshot.VehicleKind == SelectedObjectKind.ParkedRoadCar
                        ? m_LiveLaneStateParkedRoadCarText
                        : m_LiveLaneStateNoLiveLaneText;

                default:
                    return m_LiveLaneStateNotApplicableText;
            }
        }

        private string BuildActiveFlagsText(SelectedObjectDebugSnapshot snapshot)
        {
            return string.Format(
                m_ActiveFlagsFormatText,
                m_ActiveFlagsViolationNameText,
                snapshot.PublicTransportLaneViolationActive ? m_FlagOnText : m_FlagOffText,
                m_ActiveFlagsPendingNameText,
                snapshot.PendingExitActive ? m_FlagOnText : m_FlagOffText);
        }

        private string BuildTotalsText(SelectedObjectDebugSnapshot snapshot)
        {
            return string.Format(
                m_TotalsFormatText,
                snapshot.TotalViolations,
                snapshot.TotalFines);
        }

        private string BuildDisplayedVehicleEntityText(SelectedObjectDebugSnapshot snapshot)
        {
            Entity entity = snapshot.ResolvedVehicleEntity != Entity.Null
                ? snapshot.ResolvedVehicleEntity
                : snapshot.SourceSelectedEntity;
            return entity == Entity.Null
                ? string.Empty
                : FormatEntity(entity);
        }

        private string BuildSuggestedEntitySelectionValue(SelectedObjectDebugSnapshot snapshot)
        {
            Entity entity = snapshot.ResolvedVehicleEntity != Entity.Null
                ? snapshot.ResolvedVehicleEntity
                : snapshot.SourceSelectedEntity;
            return entity == Entity.Null
                ? string.Empty
                : FormatEntity(entity);
        }

        private string BuildCurrentTargetDisplayText(SelectedObjectDebugSnapshot snapshot)
        {
            string fullText = NormalizeText(snapshot.RouteDiagnosticsCurrentTargetText);
            if (string.IsNullOrEmpty(fullText))
            {
                return string.Empty;
            }

            string entityText = BuildCurrentTargetEntityText(snapshot, fullText);
            if (string.IsNullOrEmpty(entityText) ||
                !fullText.StartsWith(entityText, System.StringComparison.Ordinal))
            {
                return fullText;
            }

            string remainder = fullText.Substring(entityText.Length).TrimStart();
            return string.IsNullOrEmpty(remainder) ? fullText : remainder;
        }

        private string BuildCurrentTargetEntityText(SelectedObjectDebugSnapshot snapshot)
        {
            return BuildCurrentTargetEntityText(
                snapshot,
                NormalizeText(snapshot.RouteDiagnosticsCurrentTargetText));
        }

        private string BuildCurrentTargetEntityText(
            SelectedObjectDebugSnapshot snapshot,
            string fullText)
        {
            Entity targetEntity = snapshot.CurrentTargetEntity;
            if (targetEntity == Entity.Null)
            {
                return string.Empty;
            }

            string entityText = FormatEntity(targetEntity);
            return string.Equals(fullText, entityText, System.StringComparison.Ordinal)
                ? string.Empty
                : entityText;
        }

        private string BuildCurrentRouteEntityText(SelectedObjectDebugSnapshot snapshot)
        {
            Entity routeEntity = snapshot.CurrentRouteEntity;
            if (routeEntity == Entity.Null)
            {
                return string.Empty;
            }

            string entityText = FormatEntity(routeEntity);
            string routeText = NormalizeText(snapshot.RouteDiagnosticsCurrentRouteText);
            return string.Equals(routeText, entityText, System.StringComparison.Ordinal)
                ? string.Empty
                : entityText;
        }

        private string BuildCurrentRouteColorText(SelectedObjectDebugSnapshot snapshot)
        {
            return snapshot.CurrentRouteColorText ?? string.Empty;
        }

        private bool TryParseEntitySelectionInput(string input, out Entity entity)
        {
            return EntityReferenceUtility.TryParse(NormalizeText(input), out entity);
        }

        private string BuildFocusLogStatusText(SelectedObjectDebugSnapshot snapshot)
        {
            if (snapshot.ResolveState == SelectedObjectResolveState.None ||
                snapshot.ResolveState == SelectedObjectResolveState.NotVehicle)
            {
                return string.Empty;
            }

            if (snapshot.TleApplicability != SelectedObjectTleApplicability.ApplicableReady ||
                snapshot.ResolvedVehicleEntity == Entity.Null)
            {
                return m_NotApplicableText;
            }

            return FocusedLoggingService.IsWatched(snapshot.ResolvedVehicleEntity)
                ? m_FocusLogWatchedText
                : m_FocusLogNotWatchedText;
        }

        private void SetEntitySelectionStatus(string text, bool isError)
        {
            m_EntitySelectionStatus = NormalizeText(text);
            m_EntitySelectionStatusIsError = isError;
            m_EntitySelectionStatusBinding.Update(m_EntitySelectionStatus);
            m_EntitySelectionStatusIsErrorBinding.Update(m_EntitySelectionStatusIsError);
        }

        private void SetEntitySelectionSuccessStatus(Entity entity)
        {
            string selectedEntityText = FormatEntity(entity);
            m_EntitySelectionStatusSelectedEntity = selectedEntityText;

            SetEntitySelectionStatus(
                string.Format(
                    LocalizeText(
                        kEntitySelectionStatusSelectedFormatLocaleId,
                        "Selected {0}."),
                    selectedEntityText),
                isError: false);
        }

        private void RefreshEntitySelectionStatus(string currentSuggestedEntitySelectionValue)
        {
            if (m_EntitySelectionStatusIsError || string.IsNullOrEmpty(m_EntitySelectionStatus))
            {
                return;
            }

            bool selectedEntityChanged =
                !string.IsNullOrEmpty(m_EntitySelectionStatusSelectedEntity) &&
                !string.Equals(
                    m_EntitySelectionStatusSelectedEntity,
                    currentSuggestedEntitySelectionValue ?? string.Empty,
                    System.StringComparison.Ordinal);

            if (selectedEntityChanged)
            {
                ClearEntitySelectionStatus();
            }
        }

        private void ClearEntitySelectionStatus()
        {
            m_EntitySelectionStatusSelectedEntity = string.Empty;
            SetEntitySelectionStatus(string.Empty, isError: false);
        }

        private void SetPathObsoleteStatus(string text, bool isError, string trackedEntityText)
        {
            m_PathObsoleteStatus = NormalizeText(text);
            m_PathObsoleteStatusIsError = isError;
            m_PathObsoleteStatusSelectedEntity = trackedEntityText ?? string.Empty;
            m_PathObsoleteStatusBinding.Update(m_PathObsoleteStatus);
            m_PathObsoleteStatusIsErrorBinding.Update(m_PathObsoleteStatusIsError);
        }

        private void RefreshPathObsoleteStatus(string currentSuggestedEntitySelectionValue)
        {
            if (string.IsNullOrEmpty(m_PathObsoleteStatus))
            {
                return;
            }

            bool selectedEntityChanged =
                !string.IsNullOrEmpty(m_PathObsoleteStatusSelectedEntity) &&
                !string.Equals(
                    m_PathObsoleteStatusSelectedEntity,
                    currentSuggestedEntitySelectionValue ?? string.Empty,
                    System.StringComparison.Ordinal);

            if (selectedEntityChanged)
            {
                ClearPathObsoleteStatus();
            }
        }

        private void ClearPathObsoleteStatus()
        {
            m_PathObsoleteStatusSelectedEntity = string.Empty;
            SetPathObsoleteStatus(string.Empty, isError: false, trackedEntityText: string.Empty);
        }

        private string FormatEntity(Entity entity)
        {
            return entity == Entity.Null
                ? m_NoneText
                : $"#{entity.Index}:v{entity.Version}";
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

    }
}
