using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game;
using Game.SceneFlow;

namespace Traffic_Law_Enforcement
{
    public struct EnforcementPolicyImpactTrackingState
    {
        public long m_MonthIndex;
        public int m_TotalActualPathCount;
        public int m_TotalAvoidedPathCount;
        public int m_TotalFineAmount;
        public int m_PublicTransportLaneActualCount;
        public int m_MidBlockCrossingActualCount;
        public int m_IntersectionMovementActualCount;
        public int m_PublicTransportLaneFineAmount;
        public int m_MidBlockCrossingFineAmount;
        public int m_IntersectionMovementFineAmount;
        public int m_PublicTransportLaneAvoidedEventCount;
        public int m_MidBlockCrossingAvoidedEventCount;
        public int m_IntersectionMovementAvoidedEventCount;

        public EnforcementPolicyImpactTrackingState(
            long monthIndex,
            int totalActualPathCount,
            int totalAvoidedPathCount,
            int totalFineAmount,
            int publicTransportLaneActualCount,
            int midBlockCrossingActualCount,
            int intersectionMovementActualCount,
            int publicTransportLaneFineAmount,
            int midBlockCrossingFineAmount,
            int intersectionMovementFineAmount,
            int publicTransportLaneAvoidedEventCount,
            int midBlockCrossingAvoidedEventCount,
            int intersectionMovementAvoidedEventCount)
        {
            m_MonthIndex = monthIndex;
            m_TotalActualPathCount = totalActualPathCount;
            m_TotalAvoidedPathCount = totalAvoidedPathCount;
            m_TotalFineAmount = totalFineAmount;
            m_PublicTransportLaneActualCount = publicTransportLaneActualCount;
            m_MidBlockCrossingActualCount = midBlockCrossingActualCount;
            m_IntersectionMovementActualCount = intersectionMovementActualCount;
            m_PublicTransportLaneFineAmount = publicTransportLaneFineAmount;
            m_MidBlockCrossingFineAmount = midBlockCrossingFineAmount;
            m_IntersectionMovementFineAmount = intersectionMovementFineAmount;
            m_PublicTransportLaneAvoidedEventCount = publicTransportLaneAvoidedEventCount;
            m_MidBlockCrossingAvoidedEventCount = midBlockCrossingAvoidedEventCount;
            m_IntersectionMovementAvoidedEventCount = intersectionMovementAvoidedEventCount;
        }
    }

    public static class EnforcementPolicyImpactService
    {
        public const string kLoadedSaveOnlyLocaleId = "TrafficLawEnforcement.PolicyImpact.Text.LoadedSaveOnly";
        public const string kWaitingForTimeLocaleId = "TrafficLawEnforcement.PolicyImpact.Text.WaitingForTime";
        public const string kNoDataLocaleId = "TrafficLawEnforcement.PolicyImpact.Text.NoData";
        public const string kSummaryLineFormatLocaleId = "TrafficLawEnforcement.PolicyImpact.Text.SummaryLineFormat";
        public const string kDetailLineFormatLocaleId = "TrafficLawEnforcement.PolicyImpact.Text.DetailLineFormat";
        public const string kNoteLocaleId = "TrafficLawEnforcement.PolicyImpact.Text.Note";
        public const string kTotalLabelLocaleId = "TrafficLawEnforcement.PolicyImpact.Label.Total";
        public const string kPublicTransportLaneLabelLocaleId = "TrafficLawEnforcement.PolicyImpact.Label.PublicTransportLane";
        public const string kMidBlockLabelLocaleId = "TrafficLawEnforcement.PolicyImpact.Label.MidBlock";
        public const string kIntersectionLabelLocaleId = "TrafficLawEnforcement.PolicyImpact.Label.Intersection";

