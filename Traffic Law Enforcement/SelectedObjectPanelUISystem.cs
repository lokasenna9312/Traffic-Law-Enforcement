using Colossal.UI.Binding;
using Game;
using Game.Input;
using Game.SceneFlow;
using Game.UI;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public partial class SelectedObjectPanelUISystem : UISystemBase
    {
        private const string kGroup = "selectedObjectPanel";
        internal const string kHeaderTextLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.Header";
        internal const string kSummaryTitleLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.SummaryTitle";
        internal const string kTleStatusLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.TleStatus";
        internal const string kRoleLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.RoleOrType";
        internal const string kActiveFlagsLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.ActiveFlags";
        internal const string kViolationsFinesLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.ViolationsFines";
        internal const string kLastReasonLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.LastReason";
        internal const string kPublicTransportLanePolicyLabelLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Label.PublicTransportLanePolicy";
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
        internal const string kLiveLaneStateReadyLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStateReady";
        internal const string kLiveLaneStateNoLiveLaneLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStateNoLiveLane";
        internal const string kLiveLaneStateNotApplicableLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStateNotApplicable";
        internal const string kLiveLaneStateParkedRoadCarLocaleId = "TrafficLawEnforcement.SelectedObjectPanel.Text.LiveLaneStateParkedRoadCar";

        private SelectedObjectBridgeSystem m_SelectedObjectBridgeSystem;
        private ProxyAction m_PanelToggleAction;

        private ValueBinding<bool> m_VisibleBinding;
        private ValueBinding<bool> m_CompactBinding;
        private ValueBinding<bool> m_CollapsedBinding;
        private ValueBinding<string> m_ClassificationBinding;
        private ValueBinding<string> m_MessageBinding;
        private ValueBinding<string> m_TleStatusBinding;
        private ValueBinding<string> m_RoleBinding;
        private ValueBinding<string> m_PublicTransportLanePolicyBinding;
        private ValueBinding<string> m_VehicleIndexBinding;
        private ValueBinding<string> m_ViolationPendingBinding;
        private ValueBinding<string> m_TotalsBinding;
        private ValueBinding<string> m_LastReasonBinding;
        private ValueBinding<string> m_HeaderTextBinding;
        private ValueBinding<string> m_SummaryTitleBinding;
        private ValueBinding<string> m_TleStatusLabelBinding;
        private ValueBinding<string> m_RoleLabelBinding;
        private ValueBinding<string> m_ActiveFlagsLabelBinding;
        private ValueBinding<string> m_ViolationsFinesLabelBinding;
        private ValueBinding<string> m_LastReasonLabelBinding;
        private ValueBinding<string> m_PublicTransportLanePolicyLabelBinding;
        private ValueBinding<string> m_FooterTextBinding;
        private ValueBinding<string> m_ExpandSectionTooltipBinding;
        private ValueBinding<string> m_CollapseSectionTooltipBinding;
        private ValueBinding<string> m_LaneDetailsTitleBinding;
        private ValueBinding<string> m_CurrentLaneLabelBinding;
        private ValueBinding<string> m_PreviousLaneLabelBinding;
        private ValueBinding<string> m_LaneChangesLabelBinding;
        private ValueBinding<string> m_LiveLaneStateLabelBinding;
        private ValueBinding<string> m_CurrentLaneBinding;
        private ValueBinding<string> m_PreviousLaneBinding;
        private ValueBinding<string> m_LaneChangesBinding;
        private ValueBinding<string> m_LiveLaneStateBinding;
        private ValueBinding<bool> m_LaneDetailsCollapsedBinding;

        private bool m_IsPanelEnabled;
        private bool m_IsCollapsed;
        private bool m_IsLaneDetailsCollapsed = true;
        private int m_LastSeenPendingLoadSequence = -1;

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
            public string VehicleIndex;
            public string ViolationPending;
            public string Totals;
            public string LastReason;
            public string CurrentLane;
            public string PreviousLane;
            public string LaneChanges;
            public string LiveLaneState;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SelectedObjectBridgeSystem =
                World.GetOrCreateSystemManaged<SelectedObjectBridgeSystem>();

            AddBinding(m_VisibleBinding = new ValueBinding<bool>(kGroup, "visible", false));
            AddBinding(m_CompactBinding = new ValueBinding<bool>(kGroup, "compact", false));
            AddBinding(m_CollapsedBinding = new ValueBinding<bool>(kGroup, "collapsed", false));
            AddBinding(m_ClassificationBinding = new ValueBinding<string>(kGroup, "classification", string.Empty));
            AddBinding(m_MessageBinding = new ValueBinding<string>(kGroup, "message", string.Empty));
            AddBinding(m_TleStatusBinding = new ValueBinding<string>(kGroup, "tleStatus", string.Empty));
            AddBinding(m_RoleBinding = new ValueBinding<string>(kGroup, "role", string.Empty));
            AddBinding(m_PublicTransportLanePolicyBinding = new ValueBinding<string>(kGroup, "publicTransportLanePolicy", string.Empty));
            AddBinding(m_VehicleIndexBinding = new ValueBinding<string>(kGroup, "vehicleIndex", string.Empty));
            AddBinding(m_ViolationPendingBinding = new ValueBinding<string>(kGroup, "violationPending", string.Empty));
            AddBinding(m_TotalsBinding = new ValueBinding<string>(kGroup, "totals", string.Empty));
            AddBinding(m_LastReasonBinding = new ValueBinding<string>(kGroup, "lastReason", string.Empty));
            AddBinding(m_HeaderTextBinding = new ValueBinding<string>(kGroup, "headerText", string.Empty));
            AddBinding(m_SummaryTitleBinding = new ValueBinding<string>(kGroup, "summaryTitle", string.Empty));
            AddBinding(m_TleStatusLabelBinding = new ValueBinding<string>(kGroup, "tleStatusLabelText", string.Empty));
            AddBinding(m_RoleLabelBinding = new ValueBinding<string>(kGroup, "roleLabelText", string.Empty));
            AddBinding(m_ActiveFlagsLabelBinding = new ValueBinding<string>(kGroup, "activeFlagsLabelText", string.Empty));
            AddBinding(m_ViolationsFinesLabelBinding = new ValueBinding<string>(kGroup, "violationsFinesLabelText", string.Empty));
            AddBinding(m_LastReasonLabelBinding = new ValueBinding<string>(kGroup, "lastReasonLabelText", string.Empty));
            AddBinding(m_PublicTransportLanePolicyLabelBinding = new ValueBinding<string>(kGroup, "publicTransportLanePolicyLabelText", string.Empty));
            AddBinding(m_FooterTextBinding = new ValueBinding<string>(kGroup, "footerText", string.Empty));
            AddBinding(m_ExpandSectionTooltipBinding = new ValueBinding<string>(kGroup, "expandSectionTooltipText", string.Empty));
            AddBinding(m_CollapseSectionTooltipBinding = new ValueBinding<string>(kGroup, "collapseSectionTooltipText", string.Empty));
            AddBinding(m_LaneDetailsTitleBinding = new ValueBinding<string>(kGroup, "laneDetailsTitleText", string.Empty));
            AddBinding(m_CurrentLaneLabelBinding = new ValueBinding<string>(kGroup, "currentLaneLabelText", string.Empty));
            AddBinding(m_PreviousLaneLabelBinding = new ValueBinding<string>(kGroup, "previousLaneLabelText", string.Empty));
            AddBinding(m_LaneChangesLabelBinding = new ValueBinding<string>(kGroup, "laneChangesLabelText", string.Empty));
            AddBinding(m_LiveLaneStateLabelBinding = new ValueBinding<string>(kGroup, "liveLaneStateLabelText", string.Empty));
            AddBinding(m_CurrentLaneBinding = new ValueBinding<string>(kGroup, "currentLane", string.Empty));
            AddBinding(m_PreviousLaneBinding = new ValueBinding<string>(kGroup, "previousLane", string.Empty));
            AddBinding(m_LaneChangesBinding = new ValueBinding<string>(kGroup, "laneChanges", string.Empty));
            AddBinding(m_LiveLaneStateBinding = new ValueBinding<string>(kGroup, "liveLaneState", string.Empty));
            AddBinding(m_LaneDetailsCollapsedBinding = new ValueBinding<bool>(kGroup, "laneDetailsCollapsed", true));

            AddBinding(new TriggerBinding(kGroup, "close", HandleCloseRequested));
            AddBinding(new TriggerBinding(kGroup, "toggleCollapsed", ToggleCollapsed));
            AddBinding(new TriggerBinding(kGroup, "toggleLaneDetailsCollapsed", ToggleLaneDetailsCollapsed));
        }

        protected override void OnDestroy()
        {
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
            UpdateLocalizedTextBindings();

            if (m_SelectedObjectBridgeSystem == null)
            {
                m_SelectedObjectBridgeSystem =
                    World.GetExistingSystemManaged<SelectedObjectBridgeSystem>();
            }

            if (!m_IsPanelEnabled)
            {
                UpdateBindings(default);
                return;
            }

            if (m_SelectedObjectBridgeSystem == null || !m_SelectedObjectBridgeSystem.HasSnapshot)
            {
                UpdateBindings(BuildNoSelectionState());
                return;
            }

            UpdateBindings(BuildState(m_SelectedObjectBridgeSystem.CurrentSnapshot));
        }

        private PanelState BuildState(SelectedObjectDebugSnapshot snapshot)
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
                    Compact = true,
                    Message = LocalizeText(kNotVehicleLocaleId, "Not a vehicle")
                };
            }

            if (snapshot.TleApplicability != SelectedObjectTleApplicability.ApplicableReady)
            {
                return new PanelState
                {
                    Visible = true,
                    Compact = true,
                    Classification = snapshot.SummaryClassificationText,
                    TleStatus = BuildCompactTleStatusText(snapshot),
                    VehicleIndex = snapshot.VehicleIndex >= 0
                        ? snapshot.VehicleIndex.ToString()
                        : string.Empty,
                };
            }

            return new PanelState
            {
                Visible = true,
                Compact = false,
                Classification = snapshot.SummaryClassificationText,
                TleStatus = BuildCompactTleStatusText(snapshot),
                Role = NormalizeText(snapshot.RoleText),
                PublicTransportLanePolicy =
                    NormalizeText(snapshot.PublicTransportLanePolicyText),
                VehicleIndex = snapshot.VehicleIndex >= 0
                    ? snapshot.VehicleIndex.ToString()
                    : string.Empty,
                ViolationPending =
                    $"Violation {snapshot.PtLaneViolationActive}, Pending {snapshot.PendingExitActive}",
                Totals =
                    $"Violations {snapshot.TotalViolations}, Fines {snapshot.TotalFines}",
                LastReason = NormalizeText(snapshot.CompactLastReasonText),
                CurrentLane = FormatEntity(snapshot.CurrentLaneEntity),
                PreviousLane = FormatEntity(snapshot.PreviousLaneEntity),
                LaneChanges = snapshot.LaneChangeCount.ToString(),
                LiveLaneState = BuildLiveLaneStateText(snapshot)
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
            m_VehicleIndexBinding.Update(state.VehicleIndex ?? string.Empty);
            m_ViolationPendingBinding.Update(state.ViolationPending ?? string.Empty);
            m_TotalsBinding.Update(state.Totals ?? string.Empty);
            m_LastReasonBinding.Update(state.LastReason ?? string.Empty);
            m_CurrentLaneBinding.Update(state.CurrentLane ?? string.Empty);
            m_PreviousLaneBinding.Update(state.PreviousLane ?? string.Empty);
            m_LaneChangesBinding.Update(state.LaneChanges ?? string.Empty);
            m_LiveLaneStateBinding.Update(state.LiveLaneState ?? string.Empty);
            m_LaneDetailsCollapsedBinding.Update(m_IsLaneDetailsCollapsed);
        }

        private void UpdateLocalizedTextBindings()
        {
            m_HeaderTextBinding.Update(LocalizeText(kHeaderTextLocaleId, "Selected Object"));
            m_SummaryTitleBinding.Update(LocalizeText(kSummaryTitleLocaleId, "Summary"));
            m_TleStatusLabelBinding.Update(LocalizeText(kTleStatusLabelLocaleId, "TLE status"));
            m_RoleLabelBinding.Update(LocalizeText(kRoleLabelLocaleId, "Role / type"));
            m_ActiveFlagsLabelBinding.Update(LocalizeText(kActiveFlagsLabelLocaleId, "Active flags"));
            m_ViolationsFinesLabelBinding.Update(LocalizeText(kViolationsFinesLabelLocaleId, "Violations / fines"));
            m_LastReasonLabelBinding.Update(LocalizeText(kLastReasonLabelLocaleId, "Last reason"));
            m_PublicTransportLanePolicyLabelBinding.Update(LocalizeText(kPublicTransportLanePolicyLabelLocaleId, "PT lane policy"));
            m_FooterTextBinding.Update(LocalizeText(kFooterHintLocaleId, "If Developer Mode is enabled, press Tab for more details."));
            m_ExpandSectionTooltipBinding.Update(LocalizeText(kExpandSectionLocaleId, "Expand section"));
            m_CollapseSectionTooltipBinding.Update(LocalizeText(kCollapseSectionLocaleId, "Collapse section"));
            m_LaneDetailsTitleBinding.Update(LocalizeText(kLaneDetailsTitleLocaleId, "Lane details"));
            m_CurrentLaneLabelBinding.Update(LocalizeText(kCurrentLaneLabelLocaleId, "Current lane"));
            m_PreviousLaneLabelBinding.Update(LocalizeText(kPreviousLaneLabelLocaleId, "Previous lane"));
            m_LaneChangesLabelBinding.Update(LocalizeText(kLaneChangesLabelLocaleId, "Lane changes"));
            m_LiveLaneStateLabelBinding.Update(LocalizeText(kLiveLaneStateLabelLocaleId, "Live lane state"));
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
        }

        private void ToggleCollapsed()
        {
            if (!m_IsPanelEnabled)
            {
                return;
            }

            m_IsCollapsed = !m_IsCollapsed;
            m_CollapsedBinding.Update(m_IsCollapsed);
        }

        private void ToggleLaneDetailsCollapsed()
        {
            m_IsLaneDetailsCollapsed = !m_IsLaneDetailsCollapsed;
            m_LaneDetailsCollapsedBinding.Update(m_IsLaneDetailsCollapsed);
        }

        private static string NormalizeText(string text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : text.Trim();
        }

        private void UpdateSaveScopedUiState()
        {
            int pendingLoadSequence = SaveLoadTraceService.PendingLoadSequence;
            if (pendingLoadSequence == m_LastSeenPendingLoadSequence)
            {
                return;
            }

            if (m_LastSeenPendingLoadSequence >= 0 &&
                pendingLoadSequence > m_LastSeenPendingLoadSequence)
            {
                m_IsLaneDetailsCollapsed = true;
                m_LaneDetailsCollapsedBinding.Update(true);
            }

            m_LastSeenPendingLoadSequence = pendingLoadSequence;
        }

        private string BuildCompactTleStatusText(SelectedObjectDebugSnapshot snapshot)
        {
            switch (snapshot.TleApplicability)
            {
                case SelectedObjectTleApplicability.NotApplicable:
                    return snapshot.ResolveState == SelectedObjectResolveState.NotVehicle
                        ? string.Empty
                        : LocalizeText(kNotApplicableLocaleId, "Not applicable");

                case SelectedObjectTleApplicability.ApplicableNoLiveLaneData:
                    return LocalizeText(kNoLiveLaneLocaleId, "No live lane");

                case SelectedObjectTleApplicability.ApplicableReady:
                    return LocalizeText(kTrackingLocaleId, "Tracking");

                default:
                    return NormalizeText(snapshot.SummaryTleStatusText);
            }
        }

        private PanelState BuildNoSelectionState()
        {
            return new PanelState
            {
                Visible = true,
                Compact = true,
                Message = LocalizeText(kNoSelectionLocaleId, "No selection")
            };
        }

        private string BuildLiveLaneStateText(SelectedObjectDebugSnapshot snapshot)
        {
            switch (snapshot.TleApplicability)
            {
                case SelectedObjectTleApplicability.ApplicableReady:
                    return LocalizeText(kLiveLaneStateReadyLocaleId, "Ready");

                case SelectedObjectTleApplicability.ApplicableNoLiveLaneData:
                    return snapshot.VehicleKind == SelectedObjectKind.ParkedRoadCar
                        ? LocalizeText(kLiveLaneStateParkedRoadCarLocaleId, "Parked road car")
                        : LocalizeText(kLiveLaneStateNoLiveLaneLocaleId, "No live lane");

                default:
                    return LocalizeText(kLiveLaneStateNotApplicableLocaleId, "Not applicable");
            }
        }

        private static string FormatEntity(Entity entity)
        {
            return entity == Entity.Null
                ? "None"
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
