using Game;
using Game.Simulation;
using Unity.Entities;
using UnityEngine;

namespace Traffic_Law_Enforcement
{
    public static class EnforcementGameTime
    {
        public const int MonthsPerYear = 12;
        public const long DayTicksPerDay = TimeSystem.kTicksPerDay;

        private static bool s_HasLoggedInitialization;

        public static long CurrentTimestampMonthTicks { get; private set; }
        public static long CurrentTimestampDayTicks { get; private set; }
        public static int CurrentDaysPerYear { get; private set; } = 12;
        public static bool IsInitialized { get; private set; }
        public static long CurrentMonthTicksPerMonth => GetMonthTicksPerMonth(CurrentDaysPerYear);

        public static void Update(long currentTimestampMonthTicks, long currentTimestampDayTicks, int currentDaysPerYear)
        {
            CurrentTimestampMonthTicks = currentTimestampMonthTicks;
            CurrentTimestampDayTicks = currentTimestampDayTicks;
            CurrentDaysPerYear = Mathf.Max(1, currentDaysPerYear);
            IsInitialized = true;
        }

        public static void Reset()
        {
            CurrentTimestampMonthTicks = 0L;
            CurrentTimestampDayTicks = 0L;
            CurrentDaysPerYear = 12;
            IsInitialized = false;
            s_HasLoggedInitialization = false;
        }

        public static bool TryUpdateFromWorld(World world, bool logOnInitialization, out string failureReason)
        {
            if (world == null)
            {
                failureReason = "default game world is unavailable";
                return false;
            }

            TimeSystem timeSystem = world.GetExistingSystemManaged<TimeSystem>() ?? world.GetOrCreateSystemManaged<TimeSystem>();
            return TryUpdateFromTimeSystem(timeSystem, logOnInitialization, out failureReason);
        }

        public static bool TryUpdateFromTimeSystem(TimeSystem timeSystem, bool logOnInitialization, out string failureReason)
        {
            if (timeSystem == null)
            {
                failureReason = "time system is unavailable";
                return false;
            }

            CalculateAbsoluteTicks(timeSystem, out long absoluteMonthTicks, out long absoluteDayTicks, out int daysPerYear);
            bool wasInitialized = IsInitialized;
            Update(absoluteMonthTicks, absoluteDayTicks, daysPerYear);

            if (logOnInitialization && !wasInitialized && !s_HasLoggedInitialization)
            {
                s_HasLoggedInitialization = true;
                Mod.log.Info($"Enforcement game time initialized. year={timeSystem.year}, normalizedDate={timeSystem.normalizedDate:0.0000}, normalizedTime={timeSystem.normalizedTime:0.0000}, daysPerYear={daysPerYear}, monthTicks={absoluteMonthTicks}, dayTicks={absoluteDayTicks}");
            }

            failureReason = null;
            return true;
        }

        private static void CalculateAbsoluteTicks(TimeSystem timeSystem, out long absoluteMonthTicks, out long absoluteDayTicks, out int daysPerYear)
        {
            daysPerYear = Mathf.Max(1, timeSystem.daysPerYear);
            int dayIndex = Mathf.Clamp(Mathf.FloorToInt(timeSystem.normalizedDate * daysPerYear), 0, daysPerYear - 1);
            float normalizedTime = Mathf.Clamp01(timeSystem.normalizedTime);
            double totalDays = (((double)timeSystem.year - 1d) * daysPerYear) + dayIndex + normalizedTime;
            double totalMonths = totalDays * MonthsPerYear / daysPerYear;
            long monthTicksPerMonth = GetMonthTicksPerMonth(daysPerYear);

            absoluteDayTicks = (long)System.Math.Round(totalDays * DayTicksPerDay);
            absoluteMonthTicks = (long)System.Math.Round(totalMonths * monthTicksPerMonth);
        }

        public static long GetMonthTicksPerMonth(int daysPerYear)
        {
            int safeDaysPerYear = Mathf.Max(1, daysPerYear);
            return System.Math.Max(1L, (long)System.Math.Round((double)TimeSystem.kTicksPerDay * safeDaysPerYear / MonthsPerYear));
        }

        public static long GetMonthTickWindow(int months)
        {
            return (long)System.Math.Max(1, months) * CurrentMonthTicksPerMonth;
        }

        public static long GetMonthIndex(long timestampMonthTicks)
        {
            long monthTicksPerMonth = CurrentMonthTicksPerMonth;
            return monthTicksPerMonth > 0L ? timestampMonthTicks / monthTicksPerMonth : 0L;
        }

        public static long GetMonthTickAtMonthIndex(long monthIndex)
        {
            return monthIndex * CurrentMonthTicksPerMonth;
        }
    }

    public class EnforcementGameTimeSystem : GameSystemBase
    {
        private TimeSystem m_TimeSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
        }

        protected override void OnUpdate()
        {
            if (!EnforcementGameTime.TryUpdateFromTimeSystem(m_TimeSystem, logOnInitialization: true, out _))
            {
                return;
            }

            EnforcementPolicyImpactService.UpdateTrackingForCurrentMonth();
            EnforcementPenaltyService.LogRepeatPolicySummaryIfChanged();
            EnforcementTelemetry.PruneExpiredViolationTimestamps();
        }
    }
}