        public readonly struct PersistentTotalsSnapshot
        {
            public readonly int TotalActualPathCount;
            public readonly int TotalAvoidedPathCount;
            public readonly int TotalFineAmount;
            public readonly int PublicTransportLaneActualCount;
            public readonly int MidBlockCrossingActualCount;
            public readonly int IntersectionMovementActualCount;
            public readonly int PublicTransportLaneFineAmount;
            public readonly int MidBlockCrossingFineAmount;
            public readonly int IntersectionMovementFineAmount;
            public readonly int PublicTransportLaneAvoidedEventCount;
            public readonly int MidBlockCrossingAvoidedEventCount;
            public readonly int IntersectionMovementAvoidedEventCount;

            public PersistentTotalsSnapshot(
                int totalActualPathCount,
                int totalAvoidedPathCount,
                int totalFineAmount,
                int publicTransportLaneActualCount,
                int midBlockCrossingActualCount,
                int intersectionMovementActualCount,
                int publicTransportLaneFineAmount,
                int midBlockCrossingFineAmount,
                int intersectionMovementFineAmount,
                int publicTransportLaneAvoidedEventCount,
                int midBlockCrossingAvoidedEventCount,
                int intersectionMovementAvoidedEventCount)
            {
                TotalActualPathCount = totalActualPathCount;
                TotalAvoidedPathCount = totalAvoidedPathCount;
                TotalFineAmount = totalFineAmount;
                PublicTransportLaneActualCount = publicTransportLaneActualCount;
                MidBlockCrossingActualCount = midBlockCrossingActualCount;
                IntersectionMovementActualCount = intersectionMovementActualCount;
                PublicTransportLaneFineAmount = publicTransportLaneFineAmount;
                MidBlockCrossingFineAmount = midBlockCrossingFineAmount;
                IntersectionMovementFineAmount = intersectionMovementFineAmount;
                PublicTransportLaneAvoidedEventCount = publicTransportLaneAvoidedEventCount;
                MidBlockCrossingAvoidedEventCount = midBlockCrossingAvoidedEventCount;
                IntersectionMovementAvoidedEventCount = intersectionMovementAvoidedEventCount;
            }
        }

        private static bool s_HasTrackingState;
        private static EnforcementPolicyImpactTrackingState s_TrackingState;
        private static int s_TotalActualPathCount;
        private static int s_TotalAvoidedPathCount;
        private static int s_TotalFineAmount;
        private static int s_PublicTransportLaneActualCount;
        private static int s_MidBlockCrossingActualCount;
        private static int s_IntersectionMovementActualCount;
        private static int s_PublicTransportLaneFineAmount;
        private static int s_MidBlockCrossingFineAmount;
        private static int s_IntersectionMovementFineAmount;
        private static int s_PublicTransportLaneAvoidedEventCount;
        private static int s_MidBlockCrossingAvoidedEventCount;
        private static int s_IntersectionMovementAvoidedEventCount;

        public static bool TryGetTrackingState(out EnforcementPolicyImpactTrackingState trackingState)
        {
            trackingState = s_TrackingState;
            return s_HasTrackingState;
        }

        public static void ResetPersistentData()
        {
            s_HasTrackingState = false;
            s_TrackingState = default;
            s_TotalActualPathCount = 0;
            s_TotalAvoidedPathCount = 0;
            s_TotalFineAmount = 0;
            s_PublicTransportLaneActualCount = 0;
            s_MidBlockCrossingActualCount = 0;
            s_IntersectionMovementActualCount = 0;
            s_PublicTransportLaneFineAmount = 0;
            s_MidBlockCrossingFineAmount = 0;
            s_IntersectionMovementFineAmount = 0;
            s_PublicTransportLaneAvoidedEventCount = 0;
            s_MidBlockCrossingAvoidedEventCount = 0;
            s_IntersectionMovementAvoidedEventCount = 0;
        }

