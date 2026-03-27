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

        private SelectedObjectBridgeSystem m_SelectedObjectBridgeSystem;
        private ProxyAction m_PanelToggleAction;

        private ValueBinding<bool> m_VisibleBinding;
        private ValueBinding<bool> m_CompactBinding;
        private ValueBinding<bool> m_CollapsedBinding;
        private ValueBinding<string> m_ClassificationBinding;
        private ValueBinding<string> m_MessageBinding;
        private ValueBinding<string> m_TleStatusBinding;
        private ValueBinding<string> m_RoleBinding;
        private ValueBinding<string> m_PublicTransportLaneAllowanceBinding;
        private ValueBinding<string> m_VehicleIndexBinding;
        private ValueBinding<string> m_ViolationPendingBinding;
        private ValueBinding<string> m_TotalsBinding;
        private ValueBinding<string> m_LastReasonBinding;

        private bool m_IsPanelEnabled;
        private bool m_IsCollapsed;

        public override GameMode gameMode => GameMode.Game;

        private struct PanelState
        {
            public bool Visible;
            public bool Compact;
            public string Classification;
            public string Message;
            public string TleStatus;
            public string Role;
            public string PublicTransportLaneAllowance;
            public string VehicleIndex;
            public string ViolationPending;
            public string Totals;
            public string LastReason;
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
            AddBinding(m_PublicTransportLaneAllowanceBinding = new ValueBinding<string>(kGroup, "publicTransportLaneAllowance", string.Empty));
            AddBinding(m_VehicleIndexBinding = new ValueBinding<string>(kGroup, "vehicleIndex", string.Empty));
            AddBinding(m_ViolationPendingBinding = new ValueBinding<string>(kGroup, "violationPending", string.Empty));
            AddBinding(m_TotalsBinding = new ValueBinding<string>(kGroup, "totals", string.Empty));
            AddBinding(m_LastReasonBinding = new ValueBinding<string>(kGroup, "lastReason", string.Empty));

            AddBinding(new TriggerBinding(kGroup, "close", HandleCloseRequested));
            AddBinding(new TriggerBinding(kGroup, "toggleCollapsed", ToggleCollapsed));
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
                    Message = "Not a vehicle"
                };
            }

            if (snapshot.TleApplicability != SelectedObjectTleApplicability.ApplicableReady)
            {
                return new PanelState
                {
                    Visible = true,
                    Compact = true,
                    Classification = snapshot.SummaryClassificationText,
                    TleStatus = BuildCompactTleStatus(snapshot),
                    VehicleIndex = snapshot.VehicleIndex >= 0
                        ? snapshot.VehicleIndex.ToString()
                        : string.Empty,
                };
            }

            string role = ExtractRoleText(snapshot.RoleOrTypeText);

            return new PanelState
            {
                Visible = true,
                Compact = false,
                Classification = snapshot.SummaryClassificationText,
                TleStatus = BuildCompactTleStatus(snapshot),
                Role = role,
                PublicTransportLaneAllowance =
                    NormalizeText(snapshot.PublicTransportLaneAllowanceText),
                VehicleIndex = snapshot.VehicleIndex >= 0
                    ? snapshot.VehicleIndex.ToString()
                    : string.Empty,
                ViolationPending =
                    $"Violation {snapshot.PtLaneViolationActive}, Pending {snapshot.PendingExitActive}",
                Totals =
                    $"Violations {snapshot.TotalViolations}, Fines {snapshot.TotalFines}",
                LastReason = NormalizeText(snapshot.CompactLastReasonText)
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
            m_PublicTransportLaneAllowanceBinding.Update(state.PublicTransportLaneAllowance ?? string.Empty);
            m_VehicleIndexBinding.Update(state.VehicleIndex ?? string.Empty);
            m_ViolationPendingBinding.Update(state.ViolationPending ?? string.Empty);
            m_TotalsBinding.Update(state.Totals ?? string.Empty);
            m_LastReasonBinding.Update(state.LastReason ?? string.Empty);
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

        private static string NormalizeText(string text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : text.Trim();
        }

        private static string BuildCompactTleStatus(SelectedObjectDebugSnapshot snapshot)
        {
            switch (snapshot.TleApplicability)
            {
                case SelectedObjectTleApplicability.NotApplicable:
                    return snapshot.ResolveState == SelectedObjectResolveState.NotVehicle
                        ? string.Empty
                        : "Not applicable";

                case SelectedObjectTleApplicability.ApplicableNoLiveLaneData:
                    return "No live lane";

                case SelectedObjectTleApplicability.ApplicableReady:
                    return "Tracking";

                default:
                    return NormalizeText(snapshot.SummaryTleStatusText);
            }
        }

        private static PanelState BuildNoSelectionState()
        {
            return new PanelState
            {
                Visible = true,
                Compact = true,
                Message = "No selection"
            };
        }

        private static string ExtractRoleText(string roleOrType)
        {
            string normalized = NormalizeText(roleOrType);
            if (string.IsNullOrEmpty(normalized))
            {
                return string.Empty;
            }

            string[] segments = normalized.Split('|');
            foreach (string rawSegment in segments)
            {
                string segment = NormalizeText(rawSegment);
                if (!string.IsNullOrEmpty(segment))
                {
                    return segment;
                }
            }

            return string.Empty;
        }
    }
}
