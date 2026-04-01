using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    public static class EnforcementPenaltyService
    {
        public const int DefaultPublicTransportLaneFine = 250;
        public const int DefaultMidBlockCrossingFine = 250;
        public const int DefaultIntersectionMovementFine = 250;
        private static int s_RepeatPolicySummaryVersion = -1;
        private static string s_PublicTransportLaneRepeatPolicySummary = string.Empty;
        private static string s_MidBlockCrossingRepeatPolicySummary = string.Empty;
        private static string s_IntersectionMovementRepeatPolicySummary = string.Empty;

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
            return EnforcementGameplaySettingsService.Current.GetEffectivePublicTransportLaneFineAmount();
        }

        public static int GetMidBlockCrossingFine()
        {
            return EnforcementGameplaySettingsService.Current.GetEffectiveMidBlockCrossingFineAmount();
        }

        public static int GetIntersectionMovementFine()
        {
            return EnforcementGameplaySettingsService.Current.GetEffectiveIntersectionMovementFineAmount();
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
            EnsureRepeatPolicySummaryCache();

            switch (kind)
            {
                case EnforcementKinds.PublicTransportLane:
                    return s_PublicTransportLaneRepeatPolicySummary;
                case EnforcementKinds.MidBlockCrossing:
                    return s_MidBlockCrossingRepeatPolicySummary;
                case EnforcementKinds.IntersectionMovement:
                    return s_IntersectionMovementRepeatPolicySummary;
                default:
                    return "unknown policy";
            }
        }

        private static void EnsureRepeatPolicySummaryCache()
        {
            int settingsVersion = EnforcementGameplaySettingsService.Version;
            if (s_RepeatPolicySummaryVersion == settingsVersion)
            {
                return;
            }

            EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;
            s_PublicTransportLaneRepeatPolicySummary =
                BuildRepeatPolicyDebugSummary(
                    settings.IsPublicTransportLaneRepeatPenaltyEffectivelyEnabled(),
                    settings.PublicTransportLaneRepeatWindowMonths,
                    settings.PublicTransportLaneRepeatThreshold,
                    settings.PublicTransportLaneRepeatMultiplierPercent);
            s_MidBlockCrossingRepeatPolicySummary =
                BuildRepeatPolicyDebugSummary(
                    settings.IsMidBlockCrossingRepeatPenaltyEffectivelyEnabled(),
                    settings.MidBlockCrossingRepeatWindowMonths,
                    settings.MidBlockCrossingRepeatThreshold,
                    settings.MidBlockCrossingRepeatMultiplierPercent);
            s_IntersectionMovementRepeatPolicySummary =
                BuildRepeatPolicyDebugSummary(
                    settings.IsIntersectionMovementRepeatPenaltyEffectivelyEnabled(),
                    settings.IntersectionMovementRepeatWindowMonths,
                    settings.IntersectionMovementRepeatThreshold,
                    settings.IntersectionMovementRepeatMultiplierPercent);
            s_RepeatPolicySummaryVersion = settingsVersion;
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
            EnforcementPolicyImpactService.RecordActualViolation(kind, adjustedFine, vehicle.Index);
            EnforcementFineMoneyService.EnqueueCharge(vehicle, adjustedFine, kind);
        }

        private static RepeatOffenderPolicy GetRepeatOffenderPolicy(string kind)
        {
            EnforcementGameplaySettingsState settings = EnforcementGameplaySettingsService.Current;

            switch (kind)
            {
                case EnforcementKinds.PublicTransportLane:
                    return new RepeatOffenderPolicy(
                        settings.IsPublicTransportLaneRepeatPenaltyEffectivelyEnabled(),
                        EnforcementGameTime.GetMonthTickWindow(settings.PublicTransportLaneRepeatWindowMonths),
                        settings.PublicTransportLaneRepeatThreshold,
                        settings.PublicTransportLaneRepeatMultiplierPercent);
                case EnforcementKinds.MidBlockCrossing:
                    return new RepeatOffenderPolicy(
                        settings.IsMidBlockCrossingRepeatPenaltyEffectivelyEnabled(),
                        EnforcementGameTime.GetMonthTickWindow(settings.MidBlockCrossingRepeatWindowMonths),
                        settings.MidBlockCrossingRepeatThreshold,
                        settings.MidBlockCrossingRepeatMultiplierPercent);
                case EnforcementKinds.IntersectionMovement:
                    return new RepeatOffenderPolicy(
                        settings.IsIntersectionMovementRepeatPenaltyEffectivelyEnabled(),
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
