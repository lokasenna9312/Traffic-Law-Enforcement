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
        private static readonly Dictionary<int, VehicleEnforcementRecord> s_VehicleRecords =
            new Dictionary<int, VehicleEnforcementRecord>();

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
                if (s_VehicleRecords.Count == 0)
                {
                    return "No fined vehicles yet.";
                }

                return string.Join(
                    "\n",
                    s_VehicleRecords
                        .OrderByDescending(pair => pair.Value.TotalFines)
                        .Take(10)
                        .Select(pair => $"vehicle {pair.Key}: {pair.Value.TotalFines}")
                        .ToArray());
            }
        }

        public static string VehicleViolationCountsText
        {
            get
            {
                if (s_VehicleRecords.Count == 0)
                {
                    return "No repeat offenders yet.";
                }

                return string.Join(
                    "\n",
                    s_VehicleRecords
                        .OrderByDescending(pair => pair.Value.TotalViolations)
                        .Take(10)
                        .Select(pair => $"vehicle {pair.Key}: {pair.Value.TotalViolations} violations")
                        .ToArray());
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
            VehicleEnforcementRecord vehicleRecord = GetOrCreateVehicleRecord(vehicleId);
            RegisterViolationTimestamp(vehicleRecord, kind, nowMonthTicks);

            EnforcementRecord record = new EnforcementRecord(kind, vehicleId, laneId, fineAmount, reason);

            while (s_RecentRecords.Count >= MaxRecords)
            {
                s_RecentRecords.RemoveAt(0);
            }

            s_RecentRecords.Add(record);
            TotalFineAmount += fineAmount;

            vehicleRecord.TotalViolations += 1;
            vehicleRecord.TotalFines += fineAmount;
            vehicleRecord.LastReason = reason ?? string.Empty;
            vehicleRecord.LastKind = kind ?? string.Empty;
            vehicleRecord.LastLaneId = laneId;
            vehicleRecord.LastFineAmount = fineAmount;
            vehicleRecord.LastTimestampMonthTicks = nowMonthTicks;
        }

        public static int GetVehicleViolationCount(int vehicleId)
        {
            return s_VehicleRecords.TryGetValue(vehicleId, out VehicleEnforcementRecord record)
                ? record.TotalViolations
                : 0;
        }

        public static int GetRecentViolationCount(string kind, int vehicleId, long windowMonthTicks, bool includeCurrentEvent)
        {
            if (!s_VehicleRecords.TryGetValue(vehicleId, out VehicleEnforcementRecord record))
            {
                return includeCurrentEvent ? 1 : 0;
            }

            List<long> timestamps = record.GetTimestampHistory(kind);
            if (timestamps == null || timestamps.Count == 0)
            {
                return includeCurrentEvent ? 1 : 0;
            }

            if (!EnforcementGameTime.IsInitialized)
            {
                return timestamps.Count + (includeCurrentEvent ? 1 : 0);
            }

            long cutoff = EnforcementGameTime.CurrentTimestampMonthTicks - windowMonthTicks;
            TrimQueue(timestamps, cutoff);
            return timestamps.Count + (includeCurrentEvent ? 1 : 0);
        }

        public static IReadOnlyDictionary<int, (int violationCount, int fineTotal)> GetVehiclePenaltySnapshot()
        {
            Dictionary<int, (int violationCount, int fineTotal)> snapshot =
                new Dictionary<int, (int violationCount, int fineTotal)>();

            foreach (KeyValuePair<int, VehicleEnforcementRecord> pair in s_VehicleRecords)
            {
                snapshot[pair.Key] = (pair.Value.TotalViolations, pair.Value.TotalFines);
            }

            return snapshot;
        }

        public static bool TryGetVehicleEnforcementRecord(int vehicleId, out VehicleEnforcementRecord record)
        {
            return s_VehicleRecords.TryGetValue(vehicleId, out record);
        }

        public static IReadOnlyDictionary<int, VehicleEnforcementRecord> GetVehicleRecordSnapshot()
        {
            Dictionary<int, VehicleEnforcementRecord> snapshot =
                new Dictionary<int, VehicleEnforcementRecord>(s_VehicleRecords.Count);

            foreach (KeyValuePair<int, VehicleEnforcementRecord> pair in s_VehicleRecords)
            {
                snapshot[pair.Key] = pair.Value.Clone();
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
            List<(string kind, int vehicleId, long timestampMonthTicks)> snapshot =
                new List<(string kind, int vehicleId, long timestampMonthTicks)>();

            foreach (KeyValuePair<int, VehicleEnforcementRecord> pair in s_VehicleRecords)
            {
                AppendTimestampSnapshot(snapshot, EnforcementKinds.PublicTransportLane, pair.Key, pair.Value.PublicTransportLaneTimestamps);
                AppendTimestampSnapshot(snapshot, EnforcementKinds.MidBlockCrossing, pair.Key, pair.Value.MidBlockCrossingTimestamps);
                AppendTimestampSnapshot(snapshot, EnforcementKinds.IntersectionMovement, pair.Key, pair.Value.IntersectionMovementTimestamps);
            }

            return snapshot;
        }

        public static void ResetPersistentData()
        {
            s_RecentEvents.Clear();
            s_RecentRecords.Clear();
            s_VehicleRecords.Clear();
            ActivePublicTransportLaneViolators = 0;
            PublicTransportLaneViolationCount = 0;
            MidBlockCrossingViolationCount = 0;
            IntersectionMovementViolationCount = 0;
            TotalFineAmount = 0;
        }

        public static void LoadPersistentData(
            TrafficLawEnforcementStatistics statistics,
            int totalFineAmount,
            IDictionary<int, VehicleEnforcementRecord> vehicleRecords,
            IEnumerable<EnforcementRecord> records)
        {
            ResetPersistentData();
            SetStatistics(statistics);
            TotalFineAmount = totalFineAmount;

            if (vehicleRecords != null)
            {
                foreach (KeyValuePair<int, VehicleEnforcementRecord> pair in vehicleRecords)
                {
                    s_VehicleRecords[pair.Key] = pair.Value?.Clone() ?? new VehicleEnforcementRecord();
                }
            }

            if (records != null)
            {
                foreach (EnforcementRecord record in records)
                {
                    if (s_RecentRecords.Count >= MaxRecords)
                    {
                        s_RecentRecords.RemoveAt(0);
                    }

                    s_RecentRecords.Add(record);
                }
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

            foreach (VehicleEnforcementRecord record in s_VehicleRecords.Values)
            {
                TrimQueue(record.PublicTransportLaneTimestamps, cutoff);
                TrimQueue(record.MidBlockCrossingTimestamps, cutoff);
                TrimQueue(record.IntersectionMovementTimestamps, cutoff);
            }
        }

        private static VehicleEnforcementRecord GetOrCreateVehicleRecord(int vehicleId)
        {
            if (!s_VehicleRecords.TryGetValue(vehicleId, out VehicleEnforcementRecord record))
            {
                record = new VehicleEnforcementRecord();
                s_VehicleRecords[vehicleId] = record;
            }

            return record;
        }

        private static void RegisterViolationTimestamp(VehicleEnforcementRecord record, string kind, long timestampMonthTicks)
        {
            List<long> timestamps = record.GetTimestampHistory(kind);
            if (timestamps == null)
            {
                return;
            }

            timestamps.Add(timestampMonthTicks);
        }

        private static void AppendTimestampSnapshot(
            List<(string kind, int vehicleId, long timestampMonthTicks)> snapshot,
            string kind,
            int vehicleId,
            List<long> timestamps)
        {
            if (timestamps == null || timestamps.Count == 0)
            {
                return;
            }

            foreach (long timestamp in timestamps)
            {
                snapshot.Add((kind, vehicleId, timestamp));
            }
        }

        private static void TrimQueue(List<long> timestamps, long cutoffTicks)
        {
            if (timestamps == null || timestamps.Count == 0)
            {
                return;
            }

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
