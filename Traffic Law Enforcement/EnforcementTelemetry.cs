using System.Collections.Generic;
using System.Linq;

namespace Traffic_Law_Enforcement
{
    public static class EnforcementTelemetry
    {
        private const int MaxEvents = 8;
        private const int MaxRecords = 24;
        private static readonly List<string> s_RecentEvents = new List<string>(MaxEvents);
        private static readonly List<EnforcementRecord> s_RecentRecords = new List<EnforcementRecord>(MaxRecords);
        private static readonly Dictionary<int, int> s_VehicleFineTotals = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> s_VehicleViolationCounts = new Dictionary<int, int>();
        private static readonly Dictionary<string, Dictionary<int, List<long>>> s_ViolationTimestamps = new Dictionary<string, Dictionary<int, List<long>>>();

        public static int ActivePublicTransportLaneViolators { get; private set; }
        public static int PublicTransportLaneViolationCount { get; private set; }
        public static int MidBlockCrossingViolationCount { get; private set; }
        public static int IntersectionMovementViolationCount { get; private set; }
        public static int TotalFineAmount { get; private set; }

        public static string RecentEventsText
        {
            get
            {
                if (s_RecentEvents.Count == 0)
                {
                    return "No enforcement events yet.";
                }

                return string.Join("\n", s_RecentEvents.ToArray());
            }
        }

        public static string RecentRecordsText
        {
            get
            {
                if (s_RecentRecords.Count == 0)
                {
                    return "No vehicle records yet.";
                }

                return string.Join("\n", s_RecentRecords.Select(record => record.ToString()).ToArray());
            }
        }

        public static string VehicleFineTotalsText
        {
            get
            {
                if (s_VehicleFineTotals.Count == 0)
                {
                    return "No fined vehicles yet.";
                }

                return string.Join("\n", s_VehicleFineTotals.OrderByDescending(pair => pair.Value).Take(10).Select(pair => $"vehicle {pair.Key}: {pair.Value}").ToArray());
            }
        }

        public static string VehicleViolationCountsText
        {
            get
            {
                if (s_VehicleViolationCounts.Count == 0)
                {
                    return "No repeat offenders yet.";
                }

                return string.Join("\n", s_VehicleViolationCounts.OrderByDescending(pair => pair.Value).Take(10).Select(pair => $"vehicle {pair.Key}: {pair.Value} violations").ToArray());
            }
        }

        public static void SetStatistics(TrafficLawEnforcementStatistics statistics)
        {
            PublicTransportLaneViolationCount = statistics.m_PublicTransportLaneViolationCount;
            ActivePublicTransportLaneViolators = statistics.m_ActivePublicTransportLaneViolatorCount;
            MidBlockCrossingViolationCount = statistics.m_MidBlockCrossingViolationCount;
            IntersectionMovementViolationCount = statistics.m_IntersectionMovementViolationCount;
        }

        public static void RecordEvent(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            while (s_RecentEvents.Count >= MaxEvents)
            {
                s_RecentEvents.RemoveAt(0);
            }

            s_RecentEvents.Add(message);
        }

        public static void RecordFine(string kind, int vehicleId, int laneId, int fineAmount, string reason)
        {
            long nowMonthTicks = EnforcementGameTime.CurrentTimestampMonthTicks;
            RegisterViolationTimestamp(kind, vehicleId, nowMonthTicks);

            EnforcementRecord record = new EnforcementRecord(kind, vehicleId, laneId, fineAmount, reason);

            while (s_RecentRecords.Count >= MaxRecords)
            {
                s_RecentRecords.RemoveAt(0);
            }

            s_RecentRecords.Add(record);
            TotalFineAmount += fineAmount;

            if (s_VehicleFineTotals.TryGetValue(vehicleId, out int currentTotal))
            {
                s_VehicleFineTotals[vehicleId] = currentTotal + fineAmount;
            }
            else
            {
                s_VehicleFineTotals[vehicleId] = fineAmount;
            }

            if (s_VehicleViolationCounts.TryGetValue(vehicleId, out int violationCount))
            {
                s_VehicleViolationCounts[vehicleId] = violationCount + 1;
            }
            else
            {
                s_VehicleViolationCounts[vehicleId] = 1;
            }

        }

        public static int GetVehicleViolationCount(int vehicleId)
        {
            return s_VehicleViolationCounts.TryGetValue(vehicleId, out int count) ? count : 0;
        }

        public static int GetRecentViolationCount(string kind, int vehicleId, long windowMonthTicks, bool includeCurrentEvent)
        {
            if (!s_ViolationTimestamps.TryGetValue(kind, out Dictionary<int, List<long>> vehicleMap))
            {
                return includeCurrentEvent ? 1 : 0;
            }

            if (!vehicleMap.TryGetValue(vehicleId, out List<long> timestamps))
            {
                return includeCurrentEvent ? 1 : 0;
            }

            if (!EnforcementGameTime.IsInitialized)
            {
                return timestamps.Count + (includeCurrentEvent ? 1 : 0);
            }

            long cutoff = EnforcementGameTime.CurrentTimestampMonthTicks - windowMonthTicks;
            TrimQueue(timestamps, cutoff);
            if (timestamps.Count == 0)
            {
                vehicleMap.Remove(vehicleId);
                if (vehicleMap.Count == 0)
                {
                    s_ViolationTimestamps.Remove(kind);
                }
            }

            return timestamps.Count + (includeCurrentEvent ? 1 : 0);
        }