        public static void LoadPersistentData(
            EnforcementPolicyImpactTrackingState? trackingState,
            int totalActualPathCount,
            int totalAvoidedPathCount,
            int totalFineAmount,
            int publicTransportLaneActualCount,
            int midBlockCrossingActualCount,
            int intersectionMovementActualCount,
            int publicTransportLaneFineAmount,
            int midBlockCrossingFineAmount,
            int intersectionMovementFineAmount,
            int publicTransportLaneAvoidedEventCount,
            int midBlockCrossingAvoidedEventCount,
            int intersectionMovementAvoidedEventCount)
        {
            ResetPersistentData();

            if (trackingState.HasValue)
            {
                s_TrackingState = trackingState.Value;
                s_HasTrackingState = true;
            }

            s_TotalActualPathCount = totalActualPathCount;
            s_TotalAvoidedPathCount = totalAvoidedPathCount;
            s_TotalFineAmount = totalFineAmount;
            s_PublicTransportLaneActualCount = publicTransportLaneActualCount;
            s_MidBlockCrossingActualCount = midBlockCrossingActualCount;
            s_IntersectionMovementActualCount = intersectionMovementActualCount;
            s_PublicTransportLaneFineAmount = publicTransportLaneFineAmount;
            s_MidBlockCrossingFineAmount = midBlockCrossingFineAmount;
            s_IntersectionMovementFineAmount = intersectionMovementFineAmount;
            s_PublicTransportLaneAvoidedEventCount = publicTransportLaneAvoidedEventCount;
            s_MidBlockCrossingAvoidedEventCount = midBlockCrossingAvoidedEventCount;
            s_IntersectionMovementAvoidedEventCount = intersectionMovementAvoidedEventCount;
        }

        public static PersistentTotalsSnapshot GetPersistentTotalsSnapshot()
        {
            return new PersistentTotalsSnapshot(
                s_TotalActualPathCount,
                s_TotalAvoidedPathCount,
                s_TotalFineAmount,
                s_PublicTransportLaneActualCount,
                s_MidBlockCrossingActualCount,
                s_IntersectionMovementActualCount,
                s_PublicTransportLaneFineAmount,
                s_MidBlockCrossingFineAmount,
                s_IntersectionMovementFineAmount,
                s_PublicTransportLaneAvoidedEventCount,
                s_MidBlockCrossingAvoidedEventCount,
                s_IntersectionMovementAvoidedEventCount);
        }

        public static void UpdateTrackingForCurrentMonth()
        {
            if (!EnforcementGameTime.IsInitialized)
            {
                return;
            }

            long currentMonthIndex = EnforcementGameTime.GetMonthIndex(EnforcementGameTime.CurrentTimestampMonthTicks);
            if (!s_HasTrackingState || currentMonthIndex != s_TrackingState.m_MonthIndex)
            {
                s_TrackingState = CaptureCurrentState(currentMonthIndex);
                s_HasTrackingState = true;
            }
        }

        public static void RecordActualViolation(string kind, int fineAmount)
        {
            s_TotalActualPathCount += 1;
            s_TotalFineAmount += fineAmount;

            switch (kind)
            {
                case EnforcementKinds.PublicTransportLane:
                    s_PublicTransportLaneActualCount += 1;
                    s_PublicTransportLaneFineAmount += fineAmount;
                    break;
                case EnforcementKinds.MidBlockCrossing:
                    s_MidBlockCrossingActualCount += 1;
                    s_MidBlockCrossingFineAmount += fineAmount;
                    break;
                case EnforcementKinds.IntersectionMovement:
                    s_IntersectionMovementActualCount += 1;
                    s_IntersectionMovementFineAmount += fineAmount;
                    break;
            }
        }

        public static void RecordAvoidedReroute(bool avoidedPublicTransportLanePenalty, bool avoidedMidBlockPenalty, bool avoidedIntersectionPenalty)
        {
            if (!avoidedPublicTransportLanePenalty && !avoidedMidBlockPenalty && !avoidedIntersectionPenalty)
            {
                return;
            }

            s_TotalAvoidedPathCount += 1;

            if (avoidedPublicTransportLanePenalty)
            {
                s_PublicTransportLaneAvoidedEventCount += 1;
            }

            if (avoidedMidBlockPenalty)
            {
                s_MidBlockCrossingAvoidedEventCount += 1;
            }

            if (avoidedIntersectionPenalty)
            {
                s_IntersectionMovementAvoidedEventCount += 1;
            }
        }

