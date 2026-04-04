using System.Collections.Generic;
using System.Text;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    internal struct SelectedObjectEnforcementSummaryContext
    {
        public ComponentLookup<VehicleTrafficLawProfile> ProfileData;
        public ComponentLookup<PublicTransportLanePermissionState> PermissionStateData;
    }

    internal readonly struct SelectedObjectEnforcementSummaryData
    {
        public readonly string CompactLastReasonText;
        public readonly string CompactRepeatPenaltyText;
        public readonly string PublicTransportLanePolicyText;
        public readonly string PermissionStateSummary;

        public SelectedObjectEnforcementSummaryData(
            string compactLastReasonText,
            string compactRepeatPenaltyText,
            string publicTransportLanePolicyText,
            string permissionStateSummary)
        {
            CompactLastReasonText = compactLastReasonText;
            CompactRepeatPenaltyText = compactRepeatPenaltyText;
            PublicTransportLanePolicyText = publicTransportLanePolicyText;
            PermissionStateSummary = permissionStateSummary;
        }
    }

    internal static class SelectedObjectEnforcementSummaryBuilder
    {
        private const int kReasonSummaryCacheLimit = 256;
        private const int kPolicyTextCacheLimit = 128;

        private static readonly Dictionary<string, CompactReasonSummary> s_CompactReasonSummaryCache =
            new Dictionary<string, CompactReasonSummary>();
        private static readonly Dictionary<int, string> s_PublicTransportLanePolicyTextCache =
            new Dictionary<int, string>();
        private static string s_CachedReasonSummaryLocaleId = string.Empty;
        private static string s_CachedPolicyTextLocaleId = string.Empty;

        private readonly struct IllegalEgressApplyMarkerReadResult
        {
            internal readonly bool MarkerPresent;
            internal readonly VehicleEnforcementRecord Record;

            internal IllegalEgressApplyMarkerReadResult(
                bool markerPresent,
                VehicleEnforcementRecord record)
            {
                MarkerPresent = markerPresent;
                Record = record;
            }
        }

        private readonly struct CompactReasonSummary
        {
            internal readonly string CompactLastReasonText;
            internal readonly string CompactRepeatPenaltyText;

            internal CompactReasonSummary(
                string compactLastReasonText,
                string compactRepeatPenaltyText)
            {
                CompactLastReasonText = compactLastReasonText;
                CompactRepeatPenaltyText = compactRepeatPenaltyText;
            }
        }

        internal static SelectedObjectEnforcementSummaryData Build(
            SelectedObjectResolveResult resolveResult,
            bool tleApplicable,
            bool hasTrafficLawProfile,
            Entity vehicle,
            string lastReason,
            bool includeDebugFields,
            ref SelectedObjectEnforcementSummaryContext context)
        {
            IllegalEgressApplyMarkerReadResult illegalEgressMarker =
                tleApplicable && vehicle != Entity.Null
                    ? ReadIllegalEgressApplyMarker(vehicle)
                    : default;
            LogIllegalEgressApplyMarkerRead(vehicle, illegalEgressMarker);

            string permissionStateSummary = tleApplicable && includeDebugFields
                ? BuildPermissionStateSummary(
                    vehicle,
                    hasTrafficLawProfile,
                    illegalEgressMarker,
                    ref context)
                : string.Empty;
            CompactReasonSummary compactReasonSummary = tleApplicable
                ? GetCompactReasonSummary(lastReason)
                : default;

            return new SelectedObjectEnforcementSummaryData(
                compactReasonSummary.CompactLastReasonText,
                compactReasonSummary.CompactRepeatPenaltyText,
                BuildPublicTransportLanePolicyText(resolveResult, hasTrafficLawProfile, ref context),
                permissionStateSummary);
        }

        private static CompactReasonSummary GetCompactReasonSummary(string lastReason)
        {
            string activeLocaleId = SelectedObjectLocalization.ActiveLocaleId;
            if (activeLocaleId != s_CachedReasonSummaryLocaleId)
            {
                s_CachedReasonSummaryLocaleId = activeLocaleId;
                s_CompactReasonSummaryCache.Clear();
            }

            string cacheKey = lastReason ?? string.Empty;
            if (s_CompactReasonSummaryCache.TryGetValue(cacheKey, out CompactReasonSummary cachedSummary))
            {
                return cachedSummary;
            }

            if (s_CompactReasonSummaryCache.Count >= kReasonSummaryCacheLimit)
            {
                s_CompactReasonSummaryCache.Clear();
            }

            string normalizedReason = NormalizeReasonText(cacheKey);
            SplitRepeatPenaltyReason(
                normalizedReason,
                out string reasonText,
                out string repeatPenaltyText);

            CompactReasonSummary summary = new CompactReasonSummary(
                BuildCompactLastReasonText(reasonText),
                BuildCompactRepeatPenaltyText(repeatPenaltyText));
            s_CompactReasonSummaryCache[cacheKey] = summary;
            return summary;
        }

        private static string BuildCompactLastReasonText(
            string lastReason)
        {
            string summarizedReason = string.IsNullOrWhiteSpace(lastReason)
                ? SelectedObjectLocalization.LocalizeText(
                    SelectedObjectBridgeSystem.kReasonNoneRecordedLocaleId,
                    "None recorded")
                : SummarizeReasonText(lastReason);

            const int maxLength = 56;
            if (summarizedReason.Length <= maxLength)
            {
                return summarizedReason;
            }

            return summarizedReason.Substring(0, maxLength - 3) + "...";
        }

        private static string BuildCompactRepeatPenaltyText(
            string repeatPenaltyText)
        {
            return string.IsNullOrWhiteSpace(repeatPenaltyText)
                ? SelectedObjectLocalization.LocalizeText(
                    SelectedObjectBridgeSystem.kReasonRepeatPenaltyNotAppliedLocaleId,
                    "Not applied")
                : repeatPenaltyText;
        }

        private static string BuildPublicTransportLanePolicyText(
            SelectedObjectResolveResult resolveResult,
            bool hasTrafficLawProfile,
            ref SelectedObjectEnforcementSummaryContext context)
        {
            if (!hasTrafficLawProfile ||
                !resolveResult.IsVehicle ||
                (resolveResult.VehicleKind != SelectedObjectKind.RoadCar &&
                 resolveResult.VehicleKind != SelectedObjectKind.ParkedRoadCar))
            {
                return string.Empty;
            }

            Entity vehicle = resolveResult.ResolvedVehicleEntity;
            if (vehicle == Entity.Null ||
                !context.ProfileData.TryGetComponent(vehicle, out VehicleTrafficLawProfile profile))
            {
                return string.Empty;
            }

            PublicTransportLaneAccessBits accessBits = profile.m_PublicTransportLaneAccessBits;
            bool emergencyVehicle = profile.m_EmergencyVehicle != 0;
            string activeLocaleId = SelectedObjectLocalization.ActiveLocaleId;
            if (activeLocaleId != s_CachedPolicyTextLocaleId)
            {
                s_CachedPolicyTextLocaleId = activeLocaleId;
                s_PublicTransportLanePolicyTextCache.Clear();
            }

            int cacheKey = ((int)accessBits << 1) | (emergencyVehicle ? 1 : 0);
            if (s_PublicTransportLanePolicyTextCache.TryGetValue(cacheKey, out string cachedPolicyText))
            {
                return cachedPolicyText;
            }

            if (s_PublicTransportLanePolicyTextCache.Count >= kPolicyTextCacheLimit)
            {
                s_PublicTransportLanePolicyTextCache.Clear();
            }

            string policyText = BuildPublicTransportLanePolicyText(accessBits, emergencyVehicle);
            s_PublicTransportLanePolicyTextCache[cacheKey] = policyText;
            return policyText;
        }

        private static string BuildPublicTransportLanePolicyText(
            PublicTransportLaneAccessBits accessBits,
            bool emergencyVehicle)
        {
            string type = PublicTransportLanePolicy.DescribeType(accessBits);
            bool vanillaAllow = PublicTransportLanePolicy.VanillaAllowsAccess(accessBits);
            bool modAllow = PublicTransportLanePolicy.ModAllowsAccess(accessBits);
            string meaningFormat = SelectedObjectLocalization.LocalizeText(
                SelectedObjectBridgeSystem.kPublicTransportLanePolicyMeaningFormatLocaleId,
                "{0} ({1}, {2})");
            string vanillaMeaning = SelectedObjectLocalization.LocalizeText(
                vanillaAllow
                    ? SelectedObjectBridgeSystem.kPublicTransportLanePolicyVanillaAllowLocaleId
                    : SelectedObjectBridgeSystem.kPublicTransportLanePolicyVanillaDenyLocaleId,
                vanillaAllow ? "Vanilla Allowed" : "Vanilla Denied");
            string tleMeaning = SelectedObjectLocalization.LocalizeText(
                modAllow
                    ? SelectedObjectBridgeSystem.kPublicTransportLanePolicyTleAllowLocaleId
                    : SelectedObjectBridgeSystem.kPublicTransportLanePolicyTleDenyLocaleId,
                modAllow ? "TLE Allowed" : "TLE Denied");
            string meaning = string.Format(meaningFormat, type, vanillaMeaning, tleMeaning);
            string qualifierSeparator = SelectedObjectLocalization.LocalizeText(
                SelectedObjectBridgeSystem.kPublicTransportLanePolicyQualifierSeparatorLocaleId,
                ", ");
            StringBuilder qualifiers = null;
            if (PublicTransportLanePolicy.ModPrefersLanes(accessBits))
            {
                (qualifiers ??= new StringBuilder(24)).Append(
                    SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kPublicTransportLanePolicyQualifierPublicTransportLocaleId,
                        "PT"));
            }

            if (emergencyVehicle)
            {
                if (qualifiers != null)
                {
                    qualifiers.Append(qualifierSeparator);
                }

                (qualifiers ??= new StringBuilder(24)).Append(
                    SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kPublicTransportLanePolicyQualifierEmergencyLocaleId,
                        "Emergency"));
            }

            return qualifiers == null || qualifiers.Length == 0
                ? meaning
                : string.Format(
                    SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kPublicTransportLanePolicyQualifiedFormatLocaleId,
                        "{0} [{1}]"),
                    meaning,
                    qualifiers.ToString());
        }

        private static string SummarizeReasonText(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return SelectedObjectLocalization.LocalizeText(
                    SelectedObjectBridgeSystem.kReasonNoneRecordedLocaleId,
                    "None recorded");
            }

            const string ptLaneRevokedPrefix = "PT-lane flags revoked by mod setting: ";
            const string ptLaneMissingVanillaPrefix = "PT-lane flags missing for vanilla-authorized categories: ";
            const string ptLaneMissingGrantedRolePrefix = "PT-lane flags missing for granted role: ";
            const string ptLaneNotGrantedRolePrefix = "PT-lane flags not granted for role: ";
            const string actualMovementPrefix = "actual ";
            const string allowedMovementDelimiter = ", allowed ";

            if (reason.StartsWith(ptLaneRevokedPrefix))
            {
                return string.Format(
                    SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kReasonPublicTransportLaneRevokedByModFormatLocaleId,
                        "PT-lane access revoked by mod: {0}"),
                    reason.Substring(ptLaneRevokedPrefix.Length));
            }

            if (reason.StartsWith(ptLaneMissingVanillaPrefix))
            {
                return string.Format(
                    SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kReasonPublicTransportLaneMissingVanillaCategoriesFormatLocaleId,
                        "Vanilla-authorized PT-lane flags missing: {0}"),
                    reason.Substring(ptLaneMissingVanillaPrefix.Length));
            }

            if (reason.StartsWith(ptLaneMissingGrantedRolePrefix))
            {
                return string.Format(
                    SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kReasonPublicTransportLaneMissingGrantedRoleFormatLocaleId,
                        "Granted role missing PT-lane flags: {0}"),
                    reason.Substring(ptLaneMissingGrantedRolePrefix.Length));
            }

            if (reason.StartsWith(ptLaneNotGrantedRolePrefix))
            {
                return string.Format(
                    SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kReasonPublicTransportLaneNotGrantedRoleFormatLocaleId,
                        "PT-lane not granted for role: {0}"),
                    reason.Substring(ptLaneNotGrantedRolePrefix.Length));
            }

            if (reason == "vehicle has no PT-lane permission flags")
            {
                return SelectedObjectLocalization.LocalizeText(
                    SelectedObjectBridgeSystem.kReasonNoPublicTransportLanePermissionFlagsLocaleId,
                    "Vehicle has no PT-lane permission flags");
            }

            switch (reason)
            {
                case "vehicle switched to the opposite flow on the same road segment":
                    return SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kReasonOppositeFlowSameSegmentLocaleId,
                        "Switched to opposite flow on the same segment");
                case "vehicle entered garage access from a lane without side-access permission":
                    return SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kReasonEnteredGarageAccessNoSideAccessLocaleId,
                        "Entered garage access without side access");
                case "vehicle entered parking access from a lane without side-access permission":
                    return SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kReasonEnteredParkingAccessNoSideAccessLocaleId,
                        "Entered parking access without side access");
                case "vehicle crossed into parking connection from a lane without side-access permission":
                    return SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kReasonEnteredParkingConnectionNoSideAccessLocaleId,
                        "Entered parking connection without side access");
                case "vehicle crossed into building/service access connection from a lane without side-access permission":
                    return SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kReasonEnteredBuildingAccessNoSideAccessLocaleId,
                        "Entered building/service access without side access");
                case "vehicle exited parking access into a lane without side-access permission":
                    return SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kReasonExitedParkingAccessNoSideAccessLocaleId,
                        "Exited parking access without side access");
                case "vehicle exited garage access into a lane without side-access permission":
                    return SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kReasonExitedGarageAccessNoSideAccessLocaleId,
                        "Exited garage access without side access");
                case "vehicle exited parking connection into a lane without side-access permission":
                    return SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kReasonExitedParkingConnectionNoSideAccessLocaleId,
                        "Exited parking connection without side access");
                case "vehicle exited building/service access connection into a lane without side-access permission":
                    return SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kReasonExitedBuildingAccessNoSideAccessLocaleId,
                        "Exited building/service access without side access");
            }

            if (reason.StartsWith(actualMovementPrefix) &&
                reason.Contains(allowedMovementDelimiter))
            {
                int delimiterIndex = reason.IndexOf(allowedMovementDelimiter);
                string actualMovement = reason.Substring(
                    actualMovementPrefix.Length,
                    delimiterIndex - actualMovementPrefix.Length);
                string allowedMovement = reason.Substring(
                    delimiterIndex + allowedMovementDelimiter.Length);
                return string.Format(
                    SelectedObjectLocalization.LocalizeText(
                        SelectedObjectBridgeSystem.kReasonIntersectionMovementFormatLocaleId,
                        "Actual {0}, allowed {1}"),
                    actualMovement,
                    allowedMovement);
            }

            return reason;
        }

        private static string NormalizeReasonText(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return string.Empty;
            }

            return reason
                .Replace("public-transport-lane", "PT-lane")
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();
        }

        private static string BuildRepeatPenaltySummaryText(int baseFine, int adjustedFine)
        {
            return string.Format(
                SelectedObjectLocalization.LocalizeText(
                    SelectedObjectBridgeSystem.kReasonRepeatPenaltyAppliedFormatLocaleId,
                    "Repeat offender {0} -> {1}"),
                baseFine,
                adjustedFine);
        }

        private static string BuildRepeatPenaltySummaryText()
        {
            return SelectedObjectLocalization.LocalizeText(
                SelectedObjectBridgeSystem.kReasonRepeatPenaltyAppliedLocaleId,
                "Repeat offender multiplier applied");
        }

        private static void SplitRepeatPenaltyReason(
            string normalizedReason,
            out string baseReason,
            out string repeatPenaltyText)
        {
            const string marker = " Repeat offender multiplier applied: ";

            repeatPenaltyText = string.Empty;
            baseReason = normalizedReason;

            if (string.IsNullOrWhiteSpace(normalizedReason))
            {
                return;
            }

            int markerIndex = normalizedReason.IndexOf(marker);
            if (markerIndex < 0)
            {
                return;
            }

            baseReason = normalizedReason.Substring(0, markerIndex).Trim();

            string repeatPayload = normalizedReason.Substring(markerIndex + marker.Length).Trim();
            repeatPayload = repeatPayload.TrimEnd('.');

            string[] parts = repeatPayload.Split(new[] { " -> " }, System.StringSplitOptions.None);
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int baseFine) &&
                int.TryParse(parts[1], out int adjustedFine))
            {
                repeatPenaltyText = BuildRepeatPenaltySummaryText(baseFine, adjustedFine);
                return;
            }

            repeatPenaltyText = BuildRepeatPenaltySummaryText();
        }

        private static string BuildPermissionStateSummary(
            Entity vehicle,
            bool hasTrafficLawProfile,
            IllegalEgressApplyMarkerReadResult illegalEgressMarker,
            ref SelectedObjectEnforcementSummaryContext context)
        {
            if (vehicle == Entity.Null)
            {
                return string.Empty;
            }

            string baseSummary;
            if (!context.PermissionStateData.TryGetComponent(
                    vehicle,
                    out PublicTransportLanePermissionState permissionState))
            {
                baseSummary = hasTrafficLawProfile
                    ? "No active permission state"
                    : "Not tracked by Traffic Law Enforcement";
                return AppendIllegalEgressDebugSummary(baseSummary, illegalEgressMarker);
            }

            PublicTransportLaneAccessBits accessBits =
                permissionState.m_PublicTransportLaneAccessBits;

            bool emergencyActive =
                permissionState.m_EmergencyActive != 0;

            bool emergencyOverrideActive =
                PublicTransportLanePolicy.HasEmergencyPublicTransportLaneOverride(
                    accessBits,
                    emergencyActive);

            baseSummary =
                $"type={PublicTransportLanePolicy.DescribeType(accessBits)}, " +
                $"vanillaAllow={PublicTransportLanePolicy.VanillaAllowsAccess(accessBits)}, " +
                $"modAllow={PublicTransportLanePolicy.ModAllowsAccess(accessBits)}, " +
                $"canUsePublicTransportLane={PublicTransportLanePolicy.CanUsePublicTransportLane(accessBits, emergencyActive)}, " +
                $"vanillaPrefer={PublicTransportLanePolicy.VanillaPrefersLanes(accessBits)}, " +
                $"modPrefer={PublicTransportLanePolicy.ModPrefersLanes(accessBits)}, " +
                $"changedByMod={PublicTransportLanePolicy.PermissionChangedByMod(accessBits)}, " +
                $"emergency={emergencyActive}, " +
                $"emergencyOverride={emergencyOverrideActive}, " +
                $"graceConsumed={permissionState.m_ImmediateEntryGraceConsumed != 0}";

            return AppendIllegalEgressDebugSummary(baseSummary, illegalEgressMarker);
        }

        private static string AppendIllegalEgressDebugSummary(
            string baseSummary,
            IllegalEgressApplyMarkerReadResult illegalEgressMarker)
        {
            if (!illegalEgressMarker.MarkerPresent)
            {
                return baseSummary;
            }

            VehicleEnforcementRecord record = illegalEgressMarker.Record;
            string debugSummary =
                "ordinaryEgressApply=" +
                $"mode={record.LastAppliedIllegalEgressMode}, " +
                $"timestampMonthTicks={record.LastAppliedIllegalEgressTimestampMonthTicks}, " +
                $"originLaneId={record.LastAppliedIllegalEgressOriginLaneId}, " +
                $"roadLaneId={record.LastAppliedIllegalEgressRoadLaneId}";

            return string.IsNullOrWhiteSpace(baseSummary)
                ? debugSummary
                : $"{baseSummary} | {debugSummary}";
        }

        private static IllegalEgressApplyMarkerReadResult ReadIllegalEgressApplyMarker(
            Entity vehicle)
        {
            bool recordFound =
                EnforcementTelemetry.TryGetVehicleEnforcementRecord(
                    vehicle.Index,
                    out VehicleEnforcementRecord record);
            bool markerPresent =
                recordFound &&
                record.LastAppliedIllegalEgressMode != IllegalEgressApplyMode.None;

            return new IllegalEgressApplyMarkerReadResult(
                markerPresent,
                recordFound
                    ? record
                    : default);
        }

        private static void LogIllegalEgressApplyMarkerRead(
            Entity vehicle,
            IllegalEgressApplyMarkerReadResult illegalEgressMarker)
        {
            if (vehicle == Entity.Null ||
                !EnforcementLoggingPolicy.ShouldLogEnforcementEvents())
            {
                return;
            }

            VehicleEnforcementRecord record = illegalEgressMarker.Record;

            string message =
                "[ACCESS_EGRESS_APPLY_MARKER_READ] " +
                $"vehicle={FocusedLoggingService.FormatEntity(vehicle)} " +
                $"markerPresent={illegalEgressMarker.MarkerPresent} " +
                $"mode={(illegalEgressMarker.MarkerPresent ? record.LastAppliedIllegalEgressMode : IllegalEgressApplyMode.None)} " +
                $"timestampMonthTicks={(illegalEgressMarker.MarkerPresent ? record.LastAppliedIllegalEgressTimestampMonthTicks : 0L)} " +
                $"originLaneId={(illegalEgressMarker.MarkerPresent ? record.LastAppliedIllegalEgressOriginLaneId : -1)} " +
                $"roadLaneId={(illegalEgressMarker.MarkerPresent ? record.LastAppliedIllegalEgressRoadLaneId : -1)}";

            Mod.log.Info(message);
        }
    }
}