        public static IReadOnlyDictionary<int, (int violationCount, int fineTotal)> GetVehiclePenaltySnapshot()
        {
            Dictionary<int, (int violationCount, int fineTotal)> snapshot = new Dictionary<int, (int violationCount, int fineTotal)>();
            foreach (KeyValuePair<int, int> pair in s_VehicleViolationCounts)
            {
                s_VehicleFineTotals.TryGetValue(pair.Key, out int fineTotal);
                snapshot[pair.Key] = (pair.Value, fineTotal);
            }

            return snapshot;
        }

        public static TrafficLawEnforcementStatistics GetStatisticsSnapshot()
        {
            return new TrafficLawEnforcementStatistics
            {
                m_PublicTransportLaneViolationCount = PublicTransportLaneViolationCount,
                m_ActivePublicTransportLaneViolatorCount = ActivePublicTransportLaneViolators,
                m_MidBlockCrossingViolationCount = MidBlockCrossingViolationCount,
                m_IntersectionMovementViolationCount = IntersectionMovementViolationCount,
            };
        }

        public static IReadOnlyCollection<EnforcementRecord> GetRecentRecordsSnapshot()
        {
            return s_RecentRecords.ToArray();
        }

        public static IReadOnlyCollection<(string kind, int vehicleId, long timestampMonthTicks)> GetViolationTimestampSnapshot()
        {
            List<(string kind, int vehicleId, long timestampMonthTicks)> snapshot = new List<(string kind, int vehicleId, long timestampMonthTicks)>();
            foreach (KeyValuePair<string, Dictionary<int, List<long>>> kindEntry in s_ViolationTimestamps)
            {
                foreach (KeyValuePair<int, List<long>> vehicleEntry in kindEntry.Value)
                {
                    foreach (long timestamp in vehicleEntry.Value)
                    {
                        snapshot.Add((kindEntry.Key, vehicleEntry.Key, timestamp));
                    }
                }
            }

            return snapshot;
        }

        public static void ResetPersistentData()
        {
            s_RecentEvents.Clear();
            s_RecentRecords.Clear();
            s_VehicleFineTotals.Clear();
            s_VehicleViolationCounts.Clear();
            s_ViolationTimestamps.Clear();
            ActivePublicTransportLaneViolators = 0;
            PublicTransportLaneViolationCount = 0;
            MidBlockCrossingViolationCount = 0;
            IntersectionMovementViolationCount = 0;
            TotalFineAmount = 0;
        }

        public static void LoadPersistentData(TrafficLawEnforcementStatistics statistics, int totalFineAmount, IDictionary<int, int> vehicleFineTotals, IDictionary<int, int> vehicleViolationCounts, IEnumerable<EnforcementRecord> records, IEnumerable<(string kind, int vehicleId, long timestampMonthTicks)> timestamps)
        {
            SetStatistics(statistics);
            TotalFineAmount = totalFineAmount;

            foreach (KeyValuePair<int, int> pair in vehicleFineTotals)
            {
                s_VehicleFineTotals[pair.Key] = pair.Value;
            }

            foreach (KeyValuePair<int, int> pair in vehicleViolationCounts)
            {
                s_VehicleViolationCounts[pair.Key] = pair.Value;
            }

            int skipCount = System.Math.Max(0, records.Count() - MaxRecords);
            foreach (EnforcementRecord record in records.Skip(skipCount))
            {
                s_RecentRecords.Add(record);
            }

            foreach ((string kind, int vehicleId, long timestampMonthTicks) entry in timestamps)
            {
                RegisterViolationTimestamp(entry.kind, entry.vehicleId, entry.timestampMonthTicks);
            }

            PruneExpiredViolationTimestamps();
        }

        public static void PruneExpiredViolationTimestamps()
        {
            if (!EnforcementGameTime.IsInitialized)
            {
                return;
            }

            long maxWindowMonthTicks = EnforcementPenaltyService.GetMaximumRepeatWindowMonthTicks();
            long cutoff = EnforcementGameTime.CurrentTimestampMonthTicks - maxWindowMonthTicks;
            List<string> emptyKinds = new List<string>();

            foreach (KeyValuePair<string, Dictionary<int, List<long>>> kindEntry in s_ViolationTimestamps)
            {
                List<int> emptyVehicles = new List<int>();
                foreach (KeyValuePair<int, List<long>> vehicleEntry in kindEntry.Value)
                {
                    TrimQueue(vehicleEntry.Value, cutoff);
                    if (vehicleEntry.Value.Count == 0)
                    {
                        emptyVehicles.Add(vehicleEntry.Key);
                    }
                }

                foreach (int vehicleId in emptyVehicles)
                {
                    kindEntry.Value.Remove(vehicleId);
                }

                if (kindEntry.Value.Count == 0)
                {
                    emptyKinds.Add(kindEntry.Key);
                }
            }

            foreach (string kind in emptyKinds)
            {
                s_ViolationTimestamps.Remove(kind);
            }
        }

        private static void RegisterViolationTimestamp(string kind, int vehicleId, long timestampMonthTicks)
        {
            if (!s_ViolationTimestamps.TryGetValue(kind, out Dictionary<int, List<long>> vehicleMap))
            {
                vehicleMap = new Dictionary<int, List<long>>();
                s_ViolationTimestamps[kind] = vehicleMap;
            }

            if (!vehicleMap.TryGetValue(vehicleId, out List<long> timestamps))
            {
                timestamps = new List<long>();
                vehicleMap[vehicleId] = timestamps;
            }

            timestamps.Add(timestampMonthTicks);
            PruneExpiredViolationTimestamps();
        }

        private static void TrimQueue(List<long> timestamps, long cutoffTicks)
        {
            int removeCount = 0;
            while (removeCount < timestamps.Count && timestamps[removeCount] < cutoffTicks)
            {
                removeCount += 1;
            }

            if (removeCount > 0)
            {
                timestamps.RemoveRange(0, removeCount);
            }
        }
    }
}