        public static string GetCurrentPeriodSummaryText()
        {
            if (!IsGameplayContextAvailable())
            {
                return LocalizeText(kLoadedSaveOnlyLocaleId, "Available only in a loaded save.");
            }

            if (!EnforcementGameTime.IsInitialized)
            {
                return LocalizeText(kWaitingForTimeLocaleId, "Waiting for in-game time initialization.");
            }

            CurrentPeriodSnapshot snapshot = GetCurrentPeriodSnapshot();
            int denominator = snapshot.TotalActualPathCount + snapshot.TotalAvoidedPathCount;
            if (denominator <= 0)
            {
                return LocalizeText(kNoDataLocaleId, "No fined or rerouted penalized paths recorded yet.");
            }

            string ratio = FormatRatio(snapshot.TotalActualPathCount, denominator);
            string fines = FormatMoney(snapshot.TotalFineAmount);
            string totalLabel = LocalizeText(kTotalLabelLocaleId, "Total");
            return FormatLocalizedText(kSummaryLineFormatLocaleId, "{0}: violation rate {1}, fines {2}", totalLabel, ratio, fines);
        }

        public static string GetCurrentPeriodDetailsText()
        {
            if (!IsGameplayContextAvailable())
            {
                return LocalizeText(kLoadedSaveOnlyLocaleId, "Available only in a loaded save.");
            }

            if (!EnforcementGameTime.IsInitialized)
            {
                return LocalizeText(kWaitingForTimeLocaleId, "Waiting for in-game time initialization.");
            }

            CurrentPeriodSnapshot snapshot = GetCurrentPeriodSnapshot();
            StringBuilder builder = new StringBuilder(512);
            AppendRatioAndFineLine(builder, LocalizeText(kTotalLabelLocaleId, "Total"), snapshot.TotalActualPathCount, snapshot.TotalAvoidedPathCount, snapshot.TotalFineAmount);
            AppendRatioAndFineLine(builder, LocalizeText(kPublicTransportLaneLabelLocaleId, "PT-lane"), snapshot.PublicTransportLaneActualCount, snapshot.PublicTransportLaneAvoidedEventCount, snapshot.PublicTransportLaneFineAmount);
            AppendRatioAndFineLine(builder, LocalizeText(kMidBlockLabelLocaleId, "Mid-block"), snapshot.MidBlockCrossingActualCount, snapshot.MidBlockCrossingAvoidedEventCount, snapshot.MidBlockCrossingFineAmount);
            AppendRatioAndFineLine(builder, LocalizeText(kIntersectionLabelLocaleId, "Intersection"), snapshot.IntersectionMovementActualCount, snapshot.IntersectionMovementAvoidedEventCount, snapshot.IntersectionMovementFineAmount);
            builder.AppendLine();
            builder.Append(LocalizeText(kNoteLocaleId, "Note: D counts estimated rerouted paths that gave up a penalized route. Per-type D counts can overlap when one reroute avoids multiple penalty types."));
            return builder.ToString();
        }

        public static string GetCurrentPeriodPublicTransportLaneText()
        {
            return GetCurrentPeriodLineText(kPublicTransportLaneLabelLocaleId, "PT-lane", snapshot => snapshot.PublicTransportLaneActualCount, snapshot => snapshot.PublicTransportLaneAvoidedEventCount, snapshot => snapshot.PublicTransportLaneFineAmount);
        }

        public static string GetCurrentPeriodMidBlockText()
        {
            return GetCurrentPeriodLineText(kMidBlockLabelLocaleId, "Mid-block", snapshot => snapshot.MidBlockCrossingActualCount, snapshot => snapshot.MidBlockCrossingAvoidedEventCount, snapshot => snapshot.MidBlockCrossingFineAmount);
        }

