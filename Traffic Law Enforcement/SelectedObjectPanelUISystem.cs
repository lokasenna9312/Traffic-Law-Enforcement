using Game;
using Game.Input;
using Game.SceneFlow;
using Game.UI;
using Unity.Entities;
using UnityEngine;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public partial class SelectedObjectPanelUISystem : UISystemBase
    {
        private const string kPanelObjectName = "TrafficLawEnforcement.SelectedObjectPanel";

        private SelectedObjectBridgeSystem m_SelectedObjectBridgeSystem;
        private ProxyAction m_PanelToggleAction;
        private GameObject m_PanelObject;
        private SelectedObjectPanelView m_PanelView;
        private bool m_IsPanelEnabled;

        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SelectedObjectBridgeSystem =
                World.GetOrCreateSystemManaged<SelectedObjectBridgeSystem>();
            EnsurePanelView();
        }

        protected override void OnDestroy()
        {
            if (m_PanelToggleAction != null)
            {
                m_PanelToggleAction.shouldBeEnabled = false;
                m_PanelToggleAction = null;
            }

            DestroyPanelView();
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

            if (!EnsurePanelView())
            {
                return;
            }

            if (!m_IsPanelEnabled)
            {
                m_PanelView.UpdateState(default);
                return;
            }

            if (m_SelectedObjectBridgeSystem == null || !m_SelectedObjectBridgeSystem.HasSnapshot)
            {
                m_PanelView.UpdateState(BuildNoSelectionState());
                return;
            }

            m_PanelView.UpdateState(BuildState(m_SelectedObjectBridgeSystem.CurrentSnapshot));
        }

        private SelectedObjectPanelView.State BuildState(
            SelectedObjectDebugSnapshot snapshot)
        {
            if (snapshot.ResolveState == SelectedObjectResolveState.None)
            {
                return BuildNoSelectionState();
            }

            if (snapshot.ResolveState == SelectedObjectResolveState.NotVehicle)
            {
                return new SelectedObjectPanelView.State
                {
                    Visible = true,
                    Compact = true,
                    SelectionToken = BuildSelectionToken(snapshot),
                    Message = "Selected object is not a vehicle"
                };
            }

            bool tleApplicable =
                snapshot.TleApplicability != SelectedObjectTleApplicability.NotApplicable;
            bool tleReady =
                snapshot.TleApplicability == SelectedObjectTleApplicability.ApplicableReady;

            return new SelectedObjectPanelView.State
            {
                Visible = true,
                Compact = false,
                SelectionToken = BuildSelectionToken(snapshot),
                Classification = snapshot.SummaryClassificationText,
                TleStatus = BuildCompactTleStatus(snapshot),
                RoleOrType = NormalizeText(snapshot.RoleOrTypeText),
                VehicleIndex = snapshot.VehicleIndex >= 0
                    ? snapshot.VehicleIndex.ToString()
                    : string.Empty,
                ViolationPending = tleReady
                    ? $"Violation {snapshot.PtLaneViolationActive}, Pending {snapshot.PendingExitActive}"
                    : string.Empty,
                Totals = tleApplicable
                    ? $"Violations {snapshot.TotalViolations}, Fines {snapshot.TotalFines}"
                    : string.Empty,
                LastReason = tleApplicable
                    ? NormalizeText(snapshot.CompactLastReasonText)
                    : string.Empty,
                ResolvedEntity = FormatEntity(snapshot.ResolvedVehicleEntity)
            };
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

        private bool EnsurePanelView()
        {
            if (m_PanelView != null)
            {
                return true;
            }

            if (m_PanelObject == null)
            {
                m_PanelObject = new GameObject(kPanelObjectName)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                Object.DontDestroyOnLoad(m_PanelObject);
            }

            m_PanelView = m_PanelObject.GetComponent<SelectedObjectPanelView>();
            if (m_PanelView == null)
            {
                m_PanelView = m_PanelObject.AddComponent<SelectedObjectPanelView>();
            }

            m_PanelView.CloseRequested -= HandleCloseRequested;
            m_PanelView.CloseRequested += HandleCloseRequested;

            return m_PanelView != null;
        }

        private void DestroyPanelView()
        {
            if (m_PanelView != null)
            {
                m_PanelView.CloseRequested -= HandleCloseRequested;
            }

            if (m_PanelObject != null)
            {
                Object.Destroy(m_PanelObject);
                m_PanelObject = null;
                m_PanelView = null;
            }
        }

        private void HandleCloseRequested()
        {
            m_IsPanelEnabled = false;

            if (m_PanelView != null)
            {
                m_PanelView.UpdateState(default);
            }
        }

        private static string NormalizeText(string text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : text.Trim();
        }

        private static string FormatEntity(Entity entity)
        {
            return entity == Entity.Null
                ? "None"
                : entity.ToString();
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

        private static string BuildSelectionToken(SelectedObjectDebugSnapshot snapshot)
        {
            return
                $"{snapshot.ResolveState}|{snapshot.SourceSelectedEntity}|{snapshot.ResolvedVehicleEntity}";
        }

        private static SelectedObjectPanelView.State BuildNoSelectionState()
        {
            return new SelectedObjectPanelView.State
            {
                Visible = true,
                Compact = true,
                Message = "No object selected"
            };
        }
    }
}
