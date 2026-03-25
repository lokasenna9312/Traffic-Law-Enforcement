using System.Collections.Generic;
using Game;
using Game.Common;
using Game.Net;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public static class RerouteLoggingTelemetry
    {
        public static bool Enabled { get; private set; }
        public static int CachedSnapshotCount { get; private set; }
        public static int LastCandidateCount { get; private set; }
        public static int LastEmittedLogCount { get; private set; }

        public static void SetState(bool enabled, int cachedSnapshotCount, int lastCandidateCount, int lastEmittedLogCount)
        {
            Enabled = enabled;
            CachedSnapshotCount = cachedSnapshotCount;
            LastCandidateCount = lastCandidateCount;
            LastEmittedLogCount = lastEmittedLogCount;
        }
    }
    [BurstCompile]
    public partial class RoutePenaltyRerouteLoggingSystem : GameSystemBase
    {
        private EntityQuery m_CachedVehicleQuery;
        private const int MaxPenaltyTags = 6;
        private const int MaxLogsPerUpdate = 4;
        private const int SnapshotSweepInterval = 2048;
        private const int MaxPublicTransportLaneDecisionDiagnosticLogsPerUpdate = 8;
        private int m_PublicTransportLaneDecisionDiagnosticLogsThisUpdate;

        private EntityQuery m_CarQuery;
        private EntityQuery m_CurrentLaneChangedQuery;
        private EntityQuery m_NavigationLaneChangedQuery;
        private EntityQuery m_CarChangedQuery;
        private BufferLookup<CarNavigationLane> m_NavigationLaneData;
        private ComponentLookup<CarCurrentLane> m_CurrentLaneData;
        private ComponentLookup<Owner> m_OwnerData;
        private ComponentLookup<CarLane> m_CarLaneData;
        private ComponentLookup<VehicleTrafficLawProfile> m_ProfileData;
        private ComponentLookup<EdgeLane> m_EdgeLaneData;
        private ComponentLookup<ParkingLane> m_ParkingLaneData;
        private ComponentLookup<GarageLane> m_GarageLaneData;
        private ComponentLookup<ConnectionLane> m_ConnectionLaneData;
        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;
        private readonly Dictionary<Entity, RoutePenaltySnapshot> m_LastSnapshots = new Dictionary<Entity, RoutePenaltySnapshot>();
        private readonly HashSet<Entity> m_CandidateVehicles = new HashSet<Entity>();
        private int m_UpdateCount;
        private int m_LastObservedRuntimeWorldGeneration = -1;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CarQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>());
            m_CurrentLaneChangedQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>());
            m_CurrentLaneChangedQuery.SetChangedVersionFilter(ComponentType.ReadOnly<CarCurrentLane>());
            m_NavigationLaneChangedQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadOnly<CarNavigationLane>());
            m_NavigationLaneChangedQuery.SetChangedVersionFilter(ComponentType.ReadOnly<CarNavigationLane>());
            m_CarChangedQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>());
            m_CarChangedQuery.SetChangedVersionFilter(ComponentType.ReadOnly<Car>());
            m_NavigationLaneData = GetBufferLookup<CarNavigationLane>(true);
            m_CurrentLaneData = GetComponentLookup<CarCurrentLane>(true);
            m_OwnerData = GetComponentLookup<Owner>(true);
            m_CarLaneData = GetComponentLookup<CarLane>(true);
            m_EdgeLaneData = GetComponentLookup<EdgeLane>(true);
            m_ParkingLaneData = GetComponentLookup<ParkingLane>(true);
            m_GarageLaneData = GetComponentLookup<GarageLane>(true);
            m_ConnectionLaneData = GetComponentLookup<ConnectionLane>(true);
            m_ProfileData = GetComponentLookup<VehicleTrafficLawProfile>(true);
            m_TypeLookups = PublicTransportLaneVehicleTypeLookups.Create(this);
            m_CachedVehicleQuery = GetEntityQuery(ComponentType.ReadOnly<Car>(), ComponentType.ReadOnly<CarCurrentLane>());
            RequireForUpdate(m_CarQuery);
        }

        [BurstCompile]
        protected override void OnUpdate()
        {
            HandleRuntimeWorldReload();
            bool loggingEnabled = EnforcementLoggingPolicy.ShouldLogEstimatedReroutes();
            m_PublicTransportLaneDecisionDiagnosticLogsThisUpdate = 0;
            if (!Mod.IsEnforcementEnabled)
            {
                m_LastSnapshots.Clear();
                RerouteLoggingTelemetry.SetState(false, 0, 0, 0);
                return;
            }

            m_NavigationLaneData.Update(this);
            m_CurrentLaneData.Update(this);
            m_OwnerData.Update(this);
            m_CarLaneData.Update(this);
            m_EdgeLaneData.Update(this);
            m_ParkingLaneData.Update(this);
            m_GarageLaneData.Update(this);
            m_ConnectionLaneData.Update(this);
            m_ProfileData.Update(this);
            m_TypeLookups.Update(this);

            m_CandidateVehicles.Clear();
            CollectCandidateVehicles(m_CurrentLaneChangedQuery);
            CollectCandidateVehicles(m_NavigationLaneChangedQuery);
            CollectCandidateVehicles(m_CarChangedQuery);

            int logsEmitted = 0;
            foreach (Entity vehicle in m_CandidateVehicles)
            {
                if (!m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLane))
                {
                    continue;
                }

                RoutePenaltySnapshot snapshot =
                    BuildSnapshot(vehicle, currentLane, captureDebugStrings: loggingEnabled);

                if (m_LastSnapshots.TryGetValue(vehicle, out RoutePenaltySnapshot previousSnapshot))
                {
                    if (ShouldLogReroute(previousSnapshot, snapshot))
                    {
                        RecordRerouteTelemetry(vehicle, previousSnapshot, snapshot);

                        if (loggingEnabled && logsEmitted < MaxLogsPerUpdate)
                        {
                            LogReroute(vehicle, previousSnapshot, snapshot);
                            logsEmitted += 1;
                        }
                    }

                    m_LastSnapshots[vehicle] = snapshot;
                }
                else
                {
                    m_LastSnapshots[vehicle] = snapshot;
                }
            }

            int emittedLogs = logsEmitted;

            m_UpdateCount += 1;
            if ((m_UpdateCount % SnapshotSweepInterval) == 0)
            {
                SweepInactiveSnapshots();
            }

            RerouteLoggingTelemetry.SetState(true, m_LastSnapshots.Count, m_CandidateVehicles.Count, emittedLogs);
        }

        private void HandleRuntimeWorldReload()
        {
            int currentGeneration = EnforcementSaveDataSystem.RuntimeWorldGeneration;
            if (m_LastObservedRuntimeWorldGeneration == currentGeneration)
            {
                return;
            }

            m_LastObservedRuntimeWorldGeneration = currentGeneration;

            m_LastSnapshots.Clear();
            m_CandidateVehicles.Clear();
            m_UpdateCount = 0;
            RerouteLoggingTelemetry.SetState(false, 0, 0, 0);

            Mod.log.Info(
                $"[SAVELOAD] RoutePenaltyRerouteLoggingSystem runtime reset: generation={currentGeneration}");
        }

        private void CollectCandidateVehicles(EntityQuery query)
        {
            NativeArray<Entity> vehicles = query.ToEntityArray(Allocator.Temp);
            try
            {
                for (int index = 0; index < vehicles.Length; index++)
                {
                    m_CandidateVehicles.Add(vehicles[index]);
                }
            }
            finally
            {
                vehicles.Dispose();
            }
        }

        private RoutePenaltySnapshot BuildSnapshot(Entity vehicle, CarCurrentLane currentLane, bool captureDebugStrings)
        {
            RoutePenaltyProfile profile = default;
            bool hasResolvedPublicTransportLanePolicy =
                TryResolveAllowedOnPublicTransportLaneForLogging(
                    vehicle,
                    out bool allowedOnPublicTransportLane);
            List<string> penaltyTags = captureDebugStrings
                ? new List<string>(MaxPenaltyTags)
                : null;
            uint hash = 2166136261u;
            int omittedTagCount = 0;
            bool previousUnauthorizedPublicTransportLane = false;
            Entity previousLane = Entity.Null;
            Entity previousLaneOwner = Entity.Null;

            AppendLaneToSnapshot(
                vehicle,
                currentLane.m_Lane,
                hasResolvedPublicTransportLanePolicy,
                allowedOnPublicTransportLane,
                ref previousLane,
                ref previousLaneOwner,
                ref previousUnauthorizedPublicTransportLane,
                ref profile,
                ref hash,
                penaltyTags,
                ref omittedTagCount);

            if (m_NavigationLaneData.TryGetBuffer(vehicle, out DynamicBuffer<CarNavigationLane> navigationLanes))
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

                    AppendLaneToSnapshot(
                        vehicle,
                        nextLane,
                        hasResolvedPublicTransportLanePolicy,
                        allowedOnPublicTransportLane,
                        ref previousLane,
                        ref previousLaneOwner,
                        ref previousUnauthorizedPublicTransportLane,
                        ref profile,
                        ref hash,
                        penaltyTags,
                        ref omittedTagCount);
                }
            }

            return new RoutePenaltySnapshot(
                hash,
                profile,
                captureDebugStrings ? BuildBreakdown(profile) : null,
                captureDebugStrings ? BuildTagSummary(penaltyTags, omittedTagCount) : null,
                hasResolvedPublicTransportLanePolicy);
        }

        private void AppendLaneToSnapshot(
            Entity vehicle,
            Entity lane,
            bool hasResolvedPublicTransportLanePolicy,
            bool allowedOnPublicTransportLane,
            ref Entity previousLane,
            ref Entity previousLaneOwner,
            ref bool previousUnauthorizedPublicTransportLane,
            ref RoutePenaltyProfile profile,
            ref uint hash,
            List<string> penaltyTags,
            ref int omittedTagCount)
        {
            if (lane == Entity.Null)
            {
                return;
            }

            Entity laneOwner = GetOwner(lane);
            if (previousLane != Entity.Null)
            {
                if (TryGetMidBlockPenaltyTag(previousLane, previousLaneOwner, lane, laneOwner, out string midBlockTag))
                {
                    profile.MidBlockTransitions += 1;
                    AppendPenaltyTag(penaltyTags, midBlockTag, ref omittedTagCount);
                }

                if (TryGetIntersectionPenaltyTag(previousLane, lane, out string intersectionTag))
                {
                    profile.IntersectionTransitions += 1;
                    AppendPenaltyTag(penaltyTags, intersectionTag, ref omittedTagCount);
                }
            }

            bool unauthorizedPublicTransportLane =
                hasResolvedPublicTransportLanePolicy &&
                IsUnauthorizedPublicTransportLane(lane, allowedOnPublicTransportLane);

            MaybeLogPublicTransportLaneDecisionDiagnostic(
                vehicle,
                lane,
                hasResolvedPublicTransportLanePolicy,
                allowedOnPublicTransportLane,
                unauthorizedPublicTransportLane);

            if (unauthorizedPublicTransportLane && !previousUnauthorizedPublicTransportLane)
            {
                profile.PublicTransportLaneSegments += 1;
                AppendPenaltyTag(penaltyTags, DescribeUnauthorizedPublicTransportLaneTag(lane), ref omittedTagCount);
            }

            hash = HashLane(hash, lane, unauthorizedPublicTransportLane);
            previousLane = lane;
            previousLaneOwner = laneOwner;
            previousUnauthorizedPublicTransportLane = unauthorizedPublicTransportLane;
        }

        private void MaybeLogPublicTransportLaneDecisionDiagnostic(
            Entity vehicle,
            Entity lane,
            bool hasResolvedPublicTransportLanePolicy,
            bool allowedOnPublicTransportLane,
            bool unauthorizedPublicTransportLane)
        {
            if (!EnforcementLoggingPolicy.ShouldLogPathfindingPenaltyDiagnostics())
            {
                return;
            }

            if (m_PublicTransportLaneDecisionDiagnosticLogsThisUpdate >=
                MaxPublicTransportLaneDecisionDiagnosticLogsPerUpdate)
            {
                return;
            }

            if (!m_CarLaneData.TryGetComponent(lane, out CarLane laneData))
            {
                return;
            }

            bool publicOnly = (laneData.m_Flags & Game.Net.CarLaneFlags.PublicOnly) != 0;
            if (!publicOnly)
            {
                return;
            }

            if (hasResolvedPublicTransportLanePolicy && allowedOnPublicTransportLane)
            {
                return;
            }

            bool hasProfile =
                m_ProfileData.TryGetComponent(vehicle, out VehicleTrafficLawProfile vehicleProfile);

            bool emergency =
                EmergencyVehiclePolicy.IsEmergencyVehicle(vehicle, ref m_TypeLookups);

            bool engineHasFlag =
                PublicTransportLanePolicy.EngineHasPublicTransportLaneFlag(
                    vehicle,
                    ref m_TypeLookups);

            string vanillaAllows = hasProfile
                ? PublicTransportLanePolicy.VanillaAllowsAccess(
                    vehicleProfile.m_PublicTransportLaneAccessBits).ToString()
                : "n/a";

            string modAllows = hasProfile
                ? PublicTransportLanePolicy.ModAllowsAccess(
                    vehicleProfile.m_PublicTransportLaneAccessBits).ToString()
                : "n/a";

            string permissionChangedByMod = hasProfile
                ? PublicTransportLanePolicy.PermissionChangedByMod(
                    vehicleProfile.m_PublicTransportLaneAccessBits).ToString()
                : "n/a";

            string accessBits = hasProfile
                ? vehicleProfile.m_PublicTransportLaneAccessBits.ToString()
                : "n/a";

            string role =
                PublicTransportLanePolicy.DescribeVehicleRole(vehicle, ref m_TypeLookups);

            string laneKind = DescribeLaneKind(lane);

            string message =
                $"PT_ROUTE_DIAG: vehicle={vehicle}, role={role}, lane={lane}, laneKind={laneKind}, " +
                $"publicOnly={publicOnly}, hasResolvedPolicy={hasResolvedPublicTransportLanePolicy}, " +
                $"hasProfile={hasProfile}, allowedOnPublicTransportLane={allowedOnPublicTransportLane}, " +
                $"unauthorizedPublicTransportLane={unauthorizedPublicTransportLane}, engineHasFlag={engineHasFlag}, " +
                $"emergency={emergency}, vanillaAllows={vanillaAllows}, modAllows={modAllows}, " +
                $"permissionChangedByMod={permissionChangedByMod}, accessBits={accessBits}";

            EnforcementTelemetry.RecordEvent(message);
            Mod.log.Info(message);
            m_PublicTransportLaneDecisionDiagnosticLogsThisUpdate += 1;
        }

        private bool TryGetMidBlockPenaltyTag(Entity sourceLane, Entity sourceOwner, Entity targetLane, Entity targetOwner, out string tag)
        {
            tag = null;

            if (m_EdgeLaneData.HasComponent(sourceLane) && m_EdgeLaneData.HasComponent(targetLane))
            {
                if (!m_CarLaneData.TryGetComponent(sourceLane, out CarLane sourceCarLane) ||
                    !m_CarLaneData.TryGetComponent(targetLane, out CarLane targetCarLane))
                {
                    return false;
                }

                EdgeLane sourceEdgeLane = m_EdgeLaneData[sourceLane];
                EdgeLane targetEdgeLane = m_EdgeLaneData[targetLane];
                bool sameOwner = sourceOwner == targetOwner && targetOwner != Entity.Null;
                bool oppositeDirections = IsOppositeDirection(sourceEdgeLane, targetEdgeLane);
                bool sameCarriageway = sourceCarLane.m_CarriagewayGroup == targetCarLane.m_CarriagewayGroup;
                if (sameOwner && oppositeDirections && sameCarriageway)
                {
                    tag = "mid-block(opposite-flow)";
                    return true;
                }
            }

            if (!m_EdgeLaneData.HasComponent(sourceLane) || !m_CarLaneData.TryGetComponent(sourceLane, out CarLane originLane))
            {
                return TryGetOutboundAccessPenaltyTag(sourceLane, targetLane, out tag);
            }

            if (!LaneAllowsSideAccess(originLane))
            {
                if (m_ParkingLaneData.HasComponent(targetLane) || m_GarageLaneData.HasComponent(targetLane))
                {
                    tag = m_GarageLaneData.HasComponent(targetLane) ? "mid-block(garage-access)" : "mid-block(parking-access)";
                    return true;
                }

                if (IsAccessConnection(targetLane))
                {
                    tag = $"mid-block({DescribeAccessConnectionTag(targetLane)})";
                    return true;
                }
            }

            return TryGetOutboundAccessPenaltyTag(sourceLane, targetLane, out tag);
        }

        private bool TryResolveAllowedOnPublicTransportLaneForLogging(
            Entity vehicle,
            out bool allowedOnPublicTransportLane)
        {
            if (EmergencyVehiclePolicy.IsEmergencyVehicle(vehicle, ref m_TypeLookups))
            {
                allowedOnPublicTransportLane = true;
                return true;
            }

            if (!m_ProfileData.TryGetComponent(vehicle, out VehicleTrafficLawProfile profile))
            {
                allowedOnPublicTransportLane = false;
                return false;
            }

            allowedOnPublicTransportLane =
                PublicTransportLanePolicy.ModAllowsAccess(
                    profile.m_PublicTransportLaneAccessBits);

            return true;
        }

        private bool TryGetOutboundAccessPenaltyTag(Entity sourceLane, Entity targetLane, out string tag)
        {
            tag = null;

            if (!IsAccessOrigin(sourceLane))
            {
                return false;
            }

            if (!m_EdgeLaneData.HasComponent(targetLane) || !m_CarLaneData.TryGetComponent(targetLane, out CarLane targetCarLane))
            {
                return false;
            }

            if (LaneAllowsSideAccess(targetCarLane))
            {
                return false;
            }

            tag = $"mid-block(illegal-egress:{DescribeAccessOriginTag(sourceLane)})";
            return true;
        }

        private bool TryGetIntersectionPenaltyTag(Entity sourceLane, Entity targetLane, out string tag)
        {
            tag = null;

            if (!IntersectionMovementPolicy.TryGetIllegalIntersectionMovement(
                    m_ConnectionLaneData,
                    m_CarLaneData,
                    sourceLane,
                    targetLane,
                    out LaneMovement actualMovement,
                    out LaneMovement allowedMovement))
            {
                return false;
            }

            tag = $"intersection(illegal {IntersectionMovementPolicy.FormatMovement(actualMovement)}; allowed {IntersectionMovementPolicy.FormatMovement(allowedMovement)})";
            return true;
        }

        private bool IsUnauthorizedPublicTransportLane(Entity lane, bool allowedOnPublicTransportLane)
        {
            if (lane == Entity.Null || !m_CarLaneData.TryGetComponent(lane, out CarLane laneData))
            {
                return false;
            }

            if ((laneData.m_Flags & Game.Net.CarLaneFlags.PublicOnly) == 0)
            {
                return false;
            }

            return !allowedOnPublicTransportLane;
        }

        private bool ShouldLogReroute(
            RoutePenaltySnapshot previousSnapshot,
            RoutePenaltySnapshot currentSnapshot)
        {
            bool allowPublicTransportLaneComparison =
                previousSnapshot.PublicTransportLanePolicyResolved &&
                currentSnapshot.PublicTransportLanePolicyResolved;

            int previousComparablePenalty =
                CalculateComparableTotalPenalty(
                    previousSnapshot,
                    allowPublicTransportLaneComparison);

            int currentComparablePenalty =
                CalculateComparableTotalPenalty(
                    currentSnapshot,
                    allowPublicTransportLaneComparison);

            return previousSnapshot.RouteHash != currentSnapshot.RouteHash &&
                previousComparablePenalty > currentComparablePenalty &&
                previousComparablePenalty > 0;
        }

        private void LogReroute(
            Entity vehicle,
            RoutePenaltySnapshot previousSnapshot,
            RoutePenaltySnapshot currentSnapshot)
        {
            bool allowPublicTransportLaneComparison =
                previousSnapshot.PublicTransportLanePolicyResolved &&
                currentSnapshot.PublicTransportLanePolicyResolved;

            int previousComparablePenalty =
                CalculateComparableTotalPenalty(
                    previousSnapshot,
                    allowPublicTransportLaneComparison);

            int currentComparablePenalty =
                CalculateComparableTotalPenalty(
                    currentSnapshot,
                    allowPublicTransportLaneComparison);

            int avoidedPenalty = previousComparablePenalty - currentComparablePenalty;
            string role = PublicTransportLanePolicy.DescribeVehicleRole(vehicle, ref m_TypeLookups);

            string previousComparableBreakdown =
                BuildComparableBreakdown(
                    previousSnapshot,
                    allowPublicTransportLaneComparison);

            string currentComparableBreakdown =
                BuildComparableBreakdown(
                    currentSnapshot,
                    allowPublicTransportLaneComparison);

            string comparisonMode = allowPublicTransportLaneComparison
                ? "full"
                : "excluding-unresolved-pt";

            string message =
                $"Pathfinding reroute (estimated): vehicle={vehicle}, role={role}, comparisonMode={comparisonMode}, " +
                $"avoidedPenalty={avoidedPenalty}, " +
                $"fromPenalty={previousComparablePenalty} [{previousComparableBreakdown}], " +
                $"toPenalty={currentComparablePenalty} [{currentComparableBreakdown}], " +
                $"fromTags={previousSnapshot.Tags}, toTags={currentSnapshot.Tags}";

            EnforcementTelemetry.RecordEvent(message);
            Mod.log.Info(message);
        }

        private static void RecordRerouteTelemetry(
            Entity vehicle,
            RoutePenaltySnapshot previousSnapshot,
            RoutePenaltySnapshot currentSnapshot)
        {
            bool allowPublicTransportLaneComparison =
                previousSnapshot.PublicTransportLanePolicyResolved &&
                currentSnapshot.PublicTransportLanePolicyResolved;

            bool avoidedPublicTransportLanePenalty =
                allowPublicTransportLaneComparison &&
                previousSnapshot.Profile.PublicTransportLaneSegments >
                currentSnapshot.Profile.PublicTransportLaneSegments;

            bool avoidedMidBlockPenalty =
                previousSnapshot.Profile.MidBlockTransitions >
                currentSnapshot.Profile.MidBlockTransitions;

            bool avoidedIntersectionPenalty =
                previousSnapshot.Profile.IntersectionTransitions >
                currentSnapshot.Profile.IntersectionTransitions;

            EnforcementPolicyImpactService.RecordAvoidedReroute(
                vehicle.Index,
                avoidedPublicTransportLanePenalty,
                avoidedMidBlockPenalty,
                avoidedIntersectionPenalty);
        }

        private void SweepInactiveSnapshots()
        {
            List<Entity> removedVehicles = null;
            foreach (KeyValuePair<Entity, RoutePenaltySnapshot> pair in m_LastSnapshots)
            {
                if (EntityManager.Exists(pair.Key) && m_CurrentLaneData.HasComponent(pair.Key))
                {
                    continue;
                }

                if (removedVehicles == null)
                {
                    removedVehicles = new List<Entity>();
                }

                removedVehicles.Add(pair.Key);
            }

            if (removedVehicles == null)
            {
                return;
            }

            for (int index = 0; index < removedVehicles.Count; index++)
            {
                m_LastSnapshots.Remove(removedVehicles[index]);
            }
        }

        private Entity GetOwner(Entity lane)
        {
            if (lane != Entity.Null && m_OwnerData.TryGetComponent(lane, out Owner owner))
            {
                return owner.m_Owner;
            }

            return Entity.Null;
        }

        private string DescribeUnauthorizedPublicTransportLaneTag(Entity lane)
        {
            return DescribeLaneKind(lane) + "(public-only, illegal)";
        }

        private string DescribeLaneKind(Entity lane)
        {
            if (m_ParkingLaneData.HasComponent(lane))
            {
                return "parking-lane";
            }

            if (m_GarageLaneData.HasComponent(lane))
            {
                return "garage-lane";
            }

            if (m_ConnectionLaneData.TryGetComponent(lane, out ConnectionLane connectionLane))
            {
                bool isRoadIntersectionConnection = (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0 && (connectionLane.m_Flags & ConnectionLaneFlags.Parking) == 0;
                if (isRoadIntersectionConnection)
                {
                    LaneMovement movement = m_CarLaneData.TryGetComponent(lane, out CarLane connectionCarLane)
                        ? GetMovement(connectionCarLane.m_Flags)
                        : LaneMovement.None;
                    string movementSuffix = movement == LaneMovement.None ? string.Empty : "-" + FormatMovement(movement);
                    return "intersection" + movementSuffix;
                }

                return "access-connection";
            }

            if (m_EdgeLaneData.HasComponent(lane))
            {
                return "road";
            }

            return "lane";
        }

        private bool IsAccessOrigin(Entity lane)
        {
            return m_ParkingLaneData.HasComponent(lane) ||
                m_GarageLaneData.HasComponent(lane) ||
                IsAccessConnection(lane);
        }

        private bool IsAccessConnection(Entity lane)
        {
            if (!m_ConnectionLaneData.TryGetComponent(lane, out ConnectionLane connectionLane))
            {
                return false;
            }

            bool parkingAccess = (connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0;
            bool roadConnection = (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0;
            return parkingAccess || !roadConnection;
        }

        private string DescribeAccessOriginTag(Entity lane)
        {
            if (m_ParkingLaneData.HasComponent(lane))
            {
                return "parking-origin";
            }

            if (m_GarageLaneData.HasComponent(lane))
            {
                return "garage-origin";
            }

            if (IsAccessConnection(lane))
            {
                return DescribeAccessConnectionTag(lane) + "-origin";
            }

            return "access-origin";
        }

        private string DescribeAccessConnectionTag(Entity lane)
        {
            if (!m_ConnectionLaneData.TryGetComponent(lane, out ConnectionLane connectionLane))
            {
                return "access-connection";
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
            {
                return "parking-connection";
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0)
            {
                return "building-service-access-connection";
            }

            return "access-connection";
        }

        private static bool LaneAllowsSideAccess(CarLane lane)
        {
            return (lane.m_Flags & (Game.Net.CarLaneFlags.SideConnection | Game.Net.CarLaneFlags.ParkingLeft | Game.Net.CarLaneFlags.ParkingRight)) != 0;
        }

        private static void AppendPenaltyTag(
            List<string> penaltyTags,
            string tag,
            ref int omittedTagCount)
        {
            if (penaltyTags == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(tag) || penaltyTags.Contains(tag))
            {
                return;
            }

            if (penaltyTags.Count >= MaxPenaltyTags)
            {
                omittedTagCount += 1;
                return;
            }

            penaltyTags.Add(tag);
        }

        private static string BuildTagSummary(List<string> penaltyTags, int omittedTagCount)
        {
            if (penaltyTags.Count == 0)
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

        private static uint HashLane(uint currentHash, Entity lane, bool unauthorizedPublicTransportLane)
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

        private static string BuildBreakdown(RoutePenaltyProfile profile)
        {
            List<string> parts = new List<string>(3);
            if (profile.PublicTransportLaneSegments > 0)
            {
                parts.Add($"PT-lane {profile.PublicTransportLaneSegments} x {EnforcementPenaltyService.GetPublicTransportLaneFine()}");
            }

            if (profile.MidBlockTransitions > 0)
            {
                parts.Add($"mid-block {profile.MidBlockTransitions} x {EnforcementPenaltyService.GetMidBlockCrossingFine()}");
            }

            if (profile.IntersectionTransitions > 0)
            {
                parts.Add($"intersection {profile.IntersectionTransitions} x {EnforcementPenaltyService.GetIntersectionMovementFine()}");
            }

            return parts.Count == 0 ? "none" : string.Join(", ", parts.ToArray());
        }

        private static int CalculateComparableTotalPenalty(
            RoutePenaltySnapshot snapshot,
            bool allowPublicTransportLaneComparison)
        {
            RoutePenaltyProfile comparableProfile = snapshot.Profile;

            if (!allowPublicTransportLaneComparison)
            {
                comparableProfile.PublicTransportLaneSegments = 0;
            }

            return CalculateTotalPenalty(comparableProfile);
        }

        private static string BuildComparableBreakdown(
            RoutePenaltySnapshot snapshot,
            bool allowPublicTransportLaneComparison)
        {
            RoutePenaltyProfile comparableProfile = snapshot.Profile;

            if (!allowPublicTransportLaneComparison)
            {
                comparableProfile.PublicTransportLaneSegments = 0;
            }

            return BuildBreakdown(comparableProfile);
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

            return parts.Count == 0 ? "none" : string.Join("+", parts.ToArray());
        }

        private static bool IsOppositeDirection(EdgeLane previousLane, EdgeLane currentLane)
        {
            float previousDirection = previousLane.m_EdgeDelta.y - previousLane.m_EdgeDelta.x;
            float currentDirection = currentLane.m_EdgeDelta.y - currentLane.m_EdgeDelta.x;
            return previousDirection * currentDirection < 0f;
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

        private struct RoutePenaltyProfile
        {
            public int PublicTransportLaneSegments;
            public int MidBlockTransitions;
            public int IntersectionTransitions;
        }

        private readonly struct RoutePenaltySnapshot
        {
            public readonly uint RouteHash;
            public readonly RoutePenaltyProfile Profile;
            public readonly int TotalPenalty;
            public readonly string Breakdown;
            public readonly string Tags;
            public readonly bool PublicTransportLanePolicyResolved;

            public RoutePenaltySnapshot(
                uint routeHash,
                RoutePenaltyProfile profile,
                string breakdown,
                string tags,
                bool publicTransportLanePolicyResolved)
            {
                RouteHash = routeHash;
                Profile = profile;
                TotalPenalty = CalculateTotalPenalty(profile);
                Breakdown = breakdown;
                Tags = tags;
                PublicTransportLanePolicyResolved = publicTransportLanePolicyResolved;
            }
        }

        private static int CalculateTotalPenalty(RoutePenaltyProfile profile)
        {
            return profile.PublicTransportLaneSegments * EnforcementPenaltyService.GetPublicTransportLaneFine() +
                profile.MidBlockTransitions * EnforcementPenaltyService.GetMidBlockCrossingFine() +
                profile.IntersectionTransitions * EnforcementPenaltyService.GetIntersectionMovementFine();
        }
    }
}
