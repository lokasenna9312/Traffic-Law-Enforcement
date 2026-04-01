using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Colossal.Localization;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Simulation;
using Game.Triggers;
using Game.UI.InGame;
using Game.UI.Localization;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Traffic_Law_Enforcement
{
    public partial class MonthlyEnforcementChirperSystem : GameSystemBase
    {
        private const string kDefaultLocale = "en-US";
        private const string kSenderLocalizationId = "TrafficLawEnforcement.MonthlyChirperSender";
        public const string kSenderTextLocaleId = "TrafficLawEnforcement.MonthlyChirper.Text.Sender";
        public const string kPeriodPointFormatLocaleId = "TrafficLawEnforcement.MonthlyChirper.Text.PeriodPointFormat";
        public const string kReportHeaderFormatLocaleId = "TrafficLawEnforcement.MonthlyChirper.Text.ReportHeaderFormat";
        public const string kTotalLineFormatLocaleId = "TrafficLawEnforcement.MonthlyChirper.Text.TotalLineFormat";
        public const string kPublicTransportLaneLineFormatLocaleId = "TrafficLawEnforcement.MonthlyChirper.Text.PublicTransportLaneLineFormat";
        public const string kMidBlockLineFormatLocaleId = "TrafficLawEnforcement.MonthlyChirper.Text.MidBlockLineFormat";
        public const string kIntersectionLineFormatLocaleId = "TrafficLawEnforcement.MonthlyChirper.Text.IntersectionLineFormat";
        public const string kNoRateLocaleId = "TrafficLawEnforcement.MonthlyChirper.Text.NoRate";
        private const string kPrefabParentName = nameof(Traffic_Law_Enforcement);
        private const string kPrefabNamePrefix = "TrafficLawEnforcement.MonthlyChirper";
        private const string kSenderIconPath = "Media/Game/Icons/TransportationOverview.svg";
        private readonly Dictionary<long, Entity> m_ReportTriggerEntities = new Dictionary<long, Entity>();
        private readonly Dictionary<string, Dictionary<string, string>> m_LocalizedEntriesByLocale = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MemorySource> m_LocalizedSourcesByLocale = new Dictionary<string, MemorySource>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<string, string>> m_StaticChirperTemplatesByLocale = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        private PrefabSystem m_PrefabSystem;
        private CreateChirpSystem m_CreateChirpSystem;
        private ChirperAccount m_SenderAccountPrefab;
        private InfoviewPrefab m_SenderInfoViewPrefab;
        private Entity m_SenderAccountEntity;
        private int m_ManualPreviewSequence;
        private TimeSystem m_TimeSystem;
        private EntityQuery m_ChirperAccountQuery;
        private EntityQuery m_InfoviewPrefabQuery;
        private long m_LastProcessedMonthIndex = long.MinValue;
        private int m_LastObservedRuntimeWorldGeneration = -1;
        private bool m_HasProcessedTrackingState;
        private bool m_LastProcessedEnforcementEnabled;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_CreateChirpSystem = World.GetOrCreateSystemManaged<CreateChirpSystem>();
            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            m_ChirperAccountQuery = GetEntityQuery(ComponentType.ReadOnly<ChirperAccountData>(), ComponentType.ReadOnly<PrefabData>());
            m_InfoviewPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<InfoviewData>(), ComponentType.ReadOnly<PrefabData>());

            CacheStaticChirperTemplates();
            EnsureBaseLocalizationSources();
            EnsureSenderAccount();

            bool rebuiltLocalization = false;
            bool addedLocalizationSource = false;
            bool requiresLocaleReload = false;
            int restoredReportCount = 0;

            foreach (MonthlyEnforcementReport report in MonthlyEnforcementChirperService.GetReportHistorySnapshot())
            {
                rebuiltLocalization |= EnsureReportLocalizationEntries(
                    report,
                    out bool reportAddedLocalizationSource,
                    out bool reportRequiresLocaleReload);
                addedLocalizationSource |= reportAddedLocalizationSource;
                requiresLocaleReload |= reportRequiresLocaleReload;
                restoredReportCount += 1;
            }

            if (requiresLocaleReload && (rebuiltLocalization || addedLocalizationSource))
            {
                ReloadActiveLocale();
            }

            if (EnforcementLoggingPolicy.ShouldLogChirperDiagnostics())
            {
                Mod.log.Info($"Monthly chirper system initialized. restoredReportLocalizations={restoredReportCount}, activeTriggers={m_ReportTriggerEntities.Count}");
            }
        }

        protected override void OnDestroy()
        {
            LocalizationManager localizationManager = GameManager.instance?.localizationManager;
            foreach (KeyValuePair<string, MemorySource> pair in m_LocalizedSourcesByLocale)
            {
                SafeRemoveSource(localizationManager, pair.Key, pair.Value);
            }

            m_ReportTriggerEntities.Clear();
            m_LocalizedEntriesByLocale.Clear();
            m_LocalizedSourcesByLocale.Clear();
            m_SenderAccountEntity = Entity.Null;
            m_SenderAccountPrefab = null;
            m_SenderInfoViewPrefab = null;

            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (!EnforcementGameTime.IsInitialized &&
                !EnforcementGameTime.TryUpdateFromTimeSystem(
                    m_TimeSystem,
                    logOnInitialization: true,
                    out string _))
            {
                return;
            }

            long currentTimestampMonthTicks = EnforcementGameTime.CurrentTimestampMonthTicks;
            long currentMonthIndex = EnforcementGameTime.GetMonthIndex(currentTimestampMonthTicks);
            int currentGeneration = EnforcementSaveDataSystem.RuntimeWorldGeneration;
            if (m_LastObservedRuntimeWorldGeneration != currentGeneration)
            {
                m_LastObservedRuntimeWorldGeneration = currentGeneration;
                m_LastProcessedMonthIndex = long.MinValue;
                m_HasProcessedTrackingState = false;
            }

            bool hasPendingManualPreviewRequests =
                MonthlyEnforcementChirperService.HasPendingManualPreviewRequests();

            if (!Mod.IsEnforcementEnabled)
            {
                if (!hasPendingManualPreviewRequests &&
                    m_HasProcessedTrackingState &&
                    !m_LastProcessedEnforcementEnabled &&
                    currentMonthIndex == m_LastProcessedMonthIndex)
                {
                    return;
                }

                if (hasPendingManualPreviewRequests)
                {
                    ClearPendingManualPreviewRequests();
                }

                if (MonthlyEnforcementChirperService.ResetTrackingToCurrentMonth(currentMonthIndex) &&
                    EnforcementLoggingPolicy.ShouldLogChirperDiagnostics())
                {
                    Mod.log.Info($"Monthly chirper tracking reset while enforcement disabled. month={currentMonthIndex}");
                }

                m_LastProcessedMonthIndex = currentMonthIndex;
                m_LastProcessedEnforcementEnabled = false;
                m_HasProcessedTrackingState = true;

                return;
            }

            if (m_HasProcessedTrackingState &&
                m_LastProcessedEnforcementEnabled &&
                currentMonthIndex == m_LastProcessedMonthIndex &&
                !hasPendingManualPreviewRequests)
            {
                return;
            }

            if (MonthlyEnforcementChirperService.EnsureTrackingInitialized(currentMonthIndex) &&
                EnforcementLoggingPolicy.ShouldLogChirperDiagnostics())
            {
                Mod.log.Info($"Monthly chirper tracking initialized. month={currentMonthIndex}");
            }

            if (EnforcementLoggingPolicy.ShouldLogChirperDiagnostics() &&
                MonthlyEnforcementChirperService.TryGetTrackingState(
                    out MonthlyEnforcementTrackingState trackingState) &&
                currentMonthIndex > trackingState.m_MonthIndex)
            {
                Mod.log.Info(
                    "[ENFORCEMENT_CHIRPER_STATE] " +
                    $"phase=BeforeAdvanceMonth, trackingMonth={trackingState.m_MonthIndex}, currentMonth={currentMonthIndex}, " +
                    $"monthTicks={currentTimestampMonthTicks}, totalActual={trackingState.m_TotalActualPathCount}, " +
                    $"totalAvoided={trackingState.m_TotalAvoidedPathCount}, totalDecision={trackingState.m_TotalActualOrAvoidedPathCount}");
            }

            if (MonthlyEnforcementChirperService.TryAdvanceMonth(currentMonthIndex, out MonthlyEnforcementReport completedReport))
            {
                PublishCompletedMonthReport(completedReport);
            }

            while (MonthlyEnforcementChirperService.TryConsumeManualPreviewRequest())
            {
                TryPublishCurrentPeriodPreview(currentTimestampMonthTicks, openPanel: true, out _);
            }

            m_LastProcessedMonthIndex = currentMonthIndex;
            m_LastProcessedEnforcementEnabled = true;
            m_HasProcessedTrackingState = true;
        }

        public bool TryPublishManualPreviewNow(out string failureReason)
        {
            failureReason = null;

            if (!EnforcementGameTime.IsInitialized &&
                !EnforcementGameTime.TryUpdateFromTimeSystem(m_TimeSystem, logOnInitialization: true, out failureReason))
            {
                Mod.log.Info($"Monthly chirper manual preview skipped. reason={failureReason}");
                return false;
            }

            if (!Mod.IsEnforcementEnabled)
            {
                failureReason = "enforcement is disabled";
                Mod.log.Info($"Monthly chirper manual preview skipped. reason={failureReason}");
                return false;
            }

            long currentTimestampMonthTicks = EnforcementGameTime.CurrentTimestampMonthTicks;
            long currentMonthIndex = EnforcementGameTime.GetMonthIndex(currentTimestampMonthTicks);
            MonthlyEnforcementChirperService.EnsureTrackingInitialized(currentMonthIndex);

            return TryPublishCurrentPeriodPreview(currentTimestampMonthTicks, openPanel: true, out failureReason);
        }

        private void CacheStaticChirperTemplates()
        {
            m_StaticChirperTemplatesByLocale.Clear();

            string localeDir = GetLocalizationDirectory();
            if (!Directory.Exists(localeDir))
            {
                Mod.log.Warn($"Monthly chirper localization directory not found: {localeDir}");
                return;
            }

            string[] files = Directory.GetFiles(localeDir, "*.properties");
            foreach (string filePath in files)
            {
                string localeId = Path.GetFileNameWithoutExtension(filePath);
                Dictionary<string, string> symbolicEntries = PropertiesLocaleSource.LoadKeyValueFile(filePath);
                Dictionary<string, string> resolvedTemplates = ResolveMonthlyChirperTemplates(symbolicEntries);

                if (resolvedTemplates.Count > 0)
                {
                    m_StaticChirperTemplatesByLocale[localeId] = resolvedTemplates;
                }
            }

            if (!m_StaticChirperTemplatesByLocale.ContainsKey(kDefaultLocale))
            {
                Mod.log.Warn($"Monthly chirper default locale templates not found: {kDefaultLocale}");
            }
        }
        private string GetStaticChirperTemplate(string localeId, string key)
        {
            localeId = NormalizeLocaleId(localeId);

            if (m_StaticChirperTemplatesByLocale.TryGetValue(localeId, out Dictionary<string, string> localizedTemplates) &&
                localizedTemplates.TryGetValue(key, out string localizedValue) &&
                !string.IsNullOrWhiteSpace(localizedValue))
            {
                return localizedValue;
            }

            if (m_StaticChirperTemplatesByLocale.TryGetValue(kDefaultLocale, out Dictionary<string, string> fallbackTemplates) &&
                fallbackTemplates.TryGetValue(key, out string fallbackValue) &&
                !string.IsNullOrWhiteSpace(fallbackValue))
            {
                return fallbackValue;
            }

            return key;
        }

        private static Dictionary<string, string> ResolveMonthlyChirperTemplates(Dictionary<string, string> symbolicEntries)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            AddTemplate(symbolicEntries, result, "MonthlyChirper.SenderText", kSenderTextLocaleId);
            AddTemplate(symbolicEntries, result, "MonthlyChirper.PeriodPointFormat", kPeriodPointFormatLocaleId);
            AddTemplate(symbolicEntries, result, "MonthlyChirper.ReportHeaderFormat", kReportHeaderFormatLocaleId);
            AddTemplate(symbolicEntries, result, "MonthlyChirper.TotalLineFormat", kTotalLineFormatLocaleId);
            AddTemplate(symbolicEntries, result, "MonthlyChirper.PublicTransportLaneLineFormat", kPublicTransportLaneLineFormatLocaleId);
            AddTemplate(symbolicEntries, result, "MonthlyChirper.MidBlockLineFormat", kMidBlockLineFormatLocaleId);
            AddTemplate(symbolicEntries, result, "MonthlyChirper.IntersectionLineFormat", kIntersectionLineFormatLocaleId);
            AddTemplate(symbolicEntries, result, "MonthlyChirper.NoRate", kNoRateLocaleId);

            return result;
        }

        private static void AddTemplate(
            Dictionary<string, string> symbolicEntries,
            Dictionary<string, string> resolvedEntries,
            string symbolicKey,
            string actualLocaleKey)
        {
            if (symbolicEntries.TryGetValue(symbolicKey, out string value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                resolvedEntries[actualLocaleKey] = value;
            }
        }

        private void EnsureSenderAccount()
        {
            if (m_SenderAccountEntity != Entity.Null && EntityManager.Exists(m_SenderAccountEntity))
            {
                return;
            }

            m_SenderAccountPrefab = ScriptableObject.CreateInstance<ChirperAccount>();
            m_SenderAccountPrefab.name = $"{kPrefabNamePrefix}.Sender";
            m_SenderAccountPrefab.m_InfoView = ResolveChirperSenderInfoviewPrefab();

            Game.Prefabs.Localization localization = m_SenderAccountPrefab.AddComponent<Game.Prefabs.Localization>();
            localization.m_LocalizationID = kSenderLocalizationId;

            m_PrefabSystem.AddPrefab(m_SenderAccountPrefab, kPrefabParentName, null, null);
            m_SenderAccountEntity = m_PrefabSystem.GetEntity(m_SenderAccountPrefab);
        }

        private InfoviewPrefab ResolveChirperSenderInfoviewPrefab()
        {
            if (m_SenderInfoViewPrefab != null)
            {
                return m_SenderInfoViewPrefab;
            }

            m_SenderInfoViewPrefab = CreateSenderInfoviewPrefab();
            return m_SenderInfoViewPrefab;
        }

        private void PublishCompletedMonthReport(MonthlyEnforcementReport report)
        {
            long publishStartTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            bool updatedLocalization = EnsureReportLocalizationEntries(
                report,
                out bool addedLocalizationSource,
                out bool requiresLocaleReload);
            double localizationMilliseconds =
                GetElapsedMilliseconds(publishStartTimestamp, System.Diagnostics.Stopwatch.GetTimestamp());

            long reloadStartTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            if (requiresLocaleReload && (addedLocalizationSource || updatedLocalization))
            {
                ReloadActiveLocale();
            }
            double reloadMilliseconds =
                GetElapsedMilliseconds(reloadStartTimestamp, System.Diagnostics.Stopwatch.GetTimestamp());

            long triggerStartTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            Entity triggerEntity = EnsureReportTriggerEntity(report.m_MonthIndex);
            double triggerMilliseconds =
                GetElapsedMilliseconds(triggerStartTimestamp, System.Diagnostics.Stopwatch.GetTimestamp());

            long periodStart = MonthlyEnforcementChirperService.GetReportPeriodStartMonthTicks(report);
            long periodEnd = MonthlyEnforcementChirperService.GetReportPeriodEndMonthTicks(report);

            long enqueueStartTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            if (EnqueueChirp(triggerEntity, out double queueWaitMilliseconds))
            {
                double enqueueMilliseconds =
                    GetElapsedMilliseconds(enqueueStartTimestamp, System.Diagnostics.Stopwatch.GetTimestamp());
                if (EnforcementLoggingPolicy.ShouldLogChirperDiagnostics())
                {
                    Mod.log.Info($"Monthly chirper month-end report enqueued. month={report.m_MonthIndex}, period={FormatEnglishPeriodPoint(periodStart)} -> {FormatEnglishPeriodPoint(periodEnd)}, total={report.TotalViolationCount}, bus={report.m_PublicTransportLaneCount}, mid={report.m_MidBlockCrossingCount}, intersection={report.m_IntersectionMovementCount}, fine={report.m_TotalFineAmount}");
                    Mod.log.Info(
                        "[ENFORCEMENT_CHIRPER_REPORT] " +
                        $"month={report.m_MonthIndex}, totalActual={report.m_TotalActualPathCount}, " +
                        $"totalAvoided={report.m_TotalAvoidedPathCount}, totalDecision={report.m_TotalActualOrAvoidedPathCount}, " +
                        $"ptActual={report.m_PublicTransportLaneCount}, ptAvoided={report.m_PublicTransportLaneAvoidedEventCount}, " +
                        $"ptDecision={report.m_PublicTransportLaneActualOrAvoidedPathCount}, " +
                        $"midActual={report.m_MidBlockCrossingCount}, midAvoided={report.m_MidBlockCrossingAvoidedEventCount}, " +
                        $"midDecision={report.m_MidBlockCrossingActualOrAvoidedPathCount}, " +
                        $"intersectionActual={report.m_IntersectionMovementCount}, " +
                        $"intersectionAvoided={report.m_IntersectionMovementAvoidedEventCount}, " +
                        $"intersectionDecision={report.m_IntersectionMovementActualOrAvoidedPathCount}, " +
                        $"fine={report.m_TotalFineAmount}");
                    Mod.log.Info(
                        "[ENFORCEMENT_CHIRPER_TIMING] " +
                        $"kind=monthEnd, month={report.m_MonthIndex}, localizationMs={localizationMilliseconds:0.000}, " +
                        $"reloadMs={reloadMilliseconds:0.000}, reloadRequired={requiresLocaleReload}, " +
                        $"triggerMs={triggerMilliseconds:0.000}, enqueueMs={enqueueMilliseconds:0.000}, " +
                        $"queueWaitMs={queueWaitMilliseconds:0.000}, totalPreEnqueueMs={GetElapsedMilliseconds(publishStartTimestamp, System.Diagnostics.Stopwatch.GetTimestamp()):0.000}");
                }
                LogRollingWindowSnapshotAtMonthBoundary(report);
            }
        }

        private static void LogRollingWindowSnapshotAtMonthBoundary(MonthlyEnforcementReport report)
        {
            if (!EnforcementLoggingPolicy.ShouldLogChirperDiagnostics() ||
                !EnforcementGameTime.IsInitialized)
            {
                return;
            }

            RollingWindowSnapshot snapshot =
                EnforcementPolicyImpactService.GetRollingWindowSnapshot();

            Mod.log.Info(
                "[ENFORCEMENT_MONTH_BOUNDARY] " +
                $"reportMonth={report.m_MonthIndex}, monthTicks={EnforcementGameTime.CurrentTimestampMonthTicks}, " +
                $"rollingRoutes={snapshot.TotalPathRequestCount}, rollingActual={snapshot.TotalActualPathCount}, " +
                $"rollingAvoided={snapshot.TotalAvoidedPathCount}, rollingDecision={snapshot.TotalActualOrAvoidedPathCount}, " +
                $"rollingFine={snapshot.TotalFineAmount}, ptDecision={snapshot.PublicTransportLaneActualOrAvoidedPathCount}, " +
                $"midDecision={snapshot.MidBlockCrossingActualOrAvoidedPathCount}, intersectionDecision={snapshot.IntersectionMovementActualOrAvoidedPathCount}");
        }

        private bool TryPublishCurrentPeriodPreview(long currentTimestampMonthTicks, bool openPanel, out string failureReason)
        {
            failureReason = null;

            try
            {
                MonthlyEnforcementReport previewReport = MonthlyEnforcementChirperService.BuildCurrentPeriodPreview();
                long periodStart = MonthlyEnforcementChirperService.GetCurrentPeriodStartMonthTicks(currentTimestampMonthTicks);
                long periodEnd = currentTimestampMonthTicks;

                int previewSequence = ++m_ManualPreviewSequence;
                bool updatedLocalization = EnsurePreviewLocalizationEntries(
                    previewReport,
                    periodStart,
                    periodEnd,
                    previewSequence,
                    out bool addedLocalizationSource,
                    out bool requiresLocaleReload);

                if (requiresLocaleReload &&
                    (addedLocalizationSource || (openPanel && updatedLocalization)))
                {
                    ReloadActiveLocale();
                }

                Entity triggerEntity = CreatePreviewTriggerEntity(periodEnd, previewSequence);

                if (EnqueueChirp(triggerEntity, out double _))
                {
                    _ = openPanel && TryOpenChirperPanel();
                    return true;
                }

                failureReason = "chirp enqueue failed";
                return false;
            }
            catch (Exception ex)
            {
                failureReason = $"preview exception: {ex.GetType().Name}: {ex.Message}";
                Mod.log.Error(ex, "Monthly chirper manual preview failed.");
                return false;
            }
        }

        private bool EnsureReportLocalizationEntries(
            MonthlyEnforcementReport report,
            out bool addedLocalizationSource,
            out bool requiresLocaleReload)
        {
            long periodStart = MonthlyEnforcementChirperService.GetReportPeriodStartMonthTicks(report);
            long periodEnd = MonthlyEnforcementChirperService.GetReportPeriodEndMonthTicks(report);
            string localizationId = GetReportLocalizationId(report.m_MonthIndex);

            return EnsureLocalizationEntriesForLocales(
                localizationId,
                report,
                periodStart,
                periodEnd,
                out addedLocalizationSource,
                out requiresLocaleReload);
        }

        private Entity EnsureReportTriggerEntity(long monthIndex)
        {
            EnsureSenderAccount();

            if (m_ReportTriggerEntities.TryGetValue(monthIndex, out Entity triggerEntity) &&
                triggerEntity != Entity.Null &&
                EntityManager.Exists(triggerEntity))
            {
                return triggerEntity;
            }

            string localizationId = GetReportLocalizationId(monthIndex);
            triggerEntity = CreateTriggerEntityForLocalizedChirp($"{kPrefabNamePrefix}.Report.{monthIndex}", localizationId);
            m_ReportTriggerEntities[monthIndex] = triggerEntity;
            return triggerEntity;
        }

        private bool EnsurePreviewLocalizationEntries(
            MonthlyEnforcementReport report,
            long periodStart,
            long periodEnd,
            int previewSequence,
            out bool addedLocalizationSource,
            out bool requiresLocaleReload)
        {
            string localizationId = GetPreviewLocalizationId(periodEnd, previewSequence);
            return EnsureLocalizationEntriesForLocales(
                localizationId,
                report,
                periodStart,
                periodEnd,
                out addedLocalizationSource,
                out requiresLocaleReload);
        }

        private Entity CreatePreviewTriggerEntity(long periodEnd, int previewSequence)
        {
            EnsureSenderAccount();

            string assetKey = $"{kPrefabNamePrefix}.Preview.{periodEnd}.{previewSequence}";
            string localizationId = GetPreviewLocalizationId(periodEnd, previewSequence);
            return CreateTriggerEntityForLocalizedChirp(assetKey, localizationId);
        }

        private Entity CreateTriggerEntityForLocalizedChirp(string assetKey, string localizationId)
        {
            ServiceChirpPrefab chirpPrefab = ScriptableObject.CreateInstance<ServiceChirpPrefab>();
            chirpPrefab.name = $"{assetKey}.Chirp";
            chirpPrefab.m_Account = m_SenderAccountPrefab;

            RandomLocalization randomLocalization = chirpPrefab.AddComponent<RandomLocalization>();
            randomLocalization.m_LocalizationID = localizationId;

            m_PrefabSystem.AddPrefab(chirpPrefab, kPrefabParentName, null, null);
            Entity chirpEntity = m_PrefabSystem.GetEntity(chirpPrefab);

            TriggerPrefab triggerPrefab = ScriptableObject.CreateInstance<TriggerPrefab>();
            triggerPrefab.name = $"{assetKey}.Trigger";
            triggerPrefab.m_TriggerPrefabs = Array.Empty<PrefabBase>();

            m_PrefabSystem.AddPrefab(triggerPrefab, kPrefabParentName, null, null);
            Entity triggerEntity = m_PrefabSystem.GetEntity(triggerPrefab);

            DynamicBuffer<TriggerChirpData> triggerBuffer = EntityManager.HasBuffer<TriggerChirpData>(triggerEntity)
                ? EntityManager.GetBuffer<TriggerChirpData>(triggerEntity)
                : EntityManager.AddBuffer<TriggerChirpData>(triggerEntity);

            triggerBuffer.Clear();
            triggerBuffer.Add(new TriggerChirpData
            {
                m_Chirp = chirpEntity
            });

            return triggerEntity;
        }

        private IEnumerable<string> GetLocalizationBuildLocales()
        {
            HashSet<string> localeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                kDefaultLocale
            };

            localeIds.Add(GetActiveLocaleId());
            return localeIds;
        }

        private bool EnsureLocalizationEntriesForLocales(
            string localizationId,
            MonthlyEnforcementReport report,
            long periodStart,
            long periodEnd,
            out bool addedLocalizationSource,
            out bool requiresLocaleReload)
        {
            bool changed = false;
            bool addedSource = false;
            bool shouldReload = false;

            foreach (string localeId in GetLocalizationBuildLocales())
            {
                changed |= EnsureSenderLocalizationEntry(localeId, ref addedSource, ref shouldReload);
                changed |= EnsureLocalizationEntryForLocale(
                    localeId,
                    localizationId,
                    BuildLocalizedMessage(localeId, report, periodStart, periodEnd),
                    ref addedSource,
                    ref shouldReload);
            }

            addedLocalizationSource = addedSource;
            requiresLocaleReload = shouldReload;
            return changed;
        }

        private bool EnsureLocalizationEntryForLocale(
            string localeId,
            string localizationId,
            string localizedMessage,
            ref bool addedLocalizationSource,
            ref bool requiresLocaleReload)
        {
            localeId = NormalizeLocaleId(localeId);
            string indexedLocalizationId = LocalizationUtils.AppendIndex(localizationId, new RandomLocalizationIndex(0));
            Dictionary<string, string> entries = EnsureLocaleEntries(localeId, ref addedLocalizationSource);

            if (!entries.TryGetValue(indexedLocalizationId, out string currentLocalizedMessage) || currentLocalizedMessage != localizedMessage)
            {
                entries[indexedLocalizationId] = localizedMessage;
                if (!TrySynchronizeActiveLocaleEntry(
                        localeId,
                        localizationId,
                        indexedLocalizationId,
                        localizedMessage))
                {
                    requiresLocaleReload = true;
                }

                return true;
            }

            return false;
        }

        private bool EnsureSenderLocalizationEntry(
            string localeId,
            ref bool addedLocalizationSource,
            ref bool requiresLocaleReload)
        {
            localeId = NormalizeLocaleId(localeId);
            Dictionary<string, string> entries = EnsureLocaleEntries(localeId, ref addedLocalizationSource);
            string senderText = GetStaticChirperTemplate(localeId, kSenderTextLocaleId);

            if (entries.TryGetValue(kSenderLocalizationId, out string currentSenderText) &&
                currentSenderText == senderText)
            {
                return false;
            }

            entries[kSenderLocalizationId] = senderText;
            if (!TrySynchronizeActiveLocaleEntry(
                    localeId,
                    kSenderLocalizationId,
                    kSenderLocalizationId,
                    senderText))
            {
                requiresLocaleReload = true;
            }

            return true;
        }

        private bool TrySynchronizeActiveLocaleEntry(
            string localeId,
            string baseLocalizationId,
            string entryLocalizationId,
            string localizedMessage)
        {
            if (!string.Equals(NormalizeLocaleId(localeId), GetActiveLocaleId(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                if (GameManager.instance?.localizationManager?.activeDictionary is LocalizationDictionary dictionary)
                {
                    dictionary.Add(entryLocalizationId, localizedMessage);
                    if (!string.IsNullOrWhiteSpace(baseLocalizationId) &&
                        !string.Equals(baseLocalizationId, entryLocalizationId, StringComparison.Ordinal))
                    {
                        dictionary.indexCounts[baseLocalizationId] = 1;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Mod.log.Info($"Monthly chirper active locale sync failed: {ex.GetType().Name}: {ex.Message}");
            }

            return false;
        }

        private Dictionary<string, string> EnsureLocaleEntries(
            string localeId,
            ref bool addedLocalizationSource)
        {
            localeId = NormalizeLocaleId(localeId);
            if (m_LocalizedEntriesByLocale.TryGetValue(localeId, out Dictionary<string, string> existingEntries))
            {
                return existingEntries;
            }

            Dictionary<string, string> entries = new Dictionary<string, string>();
            MemorySource source = new MemorySource(entries);
            m_LocalizedEntriesByLocale[localeId] = entries;
            m_LocalizedSourcesByLocale[localeId] = source;
            GameManager.instance?.localizationManager?.AddSource(localeId, source);
            addedLocalizationSource = true;
            return entries;
        }

        private void EnsureBaseLocalizationSources()
        {
            bool addedLocalizationSource = false;
            foreach (string localeId in GetLocalizationBuildLocales())
            {
                bool requiresLocaleReload = false;
                _ = EnsureSenderLocalizationEntry(localeId, ref addedLocalizationSource, ref requiresLocaleReload);
            }
        }

        private bool EnqueueChirp(Entity triggerEntity, out double queueWaitMilliseconds)
        {
            queueWaitMilliseconds = 0d;
            if (triggerEntity == Entity.Null || !EntityManager.Exists(triggerEntity))
            {
                Mod.log.Info("Monthly chirper enqueue skipped because trigger entity is unavailable.");
                return false;
            }

            JobHandle dependency;
            NativeQueue<ChirpCreationData> queue = m_CreateChirpSystem.GetQueue(out dependency);
            long dependencyStartTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            dependency.Complete();
            queueWaitMilliseconds =
                GetElapsedMilliseconds(dependencyStartTimestamp, System.Diagnostics.Stopwatch.GetTimestamp());

            queue.Enqueue(new ChirpCreationData
            {
                m_TriggerPrefab = triggerEntity,
                m_Sender = m_SenderAccountEntity,
                m_Target = Entity.Null
            });

            m_CreateChirpSystem.AddQueueWriter(default);

            return true;
        }

        private static double GetElapsedMilliseconds(long startTimestamp, long endTimestamp)
        {
            return (endTimestamp - startTimestamp) * 1000d / System.Diagnostics.Stopwatch.Frequency;
        }

        private bool TryOpenChirperPanel()
        {
            try
            {
                GamePanelUISystem gamePanelSystem = World.GetOrCreateSystemManaged<GamePanelUISystem>();
                if (gamePanelSystem == null)
                {
                    Mod.log.Info("Monthly chirper panel open skipped because GamePanelUISystem is unavailable.");
                    return false;
                }

                gamePanelSystem.ShowPanel(new ChirperPanel());
                return true;
            }
            catch (Exception ex)
            {
                Mod.log.Info($"Monthly chirper panel open failed: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        private void ClearPendingManualPreviewRequests()
        {
            while (MonthlyEnforcementChirperService.HasPendingManualPreviewRequests() &&
                MonthlyEnforcementChirperService.TryConsumeManualPreviewRequest())
            {
            }
        }

        private InfoviewPrefab ResolveFallbackInfoviewPrefab()
        {
            EntityTypeHandle entityTypeHandle = GetEntityTypeHandle();

            using (NativeArray<ArchetypeChunk> chirperAccountChunks = m_ChirperAccountQuery.ToArchetypeChunkArray(Allocator.Temp))
            {
                for (int chunkIndex = 0; chunkIndex < chirperAccountChunks.Length; chunkIndex += 1)
                {
                    NativeArray<Entity> chirperAccounts =
                        chirperAccountChunks[chunkIndex].GetNativeArray(entityTypeHandle);
                    for (int index = 0; index < chirperAccounts.Length; index += 1)
                    {
                        if (m_PrefabSystem.TryGetPrefab(chirperAccounts[index], out ChirperAccount chirperAccount) &&
                            chirperAccount?.m_InfoView != null)
                        {
                            return chirperAccount.m_InfoView;
                        }
                    }
                }
            }

            using (NativeArray<ArchetypeChunk> infoviewPrefabChunks = m_InfoviewPrefabQuery.ToArchetypeChunkArray(Allocator.Temp))
            {
                for (int chunkIndex = 0; chunkIndex < infoviewPrefabChunks.Length; chunkIndex += 1)
                {
                    NativeArray<Entity> infoviewPrefabs =
                        infoviewPrefabChunks[chunkIndex].GetNativeArray(entityTypeHandle);
                    for (int index = 0; index < infoviewPrefabs.Length; index += 1)
                    {
                        if (m_PrefabSystem.TryGetPrefab(infoviewPrefabs[index], out InfoviewPrefab infoviewPrefab))
                        {
                            return infoviewPrefab;
                        }
                    }
                }
            }

            return null;
        }

        private InfoviewPrefab CreateSenderInfoviewPrefab()
        {
            InfoviewPrefab fallback = ResolveFallbackInfoviewPrefab();
            InfoviewPrefab senderInfoviewPrefab = ScriptableObject.CreateInstance<InfoviewPrefab>();
            senderInfoviewPrefab.name = $"{kPrefabNamePrefix}.SenderInfoView";
            senderInfoviewPrefab.m_IconPath = kSenderIconPath;

            if (fallback != null)
            {
                senderInfoviewPrefab.m_Infomodes = fallback.m_Infomodes;
                senderInfoviewPrefab.m_DefaultColor = fallback.m_DefaultColor;
                senderInfoviewPrefab.m_SecondaryColor = fallback.m_SecondaryColor;
                senderInfoviewPrefab.m_Priority = fallback.m_Priority;
                senderInfoviewPrefab.m_Group = fallback.m_Group;
                senderInfoviewPrefab.m_WarningCategories = fallback.m_WarningCategories;
                senderInfoviewPrefab.m_EnableNotificationIcon = fallback.m_EnableNotificationIcon;
                senderInfoviewPrefab.m_Editor = fallback.m_Editor;
            }

            m_PrefabSystem.AddPrefab(senderInfoviewPrefab, kPrefabParentName, null, null);
            return senderInfoviewPrefab;
        }

        private void ReloadActiveLocale()
        {
            try
            {
                GameManager.instance?.localizationManager?.ReloadActiveLocale();
            }
            catch (Exception ex)
            {
                Mod.log.Info($"Monthly chirper locale reload failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void SafeRemoveSource(LocalizationManager localizationManager, string localeId, MemorySource source)
        {
            if (localizationManager == null || source == null)
            {
                return;
            }

            localeId = NormalizeLocaleId(localeId);

            try
            {
                localizationManager.RemoveSource(localeId, source);
            }
            catch (NullReferenceException)
            {
                // During world teardown the localization manager can already be partially disposed.
            }
            catch (Exception ex)
            {
                Mod.log.Info($"Monthly chirper localization cleanup ignored exception for {localeId}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static string GetReportLocalizationId(long monthIndex)
        {
            return $"TrafficLawEnforcement.MonthlyChirperReport_{monthIndex}";
        }

        private static string GetPreviewLocalizationId(long periodEndMonthTicks, int previewSequence)
        {
            return $"TrafficLawEnforcement.MonthlyChirperPreview_{periodEndMonthTicks}_{previewSequence}";
        }

        private static string FormatEnglishPeriodPoint(long monthTicks)
        {
            GetPeriodParts(monthTicks, out int year, out int month, out int hour, out int minute);
            string monthName = CultureInfo.GetCultureInfo(kDefaultLocale).DateTimeFormat.GetMonthName(month);
            return $"{monthName} {year} {hour:00}:{minute:00}";
        }

        private string BuildLocalizedStatisticsLine(
            string localeId,
            string lineFormatLocaleId,
            int finedViolationCount,
            int totalPathRequestCount,
            int actualOrAvoidedPathCount,
            int fineAmount)
        {
            return FormatLocalizedTextForLocale(
                localeId,
                lineFormatLocaleId,
                FormatViolationRate(localeId, finedViolationCount, totalPathRequestCount),
                FormatSuppressionFailureRate(localeId, finedViolationCount, actualOrAvoidedPathCount),
                FormatMoney(localeId, fineAmount));
        }

        private string BuildLocalizedMessage(string localeId, MonthlyEnforcementReport report, long periodStartMonthTicks, long periodEndMonthTicks)
        {
            StringBuilder message = new StringBuilder(320);
            message.Append(
                FormatLocalizedTextForLocale(
                    localeId,
                    kReportHeaderFormatLocaleId,
                    FormatPeriodPoint(localeId, periodStartMonthTicks),
                    FormatPeriodPoint(localeId, periodEndMonthTicks),
                    report.TotalViolationCount));
            message.Append('\n');
            message.Append(
                BuildLocalizedStatisticsLine(
                    localeId,
                    kTotalLineFormatLocaleId,
                    report.m_TotalActualPathCount,
                    report.m_TotalPathRequestCount,
                    report.m_TotalActualOrAvoidedPathCount,
                    report.m_TotalFineAmount));
            message.Append('\n');
            message.Append(
                BuildLocalizedStatisticsLine(
                    localeId,
                    kPublicTransportLaneLineFormatLocaleId,
                    report.m_PublicTransportLaneCount,
                    report.m_TotalPathRequestCount,
                    report.m_PublicTransportLaneActualOrAvoidedPathCount,
                    report.m_PublicTransportLaneFineAmount));
            message.Append('\n');
            message.Append(
                BuildLocalizedStatisticsLine(
                    localeId,
                    kMidBlockLineFormatLocaleId,
                    report.m_MidBlockCrossingCount,
                    report.m_TotalPathRequestCount,
                    report.m_MidBlockCrossingActualOrAvoidedPathCount,
                    report.m_MidBlockCrossingFineAmount));
            message.Append('\n');
            message.Append(
                BuildLocalizedStatisticsLine(
                    localeId,
                    kIntersectionLineFormatLocaleId,
                    report.m_IntersectionMovementCount,
                    report.m_TotalPathRequestCount,
                    report.m_IntersectionMovementActualOrAvoidedPathCount,
                    report.m_IntersectionMovementFineAmount));
            return message.ToString();
        }

        private string FormatPeriodPoint(string localeId, long monthTicks)
        {
            GetPeriodParts(monthTicks, out int year, out int month, out int hour, out int minute);
            CultureInfo culture = GetCultureForLocale(localeId);
            string monthText = culture.DateTimeFormat.GetMonthName(month);
            if (string.IsNullOrWhiteSpace(monthText))
            {
                monthText = month.ToString(CultureInfo.InvariantCulture);
            }

            return FormatLocalizedTextForLocale(localeId, kPeriodPointFormatLocaleId, monthText, year, hour, minute);
        }

        private static string FormatViolationRate(string localeId, int finedViolationCount, int denominator)
        {
            CultureInfo culture = GetCultureForLocale(localeId);
            if (denominator <= 0)
            {
                return 0d.ToString("0.0", culture) + "%";
            }

            return (100d * finedViolationCount / denominator).ToString("0.0", culture) + "%";
        }

        private static string FormatSuppressionFailureRate(string localeId, int finedViolationCount, int actualOrAvoidedPathCount)
        {
            CultureInfo culture = GetCultureForLocale(localeId);
            int denominator = actualOrAvoidedPathCount;
            if (denominator <= 0)
            {
                return 0d.ToString("0.0", culture) + "%";
            }

            return (100d * finedViolationCount / denominator).ToString("0.0", culture) + "%";
        }

        private static string FormatMoney(string localeId, int amount)
        {
            return amount.ToString("N0", GetCultureForLocale(localeId));
        }

        private static string GetActiveLocaleId()
        {
            return NormalizeLocaleId(GameManager.instance?.localizationManager?.activeLocaleId);
        }

        private static string NormalizeLocaleId(string localeId)
        {
            return string.IsNullOrWhiteSpace(localeId) ? kDefaultLocale : localeId;
        }

        private static CultureInfo GetCultureForLocale(string localeId)
        {
            try
            {
                return CultureInfo.GetCultureInfo(NormalizeLocaleId(localeId));
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.GetCultureInfo(kDefaultLocale);
            }
        }

        private string FormatLocalizedTextForLocale(string localeId, string localeKey, params object[] args)
        {
            string format = GetStaticChirperTemplate(localeId, localeKey);
            return string.Format(GetCultureForLocale(localeId), format, args);
        }

        private static void GetPeriodParts(long monthTicks, out int year, out int month, out int hour, out int minute)
        {
            double totalMonths = Math.Max(0d, monthTicks / (double)EnforcementGameTime.CurrentMonthTicksPerMonth);
            long wholeMonths = (long)Math.Floor(totalMonths);
            double fractionalMonth = totalMonths - wholeMonths;
            int totalMinutes = (int)Math.Floor((fractionalMonth * 1440d) + 0.000001d);

            if (totalMinutes >= 1440)
            {
                wholeMonths += totalMinutes / 1440;
                totalMinutes %= 1440;
            }

            year = (int)(wholeMonths / EnforcementGameTime.MonthsPerYear) + 1;
            month = (int)(wholeMonths % EnforcementGameTime.MonthsPerYear) + 1;
            hour = totalMinutes / 60;
            minute = totalMinutes % 60;
        }

        private static string GetLocalizationDirectory()
        {
            if (!string.IsNullOrWhiteSpace(Mod.LocalizationDirectory))
            {
                return Mod.LocalizationDirectory;
            }

            return Path.Combine(AppContext.BaseDirectory, "Mods", "Traffic Law Enforcement", "Localization");
        }
    }
}

