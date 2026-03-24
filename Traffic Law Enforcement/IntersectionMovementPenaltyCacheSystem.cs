using Game;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public partial class IntersectionMovementPenaltyCacheSystem : GameSystemBase
    {
        private bool m_LastEnabled;
        private int m_LastSettingsRevision;
        private long m_LastCacheClearDayTicks;

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
                m_LastCacheClearDayTicks = 0L;
                return;
            }

            EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;

            int settingsRevision = ComputeSettingsRevision(settings);
            ulong worldRevision = (ulong)World.Unmanaged.SequenceNumber;

            IntersectionMovementPenaltyCache.RefreshContext(
                EntityManager,
                EnforcementPenaltyService.GetIntersectionMovementFine(),
                settingsRevision,
                worldRevision);

            long currentDayTicks = EnforcementGameTime.IsInitialized
                ? EnforcementGameTime.CurrentTimestampDayTicks
                : 0L;

            if (currentDayTicks > 0L &&
                currentDayTicks - m_LastCacheClearDayTicks >= EnforcementGameTime.DayTicksPerDay * 2)
            {
                IntersectionMovementPenaltyCache.Clear();
                m_LastCacheClearDayTicks = currentDayTicks;
            }

            m_LastEnabled = true;
            m_LastSettingsRevision = settingsRevision;
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
