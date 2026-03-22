using System.Text;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public static class EnforcementPenaltyService
    {
        public const int DefaultPublicTransportLaneFine = 250;
        public const int DefaultMidBlockCrossingFine = 250;
        public const int DefaultIntersectionMovementFine = 250;
        private static string s_LastLoggedRepeatPolicySummary;

        public static void RecordPublicTransportLaneViolation(Entity vehicle, Entity lane, string reason)
        {
            RecordViolation(EnforcementKinds.PublicTransportLane, vehicle, lane, GetPublicTransportLaneFine(), reason);
        }

        public static void RecordMidBlockCrossingViolation(Entity vehicle, Entity lane, string reason)
        {
            RecordViolation(EnforcementKinds.MidBlockCrossing, vehicle, lane, GetMidBlockCrossingFine(), reason);
        }

        public static void RecordIntersectionMovementViolation(Entity vehicle, Entity lane, string reason)
        {
            RecordViolation(EnforcementKinds.IntersectionMovement, vehicle, lane, GetIntersectionMovementFine(), reason);
        }

        public static int GetPublicTransportLaneFine()
        {
            return EnforcementGameplaySettingsService.Current.PublicTransportLaneFineAmount;
        }

        public static int GetMidBlockCrossingFine()
        {
            return EnforcementGameplaySettingsService.Current.MidBlockCrossingFineAmount;
        }

        public static int GetIntersectionMovementFine()
        {
            return EnforcementGameplaySettingsService.Current.IntersectionMovementFineAmount;
        }

        public static int ApplyRepeatOffenderPenalty(string kind, int baseFine, int vehicleId)
        {
            RepeatOffenderPolicy policy = GetRepeatOffenderPolicy(kind);
            if (!policy.Enabled)
            {
                return baseFine;
            }

            int violationCountInWindow = EnforcementTelemetry.GetRecentViolationCount(kind, vehicleId, policy.WindowMonthTicks, includeCurrentEvent: true);
            if (violationCountInWindow < policy.Threshold)
            {
                return baseFine;
            }

            return baseFine * policy.MultiplierPercent / 100;
        }

        public static long GetMaximumRepeatWindowMonthTicks()
        {
            EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;
            int maxMonths = System.Math.Max(settings.PublicTransportLaneRepeatWindowMonths,
                System.Math.Max(settings.MidBlockCrossingRepeatWindowMonths, settings.IntersectionMovementRepeatWindowMonths));
            return EnforcementGameTime.GetMonthTickWindow(maxMonths);
        }

        public static string GetRepeatPolicyDebugSummary(string kind)
        {
            EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;

            switch (kind)
            {
                case EnforcementKinds.PublicTransportLane:
                    return BuildRepeatPolicyDebugSummary(
                        settings.EnablePublicTransportLaneRepeatPenalty,
                        settings.PublicTransportLaneRepeatWindowMonths,
                        settings.PublicTransportLaneRepeatThreshold,
                        settings.PublicTransportLaneRepeatMultiplierPercent);
                case EnforcementKinds.MidBlockCrossing:
                    return BuildRepeatPolicyDebugSummary(
                        settings.EnableMidBlockCrossingRepeatPenalty,
                        settings.MidBlockCrossingRepeatWindowMonths,
                        settings.MidBlockCrossingRepeatThreshold,
                        settings.MidBlockCrossingRepeatMultiplierPercent);
                case EnforcementKinds.IntersectionMovement:
                    return BuildRepeatPolicyDebugSummary(
                        settings.EnableIntersectionMovementRepeatPenalty,
                        settings.IntersectionMovementRepeatWindowMonths,
                        settings.IntersectionMovementRepeatThreshold,
                        settings.IntersectionMovementRepeatMultiplierPercent);
                default:
                    return "unknown policy";
            }
        }

        public static void LogRepeatPolicySummaryIfChanged()
        {
            string summary = BuildRepeatPolicyLogSummary();
            if (string.IsNullOrWhiteSpace(summary) || summary == s_LastLoggedRepeatPolicySummary)
            {
                return;
            }

            s_LastLoggedRepeatPolicySummary = summary;
        }

        private static void RecordViolation(string kind, Entity vehicle, Entity lane, int fineAmount, string reason)
        {
            if (!IsEnforcementKindEnabled(kind))
            {
                return;
            }

            int adjustedFine = ApplyRepeatOffenderPenalty(kind, fineAmount, vehicle.Index);
            string adjustedReason = adjustedFine == fineAmount
                ? reason
                : $"{reason} Repeat offender multiplier applied: {fineAmount} -> {adjustedFine}.";

            EnforcementTelemetry.RecordFine(kind, vehicle.Index, lane.Index, adjustedFine, adjustedReason);
            EnforcementPolicyImpactService.RecordActualViolation(kind, adjustedFine);
            EnforcementFineMoneyService.EnqueueCharge(vehicle, adjustedFine, kind);
        }

        private static RepeatOffenderPolicy GetRepeatOffenderPolicy(string kind)
        {
            EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;

            switch (kind)
            {
                case EnforcementKinds.PublicTransportLane:
                    return new RepeatOffenderPolicy(
                        settings.EnablePublicTransportLaneRepeatPenalty,
                        EnforcementGameTime.GetMonthTickWindow(settings.PublicTransportLaneRepeatWindowMonths),
                        settings.PublicTransportLaneRepeatThreshold,
                        settings.PublicTransportLaneRepeatMultiplierPercent);
                case EnforcementKinds.MidBlockCrossing:
                    return new RepeatOffenderPolicy(
                        settings.EnableMidBlockCrossingRepeatPenalty,
                        EnforcementGameTime.GetMonthTickWindow(settings.MidBlockCrossingRepeatWindowMonths),
                        settings.MidBlockCrossingRepeatThreshold,
                        settings.MidBlockCrossingRepeatMultiplierPercent);
                case EnforcementKinds.IntersectionMovement:
                    return new RepeatOffenderPolicy(
                        settings.EnableIntersectionMovementRepeatPenalty,
                        EnforcementGameTime.GetMonthTickWindow(settings.IntersectionMovementRepeatWindowMonths),
                        settings.IntersectionMovementRepeatThreshold,
                        settings.IntersectionMovementRepeatMultiplierPercent);
                default:
                    return default;
            }
        }

        private static bool IsEnforcementKindEnabled(string kind)
        {
            switch (kind)
            {
                case EnforcementKinds.PublicTransportLane:
                    return Mod.IsPublicTransportLaneEnforcementEnabled;
                case EnforcementKinds.MidBlockCrossing:
                    return Mod.IsMidBlockCrossingEnforcementEnabled;
                case EnforcementKinds.IntersectionMovement:
                    return Mod.IsIntersectionMovementEnforcementEnabled;
                default:
                    return false;
            }
        }

        private static string BuildRepeatPolicyDebugSummary(bool enabled, int windowMonths, int threshold, int multiplierPercent)
        {
            return $"enabled={enabled}, window={FormatMonthCount(windowMonths)}, threshold={threshold}, multiplier={multiplierPercent}%";
        }

        private static string BuildRepeatPolicyLogSummary()
        {
            EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;
            if (!EnforcementGameTime.IsInitialized)
            {
                return null;
            }

            StringBuilder builder = new StringBuilder("Repeat-offender policy summary: ");
            AppendPolicySummary(builder, "PT-lane", settings.EnablePublicTransportLaneRepeatPenalty, settings.PublicTransportLaneRepeatWindowMonths, settings.PublicTransportLaneRepeatThreshold, settings.PublicTransportLaneRepeatMultiplierPercent);
            builder.Append("; ");
            AppendPolicySummary(builder, "mid-block", settings.EnableMidBlockCrossingRepeatPenalty, settings.MidBlockCrossingRepeatWindowMonths, settings.MidBlockCrossingRepeatThreshold, settings.MidBlockCrossingRepeatMultiplierPercent);
            builder.Append("; ");
            AppendPolicySummary(builder, "intersection", settings.EnableIntersectionMovementRepeatPenalty, settings.IntersectionMovementRepeatWindowMonths, settings.IntersectionMovementRepeatThreshold, settings.IntersectionMovementRepeatMultiplierPercent);
            builder.Append($"; timing basis: daysPerYear={EnforcementGameTime.CurrentDaysPerYear}, 12 in-game months = 1 in-game year, vanilla/default: 1 in-game month = 1 in-game day; mods changing day/month flow can break that equivalence.");
            return builder.ToString();
        }

        private static void AppendPolicySummary(StringBuilder builder, string label, bool enabled, int windowMonths, int threshold, int multiplierPercent)
        {
            builder.Append(label);
            builder.Append(" enabled=");
            builder.Append(enabled);
            builder.Append(", window=");
            builder.Append(FormatMonthCount(windowMonths));
            builder.Append(", threshold=");
            builder.Append(threshold);
            builder.Append(", multiplier=");
            builder.Append(multiplierPercent);
            builder.Append('%');
        }

        private static string FormatMonthCount(int months)
        {
            int clampedMonths = System.Math.Max(1, months);
            return clampedMonths == 1 ? "1 month" : $"{clampedMonths} months";
        }

        private readonly struct RepeatOffenderPolicy
        {
            public readonly bool Enabled;
            public readonly long WindowMonthTicks;
            public readonly int Threshold;
            public readonly int MultiplierPercent;

            public RepeatOffenderPolicy(bool enabled, long windowMonthTicks, int threshold, int multiplierPercent)
            {
                Enabled = enabled;
                WindowMonthTicks = windowMonthTicks;
                Threshold = threshold;
                MultiplierPercent = multiplierPercent;
            }
        }
    }
}
