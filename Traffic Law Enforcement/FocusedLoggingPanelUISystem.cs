using Colossal.UI.Binding;
using Game;
using Game.Input;
using Game.SceneFlow;
using Game.UI;
using Game.UI.InGame;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public partial class FocusedLoggingPanelUISystem : UISystemBase
    {
        private const string kGroup = "focusedLoggingPanel";
        private const int kWatchedVehiclePruneIntervalUpdates = 30;

        internal const string kHeaderTextLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Text.Header";
        internal const string kSelectedVehicleLabelLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Label.SelectedVehicle";
        internal const string kSelectedRoleLabelLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Label.SelectedRole";
        internal const string kSelectedWatchStatusLabelLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Label.SelectedWatchStatus";
        internal const string kWatchedCountLabelLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Label.WatchedCount";
        internal const string kWatchedVehiclesLabelLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Label.WatchedVehicles";
        internal const string kBurstLoggingLabelLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Label.BurstLogging";
        internal const string kWatchSelectedButtonLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Text.WatchSelected";
        internal const string kUnwatchSelectedButtonLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Text.UnwatchSelected";
        internal const string kClearWatchedButtonLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Text.ClearWatched";
        internal const string kToggleBurstLoggingButtonLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Text.ToggleBurstLogging";
        internal const string kBurstLoggingActiveFormatLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Text.BurstLoggingActiveFormat";
        internal const string kBurstLoggingInactiveLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Text.BurstLoggingInactive";
        internal const string kWatchedCountFormatLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Text.WatchedCountFormat";
        internal const string kWatchedStatusLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Text.WatchedStatus";
        internal const string kNotWatchedStatusLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Text.NotWatchedStatus";
        internal const string kNoEligibleSelectionLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Text.NoEligibleSelection";
        internal const string kFooterHintLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Text.FooterHint";
        internal const string kWarningLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Text.Warning";
        internal const string kNoneLocaleId =
            "TrafficLawEnforcement.FocusedLoggingPanel.Text.None";

        private ProxyAction m_PanelToggleAction;
        private SelectedObjectBridgeSystem m_SelectedObjectBridgeSystem;
        private SelectedInfoUISystem m_SelectedInfoSystem;
        private string m_LastLocalizedLocaleId = string.Empty;
        private bool m_LocalizedBindingsInitialized;
        private int m_VisibleUpdateCount;
        private string m_NoneText = "None";
        private string m_WatchedStatusText = "Watched";
        private string m_NotWatchedStatusText = "Not watched";
        private string m_NoEligibleSelectionText = "No eligible selected road vehicle";
        private string m_WatchedCountFormatText = "{0} watched";
        private string m_BurstLoggingInactiveText = "Inactive";
        private string m_BurstLoggingActiveFormatText = "Active ({0:0.0}s remaining)";
        private int m_CachedWatchedCount = int.MinValue;
        private string m_CachedWatchedCountText = string.Empty;
        private long m_CachedBurstLoggingTenths = long.MinValue;
        private string m_CachedBurstLoggingText = "Inactive";

        private ValueBinding<bool> m_VisibleBinding;
        private ValueBinding<string> m_HeaderTextBinding;
        private ValueBinding<string> m_SelectedVehicleLabelBinding;
        private ValueBinding<string> m_SelectedRoleLabelBinding;
        private ValueBinding<string> m_SelectedWatchStatusLabelBinding;
        private ValueBinding<string> m_WatchedCountLabelBinding;
        private ValueBinding<string> m_WatchedVehiclesLabelBinding;
        private ValueBinding<string> m_BurstLoggingLabelBinding;
        private ValueBinding<string> m_WatchSelectedTextBinding;
        private ValueBinding<string> m_UnwatchSelectedTextBinding;
        private ValueBinding<string> m_ClearWatchedTextBinding;
        private ValueBinding<string> m_ToggleBurstLoggingTextBinding;
        private ValueBinding<string> m_FooterHintBinding;
        private ValueBinding<string> m_WarningBinding;
        private ValueBinding<string> m_SelectedVehicleBinding;
        private ValueBinding<string> m_SelectedRoleBinding;
        private ValueBinding<string> m_SelectedWatchStatusBinding;
        private ValueBinding<string> m_WatchedCountBinding;
        private ValueBinding<string> m_WatchedVehiclesBinding;
        private ValueBinding<string> m_BurstLoggingBinding;
        private ValueBinding<bool> m_BurstLoggingActiveBinding;
        private ValueBinding<string> m_MessageBinding;
        private ValueBinding<bool> m_WatchSelectedEnabledBinding;
        private ValueBinding<bool> m_UnwatchSelectedEnabledBinding;
        private ValueBinding<bool> m_ClearWatchedEnabledBinding;
        private ValueBinding<bool> m_ToggleBurstLoggingEnabledBinding;

        public override GameMode gameMode => GameMode.Game;

        private struct PanelState
        {
            public bool Visible;
            public string SelectedVehicle;
            public string SelectedRole;
            public string SelectedWatchStatus;
            public string WatchedCount;
            public string WatchedVehicles;
            public string BurstLogging;
            public bool BurstLoggingActive;
            public string Message;
            public bool WatchSelectedEnabled;
            public bool UnwatchSelectedEnabled;
            public bool ClearWatchedEnabled;
            public bool ToggleBurstLoggingEnabled;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SelectedObjectBridgeSystem =
                World.GetOrCreateSystemManaged<SelectedObjectBridgeSystem>();
            m_SelectedInfoSystem =
                World.GetExistingSystemManaged<SelectedInfoUISystem>();

            AddBinding(m_VisibleBinding = new ValueBinding<bool>(kGroup, "visible", false));
            AddBinding(m_HeaderTextBinding = new ValueBinding<string>(kGroup, "headerText", string.Empty));
            AddBinding(m_SelectedVehicleLabelBinding = new ValueBinding<string>(kGroup, "selectedVehicleLabelText", string.Empty));
            AddBinding(m_SelectedRoleLabelBinding = new ValueBinding<string>(kGroup, "selectedRoleLabelText", string.Empty));
            AddBinding(m_SelectedWatchStatusLabelBinding = new ValueBinding<string>(kGroup, "selectedWatchStatusLabelText", string.Empty));
            AddBinding(m_WatchedCountLabelBinding = new ValueBinding<string>(kGroup, "watchedCountLabelText", string.Empty));
            AddBinding(m_WatchedVehiclesLabelBinding = new ValueBinding<string>(kGroup, "watchedVehiclesLabelText", string.Empty));
            AddBinding(m_BurstLoggingLabelBinding = new ValueBinding<string>(kGroup, "burstLoggingLabelText", string.Empty));
            AddBinding(m_WatchSelectedTextBinding = new ValueBinding<string>(kGroup, "watchSelectedText", string.Empty));
            AddBinding(m_UnwatchSelectedTextBinding = new ValueBinding<string>(kGroup, "unwatchSelectedText", string.Empty));
            AddBinding(m_ClearWatchedTextBinding = new ValueBinding<string>(kGroup, "clearWatchedText", string.Empty));
            AddBinding(m_ToggleBurstLoggingTextBinding = new ValueBinding<string>(kGroup, "toggleBurstLoggingText", string.Empty));
            AddBinding(m_FooterHintBinding = new ValueBinding<string>(kGroup, "footerHintText", string.Empty));
            AddBinding(m_WarningBinding = new ValueBinding<string>(kGroup, "warningText", string.Empty));
            AddBinding(m_SelectedVehicleBinding = new ValueBinding<string>(kGroup, "selectedVehicle", string.Empty));
            AddBinding(m_SelectedRoleBinding = new ValueBinding<string>(kGroup, "selectedRole", string.Empty));
            AddBinding(m_SelectedWatchStatusBinding = new ValueBinding<string>(kGroup, "selectedWatchStatus", string.Empty));
            AddBinding(m_WatchedCountBinding = new ValueBinding<string>(kGroup, "watchedCount", string.Empty));
            AddBinding(m_WatchedVehiclesBinding = new ValueBinding<string>(kGroup, "watchedVehicles", string.Empty));
            AddBinding(m_BurstLoggingBinding = new ValueBinding<string>(kGroup, "burstLogging", string.Empty));
            AddBinding(m_BurstLoggingActiveBinding = new ValueBinding<bool>(kGroup, "burstLoggingActive", false));
            AddBinding(m_MessageBinding = new ValueBinding<string>(kGroup, "message", string.Empty));
            AddBinding(m_WatchSelectedEnabledBinding = new ValueBinding<bool>(kGroup, "watchSelectedEnabled", false));
            AddBinding(m_UnwatchSelectedEnabledBinding = new ValueBinding<bool>(kGroup, "unwatchSelectedEnabled", false));
            AddBinding(m_ClearWatchedEnabledBinding = new ValueBinding<bool>(kGroup, "clearWatchedEnabled", false));
            AddBinding(m_ToggleBurstLoggingEnabledBinding = new ValueBinding<bool>(kGroup, "toggleBurstLoggingEnabled", false));

            AddBinding(new TriggerBinding(kGroup, "close", HandleCloseRequested));
            AddBinding(new TriggerBinding(kGroup, "watchSelected", HandleWatchSelectedRequested));
            AddBinding(new TriggerBinding(kGroup, "unwatchSelected", HandleUnwatchSelectedRequested));
            AddBinding(new TriggerBinding(kGroup, "clearWatched", HandleClearWatchedRequested));
            AddBinding(new TriggerBinding(kGroup, "toggleBurstLogging", HandleToggleBurstLoggingRequested));
            AddBinding(new TriggerBinding<string>(kGroup, "selectWatchedVehicle", HandleSelectWatchedVehicleRequested));
        }

        protected override void OnDestroy()
        {
            SelectedObjectBridgeSystem.SetFocusedLoggingMinimalSnapshotConsumerActive(false);
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
            bool windowVisible = FocusedLoggingService.IsWindowVisible;
            SelectedObjectBridgeSystem.SetFocusedLoggingMinimalSnapshotConsumerActive(
                windowVisible);

            if (!windowVisible)
            {
                m_VisibleUpdateCount = 0;
                m_VisibleBinding.Update(false);
                return;
            }

            m_VisibleUpdateCount += 1;

            UpdateLocalizedTextBindingsIfNeeded();

            if (m_SelectedObjectBridgeSystem == null)
            {
                m_SelectedObjectBridgeSystem =
                    World.GetExistingSystemManaged<SelectedObjectBridgeSystem>();
            }

            int watchedVehicleCount = FocusedLoggingService.WatchedVehicleCount;
            bool hasWatchedVehicles = watchedVehicleCount > 0;
            if (hasWatchedVehicles)
            {
                bool shouldPruneWatchedVehicles =
                    m_VisibleUpdateCount == 1 ||
                    (m_VisibleUpdateCount % kWatchedVehiclePruneIntervalUpdates) == 0;
                if (shouldPruneWatchedVehicles)
                {
                    FocusedLoggingService.PruneMissingVehicles(EntityManager);
                }
            }

            PanelState state = BuildState(
                windowVisible,
                hasWatchedVehicles,
                watchedVehicleCount);
            UpdateBindings(state);
        }

        private PanelState BuildState(
            bool windowVisible,
            bool hasWatchedVehicles,
            int watchedVehicleCount)
        {
            bool hasSelectedVehicle =
                TryGetSelectedReadyRoadVehicle(
                    out SelectedObjectDebugSnapshot snapshot,
                    out Entity vehicle);
            bool watchedVehicle =
                hasSelectedVehicle &&
                FocusedLoggingService.IsWatched(vehicle);

            string watchedVehicles =
                hasWatchedVehicles
                    ? FocusedLoggingService.DescribeWatchedVehicles()
                    : string.Empty;
            double burstLoggingRemainingSeconds = BurstLoggingService.GetRemainingSeconds();
            bool burstLoggingActive = burstLoggingRemainingSeconds > 0d;

            return new PanelState
            {
                Visible = windowVisible,
                SelectedVehicle = hasSelectedVehicle
                    ? FocusedLoggingService.FormatEntity(vehicle)
                    : m_NoneText,
                SelectedRole = hasSelectedVehicle
                    ? NormalizeText(snapshot.RoleText)
                    : string.Empty,
                SelectedWatchStatus = hasSelectedVehicle
                    ? (watchedVehicle ? m_WatchedStatusText : m_NotWatchedStatusText)
                    : m_NoEligibleSelectionText,
                WatchedCount = GetWatchedCountText(watchedVehicleCount),
                WatchedVehicles = string.IsNullOrWhiteSpace(watchedVehicles)
                    ? m_NoneText
                    : watchedVehicles,
                BurstLogging = BuildBurstLoggingText(burstLoggingRemainingSeconds),
                BurstLoggingActive = burstLoggingActive,
                Message = hasSelectedVehicle
                    ? string.Empty
                    : m_NoEligibleSelectionText,
                WatchSelectedEnabled = hasSelectedVehicle && !watchedVehicle,
                UnwatchSelectedEnabled = hasSelectedVehicle && watchedVehicle,
                ClearWatchedEnabled = hasWatchedVehicles,
                ToggleBurstLoggingEnabled = true,
            };
        }

        private void UpdateBindings(PanelState state)
        {
            m_VisibleBinding.Update(state.Visible);
            m_SelectedVehicleBinding.Update(state.SelectedVehicle ?? string.Empty);
            m_SelectedRoleBinding.Update(state.SelectedRole ?? string.Empty);
            m_SelectedWatchStatusBinding.Update(state.SelectedWatchStatus ?? string.Empty);
            m_WatchedCountBinding.Update(state.WatchedCount ?? string.Empty);
            m_WatchedVehiclesBinding.Update(state.WatchedVehicles ?? string.Empty);
            m_BurstLoggingBinding.Update(state.BurstLogging ?? string.Empty);
            m_BurstLoggingActiveBinding.Update(state.BurstLoggingActive);
            m_MessageBinding.Update(state.Message ?? string.Empty);
            m_WatchSelectedEnabledBinding.Update(state.WatchSelectedEnabled);
            m_UnwatchSelectedEnabledBinding.Update(state.UnwatchSelectedEnabled);
            m_ClearWatchedEnabledBinding.Update(state.ClearWatchedEnabled);
            m_ToggleBurstLoggingEnabledBinding.Update(state.ToggleBurstLoggingEnabled);
        }

        private void UpdateLocalizedTextBindings()
        {
            m_HeaderTextBinding.Update(LocalizeText(kHeaderTextLocaleId, "Focused logging"));
            m_SelectedVehicleLabelBinding.Update(LocalizeText(kSelectedVehicleLabelLocaleId, "Selected vehicle"));
            m_SelectedRoleLabelBinding.Update(LocalizeText(kSelectedRoleLabelLocaleId, "Selected role"));
            m_SelectedWatchStatusLabelBinding.Update(LocalizeText(kSelectedWatchStatusLabelLocaleId, "Selected watch status"));
            m_WatchedCountLabelBinding.Update(LocalizeText(kWatchedCountLabelLocaleId, "Watched count"));
            m_WatchedVehiclesLabelBinding.Update(LocalizeText(kWatchedVehiclesLabelLocaleId, "Watched vehicles"));
            m_BurstLoggingLabelBinding.Update(LocalizeText(kBurstLoggingLabelLocaleId, "Burst logging"));
            m_WatchSelectedTextBinding.Update(LocalizeText(kWatchSelectedButtonLocaleId, "Watch selected"));
            m_UnwatchSelectedTextBinding.Update(LocalizeText(kUnwatchSelectedButtonLocaleId, "Unwatch selected"));
            m_ClearWatchedTextBinding.Update(LocalizeText(kClearWatchedButtonLocaleId, "Clear watched"));
            m_ToggleBurstLoggingTextBinding.Update(LocalizeText(kToggleBurstLoggingButtonLocaleId, "Toggle burst logging (5s)"));
            m_WarningBinding.Update(LocalizeText(
                kWarningLocaleId,
                "Warning: Focused logging only records log types enabled in Debug options."));
            m_FooterHintBinding.Update(LocalizeText(
                kFooterHintLocaleId,
                "For enabled vehicle-specific debug logs, watched vehicles stay filtered while global/state logs remain unchanged."));
            m_NoneText = LocalizeText(kNoneLocaleId, "None");
            m_WatchedStatusText = LocalizeText(kWatchedStatusLocaleId, "Watched");
            m_NotWatchedStatusText = LocalizeText(kNotWatchedStatusLocaleId, "Not watched");
            m_NoEligibleSelectionText = LocalizeText(
                kNoEligibleSelectionLocaleId,
                "No eligible selected road vehicle");
            m_WatchedCountFormatText = LocalizeText(kWatchedCountFormatLocaleId, "{0} watched");
            m_BurstLoggingInactiveText = LocalizeText(kBurstLoggingInactiveLocaleId, "Inactive");
            m_BurstLoggingActiveFormatText = LocalizeText(
                kBurstLoggingActiveFormatLocaleId,
                "Active ({0:0.0}s remaining)");
            InvalidateDynamicTextCaches();
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
                FocusedLoggingService.ToggleWindowVisible();
            }
        }

        private ProxyAction GetPanelToggleAction()
        {
            if (m_PanelToggleAction == null && Mod.Settings != null)
            {
                m_PanelToggleAction =
                    Mod.Settings.GetAction(KeybindingIds.FocusedLoggingPanelToggleActionName);

                if (m_PanelToggleAction != null)
                {
                    m_PanelToggleAction.shouldBeEnabled = true;
                }
            }

            return m_PanelToggleAction;
        }

        private void HandleCloseRequested()
        {
            FocusedLoggingService.SetWindowVisible(false);
            SelectedObjectBridgeSystem.SetFocusedLoggingMinimalSnapshotConsumerActive(false);
        }

        private void HandleWatchSelectedRequested()
        {
            if (TryGetSelectedReadyRoadVehicle(
                    out SelectedObjectDebugSnapshot _,
                    out Entity vehicle))
            {
                FocusedLoggingService.AddWatchedVehicle(vehicle);
            }
        }

        private void HandleUnwatchSelectedRequested()
        {
            if (TryGetSelectedReadyRoadVehicle(
                    out SelectedObjectDebugSnapshot _,
                    out Entity vehicle))
            {
                FocusedLoggingService.RemoveWatchedVehicle(vehicle);
            }
        }

        private void HandleClearWatchedRequested()
        {
            FocusedLoggingService.ClearWatchedVehicles();
        }

        private void HandleToggleBurstLoggingRequested()
        {
            BurstLoggingService.ToggleDefaultBurst();
        }

        private void HandleSelectWatchedVehicleRequested(string entityText)
        {
            if (!EntityReferenceUtility.TryParse(entityText, out Entity entity) ||
                !EntityManager.Exists(entity))
            {
                FocusedLoggingService.PruneMissingVehicles(EntityManager);
                return;
            }

            if (m_SelectedInfoSystem == null)
            {
                m_SelectedInfoSystem =
                    World.GetExistingSystemManaged<SelectedInfoUISystem>();
            }

            m_SelectedInfoSystem?.SetSelection(entity);
        }

        private string BuildBurstLoggingText(double remainingSeconds)
        {
            if (remainingSeconds <= 0d)
            {
                m_CachedBurstLoggingTenths = 0;
                m_CachedBurstLoggingText = m_BurstLoggingInactiveText;
                return m_CachedBurstLoggingText;
            }

            long remainingTenths = (long)System.Math.Ceiling(remainingSeconds * 10d);
            if (m_CachedBurstLoggingTenths == remainingTenths)
            {
                return m_CachedBurstLoggingText;
            }

            m_CachedBurstLoggingTenths = remainingTenths;
            m_CachedBurstLoggingText = string.Format(
                m_BurstLoggingActiveFormatText,
                remainingTenths / 10d);
            return m_CachedBurstLoggingText;
        }

        private string GetWatchedCountText(int watchedCount)
        {
            if (m_CachedWatchedCount == watchedCount)
            {
                return m_CachedWatchedCountText;
            }

            m_CachedWatchedCount = watchedCount;
            m_CachedWatchedCountText = string.Format(
                m_WatchedCountFormatText,
                watchedCount);
            return m_CachedWatchedCountText;
        }

        private void InvalidateDynamicTextCaches()
        {
            m_CachedWatchedCount = int.MinValue;
            m_CachedWatchedCountText = string.Empty;
            m_CachedBurstLoggingTenths = long.MinValue;
            m_CachedBurstLoggingText = m_BurstLoggingInactiveText;
        }

        private bool TryGetSelectedReadyRoadVehicle(
            out SelectedObjectDebugSnapshot snapshot,
            out Entity vehicle)
        {
            vehicle = Entity.Null;
            if (m_SelectedObjectBridgeSystem == null || !m_SelectedObjectBridgeSystem.HasSnapshot)
            {
                snapshot = default;
                return false;
            }

            snapshot = m_SelectedObjectBridgeSystem.CurrentSnapshot;
            if (snapshot.TleApplicability != SelectedObjectTleApplicability.ApplicableReady ||
                snapshot.ResolvedVehicleEntity == Entity.Null)
            {
                return false;
            }

            vehicle = snapshot.ResolvedVehicleEntity;
            return true;
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
