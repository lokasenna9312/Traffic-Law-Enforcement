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
        public ComponentLookup<Owner> OwnerData;
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

    internal readonly struct RoutePenaltyInspectionResult
    {
        public readonly uint RouteHash;
        public readonly RoutePenaltyProfile Profile;
        public readonly int TotalPenalty;
        public readonly string Breakdown;
        public readonly string Tags;
        public readonly bool PublicTransportLanePolicyResolved;
        public readonly bool AllowedOnPublicTransportLane;

        public RoutePenaltyInspectionResult(
            uint routeHash,
            RoutePenaltyProfile profile,
            string breakdown,
            string tags,
            bool publicTransportLanePolicyResolved,
            bool allowedOnPublicTransportLane)
        {
            RouteHash = routeHash;
            Profile = profile;
            TotalPenalty = RoutePenaltyInspection.CalculateTotalPenalty(profile);
            Breakdown = breakdown;
            Tags = tags;
            PublicTransportLanePolicyResolved = publicTransportLanePolicyResolved;
            AllowedOnPublicTransportLane = allowedOnPublicTransportLane;
        }
    }

    internal static class RoutePenaltyInspection
    {
        internal const int DefaultMaxPenaltyTags = 6;
        private const uint kInitialRouteHash = 2166136261u;

        internal static RoutePenaltyInspectionResult InspectCurrentRoute(
            Entity vehicle,
            Entity currentLane,
            DynamicBuffer<CarNavigationLane> navigationLanes,
            bool hasNavigationLanes,
            ref RoutePenaltyInspectionContext context,
            bool captureDebugStrings,
            int maxPenaltyTags = DefaultMaxPenaltyTags)
        {
            RoutePenaltyProfile profile = default;
            bool publicTransportLanePolicyResolved =
                TryResolveAllowedOnPublicTransportLane(
                    vehicle,
                    ref context,
                    out bool allowedOnPublicTransportLane);

            List<string> penaltyTags = captureDebugStrings
                ? new List<string>(maxPenaltyTags)
                : null;

            uint hash = kInitialRouteHash;
            int omittedTagCount = 0;
            bool previousUnauthorizedPublicTransportLane = false;
            Entity previousLane = Entity.Null;
            Entity previousLaneOwner = Entity.Null;

            AppendLaneToProfile(
                vehicle,
                currentLane,
                publicTransportLanePolicyResolved,
                allowedOnPublicTransportLane,
                ref previousLane,
                ref previousLaneOwner,
                ref previousUnauthorizedPublicTransportLane,
                ref profile,
                ref hash,
                penaltyTags,
                ref omittedTagCount,
                ref context,
                maxPenaltyTags);

            if (hasNavigationLanes)
            {
                for (int index = 0; index < navigationLanes.Length; index++)
                {
                    Entity nextLane = navigationLanes[index].m_Lane;
                    if (nextLane == Entity.Null)
                    {
                        continue;
                    }

                    if (index == 0 && nextLane == previousLane)
                    {
                        continue;
                    }

                    AppendLaneToProfile(
                        vehicle,
                        nextLane,
                        publicTransportLanePolicyResolved,
                        allowedOnPublicTransportLane,
                        ref previousLane,
                        ref previousLaneOwner,
                        ref previousUnauthorizedPublicTransportLane,
                        ref profile,
                        ref hash,
                        penaltyTags,
                        ref omittedTagCount,
                        ref context,
                        maxPenaltyTags);
                }
            }

            return new RoutePenaltyInspectionResult(
                hash,
                profile,
                captureDebugStrings ? BuildBreakdown(profile) : null,
                captureDebugStrings ? BuildTagSummary(penaltyTags, omittedTagCount) : null,
                publicTransportLanePolicyResolved,
                allowedOnPublicTransportLane);
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

            List<string> preview = new List<string>(maxPreviewLanes);
            int totalUpcoming = 0;

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
                if (preview.Count >= maxPreviewLanes)
                {
                    continue;
                }

                preview.Add($"{FormatEntity(lane)} {DescribeLaneKind(lane, ref context)}");
            }

            if (totalUpcoming == 0)
            {
                return "none";
            }

            string summary = $"{totalUpcoming} total: {string.Join(" -> ", preview.ToArray())}";
            if (totalUpcoming > preview.Count)
            {
                summary += $" (+{totalUpcoming - preview.Count} more)";
            }

            return summary;
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

        internal static string DescribeLaneKind(
            Entity lane,
            ref RoutePenaltyInspectionContext context)
        {
            if (context.ParkingLaneData.HasComponent(lane))
            {
                return "parking-lane";
            }

            if (context.GarageLaneData.HasComponent(lane))
            {
                return "garage-lane";
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
                    string movementSuffix =
                        movement == LaneMovement.None
                            ? string.Empty
                            : "-" + FormatMovement(movement);
                    return "intersection" + movementSuffix;
                }

                return "access-connection";
            }

            if (context.EdgeLaneData.HasComponent(lane))
            {
                return "road";
            }

            return "lane";
        }

        internal static string BuildBreakdown(RoutePenaltyProfile profile)
        {
            List<string> parts = new List<string>(3);
            if (profile.PublicTransportLaneSegments > 0)
            {
                parts.Add(
                    $"PT-lane {profile.PublicTransportLaneSegments} x {EnforcementPenaltyService.GetPublicTransportLaneFine()}");
            }

            if (profile.MidBlockTransitions > 0)
            {
                parts.Add(
                    $"mid-block {profile.MidBlockTransitions} x {EnforcementPenaltyService.GetMidBlockCrossingFine()}");
            }

            if (profile.IntersectionTransitions > 0)
            {
                parts.Add(
                    $"intersection {profile.IntersectionTransitions} x {EnforcementPenaltyService.GetIntersectionMovementFine()}");
            }

            return parts.Count == 0
                ? "none"
                : string.Join(", ", parts.ToArray());
        }

        internal static string BuildTagSummary(List<string> penaltyTags, int omittedTagCount)
        {
            if (penaltyTags == null || penaltyTags.Count == 0)
            {
                return "none";
            }

            string summary = string.Join("; ", penaltyTags.ToArray());
            if (omittedTagCount > 0)
            {
                summary += $"; ... (+{omittedTagCount} more)";
            }

            return summary;
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

        private static void AppendLaneToProfile(
            Entity vehicle,
            Entity lane,
            bool publicTransportLanePolicyResolved,
            bool allowedOnPublicTransportLane,
            ref Entity previousLane,
            ref Entity previousLaneOwner,
            ref bool previousUnauthorizedPublicTransportLane,
            ref RoutePenaltyProfile profile,
            ref uint hash,
            List<string> penaltyTags,
            ref int omittedTagCount,
            ref RoutePenaltyInspectionContext context,
            int maxPenaltyTags)
        {
            if (lane == Entity.Null)
            {
                return;
            }

            Entity laneOwner = GetOwner(lane, ref context);
            if (previousLane != Entity.Null)
            {
                if (TryGetMidBlockPenaltyTag(
                        previousLane,
                        lane,
                        out string midBlockTag,
                        ref context))
                {
                    profile.MidBlockTransitions += 1;
                    AppendPenaltyTag(
                        penaltyTags,
                        midBlockTag,
                        ref omittedTagCount,
                        maxPenaltyTags);
                }

                if (TryGetIntersectionPenaltyTag(
                        previousLane,
                        lane,
                        out string intersectionTag,
                        ref context))
                {
                    profile.IntersectionTransitions += 1;
                    AppendPenaltyTag(
                        penaltyTags,
                        intersectionTag,
                        ref omittedTagCount,
                        maxPenaltyTags);
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
                AppendPenaltyTag(
                    penaltyTags,
                    DescribeLaneKind(lane, ref context) + "(public-only, illegal)",
                    ref omittedTagCount,
                    maxPenaltyTags);
            }

            hash = HashLane(hash, lane, unauthorizedPublicTransportLane);
            previousLane = lane;
            previousLaneOwner = laneOwner;
            previousUnauthorizedPublicTransportLane = unauthorizedPublicTransportLane;
        }

        private static Entity GetOwner(
            Entity lane,
            ref RoutePenaltyInspectionContext context)
        {
            if (lane != Entity.Null &&
                context.OwnerData.TryGetComponent(lane, out Owner owner))
            {
                return owner.m_Owner;
            }

            return Entity.Null;
        }

        private static void AppendPenaltyTag(
            List<string> penaltyTags,
            string tag,
            ref int omittedTagCount,
            int maxPenaltyTags)
        {
            if (penaltyTags == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(tag) || penaltyTags.Contains(tag))
            {
                return;
            }

            if (penaltyTags.Count >= maxPenaltyTags)
            {
                omittedTagCount += 1;
                return;
            }

            penaltyTags.Add(tag);
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
            List<string> parts = new List<string>(4);
            if ((movement & LaneMovement.Forward) != 0)
            {
                parts.Add("forward");
            }

            if ((movement & LaneMovement.Left) != 0)
            {
                parts.Add("left");
            }

            if ((movement & LaneMovement.Right) != 0)
            {
                parts.Add("right");
            }

            if ((movement & LaneMovement.UTurn) != 0)
            {
                parts.Add("u-turn");
            }

            return parts.Count == 0
                ? "none"
                : string.Join("+", parts.ToArray());
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
