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

        protected override void OnCreate()
        {
            base.OnCreate();

            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_CreateChirpSystem = World.GetOrCreateSystemManaged<CreateChirpSystem>();
            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            m_ChirperAccountQuery = GetEntityQuery(ComponentType.ReadOnly<ChirperAccountData>(), ComponentType.ReadOnly<PrefabData>());
            m_InfoviewPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<InfoviewData>(), ComponentType.ReadOnly<PrefabData>());

            CacheStaticChirperTemplates();
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

            ReloadActiveLocale();
        }

        private void CacheStaticChirperTemplates()
        {
            m_StaticChirperTemplatesByLocale.Clear();

            Dictionary<string, string> enTemplates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            LocaleEN.AddMonthlyChirperEntries(enTemplates);
            m_StaticChirperTemplatesByLocale["en-US"] = enTemplates;

            Dictionary<string, string> koTemplates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            LocaleKO.AddMonthlyChirperEntries(koTemplates);
            m_StaticChirperTemplatesByLocale["ko-KR"] = koTemplates;
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
            bool localizationChanged = EnsureLocalizationEntriesForLocales(localizationId, report, periodStart, periodEnd);

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
            bool localizationChanged = EnsureLocalizationEntriesForLocales(localizationId, report, periodStart, periodEnd);

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

        private IEnumerable<string> GetLocalizationBuildLocales()
        {
            HashSet<string> localeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                kDefaultLocale
            };

            localeIds.Add(GetActiveLocaleId());
            return localeIds;
        }

        private bool EnsureLocalizationEntriesForLocales(string localizationId, MonthlyEnforcementReport report, long periodStart, long periodEnd)
        {
            bool changed = false;

            foreach (string localeId in GetLocalizationBuildLocales())
            {
                changed |= EnsureSenderLocalizationEntry(localeId);
                changed |= EnsureLocalizationEntryForLocale(
                    localeId,
                    localizationId,
                    BuildLocalizedMessage(localeId, report, periodStart, periodEnd));
            }

            return changed;
        }

        private bool EnsureLocalizationEntryForLocale(string localeId, string localizationId, string localizedMessage)
        {
            localeId = NormalizeLocaleId(localeId);
            string indexedLocalizationId = LocalizationUtils.AppendIndex(localizationId, new RandomLocalizationIndex(0));
            Dictionary<string, string> entries = EnsureLocaleEntries(localeId);

            if (!entries.TryGetValue(indexedLocalizationId, out string currentLocalizedMessage) || currentLocalizedMessage != localizedMessage)
            {
                entries[indexedLocalizationId] = localizedMessage;
                return true;
            }

            return false;
        }

        private bool EnsureSenderLocalizationEntry(string localeId)
        {
            localeId = NormalizeLocaleId(localeId);
            Dictionary<string, string> entries = EnsureLocaleEntries(localeId);
            string senderText = GetStaticChirperTemplate(localeId, kSenderTextLocaleId);

            if (entries.TryGetValue(kSenderLocalizationId, out string currentSenderText) &&
                currentSenderText == senderText)
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

        private static string FormatEnglishPeriodPoint(long monthTicks)
        {
            GetPeriodParts(monthTicks, out int year, out int month, out int hour, out int minute);
            string monthName = CultureInfo.GetCultureInfo(kDefaultLocale).DateTimeFormat.GetMonthName(month);
            return $"{monthName} {year} {hour:00}:{minute:00}";
        }

        private string BuildLocalizedMessage(string localeId, MonthlyEnforcementReport report, long periodStartMonthTicks, long periodEndMonthTicks)
        {
            return
                FormatLocalizedTextForLocale(
                    localeId,
                    kReportHeaderFormatLocaleId,
                    FormatPeriodPoint(localeId, periodStartMonthTicks),
                    FormatPeriodPoint(localeId, periodEndMonthTicks),
                    report.TotalViolationCount) + "\n" +
                FormatLocalizedTextForLocale(
                    localeId,
                    kTotalLineFormatLocaleId,
                    FormatViolationRate(localeId, report.TotalViolationCount, report.m_TotalPathRequestCount),
                    FormatSuppressionFailureRate(localeId, report.TotalViolationCount, report.m_TotalAvoidedPathCount),
                    FormatMoney(localeId, report.m_TotalFineAmount)) + "\n" +
                FormatLocalizedTextForLocale(
                    localeId,
                    kPublicTransportLaneLineFormatLocaleId,
                    FormatViolationRate(localeId, report.m_PublicTransportLaneCount, report.m_TotalPathRequestCount),
                    FormatSuppressionFailureRate(localeId, report.m_PublicTransportLaneCount, report.m_PublicTransportLaneAvoidedEventCount),
                    FormatMoney(localeId, report.m_PublicTransportLaneFineAmount)) + "\n" +
                FormatLocalizedTextForLocale(
                    localeId,
                    kMidBlockLineFormatLocaleId,
                    FormatViolationRate(localeId, report.m_MidBlockCrossingCount, report.m_TotalPathRequestCount),
                    FormatSuppressionFailureRate(localeId, report.m_MidBlockCrossingCount, report.m_MidBlockCrossingAvoidedEventCount),
                    FormatMoney(localeId, report.m_MidBlockCrossingFineAmount)) + "\n" +
                FormatLocalizedTextForLocale(
                    localeId,
                    kIntersectionLineFormatLocaleId,
                    FormatViolationRate(localeId, report.m_IntersectionMovementCount, report.m_TotalPathRequestCount),
                    FormatSuppressionFailureRate(localeId, report.m_IntersectionMovementCount, report.m_IntersectionMovementAvoidedEventCount),
                    FormatMoney(localeId, report.m_IntersectionMovementFineAmount));
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

        private static string FormatViolationRate(string localeId, int finedViolationCount, int avoidedPathCount)
        {
            CultureInfo culture = GetCultureForLocale(localeId);
            if (avoidedPathCount <= 0)
            {
                return 0d.ToString("0.0", culture) + "%";
            }

            return (100d * finedViolationCount / avoidedPathCount).ToString("0.0", culture) + "%";
        }

        private static string FormatSuppressionFailureRate(string localeId, int finedViolationCount, int avoidedPathCount)
        {
            CultureInfo culture = GetCultureForLocale(localeId);
            int denominator = finedViolationCount + avoidedPathCount;
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
    }
}

