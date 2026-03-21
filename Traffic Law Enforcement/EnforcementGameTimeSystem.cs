using Game;
using Game.Net;
using Game.Pathfind;
using Game.Simulation;
using Game.Vehicles;
using Unity.Collections;
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

    public partial class EnforcementGameTimeSystem : GameSystemBase
    {
        private TimeSystem m_TimeSystem;
        private EntityQuery m_ActiveTrafficQuery;
        private ComponentLookup<EdgeLane> m_EdgeLaneData;
        private ComponentLookup<CarLane> m_CarLaneData;
        private ComponentLookup<ParkingLane> m_ParkingLaneData;
        private ComponentLookup<GarageLane> m_GarageLaneData;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            m_ActiveTrafficQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadOnly<PathOwner>());
            m_EdgeLaneData = GetComponentLookup<EdgeLane>(true);
            m_CarLaneData = GetComponentLookup<CarLane>(true);
            m_ParkingLaneData = GetComponentLookup<ParkingLane>(true);
            m_GarageLaneData = GetComponentLookup<GarageLane>(true);
        }

        protected override void OnUpdate()
        {
            bool result = EnforcementGameTime.TryUpdateFromTimeSystem(m_TimeSystem, logOnInitialization: true, out string failureReason);
            if (!result)
            {
                return;
            }

            EnforcementPolicyImpactService.UpdateTrackingForCurrentMonth();
            EnforcementPenaltyService.LogRepeatPolicySummaryIfChanged();
            EnforcementTelemetry.PruneExpiredViolationTimestamps();
        }

        private int CountActiveRoadTrafficVehicles()
        {
            m_EdgeLaneData.Update(this);
            m_CarLaneData.Update(this);
            m_ParkingLaneData.Update(this);
            m_GarageLaneData.Update(this);

            NativeArray<CarCurrentLane> currentLanes = m_ActiveTrafficQuery.ToComponentDataArray<CarCurrentLane>(Allocator.Temp);
            try
            {
                int activeRoadTrafficCount = 0;
                int nullLaneCount = 0;
                int missingEdgeLaneCount = 0;
                int missingCarLaneCount = 0;
                int parkingLaneCount = 0;
                int garageLaneCount = 0;
                for (int index = 0; index < currentLanes.Length; index += 1)
                {
                    Entity lane = currentLanes[index].m_Lane;
                    if (lane == Entity.Null)
                    {
                        nullLaneCount += 1;
                        continue;
                    }

                    if (!m_EdgeLaneData.HasComponent(lane))
                    {
                        missingEdgeLaneCount += 1;
                        continue;
                    }

                    if (!m_CarLaneData.HasComponent(lane))
                    {
                        missingCarLaneCount += 1;
                        continue;
                    }

                    if (m_ParkingLaneData.HasComponent(lane))
                    {
                        parkingLaneCount += 1;
                        continue;
                    }

                    if (m_GarageLaneData.HasComponent(lane))
                    {
                        garageLaneCount += 1;
                        continue;
                    }

                    activeRoadTrafficCount += 1;
                }
                return activeRoadTrafficCount;
            }
            finally
            {
                currentLanes.Dispose();
            }
        }
    }
}
