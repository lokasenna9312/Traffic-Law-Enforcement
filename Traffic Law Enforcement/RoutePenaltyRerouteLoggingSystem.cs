using System.Collections.Generic;
using System.Text;
using Game;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Routes;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public static class RouteDebugLoggingTelemetry
    {
        public static bool Enabled { get; private set; }
        public static int CachedSnapshotCount { get; private set; }
        public static int LastCandidateCount { get; private set; }
        public static int LastRouteSelectionLogsEmitted { get; private set; }

        public static void SetState(bool enabled, int cachedSnapshotCount, int lastCandidateCount, int lastRouteSelectionLogsEmitted)
        {
            Enabled = enabled;
            CachedSnapshotCount = cachedSnapshotCount;
            LastCandidateCount = lastCandidateCount;
            LastRouteSelectionLogsEmitted = lastRouteSelectionLogsEmitted;
        }
    }
    [BurstCompile]
    public partial class RoutePenaltyRerouteLoggingSystem : GameSystemBase
    {
        [System.Flags]
        private enum CandidateChangeReason
        {
            None = 0,
            CurrentLane = 1 << 0,
            NavigationLane = 1 << 1,
            Car = 1 << 2,
            Target = 1 << 3,
            CurrentRoute = 1 << 4,
            PathOwner = 1 << 5,
            PathInformation = 1 << 6,
        }

        [System.Flags]
        private enum PublicTransportLaneDiagnosticComponentBits : byte
        {
            None = 0,
            RouteLane = 1 << 0,
            SlaveLane = 1 << 1,
            EdgeLane = 1 << 2,
            ConnectionLane = 1 << 3,
        }

        private enum DiagnosticBoolState : byte
        {
            Unknown = 0,
            False = 1,
            True = 2,
        }

        private readonly struct WatchedPublicTransportLaneDiagnosticKey :
            System.IEquatable<WatchedPublicTransportLaneDiagnosticKey>
        {
            public readonly Entity Vehicle;
            public readonly Entity Lane;

            public WatchedPublicTransportLaneDiagnosticKey(Entity vehicle, Entity lane)
            {
                Vehicle = vehicle;
                Lane = lane;
            }

            public bool Equals(WatchedPublicTransportLaneDiagnosticKey other)
            {
                return Vehicle == other.Vehicle &&
                    Lane == other.Lane;
            }

            public override bool Equals(object obj)
            {
                return obj is WatchedPublicTransportLaneDiagnosticKey other &&
                    Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = Vehicle.Index;
                    hash = (hash * 397) ^ Vehicle.Version;
                    hash = (hash * 397) ^ Lane.Index;
                    hash = (hash * 397) ^ Lane.Version;
                    return hash;
                }
            }
        }

        private readonly struct WatchedPublicTransportLaneDiagnosticState
        {
            public readonly Entity CurrentLane;
            public readonly Entity Owner;
            public readonly Entity Aggregate;
            public readonly Entity ResolvedRoad;
            public readonly PublicTransportLaneDiagnosticComponentBits LaneComponentBits;
            public readonly bool PublicOnly;
            public readonly bool UnauthorizedPublicTransportLane;
            public readonly bool HasResolvedPublicTransportLanePolicy;
            public readonly DiagnosticBoolState VanillaAllows;
            public readonly DiagnosticBoolState ModAllows;

            public WatchedPublicTransportLaneDiagnosticState(
                Entity currentLane,
                Entity owner,
                Entity aggregate,
                Entity resolvedRoad,
                PublicTransportLaneDiagnosticComponentBits laneComponentBits,
                bool publicOnly,
                bool unauthorizedPublicTransportLane,
                bool hasResolvedPublicTransportLanePolicy,
                DiagnosticBoolState vanillaAllows,
                DiagnosticBoolState modAllows)
            {
                CurrentLane = currentLane;
                Owner = owner;
                Aggregate = aggregate;
                ResolvedRoad = resolvedRoad;
                LaneComponentBits = laneComponentBits;
                PublicOnly = publicOnly;
                UnauthorizedPublicTransportLane = unauthorizedPublicTransportLane;
                HasResolvedPublicTransportLanePolicy = hasResolvedPublicTransportLanePolicy;
                VanillaAllows = vanillaAllows;
                ModAllows = modAllows;
            }
        }

        private readonly struct LaneResolutionDiagnostic
        {
            public readonly Entity Owner;
            public readonly Entity Aggregate;
            public readonly Entity ResolvedRoad;
            public readonly string DisplayText;
            public readonly string OwnerText;
            public readonly string AggregateText;
            public readonly string ResolvedRoadText;
            public readonly string NameSource;

            public LaneResolutionDiagnostic(
                Entity owner,
                Entity aggregate,
                Entity resolvedRoad,
                string displayText,
                string ownerText,
                string aggregateText,
                string resolvedRoadText,
                string nameSource)
            {
                Owner = owner;
                Aggregate = aggregate;
                ResolvedRoad = resolvedRoad;
                DisplayText = displayText;
                OwnerText = ownerText;
                AggregateText = aggregateText;
                ResolvedRoadText = resolvedRoadText;
                NameSource = nameSource;
            }
        }

        private const CandidateChangeReason RouteSelectionChangeReasons =
            CandidateChangeReason.Target |
            CandidateChangeReason.CurrentRoute |
            CandidateChangeReason.PathOwner |
            CandidateChangeReason.PathInformation;

        private EntityQuery m_CachedVehicleQuery;
        private const int MaxPenaltyTags = 6;
        private const int MaxRouteSelectionChangeLogsPerUpdate = 12;
        private const int SnapshotSweepInterval = 2048;
        private const int MaxPublicTransportLaneDecisionDiagnosticLogsPerUpdate = 8;
        private int m_PublicTransportLaneDecisionDiagnosticLogsThisUpdate;

        private EntityQuery m_CarQuery;
        private EntityQuery m_CurrentLaneChangedQuery;
        private EntityQuery m_NavigationLaneChangedQuery;
        private EntityQuery m_CarChangedQuery;
        private EntityQuery m_TargetChangedQuery;
        private EntityQuery m_CurrentRouteChangedQuery;
        private EntityQuery m_PathOwnerChangedQuery;
        private EntityQuery m_PathInformationChangedQuery;
        private BufferLookup<CarNavigationLane> m_NavigationLaneData;
        private ComponentLookup<CarCurrentLane> m_CurrentLaneData;
        private ComponentLookup<Target> m_TargetData;
        private ComponentLookup<CurrentRoute> m_CurrentRouteData;
        private ComponentLookup<PathOwner> m_PathOwnerData;
        private ComponentLookup<PathInformation> m_PathInformationData;
        private ComponentLookup<Game.Objects.Transform> m_TransformData;
        private ComponentLookup<Owner> m_OwnerData;
        private ComponentLookup<Aggregated> m_AggregatedData;
        private ComponentLookup<Game.Prefabs.PrefabRef> m_PrefabRefData;
        private ComponentLookup<CarLane> m_CarLaneData;
        private ComponentLookup<Game.Prefabs.CarData> m_PrefabCarData;
        private ComponentLookup<CargoTransport> m_CargoTransportData;
        private ComponentLookup<PublicTransport> m_PublicTransportData;
        private ComponentLookup<PublicTransportLanePendingExit> m_PendingExitData;
        private ComponentLookup<SlaveLane> m_SlaveLaneData;
        private ComponentLookup<RouteLane> m_RouteLaneData;
        private ComponentLookup<VehicleTrafficLawProfile> m_ProfileData;
        private ComponentLookup<EdgeLane> m_EdgeLaneData;
        private ComponentLookup<ParkingLane> m_ParkingLaneData;
        private ComponentLookup<GarageLane> m_GarageLaneData;
        private ComponentLookup<ConnectionLane> m_ConnectionLaneData;
        private BufferLookup<SubLane> m_SubLaneData;
        private BufferLookup<PathElement> m_PathElementData;
        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;
        private EntityTypeHandle m_EntityTypeHandle;
        private readonly Dictionary<Entity, RoutePenaltyInspectionResult> m_LastSnapshots = new Dictionary<Entity, RoutePenaltyInspectionResult>();
        private readonly Dictionary<Entity, RouteSelectionChangeSnapshot> m_LastRouteSelectionSnapshots = new Dictionary<Entity, RouteSelectionChangeSnapshot>();
        private readonly Dictionary<Entity, CandidateChangeReason> m_CandidateVehicles = new Dictionary<Entity, CandidateChangeReason>();
        private readonly Dictionary<WatchedPublicTransportLaneDiagnosticKey, WatchedPublicTransportLaneDiagnosticState> m_LastWatchedPublicTransportLaneDiagnostics = new Dictionary<WatchedPublicTransportLaneDiagnosticKey, WatchedPublicTransportLaneDiagnosticState>();
        private readonly List<Entity> m_RemovedVehiclesBuffer = new List<Entity>();
        private readonly List<WatchedPublicTransportLaneDiagnosticKey> m_RemovedWatchedPublicTransportLaneDiagnosticKeys = new List<WatchedPublicTransportLaneDiagnosticKey>();
        private Game.UI.NameSystem m_NameSystem;
        private Game.Prefabs.PrefabSystem m_PrefabSystem;
        private const int RerouteSummaryFlushInterval = 64;
        private int m_RerouteSummaryPendingCount;
        private int m_RerouteSummaryPendingAvoidedPenalty;
        private int m_RerouteSummaryPendingPublicTransport;
        private int m_RerouteSummaryPendingMidBlock;
        private int m_RerouteSummaryPendingIntersection;
        private int m_RerouteSummaryPendingUpdates;
        private int m_UpdateCount;
        private int m_LastObservedRuntimeWorldGeneration = -1;
        private bool m_ShouldSeedSnapshotHistoryAfterRuntimeReload;

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
            m_TargetChangedQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadOnly<Target>());
            m_TargetChangedQuery.SetChangedVersionFilter(ComponentType.ReadOnly<Target>());
            m_CurrentRouteChangedQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadOnly<CurrentRoute>());
            m_CurrentRouteChangedQuery.SetChangedVersionFilter(ComponentType.ReadOnly<CurrentRoute>());
            m_PathOwnerChangedQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadOnly<PathOwner>());
            m_PathOwnerChangedQuery.SetChangedVersionFilter(ComponentType.ReadOnly<PathOwner>());
            m_PathInformationChangedQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadOnly<PathInformation>());
            m_PathInformationChangedQuery.SetChangedVersionFilter(ComponentType.ReadOnly<PathInformation>());
            m_NavigationLaneData = GetBufferLookup<CarNavigationLane>(true);
            m_CurrentLaneData = GetComponentLookup<CarCurrentLane>(true);
            m_TargetData = GetComponentLookup<Target>(true);
            m_CurrentRouteData = GetComponentLookup<CurrentRoute>(true);
            m_PathOwnerData = GetComponentLookup<PathOwner>(true);
            m_PathInformationData = GetComponentLookup<PathInformation>(true);
            m_TransformData = GetComponentLookup<Game.Objects.Transform>(true);
            m_OwnerData = GetComponentLookup<Owner>(true);
            m_AggregatedData = GetComponentLookup<Aggregated>(true);
            m_PrefabRefData = GetComponentLookup<Game.Prefabs.PrefabRef>(true);
            m_CarLaneData = GetComponentLookup<CarLane>(true);
            m_PrefabCarData = GetComponentLookup<Game.Prefabs.CarData>(true);
            m_CargoTransportData = GetComponentLookup<CargoTransport>(true);
            m_PublicTransportData = GetComponentLookup<PublicTransport>(true);
            m_PendingExitData = GetComponentLookup<PublicTransportLanePendingExit>(true);
            m_SlaveLaneData = GetComponentLookup<SlaveLane>(true);
            m_RouteLaneData = GetComponentLookup<RouteLane>(true);
            m_EdgeLaneData = GetComponentLookup<EdgeLane>(true);
            m_ParkingLaneData = GetComponentLookup<ParkingLane>(true);
            m_GarageLaneData = GetComponentLookup<GarageLane>(true);
            m_ConnectionLaneData = GetComponentLookup<ConnectionLane>(true);
            m_SubLaneData = GetBufferLookup<SubLane>(true);
            m_PathElementData = GetBufferLookup<PathElement>(true);
            m_ProfileData = GetComponentLookup<VehicleTrafficLawProfile>(true);
            m_TypeLookups = PublicTransportLaneVehicleTypeLookups.Create(this);
            m_NameSystem = World.GetOrCreateSystemManaged<Game.UI.NameSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<Game.Prefabs.PrefabSystem>();
            m_CachedVehicleQuery = GetEntityQuery(ComponentType.ReadOnly<Car>(), ComponentType.ReadOnly<CarCurrentLane>());
            RequireForUpdate(m_CarQuery);
        }

        [BurstCompile]
        protected override void OnUpdate()
        {
            HandleRuntimeWorldReload();
            bool estimatedRerouteLoggingEnabled = EnforcementLoggingPolicy.ShouldLogEstimatedReroutes();
            bool routeSelectionSummaryLoggingEnabled =
                EnforcementLoggingPolicy.ShouldLogAllVehicleRouteSelectionChanges();
            bool focusedRouteRebuildDiagnosticsLoggingEnabled =
                EnforcementLoggingPolicy.ShouldLogFocusedRouteRebuildDiagnostics();
            bool pathfindingPenaltyDiagnosticLoggingEnabled =
                EnforcementLoggingPolicy.ShouldLogPathfindingPenaltyDiagnostics();
            bool trackPolicyImpactAvoidances = Mod.IsEnforcementEnabled;
            bool observeRouteDebugState =
                EnforcementLoggingPolicy.ShouldObserveRouteDebugState();
            bool restrictVehicleSpecificRouteLogsToWatchedVehicles =
                EnforcementLoggingPolicy.ShouldRestrictVehicleSpecificRouteDebugLogsToWatchedVehicles();
            bool burstLoggingActive = EnforcementLoggingPolicy.IsBurstLoggingActive;
            int routeSelectionLogLimit =
                burstLoggingActive
                    ? BurstLoggingService.BurstRouteSelectionChangeLogsPerUpdate
                    : MaxRouteSelectionChangeLogsPerUpdate;
            m_PublicTransportLaneDecisionDiagnosticLogsThisUpdate = 0;
            if (!observeRouteDebugState && !trackPolicyImpactAvoidances)
            {
                ClearRouteLoggingState();
                return;
            }

            bool needsWatchedVehicleState =
                restrictVehicleSpecificRouteLogsToWatchedVehicles ||
                focusedRouteRebuildDiagnosticsLoggingEnabled;
            if (needsWatchedVehicleState ||
                m_LastWatchedPublicTransportLaneDiagnostics.Count > 0)
            {
                FocusedLoggingService.PruneMissingVehicles(EntityManager);
                PruneWatchedPublicTransportLaneDecisionDiagnostics();
            }

            bool hasWatchedVehicles = FocusedLoggingService.HasWatchedVehicles;
            bool trackRouteSelectionChanges =
                (routeSelectionSummaryLoggingEnabled &&
                 (!restrictVehicleSpecificRouteLogsToWatchedVehicles || hasWatchedVehicles)) ||
                (focusedRouteRebuildDiagnosticsLoggingEnabled && hasWatchedVehicles);
            bool trackVehicleSpecificRouteLogs =
                estimatedRerouteLoggingEnabled ||
                pathfindingPenaltyDiagnosticLoggingEnabled ||
                trackRouteSelectionChanges;
            bool needsSnapshotHistory =
                trackPolicyImpactAvoidances ||
                estimatedRerouteLoggingEnabled ||
                trackRouteSelectionChanges;

            if (!trackVehicleSpecificRouteLogs && !trackPolicyImpactAvoidances)
            {
                ClearRouteLoggingState();
                return;
            }

            if (!needsSnapshotHistory)
            {
                m_LastSnapshots.Clear();
                m_LastRouteSelectionSnapshots.Clear();
            }

            bool hasCandidateChanges =
                !m_CurrentLaneChangedQuery.IsEmptyIgnoreFilter ||
                !m_NavigationLaneChangedQuery.IsEmptyIgnoreFilter ||
                !m_CarChangedQuery.IsEmptyIgnoreFilter ||
                (trackRouteSelectionChanges &&
                 (!m_TargetChangedQuery.IsEmptyIgnoreFilter ||
                  !m_CurrentRouteChangedQuery.IsEmptyIgnoreFilter ||
                  !m_PathOwnerChangedQuery.IsEmptyIgnoreFilter ||
                  !m_PathInformationChangedQuery.IsEmptyIgnoreFilter));

            if (!hasCandidateChanges)
            {
                m_UpdateCount += 1;
                if ((m_UpdateCount % SnapshotSweepInterval) == 0)
                {
                    m_CurrentLaneData.Update(this);
                    SweepInactiveSnapshots();
                }

                RouteDebugLoggingTelemetry.SetState(true, m_LastSnapshots.Count, 0, 0);
                return;
            }

            m_EntityTypeHandle = GetEntityTypeHandle();
            UpdateCoreInspectionLookups();

            if (trackRouteSelectionChanges)
            {
                UpdateRouteSelectionLookups();
            }
            else
            {
                m_LastRouteSelectionSnapshots.Clear();
            }

            if (focusedRouteRebuildDiagnosticsLoggingEnabled && hasWatchedVehicles)
            {
                UpdateFocusedRouteDiagnosticLookups();
            }

            if (needsSnapshotHistory && m_ShouldSeedSnapshotHistoryAfterRuntimeReload)
            {
                SeedSnapshotHistoryAfterRuntimeReload(trackRouteSelectionChanges);
            }

            bool requireAllCandidateVehicles =
                trackPolicyImpactAvoidances ||
                (routeSelectionSummaryLoggingEnabled &&
                 !restrictVehicleSpecificRouteLogsToWatchedVehicles) ||
                (estimatedRerouteLoggingEnabled &&
                 !restrictVehicleSpecificRouteLogsToWatchedVehicles) ||
                (pathfindingPenaltyDiagnosticLoggingEnabled &&
                 !restrictVehicleSpecificRouteLogsToWatchedVehicles);

            bool watchedOnlyCandidates = !requireAllCandidateVehicles;

            m_CandidateVehicles.Clear();
            CollectCandidateVehicles(
                m_CurrentLaneChangedQuery,
                watchedOnlyCandidates,
                CandidateChangeReason.CurrentLane);
            CollectCandidateVehicles(
                m_NavigationLaneChangedQuery,
                watchedOnlyCandidates,
                CandidateChangeReason.NavigationLane);
            CollectCandidateVehicles(
                m_CarChangedQuery,
                watchedOnlyCandidates,
                CandidateChangeReason.Car);
            if (trackRouteSelectionChanges)
            {
                CollectCandidateVehicles(
                    m_TargetChangedQuery,
                    watchedOnlyCandidates,
                    CandidateChangeReason.Target);
                CollectCandidateVehicles(
                    m_CurrentRouteChangedQuery,
                    watchedOnlyCandidates,
                    CandidateChangeReason.CurrentRoute);
                CollectCandidateVehicles(
                    m_PathOwnerChangedQuery,
                    watchedOnlyCandidates,
                    CandidateChangeReason.PathOwner);
                CollectCandidateVehicles(
                    m_PathInformationChangedQuery,
                    watchedOnlyCandidates,
                    CandidateChangeReason.PathInformation);
            }

            int routeSelectionLogsEmitted = 0;
            int routeSelectionLogsDropped = 0;
            foreach (KeyValuePair<Entity, CandidateChangeReason> candidate in m_CandidateVehicles)
            {
                Entity vehicle = candidate.Key;
                CandidateChangeReason candidateReasons = candidate.Value;
                if (!m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLane))
                {
                    continue;
                }

                bool watchedVehicle = FocusedLoggingService.IsWatched(vehicle);
                bool allowVehicleSpecificVisibleLogs =
                    !restrictVehicleSpecificRouteLogsToWatchedVehicles ||
                    watchedVehicle;
                bool allowVehicleSpecificPenaltyDiagnostics =
                    pathfindingPenaltyDiagnosticLoggingEnabled &&
                    allowVehicleSpecificVisibleLogs;

                if (!needsSnapshotHistory)
                {
                    if (allowVehicleSpecificPenaltyDiagnostics)
                    {
                        bool hasNavigationLanes =
                            m_NavigationLaneData.TryGetBuffer(
                                vehicle,
                                out DynamicBuffer<CarNavigationLane> navigationLanes);

                        LogPublicTransportLaneDecisionDiagnostics(
                            vehicle,
                            currentLane.m_Lane,
                            navigationLanes,
                            hasNavigationLanes,
                            false);
                    }

                    continue;
                }

                RoutePenaltyInspectionResult snapshot =
                    BuildSnapshot(
                        vehicle,
                        currentLane,
                        captureDebugStrings:
                            estimatedRerouteLoggingEnabled ||
                            routeSelectionSummaryLoggingEnabled ||
                            focusedRouteRebuildDiagnosticsLoggingEnabled,
                        allowVehicleSpecificPenaltyDiagnostics:
                            allowVehicleSpecificPenaltyDiagnostics);

                bool rerouteDetected = false;
                if (m_LastSnapshots.TryGetValue(vehicle, out RoutePenaltyInspectionResult previousSnapshot))
                {
                    rerouteDetected = ShouldLogReroute(previousSnapshot, snapshot);
                    if (rerouteDetected)
                    {
                        RecordRerouteTelemetry(vehicle, previousSnapshot, snapshot);

                        bool allowPublicTransportLaneComparison =
                            previousSnapshot.PublicTransportLanePolicyResolved &&
                            snapshot.PublicTransportLanePolicyResolved;

                        int previousComparablePenalty =
                            CalculateComparableTotalPenalty(
                                previousSnapshot,
                                allowPublicTransportLaneComparison);

                        int currentComparablePenalty =
                            CalculateComparableTotalPenalty(
                                snapshot,
                                allowPublicTransportLaneComparison);

                        int avoidedPenalty = previousComparablePenalty - currentComparablePenalty;

                        bool avoidedPublicTransportLanePenalty =
                            allowPublicTransportLaneComparison &&
                            previousSnapshot.Profile.PublicTransportLaneSegments >
                            snapshot.Profile.PublicTransportLaneSegments;

                        bool avoidedMidBlockPenalty =
                            previousSnapshot.Profile.MidBlockTransitions >
                            snapshot.Profile.MidBlockTransitions;

                        bool avoidedIntersectionPenalty =
                            previousSnapshot.Profile.IntersectionTransitions >
                            snapshot.Profile.IntersectionTransitions;

                        if (estimatedRerouteLoggingEnabled && allowVehicleSpecificVisibleLogs)
                        {
                            if (watchedVehicle)
                            {
                                LogReroute(
                                    vehicle,
                                    previousSnapshot,
                                    snapshot,
                                    watchedVehicle);
                            }
                            else
                            {
                                m_RerouteSummaryPendingCount += 1;
                                m_RerouteSummaryPendingAvoidedPenalty += avoidedPenalty;

                                if (avoidedPublicTransportLanePenalty)
                                {
                                    m_RerouteSummaryPendingPublicTransport += 1;
                                }

                                if (avoidedMidBlockPenalty)
                                {
                                    m_RerouteSummaryPendingMidBlock += 1;
                                }

                                if (avoidedIntersectionPenalty)
                                {
                                    m_RerouteSummaryPendingIntersection += 1;
                                }
                            }
                        }
                    }

                    m_LastSnapshots[vehicle] = snapshot;
                }
                else
                {
                    m_LastSnapshots[vehicle] = snapshot;
                }

                if (!trackRouteSelectionChanges)
                {
                    continue;
                }

                bool hasRouteSelectionCandidateChanges =
                    (candidateReasons & RouteSelectionChangeReasons) != 0;
                if (!hasRouteSelectionCandidateChanges && !rerouteDetected)
                {
                    continue;
                }

                bool hasPreviousRouteSelectionSnapshot =
                    m_LastRouteSelectionSnapshots.TryGetValue(
                        vehicle,
                        out RouteSelectionChangeSnapshot previousRouteSelectionSnapshot);
                RouteSelectionChangeSnapshot routeSelectionSnapshot =
                    BuildRouteSelectionSnapshot(
                        vehicle,
                        currentLane.m_Lane,
                        snapshot,
                        hasPreviousRouteSelectionSnapshot
                            ? previousRouteSelectionSnapshot
                            : (RouteSelectionChangeSnapshot?)null,
                        candidateReasons);

                if (hasPreviousRouteSelectionSnapshot)
                {
                    if (ShouldLogRouteSelectionChange(
                            previousRouteSelectionSnapshot,
                            routeSelectionSnapshot,
                            rerouteDetected))
                    {
                        bool emitFocusedDiagnostics =
                            focusedRouteRebuildDiagnosticsLoggingEnabled &&
                            watchedVehicle;
                        bool emitSummary =
                            (routeSelectionSummaryLoggingEnabled &&
                             allowVehicleSpecificVisibleLogs) ||
                            emitFocusedDiagnostics;

                        if (!emitSummary)
                        {
                            m_LastRouteSelectionSnapshots[vehicle] = routeSelectionSnapshot;
                            continue;
                        }

                        if (!watchedVehicle &&
                            !emitFocusedDiagnostics &&
                            IsLowValueNonWatchedRouteSelectionChange(
                                previousRouteSelectionSnapshot,
                                routeSelectionSnapshot,
                                rerouteDetected))
                        {
                            m_LastRouteSelectionSnapshots[vehicle] = routeSelectionSnapshot;
                            continue;
                        }

                        if (emitFocusedDiagnostics ||
                            routeSelectionLogsEmitted < routeSelectionLogLimit)
                        {
                            LogRouteSelectionChange(
                                vehicle,
                                previousRouteSelectionSnapshot,
                                routeSelectionSnapshot,
                                rerouteDetected,
                                watchedVehicle,
                                emitFocusedDiagnostics);

                            if (!emitFocusedDiagnostics)
                            {
                                routeSelectionLogsEmitted += 1;
                            }
                        }
                        else
                        {
                            routeSelectionLogsDropped += 1;
                        }
                    }

                    m_LastRouteSelectionSnapshots[vehicle] = routeSelectionSnapshot;
                }
                else
                {
                    m_LastRouteSelectionSnapshots[vehicle] = routeSelectionSnapshot;
                }
            }

            if (m_RerouteSummaryPendingCount > 0)
            {
                m_RerouteSummaryPendingUpdates += 1;

                if (m_RerouteSummaryPendingUpdates >= RerouteSummaryFlushInterval)
                {
                    string rerouteSummaryMessage =
                        $"[REROUTE_SUMMARY] count={m_RerouteSummaryPendingCount}, " +
                        $"avoidedPenalty={m_RerouteSummaryPendingAvoidedPenalty}, " +
                        $"avoidedPT={m_RerouteSummaryPendingPublicTransport}, " +
                        $"avoidedMidBlock={m_RerouteSummaryPendingMidBlock}, " +
                        $"avoidedIntersection={m_RerouteSummaryPendingIntersection}, " +
                        $"updates={m_RerouteSummaryPendingUpdates}";

                    EnforcementTelemetry.RecordEvent(rerouteSummaryMessage);
                    Mod.log.Info(rerouteSummaryMessage);

                    m_RerouteSummaryPendingCount = 0;
                    m_RerouteSummaryPendingAvoidedPenalty = 0;
                    m_RerouteSummaryPendingPublicTransport = 0;
                    m_RerouteSummaryPendingMidBlock = 0;
                    m_RerouteSummaryPendingIntersection = 0;
                    m_RerouteSummaryPendingUpdates = 0;
                }
            }

            if (trackRouteSelectionChanges && routeSelectionLogsDropped > 0)
            {
                string droppedMessage =
                    $"Route selection change logging throttled: dropped={routeSelectionLogsDropped}, emitted={routeSelectionLogsEmitted}, candidates={m_CandidateVehicles.Count}, limit={routeSelectionLogLimit}, burstActive={burstLoggingActive}";
                EnforcementTelemetry.RecordEvent(droppedMessage);
                Mod.log.Info(droppedMessage);
            }

            m_UpdateCount += 1;
            if ((m_UpdateCount % SnapshotSweepInterval) == 0)
            {
                SweepInactiveSnapshots();
            }

            RouteDebugLoggingTelemetry.SetState(true, m_LastSnapshots.Count, m_CandidateVehicles.Count, routeSelectionLogsEmitted);
        }

        private void ClearRouteLoggingState()
        {
            m_RerouteSummaryPendingCount = 0;
            m_RerouteSummaryPendingAvoidedPenalty = 0;
            m_RerouteSummaryPendingPublicTransport = 0;
            m_RerouteSummaryPendingMidBlock = 0;
            m_RerouteSummaryPendingIntersection = 0;
            m_RerouteSummaryPendingUpdates = 0;
            m_LastSnapshots.Clear();
            m_LastRouteSelectionSnapshots.Clear();
            m_CandidateVehicles.Clear();
            m_LastWatchedPublicTransportLaneDiagnostics.Clear();
            RouteDebugLoggingTelemetry.SetState(false, 0, 0, 0);
        }

        private void PruneWatchedPublicTransportLaneDecisionDiagnostics()
        {
            if (m_LastWatchedPublicTransportLaneDiagnostics.Count == 0)
            {
                return;
            }

            if (!FocusedLoggingService.HasWatchedVehicles)
            {
                m_LastWatchedPublicTransportLaneDiagnostics.Clear();
                return;
            }

            m_CurrentLaneData.Update(this);
            m_RemovedWatchedPublicTransportLaneDiagnosticKeys.Clear();
            foreach (KeyValuePair<WatchedPublicTransportLaneDiagnosticKey, WatchedPublicTransportLaneDiagnosticState> pair in
                     m_LastWatchedPublicTransportLaneDiagnostics)
            {
                Entity vehicle = pair.Key.Vehicle;
                if (!EntityManager.Exists(vehicle) ||
                    !EntityManager.Exists(pair.Key.Lane) ||
                    !FocusedLoggingService.IsWatched(vehicle))
                {
                    m_RemovedWatchedPublicTransportLaneDiagnosticKeys.Add(pair.Key);
                    continue;
                }

                Entity activeCurrentLane =
                    m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLaneData)
                        ? currentLaneData.m_Lane
                        : Entity.Null;
                if (activeCurrentLane != pair.Value.CurrentLane)
                {
                    m_RemovedWatchedPublicTransportLaneDiagnosticKeys.Add(pair.Key);
                }
            }

            for (int index = 0; index < m_RemovedWatchedPublicTransportLaneDiagnosticKeys.Count; index += 1)
            {
                m_LastWatchedPublicTransportLaneDiagnostics.Remove(
                    m_RemovedWatchedPublicTransportLaneDiagnosticKeys[index]);
            }

            m_RemovedWatchedPublicTransportLaneDiagnosticKeys.Clear();
        }

        private void HandleRuntimeWorldReload()
        {
            int currentGeneration = EnforcementSaveDataSystem.RuntimeWorldGeneration;
            if (m_LastObservedRuntimeWorldGeneration == currentGeneration)
            {
                return;
            }

            m_LastObservedRuntimeWorldGeneration = currentGeneration;
            m_RerouteSummaryPendingCount = 0;
            m_RerouteSummaryPendingAvoidedPenalty = 0;
            m_RerouteSummaryPendingPublicTransport = 0;
            m_RerouteSummaryPendingMidBlock = 0;
            m_RerouteSummaryPendingIntersection = 0;
            m_RerouteSummaryPendingUpdates = 0;
            m_LastSnapshots.Clear();
            m_LastRouteSelectionSnapshots.Clear();
            m_CandidateVehicles.Clear();
            m_LastWatchedPublicTransportLaneDiagnostics.Clear();
            m_UpdateCount = 0;
            m_ShouldSeedSnapshotHistoryAfterRuntimeReload = true;
            ObsoleteAttemptCorrelationService.ResetForRuntimeWorldGeneration(currentGeneration);
            FocusedLoggingService.ClearWatchedVehiclesForRuntimeWorldReset(currentGeneration);
            RouteDebugLoggingTelemetry.SetState(false, 0, 0, 0);

        }

        private void SeedSnapshotHistoryAfterRuntimeReload(bool trackRouteSelectionChanges)
        {
            NativeArray<Entity> vehicles =
                m_CachedVehicleQuery.ToEntityArray(Allocator.Temp);
            int seededSnapshotCount = 0;
            int seededRouteSelectionSnapshotCount = 0;

            try
            {
                for (int index = 0; index < vehicles.Length; index += 1)
                {
                    Entity vehicle = vehicles[index];
                    if (!m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLane))
                    {
                        continue;
                    }

                    RoutePenaltyInspectionResult snapshot =
                        BuildSnapshot(
                            vehicle,
                            currentLane,
                            captureDebugStrings: false,
                            allowVehicleSpecificPenaltyDiagnostics: false);

                    m_LastSnapshots[vehicle] = snapshot;
                    seededSnapshotCount += 1;

                    if (!trackRouteSelectionChanges)
                    {
                        continue;
                    }

                    RouteSelectionChangeSnapshot routeSelectionSnapshot =
                        BuildRouteSelectionSnapshot(
                            vehicle,
                            currentLane.m_Lane,
                            snapshot,
                            previousSnapshot: null,
                            CandidateChangeReason.None);

                    m_LastRouteSelectionSnapshots[vehicle] =
                        routeSelectionSnapshot;
                    seededRouteSelectionSnapshotCount += 1;
                }
            }
            finally
            {
                vehicles.Dispose();
            }

            m_ShouldSeedSnapshotHistoryAfterRuntimeReload = false;

            if (EnforcementLoggingPolicy.ShouldLogRerouteDiagnostics())
            {
                Mod.log.Info(
                    "[ENFORCEMENT_REROUTE_STATE] " +
                    $"phase=SeedSnapshotHistoryAfterRuntimeReload, snapshots={seededSnapshotCount}, " +
                    $"routeSelectionSnapshots={seededRouteSelectionSnapshotCount}, runtimeWorldGeneration={m_LastObservedRuntimeWorldGeneration}");
            }
        }

        private void UpdateCoreInspectionLookups()
        {
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
        }

        private void UpdateRouteSelectionLookups()
        {
            m_TargetData.Update(this);
            m_CurrentRouteData.Update(this);
            m_PathOwnerData.Update(this);
            m_PathInformationData.Update(this);
            m_PathElementData.Update(this);
        }

        private void UpdateFocusedRouteDiagnosticLookups()
        {
            m_TransformData.Update(this);
            m_AggregatedData.Update(this);
            m_PrefabRefData.Update(this);
            m_PrefabCarData.Update(this);
            m_CargoTransportData.Update(this);
            m_PublicTransportData.Update(this);
            m_PendingExitData.Update(this);
            m_SlaveLaneData.Update(this);
            m_RouteLaneData.Update(this);
            m_SubLaneData.Update(this);
        }

        private void CollectCandidateVehicles(
            EntityQuery query,
            bool watchedOnly,
            CandidateChangeReason changeReason)
        {
            NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            try
            {
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex += 1)
                {
                    NativeArray<Entity> vehicles = chunks[chunkIndex].GetNativeArray(m_EntityTypeHandle);
                    for (int index = 0; index < vehicles.Length; index += 1)
                    {
                        Entity vehicle = vehicles[index];
                        if (watchedOnly && !FocusedLoggingService.IsWatched(vehicle))
                        {
                            continue;
                        }

                        if (m_CandidateVehicles.TryGetValue(vehicle, out CandidateChangeReason existingReasons))
                        {
                            m_CandidateVehicles[vehicle] = existingReasons | changeReason;
                        }
                        else
                        {
                            m_CandidateVehicles[vehicle] = changeReason;
                        }
                    }
                }
            }
            finally
            {
                chunks.Dispose();
            }
        }

        private RoutePenaltyInspectionResult BuildSnapshot(
            Entity vehicle,
            CarCurrentLane currentLane,
            bool captureDebugStrings,
            bool allowVehicleSpecificPenaltyDiagnostics)
        {
            bool hasNavigationLanes =
                m_NavigationLaneData.TryGetBuffer(vehicle, out DynamicBuffer<CarNavigationLane> navigationLanes);

            if (allowVehicleSpecificPenaltyDiagnostics)
            {
                LogPublicTransportLaneDecisionDiagnostics(
                    vehicle,
                    currentLane.m_Lane,
                    navigationLanes,
                    hasNavigationLanes,
                    false);
            }

            RoutePenaltyInspectionContext context = CreateInspectionContext();
            return RoutePenaltyInspection.InspectCurrentRoute(
                vehicle,
                currentLane.m_Lane,
                navigationLanes,
                hasNavigationLanes,
                ref context,
                captureBreakdown: false,
                captureTagSummary: captureDebugStrings,
                MaxPenaltyTags);
        }

        private RouteSelectionChangeSnapshot BuildRouteSelectionSnapshot(
            Entity vehicle,
            Entity currentLane,
            RoutePenaltyInspectionResult inspection,
            RouteSelectionChangeSnapshot? previousSnapshot,
            CandidateChangeReason candidateReasons)
        {
            bool hasPreviousSnapshot = previousSnapshot.HasValue;
            RouteSelectionChangeSnapshot previous = previousSnapshot.GetValueOrDefault();

            bool currentTargetChanged =
                !hasPreviousSnapshot ||
                (candidateReasons & CandidateChangeReason.Target) != 0;
            bool currentRouteChanged =
                !hasPreviousSnapshot ||
                (candidateReasons & CandidateChangeReason.CurrentRoute) != 0;
            bool pathOwnerChanged =
                !hasPreviousSnapshot ||
                (candidateReasons & CandidateChangeReason.PathOwner) != 0;
            bool pathInformationChanged =
                !hasPreviousSnapshot ||
                (candidateReasons & CandidateChangeReason.PathInformation) != 0;

            bool hasCurrentTarget = previous.HasCurrentTarget;
            Entity currentTarget = previous.CurrentTarget;
            if (currentTargetChanged)
            {
                hasCurrentTarget =
                    m_TargetData.TryGetComponent(vehicle, out Target targetData) &&
                    targetData.m_Target != Entity.Null;
                currentTarget =
                    hasCurrentTarget
                        ? targetData.m_Target
                        : Entity.Null;
            }

            bool hasCurrentRoute = previous.HasCurrentRoute;
            Entity currentRoute = previous.CurrentRoute;
            if (currentRouteChanged)
            {
                hasCurrentRoute =
                    m_CurrentRouteData.TryGetComponent(vehicle, out CurrentRoute currentRouteData) &&
                    currentRouteData.m_Route != Entity.Null;
                currentRoute =
                    hasCurrentRoute
                        ? currentRouteData.m_Route
                        : Entity.Null;
            }

            bool hasPathOwner = previous.HasPathOwner;
            PathFlags pathFlags = previous.PathFlags;
            if (pathOwnerChanged)
            {
                hasPathOwner =
                    m_PathOwnerData.TryGetComponent(vehicle, out PathOwner pathOwner);
                pathFlags =
                    hasPathOwner
                        ? pathOwner.m_State
                        : default;
            }

            bool hasPathInformation = previous.HasPathInformation;
            int pathInfoHash = previous.PathInfoHash;
            int acceptedPathHash = previous.AcceptedPathHash;
            int acceptedPathElementCount = previous.AcceptedPathElementCount;
            int acceptedResultHash = previous.AcceptedResultHash;
            if (pathInformationChanged)
            {
                hasPathInformation =
                    m_PathInformationData.TryGetComponent(vehicle, out PathInformation pathInformation);
                pathInfoHash =
                    hasPathInformation
                        ? ComputePathInformationHash(pathInformation)
                        : 0;
                acceptedPathHash = ComputeAcceptedPathHash(vehicle);
                acceptedPathElementCount =
                    TryGetAcceptedPathElementCount(vehicle, out int pathElementCount)
                        ? pathElementCount
                        : 0;
                acceptedResultHash =
                    ComputeAcceptedResultHash(
                        hasPathInformation,
                        pathInfoHash,
                        acceptedPathHash,
                        acceptedPathElementCount);
            }

            return new RouteSelectionChangeSnapshot(
                inspection,
                currentLane,
                hasCurrentRoute,
                currentRoute,
                hasCurrentTarget,
                currentTarget,
                hasPathOwner,
                pathFlags,
                hasPathInformation,
                pathInfoHash,
                acceptedPathHash,
                acceptedResultHash,
                acceptedPathElementCount);
        }

        private RoutePenaltyInspectionContext CreateInspectionContext()
        {
            return new RoutePenaltyInspectionContext
            {
                EntityManager = EntityManager,
                CarLaneData = m_CarLaneData,
                EdgeLaneData = m_EdgeLaneData,
                ParkingLaneData = m_ParkingLaneData,
                GarageLaneData = m_GarageLaneData,
                ConnectionLaneData = m_ConnectionLaneData,
                ProfileData = m_ProfileData,
                TypeLookups = m_TypeLookups,
            };
        }

        private SelectedObjectDisplayFormatterContext CreateDisplayFormatterContext()
        {
            return new SelectedObjectDisplayFormatterContext
            {
                EntityManager = EntityManager,
                NameSystem = m_NameSystem,
                PrefabSystem = m_PrefabSystem,
                OwnerData = m_OwnerData,
                AggregatedData = m_AggregatedData,
                SlaveLaneData = m_SlaveLaneData,
                CarLaneData = m_CarLaneData,
                ParkingLaneData = m_ParkingLaneData,
                GarageLaneData = m_GarageLaneData,
                ConnectionLaneData = m_ConnectionLaneData,
            };
        }

        private static bool ShouldLogRouteSelectionChange(
            RouteSelectionChangeSnapshot previousSnapshot,
            RouteSelectionChangeSnapshot currentSnapshot,
            bool rerouteDetected)
        {
            return rerouteDetected ||
                previousSnapshot.HasCurrentRoute != currentSnapshot.HasCurrentRoute ||
                previousSnapshot.CurrentRoute != currentSnapshot.CurrentRoute ||
                previousSnapshot.HasCurrentTarget != currentSnapshot.HasCurrentTarget ||
                previousSnapshot.CurrentTarget != currentSnapshot.CurrentTarget ||
                previousSnapshot.HasPathOwner != currentSnapshot.HasPathOwner ||
                previousSnapshot.PathFlags != currentSnapshot.PathFlags ||
                previousSnapshot.HasPathInformation != currentSnapshot.HasPathInformation ||
                previousSnapshot.PathInfoHash != currentSnapshot.PathInfoHash ||
                previousSnapshot.AcceptedPathHash != currentSnapshot.AcceptedPathHash ||
                previousSnapshot.AcceptedResultHash != currentSnapshot.AcceptedResultHash;
        }

        private static bool AreTagSnapshotsEqual(
            RoutePenaltyTagSnapshot previousTags,
            RoutePenaltyTagSnapshot currentTags)
        {
            if (previousTags.Count != currentTags.Count ||
                previousTags.OmittedCount != currentTags.OmittedCount)
            {
                return false;
            }

            for (int i = 0; i < previousTags.Count; i += 1)
            {
                if (previousTags.GetToken(i) != currentTags.GetToken(i))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreInspectionTagSnapshotsEqual(
            RoutePenaltyInspectionResult previousInspection,
            RoutePenaltyInspectionResult currentInspection)
        {
            return AreTagSnapshotsEqual(
                       previousInspection.TagSnapshot,
                       currentInspection.TagSnapshot) &&
                AreTagSnapshotsEqual(
                    previousInspection.NormalizedTagSnapshot,
                    currentInspection.NormalizedTagSnapshot);
        }

        private static bool IsLowValueNonWatchedRouteSelectionChange(
            RouteSelectionChangeSnapshot previousSnapshot,
            RouteSelectionChangeSnapshot currentSnapshot,
            bool rerouteDetected)
        {
            if (rerouteDetected)
            {
                return false;
            }

            bool routeEndpointsUnchanged =
                previousSnapshot.HasCurrentRoute == currentSnapshot.HasCurrentRoute &&
                previousSnapshot.CurrentRoute == currentSnapshot.CurrentRoute &&
                previousSnapshot.HasCurrentTarget == currentSnapshot.HasCurrentTarget &&
                previousSnapshot.CurrentTarget == currentSnapshot.CurrentTarget;

            bool acceptedResultUnchanged =
                previousSnapshot.HasPathInformation == currentSnapshot.HasPathInformation &&
                previousSnapshot.PathInfoHash == currentSnapshot.PathInfoHash &&
                previousSnapshot.AcceptedPathHash == currentSnapshot.AcceptedPathHash &&
                previousSnapshot.AcceptedResultHash == currentSnapshot.AcceptedResultHash;

            bool penaltyUnchanged =
                previousSnapshot.Inspection.TotalPenalty == currentSnapshot.Inspection.TotalPenalty;

            bool tagsUnchanged =
                AreInspectionTagSnapshotsEqual(
                    previousSnapshot.Inspection,
                    currentSnapshot.Inspection);

            bool existingLowValueCase =
                routeEndpointsUnchanged &&
                acceptedResultUnchanged &&
                penaltyUnchanged &&
                tagsUnchanged;

            bool routeIntentUnchanged =
                previousSnapshot.RouteHash == currentSnapshot.RouteHash &&
                routeEndpointsUnchanged;

            bool zeroPenaltyNoTags =
                previousSnapshot.Inspection.TotalPenalty == 0 &&
                currentSnapshot.Inspection.TotalPenalty == 0 &&
                previousSnapshot.Inspection.TagSnapshot.Count == 0 &&
                currentSnapshot.Inspection.TagSnapshot.Count == 0 &&
                previousSnapshot.Inspection.NormalizedTagSnapshot.Count == 0 &&
                currentSnapshot.Inspection.NormalizedTagSnapshot.Count == 0 &&
                previousSnapshot.Inspection.TagSnapshot.OmittedCount == 0 &&
                currentSnapshot.Inspection.TagSnapshot.OmittedCount == 0 &&
                previousSnapshot.Inspection.NormalizedTagSnapshot.OmittedCount == 0 &&
                currentSnapshot.Inspection.NormalizedTagSnapshot.OmittedCount == 0;

            bool acceptedResultRebuildChurn =
                previousSnapshot.HasPathOwner != currentSnapshot.HasPathOwner ||
                previousSnapshot.PathFlags != currentSnapshot.PathFlags ||
                previousSnapshot.HasPathInformation != currentSnapshot.HasPathInformation ||
                previousSnapshot.PathInfoHash != currentSnapshot.PathInfoHash ||
                previousSnapshot.AcceptedPathHash != currentSnapshot.AcceptedPathHash ||
                previousSnapshot.AcceptedResultHash != currentSnapshot.AcceptedResultHash;

            bool zeroPenaltyAcceptedResultChurn =
                routeIntentUnchanged &&
                zeroPenaltyNoTags &&
                acceptedResultRebuildChurn;

            bool targetChanged =
                previousSnapshot.HasCurrentTarget != currentSnapshot.HasCurrentTarget ||
                previousSnapshot.CurrentTarget != currentSnapshot.CurrentTarget;

            bool currentRouteUnchanged =
                previousSnapshot.HasCurrentRoute == currentSnapshot.HasCurrentRoute &&
                previousSnapshot.CurrentRoute == currentSnapshot.CurrentRoute;

            bool zeroPenaltyRetargetChurn =
                previousSnapshot.RouteHash == currentSnapshot.RouteHash &&
                currentRouteUnchanged &&
                targetChanged &&
                acceptedResultUnchanged &&
                zeroPenaltyNoTags;

            return existingLowValueCase || zeroPenaltyAcceptedResultChurn || zeroPenaltyRetargetChurn;
        }

        private void LogRouteSelectionChange(
            Entity vehicle,
            RouteSelectionChangeSnapshot previousSnapshot,
            RouteSelectionChangeSnapshot currentSnapshot,
            bool rerouteDetected,
            bool focusedWatch,
            bool emitFocusedDiagnostics)
        {
            string role =
                PublicTransportLanePolicy.DescribeVehicleRole(vehicle, ref m_TypeLookups);
            string reasons =
                BuildRouteSelectionChangeReasons(
                    previousSnapshot,
                    currentSnapshot,
                    rerouteDetected);
            string previousBreakdown =
                RoutePenaltyInspection.BuildBreakdown(
                    previousSnapshot.Inspection.Profile);
            string currentBreakdown =
                RoutePenaltyInspection.BuildBreakdown(
                    currentSnapshot.Inspection.Profile);
            string previousTags =
                RoutePenaltyInspection.BuildTagSummary(
                    previousSnapshot.Inspection.TagSnapshot);
            string currentTags =
                RoutePenaltyInspection.BuildTagSummary(
                    currentSnapshot.Inspection.TagSnapshot);
            string previousNormalizedTags =
                RoutePenaltyInspection.BuildTagSummary(
                    previousSnapshot.Inspection.NormalizedTagSnapshot);
            string currentNormalizedTags =
                RoutePenaltyInspection.BuildTagSummary(
                    currentSnapshot.Inspection.NormalizedTagSnapshot);

            string message =
                $"Route selection change: vehicle={FormatEntityOrNone(vehicle)}, role={role}, focusedWatch={focusedWatch}, reasons={reasons}, " +
                $"currentLane={FormatEntityOrNone(currentSnapshot.CurrentLane)}, " +
                $"routeHash={previousSnapshot.RouteHash}->{currentSnapshot.RouteHash}, " +
                $"currentRoute={FormatOptionalEntity(previousSnapshot.HasCurrentRoute, previousSnapshot.CurrentRoute)}->{FormatOptionalEntity(currentSnapshot.HasCurrentRoute, currentSnapshot.CurrentRoute)}, " +
                $"currentTarget={FormatOptionalEntity(previousSnapshot.HasCurrentTarget, previousSnapshot.CurrentTarget)}->{FormatOptionalEntity(currentSnapshot.HasCurrentTarget, currentSnapshot.CurrentTarget)}, " +
                $"pathState={FormatPathState(previousSnapshot)}->{FormatPathState(currentSnapshot)}, " +
                $"pathInfoHash={FormatHashChange(previousSnapshot.PathInfoHash, currentSnapshot.PathInfoHash)}, " +
                $"acceptedPathHash={FormatHashChange(previousSnapshot.AcceptedPathHash, currentSnapshot.AcceptedPathHash)}, " +
                $"acceptedResultHash={FormatHashChange(previousSnapshot.AcceptedResultHash, currentSnapshot.AcceptedResultHash)}, " +
                $"plannedPenalty={previousSnapshot.Inspection.TotalPenalty} [{previousBreakdown}] -> {currentSnapshot.Inspection.TotalPenalty} [{currentBreakdown}], " +
                $"tags={previousTags} -> {currentTags}, " +
                $"normalizedTags={previousNormalizedTags} -> {currentNormalizedTags}";

            EnforcementTelemetry.RecordEvent(message);
            Mod.log.Info(message);

            if (emitFocusedDiagnostics)
            {
                string rebuildContextMessage =
                    BuildFocusedRouteRebuildContext(
                        vehicle,
                        previousSnapshot,
                        currentSnapshot);
                EnforcementTelemetry.RecordEvent(rebuildContextMessage);
                Mod.log.Info(rebuildContextMessage);

                string transitionPreviewMessage =
                    BuildFocusedRouteTransitionPreview(
                        vehicle,
                        currentSnapshot);
                EnforcementTelemetry.RecordEvent(transitionPreviewMessage);
                Mod.log.Info(transitionPreviewMessage);

                string accessWindowMessage =
                    BuildFocusedAccessWindowPreview(
                        vehicle,
                        currentSnapshot);
                EnforcementTelemetry.RecordEvent(accessWindowMessage);
                Mod.log.Info(accessWindowMessage);

                string nameResolutionMessage =
                    BuildFocusedLaneNameResolutionPreview(
                        vehicle,
                        currentSnapshot);
                EnforcementTelemetry.RecordEvent(nameResolutionMessage);
                Mod.log.Info(nameResolutionMessage);

                string acceptedResultMessage =
                    BuildFocusedAcceptedRouteResult(
                        vehicle,
                        previousSnapshot,
                        currentSnapshot);
                EnforcementTelemetry.RecordEvent(acceptedResultMessage);
                Mod.log.Info(acceptedResultMessage);
            }
        }

        private static string BuildRouteSelectionChangeReasons(
            RouteSelectionChangeSnapshot previousSnapshot,
            RouteSelectionChangeSnapshot currentSnapshot,
            bool rerouteDetected)
        {
            StringBuilder reasons = new StringBuilder(48);
            bool hasReasons = false;
            if (rerouteDetected)
            {
                reasons.Append("reroute");
                hasReasons = true;
            }

            if (previousSnapshot.RouteHash != currentSnapshot.RouteHash)
            {
                if (hasReasons)
                {
                    reasons.Append(',');
                }

                reasons.Append("route-hash");
                hasReasons = true;
            }

            if (previousSnapshot.HasCurrentRoute != currentSnapshot.HasCurrentRoute ||
                previousSnapshot.CurrentRoute != currentSnapshot.CurrentRoute)
            {
                if (hasReasons)
                {
                    reasons.Append(',');
                }

                reasons.Append("current-route");
                hasReasons = true;
            }

            if (previousSnapshot.HasCurrentTarget != currentSnapshot.HasCurrentTarget ||
                previousSnapshot.CurrentTarget != currentSnapshot.CurrentTarget)
            {
                if (hasReasons)
                {
                    reasons.Append(',');
                }

                reasons.Append("target");
                hasReasons = true;
            }

            if (previousSnapshot.HasPathOwner != currentSnapshot.HasPathOwner ||
                previousSnapshot.PathFlags != currentSnapshot.PathFlags)
            {
                if (hasReasons)
                {
                    reasons.Append(',');
                }

                reasons.Append("path-state");
                hasReasons = true;
            }

            if (previousSnapshot.HasPathInformation != currentSnapshot.HasPathInformation ||
                previousSnapshot.PathInfoHash != currentSnapshot.PathInfoHash)
            {
                if (hasReasons)
                {
                    reasons.Append(',');
                }

                reasons.Append("path-info");
                hasReasons = true;
            }

            if (previousSnapshot.AcceptedPathHash != currentSnapshot.AcceptedPathHash)
            {
                if (hasReasons)
                {
                    reasons.Append(',');
                }

                reasons.Append("accepted-path");
                hasReasons = true;
            }

            if (previousSnapshot.AcceptedResultHash != currentSnapshot.AcceptedResultHash)
            {
                if (hasReasons)
                {
                    reasons.Append(',');
                }

                reasons.Append("accepted-result");
                hasReasons = true;
            }

            return !hasReasons
                ? "none"
                : reasons.ToString();
        }

        private static string FormatOptionalEntity(bool hasValue, Entity entity)
        {
            return hasValue && entity != Entity.Null
                ? FormatEntityOrNone(entity)
                : "none";
        }

        private static string FormatPathState(RouteSelectionChangeSnapshot snapshot)
        {
            return snapshot.HasPathOwner
                ? snapshot.PathFlags.ToString()
                : "none";
        }

        private static string FormatHashChange(int previousHash, int currentHash)
        {
            return $"{previousHash}->{currentHash}";
        }

        private string BuildFocusedRouteRebuildContext(
            Entity vehicle,
            RouteSelectionChangeSnapshot previousSnapshot,
            RouteSelectionChangeSnapshot currentSnapshot)
        {
            Entity previousTarget =
                previousSnapshot.HasCurrentTarget
                    ? previousSnapshot.CurrentTarget
                    : Entity.Null;
            Entity currentTarget =
                currentSnapshot.HasCurrentTarget
                    ? currentSnapshot.CurrentTarget
                    : Entity.Null;
            Entity currentLane = currentSnapshot.CurrentLane;
            Entity normalizedCurrentLane = NormalizeLaneForAppendOrigin(currentLane);

            bool targetChanged =
                previousTarget != Entity.Null &&
                previousTarget != currentTarget;
            bool previousTargetEndMatchesCurrent =
                TryPreviousTargetEndMatchesCurrentLane(
                    previousTarget,
                    normalizedCurrentLane,
                    out Entity previousTargetEndLane);

            string predictedOriginSource =
                targetChanged && previousTargetEndMatchesCurrent
                    ? "previousTarget"
                    : "currentLocation";
            string targetRouteLane =
                DescribeRouteLane(currentTarget);
            string previousTargetRouteLane =
                DescribeRouteLane(previousTarget);
            string upcomingPathPreview =
                BuildUpcomingPathElementPreview(vehicle);
            string navigationPreview =
                BuildNavigationPreview(vehicle, currentLane);
            string pathfindContext =
                BuildPredictedPathfindContext(vehicle);
            string liveState = BuildLiveRouteState(vehicle);
            string obsoleteAttemptId =
                ObsoleteAttemptCorrelationService.GetAttemptId(vehicle);
            string elapsedSinceObsolete =
                ObsoleteAttemptCorrelationService.GetElapsedSinceObsolete(vehicle);
            string targetKindNormalized =
                RouteDebugNormalization.NormalizeTargetKind(EntityManager, currentTarget);
            targetKindNormalized =
                ObsoleteAttemptCorrelationService.ResolveTargetKindNormalized(
                    vehicle,
                    targetKindNormalized);

            string message =
                $"FOCUSED_ROUTE_REBUILD: vehicle={FormatEntityOrNone(vehicle)}, " +
                $"obsoleteAttemptId={obsoleteAttemptId}, " +
                $"elapsedSinceObsolete={elapsedSinceObsolete}, " +
                $"targetKindNormalized={targetKindNormalized}, " +
                $"previousTarget={FormatOptionalEntity(previousSnapshot.HasCurrentTarget, previousTarget)}, " +
                $"currentTarget={FormatOptionalEntity(currentSnapshot.HasCurrentTarget, currentTarget)}, " +
                $"currentLane={FormatEntityOrNone(currentLane)}, normalizedCurrentLane={FormatEntityOrNone(normalizedCurrentLane)}, " +
                $"targetChanged={targetChanged}, " +
                $"predictedOriginSource={predictedOriginSource}, " +
                $"previousTargetEndMatchesCurrent={previousTargetEndMatchesCurrent}, " +
                $"previousTargetEndLane={FormatEntityOrNone(previousTargetEndLane)}, " +
                $"targetRouteLane={targetRouteLane}, " +
                $"previousTargetRouteLane={previousTargetRouteLane}, " +
                $"upcomingPath={upcomingPathPreview}, " +
                $"navigationPreview={navigationPreview}, " +
                $"{pathfindContext}";

            if (!string.IsNullOrWhiteSpace(liveState))
            {
                message += $", {liveState}";
            }

            return message;
        }

        private string BuildFocusedAcceptedRouteResult(
            Entity vehicle,
            RouteSelectionChangeSnapshot previousSnapshot,
            RouteSelectionChangeSnapshot currentSnapshot)
        {
            bool hasPathInformation =
                m_PathInformationData.TryGetComponent(vehicle, out PathInformation pathInformation);
            bool hasPathOwner =
                m_PathOwnerData.TryGetComponent(vehicle, out PathOwner pathOwner);
            bool hasPathElements =
                m_PathElementData.TryGetBuffer(vehicle, out DynamicBuffer<PathElement> pathElements);

            int pathElementCount = hasPathElements ? pathElements.Length : 0;
            int elementIndex = hasPathOwner ? pathOwner.m_ElementIndex : -1;
            string acceptedPathPreview = BuildUpcomingPathElementPreview(vehicle, maxPreviewElements: 8);
            string acceptedPathHeadPreview = BuildAcceptedPathHeadPreview(vehicle, maxPreviewElements: 8);
            string acceptedHeadFamilyKey =
                hasPathElements
                    ? RouteDebugNormalization.BuildAcceptedHeadFamilyKey(EntityManager, pathElements)
                    : "none";
            string liveState = BuildLiveRouteState(vehicle);
            bool pathInfoChanged =
                previousSnapshot.HasPathInformation != currentSnapshot.HasPathInformation ||
                previousSnapshot.PathInfoHash != currentSnapshot.PathInfoHash;
            bool acceptedPathChanged =
                previousSnapshot.AcceptedPathHash != currentSnapshot.AcceptedPathHash;
            bool acceptedResultChanged =
                previousSnapshot.AcceptedResultHash != currentSnapshot.AcceptedResultHash;
            Entity currentTarget =
                currentSnapshot.HasCurrentTarget
                    ? currentSnapshot.CurrentTarget
                    : (hasPathInformation
                        ? pathInformation.m_Destination
                        : ObsoleteAttemptCorrelationService.GetLastKnownTarget(vehicle));
            string obsoleteAttemptId =
                ObsoleteAttemptCorrelationService.GetAttemptId(vehicle);
            string elapsedSinceObsolete =
                ObsoleteAttemptCorrelationService.GetElapsedSinceObsolete(vehicle);
            string targetKindNormalized =
                RouteDebugNormalization.NormalizeTargetKind(EntityManager, currentTarget);
            targetKindNormalized =
                ObsoleteAttemptCorrelationService.ResolveTargetKindNormalized(
                    vehicle,
                    targetKindNormalized);

            string message =
                $"FOCUSED_ROUTE_ACCEPTED_RESULT: vehicle={FormatEntityOrNone(vehicle)}, " +
                $"obsoleteAttemptId={obsoleteAttemptId}, " +
                $"elapsedSinceObsolete={elapsedSinceObsolete}, " +
                $"targetKindNormalized={targetKindNormalized}, " +
                $"routeHash={previousSnapshot.RouteHash}->{currentSnapshot.RouteHash}, " +
                $"previousPathState={FormatPathState(previousSnapshot)}, " +
                $"currentPathState={FormatPathState(currentSnapshot)}, " +
                $"pathInfoHash={FormatHashChange(previousSnapshot.PathInfoHash, currentSnapshot.PathInfoHash)}, " +
                $"acceptedPathHash={FormatHashChange(previousSnapshot.AcceptedPathHash, currentSnapshot.AcceptedPathHash)}, " +
                $"acceptedResultHash={FormatHashChange(previousSnapshot.AcceptedResultHash, currentSnapshot.AcceptedResultHash)}, " +
                $"pathInfoChanged={pathInfoChanged}, " +
                $"acceptedPathChanged={acceptedPathChanged}, " +
                $"acceptedResultChanged={acceptedResultChanged}, " +
                $"hasPathInformation={hasPathInformation}, " +
                $"resultOrigin={(hasPathInformation ? FormatEntityOrNone(pathInformation.m_Origin) : "none")}, " +
                $"resultDestination={(hasPathInformation ? FormatEntityOrNone(pathInformation.m_Destination) : "none")}, " +
                $"resultDistance={(hasPathInformation ? pathInformation.m_Distance.ToString("0.###") : "n/a")}, " +
                $"resultDuration={(hasPathInformation ? pathInformation.m_Duration.ToString("0.###") : "n/a")}, " +
                $"resultTotalCost={(hasPathInformation ? pathInformation.m_TotalCost.ToString("0.###") : "n/a")}, " +
                $"resultMethods={(hasPathInformation ? FormatPathMethods(pathInformation.m_Methods) : "none")}, " +
                $"resultState={(hasPathInformation ? pathInformation.m_State.ToString() : "none")}, " +
                $"pathElementCount={pathElementCount}, " +
                $"elementIndex={elementIndex}, " +
                $"acceptedHeadFamilyKey={acceptedHeadFamilyKey}, " +
                $"acceptedPathHead={acceptedPathHeadPreview}, " +
                $"acceptedPath={acceptedPathPreview}";

            if (!string.IsNullOrWhiteSpace(liveState))
            {
                message += $", {liveState}";
            }

            return message;
        }

        private string BuildFocusedRouteTransitionPreview(
            Entity vehicle,
            RouteSelectionChangeSnapshot currentSnapshot,
            int maxTransitions = 4)
        {
            Entity currentLane = currentSnapshot.CurrentLane;
            Entity currentTarget =
                currentSnapshot.HasCurrentTarget
                    ? currentSnapshot.CurrentTarget
                    : Entity.Null;

            Entity targetStartLane = Entity.Null;
            Entity targetEndLane = Entity.Null;
            if (currentTarget != Entity.Null &&
                m_RouteLaneData.TryGetComponent(currentTarget, out RouteLane targetRouteLane))
            {
                targetStartLane = targetRouteLane.m_StartLane;
                targetEndLane = targetRouteLane.m_EndLane;
            }

            bool hasNavigationLanes =
                m_NavigationLaneData.TryGetBuffer(vehicle, out DynamicBuffer<CarNavigationLane> navigationLanes);

            bool hasResolvedPolicy =
                TryResolveAllowedOnPublicTransportLaneForLogging(
                    vehicle,
                    out bool allowedOnPublicTransportLane);

            RoutePenaltyInspectionContext context = CreateInspectionContext();
            bool previousUnauthorizedPublicTransportLane =
                hasResolvedPolicy &&
                IsUnauthorizedPublicTransportLane(currentLane, allowedOnPublicTransportLane);

            StringBuilder transitions = new StringBuilder(maxTransitions * 48);
            int transitionCount = 0;
            Entity sourceLane = currentLane;
            if (hasNavigationLanes)
            {
                for (int index = 0; index < navigationLanes.Length && transitionCount < maxTransitions; index += 1)
                {
                    Entity targetLane = navigationLanes[index].m_Lane;
                    if (targetLane == Entity.Null || targetLane == sourceLane)
                    {
                        continue;
                    }

                    if (transitionCount > 0)
                    {
                        transitions.Append("; ");
                    }

                    transitions.Append(
                        DescribeFocusedChosenTransition(
                            sourceLane,
                            targetLane,
                            targetStartLane,
                            targetEndLane,
                            hasResolvedPolicy,
                            allowedOnPublicTransportLane,
                            ref previousUnauthorizedPublicTransportLane,
                            ref context));
                    transitionCount += 1;
                    sourceLane = targetLane;
                }
            }

            string transitionSummary =
                transitionCount == 0
                    ? "none"
                    : transitions.ToString();

            return
                $"FOCUSED_ROUTE_TRANSITIONS: vehicle={FormatEntityOrNone(vehicle)}, " +
                $"targetStartLane={FormatEntityOrNone(targetStartLane)}, " +
                $"targetEndLane={FormatEntityOrNone(targetEndLane)}, " +
                $"chosenTransitions={transitionSummary}";
        }

        private string BuildFocusedAccessWindowPreview(
            Entity vehicle,
            RouteSelectionChangeSnapshot currentSnapshot)
        {
            bool hasNavigationLanes =
                m_NavigationLaneData.TryGetBuffer(vehicle, out DynamicBuffer<CarNavigationLane> navigationLanes);

            RoutePenaltyInspectionContext context = CreateInspectionContext();
            return RoutePenaltyInspection.BuildFocusedAccessWindowDiagnostic(
                vehicle,
                currentSnapshot.CurrentLane,
                navigationLanes,
                hasNavigationLanes,
                ref context);
        }

        private string BuildFocusedLaneNameResolutionPreview(
            Entity vehicle,
            RouteSelectionChangeSnapshot currentSnapshot,
            int maxNavigationLanes = 4)
        {
            Entity currentLane = currentSnapshot.CurrentLane;
            Entity currentTarget =
                currentSnapshot.HasCurrentTarget
                    ? currentSnapshot.CurrentTarget
                    : Entity.Null;

            Entity targetStartLane = Entity.Null;
            Entity targetEndLane = Entity.Null;
            if (currentTarget != Entity.Null &&
                m_RouteLaneData.TryGetComponent(currentTarget, out RouteLane targetRouteLane))
            {
                targetStartLane = targetRouteLane.m_StartLane;
                targetEndLane = targetRouteLane.m_EndLane;
            }

            SelectedObjectDisplayFormatterContext formatterContext =
                CreateDisplayFormatterContext();
            string currentLaneResolution =
                DescribeFocusedLaneNameResolution(currentLane, ref formatterContext);
            string targetStartResolution =
                DescribeFocusedLaneNameResolution(targetStartLane, ref formatterContext);
            string targetEndResolution =
                DescribeFocusedLaneNameResolution(targetEndLane, ref formatterContext);

            StringBuilder navigationLaneResolutions = new StringBuilder(maxNavigationLanes * 40);
            int navigationLaneResolutionCount = 0;
            if (m_NavigationLaneData.TryGetBuffer(vehicle, out DynamicBuffer<CarNavigationLane> navigationLanes))
            {
                for (int index = 0; index < navigationLanes.Length && navigationLaneResolutionCount < maxNavigationLanes; index += 1)
                {
                    Entity lane = navigationLanes[index].m_Lane;
                    if (lane == Entity.Null ||
                        lane == currentLane ||
                        lane == targetStartLane ||
                        lane == targetEndLane)
                    {
                        continue;
                    }

                    if (navigationLaneResolutionCount > 0)
                    {
                        navigationLaneResolutions.Append("; ");
                    }

                    navigationLaneResolutions.Append('n')
                        .Append(navigationLaneResolutionCount)
                        .Append('=')
                        .Append(DescribeFocusedLaneNameResolution(lane, ref formatterContext));
                    navigationLaneResolutionCount += 1;
                }
            }

            string navigationSummary =
                navigationLaneResolutionCount == 0
                    ? "none"
                    : navigationLaneResolutions.ToString();

            return
                $"FOCUSED_ROUTE_NAME_RESOLUTION: vehicle={FormatEntityOrNone(vehicle)}, " +
                $"current={currentLaneResolution}, " +
                $"targetStart={targetStartResolution}, " +
                $"targetEnd={targetEndResolution}, " +
                $"navigation={navigationSummary}";
        }

        private string DescribeFocusedChosenTransition(
            Entity sourceLane,
            Entity targetLane,
            Entity targetStartLane,
            Entity targetEndLane,
            bool hasResolvedPolicy,
            bool allowedOnPublicTransportLane,
            ref bool previousUnauthorizedPublicTransportLane,
            ref RoutePenaltyInspectionContext context)
        {
            string sourceKind =
                RoutePenaltyInspection.DescribeLaneKind(sourceLane, ref context);
            string targetKind =
                RoutePenaltyInspection.DescribeLaneKind(targetLane, ref context);

            bool unauthorizedTargetLane =
                hasResolvedPolicy &&
                IsUnauthorizedPublicTransportLane(targetLane, allowedOnPublicTransportLane);

            StringBuilder penaltyParts = new StringBuilder(64);
            bool hasPenaltyParts = false;
            if (unauthorizedTargetLane && !previousUnauthorizedPublicTransportLane)
            {
                penaltyParts.Append("pt=+")
                    .Append(EnforcementPenaltyService.GetPublicTransportLaneFine());
                hasPenaltyParts = true;
            }

            if (TryGetMidBlockPenaltyTag(
                    sourceLane,
                    Entity.Null,
                    targetLane,
                    Entity.Null,
                    out string midBlockTag))
            {
                if (hasPenaltyParts)
                {
                    penaltyParts.Append('|');
                }

                penaltyParts.Append("mid=+")
                    .Append(EnforcementPenaltyService.GetMidBlockCrossingFine())
                    .Append('(')
                    .Append(midBlockTag)
                    .Append(')');
                hasPenaltyParts = true;
            }

            if (TryGetIntersectionPenaltyTag(
                    sourceLane,
                    targetLane,
                    out string intersectionTag))
            {
                if (hasPenaltyParts)
                {
                    penaltyParts.Append('|');
                }

                penaltyParts.Append("int=+")
                    .Append(EnforcementPenaltyService.GetIntersectionMovementFine())
                    .Append('(')
                    .Append(intersectionTag)
                    .Append(')');
                hasPenaltyParts = true;
            }

            if (TryGetOutboundAccessPenaltyTag(
                    sourceLane,
                    targetLane,
                    out string outboundAccessTag))
            {
                if (hasPenaltyParts)
                {
                    penaltyParts.Append('|');
                }

                penaltyParts.Append("rule=")
                    .Append(outboundAccessTag);
                hasPenaltyParts = true;
            }

            previousUnauthorizedPublicTransportLane = unauthorizedTargetLane;

            string penaltySummary =
                !hasPenaltyParts
                    ? "none"
                    : penaltyParts.ToString();

            return
                $"{FormatEntityOrNone(sourceLane)}->{FormatEntityOrNone(targetLane)}" +
                $"[{sourceKind}->{targetKind}, " +
                $"targetStart={targetLane == targetStartLane}, " +
                $"targetEnd={targetLane == targetEndLane}, " +
                $"penalties={penaltySummary}]";
        }

        private string DescribeFocusedLaneNameResolution(
            Entity lane,
            ref SelectedObjectDisplayFormatterContext formatterContext)
        {
            if (lane == Entity.Null)
            {
                return "none";
            }

            LaneResolutionDiagnostic laneResolution =
                BuildLaneResolutionDiagnostic(lane, ref formatterContext);

            return
                $"{FormatEntityOrNone(lane)}" +
                $"{{display=\"{laneResolution.DisplayText}\", owner={laneResolution.OwnerText}, aggregate={laneResolution.AggregateText}, " +
                $"resolvedRoad={laneResolution.ResolvedRoadText}, nameSource={laneResolution.NameSource}}}";
        }

        private LaneResolutionDiagnostic BuildLaneResolutionDiagnostic(
            Entity lane,
            ref SelectedObjectDisplayFormatterContext formatterContext)
        {
            Entity ownerEntity = Entity.Null;
            Entity aggregateEntity = Entity.Null;
            if (m_OwnerData.TryGetComponent(lane, out Owner owner) &&
                owner.m_Owner != Entity.Null)
            {
                ownerEntity = owner.m_Owner;
                if (m_AggregatedData.TryGetComponent(ownerEntity, out Aggregated aggregated) &&
                    aggregated.m_Aggregate != Entity.Null)
                {
                    aggregateEntity = aggregated.m_Aggregate;
                }
            }

            Entity resolvedRoadEntity =
                SelectedObjectDisplayFormatter.ResolveRoadEntityFromLane(
                    lane,
                    ref formatterContext);
            string displayText =
                SelectedObjectDisplayFormatter.BuildLaneDisplayText(
                    lane,
                    ref formatterContext);
            string ownerText =
                FormatNamedEntityOrNone(ownerEntity, ref formatterContext);
            string aggregateText =
                FormatNamedEntityOrNone(aggregateEntity, ref formatterContext);
            string resolvedRoadText =
                FormatNamedEntityOrNone(resolvedRoadEntity, ref formatterContext);

            string nameSource;
            if (resolvedRoadEntity == Entity.Null)
            {
                nameSource = "entity-fallback";
            }
            else if (aggregateEntity != Entity.Null && resolvedRoadEntity == aggregateEntity)
            {
                nameSource = "aggregate-road";
            }
            else if (ownerEntity != Entity.Null && resolvedRoadEntity == ownerEntity)
            {
                nameSource = "owner-road";
            }
            else
            {
                nameSource = "resolved-road";
            }

            return new LaneResolutionDiagnostic(
                ownerEntity,
                aggregateEntity,
                resolvedRoadEntity,
                displayText,
                ownerText,
                aggregateText,
                resolvedRoadText,
                nameSource);
        }

        private string BuildNavigationPreview(Entity vehicle, Entity currentLane)
        {
            bool hasNavigationLanes =
                m_NavigationLaneData.TryGetBuffer(vehicle, out DynamicBuffer<CarNavigationLane> navigationLanes);
            RoutePenaltyInspectionContext context = CreateInspectionContext();
            return RoutePenaltyInspection.BuildNavigationPreview(
                currentLane,
                navigationLanes,
                hasNavigationLanes,
                ref context);
        }

        private string BuildPredictedPathfindContext(Entity vehicle)
        {
            bool hasProfile =
                m_ProfileData.TryGetComponent(vehicle, out VehicleTrafficLawProfile vehicleProfile);
            bool emergency =
                EmergencyVehiclePolicy.IsEmergencyVehicle(vehicle, ref m_TypeLookups);

            bool pendingExitActive =
                m_PendingExitData.TryGetComponent(vehicle, out PublicTransportLanePendingExit pendingExit) &&
                pendingExit.m_HasLeftPublicTransportLane == 0;

            bool allowOnPublicTransportLane =
                hasProfile
                    ? PublicTransportLanePolicy.CanUsePublicTransportLane(vehicleProfile)
                    : emergency;
            if (!allowOnPublicTransportLane && pendingExitActive)
            {
                allowOnPublicTransportLane = true;
            }

            bool hasPublicTransport =
                m_PublicTransportData.TryGetComponent(vehicle, out PublicTransport publicTransport);
            bool hasCargoTransport =
                m_CargoTransportData.TryGetComponent(vehicle, out CargoTransport cargoTransport);

            bool evacuating =
                hasPublicTransport &&
                (publicTransport.m_State & (PublicTransportFlags.Returning | PublicTransportFlags.Evacuating)) ==
                PublicTransportFlags.Evacuating;

            PathfindFlags predictedPathfindFlags = default;
            bool routeSourceEnRoute =
                (hasCargoTransport &&
                 (cargoTransport.m_State & (CargoTransportFlags.EnRoute | CargoTransportFlags.RouteSource)) ==
                 (CargoTransportFlags.EnRoute | CargoTransportFlags.RouteSource)) ||
                (hasPublicTransport &&
                 (publicTransport.m_State & (PublicTransportFlags.EnRoute | PublicTransportFlags.RouteSource)) ==
                 (PublicTransportFlags.EnRoute | PublicTransportFlags.RouteSource));
            if (routeSourceEnRoute)
            {
                predictedPathfindFlags = PathfindFlags.Stable | PathfindFlags.IgnoreFlow;
            }

            RuleFlags predictedIgnoredRules = default;
            bool hasPredictedIgnoredRules = false;
            if (m_PrefabRefData.TryGetComponent(vehicle, out Game.Prefabs.PrefabRef prefabRef) &&
                prefabRef.m_Prefab != Entity.Null &&
                m_PrefabCarData.TryGetComponent(prefabRef.m_Prefab, out Game.Prefabs.CarData carData))
            {
                predictedIgnoredRules =
                    RuleFlags.ForbidPrivateTraffic |
                    VehicleUtils.GetIgnoredPathfindRules(carData);

                if (evacuating)
                {
                    predictedIgnoredRules |=
                        RuleFlags.ForbidCombustionEngines |
                        RuleFlags.ForbidTransitTraffic |
                        RuleFlags.ForbidHeavyTraffic;
                }

                SetRuleFlag(
                    ref predictedIgnoredRules,
                    RuleFlags.ForbidPrivateTraffic,
                    allowOnPublicTransportLane);
                hasPredictedIgnoredRules = true;
            }

            string accessBits =
                hasProfile
                    ? vehicleProfile.m_PublicTransportLaneAccessBits.ToString()
                    : "n/a";
            string predictedWeights =
                evacuating
                    ? "time=1,behaviour=0.2,money=0,comfort=0.1"
                    : "time=1,behaviour=1,money=1,comfort=1";

            return
                $"predictedPathfindFlags={FormatPathfindFlags(predictedPathfindFlags)}, " +
                $"predictedIgnoredRules={(hasPredictedIgnoredRules ? FormatRuleFlags(predictedIgnoredRules) : "unavailable")}, " +
                $"predictedWeights={predictedWeights}, " +
                $"publicTransportState={(hasPublicTransport ? publicTransport.m_State.ToString() : "none")}, " +
                $"cargoTransportState={(hasCargoTransport ? cargoTransport.m_State.ToString() : "none")}, " +
                $"allowOnPTLane={allowOnPublicTransportLane}, pendingExitActive={pendingExitActive}, " +
                $"hasProfile={hasProfile}, accessBits={accessBits}, emergency={emergency}, " +
                $"configuredFines=pt:{EnforcementPenaltyService.GetPublicTransportLaneFine()},midBlock:{EnforcementPenaltyService.GetMidBlockCrossingFine()},intersection:{EnforcementPenaltyService.GetIntersectionMovementFine()}";
        }

        private string BuildUpcomingPathElementPreview(Entity vehicle, int maxPreviewElements = 5)
        {
            if (!m_PathOwnerData.TryGetComponent(vehicle, out PathOwner pathOwner) ||
                !m_PathElementData.TryGetBuffer(vehicle, out DynamicBuffer<PathElement> pathElements) ||
                pathElements.Length == 0)
            {
                return "none";
            }

            int startIndex = pathOwner.m_ElementIndex;
            if (startIndex < 0)
            {
                startIndex = 0;
            }
            else if (startIndex >= pathElements.Length)
            {
                startIndex = pathElements.Length - 1;
            }

            return BuildPathElementPreview(pathElements, startIndex, maxPreviewElements);
        }

        private string BuildAcceptedPathHeadPreview(Entity vehicle, int maxPreviewElements = 5)
        {
            if (!m_PathElementData.TryGetBuffer(vehicle, out DynamicBuffer<PathElement> pathElements) ||
                pathElements.Length == 0)
            {
                return "none";
            }

            return BuildPathElementPreview(pathElements, startIndex: 0, maxPreviewElements);
        }

        private string BuildPathElementPreview(
            DynamicBuffer<PathElement> pathElements,
            int startIndex,
            int maxPreviewElements)
        {
            SelectedObjectDisplayFormatterContext formatterContext =
                CreateDisplayFormatterContext();

            StringBuilder parts = new StringBuilder(maxPreviewElements * 48);
            int emitted = 0;
            for (int index = startIndex; index < pathElements.Length && emitted < maxPreviewElements; index++, emitted++)
            {
                PathElement pathElement = pathElements[index];
                string laneText =
                    SelectedObjectDisplayFormatter.BuildLaneDisplayText(
                        pathElement.m_Target,
                        ref formatterContext);
                if (emitted > 0)
                {
                    parts.Append("; ");
                }

                parts.Append(index)
                    .Append(':')
                    .Append(pathElement.m_Target)
                    .Append(" \"")
                    .Append(laneText)
                    .Append("\"[")
                    .Append(pathElement.m_TargetDelta.x.ToString("0.###"))
                    .Append("->")
                    .Append(pathElement.m_TargetDelta.y.ToString("0.###"))
                    .Append('|')
                    .Append(pathElement.m_Flags)
                    .Append(']');
            }

            int remaining = pathElements.Length - startIndex - emitted;
            if (remaining > 0)
            {
                if (emitted > 0)
                {
                    parts.Append("; ");
                }

                parts.Append('+')
                    .Append(remaining)
                    .Append(" more");
            }

            return emitted == 0 && remaining <= 0
                ? "none"
                : parts.ToString();
        }

        private bool TryGetAcceptedPathElementCount(Entity vehicle, out int count)
        {
            if (m_PathElementData.TryGetBuffer(vehicle, out DynamicBuffer<PathElement> pathElements))
            {
                count = pathElements.Length;
                return true;
            }

            count = 0;
            return false;
        }

        private int ComputeAcceptedPathHash(Entity vehicle)
        {
            if (!m_PathElementData.TryGetBuffer(vehicle, out DynamicBuffer<PathElement> pathElements) ||
                pathElements.Length == 0)
            {
                return 0;
            }

            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + pathElements.Length;
                for (int index = 0; index < pathElements.Length; index += 1)
                {
                    PathElement pathElement = pathElements[index];
                    hash = CombineHash(hash, pathElement.m_Target);
                    hash = (hash * 31) + pathElement.m_TargetDelta.x.GetHashCode();
                    hash = (hash * 31) + pathElement.m_TargetDelta.y.GetHashCode();
                    hash = (hash * 31) + (int)pathElement.m_Flags;
                }

                return hash;
            }
        }

        private static int ComputePathInformationHash(PathInformation pathInformation)
        {
            unchecked
            {
                int hash = 17;
                hash = CombineHash(hash, pathInformation.m_Origin);
                hash = CombineHash(hash, pathInformation.m_Destination);
                hash = (hash * 31) + pathInformation.m_Distance.GetHashCode();
                hash = (hash * 31) + pathInformation.m_Duration.GetHashCode();
                hash = (hash * 31) + pathInformation.m_TotalCost.GetHashCode();
                hash = (hash * 31) + (int)pathInformation.m_Methods;
                hash = (hash * 31) + (int)pathInformation.m_State;
                return hash;
            }
        }

        private static int ComputeAcceptedResultHash(
            bool hasPathInformation,
            int pathInfoHash,
            int acceptedPathHash,
            int acceptedPathElementCount)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + (hasPathInformation ? 1 : 0);
                hash = (hash * 31) + pathInfoHash;
                hash = (hash * 31) + acceptedPathHash;
                hash = (hash * 31) + acceptedPathElementCount;
                return hash;
            }
        }

        private static int CombineHash(int hash, Entity entity)
        {
            unchecked
            {
                hash = (hash * 31) + entity.Index;
                hash = (hash * 31) + entity.Version;
                return hash;
            }
        }

        private static string FormatNamedEntityOrNone(
            Entity entity,
            ref SelectedObjectDisplayFormatterContext formatterContext)
        {
            return entity == Entity.Null
                ? "none"
                : SelectedObjectDisplayFormatter.FormatNamedEntity(entity, ref formatterContext);
        }

        private string BuildLiveRouteState(Entity vehicle)
        {
            string currentLaneState = string.Empty;
            if (m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLane))
            {
                Entity normalizedCurrentLane = NormalizeLaneForAppendOrigin(currentLane.m_Lane);
                Entity normalizedChangeLane = NormalizeLaneForAppendOrigin(currentLane.m_ChangeLane);
                currentLaneState =
                    $"liveCurrentLane={FormatEntityOrNone(currentLane.m_Lane)}, " +
                    $"liveNormalizedCurrentLane={FormatEntityOrNone(normalizedCurrentLane)}, " +
                    $"liveChangeLane={FormatEntityOrNone(currentLane.m_ChangeLane)}, " +
                    $"liveNormalizedChangeLane={FormatEntityOrNone(normalizedChangeLane)}, " +
                    $"liveCurve={FormatFloat3(currentLane.m_CurvePosition)}, " +
                    $"liveChangeProgress={currentLane.m_ChangeProgress:0.###}, " +
                    $"liveLanePosition={currentLane.m_LanePosition:0.###}, " +
                    $"liveLaneDistance={currentLane.m_Distance:0.###}, " +
                    $"liveLaneDuration={currentLane.m_Duration:0.###}, " +
                    $"liveLaneFlags={(currentLane.m_LaneFlags == 0 ? "none" : currentLane.m_LaneFlags.ToString())}";
            }

            string pathOwnerState = string.Empty;
            if (m_PathOwnerData.TryGetComponent(vehicle, out PathOwner pathOwner))
            {
                int pathElementCount =
                    m_PathElementData.TryGetBuffer(vehicle, out DynamicBuffer<PathElement> pathElements)
                        ? pathElements.Length
                        : 0;
                int remainingElements =
                    pathElementCount > 0
                        ? math.max(0, pathElementCount - pathOwner.m_ElementIndex)
                        : 0;
                pathOwnerState =
                    $"livePathElementIndex={pathOwner.m_ElementIndex}, " +
                    $"livePathElementCount={pathElementCount}, " +
                    $"liveRemainingElements={remainingElements}";
            }

            string transformState = string.Empty;
            if (m_TransformData.TryGetComponent(vehicle, out Game.Objects.Transform transform))
            {
                transformState = $"liveWorldPos={FormatFloat3(transform.m_Position)}";
            }

            return JoinNonEmpty(currentLaneState, pathOwnerState, transformState);
        }

        private static string JoinNonEmpty(params string[] parts)
        {
            string result = string.Empty;
            for (int index = 0; index < parts.Length; index += 1)
            {
                if (string.IsNullOrWhiteSpace(parts[index]))
                {
                    continue;
                }

                result = string.IsNullOrEmpty(result)
                    ? parts[index]
                    : result + ", " + parts[index];
            }

            return result;
        }

        private static string FormatFloat3(float3 value)
        {
            return $"({value.x:0.###},{value.y:0.###},{value.z:0.###})";
        }

        private Entity NormalizeLaneForAppendOrigin(Entity lane)
        {
            if (lane == Entity.Null)
            {
                return Entity.Null;
            }

            Entity normalizedLane = lane;
            if (m_SlaveLaneData.TryGetComponent(lane, out SlaveLane slaveLane) &&
                m_OwnerData.TryGetComponent(lane, out Owner owner) &&
                m_SubLaneData.TryGetBuffer(owner.m_Owner, out DynamicBuffer<SubLane> subLanes) &&
                slaveLane.m_MasterIndex >= 0 &&
                slaveLane.m_MasterIndex < subLanes.Length)
            {
                Entity masterLane = subLanes[slaveLane.m_MasterIndex].m_SubLane;
                if (masterLane != Entity.Null)
                {
                    normalizedLane = masterLane;
                }
            }

            return normalizedLane;
        }

        private bool TryPreviousTargetEndMatchesCurrentLane(
            Entity previousTarget,
            Entity normalizedCurrentLane,
            out Entity previousTargetEndLane)
        {
            previousTargetEndLane = Entity.Null;

            if (previousTarget == Entity.Null ||
                normalizedCurrentLane == Entity.Null ||
                !m_RouteLaneData.TryGetComponent(previousTarget, out RouteLane previousTargetRouteLane))
            {
                return false;
            }

            previousTargetEndLane = previousTargetRouteLane.m_EndLane;
            return previousTargetRouteLane.m_EndLane == normalizedCurrentLane;
        }

        private string DescribeRouteLane(Entity waypoint)
        {
            if (waypoint == Entity.Null ||
                !m_RouteLaneData.TryGetComponent(waypoint, out RouteLane routeLane))
            {
                return "none";
            }

            return
                $"start={FormatEntityOrNone(routeLane.m_StartLane)}@{routeLane.m_StartCurvePos:0.###}, " +
                $"end={FormatEntityOrNone(routeLane.m_EndLane)}@{routeLane.m_EndCurvePos:0.###}";
        }

        private static string FormatEntityOrNone(Entity entity)
        {
            return entity == Entity.Null
                ? "none"
                : entity.ToString();
        }

        private static string FormatRuleFlags(RuleFlags flags)
        {
            return flags == 0 ? "none" : flags.ToString();
        }

        private static string FormatPathfindFlags(PathfindFlags flags)
        {
            return flags == 0 ? "none" : flags.ToString();
        }

        private static string FormatPathMethods(PathMethod methods)
        {
            return methods == 0 ? "none" : methods.ToString();
        }

        private static void SetRuleFlag(ref RuleFlags rules, RuleFlags flag, bool enabled)
        {
            if (enabled)
            {
                rules |= flag;
            }
            else
            {
                rules &= ~flag;
            }
        }

        private void LogPublicTransportLaneDecisionDiagnostics(
            Entity vehicle,
            Entity currentLane,
            DynamicBuffer<CarNavigationLane> navigationLanes,
            bool hasNavigationLanes,
            bool forceLogging)
        {
            RoutePenaltyInspectionContext context = CreateInspectionContext();
            bool hasResolvedPublicTransportLanePolicy =
                RoutePenaltyInspection.TryResolveAllowedOnPublicTransportLane(
                    vehicle,
                    ref context,
                    out bool allowedOnPublicTransportLane);

            MaybeLogPublicTransportLaneDecisionDiagnostic(
                vehicle,
                currentLane,
                hasResolvedPublicTransportLanePolicy,
                allowedOnPublicTransportLane,
                forceLogging);

            if (!hasNavigationLanes)
            {
                return;
            }

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

                MaybeLogPublicTransportLaneDecisionDiagnostic(
                    vehicle,
                    lane,
                    hasResolvedPublicTransportLanePolicy,
                    allowedOnPublicTransportLane,
                    forceLogging);
            }
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
                forceLogging: false);

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
            bool forceLogging)
        {
            if (!EnforcementLoggingPolicy.ShouldLogPathfindingPenaltyDiagnostics() &&
                !forceLogging)
            {
                return;
            }

            if (!forceLogging &&
                m_PublicTransportLaneDecisionDiagnosticLogsThisUpdate >=
                GetPublicTransportLaneDecisionDiagnosticLogLimit())
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

            RoutePenaltyInspectionContext context = CreateInspectionContext();
            bool unauthorizedPublicTransportLane =
                hasResolvedPublicTransportLanePolicy &&
                RoutePenaltyInspection.IsUnauthorizedPublicTransportLane(
                    lane,
                    allowedOnPublicTransportLane,
                    ref context);

            bool hasProfile =
                m_ProfileData.TryGetComponent(vehicle, out VehicleTrafficLawProfile vehicleProfile);

            bool emergency =
                EmergencyVehiclePolicy.IsEmergencyVehicle(vehicle, ref m_TypeLookups);

            bool engineHasFlag =
                PublicTransportLanePolicy.EngineHasPublicTransportLaneFlag(
                    vehicle,
                    ref m_TypeLookups);

            bool watchedVehicle = FocusedLoggingService.IsWatched(vehicle);

            bool permissionChangedByMod = hasProfile &&
                PublicTransportLanePolicy.PermissionChangedByMod(
                    vehicleProfile.m_PublicTransportLaneAccessBits);

            bool interestingNonWatchedCase =
                permissionChangedByMod ||
                (!hasResolvedPublicTransportLanePolicy && hasProfile);

            if (!forceLogging && !watchedVehicle && !interestingNonWatchedCase)
            {
                return;
            }

            bool vanillaAllowsValue = hasProfile &&
                PublicTransportLanePolicy.VanillaAllowsAccess(
                    vehicleProfile.m_PublicTransportLaneAccessBits);
            bool modAllowsValue = hasProfile &&
                PublicTransportLanePolicy.ModAllowsAccess(
                    vehicleProfile.m_PublicTransportLaneAccessBits);
            DiagnosticBoolState vanillaAllowsState =
                GetDiagnosticBoolState(hasProfile, vanillaAllowsValue);
            DiagnosticBoolState modAllowsState =
                GetDiagnosticBoolState(hasProfile, modAllowsValue);

            string vanillaAllows = FormatDiagnosticBoolState(vanillaAllowsState);

            string modAllows = FormatDiagnosticBoolState(modAllowsState);

            string canUsePublicTransportLane = hasProfile
                ? PublicTransportLanePolicy.CanUsePublicTransportLane(
                    vehicleProfile.m_PublicTransportLaneAccessBits,
                    emergency).ToString()
                : "n/a";

            string type = hasProfile
                ? PublicTransportLanePolicy.DescribeType(
                    vehicleProfile.m_PublicTransportLaneAccessBits)
                : "n/a";

            string permissionChangedByModText = hasProfile
                ? permissionChangedByMod.ToString()
                : "n/a";

            string emergencyOverrideActive = hasProfile
                ? PublicTransportLanePolicy.HasEmergencyPublicTransportLaneOverride(
                    vehicleProfile.m_PublicTransportLaneAccessBits,
                    emergency).ToString()
                : "n/a";

            string accessBits = hasProfile
                ? vehicleProfile.m_PublicTransportLaneAccessBits.ToString()
                : "n/a";

            string role =
                PublicTransportLanePolicy.DescribeVehicleRole(vehicle, ref m_TypeLookups);

            string laneKind =
                RoutePenaltyInspection.DescribeLaneKind(lane, ref context);

            string watchedDetail = string.Empty;
            if (watchedVehicle)
            {
                Entity currentVehicleLane =
                    m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLaneData)
                        ? currentLaneData.m_Lane
                        : Entity.Null;
                SelectedObjectDisplayFormatterContext formatterContext =
                    CreateDisplayFormatterContext();
                LaneResolutionDiagnostic laneResolution =
                    BuildLaneResolutionDiagnostic(lane, ref formatterContext);
                PublicTransportLaneDiagnosticComponentBits laneComponentBits =
                    GetPublicTransportLaneDiagnosticComponentBits(lane);
                WatchedPublicTransportLaneDiagnosticState watchedState =
                    new WatchedPublicTransportLaneDiagnosticState(
                        currentVehicleLane,
                        laneResolution.Owner,
                        laneResolution.Aggregate,
                        laneResolution.ResolvedRoad,
                        laneComponentBits,
                        publicOnly,
                        unauthorizedPublicTransportLane,
                        hasResolvedPublicTransportLanePolicy,
                        vanillaAllowsState,
                        modAllowsState);

                if (!forceLogging &&
                    !ShouldEmitWatchedPublicTransportLaneDiagnostic(
                        vehicle,
                        lane,
                        watchedState))
                {
                    return;
                }

                watchedDetail =
                    $", currentLane={FormatEntityOrNone(currentVehicleLane)}, " +
                    $"laneDisplay=\"{laneResolution.DisplayText}\", " +
                    $"owner={laneResolution.OwnerText}, aggregate={laneResolution.AggregateText}, " +
                    $"resolvedRoad={laneResolution.ResolvedRoadText}, resolvedRoadSource={laneResolution.NameSource}, " +
                    $"hasEdgeLane={HasLaneDiagnosticBit(laneComponentBits, PublicTransportLaneDiagnosticComponentBits.EdgeLane)}, " +
                    $"hasConnectionLane={HasLaneDiagnosticBit(laneComponentBits, PublicTransportLaneDiagnosticComponentBits.ConnectionLane)}, " +
                    $"hasSlaveLane={HasLaneDiagnosticBit(laneComponentBits, PublicTransportLaneDiagnosticComponentBits.SlaveLane)}, " +
                    $"hasRouteLane={HasLaneDiagnosticBit(laneComponentBits, PublicTransportLaneDiagnosticComponentBits.RouteLane)}";
            }

            string message =
                $"PT_ROUTE_DIAG: vehicle={FormatEntityOrNone(vehicle)}, role={role}, lane={FormatEntityOrNone(lane)}, laneKind={laneKind}, " +
                $"forceLogging={forceLogging}, " +
                $"publicOnly={publicOnly}, hasResolvedPolicy={hasResolvedPublicTransportLanePolicy}, " +
                $"hasProfile={hasProfile}, allowedOnPublicTransportLane={allowedOnPublicTransportLane}, " +
                $"unauthorizedPublicTransportLane={unauthorizedPublicTransportLane}, engineHasFlag={engineHasFlag}, " +
                $"emergency={emergency}, emergencyOverrideActive={emergencyOverrideActive}, " +
                $"type={type}, vanillaAllows={vanillaAllows}, modAllows={modAllows}, " +
                $"canUsePublicTransportLane={canUsePublicTransportLane}, " +
                $"permissionChangedByMod={permissionChangedByModText}, accessBits={accessBits}" +
                watchedDetail;

            EnforcementTelemetry.RecordEvent(message);
            Mod.log.Info(message);

            if (!forceLogging)
            {
                m_PublicTransportLaneDecisionDiagnosticLogsThisUpdate += 1;
            }
        }

        private static int GetPublicTransportLaneDecisionDiagnosticLogLimit()
        {
            return EnforcementLoggingPolicy.IsBurstLoggingActive
                ? BurstLoggingService.BurstPublicTransportLaneDecisionDiagnosticLogsPerUpdate
                : MaxPublicTransportLaneDecisionDiagnosticLogsPerUpdate;
        }

        private bool ShouldEmitWatchedPublicTransportLaneDiagnostic(
            Entity vehicle,
            Entity lane,
            WatchedPublicTransportLaneDiagnosticState currentState)
        {
            WatchedPublicTransportLaneDiagnosticKey key =
                new WatchedPublicTransportLaneDiagnosticKey(vehicle, lane);
            if (m_LastWatchedPublicTransportLaneDiagnostics.TryGetValue(
                    key,
                    out WatchedPublicTransportLaneDiagnosticState previousState) &&
                WatchedPublicTransportLaneDiagnosticStatesEqual(
                    previousState,
                    currentState))
            {
                return false;
            }

            m_LastWatchedPublicTransportLaneDiagnostics[key] = currentState;
            return true;
        }

        private static bool WatchedPublicTransportLaneDiagnosticStatesEqual(
            WatchedPublicTransportLaneDiagnosticState left,
            WatchedPublicTransportLaneDiagnosticState right)
        {
            return left.CurrentLane == right.CurrentLane &&
                left.Owner == right.Owner &&
                left.Aggregate == right.Aggregate &&
                left.ResolvedRoad == right.ResolvedRoad &&
                left.LaneComponentBits == right.LaneComponentBits &&
                left.PublicOnly == right.PublicOnly &&
                left.UnauthorizedPublicTransportLane ==
                right.UnauthorizedPublicTransportLane &&
                left.HasResolvedPublicTransportLanePolicy ==
                right.HasResolvedPublicTransportLanePolicy &&
                left.VanillaAllows == right.VanillaAllows &&
                left.ModAllows == right.ModAllows;
        }

        private PublicTransportLaneDiagnosticComponentBits GetPublicTransportLaneDiagnosticComponentBits(
            Entity lane)
        {
            PublicTransportLaneDiagnosticComponentBits bits =
                PublicTransportLaneDiagnosticComponentBits.None;
            if (m_RouteLaneData.HasComponent(lane))
            {
                bits |= PublicTransportLaneDiagnosticComponentBits.RouteLane;
            }

            if (m_SlaveLaneData.HasComponent(lane))
            {
                bits |= PublicTransportLaneDiagnosticComponentBits.SlaveLane;
            }

            if (m_EdgeLaneData.HasComponent(lane))
            {
                bits |= PublicTransportLaneDiagnosticComponentBits.EdgeLane;
            }

            if (m_ConnectionLaneData.HasComponent(lane))
            {
                bits |= PublicTransportLaneDiagnosticComponentBits.ConnectionLane;
            }

            return bits;
        }

        private static bool HasLaneDiagnosticBit(
            PublicTransportLaneDiagnosticComponentBits value,
            PublicTransportLaneDiagnosticComponentBits flag)
        {
            return (value & flag) != 0;
        }

        private static DiagnosticBoolState GetDiagnosticBoolState(
            bool hasValue,
            bool value)
        {
            if (!hasValue)
            {
                return DiagnosticBoolState.Unknown;
            }

            return value
                ? DiagnosticBoolState.True
                : DiagnosticBoolState.False;
        }

        private static string FormatDiagnosticBoolState(DiagnosticBoolState value)
        {
            switch (value)
            {
                case DiagnosticBoolState.False:
                    return bool.FalseString;

                case DiagnosticBoolState.True:
                    return bool.TrueString;

                default:
                    return "n/a";
            }
        }

        private bool TryGetMidBlockPenaltyTag(
            Entity sourceLane,
            Entity sourceOwner,
            Entity targetLane,
            Entity targetOwner,
            out string tag)
        {
            tag = null;

            if (!MidBlockCrossingPolicy.TryGetIllegalTransition(
                    EntityManager,
                    sourceLane,
                    targetLane,
                    out LaneTransitionViolationReasonCode reasonCode))
            {
                return false;
            }

            tag = $"mid-block({FormatMidBlockReasonTag(reasonCode)})";
            return true;
        }

        private static string FormatMidBlockReasonTag(
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

        private bool TryResolveAllowedOnPublicTransportLaneForLogging(
            Entity vehicle,
            out bool allowedOnPublicTransportLane)
        {
            if (!m_ProfileData.TryGetComponent(vehicle, out VehicleTrafficLawProfile profile))
            {
                allowedOnPublicTransportLane =
                    EmergencyVehiclePolicy.IsEmergencyVehicle(vehicle, ref m_TypeLookups);
                return allowedOnPublicTransportLane;
            }

            allowedOnPublicTransportLane =
                PublicTransportLanePolicy.CanUsePublicTransportLane(profile);

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
            RoutePenaltyInspectionResult previousSnapshot,
            RoutePenaltyInspectionResult currentSnapshot)
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
            RoutePenaltyInspectionResult previousSnapshot,
            RoutePenaltyInspectionResult currentSnapshot,
            bool focusedWatch)
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
            string previousTags =
                RoutePenaltyInspection.BuildTagSummary(previousSnapshot.TagSnapshot);
            string currentTags =
                RoutePenaltyInspection.BuildTagSummary(currentSnapshot.TagSnapshot);
            string previousNormalizedTags =
                RoutePenaltyInspection.BuildTagSummary(previousSnapshot.NormalizedTagSnapshot);
            string currentNormalizedTags =
                RoutePenaltyInspection.BuildTagSummary(currentSnapshot.NormalizedTagSnapshot);

            string comparisonMode = allowPublicTransportLaneComparison
                ? "full"
                : "excluding-unresolved-pt";

            string message =
                $"Pathfinding reroute (estimated): vehicle={FormatEntityOrNone(vehicle)}, role={role}, focusedWatch={focusedWatch}, comparisonMode={comparisonMode}, " +
                $"avoidedPenalty={avoidedPenalty}, " +
                $"fromPenalty={previousComparablePenalty} [{previousComparableBreakdown}], " +
                $"toPenalty={currentComparablePenalty} [{currentComparableBreakdown}], " +
                $"fromTags={previousTags}, toTags={currentTags}, " +
                $"fromNormalizedTags={previousNormalizedTags}, toNormalizedTags={currentNormalizedTags}";

            EnforcementTelemetry.RecordEvent(message);
            Mod.log.Info(message);
        }

        private static void RecordRerouteTelemetry(
            Entity vehicle,
            RoutePenaltyInspectionResult previousSnapshot,
            RoutePenaltyInspectionResult currentSnapshot)
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
            m_RemovedVehiclesBuffer.Clear();
            foreach (KeyValuePair<Entity, RoutePenaltyInspectionResult> pair in m_LastSnapshots)
            {
                if (EntityManager.Exists(pair.Key) && m_CurrentLaneData.HasComponent(pair.Key))
                {
                    continue;
                }

                m_RemovedVehiclesBuffer.Add(pair.Key);
            }

            if (m_RemovedVehiclesBuffer.Count == 0)
            {
                return;
            }

            for (int index = 0; index < m_RemovedVehiclesBuffer.Count; index++)
            {
                Entity vehicle = m_RemovedVehiclesBuffer[index];
                m_LastSnapshots.Remove(vehicle);
                m_LastRouteSelectionSnapshots.Remove(vehicle);
            }

            m_RemovedVehiclesBuffer.Clear();
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

            StringBuilder summary = new StringBuilder(penaltyTags.Count * 24);
            for (int index = 0; index < penaltyTags.Count; index += 1)
            {
                if (index > 0)
                {
                    summary.Append("; ");
                }

                summary.Append(penaltyTags[index]);
            }

            if (omittedTagCount > 0)
            {
                summary.Append("; ... (+")
                    .Append(omittedTagCount)
                    .Append(" more)");
            }

            return summary.ToString();
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
            StringBuilder parts = new StringBuilder(64);
            bool hasParts = false;
            if (profile.PublicTransportLaneSegments > 0)
            {
                parts.Append("PT-lane ")
                    .Append(profile.PublicTransportLaneSegments)
                    .Append(" x ")
                    .Append(EnforcementPenaltyService.GetPublicTransportLaneFine());
                hasParts = true;
            }

            if (profile.MidBlockTransitions > 0)
            {
                if (hasParts)
                {
                    parts.Append(", ");
                }

                parts.Append("mid-block ")
                    .Append(profile.MidBlockTransitions)
                    .Append(" x ")
                    .Append(EnforcementPenaltyService.GetMidBlockCrossingFine());
                hasParts = true;
            }

            if (profile.IntersectionTransitions > 0)
            {
                if (hasParts)
                {
                    parts.Append(", ");
                }

                parts.Append("intersection ")
                    .Append(profile.IntersectionTransitions)
                    .Append(" x ")
                    .Append(EnforcementPenaltyService.GetIntersectionMovementFine());
                hasParts = true;
            }

            return hasParts ? parts.ToString() : "none";
        }

        private static int CalculateComparableTotalPenalty(
            RoutePenaltyInspectionResult snapshot,
            bool allowPublicTransportLaneComparison)
        {
            var comparableProfile = snapshot.Profile;

            if (!allowPublicTransportLaneComparison)
            {
                comparableProfile.PublicTransportLaneSegments = 0;
            }

            return RoutePenaltyInspection.CalculateTotalPenalty(comparableProfile);
        }

        private static string BuildComparableBreakdown(
            RoutePenaltyInspectionResult snapshot,
            bool allowPublicTransportLaneComparison)
        {
            var comparableProfile = snapshot.Profile;

            if (!allowPublicTransportLaneComparison)
            {
                comparableProfile.PublicTransportLaneSegments = 0;
            }

            return RoutePenaltyInspection.BuildBreakdown(comparableProfile);
        }

        private static string FormatMovement(LaneMovement movement)
        {
            StringBuilder parts = new StringBuilder(24);
            bool hasParts = false;
            if ((movement & LaneMovement.Forward) != 0)
            {
                parts.Append("forward");
                hasParts = true;
            }

            if ((movement & LaneMovement.Left) != 0)
            {
                if (hasParts)
                {
                    parts.Append('+');
                }

                parts.Append("left");
                hasParts = true;
            }

            if ((movement & LaneMovement.Right) != 0)
            {
                if (hasParts)
                {
                    parts.Append('+');
                }

                parts.Append("right");
                hasParts = true;
            }

            if ((movement & LaneMovement.UTurn) != 0)
            {
                if (hasParts)
                {
                    parts.Append('+');
                }

                parts.Append("u-turn");
                hasParts = true;
            }

            return hasParts ? parts.ToString() : "none";
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

        private readonly struct RouteSelectionChangeSnapshot
        {
            public readonly uint RouteHash;
            public readonly RoutePenaltyInspectionResult Inspection;
            public readonly Entity CurrentLane;
            public readonly bool HasCurrentRoute;
            public readonly Entity CurrentRoute;
            public readonly bool HasCurrentTarget;
            public readonly Entity CurrentTarget;
            public readonly bool HasPathOwner;
            public readonly PathFlags PathFlags;
            public readonly bool HasPathInformation;
            public readonly int PathInfoHash;
            public readonly int AcceptedPathHash;
            public readonly int AcceptedResultHash;
            public readonly int AcceptedPathElementCount;

            public RouteSelectionChangeSnapshot(
                RoutePenaltyInspectionResult inspection,
                Entity currentLane,
                bool hasCurrentRoute,
                Entity currentRoute,
                bool hasCurrentTarget,
                Entity currentTarget,
                bool hasPathOwner,
                PathFlags pathFlags,
                bool hasPathInformation,
                int pathInfoHash,
                int acceptedPathHash,
                int acceptedResultHash,
                int acceptedPathElementCount)
            {
                RouteHash = inspection.RouteHash;
                Inspection = inspection;
                CurrentLane = currentLane;
                HasCurrentRoute = hasCurrentRoute;
                CurrentRoute = currentRoute;
                HasCurrentTarget = hasCurrentTarget;
                CurrentTarget = currentTarget;
                HasPathOwner = hasPathOwner;
                PathFlags = pathFlags;
                HasPathInformation = hasPathInformation;
                PathInfoHash = pathInfoHash;
                AcceptedPathHash = acceptedPathHash;
                AcceptedResultHash = acceptedResultHash;
                AcceptedPathElementCount = acceptedPathElementCount;
            }
        }

    }
}
