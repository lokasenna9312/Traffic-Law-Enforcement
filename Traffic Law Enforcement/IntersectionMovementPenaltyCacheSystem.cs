using Game;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public partial class IntersectionMovementPenaltyCacheSystem : GameSystemBase
    {
        private bool m_LastEnabled;
        private int m_LastSettingsRevision;
        private int m_LastSettingsVersion = -1;
        private long m_LastCacheClearDayTicks;
        private long m_LastObservedDayTicks = long.MinValue;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<Game.Vehicles.Car>()));
        }

        protected override void OnUpdate()
        {
            bool enabled = Mod.IsIntersectionMovementEnforcementEnabled;

            if (!enabled)
            {
                if (m_LastEnabled)
                {
                    IntersectionMovementPenaltyCache.Clear();
                }

                m_LastEnabled = false;
                m_LastSettingsRevision = 0;
                m_LastSettingsVersion = -1;
                m_LastCacheClearDayTicks = 0L;
                m_LastObservedDayTicks = long.MinValue;
                return;
            }

            int settingsVersion = EnforcementGameplaySettingsService.Version;
            long currentDayTicks = EnforcementGameTime.IsInitialized
                ? EnforcementGameTime.CurrentTimestampDayTicks
                : 0L;
            if (m_LastEnabled &&
                settingsVersion == m_LastSettingsVersion &&
                currentDayTicks == m_LastObservedDayTicks)
            {
                return;
            }

            int settingsRevision = m_LastSettingsRevision;
            bool settingsRevisionChanged = false;
            if (!m_LastEnabled || settingsVersion != m_LastSettingsVersion)
            {
                settingsRevision = ComputeSettingsRevision(
                    EnforcementGameplaySettingsService.Current);
                settingsRevisionChanged =
                    settingsRevision != m_LastSettingsRevision;
            }

            bool cacheExpired =
                currentDayTicks > 0L &&
                currentDayTicks - m_LastCacheClearDayTicks >=
                EnforcementGameTime.DayTicksPerDay * 2;

            if (cacheExpired)
            {
                IntersectionMovementPenaltyCache.Clear();
                m_LastCacheClearDayTicks = currentDayTicks;
            }

            if (!m_LastEnabled || settingsRevisionChanged || cacheExpired)
            {
                IntersectionMovementPenaltyCache.RefreshContext(
                    EntityManager,
                    EnforcementPenaltyService.GetIntersectionMovementFine(),
                    settingsRevision,
                    (ulong)World.Unmanaged.SequenceNumber);
            }

            m_LastEnabled = true;
            m_LastSettingsRevision = settingsRevision;
            m_LastSettingsVersion = settingsVersion;
            m_LastObservedDayTicks = currentDayTicks;
        }

        protected override void OnDestroy()
        {
            IntersectionMovementPenaltyCache.Clear();
            base.OnDestroy();
        }

        private static int ComputeSettingsRevision(EnforcementGameplaySettingsState settings)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + (settings.EnableIntersectionMovementEnforcement ? 1 : 0);
                hash = (hash * 31) + settings.IntersectionMovementFineAmount;
                hash = (hash * 31) + (settings.EnableIntersectionMovementRepeatPenalty ? 1 : 0);
                hash = (hash * 31) + settings.IntersectionMovementRepeatWindowMonths;
                hash = (hash * 31) + settings.IntersectionMovementRepeatThreshold;
                hash = (hash * 31) + settings.IntersectionMovementRepeatMultiplierPercent;
                return hash;
            }
        }
    }
}
