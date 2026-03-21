using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Simulation;
using Game.Triggers;

namespace Traffic_Law_Enforcement
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(Traffic_Law_Enforcement)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        public static Setting Settings { get; private set; }
        public static bool IsEnforcementEnabled => EnforcementGameplaySettingsService.Current.HasAnyEnforcementEnabled();
        public static bool IsPublicTransportLaneEnforcementEnabled => EnforcementGameplaySettingsService.Current.EnablePublicTransportLaneEnforcement;
        public static bool IsMidBlockCrossingEnforcementEnabled => EnforcementGameplaySettingsService.Current.EnableMidBlockCrossingEnforcement;
        public static bool IsIntersectionMovementEnforcementEnabled => EnforcementGameplaySettingsService.Current.EnableIntersectionMovementEnforcement;
        private Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            EnforcementGameTime.Reset();

            m_Setting = new Setting(this);
            Settings = m_Setting;
            AssetDatabase.global.LoadSettings(nameof(Traffic_Law_Enforcement), m_Setting, new Setting(this));
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));
            GameManager.instance.localizationManager.AddSource("ko-KR", new LocaleKO(m_Setting));
            BudgetUIPatches.Apply();
            VehicleUtilsPatches.Apply();
            updateSystem.UpdateAfter<EnforcementSaveDataSystem, EnforcementGameTimeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<PathfindingMoneyPenaltySystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<PublicTransportLanePermissionSystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<PublicTransportLanePermissionSystem, PublicTransportLaneViolationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<CenterlineAccessObsoleteSystem, PublicTransportLanePermissionSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<CenterlineAccessObsoleteSystem, PublicTransportLaneExitPressureSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<CenterlineAccessObsoleteSystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<EnforcementGameTimeSystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<SettingsChangeLoggingSystem, EnforcementGameTimeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<MonthlyEnforcementChirperSystem, EnforcementGameTimeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<MonthlyEnforcementChirperSystem, CreateChirpSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<VehicleLaneHistorySystem, EnforcementGameTimeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<RoutePenaltyRerouteLoggingSystem, VehicleLaneHistorySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<PublicTransportLaneViolationSystem, EnforcementGameTimeSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<PublicTransportLaneExitPressureSystem, PublicTransportLaneViolationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<PublicTransportLaneExitPressureSystem, CarNavigationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<LaneTransitionViolationSystem, VehicleLaneHistorySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<EnforcementFineMoneySystem, PublicTransportLaneViolationSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<EnforcementFineMoneySystem, LaneTransitionViolationSystem>(SystemUpdatePhase.GameSimulation);
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            VehicleUtilsPatches.Remove();
            BudgetUIPatches.Remove();
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
                Settings = null;
            }
        }
    }
}
