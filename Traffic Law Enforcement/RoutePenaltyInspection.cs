using System.Text;
using System.Collections.Generic;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Vehicles;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    internal struct RoutePenaltyInspectionContext
    {
        public EntityManager EntityManager;
        public ComponentLookup<CarLane> CarLaneData;
        public ComponentLookup<EdgeLane> EdgeLaneData;
        public ComponentLookup<ParkingLane> ParkingLaneData;
        public ComponentLookup<GarageLane> GarageLaneData;
        public ComponentLookup<ConnectionLane> ConnectionLaneData;
        public ComponentLookup<VehicleTrafficLawProfile> ProfileData;
        public PublicTransportLaneVehicleTypeLookups TypeLookups;
    }

    internal struct RoutePenaltyProfile
    {
        public int PublicTransportLaneSegments;
        public int MidBlockTransitions;
        public int IntersectionTransitions;

        public bool HasAnyPenalty =>
            PublicTransportLaneSegments > 0 ||
            MidBlockTransitions > 0 ||
            IntersectionTransitions > 0;
    }

    internal readonly struct RoutePenaltyTagSnapshot
    {
        public readonly int Count;
        public readonly int OmittedCount;
        public readonly int Token0;
        public readonly int Token1;
        public readonly int Token2;
        public readonly int Token3;
        public readonly int Token4;
        public readonly int Token5;

        public RoutePenaltyTagSnapshot(
            int count,
            int omittedCount,
            int token0,
            int token1,
            int token2,
            int token3,
            int token4,
            int token5)
        {
            Count = count;
            OmittedCount = omittedCount;
            Token0 = token0;
            Token1 = token1;
            Token2 = token2;
            Token3 = token3;
            Token4 = token4;
            Token5 = token5;
        }

        public int GetToken(int index)
        {
            return index switch
            {
                0 => Token0,
                1 => Token1,
                2 => Token2,
                3 => Token3,
                4 => Token4,
                5 => Token5,
                _ => 0,
            };
        }
    }

    internal readonly struct RoutePenaltyInspectionResult
    {
        public readonly uint RouteHash;
        public readonly RoutePenaltyProfile Profile;
        public readonly int TotalPenalty;
        public readonly string Breakdown;
        public readonly RoutePenaltyTagSnapshot TagSnapshot;
        public readonly RoutePenaltyTagSnapshot NormalizedTagSnapshot;
        public readonly bool PublicTransportLanePolicyResolved;
        public readonly bool AllowedOnPublicTransportLane;

        public RoutePenaltyInspectionResult(
            uint routeHash,
            RoutePenaltyProfile profile,
            string breakdown,
            RoutePenaltyTagSnapshot tagSnapshot,
            RoutePenaltyTagSnapshot normalizedTagSnapshot,
            bool publicTransportLanePolicyResolved,
            bool allowedOnPublicTransportLane)
        {
            RouteHash = routeHash;
            Profile = profile;
            TotalPenalty = RoutePenaltyInspection.CalculateTotalPenalty(profile);
            Breakdown = breakdown;
            TagSnapshot = tagSnapshot;
            NormalizedTagSnapshot = normalizedTagSnapshot;
            PublicTransportLanePolicyResolved = publicTransportLanePolicyResolved;
            AllowedOnPublicTransportLane = allowedOnPublicTransportLane;
        }
    }

    internal static class RoutePenaltyInspection
    {
        internal const int DefaultMaxPenaltyTags = 6;
        private const uint kInitialRouteHash = 2166136261u;
        private const int TagKindShift = 24;
        private const int TagPayloadMask = 0x00FFFFFF;
        private const int TagKindMidBlock = 1;
        private const int TagKindIntersection = 2;
        private const int TagKindUnauthorizedPublicTransportLane = 3;
        private const int LaneKindTokenLane = 1;
        private const int LaneKindTokenRoad = 2;
        private const int LaneKindTokenParkingLane = 3;
        private const int LaneKindTokenGarageLane = 4;
        private const int LaneKindTokenAccessConnection = 5;
        private const int LaneKindTokenIntersectionBase = 0x100;
        [System.ThreadStatic]
        private static List<Entity> s_TemporaryLaneSequence;

        private enum NormalizedAccessConnectionKind : byte
        {
            None = 0,
            Parking = 1,
            NonRoad = 2,
        }

        private enum NormalizedAccessEndpointKind : byte
        {
            None = 0,
            Parking = 1,
            Garage = 2,
        }

        private enum AccessWindowBlockedReason : byte
        {
            None = 0,
            NoThreeLaneWindowInCurrentSequence = 1,
            NoAccessConnectionInCurrentSequence = 2,
            MiddleRoadConnectionExcluded = 3,
            MissingExplicitEndpoint = 4,
            ExactAnchorPairNotIllegal = 5,
        }

        private struct RoutePenaltyTagCollector
        {
            public int Count;
            public int OmittedCount;
            public int Token0;
            public int Token1;
            public int Token2;
            public int Token3;
            public int Token4;
            public int Token5;

            public int GetToken(int index)
            {
                return index switch
                {
                    0 => Token0,
                    1 => Token1,
                    2 => Token2,
                    3 => Token3,
                    4 => Token4,
                    5 => Token5,
                    _ => 0,
                };
            }

            public void Append(int token, int maxPenaltyTags)
            {
                if (token == 0)
                {
                    return;
                }

                for (int index = 0; index < Count; index += 1)
                {
                    if (GetToken(index) == token)
                    {
                        return;
                    }
                }

                if (Count >= maxPenaltyTags)
                {
                    OmittedCount += 1;
                    return;
                }

                switch (Count)
                {
                    case 0:
                        Token0 = token;
                        break;
                    case 1:
                        Token1 = token;
                        break;
                    case 2:
                        Token2 = token;
                        break;
                    case 3:
                        Token3 = token;
                        break;
                    case 4:
                        Token4 = token;
                        break;
                    case 5:
                        Token5 = token;
                        break;
                }

                Count += 1;
            }

            public RoutePenaltyTagSnapshot ToSnapshot()
            {
                return new RoutePenaltyTagSnapshot(
                    Count,
                    OmittedCount,
                    Token0,
                    Token1,
                    Token2,
                    Token3,
                    Token4,
                    Token5);
            }
        }

        internal static RoutePenaltyInspectionResult InspectCurrentRoute(
            Entity vehicle,
            Entity currentLane,
            DynamicBuffer<CarNavigationLane> navigationLanes,
            bool hasNavigationLanes,
            ref RoutePenaltyInspectionContext context,
            bool captureBreakdown,
            bool captureTagSummary,
            int maxPenaltyTags = DefaultMaxPenaltyTags)
        {
            RoutePenaltyProfile profile = default;
            bool publicTransportLanePolicyResolved =
                TryResolveAllowedOnPublicTransportLane(
                    vehicle,
                    ref context,
                    out bool allowedOnPublicTransportLane);

            RoutePenaltyTagCollector penaltyTags = default;
            uint hash = kInitialRouteHash;
            bool previousUnauthorizedPublicTransportLane = false;
            Entity previousLane = Entity.Null;

            if (!captureTagSummary)
            {
                AppendLaneToProfile(
                    currentLane,
                    publicTransportLanePolicyResolved,
                    allowedOnPublicTransportLane,
                    ref previousLane,
                    ref previousUnauthorizedPublicTransportLane,
                    ref profile,
                    ref hash,
                    ref penaltyTags,
                    ref context,
                    captureTagSummary: false,
                    maxPenaltyTags);

                if (hasNavigationLanes)
                {
                    Entity previousSequenceLane = previousLane;
                    for (int index = 0; index < navigationLanes.Length; index += 1)
                    {
                        Entity nextLane = navigationLanes[index].m_Lane;
                        if (nextLane == Entity.Null ||
                            nextLane == previousSequenceLane)
                        {
                            continue;
                        }

                        AppendLaneToProfile(
                            nextLane,
                            publicTransportLanePolicyResolved,
                            allowedOnPublicTransportLane,
                            ref previousLane,
                            ref previousUnauthorizedPublicTransportLane,
                            ref profile,
                            ref hash,
                            ref penaltyTags,
                            ref context,
                            captureTagSummary: false,
                            maxPenaltyTags);
                        previousSequenceLane = nextLane;
                    }
                }

                return new RoutePenaltyInspectionResult(
                    hash,
                    profile,
                    captureBreakdown ? BuildBreakdown(profile) : null,
                    default,
                    default,
                    publicTransportLanePolicyResolved,
                    allowedOnPublicTransportLane);
            }

            RoutePenaltyTagCollector normalizedPenaltyTags = default;
            List<Entity> laneSequence =
                AcquireTemporaryLaneSequence(
                    hasNavigationLanes ? navigationLanes.Length + 1 : 1);

            try
            {
                BuildLaneSequence(
                    currentLane,
                    navigationLanes,
                    hasNavigationLanes,
                    laneSequence);

                for (int index = 0; index < laneSequence.Count; index += 1)
                {
                    AppendLaneToProfile(
                        laneSequence[index],
                        publicTransportLanePolicyResolved,
                        allowedOnPublicTransportLane,
                        ref previousLane,
                        ref previousUnauthorizedPublicTransportLane,
                        ref profile,
                        ref hash,
                        ref penaltyTags,
                        ref context,
                        captureTagSummary: true,
                        maxPenaltyTags);
                }

                AppendNormalizedAccessTags(
                    laneSequence,
                    ref normalizedPenaltyTags,
                    ref context,
                    maxPenaltyTags);

                return new RoutePenaltyInspectionResult(
                    hash,
                    profile,
                    captureBreakdown ? BuildBreakdown(profile) : null,
                    penaltyTags.ToSnapshot(),
                    normalizedPenaltyTags.ToSnapshot(),
                    publicTransportLanePolicyResolved,
                    allowedOnPublicTransportLane);
            }
            finally
            {
                laneSequence.Clear();
            }
        }

        internal static string BuildNavigationPreview(
            Entity currentLane,
            DynamicBuffer<CarNavigationLane> navigationLanes,
            bool hasNavigationLanes,
            ref RoutePenaltyInspectionContext context,
            int maxPreviewLanes = 4)
        {
            if (!hasNavigationLanes || navigationLanes.Length == 0)
            {
                return "none";
            }

            int totalUpcoming = 0;
            int previewCount = 0;
            StringBuilder preview = new StringBuilder(96);

            for (int index = 0; index < navigationLanes.Length; index++)
            {
                Entity lane = navigationLanes[index].m_Lane;
                if (lane == Entity.Null)
                {
                    continue;
                }

                if (index == 0 && lane == currentLane)
                {
                    continue;
                }

                totalUpcoming += 1;
                if (previewCount >= maxPreviewLanes)
                {
                    continue;
                }

                if (preview.Length > 0)
                {
                    preview.Append(" -> ");
                }

                preview
                    .Append(FormatEntity(lane))
                    .Append(' ')
                    .Append(DescribeLaneKind(lane, ref context));
                previewCount += 1;
            }

            if (totalUpcoming == 0)
            {
                return "none";
            }

            StringBuilder summary = new StringBuilder(128);
            summary.Append(totalUpcoming).Append(" total");
            if (preview.Length > 0)
            {
                summary.Append(": ").Append(preview);
            }

            if (totalUpcoming > previewCount)
            {
                summary
                    .Append(" (+")
                    .Append(totalUpcoming - previewCount)
                    .Append(" more)");
            }

            return summary.ToString();
        }

        // Diagnostic-only summary of what the current inspection lane sequence
        // contains for exact and normalized illegal-access evidence.
        internal static string BuildFocusedAccessWindowDiagnostic(
            Entity vehicle,
            Entity currentLane,
            DynamicBuffer<CarNavigationLane> navigationLanes,
            bool hasNavigationLanes,
            ref RoutePenaltyInspectionContext context)
        {
            List<Entity> laneSequence =
                AcquireTemporaryLaneSequence(
                    hasNavigationLanes ? navigationLanes.Length + 1 : 1);

            try
            {
                BuildLaneSequence(
                    currentLane,
                    navigationLanes,
                    hasNavigationLanes,
                    laneSequence);

                FindFirstExactIllegalAccessPair(
                    laneSequence,
                    out Entity exactSource,
                    out Entity exactTarget,
                    out LaneTransitionViolationReasonCode exactReasonCode,
                    ref context);

                AccessWindowBlockedReason blockedReason =
                    FindFirstNormalizedAccessWindow(
                        laneSequence,
                        out Entity normalizedPrevious,
                        out Entity normalizedCurrent,
                        out Entity normalizedNext,
                        out LaneTransitionViolationReasonCode normalizedReasonCode,
                        out Entity blockedPrevious,
                        out Entity blockedCurrent,
                        out Entity blockedNext,
                        ref context);

                string exactFirst =
                    exactReasonCode == LaneTransitionViolationReasonCode.None
                        ? "none"
                        : $"{FormatEntity(exactSource)}->{FormatEntity(exactTarget)}:{exactReasonCode}";

                string normalizedFirst =
                    FormatNormalizedAccessMatch(
                        normalizedPrevious,
                        normalizedCurrent,
                        normalizedNext,
                        normalizedReasonCode);

                string normalizedBlocked =
                    normalizedReasonCode != LaneTransitionViolationReasonCode.None
                        ? "none"
                        : FormatBlockedReason(blockedReason, blockedPrevious, blockedCurrent, blockedNext);

                return
                    $"[FOCUSED_ACCESS_WINDOW] vehicle={FormatEntity(vehicle)}, " +
                    $"exactFirst={exactFirst}, " +
                    $"normalizedFirst={normalizedFirst}, " +
                    $"normalizedBlocked={normalizedBlocked}";
            }
            finally
            {
                laneSequence.Clear();
            }
        }

        internal static bool TryResolveAllowedOnPublicTransportLane(
            Entity vehicle,
            ref RoutePenaltyInspectionContext context,
            out bool allowedOnPublicTransportLane)
        {
            if (!context.ProfileData.TryGetComponent(vehicle, out VehicleTrafficLawProfile profile))
            {
                allowedOnPublicTransportLane =
                    EmergencyVehiclePolicy.IsEmergencyVehicle(vehicle, ref context.TypeLookups);
                return allowedOnPublicTransportLane;
            }

            allowedOnPublicTransportLane =
                PublicTransportLanePolicy.CanUsePublicTransportLane(profile);
            return true;
        }

        internal static bool IsUnauthorizedPublicTransportLane(
            Entity lane,
            bool allowedOnPublicTransportLane,
            ref RoutePenaltyInspectionContext context)
        {
            if (lane == Entity.Null ||
                !context.CarLaneData.TryGetComponent(lane, out CarLane laneData))
            {
                return false;
            }

            if ((laneData.m_Flags & Game.Net.CarLaneFlags.PublicOnly) == 0)
            {
                return false;
            }

            return !allowedOnPublicTransportLane;
        }

        internal static bool TryGetMidBlockPenaltyTag(
            Entity sourceLane,
            Entity targetLane,
            out string tag,
            ref RoutePenaltyInspectionContext context)
        {
            tag = null;

            if (!MidBlockCrossingPolicy.TryGetIllegalTransition(
                    context.EntityManager,
                    sourceLane,
                    targetLane,
                    out LaneTransitionViolationReasonCode reasonCode))
            {
                return false;
            }

            tag = $"mid-block({FormatMidBlockReasonTag(reasonCode)})";
            return true;
        }

        private static bool TryGetMidBlockPenaltyToken(
            Entity sourceLane,
            Entity targetLane,
            out int token,
            ref RoutePenaltyInspectionContext context)
        {
            token = 0;

            if (!MidBlockCrossingPolicy.TryGetIllegalTransition(
                    context.EntityManager,
                    sourceLane,
                    targetLane,
                    out LaneTransitionViolationReasonCode reasonCode))
            {
                return false;
            }

            token = EncodeMidBlockPenaltyTag(reasonCode);
            return true;
        }

        internal static string FormatMidBlockReasonTag(
            LaneTransitionViolationReasonCode reasonCode)
        {
            switch (reasonCode)
            {
                case LaneTransitionViolationReasonCode.OppositeFlowSameRoadSegment:
                    return "opposite-flow";

                case LaneTransitionViolationReasonCode.EnteredGarageAccessWithoutSideAccess:
                    return "garage-access-ingress";

                case LaneTransitionViolationReasonCode.EnteredParkingAccessWithoutSideAccess:
                    return "parking-access-ingress";

                case LaneTransitionViolationReasonCode.EnteredParkingConnectionWithoutSideAccess:
                    return "parking-connection-ingress";

                case LaneTransitionViolationReasonCode.EnteredBuildingAccessConnectionWithoutSideAccess:
                    return "building-service-access-ingress";

                case LaneTransitionViolationReasonCode.ExitedParkingAccessWithoutSideAccess:
                    return "parking-access-egress";

                case LaneTransitionViolationReasonCode.ExitedGarageAccessWithoutSideAccess:
                    return "garage-access-egress";

                case LaneTransitionViolationReasonCode.ExitedParkingConnectionWithoutSideAccess:
                    return "parking-connection-egress";

                case LaneTransitionViolationReasonCode.ExitedBuildingAccessConnectionWithoutSideAccess:
                    return "building-service-access-egress";

                default:
                    return "illegal-transition";
            }
        }

        internal static bool TryGetIntersectionPenaltyTag(
            Entity sourceLane,
            Entity targetLane,
            out string tag,
            ref RoutePenaltyInspectionContext context)
        {
            tag = null;

            if (!IntersectionMovementPolicy.TryGetIllegalIntersectionMovement(
                    context.ConnectionLaneData,
                    context.CarLaneData,
                    sourceLane,
                    targetLane,
                    out LaneMovement actualMovement,
                    out LaneMovement allowedMovement))
            {
                return false;
            }

            tag =
                $"intersection(illegal {IntersectionMovementPolicy.FormatMovement(actualMovement)}; allowed {IntersectionMovementPolicy.FormatMovement(allowedMovement)})";
            return true;
        }

        private static bool TryGetIntersectionPenaltyToken(
            Entity sourceLane,
            Entity targetLane,
            out int token,
            ref RoutePenaltyInspectionContext context)
        {
            token = 0;

            if (!IntersectionMovementPolicy.TryGetIllegalIntersectionMovement(
                    context.ConnectionLaneData,
                    context.CarLaneData,
                    sourceLane,
                    targetLane,
                    out LaneMovement actualMovement,
                    out LaneMovement allowedMovement))
            {
                return false;
            }

            token = EncodeIntersectionPenaltyTag(actualMovement, allowedMovement);
            return true;
        }

        internal static string DescribeLaneKind(
            Entity lane,
            ref RoutePenaltyInspectionContext context)
        {
            return DescribeLaneKindToken(EncodeLaneKindToken(lane, ref context));
        }

        internal static string BuildBreakdown(RoutePenaltyProfile profile)
        {
            StringBuilder parts = new StringBuilder(64);
            if (profile.PublicTransportLaneSegments > 0)
            {
                parts
                    .Append("PT-lane ")
                    .Append(profile.PublicTransportLaneSegments)
                    .Append(" x ")
                    .Append(EnforcementPenaltyService.GetPublicTransportLaneFine());
            }

            if (profile.MidBlockTransitions > 0)
            {
                AppendBreakdownPart(
                    parts,
                    "mid-block ",
                    profile.MidBlockTransitions,
                    EnforcementPenaltyService.GetMidBlockCrossingFine());
            }

            if (profile.IntersectionTransitions > 0)
            {
                AppendBreakdownPart(
                    parts,
                    "intersection ",
                    profile.IntersectionTransitions,
                    EnforcementPenaltyService.GetIntersectionMovementFine());
            }

            return parts.Length == 0
                ? "none"
                : parts.ToString();
        }

        internal static string BuildTagSummary(RoutePenaltyTagSnapshot tagSnapshot)
        {
            if (tagSnapshot.Count == 0)
            {
                return "none";
            }

            StringBuilder summary = new StringBuilder(96);
            for (int index = 0; index < tagSnapshot.Count; index += 1)
            {
                if (index > 0)
                {
                    summary.Append("; ");
                }

                summary.Append(FormatTagToken(tagSnapshot.GetToken(index)));
            }

            if (tagSnapshot.OmittedCount > 0)
            {
                summary
                    .Append("; ... (+")
                    .Append(tagSnapshot.OmittedCount)
                    .Append(" more)");
            }

            return summary.ToString();
        }

        private static int EncodeMidBlockPenaltyTag(
            LaneTransitionViolationReasonCode reasonCode)
        {
            return (TagKindMidBlock << TagKindShift) | ((int)reasonCode & TagPayloadMask);
        }

        private static int EncodeIntersectionPenaltyTag(
            LaneMovement actualMovement,
            LaneMovement allowedMovement)
        {
            int payload =
                ((int)actualMovement & 0xFF) |
                (((int)allowedMovement & 0xFF) << 8);
            return (TagKindIntersection << TagKindShift) | (payload & TagPayloadMask);
        }

        private static int EncodeUnauthorizedPublicTransportLaneTag(
            Entity lane,
            ref RoutePenaltyInspectionContext context)
        {
            int payload = EncodeLaneKindToken(lane, ref context);
            return (TagKindUnauthorizedPublicTransportLane << TagKindShift) |
                (payload & TagPayloadMask);
        }

        private static string FormatTagToken(int token)
        {
            int tagKind = token >> TagKindShift;
            int payload = token & TagPayloadMask;

            return tagKind switch
            {
                TagKindMidBlock =>
                    $"mid-block({FormatMidBlockReasonTag((LaneTransitionViolationReasonCode)payload)})",
                TagKindIntersection =>
                    BuildIntersectionPenaltyTagText(payload),
                TagKindUnauthorizedPublicTransportLane =>
                    DescribeLaneKindToken(payload) + "(public-only, illegal)",
                _ => "unknown",
            };
        }

        private static string BuildIntersectionPenaltyTagText(int payload)
        {
            LaneMovement actualMovement = (LaneMovement)(payload & 0xFF);
            LaneMovement allowedMovement = (LaneMovement)((payload >> 8) & 0xFF);
            return
                $"intersection(illegal {IntersectionMovementPolicy.FormatMovement(actualMovement)}; allowed {IntersectionMovementPolicy.FormatMovement(allowedMovement)})";
        }

        private static int EncodeLaneKindToken(
            Entity lane,
            ref RoutePenaltyInspectionContext context)
        {
            if (context.ParkingLaneData.HasComponent(lane))
            {
                return LaneKindTokenParkingLane;
            }

            if (context.GarageLaneData.HasComponent(lane))
            {
                return LaneKindTokenGarageLane;
            }

            if (context.ConnectionLaneData.TryGetComponent(lane, out ConnectionLane connectionLane))
            {
                bool isRoadIntersectionConnection =
                    (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0 &&
                    (connectionLane.m_Flags & ConnectionLaneFlags.Parking) == 0;
                if (isRoadIntersectionConnection)
                {
                    LaneMovement movement =
                        context.CarLaneData.TryGetComponent(lane, out CarLane connectionCarLane)
                            ? GetMovement(connectionCarLane.m_Flags)
                            : LaneMovement.None;
                    return LaneKindTokenIntersectionBase + (int)movement;
                }

                return LaneKindTokenAccessConnection;
            }

            if (context.EdgeLaneData.HasComponent(lane))
            {
                return LaneKindTokenRoad;
            }

            return LaneKindTokenLane;
        }

        private static string DescribeLaneKindToken(int laneKindToken)
        {
            if (laneKindToken >= LaneKindTokenIntersectionBase)
            {
                LaneMovement movement =
                    (LaneMovement)(laneKindToken - LaneKindTokenIntersectionBase);
                return movement == LaneMovement.None
                    ? "intersection"
                    : "intersection-" + FormatMovement(movement);
            }

            return laneKindToken switch
            {
                LaneKindTokenParkingLane => "parking-lane",
                LaneKindTokenGarageLane => "garage-lane",
                LaneKindTokenAccessConnection => "access-connection",
                LaneKindTokenRoad => "road",
                _ => "lane",
            };
        }

        internal static int CalculateTotalPenalty(RoutePenaltyProfile profile)
        {
            return profile.PublicTransportLaneSegments *
                EnforcementPenaltyService.GetPublicTransportLaneFine() +
                profile.MidBlockTransitions *
                EnforcementPenaltyService.GetMidBlockCrossingFine() +
                profile.IntersectionTransitions *
                EnforcementPenaltyService.GetIntersectionMovementFine();
        }

        internal static string FormatEntity(Entity entity)
        {
            return entity == Entity.Null
                ? "none"
                : $"#{entity.Index}:v{entity.Version}";
        }

        private static List<Entity> AcquireTemporaryLaneSequence(
            int minimumCapacity)
        {
            List<Entity> laneSequence = s_TemporaryLaneSequence;
            if (laneSequence == null)
            {
                laneSequence = new List<Entity>(minimumCapacity);
                s_TemporaryLaneSequence = laneSequence;
            }
            else if (laneSequence.Capacity < minimumCapacity)
            {
                laneSequence.Capacity = minimumCapacity;
            }

            return laneSequence;
        }

        private static void BuildLaneSequence(
            Entity currentLane,
            DynamicBuffer<CarNavigationLane> navigationLanes,
            bool hasNavigationLanes,
            List<Entity> laneSequence)
        {
            laneSequence.Clear();
            AppendLaneToSequence(currentLane, laneSequence);

            if (!hasNavigationLanes)
            {
                return;
            }

            for (int index = 0; index < navigationLanes.Length; index += 1)
            {
                AppendLaneToSequence(navigationLanes[index].m_Lane, laneSequence);
            }
        }

        private static void AppendLaneToSequence(Entity lane, List<Entity> laneSequence)
        {
            if (lane == Entity.Null)
            {
                return;
            }

            if (laneSequence.Count > 0 &&
                laneSequence[laneSequence.Count - 1] == lane)
            {
                return;
            }

            laneSequence.Add(lane);
        }

        private static void FindFirstExactIllegalAccessPair(
            List<Entity> laneSequence,
            out Entity sourceLane,
            out Entity targetLane,
            out LaneTransitionViolationReasonCode reasonCode,
            ref RoutePenaltyInspectionContext context)
        {
            sourceLane = Entity.Null;
            targetLane = Entity.Null;
            reasonCode = LaneTransitionViolationReasonCode.None;

            for (int index = 1; index < laneSequence.Count; index += 1)
            {
                Entity previousLane = laneSequence[index - 1];
                Entity currentLane = laneSequence[index];

                if (!MidBlockCrossingPolicy.TryGetIllegalAccessTransition(
                        context.EntityManager,
                        previousLane,
                        currentLane,
                        out LaneTransitionViolationReasonCode exactReasonCode))
                {
                    continue;
                }

                sourceLane = previousLane;
                targetLane = currentLane;
                reasonCode = exactReasonCode;
                return;
            }
        }

        private static AccessWindowBlockedReason FindFirstNormalizedAccessWindow(
            List<Entity> laneSequence,
            out Entity previousLane,
            out Entity currentLane,
            out Entity nextLane,
            out LaneTransitionViolationReasonCode reasonCode,
            out Entity blockedPreviousLane,
            out Entity blockedCurrentLane,
            out Entity blockedNextLane,
            ref RoutePenaltyInspectionContext context)
        {
            previousLane = Entity.Null;
            currentLane = Entity.Null;
            nextLane = Entity.Null;
            reasonCode = LaneTransitionViolationReasonCode.None;
            blockedPreviousLane = Entity.Null;
            blockedCurrentLane = Entity.Null;
            blockedNextLane = Entity.Null;

            if (laneSequence.Count >= 2 &&
                TryNormalizeTerminalAccessEgress(
                    laneSequence[0],
                    laneSequence[1],
                    out reasonCode,
                    ref context))
            {
                previousLane = laneSequence[0];
                currentLane = laneSequence[1];
                nextLane = Entity.Null;
                return AccessWindowBlockedReason.None;
            }

            if (laneSequence.Count == 2 &&
                TryNormalizeTerminalAccessIngress(
                    laneSequence[laneSequence.Count - 2],
                    laneSequence[laneSequence.Count - 1],
                    out reasonCode,
                    ref context))
            {
                previousLane = laneSequence[laneSequence.Count - 2];
                currentLane = laneSequence[laneSequence.Count - 1];
                nextLane = Entity.Null;
                return AccessWindowBlockedReason.None;
            }

            if (laneSequence.Count < 3)
            {
                return AccessWindowBlockedReason.NoThreeLaneWindowInCurrentSequence;
            }

            bool sawAccessConnection = false;
            AccessWindowBlockedReason firstBlockedReason = AccessWindowBlockedReason.None;

            for (int index = 1; index + 1 < laneSequence.Count; index += 1)
            {
                Entity candidatePreviousLane = laneSequence[index - 1];
                Entity candidateCurrentLane = laneSequence[index];
                Entity candidateNextLane = laneSequence[index + 1];

                if (!context.ConnectionLaneData.TryGetComponent(
                        candidateCurrentLane,
                        out _))
                {
                    continue;
                }

                sawAccessConnection = true;

                if (!TryGetNormalizableAccessConnectionKind(
                        candidateCurrentLane,
                        out NormalizedAccessConnectionKind connectionKind,
                        ref context))
                {
                    RecordBlockedWindow(
                        ref firstBlockedReason,
                        AccessWindowBlockedReason.MiddleRoadConnectionExcluded,
                        candidatePreviousLane,
                        candidateCurrentLane,
                        candidateNextLane,
                        ref blockedPreviousLane,
                        ref blockedCurrentLane,
                        ref blockedNextLane);
                    continue;
                }

                bool previousIsRoad =
                    IsRoadLane(candidatePreviousLane, ref context);
                bool nextIsRoad =
                    IsRoadLane(candidateNextLane, ref context);
                bool previousIsExplicitEndpoint =
                    TryGetExplicitAccessEndpointKind(
                        candidatePreviousLane,
                        out _,
                        ref context);
                bool nextIsExplicitEndpoint =
                    TryGetExplicitAccessEndpointKind(
                        candidateNextLane,
                        out _,
                        ref context);

                if (TryNormalizeAccessIngress(
                        candidatePreviousLane,
                        candidateCurrentLane,
                        candidateNextLane,
                        connectionKind,
                        out reasonCode,
                        ref context))
                {
                    previousLane = candidatePreviousLane;
                    currentLane = candidateCurrentLane;
                    nextLane = candidateNextLane;
                    return AccessWindowBlockedReason.None;
                }

                if (TryNormalizeAccessEgress(
                        candidatePreviousLane,
                        candidateCurrentLane,
                        candidateNextLane,
                        connectionKind,
                        out reasonCode,
                        ref context))
                {
                    previousLane = candidatePreviousLane;
                    currentLane = candidateCurrentLane;
                    nextLane = candidateNextLane;
                    return AccessWindowBlockedReason.None;
                }

                if ((previousIsRoad && nextIsExplicitEndpoint) ||
                    (previousIsExplicitEndpoint && nextIsRoad))
                {
                    RecordBlockedWindow(
                        ref firstBlockedReason,
                        AccessWindowBlockedReason.ExactAnchorPairNotIllegal,
                        candidatePreviousLane,
                        candidateCurrentLane,
                        candidateNextLane,
                        ref blockedPreviousLane,
                        ref blockedCurrentLane,
                        ref blockedNextLane);
                    continue;
                }

                RecordBlockedWindow(
                    ref firstBlockedReason,
                    AccessWindowBlockedReason.MissingExplicitEndpoint,
                    candidatePreviousLane,
                    candidateCurrentLane,
                    candidateNextLane,
                    ref blockedPreviousLane,
                    ref blockedCurrentLane,
                    ref blockedNextLane);
            }

            if (!sawAccessConnection)
            {
                if (laneSequence.Count >= 2 &&
                    context.ConnectionLaneData.HasComponent(laneSequence[0]))
                {
                    return AccessWindowBlockedReason.MissingExplicitEndpoint;
                }

                if (laneSequence.Count >= 2 &&
                    context.ConnectionLaneData.HasComponent(
                        laneSequence[laneSequence.Count - 1]))
                {
                    return AccessWindowBlockedReason.MissingExplicitEndpoint;
                }

                return AccessWindowBlockedReason.NoAccessConnectionInCurrentSequence;
            }

            if (laneSequence.Count > 2 &&
                TryNormalizeTerminalAccessIngress(
                    laneSequence[laneSequence.Count - 2],
                    laneSequence[laneSequence.Count - 1],
                    out reasonCode,
                    ref context))
            {
                previousLane = laneSequence[laneSequence.Count - 2];
                currentLane = laneSequence[laneSequence.Count - 1];
                nextLane = Entity.Null;
                return AccessWindowBlockedReason.None;
            }

            return firstBlockedReason == AccessWindowBlockedReason.None
                ? AccessWindowBlockedReason.NoAccessConnectionInCurrentSequence
                : firstBlockedReason;
        }

        private static void RecordBlockedWindow(
            ref AccessWindowBlockedReason firstBlockedReason,
            AccessWindowBlockedReason candidateReason,
            Entity candidatePreviousLane,
            Entity candidateCurrentLane,
            Entity candidateNextLane,
            ref Entity blockedPreviousLane,
            ref Entity blockedCurrentLane,
            ref Entity blockedNextLane)
        {
            if (firstBlockedReason != AccessWindowBlockedReason.None)
            {
                return;
            }

            firstBlockedReason = candidateReason;
            blockedPreviousLane = candidatePreviousLane;
            blockedCurrentLane = candidateCurrentLane;
            blockedNextLane = candidateNextLane;
        }

        private static string FormatBlockedReason(
            AccessWindowBlockedReason blockedReason,
            Entity previousLane,
            Entity currentLane,
            Entity nextLane)
        {
            if (blockedReason == AccessWindowBlockedReason.None)
            {
                return "none";
            }

            string reasonText = blockedReason.ToString();
            if (previousLane == Entity.Null ||
                currentLane == Entity.Null ||
                nextLane == Entity.Null)
            {
                return reasonText;
            }

            return
                $"{reasonText}@" +
                $"{FormatEntity(previousLane)}->{FormatEntity(currentLane)}->{FormatEntity(nextLane)}";
        }

        // Patch 2 keeps exact-pair legality in MidBlockCrossingPolicy and adds a
        // separate interpretation layer for user-facing route inspection.
        // A qualifying 3-lane road->connection->final-access window or a terminal
        // road<->ConnectionLane(Parking) pair can produce one conceptual access
        // label without replacing the exact tags.
        private static void AppendNormalizedAccessTags(
            List<Entity> laneSequence,
            ref RoutePenaltyTagCollector normalizedPenaltyTags,
            ref RoutePenaltyInspectionContext context,
            int maxPenaltyTags)
        {
            if (laneSequence.Count >= 2 &&
                TryNormalizeTerminalAccessEgress(
                    laneSequence[0],
                    laneSequence[1],
                    out LaneTransitionViolationReasonCode headReasonCode,
                    ref context))
            {
                normalizedPenaltyTags.Append(
                    EncodeMidBlockPenaltyTag(headReasonCode),
                    maxPenaltyTags);
            }

            for (int index = 1; index + 1 < laneSequence.Count; index += 1)
            {
                if (TryGetNormalizedAccessPenaltyToken(
                        laneSequence[index - 1],
                        laneSequence[index],
                        laneSequence[index + 1],
                        out int token,
                        ref context))
                {
                    normalizedPenaltyTags.Append(token, maxPenaltyTags);
                }
            }

            if (laneSequence.Count >= 2 &&
                TryNormalizeTerminalAccessIngress(
                    laneSequence[laneSequence.Count - 2],
                    laneSequence[laneSequence.Count - 1],
                    out LaneTransitionViolationReasonCode tailReasonCode,
                    ref context))
            {
                normalizedPenaltyTags.Append(
                    EncodeMidBlockPenaltyTag(tailReasonCode),
                    maxPenaltyTags);
            }
        }

        private static bool TryGetNormalizedAccessPenaltyToken(
            Entity previousLane,
            Entity currentLane,
            Entity nextLane,
            out int token,
            ref RoutePenaltyInspectionContext context)
        {
            token = 0;

            if (!TryGetNormalizedAccessReason(
                    previousLane,
                    currentLane,
                    nextLane,
                    out LaneTransitionViolationReasonCode reasonCode,
                    ref context))
            {
                return false;
            }

            token = EncodeMidBlockPenaltyTag(reasonCode);
            return true;
        }

        private static bool TryGetNormalizedAccessReason(
            Entity previousLane,
            Entity currentLane,
            Entity nextLane,
            out LaneTransitionViolationReasonCode reasonCode,
            ref RoutePenaltyInspectionContext context)
        {
            reasonCode = LaneTransitionViolationReasonCode.None;

            if (!TryGetNormalizableAccessConnectionKind(
                    currentLane,
                    out NormalizedAccessConnectionKind connectionKind,
                    ref context))
            {
                return false;
            }

            if (TryNormalizeAccessIngress(
                    previousLane,
                    currentLane,
                    nextLane,
                    connectionKind,
                    out reasonCode,
                    ref context))
            {
                return true;
            }

            return TryNormalizeAccessEgress(
                previousLane,
                currentLane,
                nextLane,
                connectionKind,
                out reasonCode,
                ref context);
        }

        private static bool TryNormalizeTerminalAccessIngress(
            Entity previousLane,
            Entity terminalLane,
            out LaneTransitionViolationReasonCode reasonCode,
            ref RoutePenaltyInspectionContext context)
        {
            reasonCode = LaneTransitionViolationReasonCode.None;

            if (!IsRoadLane(previousLane, ref context) ||
                !TryGetTerminalParkingConnectionEndpointKind(
                    terminalLane,
                    out NormalizedAccessEndpointKind endpointKind,
                    ref context))
            {
                return false;
            }

            if (!MidBlockCrossingPolicy.TryGetIllegalAccessTransition(
                    context.EntityManager,
                    previousLane,
                    terminalLane,
                    out LaneTransitionViolationReasonCode exactReason) ||
                !IsAccessIngressReason(exactReason))
            {
                return false;
            }

            reasonCode = endpointKind switch
            {
                NormalizedAccessEndpointKind.Garage =>
                    LaneTransitionViolationReasonCode.EnteredGarageAccessWithoutSideAccess,
                NormalizedAccessEndpointKind.Parking =>
                    LaneTransitionViolationReasonCode.EnteredParkingAccessWithoutSideAccess,
                _ => LaneTransitionViolationReasonCode.None,
            };

            return reasonCode != LaneTransitionViolationReasonCode.None;
        }

        private static bool TryNormalizeTerminalAccessEgress(
            Entity terminalLane,
            Entity nextLane,
            out LaneTransitionViolationReasonCode reasonCode,
            ref RoutePenaltyInspectionContext context)
        {
            reasonCode = LaneTransitionViolationReasonCode.None;

            if (!IsRoadLane(nextLane, ref context) ||
                !TryGetTerminalParkingConnectionEndpointKind(
                    terminalLane,
                    out NormalizedAccessEndpointKind endpointKind,
                    ref context))
            {
                return false;
            }

            if (!MidBlockCrossingPolicy.TryGetIllegalAccessTransition(
                    context.EntityManager,
                    terminalLane,
                    nextLane,
                    out LaneTransitionViolationReasonCode exactReason) ||
                !IsAccessEgressReason(exactReason))
            {
                return false;
            }

            reasonCode = endpointKind switch
            {
                NormalizedAccessEndpointKind.Garage =>
                    LaneTransitionViolationReasonCode.ExitedGarageAccessWithoutSideAccess,
                NormalizedAccessEndpointKind.Parking =>
                    LaneTransitionViolationReasonCode.ExitedParkingAccessWithoutSideAccess,
                _ => LaneTransitionViolationReasonCode.None,
            };

            return reasonCode != LaneTransitionViolationReasonCode.None;
        }

        private static bool TryNormalizeAccessIngress(
            Entity previousLane,
            Entity currentLane,
            Entity nextLane,
            NormalizedAccessConnectionKind connectionKind,
            out LaneTransitionViolationReasonCode reasonCode,
            ref RoutePenaltyInspectionContext context)
        {
            reasonCode = LaneTransitionViolationReasonCode.None;

            if (connectionKind == NormalizedAccessConnectionKind.None ||
                !IsRoadLane(previousLane, ref context) ||
                !TryGetExplicitAccessEndpointKind(
                    nextLane,
                    out NormalizedAccessEndpointKind endpointKind,
                    ref context))
            {
                return false;
            }

            if (!MidBlockCrossingPolicy.TryGetIllegalAccessTransition(
                    context.EntityManager,
                    previousLane,
                    currentLane,
                    out LaneTransitionViolationReasonCode exactReason) ||
                !IsAccessIngressReason(exactReason))
            {
                return false;
            }

            reasonCode = endpointKind switch
            {
                NormalizedAccessEndpointKind.Garage =>
                    LaneTransitionViolationReasonCode.EnteredGarageAccessWithoutSideAccess,
                NormalizedAccessEndpointKind.Parking =>
                    LaneTransitionViolationReasonCode.EnteredParkingAccessWithoutSideAccess,
                _ => LaneTransitionViolationReasonCode.None,
            };

            return reasonCode != LaneTransitionViolationReasonCode.None;
        }

        private static bool TryNormalizeAccessEgress(
            Entity previousLane,
            Entity currentLane,
            Entity nextLane,
            NormalizedAccessConnectionKind connectionKind,
            out LaneTransitionViolationReasonCode reasonCode,
            ref RoutePenaltyInspectionContext context)
        {
            reasonCode = LaneTransitionViolationReasonCode.None;

            if (connectionKind == NormalizedAccessConnectionKind.None ||
                !TryGetExplicitAccessEndpointKind(
                    previousLane,
                    out NormalizedAccessEndpointKind endpointKind,
                    ref context) ||
                !IsRoadLane(nextLane, ref context))
            {
                return false;
            }

            if (!MidBlockCrossingPolicy.TryGetIllegalAccessTransition(
                    context.EntityManager,
                    currentLane,
                    nextLane,
                    out LaneTransitionViolationReasonCode exactReason) ||
                !IsAccessEgressReason(exactReason))
            {
                return false;
            }

            reasonCode = endpointKind switch
            {
                NormalizedAccessEndpointKind.Garage =>
                    LaneTransitionViolationReasonCode.ExitedGarageAccessWithoutSideAccess,
                NormalizedAccessEndpointKind.Parking =>
                    LaneTransitionViolationReasonCode.ExitedParkingAccessWithoutSideAccess,
                _ => LaneTransitionViolationReasonCode.None,
            };

            return reasonCode != LaneTransitionViolationReasonCode.None;
        }

        private static bool TryGetNormalizableAccessConnectionKind(
            Entity lane,
            out NormalizedAccessConnectionKind kind,
            ref RoutePenaltyInspectionContext context)
        {
            kind = NormalizedAccessConnectionKind.None;

            if (!context.ConnectionLaneData.TryGetComponent(lane, out ConnectionLane connectionLane))
            {
                return false;
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
            {
                kind = NormalizedAccessConnectionKind.Parking;
                return true;
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0)
            {
                kind = NormalizedAccessConnectionKind.NonRoad;
                return true;
            }

            return false;
        }

        private static bool TryGetExplicitAccessEndpointKind(
            Entity lane,
            out NormalizedAccessEndpointKind kind,
            ref RoutePenaltyInspectionContext context)
        {
            kind = NormalizedAccessEndpointKind.None;

            if (context.GarageLaneData.HasComponent(lane))
            {
                kind = NormalizedAccessEndpointKind.Garage;
                return true;
            }

            if (context.ParkingLaneData.HasComponent(lane))
            {
                kind = NormalizedAccessEndpointKind.Parking;
                return true;
            }

            return false;
        }

        private static bool TryGetTerminalParkingConnectionEndpointKind(
            Entity lane,
            out NormalizedAccessEndpointKind kind,
            ref RoutePenaltyInspectionContext context)
        {
            kind = NormalizedAccessEndpointKind.None;

            // Vanilla roadside-building car traversal can end directly on a
            // ConnectionLane(Parking). Treat that terminal parking connection as
            // sufficient conceptual endpoint evidence in the interpretation layer.
            if (!context.ConnectionLaneData.TryGetComponent(lane, out ConnectionLane connectionLane) ||
                (connectionLane.m_Flags & ConnectionLaneFlags.Parking) == 0)
            {
                return false;
            }

            if (context.GarageLaneData.HasComponent(lane))
            {
                kind = NormalizedAccessEndpointKind.Garage;
                return true;
            }

            kind = NormalizedAccessEndpointKind.Parking;
            return true;
        }

        private static string FormatNormalizedAccessMatch(
            Entity previousLane,
            Entity currentLane,
            Entity nextLane,
            LaneTransitionViolationReasonCode reasonCode)
        {
            if (reasonCode == LaneTransitionViolationReasonCode.None)
            {
                return "none";
            }

            if (nextLane == Entity.Null)
            {
                return
                    $"{FormatEntity(previousLane)}->" +
                    $"{FormatEntity(currentLane)}:{reasonCode}";
            }

            return
                $"{FormatEntity(previousLane)}->" +
                $"{FormatEntity(currentLane)}->" +
                $"{FormatEntity(nextLane)}:{reasonCode}";
        }

        private static bool IsRoadLane(
            Entity lane,
            ref RoutePenaltyInspectionContext context)
        {
            return context.EdgeLaneData.HasComponent(lane) &&
                context.CarLaneData.HasComponent(lane);
        }

        private static bool IsAccessIngressReason(
            LaneTransitionViolationReasonCode reasonCode)
        {
            switch (reasonCode)
            {
                case LaneTransitionViolationReasonCode.EnteredGarageAccessWithoutSideAccess:
                case LaneTransitionViolationReasonCode.EnteredParkingAccessWithoutSideAccess:
                case LaneTransitionViolationReasonCode.EnteredParkingConnectionWithoutSideAccess:
                case LaneTransitionViolationReasonCode.EnteredBuildingAccessConnectionWithoutSideAccess:
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsAccessEgressReason(
            LaneTransitionViolationReasonCode reasonCode)
        {
            switch (reasonCode)
            {
                case LaneTransitionViolationReasonCode.ExitedParkingAccessWithoutSideAccess:
                case LaneTransitionViolationReasonCode.ExitedGarageAccessWithoutSideAccess:
                case LaneTransitionViolationReasonCode.ExitedParkingConnectionWithoutSideAccess:
                case LaneTransitionViolationReasonCode.ExitedBuildingAccessConnectionWithoutSideAccess:
                    return true;

                default:
                    return false;
            }
        }

        private static void AppendLaneToProfile(
            Entity lane,
            bool publicTransportLanePolicyResolved,
            bool allowedOnPublicTransportLane,
            ref Entity previousLane,
            ref bool previousUnauthorizedPublicTransportLane,
            ref RoutePenaltyProfile profile,
            ref uint hash,
            ref RoutePenaltyTagCollector penaltyTags,
            ref RoutePenaltyInspectionContext context,
            bool captureTagSummary,
            int maxPenaltyTags)
        {
            if (lane == Entity.Null)
            {
                return;
            }

            if (previousLane != Entity.Null)
            {
                if (TryGetMidBlockPenaltyToken(
                        previousLane,
                        lane,
                        out int midBlockTag,
                        ref context))
                {
                    profile.MidBlockTransitions += 1;
                    if (captureTagSummary)
                    {
                        penaltyTags.Append(midBlockTag, maxPenaltyTags);
                    }
                }

                if (TryGetIntersectionPenaltyToken(
                        previousLane,
                        lane,
                        out int intersectionTag,
                        ref context))
                {
                    profile.IntersectionTransitions += 1;
                    if (captureTagSummary)
                    {
                        penaltyTags.Append(intersectionTag, maxPenaltyTags);
                    }
                }
            }

            bool unauthorizedPublicTransportLane =
                publicTransportLanePolicyResolved &&
                IsUnauthorizedPublicTransportLane(
                    lane,
                    allowedOnPublicTransportLane,
                    ref context);

            if (unauthorizedPublicTransportLane &&
                !previousUnauthorizedPublicTransportLane)
            {
                profile.PublicTransportLaneSegments += 1;
                if (captureTagSummary)
                {
                    penaltyTags.Append(
                        EncodeUnauthorizedPublicTransportLaneTag(lane, ref context),
                        maxPenaltyTags);
                }
            }

            hash = HashLane(hash, lane, unauthorizedPublicTransportLane);
            previousLane = lane;
            previousUnauthorizedPublicTransportLane = unauthorizedPublicTransportLane;
        }

        private static uint HashLane(
            uint currentHash,
            Entity lane,
            bool unauthorizedPublicTransportLane)
        {
            unchecked
            {
                currentHash ^= (uint)lane.Index;
                currentHash *= 16777619u;
                currentHash ^= unauthorizedPublicTransportLane ? 0xBADA55u : 0u;
                currentHash *= 16777619u;
                return currentHash;
            }
        }

        private static string FormatMovement(LaneMovement movement)
        {
            StringBuilder parts = new StringBuilder(24);
            if ((movement & LaneMovement.Forward) != 0)
            {
                parts.Append("forward");
            }

            if ((movement & LaneMovement.Left) != 0)
            {
                AppendMovementPart(parts, "left");
            }

            if ((movement & LaneMovement.Right) != 0)
            {
                AppendMovementPart(parts, "right");
            }

            if ((movement & LaneMovement.UTurn) != 0)
            {
                AppendMovementPart(parts, "u-turn");
            }

            return parts.Length == 0
                ? "none"
                : parts.ToString();
        }

        private static void AppendBreakdownPart(
            StringBuilder parts,
            string label,
            int count,
            int fine)
        {
            if (count <= 0)
            {
                return;
            }

            if (parts.Length > 0)
            {
                parts.Append(", ");
            }

            parts
                .Append(label)
                .Append(count)
                .Append(" x ")
                .Append(fine);
        }

        private static void AppendMovementPart(StringBuilder parts, string part)
        {
            if (parts.Length > 0)
            {
                parts.Append('+');
            }

            parts.Append(part);
        }

        private static LaneMovement GetMovement(Game.Net.CarLaneFlags flags)
        {
            LaneMovement movement = LaneMovement.None;

            if ((flags & Game.Net.CarLaneFlags.Forward) != 0)
            {
                movement |= LaneMovement.Forward;
            }

            if ((flags & (Game.Net.CarLaneFlags.TurnLeft | Game.Net.CarLaneFlags.GentleTurnLeft)) != 0)
            {
                movement |= LaneMovement.Left;
            }

            if ((flags & (Game.Net.CarLaneFlags.TurnRight | Game.Net.CarLaneFlags.GentleTurnRight)) != 0)
            {
                movement |= LaneMovement.Right;
            }

            if ((flags & (Game.Net.CarLaneFlags.UTurnLeft | Game.Net.CarLaneFlags.UTurnRight)) != 0)
            {
                movement |= LaneMovement.UTurn;
            }

            return movement;
        }
    }
}