        public static string GetCurrentPeriodIntersectionText()
        {
            return GetCurrentPeriodLineText(kIntersectionLabelLocaleId, "Intersection", snapshot => snapshot.IntersectionMovementActualCount, snapshot => snapshot.IntersectionMovementAvoidedEventCount, snapshot => snapshot.IntersectionMovementFineAmount);
        }

        private static CurrentPeriodSnapshot GetCurrentPeriodSnapshot()
        {
            UpdateTrackingForCurrentMonth();

            EnforcementPolicyImpactTrackingState baseline = s_HasTrackingState
                ? s_TrackingState
                : CaptureCurrentState(EnforcementGameTime.GetMonthIndex(EnforcementGameTime.CurrentTimestampMonthTicks));

            return new CurrentPeriodSnapshot(
                ClampToNonNegative(s_TotalActualPathCount - baseline.m_TotalActualPathCount),
                ClampToNonNegative(s_TotalAvoidedPathCount - baseline.m_TotalAvoidedPathCount),
                ClampToNonNegative(s_TotalFineAmount - baseline.m_TotalFineAmount),
                ClampToNonNegative(s_PublicTransportLaneActualCount - baseline.m_PublicTransportLaneActualCount),
                ClampToNonNegative(s_MidBlockCrossingActualCount - baseline.m_MidBlockCrossingActualCount),
                ClampToNonNegative(s_IntersectionMovementActualCount - baseline.m_IntersectionMovementActualCount),
                ClampToNonNegative(s_PublicTransportLaneFineAmount - baseline.m_PublicTransportLaneFineAmount),
                ClampToNonNegative(s_MidBlockCrossingFineAmount - baseline.m_MidBlockCrossingFineAmount),
                ClampToNonNegative(s_IntersectionMovementFineAmount - baseline.m_IntersectionMovementFineAmount),
                ClampToNonNegative(s_PublicTransportLaneAvoidedEventCount - baseline.m_PublicTransportLaneAvoidedEventCount),
                ClampToNonNegative(s_MidBlockCrossingAvoidedEventCount - baseline.m_MidBlockCrossingAvoidedEventCount),
                ClampToNonNegative(s_IntersectionMovementAvoidedEventCount - baseline.m_IntersectionMovementAvoidedEventCount));
        }

        private static EnforcementPolicyImpactTrackingState CaptureCurrentState(long monthIndex)
        {
            return new EnforcementPolicyImpactTrackingState(
                monthIndex,
                s_TotalActualPathCount,
                s_TotalAvoidedPathCount,
                s_TotalFineAmount,
                s_PublicTransportLaneActualCount,
                s_MidBlockCrossingActualCount,
                s_IntersectionMovementActualCount,
                s_PublicTransportLaneFineAmount,
                s_MidBlockCrossingFineAmount,
                s_IntersectionMovementFineAmount,
                s_PublicTransportLaneAvoidedEventCount,
                s_MidBlockCrossingAvoidedEventCount,
                s_IntersectionMovementAvoidedEventCount);
        }

