using Game;
using Game.SceneFlow;
using Game.UI;
using Unity.Entities;
using UnityEngine;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public partial class SelectedVehiclePanelUISystem : UISystemBase
    {
        private const string kPanelObjectName = "TrafficLawEnforcement.SelectedVehiclePanel";

        private SelectedVehicleBridgeSystem m_SelectedVehicleBridgeSystem;
        private GameObject m_PanelObject;
        private SelectedVehiclePanelView m_PanelView;

        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SelectedVehicleBridgeSystem =
                World.GetOrCreateSystemManaged<SelectedVehicleBridgeSystem>();
            EnsurePanelView();
        }

        protected override void OnDestroy()
        {
            DestroyPanelView();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (m_SelectedVehicleBridgeSystem == null)
            {
                m_SelectedVehicleBridgeSystem =
                    World.GetExistingSystemManaged<SelectedVehicleBridgeSystem>();
            }

            if (!EnsurePanelView())
            {
                return;
            }

            if (m_SelectedVehicleBridgeSystem == null || !m_SelectedVehicleBridgeSystem.HasSnapshot)
            {
                m_PanelView.UpdateState(default);
                return;
            }

            m_PanelView.UpdateState(BuildState(m_SelectedVehicleBridgeSystem.CurrentSnapshot));
        }

        private SelectedVehiclePanelView.State BuildState(
            SelectedVehicleDebugSnapshot snapshot)
        {
            if (snapshot.ResolveState == SelectedVehicleResolveState.None)
            {
                return default;
            }

            if (snapshot.ResolveState == SelectedVehicleResolveState.NotVehicle)
            {
                return new SelectedVehiclePanelView.State
                {
                    Visible = true,
                    Compact = true,
                    SelectionToken = BuildSelectionToken(snapshot),
                    Message = "Selected object is not a vehicle"
                };
            }

            bool tleApplicable =
                snapshot.TleApplicability != SelectedVehicleTleApplicability.NotApplicable;
            bool tleReady =
                snapshot.TleApplicability == SelectedVehicleTleApplicability.ApplicableReady;

            return new SelectedVehiclePanelView.State
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

            m_PanelView = m_PanelObject.GetComponent<SelectedVehiclePanelView>();
            if (m_PanelView == null)
            {
                m_PanelView = m_PanelObject.AddComponent<SelectedVehiclePanelView>();
            }

            return m_PanelView != null;
        }

        private void DestroyPanelView()
        {
            if (m_PanelObject != null)
            {
                Object.Destroy(m_PanelObject);
                m_PanelObject = null;
                m_PanelView = null;
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

        private static string BuildCompactTleStatus(SelectedVehicleDebugSnapshot snapshot)
        {
            switch (snapshot.TleApplicability)
            {
                case SelectedVehicleTleApplicability.NotApplicable:
                    return snapshot.ResolveState == SelectedVehicleResolveState.NotVehicle
                        ? string.Empty
                        : "Not applicable";

                case SelectedVehicleTleApplicability.ApplicableNoLiveLaneData:
                    return "No live lane";

                case SelectedVehicleTleApplicability.ApplicableReady:
                    return "Tracking";

                default:
                    return NormalizeText(snapshot.SummaryTleStatusText);
            }
        }

        private static string BuildSelectionToken(SelectedVehicleDebugSnapshot snapshot)
        {
            return
                $"{snapshot.ResolveState}|{snapshot.SourceSelectedEntity}|{snapshot.ResolvedVehicleEntity}";
        }
    }
}
