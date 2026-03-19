using System;
using System.Collections.Generic;
using System.Globalization;
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

        private PrefabSystem m_PrefabSystem;
        private CreateChirpSystem m_CreateChirpSystem;
        private ChirperAccount m_SenderAccountPrefab;
        private InfoviewPrefab m_SenderInfoViewPrefab;
        private Entity m_SenderAccountEntity;
        private int m_ManualPreviewSequence;
        private TimeSystem m_TimeSystem;
        private EntityQuery m_ChirperAccountQuery;
        private EntityQuery m_InfoviewPrefabQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_CreateChirpSystem = World.GetOrCreateSystemManaged<CreateChirpSystem>();
            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            m_ChirperAccountQuery = GetEntityQuery(ComponentType.ReadOnly<ChirperAccountData>(), ComponentType.ReadOnly<PrefabData>());
            m_InfoviewPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<InfoviewData>(), ComponentType.ReadOnly<PrefabData>());

            RegisterLocalizationSources();
            EnsureSenderAccount();

            bool rebuiltLocalization = false;
            foreach (MonthlyEnforcementReport report in MonthlyEnforcementChirperService.GetReportHistorySnapshot())
            {
                rebuiltLocalization |= EnsureReportAssets(report, out _);
            }

            if (rebuiltLocalization)
            {
                ReloadActiveLocale();
            }

            Mod.log.Info($"Monthly chirper system initialized. restoredReports={m_ReportTriggerEntities.Count}");
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
            if (!EnforcementGameTime.TryUpdateFromTimeSystem(m_TimeSystem, logOnInitialization: true, out _))
            {
                return;
            }

            long currentTimestampMonthTicks = EnforcementGameTime.CurrentTimestampMonthTicks;
            long currentMonthIndex = EnforcementGameTime.GetMonthIndex(currentTimestampMonthTicks);

            if (!Mod.IsEnforcementEnabled)
            {
                ClearPendingManualPreviewRequests();

                if (MonthlyEnforcementChirperService.ResetTrackingToCurrentMonth(currentMonthIndex))
                {
                    Mod.log.Info($"Monthly chirper tracking reset while enforcement disabled. month={currentMonthIndex}");
                }

                return;
            }

            if (MonthlyEnforcementChirperService.EnsureTrackingInitialized(currentMonthIndex))
            {
                Mod.log.Info($"Monthly chirper tracking initialized. month={currentMonthIndex}");
            }

            if (MonthlyEnforcementChirperService.TryAdvanceMonth(currentMonthIndex, out MonthlyEnforcementReport completedReport))
            {
                PublishCompletedMonthReport(completedReport);
            }

            while (MonthlyEnforcementChirperService.TryConsumeManualPreviewRequest())
            {
                TryPublishCurrentPeriodPreview(currentTimestampMonthTicks, openPanel: false, out _);
            }
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
            if (MonthlyEnforcementChirperService.EnsureTrackingInitialized(currentMonthIndex))
            {
                Mod.log.Info($"Monthly chirper tracking initialized from manual preview. month={currentMonthIndex}");
            }

            return TryPublishCurrentPeriodPreview(currentTimestampMonthTicks, openPanel: false, out failureReason);
        }

        private void RegisterLocalizationSources()
        {
            LocalizationManager localizationManager = GameManager.instance?.localizationManager;
            if (localizationManager == null)
            {
                Mod.log.Info("Monthly chirper localization manager unavailable during initialization.");
                return;
            }

            EnsureSenderLocalizationEntry(GetActiveLocaleId());

            ReloadActiveLocale();
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
            bool updatedLocalization = EnsureReportAssets(report, out Entity triggerEntity);
            if (updatedLocalization)
            {
                ReloadActiveLocale();
            }

            long periodStart = MonthlyEnforcementChirperService.GetReportPeriodStartMonthTicks(report);
            long periodEnd = MonthlyEnforcementChirperService.GetReportPeriodEndMonthTicks(report);

            if (EnqueueChirp(triggerEntity))
            {
                Mod.log.Info($"Monthly chirper month-end report enqueued. month={report.m_MonthIndex}, period={FormatEnglishPeriodPoint(periodStart)} -> {FormatEnglishPeriodPoint(periodEnd)}, total={report.TotalViolationCount}, bus={report.m_PublicTransportLaneCount}, mid={report.m_MidBlockCrossingCount}, intersection={report.m_IntersectionMovementCount}, fine={report.m_TotalFineAmount}");
            }
        }

        private bool TryPublishCurrentPeriodPreview(long currentTimestampMonthTicks, bool openPanel, out string failureReason)
        {
            failureReason = null;

            MonthlyEnforcementReport previewReport = MonthlyEnforcementChirperService.BuildCurrentPeriodPreview();
            long periodStart = MonthlyEnforcementChirperService.GetCurrentPeriodStartMonthTicks(currentTimestampMonthTicks);
            long periodEnd = currentTimestampMonthTicks;

            bool updatedLocalization = EnsurePreviewAssets(previewReport, periodStart, periodEnd, out Entity triggerEntity);
            if (updatedLocalization)
            {
                ReloadActiveLocale();
            }

            if (EnqueueChirp(triggerEntity))
            {
                bool panelOpened = openPanel && TryOpenChirperPanel();
                Mod.log.Info($"Monthly chirper manual preview enqueued. period={FormatEnglishPeriodPoint(periodStart)} -> {FormatEnglishPeriodPoint(periodEnd)}, total={previewReport.TotalViolationCount}, bus={previewReport.m_PublicTransportLaneCount}, mid={previewReport.m_MidBlockCrossingCount}, intersection={previewReport.m_IntersectionMovementCount}, fine={previewReport.m_TotalFineAmount}, panelOpened={panelOpened}");
                return true;
            }

            failureReason = "chirp enqueue failed";
            Mod.log.Info($"Monthly chirper manual preview skipped. reason={failureReason}");
            return false;
        }

        private bool EnsureReportAssets(MonthlyEnforcementReport report, out Entity triggerEntity)
        {
            EnsureSenderAccount();

            long periodStart = MonthlyEnforcementChirperService.GetReportPeriodStartMonthTicks(report);
            long periodEnd = MonthlyEnforcementChirperService.GetReportPeriodEndMonthTicks(report);
            string localizationId = GetReportLocalizationId(report.m_MonthIndex);
            bool localizationChanged = EnsureLocalizationEntries(localizationId, BuildLocalizedMessage(report, periodStart, periodEnd));

            if (localizationChanged)
            {
                ReloadActiveLocale();
            }

            if (m_ReportTriggerEntities.TryGetValue(report.m_MonthIndex, out triggerEntity) &&
                triggerEntity != Entity.Null &&
                EntityManager.Exists(triggerEntity))
            {
                return localizationChanged;
            }

            triggerEntity = CreateChirpTriggerEntity($"{kPrefabNamePrefix}.Report.{report.m_MonthIndex}", localizationId);
            m_ReportTriggerEntities[report.m_MonthIndex] = triggerEntity;
            return true;
        }

        private bool EnsurePreviewAssets(MonthlyEnforcementReport report, long periodStart, long periodEnd, out Entity triggerEntity)
        {
            EnsureSenderAccount();

            int previewSequence = ++m_ManualPreviewSequence;
            string assetKey = $"{kPrefabNamePrefix}.Preview.{periodEnd}.{previewSequence}";
            string localizationId = GetPreviewLocalizationId(periodEnd, previewSequence);
            bool localizationChanged = EnsureLocalizationEntries(localizationId, BuildLocalizedMessage(report, periodStart, periodEnd));

            if (localizationChanged)
            {
                ReloadActiveLocale();
            }

            triggerEntity = CreateChirpTriggerEntity(assetKey, localizationId);
            return localizationChanged;
        }

        private Entity CreateChirpTriggerEntity(string assetKey, string localizationId)
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

            Mod.log.Info($"Monthly chirper prefab assets created. key={assetKey}");
            return triggerEntity;
        }

        private bool EnsureLocalizationEntries(string localizationId, string localizedMessage)
        {
            string localeId = GetActiveLocaleId();
            string indexedLocalizationId = LocalizationUtils.AppendIndex(localizationId, new RandomLocalizationIndex(0));
            bool changed = EnsureSenderLocalizationEntry(localeId);
            Dictionary<string, string> entries = EnsureLocaleEntries(localeId);

            if (!entries.TryGetValue(indexedLocalizationId, out string currentLocalizedMessage) || currentLocalizedMessage != localizedMessage)
            {
                entries[indexedLocalizationId] = localizedMessage;
                changed = true;
            }

            return changed;
        }

        private bool EnsureSenderLocalizationEntry(string localeId)
        {
            Dictionary<string, string> entries = EnsureLocaleEntries(localeId);
            string senderText = LocalizeText(kSenderTextLocaleId, "Traffic Law Enforcement");
            if (entries.TryGetValue(kSenderLocalizationId, out string currentSenderText) && currentSenderText == senderText)
            {
                return false;
            }

            entries[kSenderLocalizationId] = senderText;
            return true;
        }

        private Dictionary<string, string> EnsureLocaleEntries(string localeId)
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
            return entries;
        }

        private bool EnqueueChirp(Entity triggerEntity)
        {
            if (triggerEntity == Entity.Null || !EntityManager.Exists(triggerEntity))
            {
                Mod.log.Info("Monthly chirper enqueue skipped because trigger entity is unavailable.");
                return false;
            }

            JobHandle dependency;
            NativeQueue<ChirpCreationData> queue = m_CreateChirpSystem.GetQueue(out dependency);
            dependency.Complete();

            queue.Enqueue(new ChirpCreationData
            {
                m_TriggerPrefab = triggerEntity,
                m_Sender = m_SenderAccountEntity,
                m_Target = Entity.Null
            });

            m_CreateChirpSystem.AddQueueWriter(default);
            return true;
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
                Mod.log.Info("Monthly chirper panel opened.");
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
            while (MonthlyEnforcementChirperService.TryConsumeManualPreviewRequest())
            {
            }
        }

        private InfoviewPrefab ResolveFallbackInfoviewPrefab()
        {
            using (NativeArray<Entity> chirperAccounts = m_ChirperAccountQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < chirperAccounts.Length; i++)
                {
                    if (m_PrefabSystem.TryGetPrefab(chirperAccounts[i], out ChirperAccount chirperAccount) &&
                        chirperAccount?.m_InfoView != null)
                    {
                        return chirperAccount.m_InfoView;
                    }
                }
            }

            using (NativeArray<Entity> infoviewPrefabs = m_InfoviewPrefabQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < infoviewPrefabs.Length; i++)
                {
                    if (m_PrefabSystem.TryGetPrefab(infoviewPrefabs[i], out InfoviewPrefab infoviewPrefab))
                    {
                        return infoviewPrefab;
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

        private static string BuildEnglishMessage(MonthlyEnforcementReport report, long periodStartMonthTicks, long periodEndMonthTicks)
        {
            return
                $"Traffic enforcement report for {FormatEnglishPeriodPoint(periodStartMonthTicks)} to {FormatEnglishPeriodPoint(periodEndMonthTicks)}: {report.TotalViolationCount} violations.\n" +
                $"Total violation rate: {FormatViolationRate(report.TotalViolationCount, report.m_TotalPathRequestCount)}. Total suppression failure rate: {FormatSuppressionFailureRate(report.TotalViolationCount, report.m_TotalAvoidedPathCount)}. Total fines: {FormatMoney(report.m_TotalFineAmount)}\u20A1.\n" +
                $"PT-lane: violation rate {FormatViolationRate(report.m_PublicTransportLaneCount, report.m_TotalPathRequestCount)}, suppression failure rate {FormatSuppressionFailureRate(report.m_PublicTransportLaneCount, report.m_PublicTransportLaneAvoidedEventCount)}, fines {FormatMoney(report.m_PublicTransportLaneFineAmount)}\u20A1.\n" +
                $"Mid-block: violation rate {FormatViolationRate(report.m_MidBlockCrossingCount, report.m_TotalPathRequestCount)}, suppression failure rate {FormatSuppressionFailureRate(report.m_MidBlockCrossingCount, report.m_MidBlockCrossingAvoidedEventCount)}, fines {FormatMoney(report.m_MidBlockCrossingFineAmount)}\u20A1.\n" +
                $"Intersection: violation rate {FormatViolationRate(report.m_IntersectionMovementCount, report.m_TotalPathRequestCount)}, suppression failure rate {FormatSuppressionFailureRate(report.m_IntersectionMovementCount, report.m_IntersectionMovementAvoidedEventCount)}, fines {FormatMoney(report.m_IntersectionMovementFineAmount)}\u20A1.";
        }

        private static string BuildKoreanMessageLegacy(MonthlyEnforcementReport report, long periodStartMonthTicks, long periodEndMonthTicks)
        {
            string fineText = report.m_TotalFineAmount.ToString("N0", CultureInfo.InvariantCulture);
            return $"{FormatKoreanPeriodPoint(periodStartMonthTicks)}부터 {FormatKoreanPeriodPoint(periodEndMonthTicks)}까지 버스전용차선 침범 {report.m_PublicTransportLaneCount}건, 중앙선 침범 {report.m_MidBlockCrossingCount}건, 교차로 통행규칙 위반 {report.m_IntersectionMovementCount}건, 총 {report.TotalViolationCount}건의 교통법규 위반 단속이 있었습니다. 총 벌금은 {fineText}\u20A1입니다.";
        }

        private static string FormatEnglishPeriodPoint(long monthTicks)
        {
            GetPeriodParts(monthTicks, out int year, out int month, out int hour, out int minute);
            string monthName = CultureInfo.GetCultureInfo(kDefaultLocale).DateTimeFormat.GetMonthName(month);
            return $"{monthName} {year} {hour:00}:{minute:00}";
        }

        private static string FormatKoreanPeriodPoint(long monthTicks)
        {
            GetPeriodParts(monthTicks, out int year, out int month, out int hour, out int minute);
            return $"{year}년 {month}월 {hour:00}:{minute:00}";
        }

        private static string BuildKoreanMessage(MonthlyEnforcementReport report, long periodStartMonthTicks, long periodEndMonthTicks)
        {
            return
                $"{FormatKoreanPeriodText(periodStartMonthTicks)}부터 {FormatKoreanPeriodText(periodEndMonthTicks)}까지 교통법규 단속 보고입니다. 총 위반 적발 {report.TotalViolationCount}건.\n" +
                $"전체 위반율: {FormatViolationRate(report.TotalViolationCount, report.m_TotalPathRequestCount)}. 전체 억제 실패율: {FormatSuppressionFailureRate(report.TotalViolationCount, report.m_TotalAvoidedPathCount)}. 총 벌금: {FormatMoney(report.m_TotalFineAmount)}\u20A1.\n" +
                $"대중교통 전용차선: 위반율 {FormatViolationRate(report.m_PublicTransportLaneCount, report.m_TotalPathRequestCount)}, 억제 실패율 {FormatSuppressionFailureRate(report.m_PublicTransportLaneCount, report.m_PublicTransportLaneAvoidedEventCount)}, 벌금 {FormatMoney(report.m_PublicTransportLaneFineAmount)}\u20A1.\n" +
                $"중앙선 침범: 위반율 {FormatViolationRate(report.m_MidBlockCrossingCount, report.m_TotalPathRequestCount)}, 억제 실패율 {FormatSuppressionFailureRate(report.m_MidBlockCrossingCount, report.m_MidBlockCrossingAvoidedEventCount)}, 벌금 {FormatMoney(report.m_MidBlockCrossingFineAmount)}\u20A1.\n" +
                $"교차로 통행규칙 위반: 위반율 {FormatViolationRate(report.m_IntersectionMovementCount, report.m_TotalPathRequestCount)}, 억제 실패율 {FormatSuppressionFailureRate(report.m_IntersectionMovementCount, report.m_IntersectionMovementAvoidedEventCount)}, 벌금 {FormatMoney(report.m_IntersectionMovementFineAmount)}\u20A1.";
        }

        private static string FormatKoreanPeriodText(long monthTicks)
        {
            GetPeriodParts(monthTicks, out int year, out int month, out int hour, out int minute);
            return $"{year}년 {month}월 {hour:00}:{minute:00}";
        }

        private static string BuildLocalizedMessage(MonthlyEnforcementReport report, long periodStartMonthTicks, long periodEndMonthTicks)
        {
            return
                FormatLocalizedText(kReportHeaderFormatLocaleId, "Traffic enforcement report for {0} to {1}: {2} violations.", FormatPeriodPoint(periodStartMonthTicks), FormatPeriodPoint(periodEndMonthTicks), report.TotalViolationCount) + "\n" +
                FormatLocalizedText(kTotalLineFormatLocaleId, "Total: violation rate {0}, suppression failure rate {1}, fines {2}\u20A1.", FormatViolationRate(report.TotalViolationCount, report.m_TotalPathRequestCount), FormatSuppressionFailureRate(report.TotalViolationCount, report.m_TotalAvoidedPathCount), FormatMoney(report.m_TotalFineAmount)) + "\n" +
                FormatLocalizedText(kPublicTransportLaneLineFormatLocaleId, "PT-lane: violation rate {0}, suppression failure rate {1}, fines {2}\u20A1.", FormatViolationRate(report.m_PublicTransportLaneCount, report.m_TotalPathRequestCount), FormatSuppressionFailureRate(report.m_PublicTransportLaneCount, report.m_PublicTransportLaneAvoidedEventCount), FormatMoney(report.m_PublicTransportLaneFineAmount)) + "\n" +
                FormatLocalizedText(kMidBlockLineFormatLocaleId, "Mid-block: violation rate {0}, suppression failure rate {1}, fines {2}\u20A1.", FormatViolationRate(report.m_MidBlockCrossingCount, report.m_TotalPathRequestCount), FormatSuppressionFailureRate(report.m_MidBlockCrossingCount, report.m_MidBlockCrossingAvoidedEventCount), FormatMoney(report.m_MidBlockCrossingFineAmount)) + "\n" +
                FormatLocalizedText(kIntersectionLineFormatLocaleId, "Intersection: violation rate {0}, suppression failure rate {1}, fines {2}\u20A1.", FormatViolationRate(report.m_IntersectionMovementCount, report.m_TotalPathRequestCount), FormatSuppressionFailureRate(report.m_IntersectionMovementCount, report.m_IntersectionMovementAvoidedEventCount), FormatMoney(report.m_IntersectionMovementFineAmount));
        }

        private static string FormatPeriodPoint(long monthTicks)
        {
            GetPeriodParts(monthTicks, out int year, out int month, out int hour, out int minute);
            CultureInfo culture = GetActiveCulture();
            string monthText = culture.DateTimeFormat.GetMonthName(month);
            if (string.IsNullOrWhiteSpace(monthText))
            {
                monthText = month.ToString(CultureInfo.InvariantCulture);
            }

            return FormatLocalizedText(kPeriodPointFormatLocaleId, "{0} {1} {2:00}:{3:00}", monthText, year, hour, minute);
        }

        private static string FormatViolationRate(int finedViolationCount, int avoidedPathCount)
        {
            if (avoidedPathCount <= 0)
            {
                return FormatZeroPercent();
            }

            return (100d * finedViolationCount / avoidedPathCount).ToString("0.0", GetActiveCulture()) + "%";
        }

        private static string FormatSuppressionFailureRate(int finedViolationCount, int avoidedPathCount)
        {
            int denominator = finedViolationCount + avoidedPathCount;
            if (denominator <= 0)
            {
                return FormatZeroPercent();
            }

            return (100d * finedViolationCount / denominator).ToString("0.0", GetActiveCulture()) + "%";
        }

        private static string FormatZeroPercent()
        {
            return 0d.ToString("0.0", GetActiveCulture()) + "%";
        }

        private static string FormatMoney(int amount)
        {
            return amount.ToString("N0", GetActiveCulture());
        }

        private static string GetActiveLocaleId()
        {
            return NormalizeLocaleId(GameManager.instance?.localizationManager?.activeLocaleId);
        }

        private static string NormalizeLocaleId(string localeId)
        {
            return string.IsNullOrWhiteSpace(localeId) ? kDefaultLocale : localeId;
        }

        private static CultureInfo GetActiveCulture()
        {
            try
            {
                return CultureInfo.GetCultureInfo(GetActiveLocaleId());
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.GetCultureInfo(kDefaultLocale);
            }
        }

        private static string LocalizeText(string localeId, string fallback)
        {
            if (GameManager.instance?.localizationManager?.activeDictionary != null &&
                GameManager.instance.localizationManager.activeDictionary.TryGetValue(localeId, out string value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return fallback;
        }

        private static string FormatLocalizedText(string localeId, string fallbackFormat, params object[] args)
        {
            string format = LocalizeText(localeId, fallbackFormat);
            return string.Format(GetActiveCulture(), format, args);
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
    }
}