        private static bool IsGameplayContextAvailable()
        {
            return GameManager.instance != null && GameManager.instance.gameMode.IsGameOrEditor();
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

        private static string GetCurrentPeriodLineText(string labelLocaleId, string fallbackLabel, Func<CurrentPeriodSnapshot, int> actualCountSelector, Func<CurrentPeriodSnapshot, int> avoidedCountSelector, Func<CurrentPeriodSnapshot, int> fineAmountSelector)
        {
            if (!IsGameplayContextAvailable())
            {
                return LocalizeText(kLoadedSaveOnlyLocaleId, "Available only in a loaded save.");
            }

            if (!EnforcementGameTime.IsInitialized)
            {
                return LocalizeText(kWaitingForTimeLocaleId, "Waiting for in-game time initialization.");
            }

            CurrentPeriodSnapshot snapshot = GetCurrentPeriodSnapshot();
            string label = LocalizeText(labelLocaleId, fallbackLabel);
            int actualCount = actualCountSelector(snapshot);
            int avoidedCount = avoidedCountSelector(snapshot);
            int fineAmount = fineAmountSelector(snapshot);
            int denominator = actualCount + avoidedCount;
            string ratio = denominator > 0 ? FormatRatio(actualCount, denominator) : "n/a";
            string fines = FormatMoney(fineAmount);
            return FormatLocalizedText(kDetailLineFormatLocaleId, "{0}: violation rate {1}, fines {2}", label, ratio, fines);
        }

        private static int ClampToNonNegative(int value)
        {
            return value < 0 ? 0 : value;
        }

        private static string FormatRatio(int numerator, int denominator)
        {
            if (denominator <= 0)
            {
                return "n/a";
            }

            return (100d * numerator / denominator).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatMoney(int amount)
        {
            return amount.ToString("N0", CultureInfo.InvariantCulture);
        }

        private static string FormatLocalizedText(string localeId, string fallbackFormat, params object[] args)
        {
            string format = LocalizeText(localeId, fallbackFormat);
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

        private static void AppendRatioAndFineLine(StringBuilder builder, string label, int actualCount, int avoidedCount, int fineAmount)
        {
            int denominator = actualCount + avoidedCount;
            string ratio = denominator > 0 ? FormatRatio(actualCount, denominator) : "n/a";
            string fines = FormatMoney(fineAmount);
            builder.AppendLine(FormatLocalizedText(kDetailLineFormatLocaleId, "{0}: violation rate {1}, fines {2}", label, ratio, fines));
        }

        private readonly struct CurrentPeriodSnapshot
        {
            public readonly int TotalActualPathCount;
            public readonly int TotalAvoidedPathCount;
            public readonly int TotalFineAmount;
            public readonly int PublicTransportLaneActualCount;
            public readonly int MidBlockCrossingActualCount;
            public readonly int IntersectionMovementActualCount;
            public readonly int PublicTransportLaneFineAmount;
            public readonly int MidBlockCrossingFineAmount;
            public readonly int IntersectionMovementFineAmount;
            public readonly int PublicTransportLaneAvoidedEventCount;
            public readonly int MidBlockCrossingAvoidedEventCount;
            public readonly int IntersectionMovementAvoidedEventCount;

            public CurrentPeriodSnapshot(
                int totalActualPathCount,
                int totalAvoidedPathCount,
                int totalFineAmount,
                int publicTransportLaneActualCount,
                int midBlockCrossingActualCount,
                int intersectionMovementActualCount,
                int publicTransportLaneFineAmount,
                int midBlockCrossingFineAmount,
                int intersectionMovementFineAmount,
                int publicTransportLaneAvoidedEventCount,
                int midBlockCrossingAvoidedEventCount,
                int intersectionMovementAvoidedEventCount)
            {
                TotalActualPathCount = totalActualPathCount;
                TotalAvoidedPathCount = totalAvoidedPathCount;
                TotalFineAmount = totalFineAmount;
                PublicTransportLaneActualCount = publicTransportLaneActualCount;
                MidBlockCrossingActualCount = midBlockCrossingActualCount;
                IntersectionMovementActualCount = intersectionMovementActualCount;
                PublicTransportLaneFineAmount = publicTransportLaneFineAmount;
                MidBlockCrossingFineAmount = midBlockCrossingFineAmount;
                IntersectionMovementFineAmount = intersectionMovementFineAmount;
                PublicTransportLaneAvoidedEventCount = publicTransportLaneAvoidedEventCount;
                MidBlockCrossingAvoidedEventCount = midBlockCrossingAvoidedEventCount;
                IntersectionMovementAvoidedEventCount = intersectionMovementAvoidedEventCount;
            }
        }
    }
}
