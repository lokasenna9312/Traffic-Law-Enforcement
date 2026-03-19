using System.Collections.Generic;
using Colossal.Serialization.Entities;
using Game;
using Game.Serialization;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public partial class EnforcementSaveDataSystem : GameSystemBase, IDefaultSerializable, ISerializable, IPreDeserialize, IPostDeserialize
    {
        private const int kSerializationVersion = 6;

        private EntityQuery m_StatisticsQuery;
        private EntityQuery m_PublicTransportLaneViolationQuery;
        private EntityQuery m_PublicTransportLanePermissionStateQuery;
        private EntityQuery m_PublicTransportLaneType3UsageStateQuery;
        private bool m_HasDeserializedData;
        private bool m_ShouldClearLegacyRuntimeState;
        private bool m_PendingPostDeserializeApply;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_StatisticsQuery = GetEntityQuery(ComponentType.ReadWrite<TrafficLawEnforcementStatistics>());
            m_PublicTransportLaneViolationQuery = GetEntityQuery(ComponentType.ReadOnly<PublicTransportLaneViolation>());
            m_PublicTransportLanePermissionStateQuery = GetEntityQuery(ComponentType.ReadOnly<PublicTransportLanePermissionState>());
            m_PublicTransportLaneType3UsageStateQuery = GetEntityQuery(ComponentType.ReadOnly<PublicTransportLaneType3UsageState>());
        }

        protected override void OnUpdate()
        {
            if (!m_PendingPostDeserializeApply)
            {
                return;
            }

            ApplyLoadedStateToWorld();
            m_PendingPostDeserializeApply = false;
        }

        public void SetDefaults(Context context)
        {
            ResetRuntimeState();
            EnforcementGameplaySettingsService.Apply(CreateInitialGameplaySettings(context));
            m_HasDeserializedData = false;
            m_ShouldClearLegacyRuntimeState = context.purpose == Purpose.LoadGame;
            m_PendingPostDeserializeApply = true;
        }

        public void PreDeserialize(Context context)
        {
            ResetRuntimeState();
            m_HasDeserializedData = false;
            m_ShouldClearLegacyRuntimeState = false;
            m_PendingPostDeserializeApply = false;
        }

        public void PostDeserialize(Context context)
        {
            m_PendingPostDeserializeApply = true;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(kSerializationVersion);
            WriteGameplaySettings(writer, EnforcementGameplaySettingsService.Current);

            TrafficLawEnforcementStatistics statistics = EnforcementTelemetry.GetStatisticsSnapshot();
            writer.Write(statistics.m_PublicTransportLaneViolationCount);
            writer.Write(statistics.m_ActivePublicTransportLaneViolatorCount);
            writer.Write(statistics.m_MidBlockCrossingViolationCount);
            writer.Write(statistics.m_IntersectionMovementViolationCount);
            writer.Write(EnforcementTelemetry.TotalFineAmount);

            IReadOnlyDictionary<int, (int violationCount, int fineTotal)> vehiclePenaltySnapshot = EnforcementTelemetry.GetVehiclePenaltySnapshot();
            writer.Write(vehiclePenaltySnapshot.Count);
            foreach (KeyValuePair<int, (int violationCount, int fineTotal)> pair in vehiclePenaltySnapshot)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value.violationCount);
                writer.Write(pair.Value.fineTotal);
            }

            IReadOnlyCollection<EnforcementRecord> recentRecords = EnforcementTelemetry.GetRecentRecordsSnapshot();
            writer.Write(recentRecords.Count);
            foreach (EnforcementRecord record in recentRecords)
            {
                writer.Write(record.Kind);
                writer.Write(record.VehicleId);
                writer.Write(record.LaneId);
                writer.Write(record.FineAmount);
                writer.Write(record.Reason);
            }

            IReadOnlyCollection<(string kind, int vehicleId, long timestampMonthTicks)> violationTimestamps = EnforcementTelemetry.GetViolationTimestampSnapshot();
            writer.Write(violationTimestamps.Count);
            foreach ((string kind, int vehicleId, long timestampMonthTicks) entry in violationTimestamps)
            {
                writer.Write(entry.kind);
                writer.Write(entry.vehicleId);
                writer.Write(entry.timestampMonthTicks);
            }

            IReadOnlyCollection<EnforcementBudgetUIService.FineIncomeEvent> fineIncomeEvents = EnforcementBudgetUIService.GetFineIncomeEventSnapshot();
            writer.Write(fineIncomeEvents.Count);
            foreach (EnforcementBudgetUIService.FineIncomeEvent entry in fineIncomeEvents)
            {
                writer.Write(entry.TimestampMonthTicks);
                writer.Write(entry.Amount);
                writer.Write(entry.Kind);
            }

            bool hasTrackingState = MonthlyEnforcementChirperService.TryGetTrackingState(out MonthlyEnforcementTrackingState trackingState);
            writer.Write(hasTrackingState);
            if (hasTrackingState)
            {
                writer.Write(trackingState.m_MonthIndex);
                writer.Write(trackingState.m_TotalPathRequestCount);
                writer.Write(trackingState.m_PublicTransportLaneCount);
                writer.Write(trackingState.m_MidBlockCrossingCount);
                writer.Write(trackingState.m_IntersectionMovementCount);
                writer.Write(trackingState.m_TotalFineAmount);
                writer.Write(trackingState.m_TotalAvoidedPathCount);
                writer.Write(trackingState.m_PublicTransportLaneFineAmount);
                writer.Write(trackingState.m_MidBlockCrossingFineAmount);
                writer.Write(trackingState.m_IntersectionMovementFineAmount);
                writer.Write(trackingState.m_PublicTransportLaneAvoidedEventCount);
                writer.Write(trackingState.m_MidBlockCrossingAvoidedEventCount);
                writer.Write(trackingState.m_IntersectionMovementAvoidedEventCount);
            }

            IReadOnlyCollection<MonthlyEnforcementReport> reports = MonthlyEnforcementChirperService.GetReportHistorySnapshot();
            writer.Write(reports.Count);
            foreach (MonthlyEnforcementReport report in reports)
            {
                writer.Write(report.m_MonthIndex);
                writer.Write(report.m_TotalPathRequestCount);
                writer.Write(report.m_PublicTransportLaneCount);
                writer.Write(report.m_MidBlockCrossingCount);
                writer.Write(report.m_IntersectionMovementCount);
                writer.Write(report.m_TotalFineAmount);
                writer.Write(report.m_TotalAvoidedPathCount);
                writer.Write(report.m_PublicTransportLaneFineAmount);
                writer.Write(report.m_MidBlockCrossingFineAmount);
                writer.Write(report.m_IntersectionMovementFineAmount);
                writer.Write(report.m_PublicTransportLaneAvoidedEventCount);
                writer.Write(report.m_MidBlockCrossingAvoidedEventCount);
                writer.Write(report.m_IntersectionMovementAvoidedEventCount);
            }

            bool hasPolicyImpactTrackingState = EnforcementPolicyImpactService.TryGetTrackingState(out EnforcementPolicyImpactTrackingState policyImpactTrackingState);
            writer.Write(hasPolicyImpactTrackingState);
            if (hasPolicyImpactTrackingState)
            {
                writer.Write(policyImpactTrackingState.m_MonthIndex);
                writer.Write(policyImpactTrackingState.m_TotalPathRequestCount);
                writer.Write(policyImpactTrackingState.m_TotalActualPathCount);
                writer.Write(policyImpactTrackingState.m_TotalAvoidedPathCount);
                writer.Write(policyImpactTrackingState.m_TotalFineAmount);
                writer.Write(policyImpactTrackingState.m_PublicTransportLaneActualCount);
                writer.Write(policyImpactTrackingState.m_MidBlockCrossingActualCount);
                writer.Write(policyImpactTrackingState.m_IntersectionMovementActualCount);
                writer.Write(policyImpactTrackingState.m_PublicTransportLaneFineAmount);
                writer.Write(policyImpactTrackingState.m_MidBlockCrossingFineAmount);
                writer.Write(policyImpactTrackingState.m_IntersectionMovementFineAmount);
                writer.Write(policyImpactTrackingState.m_PublicTransportLaneAvoidedEventCount);
                writer.Write(policyImpactTrackingState.m_MidBlockCrossingAvoidedEventCount);
                writer.Write(policyImpactTrackingState.m_IntersectionMovementAvoidedEventCount);
            }

            EnforcementPolicyImpactService.PersistentTotalsSnapshot totals = EnforcementPolicyImpactService.GetPersistentTotalsSnapshot();
            writer.Write(totals.TotalPathRequestCount);
            writer.Write(totals.TotalActualPathCount);
            writer.Write(totals.TotalAvoidedPathCount);
            writer.Write(totals.TotalFineAmount);
            writer.Write(totals.PublicTransportLaneActualCount);
            writer.Write(totals.MidBlockCrossingActualCount);
            writer.Write(totals.IntersectionMovementActualCount);
            writer.Write(totals.PublicTransportLaneFineAmount);
            writer.Write(totals.MidBlockCrossingFineAmount);
            writer.Write(totals.IntersectionMovementFineAmount);
            writer.Write(totals.PublicTransportLaneAvoidedEventCount);
            writer.Write(totals.MidBlockCrossingAvoidedEventCount);
            writer.Write(totals.IntersectionMovementAvoidedEventCount);

            IReadOnlyCollection<PathRequestEvent> pathRequestEvents = EnforcementPolicyImpactService.GetPathRequestEventSnapshot();
            writer.Write(pathRequestEvents.Count);
            foreach (PathRequestEvent entry in pathRequestEvents)
            {
                writer.Write(entry.TimestampMonthTicks);
            }

            IReadOnlyCollection<ActualViolationEvent> actualViolationEvents = EnforcementPolicyImpactService.GetActualViolationEventSnapshot();
            writer.Write(actualViolationEvents.Count);
            foreach (ActualViolationEvent entry in actualViolationEvents)
            {
                writer.Write(entry.TimestampMonthTicks);
                writer.Write(entry.Kind);
                writer.Write(entry.FineAmount);
            }

            IReadOnlyCollection<AvoidedRerouteEvent> avoidedRerouteEvents = EnforcementPolicyImpactService.GetAvoidedRerouteEventSnapshot();
            writer.Write(avoidedRerouteEvents.Count);
            foreach (AvoidedRerouteEvent entry in avoidedRerouteEvents)
            {
                writer.Write(entry.TimestampMonthTicks);
                writer.Write(entry.AvoidedPublicTransportLanePenalty);
                writer.Write(entry.AvoidedMidBlockPenalty);
                writer.Write(entry.AvoidedIntersectionPenalty);
            }
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int version);
            if (version != 3 && version != 4 && version != 5 && version != kSerializationVersion)
            {
                Mod.log.Info($"Unsupported enforcement save-data version {version}. Falling back to defaults.");
                return;
            }

            EnforcementGameplaySettingsState gameplaySettings = ReadGameplaySettings(reader, version);

            TrafficLawEnforcementStatistics statistics = default;
            reader.Read(out statistics.m_PublicTransportLaneViolationCount);
            reader.Read(out statistics.m_ActivePublicTransportLaneViolatorCount);
            reader.Read(out statistics.m_MidBlockCrossingViolationCount);
            reader.Read(out statistics.m_IntersectionMovementViolationCount);
            reader.Read(out int totalFineAmount);

            Dictionary<int, int> vehicleViolationCounts = new Dictionary<int, int>();
            Dictionary<int, int> vehicleFineTotals = new Dictionary<int, int>();
            reader.Read(out int vehiclePenaltyCount);
            for (int index = 0; index < vehiclePenaltyCount; index += 1)
            {
                reader.Read(out int vehicleId);
                reader.Read(out int violationCount);
                reader.Read(out int fineTotal);
                vehicleViolationCounts[vehicleId] = violationCount;
                vehicleFineTotals[vehicleId] = fineTotal;
            }

            List<EnforcementRecord> records = new List<EnforcementRecord>();
            reader.Read(out int recordCount);
            for (int index = 0; index < recordCount; index += 1)
            {
                reader.Read(out string kind);
                reader.Read(out int vehicleId);
                reader.Read(out int laneId);
                reader.Read(out int fineAmount);
                reader.Read(out string reason);
                records.Add(new EnforcementRecord(kind, vehicleId, laneId, fineAmount, reason));
            }

            List<(string kind, int vehicleId, long timestampMonthTicks)> timestamps = new List<(string kind, int vehicleId, long timestampMonthTicks)>();
            reader.Read(out int timestampCount);
            for (int index = 0; index < timestampCount; index += 1)
            {
                reader.Read(out string kind);
                reader.Read(out int vehicleId);
                reader.Read(out long timestampMonthTicks);
                timestamps.Add((kind, vehicleId, timestampMonthTicks));
            }

            List<EnforcementBudgetUIService.FineIncomeEvent> fineIncomeEvents = new List<EnforcementBudgetUIService.FineIncomeEvent>();
            reader.Read(out int fineIncomeCount);
            for (int index = 0; index < fineIncomeCount; index += 1)
            {
                reader.Read(out long timestampMonthTicks);
                reader.Read(out int amount);
                reader.Read(out string kind);
                fineIncomeEvents.Add(new EnforcementBudgetUIService.FineIncomeEvent(timestampMonthTicks, amount, kind));
            }

            bool migratedLegacyPathRequestTracking = version < 4;
            MonthlyEnforcementTrackingState? trackingState = null;
            reader.Read(out bool hasTrackingState);
            if (hasTrackingState)
            {
                reader.Read(out long monthIndex);
                int trackingTotalPathRequestCount = 0;
                if (version >= 4)
                {
                    reader.Read(out trackingTotalPathRequestCount);
                }
                reader.Read(out int publicTransportLaneCount);
                reader.Read(out int midBlockCrossingCount);
                reader.Read(out int intersectionMovementCount);
                reader.Read(out int trackingFineAmount);
                reader.Read(out int trackingTotalAvoidedPathCount);
                reader.Read(out int trackingPublicTransportLaneFineAmount);
                reader.Read(out int trackingMidBlockCrossingFineAmount);
                reader.Read(out int trackingIntersectionMovementFineAmount);
                reader.Read(out int trackingPublicTransportLaneAvoidedEventCount);
                reader.Read(out int trackingMidBlockCrossingAvoidedEventCount);
                reader.Read(out int trackingIntersectionMovementAvoidedEventCount);
                trackingState = new MonthlyEnforcementTrackingState(
                    monthIndex,
                    trackingTotalPathRequestCount,
                    publicTransportLaneCount,
                    midBlockCrossingCount,
                    intersectionMovementCount,
                    trackingFineAmount,
                    trackingTotalAvoidedPathCount,
                    trackingPublicTransportLaneFineAmount,
                    trackingMidBlockCrossingFineAmount,
                    trackingIntersectionMovementFineAmount,
                    trackingPublicTransportLaneAvoidedEventCount,
                    trackingMidBlockCrossingAvoidedEventCount,
                    trackingIntersectionMovementAvoidedEventCount);

                if (migratedLegacyPathRequestTracking || HasInconsistentPathRequestTracking(trackingState.Value.m_TotalPathRequestCount, trackingState.Value.m_PublicTransportLaneCount + trackingState.Value.m_MidBlockCrossingCount + trackingState.Value.m_IntersectionMovementCount, trackingState.Value.m_TotalAvoidedPathCount, trackingState.Value.m_TotalFineAmount))
                {
                    trackingState = null;
                }
            }

            List<MonthlyEnforcementReport> reports = new List<MonthlyEnforcementReport>();
            reader.Read(out int reportCount);
            for (int index = 0; index < reportCount; index += 1)
            {
                reader.Read(out long monthIndex);
                int reportTotalPathRequestCount = 0;
                if (version >= 4)
                {
                    reader.Read(out reportTotalPathRequestCount);
                }
                reader.Read(out int publicTransportLaneCount);
                reader.Read(out int midBlockCrossingCount);
                reader.Read(out int intersectionMovementCount);
                reader.Read(out int reportFineAmount);
                reader.Read(out int reportTotalAvoidedPathCount);
                reader.Read(out int reportPublicTransportLaneFineAmount);
                reader.Read(out int reportMidBlockCrossingFineAmount);
                reader.Read(out int reportIntersectionMovementFineAmount);
                reader.Read(out int reportPublicTransportLaneAvoidedEventCount);
                reader.Read(out int reportMidBlockCrossingAvoidedEventCount);
                reader.Read(out int reportIntersectionMovementAvoidedEventCount);
                reports.Add(new MonthlyEnforcementReport(
                    monthIndex,
                    reportTotalPathRequestCount,
                    publicTransportLaneCount,
                    midBlockCrossingCount,
                    intersectionMovementCount,
                    reportFineAmount,
                    reportTotalAvoidedPathCount,
                    reportPublicTransportLaneFineAmount,
                    reportMidBlockCrossingFineAmount,
                    reportIntersectionMovementFineAmount,
                    reportPublicTransportLaneAvoidedEventCount,
                    reportMidBlockCrossingAvoidedEventCount,
                    reportIntersectionMovementAvoidedEventCount));
            }

            EnforcementPolicyImpactTrackingState? policyImpactTrackingState = null;
            int totalPathRequestCount = 0;
            int totalActualPathCount = 0;
            int totalAvoidedPathCount = 0;
            int totalPolicyImpactFineAmount = 0;
            int publicTransportLaneActualCount = 0;
            int midBlockCrossingActualCount = 0;
            int intersectionMovementActualCount = 0;
            int publicTransportLaneFineAmount = 0;
            int midBlockCrossingFineAmount = 0;
            int intersectionMovementFineAmount = 0;
            int publicTransportLaneAvoidedEventCount = 0;
            int midBlockCrossingAvoidedEventCount = 0;
            int intersectionMovementAvoidedEventCount = 0;

            reader.Read(out bool hasPolicyImpactTrackingState);
            if (hasPolicyImpactTrackingState)
            {
                reader.Read(out long policyImpactMonthIndex);
                int trackingTotalPathRequestCount = 0;
                if (version >= 4)
                {
                    reader.Read(out trackingTotalPathRequestCount);
                }
                reader.Read(out int trackingTotalActualPathCount);
                reader.Read(out int trackingTotalAvoidedPathCount);
                reader.Read(out int trackingTotalFineAmount);
                reader.Read(out int trackingPublicTransportLaneActualCount);
                reader.Read(out int trackingMidBlockCrossingActualCount);
                reader.Read(out int trackingIntersectionMovementActualCount);
                reader.Read(out int trackingPublicTransportLaneFineAmount);
                reader.Read(out int trackingMidBlockCrossingFineAmount);
                reader.Read(out int trackingIntersectionMovementFineAmount);
                reader.Read(out int trackingPublicTransportLaneAvoidedEventCount);
                reader.Read(out int trackingMidBlockCrossingAvoidedEventCount);
                reader.Read(out int trackingIntersectionMovementAvoidedEventCount);
                policyImpactTrackingState = new EnforcementPolicyImpactTrackingState(
                    policyImpactMonthIndex,
                    trackingTotalPathRequestCount,
                    trackingTotalActualPathCount,
                    trackingTotalAvoidedPathCount,
                    trackingTotalFineAmount,
                    trackingPublicTransportLaneActualCount,
                    trackingMidBlockCrossingActualCount,
                    trackingIntersectionMovementActualCount,
                    trackingPublicTransportLaneFineAmount,
                    trackingMidBlockCrossingFineAmount,
                    trackingIntersectionMovementFineAmount,
                    trackingPublicTransportLaneAvoidedEventCount,
                    trackingMidBlockCrossingAvoidedEventCount,
                    trackingIntersectionMovementAvoidedEventCount);

                if (migratedLegacyPathRequestTracking || HasInconsistentPathRequestTracking(policyImpactTrackingState.Value.m_TotalPathRequestCount, policyImpactTrackingState.Value.m_TotalActualPathCount, policyImpactTrackingState.Value.m_TotalAvoidedPathCount, policyImpactTrackingState.Value.m_TotalFineAmount))
                {
                    policyImpactTrackingState = null;
                }
            }

            if (version >= 4)
            {
                reader.Read(out totalPathRequestCount);
            }
            reader.Read(out totalActualPathCount);
            reader.Read(out totalAvoidedPathCount);
            reader.Read(out totalPolicyImpactFineAmount);
            reader.Read(out publicTransportLaneActualCount);
            reader.Read(out midBlockCrossingActualCount);
            reader.Read(out intersectionMovementActualCount);
            reader.Read(out publicTransportLaneFineAmount);
            reader.Read(out midBlockCrossingFineAmount);
            reader.Read(out intersectionMovementFineAmount);
            reader.Read(out publicTransportLaneAvoidedEventCount);
            reader.Read(out midBlockCrossingAvoidedEventCount);
            reader.Read(out intersectionMovementAvoidedEventCount);

            List<PathRequestEvent> pathRequestEvents = new List<PathRequestEvent>();
            List<ActualViolationEvent> actualViolationEvents = new List<ActualViolationEvent>();
            List<AvoidedRerouteEvent> avoidedRerouteEvents = new List<AvoidedRerouteEvent>();
            if (version >= 5)
            {
                reader.Read(out int pathRequestEventCount);
                for (int index = 0; index < pathRequestEventCount; index += 1)
                {
                    reader.Read(out long timestampMonthTicks);
                    pathRequestEvents.Add(new PathRequestEvent(timestampMonthTicks));
                }

                reader.Read(out int actualViolationEventCount);
                for (int index = 0; index < actualViolationEventCount; index += 1)
                {
                    reader.Read(out long timestampMonthTicks);
                    reader.Read(out string kind);
                    reader.Read(out int fineAmount);
                    actualViolationEvents.Add(new ActualViolationEvent(timestampMonthTicks, kind, fineAmount));
                }

                reader.Read(out int avoidedRerouteEventCount);
                for (int index = 0; index < avoidedRerouteEventCount; index += 1)
                {
                    reader.Read(out long timestampMonthTicks);
                    reader.Read(out bool avoidedPublicTransportLanePenalty);
                    reader.Read(out bool avoidedMidBlockPenalty);
                    reader.Read(out bool avoidedIntersectionPenalty);
                    avoidedRerouteEvents.Add(new AvoidedRerouteEvent(timestampMonthTicks, avoidedPublicTransportLanePenalty, avoidedMidBlockPenalty, avoidedIntersectionPenalty));
                }
            }

            EnforcementGameplaySettingsService.Apply(gameplaySettings);
            EnforcementTelemetry.LoadPersistentData(statistics, totalFineAmount, vehicleFineTotals, vehicleViolationCounts, records, timestamps);
            EnforcementBudgetUIService.LoadPersistentData(fineIncomeEvents);
            MonthlyEnforcementChirperService.LoadPersistentData(trackingState, reports);
            EnforcementPolicyImpactService.LoadPersistentData(
                policyImpactTrackingState,
                totalPathRequestCount,
                totalActualPathCount,
                totalAvoidedPathCount,
                totalPolicyImpactFineAmount,
                publicTransportLaneActualCount,
                midBlockCrossingActualCount,
                intersectionMovementActualCount,
                publicTransportLaneFineAmount,
                midBlockCrossingFineAmount,
                intersectionMovementFineAmount,
                publicTransportLaneAvoidedEventCount,
                midBlockCrossingAvoidedEventCount,
                intersectionMovementAvoidedEventCount,
                pathRequestEvents,
                actualViolationEvents,
                avoidedRerouteEvents);

            m_HasDeserializedData = true;
            m_ShouldClearLegacyRuntimeState = false;
            m_PendingPostDeserializeApply = true;
        }

        private void ApplyLoadedStateToWorld()
        {
            Entity statisticsEntity = EnsureStatisticsEntity();
            EntityManager.SetComponentData(statisticsEntity, EnforcementTelemetry.GetStatisticsSnapshot());

            if (!m_PublicTransportLaneType3UsageStateQuery.IsEmptyIgnoreFilter)
            {
                EntityManager.RemoveComponent<PublicTransportLaneType3UsageState>(m_PublicTransportLaneType3UsageStateQuery);
            }

            if (!m_HasDeserializedData && m_ShouldClearLegacyRuntimeState)
            {
                if (!m_PublicTransportLaneViolationQuery.IsEmptyIgnoreFilter)
                {
                    EntityManager.RemoveComponent<PublicTransportLaneViolation>(m_PublicTransportLaneViolationQuery);
                }

                if (!m_PublicTransportLanePermissionStateQuery.IsEmptyIgnoreFilter)
                {
                    EntityManager.RemoveComponent<PublicTransportLanePermissionState>(m_PublicTransportLanePermissionStateQuery);
                }
            }
        }

        private Entity EnsureStatisticsEntity()
        {
            if (m_StatisticsQuery.IsEmptyIgnoreFilter)
            {
                Entity entity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(entity, default(TrafficLawEnforcementStatistics));
                return entity;
            }

            return m_StatisticsQuery.GetSingletonEntity();
        }

        private static EnforcementGameplaySettingsState CreateInitialGameplaySettings(Context context)
        {
            if (context.purpose == Purpose.NewGame || context.purpose == Purpose.LoadGame)
            {
                return Mod.Settings?.GetNewSaveDefaultSettings() ?? EnforcementGameplaySettingsState.CreateCodeDefaults();
            }

            return EnforcementGameplaySettingsState.CreateCodeDefaults();
        }

        private static void ResetRuntimeState()
        {
            EnforcementTelemetry.ResetPersistentData();
            MonthlyEnforcementChirperService.ResetPersistentData();
            EnforcementBudgetUIService.ResetPersistentData();
            EnforcementPolicyImpactService.ResetPersistentData();
            EnforcementFineMoneyService.ClearPendingCharges();
        }

        private static bool HasInconsistentPathRequestTracking(int totalPathRequestCount, int totalViolationCount, int totalAvoidedPathCount, int totalFineAmount)
        {
            return totalPathRequestCount <= 0 && (totalViolationCount > 0 || totalAvoidedPathCount > 0 || totalFineAmount > 0);
        }

        private static void WriteGameplaySettings<TWriter>(TWriter writer, EnforcementGameplaySettingsState state) where TWriter : IWriter
        {
            writer.Write(state.EnablePublicTransportLaneEnforcement);
            writer.Write(state.EnableMidBlockCrossingEnforcement);
            writer.Write(state.EnableIntersectionMovementEnforcement);
            writer.Write(state.AllowRoadPublicTransportVehicles);
            writer.Write(state.AllowTaxis);
            writer.Write(state.AllowPoliceCars);
            writer.Write(state.AllowFireEngines);
            writer.Write(state.AllowAmbulances);
            writer.Write(state.AllowGarbageTrucks);
            writer.Write(state.AllowPostVans);
            writer.Write(state.AllowRoadMaintenanceVehicles);
            writer.Write(state.AllowSnowplows);
            writer.Write(state.AllowVehicleMaintenanceVehicles);
            writer.Write(state.AllowPersonalCars);
            writer.Write(state.AllowDeliveryTrucks);
            writer.Write(state.AllowCargoTransportVehicles);
            writer.Write(state.AllowHearses);
            writer.Write(state.AllowPrisonerTransports);
            writer.Write(state.AllowParkMaintenanceVehicles);
            writer.Write(state.PublicTransportLaneExitPressureThresholdDays);
            writer.Write(state.PublicTransportLaneFineAmount);
            writer.Write(state.MidBlockCrossingFineAmount);
            writer.Write(state.IntersectionMovementFineAmount);
            writer.Write(state.EnablePublicTransportLaneRepeatPenalty);
            writer.Write(state.PublicTransportLaneRepeatWindowMonths);
            writer.Write(state.PublicTransportLaneRepeatThreshold);
            writer.Write(state.PublicTransportLaneRepeatMultiplierPercent);
            writer.Write(state.EnableMidBlockCrossingRepeatPenalty);
            writer.Write(state.MidBlockCrossingRepeatWindowMonths);
            writer.Write(state.MidBlockCrossingRepeatThreshold);
            writer.Write(state.MidBlockCrossingRepeatMultiplierPercent);
            writer.Write(state.EnableIntersectionMovementRepeatPenalty);
            writer.Write(state.IntersectionMovementRepeatWindowMonths);
            writer.Write(state.IntersectionMovementRepeatThreshold);
            writer.Write(state.IntersectionMovementRepeatMultiplierPercent);
        }

        private static EnforcementGameplaySettingsState ReadGameplaySettings<TReader>(TReader reader, int version) where TReader : IReader
        {
            EnforcementGameplaySettingsState state = default;
            if (version >= 6)
            {
                reader.Read(out state.EnablePublicTransportLaneEnforcement);
                reader.Read(out state.EnableMidBlockCrossingEnforcement);
                reader.Read(out state.EnableIntersectionMovementEnforcement);
            }
            else
            {
                reader.Read(out bool enableEnforcement);
                state.EnablePublicTransportLaneEnforcement = enableEnforcement;
                state.EnableMidBlockCrossingEnforcement = enableEnforcement;
                state.EnableIntersectionMovementEnforcement = enableEnforcement;
            }

            reader.Read(out state.AllowRoadPublicTransportVehicles);
            reader.Read(out state.AllowTaxis);
            reader.Read(out state.AllowPoliceCars);
            reader.Read(out state.AllowFireEngines);
            reader.Read(out state.AllowAmbulances);
            reader.Read(out state.AllowGarbageTrucks);
            reader.Read(out state.AllowPostVans);
            reader.Read(out state.AllowRoadMaintenanceVehicles);
            reader.Read(out state.AllowSnowplows);
            reader.Read(out state.AllowVehicleMaintenanceVehicles);
            reader.Read(out state.AllowPersonalCars);
            reader.Read(out state.AllowDeliveryTrucks);
            reader.Read(out state.AllowCargoTransportVehicles);
            reader.Read(out state.AllowHearses);
            reader.Read(out state.AllowPrisonerTransports);
            reader.Read(out state.AllowParkMaintenanceVehicles);
            reader.Read(out state.PublicTransportLaneExitPressureThresholdDays);
            reader.Read(out state.PublicTransportLaneFineAmount);
            reader.Read(out state.MidBlockCrossingFineAmount);
            reader.Read(out state.IntersectionMovementFineAmount);
            reader.Read(out state.EnablePublicTransportLaneRepeatPenalty);
            reader.Read(out state.PublicTransportLaneRepeatWindowMonths);
            reader.Read(out state.PublicTransportLaneRepeatThreshold);
            reader.Read(out state.PublicTransportLaneRepeatMultiplierPercent);
            reader.Read(out state.EnableMidBlockCrossingRepeatPenalty);
            reader.Read(out state.MidBlockCrossingRepeatWindowMonths);
            reader.Read(out state.MidBlockCrossingRepeatThreshold);
            reader.Read(out state.MidBlockCrossingRepeatMultiplierPercent);
            reader.Read(out state.EnableIntersectionMovementRepeatPenalty);
            reader.Read(out state.IntersectionMovementRepeatWindowMonths);
            reader.Read(out state.IntersectionMovementRepeatThreshold);
            reader.Read(out state.IntersectionMovementRepeatMultiplierPercent);
            return state;
        }
    }
}
