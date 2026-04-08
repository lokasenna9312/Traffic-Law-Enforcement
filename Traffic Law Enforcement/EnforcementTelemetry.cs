using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Traffic_Law_Enforcement
{
    public static class EnforcementTelemetry
    {
        private const int MaxEvents = 8;
        private const int MaxRecords = 24;

        private static readonly List<string> s_RecentEvents = new List<string>(MaxEvents);
        private static readonly List<EnforcementRecord> s_RecentRecords = new List<EnforcementRecord>(MaxRecords);
        private static readonly ReadOnlyCollection<EnforcementRecord> s_RecentRecordsView =
            s_RecentRecords.AsReadOnly();
        private static readonly Dictionary<int, VehicleEnforcementRecord> s_VehicleRecords =
            new Dictionary<int, VehicleEnforcementRecord>();
        private static readonly List<KeyValuePair<int, VehicleEnforcementRecord>> s_VehicleSummaryPairsBuffer =
            new List<KeyValuePair<int, VehicleEnforcementRecord>>();
        private static string s_RecentEventsText = "No enforcement events yet.";
        private static string s_RecentRecordsText = "No vehicle records yet.";
        private static string s_VehicleFineTotalsText = "No fined vehicles yet.";
        private static string s_VehicleViolationCountsText = "No repeat offenders yet.";
        private static bool s_RecentEventsTextDirty;
        private static bool s_RecentRecordsTextDirty;
        private static bool s_VehicleFineTotalsTextDirty;
        private static bool s_VehicleViolationCountsTextDirty;
        private static bool s_ViolationTimestampPruneDirty = true;
        private static long s_LastViolationTimestampPruneWindowMonthTicks = -1L;
        private static long s_NextViolationTimestampPruneMonthTicks = long.MaxValue;

        public static int ActivePublicTransportLaneViolators { get; private set; }
        public static int PublicTransportLaneViolationCount { get; private set; }
        public static int MidBlockCrossingViolationCount { get; private set; }
        public static int IntersectionMovementViolationCount { get; private set; }
        public static int TotalFineAmount { get; private set; }

        public static string RecentEventsText
        {
            get
            {
                if (s_RecentEventsTextDirty)
                {
                    s_RecentEventsText = BuildRecentEventsText();
                    s_RecentEventsTextDirty = false;
                }

                return s_RecentEventsText;
            }
        }

        public static string RecentRecordsText
        {
            get
            {
                if (s_RecentRecordsTextDirty)
                {
                    s_RecentRecordsText = BuildRecentRecordsText();
                    s_RecentRecordsTextDirty = false;
                }

                return s_RecentRecordsText;
            }
        }

        public static string VehicleFineTotalsText
        {
            get
            {
                if (s_VehicleFineTotalsTextDirty)
                {
                    s_VehicleFineTotalsText =
                        BuildVehicleSummaryText(
                            "No fined vehicles yet.",
                            static record => record.TotalFines,
                            static (vehicleId, value) => $"vehicle {vehicleId}: {value}");
                    s_VehicleFineTotalsTextDirty = false;
                }

                return s_VehicleFineTotalsText;
            }
        }

        public static string VehicleViolationCountsText
        {
            get
            {
                if (s_VehicleViolationCountsTextDirty)
                {
                    s_VehicleViolationCountsText =
                        BuildVehicleSummaryText(
                            "No repeat offenders yet.",
                            static record => record.TotalViolations,
                            static (vehicleId, value) => $"vehicle {vehicleId}: {value} violations");
                    s_VehicleViolationCountsTextDirty = false;
                }

                return s_VehicleViolationCountsText;
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
            s_RecentEventsTextDirty = true;
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
            s_RecentRecordsTextDirty = true;
            s_VehicleFineTotalsTextDirty = true;
            s_VehicleViolationCountsTextDirty = true;

            vehicleRecord.TotalViolations += 1;
            vehicleRecord.TotalFines += fineAmount;
            vehicleRecord.LastReason = reason ?? string.Empty;
            vehicleRecord.LastKind = kind ?? string.Empty;
            vehicleRecord.LastLaneId = laneId;
            vehicleRecord.LastFineAmount = fineAmount;
            vehicleRecord.LastTimestampMonthTicks = nowMonthTicks;
        }

        public static void RecordAppliedIllegalEgressMarker(
            int vehicleId,
            IllegalEgressApplyMode mode,
            int originLaneId,
            int roadLaneId)
        {
            if (vehicleId < 0 ||
                mode == IllegalEgressApplyMode.None ||
                originLaneId < 0 ||
                roadLaneId < 0)
            {
                return;
            }

            VehicleEnforcementRecord vehicleRecord = GetOrCreateVehicleRecord(vehicleId);
            vehicleRecord.LastAppliedIllegalEgressMode = mode;
            vehicleRecord.LastAppliedIllegalEgressTimestampMonthTicks =
                EnforcementGameTime.CurrentTimestampMonthTicks;
            vehicleRecord.LastAppliedIllegalEgressOriginLaneId = originLaneId;
            vehicleRecord.LastAppliedIllegalEgressRoadLaneId = roadLaneId;
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
            return s_RecentRecordsView;
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
            s_RecentEventsText = "No enforcement events yet.";
            s_RecentRecordsText = "No vehicle records yet.";
            s_VehicleFineTotalsText = "No fined vehicles yet.";
            s_VehicleViolationCountsText = "No repeat offenders yet.";
            s_RecentEventsTextDirty = false;
            s_RecentRecordsTextDirty = false;
            s_VehicleFineTotalsTextDirty = false;
            s_VehicleViolationCountsTextDirty = false;
            s_ViolationTimestampPruneDirty = true;
            s_LastViolationTimestampPruneWindowMonthTicks = -1L;
            s_NextViolationTimestampPruneMonthTicks = long.MaxValue;
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

                s_VehicleFineTotalsTextDirty = true;
                s_VehicleViolationCountsTextDirty = true;
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

                s_RecentRecordsTextDirty = true;
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
            long currentTimestampMonthTicks = EnforcementGameTime.CurrentTimestampMonthTicks;
            if (!s_ViolationTimestampPruneDirty &&
                s_LastViolationTimestampPruneWindowMonthTicks == maxWindowMonthTicks &&
                currentTimestampMonthTicks < s_NextViolationTimestampPruneMonthTicks)
            {
                return;
            }

            long cutoff = EnforcementGameTime.CurrentTimestampMonthTicks - maxWindowMonthTicks;
            long earliestRetainedTimestamp = long.MaxValue;

            foreach (VehicleEnforcementRecord record in s_VehicleRecords.Values)
            {
                TrimQueue(record.PublicTransportLaneTimestamps, cutoff);
                TrimQueue(record.MidBlockCrossingTimestamps, cutoff);
                TrimQueue(record.IntersectionMovementTimestamps, cutoff);

                UpdateEarliestTimestamp(
                    record.PublicTransportLaneTimestamps,
                    ref earliestRetainedTimestamp);
                UpdateEarliestTimestamp(
                    record.MidBlockCrossingTimestamps,
                    ref earliestRetainedTimestamp);
                UpdateEarliestTimestamp(
                    record.IntersectionMovementTimestamps,
                    ref earliestRetainedTimestamp);
            }

            s_LastViolationTimestampPruneWindowMonthTicks = maxWindowMonthTicks;
            s_NextViolationTimestampPruneMonthTicks =
                earliestRetainedTimestamp == long.MaxValue
                    ? long.MaxValue
                    : earliestRetainedTimestamp + maxWindowMonthTicks;
            s_ViolationTimestampPruneDirty = false;
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
            s_ViolationTimestampPruneDirty = true;
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

        private static void UpdateEarliestTimestamp(
            List<long> timestamps,
            ref long earliestTimestamp)
        {
            if (timestamps == null || timestamps.Count == 0)
            {
                return;
            }

            long timestamp = timestamps[0];
            if (timestamp < earliestTimestamp)
            {
                earliestTimestamp = timestamp;
            }
        }

        private static string BuildRecentEventsText()
        {
            if (s_RecentEvents.Count == 0)
            {
                return "No enforcement events yet.";
            }

            StringBuilder text = new StringBuilder(s_RecentEvents.Count * 48);
            for (int index = 0; index < s_RecentEvents.Count; index += 1)
            {
                if (index > 0)
                {
                    text.Append('\n');
                }

                text.Append(s_RecentEvents[index]);
            }

            return text.ToString();
        }

        private static string BuildRecentRecordsText()
        {
            if (s_RecentRecords.Count == 0)
            {
                return "No vehicle records yet.";
            }

            StringBuilder text = new StringBuilder(s_RecentRecords.Count * 72);
            for (int index = 0; index < s_RecentRecords.Count; index += 1)
            {
                if (index > 0)
                {
                    text.Append('\n');
                }

                text.Append(s_RecentRecords[index]);
            }

            return text.ToString();
        }

        private static string BuildVehicleSummaryText(
            string emptyText,
            System.Func<VehicleEnforcementRecord, int> selector,
            System.Func<int, int, string> formatter)
        {
            if (s_VehicleRecords.Count == 0)
            {
                return emptyText;
            }

            s_VehicleSummaryPairsBuffer.Clear();
            foreach (KeyValuePair<int, VehicleEnforcementRecord> pair in s_VehicleRecords)
            {
                s_VehicleSummaryPairsBuffer.Add(pair);
            }

            s_VehicleSummaryPairsBuffer.Sort((left, right) =>
            {
                int rightValue = selector(right.Value);
                int leftValue = selector(left.Value);
                int valueComparison = rightValue.CompareTo(leftValue);
                return valueComparison != 0
                    ? valueComparison
                    : left.Key.CompareTo(right.Key);
            });

            int count = System.Math.Min(10, s_VehicleSummaryPairsBuffer.Count);
            StringBuilder text = new StringBuilder(count * 24);
            for (int index = 0; index < count; index += 1)
            {
                KeyValuePair<int, VehicleEnforcementRecord> pair = s_VehicleSummaryPairsBuffer[index];
                if (index > 0)
                {
                    text.Append('\n');
                }

                text.Append(formatter(pair.Key, selector(pair.Value)));
            }

            string summary = text.ToString();
            s_VehicleSummaryPairsBuffer.Clear();
            return summary;
        }
    }
}
