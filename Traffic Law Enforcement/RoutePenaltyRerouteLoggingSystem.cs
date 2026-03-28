using System.Collections.Generic;
using Game;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Routes;
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
        private BufferLookup<CarNavigationLane> m_NavigationLaneData;
        private ComponentLookup<CarCurrentLane> m_CurrentLaneData;
        private ComponentLookup<Target> m_TargetData;
        private ComponentLookup<CurrentRoute> m_CurrentRouteData;
        private ComponentLookup<PathOwner> m_PathOwnerData;
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
        private readonly Dictionary<Entity, RoutePenaltyInspectionResult> m_LastSnapshots = new Dictionary<Entity, RoutePenaltyInspectionResult>();
        private readonly Dictionary<Entity, RouteSelectionChangeSnapshot> m_LastRouteSelectionSnapshots = new Dictionary<Entity, RouteSelectionChangeSnapshot>();
        private readonly HashSet<Entity> m_CandidateVehicles = new HashSet<Entity>();
        private Game.UI.NameSystem m_NameSystem;
        private Game.Prefabs.PrefabSystem m_PrefabSystem;
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
            m_NavigationLaneData = GetBufferLookup<CarNavigationLane>(true);
            m_CurrentLaneData = GetComponentLookup<CarCurrentLane>(true);
            m_TargetData = GetComponentLookup<Target>(true);
            m_CurrentRouteData = GetComponentLookup<CurrentRoute>(true);
            m_PathOwnerData = GetComponentLookup<PathOwner>(true);
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
            bool restrictVehicleSpecificRouteLogsToWatchedVehicles =
                EnforcementLoggingPolicy.ShouldRestrictVehicleSpecificRouteDebugLogsToWatchedVehicles();
            bool burstLoggingActive = EnforcementLoggingPolicy.IsBurstLoggingActive;
            int rerouteLogLimit =
                burstLoggingActive
                    ? BurstLoggingService.BurstEstimatedRerouteLogsPerUpdate
                    : MaxLogsPerUpdate;
            int routeSelectionLogLimit =
                burstLoggingActive
                    ? BurstLoggingService.BurstRouteSelectionChangeLogsPerUpdate
                    : MaxRouteSelectionChangeLogsPerUpdate;
            m_PublicTransportLaneDecisionDiagnosticLogsThisUpdate = 0;
            if (!Mod.IsEnforcementEnabled)
            {
                m_LastSnapshots.Clear();
                m_LastRouteSelectionSnapshots.Clear();
                RerouteLoggingTelemetry.SetState(false, 0, 0, 0);
                return;
            }

            m_NavigationLaneData.Update(this);
            m_CurrentLaneData.Update(this);
            m_TargetData.Update(this);
            m_CurrentRouteData.Update(this);
            m_PathOwnerData.Update(this);
            m_OwnerData.Update(this);
            m_AggregatedData.Update(this);
            m_PrefabRefData.Update(this);
            m_CarLaneData.Update(this);
            m_PrefabCarData.Update(this);
            m_CargoTransportData.Update(this);
            m_PublicTransportData.Update(this);
            m_PendingExitData.Update(this);
            m_SlaveLaneData.Update(this);
            m_RouteLaneData.Update(this);
            m_EdgeLaneData.Update(this);
            m_ParkingLaneData.Update(this);
            m_GarageLaneData.Update(this);
            m_ConnectionLaneData.Update(this);
            m_SubLaneData.Update(this);
            m_PathElementData.Update(this);
            m_ProfileData.Update(this);
            m_TypeLookups.Update(this);
            FocusedLoggingService.PruneMissingVehicles(EntityManager);
            bool hasWatchedVehicles = FocusedLoggingService.HasWatchedVehicles;
            bool trackRouteSelectionChanges =
                routeSelectionSummaryLoggingEnabled ||
                (focusedRouteRebuildDiagnosticsLoggingEnabled && hasWatchedVehicles);

            m_CandidateVehicles.Clear();
            bool trackVehicleSpecificRouteLogs =
                estimatedRerouteLoggingEnabled ||
                pathfindingPenaltyDiagnosticLoggingEnabled ||
                trackRouteSelectionChanges;
            if (trackVehicleSpecificRouteLogs)
            {
                CollectCandidateVehicles(m_CurrentLaneChangedQuery);
                CollectCandidateVehicles(m_NavigationLaneChangedQuery);
                CollectCandidateVehicles(m_CarChangedQuery);
                if (trackRouteSelectionChanges)
                {
                    CollectCandidateVehicles(m_TargetChangedQuery);
                    CollectCandidateVehicles(m_CurrentRouteChangedQuery);
                    CollectCandidateVehicles(m_PathOwnerChangedQuery);
                }

                bool requireAllCandidateVehicles =
                    routeSelectionSummaryLoggingEnabled ||
                    (estimatedRerouteLoggingEnabled &&
                     !restrictVehicleSpecificRouteLogsToWatchedVehicles) ||
                    (pathfindingPenaltyDiagnosticLoggingEnabled &&
                     !restrictVehicleSpecificRouteLogsToWatchedVehicles);

                if (!requireAllCandidateVehicles)
                {
                    m_CandidateVehicles.RemoveWhere(
                        static vehicle => !FocusedLoggingService.IsWatched(vehicle));
                }
            }
            else
            {
                m_LastRouteSelectionSnapshots.Clear();
            }

            int logsEmitted = 0;
            int routeSelectionLogsEmitted = 0;
            int routeSelectionLogsDropped = 0;
            foreach (Entity vehicle in m_CandidateVehicles)
            {
                if (!m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLane))
                {
                    continue;
                }

                bool watchedVehicle = FocusedLoggingService.IsWatched(vehicle);

                RoutePenaltyInspectionResult snapshot =
                    BuildSnapshot(
                        vehicle,
                        currentLane,
                        captureDebugStrings:
                            estimatedRerouteLoggingEnabled ||
                            routeSelectionSummaryLoggingEnabled ||
                            focusedRouteRebuildDiagnosticsLoggingEnabled ||
                            pathfindingPenaltyDiagnosticLoggingEnabled,
                        allowVehicleSpecificPenaltyDiagnostics:
                            EnforcementLoggingPolicy.ShouldLogVehicleSpecificPathfindingPenaltyDiagnostics(vehicle));

                bool rerouteDetected = false;
                bool allowVehicleSpecificWatchedLogs =
                    !restrictVehicleSpecificRouteLogsToWatchedVehicles ||
                    watchedVehicle;

                if (m_LastSnapshots.TryGetValue(vehicle, out RoutePenaltyInspectionResult previousSnapshot))
                {
                    rerouteDetected = ShouldLogReroute(previousSnapshot, snapshot);
                    if (rerouteDetected)
                    {
                        RecordRerouteTelemetry(vehicle, previousSnapshot, snapshot);

                        if (estimatedRerouteLoggingEnabled &&
                            allowVehicleSpecificWatchedLogs &&
                            (watchedVehicle || logsEmitted < rerouteLogLimit))
                        {
                            LogReroute(
                                vehicle,
                                previousSnapshot,
                                snapshot,
                                watchedVehicle);

                            if (!watchedVehicle)
                            {
                                logsEmitted += 1;
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

                RouteSelectionChangeSnapshot routeSelectionSnapshot =
                    BuildRouteSelectionSnapshot(vehicle, currentLane.m_Lane, snapshot);

                if (m_LastRouteSelectionSnapshots.TryGetValue(
                        vehicle,
                        out RouteSelectionChangeSnapshot previousRouteSelectionSnapshot))
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
                            routeSelectionSummaryLoggingEnabled ||
                            emitFocusedDiagnostics;

                        if (!emitSummary)
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

            if (trackRouteSelectionChanges && routeSelectionLogsDropped > 0)
            {
                string droppedMessage =
                    $"Route selection change logging throttled: dropped={routeSelectionLogsDropped}, emitted={routeSelectionLogsEmitted}, candidates={m_CandidateVehicles.Count}, limit={routeSelectionLogLimit}, burstActive={burstLoggingActive}";
                EnforcementTelemetry.RecordEvent(droppedMessage);
                Mod.log.Info(droppedMessage);
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
            m_LastRouteSelectionSnapshots.Clear();
            m_CandidateVehicles.Clear();
            m_UpdateCount = 0;
            FocusedLoggingService.ClearWatchedVehiclesForRuntimeWorldReset(currentGeneration);
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
                captureDebugStrings,
                MaxPenaltyTags);
        }

        private RouteSelectionChangeSnapshot BuildRouteSelectionSnapshot(
            Entity vehicle,
            Entity currentLane,
            RoutePenaltyInspectionResult inspection)
        {
            bool hasCurrentTarget =
                m_TargetData.TryGetComponent(vehicle, out Target targetData) &&
                targetData.m_Target != Entity.Null;
            Entity currentTarget =
                hasCurrentTarget
                    ? targetData.m_Target
                    : Entity.Null;

            bool hasCurrentRoute =
                m_CurrentRouteData.TryGetComponent(vehicle, out CurrentRoute currentRouteData) &&
                currentRouteData.m_Route != Entity.Null;
            Entity currentRoute =
                hasCurrentRoute
                    ? currentRouteData.m_Route
                    : Entity.Null;

            bool hasPathOwner =
                m_PathOwnerData.TryGetComponent(vehicle, out PathOwner pathOwner);
            PathFlags pathFlags =
                hasPathOwner
                    ? pathOwner.m_State
                    : default;

            return new RouteSelectionChangeSnapshot(
                inspection,
                currentLane,
                hasCurrentRoute,
                currentRoute,
                hasCurrentTarget,
                currentTarget,
                hasPathOwner,
                pathFlags);
        }

        private RoutePenaltyInspectionContext CreateInspectionContext()
        {
            return new RoutePenaltyInspectionContext
            {
                EntityManager = EntityManager,
                OwnerData = m_OwnerData,
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
                previousSnapshot.RouteHash != currentSnapshot.RouteHash ||
                previousSnapshot.HasCurrentRoute != currentSnapshot.HasCurrentRoute ||
                previousSnapshot.CurrentRoute != currentSnapshot.CurrentRoute ||
                previousSnapshot.HasCurrentTarget != currentSnapshot.HasCurrentTarget ||
                previousSnapshot.CurrentTarget != currentSnapshot.CurrentTarget ||
                previousSnapshot.HasPathOwner != currentSnapshot.HasPathOwner ||
                previousSnapshot.PathFlags != currentSnapshot.PathFlags;
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

            string message =
                $"Route selection change: vehicle={vehicle}, role={role}, focusedWatch={focusedWatch}, reasons={reasons}, " +
                $"currentLane={currentSnapshot.CurrentLane}, " +
                $"routeHash={previousSnapshot.RouteHash}->{currentSnapshot.RouteHash}, " +
                $"currentRoute={FormatOptionalEntity(previousSnapshot.HasCurrentRoute, previousSnapshot.CurrentRoute)}->{FormatOptionalEntity(currentSnapshot.HasCurrentRoute, currentSnapshot.CurrentRoute)}, " +
                $"currentTarget={FormatOptionalEntity(previousSnapshot.HasCurrentTarget, previousSnapshot.CurrentTarget)}->{FormatOptionalEntity(currentSnapshot.HasCurrentTarget, currentSnapshot.CurrentTarget)}, " +
                $"pathState={FormatPathState(previousSnapshot)}->{FormatPathState(currentSnapshot)}, " +
                $"plannedPenalty={previousSnapshot.Inspection.TotalPenalty} [{previousSnapshot.Inspection.Breakdown}] -> {currentSnapshot.Inspection.TotalPenalty} [{currentSnapshot.Inspection.Breakdown}], " +
                $"tags={previousSnapshot.Inspection.Tags} -> {currentSnapshot.Inspection.Tags}";

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

                string nameResolutionMessage =
                    BuildFocusedLaneNameResolutionPreview(
                        vehicle,
                        currentSnapshot);
                EnforcementTelemetry.RecordEvent(nameResolutionMessage);
                Mod.log.Info(nameResolutionMessage);
            }
        }

        private static string BuildRouteSelectionChangeReasons(
            RouteSelectionChangeSnapshot previousSnapshot,
            RouteSelectionChangeSnapshot currentSnapshot,
            bool rerouteDetected)
        {
            List<string> reasons = new List<string>(5);
            if (rerouteDetected)
            {
                reasons.Add("reroute");
            }

            if (previousSnapshot.RouteHash != currentSnapshot.RouteHash)
            {
                reasons.Add("route-hash");
            }

            if (previousSnapshot.HasCurrentRoute != currentSnapshot.HasCurrentRoute ||
                previousSnapshot.CurrentRoute != currentSnapshot.CurrentRoute)
            {
                reasons.Add("current-route");
            }

            if (previousSnapshot.HasCurrentTarget != currentSnapshot.HasCurrentTarget ||
                previousSnapshot.CurrentTarget != currentSnapshot.CurrentTarget)
            {
                reasons.Add("target");
            }

            if (previousSnapshot.HasPathOwner != currentSnapshot.HasPathOwner ||
                previousSnapshot.PathFlags != currentSnapshot.PathFlags)
            {
                reasons.Add("path-state");
            }

            return reasons.Count == 0
                ? "none"
                : string.Join(",", reasons.ToArray());
        }

        private static string FormatOptionalEntity(bool hasValue, Entity entity)
        {
            return hasValue && entity != Entity.Null
                ? entity.ToString()
                : "none";
        }

        private static string FormatPathState(RouteSelectionChangeSnapshot snapshot)
        {
            return snapshot.HasPathOwner
                ? snapshot.PathFlags.ToString()
                : "none";
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

            return
                $"FOCUSED_ROUTE_REBUILD: vehicle={vehicle}, " +
                $"previousTarget={FormatOptionalEntity(previousSnapshot.HasCurrentTarget, previousTarget)}, " +
                $"currentTarget={FormatOptionalEntity(currentSnapshot.HasCurrentTarget, currentTarget)}, " +
                $"currentLane={currentLane}, normalizedCurrentLane={normalizedCurrentLane}, " +
                $"targetChanged={targetChanged}, " +
                $"predictedOriginSource={predictedOriginSource}, " +
                $"previousTargetEndMatchesCurrent={previousTargetEndMatchesCurrent}, " +
                $"previousTargetEndLane={FormatEntityOrNone(previousTargetEndLane)}, " +
                $"targetRouteLane={targetRouteLane}, " +
                $"previousTargetRouteLane={previousTargetRouteLane}, " +
                $"upcomingPath={upcomingPathPreview}, " +
                $"navigationPreview={navigationPreview}, " +
                $"{pathfindContext}";
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

            List<string> transitions = new List<string>(maxTransitions);
            Entity sourceLane = currentLane;
            if (hasNavigationLanes)
            {
                for (int index = 0; index < navigationLanes.Length && transitions.Count < maxTransitions; index += 1)
                {
                    Entity targetLane = navigationLanes[index].m_Lane;
                    if (targetLane == Entity.Null || targetLane == sourceLane)
                    {
                        continue;
                    }

                    transitions.Add(
                        DescribeFocusedChosenTransition(
                            sourceLane,
                            targetLane,
                            targetStartLane,
                            targetEndLane,
                            hasResolvedPolicy,
                            allowedOnPublicTransportLane,
                            ref previousUnauthorizedPublicTransportLane,
                            ref context));
                    sourceLane = targetLane;
                }
            }

            string transitionSummary =
                transitions.Count == 0
                    ? "none"
                    : string.Join("; ", transitions.ToArray());

            return
                $"FOCUSED_ROUTE_TRANSITIONS: vehicle={vehicle}, " +
                $"targetStartLane={FormatEntityOrNone(targetStartLane)}, " +
                $"targetEndLane={FormatEntityOrNone(targetEndLane)}, " +
                $"chosenTransitions={transitionSummary}";
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

            List<string> navigationLaneResolutions = new List<string>(maxNavigationLanes);
            if (m_NavigationLaneData.TryGetBuffer(vehicle, out DynamicBuffer<CarNavigationLane> navigationLanes))
            {
                for (int index = 0; index < navigationLanes.Length && navigationLaneResolutions.Count < maxNavigationLanes; index += 1)
                {
                    Entity lane = navigationLanes[index].m_Lane;
                    if (lane == Entity.Null ||
                        lane == currentLane ||
                        lane == targetStartLane ||
                        lane == targetEndLane)
                    {
                        continue;
                    }

                    navigationLaneResolutions.Add(
                        $"n{navigationLaneResolutions.Count}=" +
                        DescribeFocusedLaneNameResolution(lane, ref formatterContext));
                }
            }

            string navigationSummary =
                navigationLaneResolutions.Count == 0
                    ? "none"
                    : string.Join("; ", navigationLaneResolutions.ToArray());

            return
                $"FOCUSED_ROUTE_NAME_RESOLUTION: vehicle={vehicle}, " +
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

            List<string> penaltyParts = new List<string>(3);
            if (unauthorizedTargetLane && !previousUnauthorizedPublicTransportLane)
            {
                penaltyParts.Add(
                    $"pt=+{EnforcementPenaltyService.GetPublicTransportLaneFine()}");
            }

            if (TryGetMidBlockPenaltyTag(
                    sourceLane,
                    Entity.Null,
                    targetLane,
                    Entity.Null,
                    out string midBlockTag))
            {
                penaltyParts.Add(
                    $"mid=+{EnforcementPenaltyService.GetMidBlockCrossingFine()}({midBlockTag})");
            }

            if (TryGetIntersectionPenaltyTag(
                    sourceLane,
                    targetLane,
                    out string intersectionTag))
            {
                penaltyParts.Add(
                    $"int=+{EnforcementPenaltyService.GetIntersectionMovementFine()}({intersectionTag})");
            }

            if (TryGetOutboundAccessPenaltyTag(
                    sourceLane,
                    targetLane,
                    out string outboundAccessTag))
            {
                penaltyParts.Add(
                    $"rule={outboundAccessTag}");
            }

            previousUnauthorizedPublicTransportLane = unauthorizedTargetLane;

            string penaltySummary =
                penaltyParts.Count == 0
                    ? "none"
                    : string.Join("|", penaltyParts.ToArray());

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

            return
                $"{FormatEntityOrNone(lane)}" +
                $"{{display=\"{displayText}\", owner={ownerText}, aggregate={aggregateText}, " +
                $"resolvedRoad={resolvedRoadText}, nameSource={nameSource}}}";
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

            SelectedObjectDisplayFormatterContext formatterContext =
                CreateDisplayFormatterContext();

            int startIndex = pathOwner.m_ElementIndex;
            if (startIndex < 0)
            {
                startIndex = 0;
            }
            else if (startIndex >= pathElements.Length)
            {
                startIndex = pathElements.Length - 1;
            }

            List<string> parts = new List<string>(maxPreviewElements + 1);
            int emitted = 0;
            for (int index = startIndex; index < pathElements.Length && emitted < maxPreviewElements; index++, emitted++)
            {
                PathElement pathElement = pathElements[index];
                string laneText =
                    SelectedObjectDisplayFormatter.BuildLaneDisplayText(
                        pathElement.m_Target,
                        ref formatterContext);
                parts.Add(
                    $"{index}:{pathElement.m_Target} \"{laneText}\"[{pathElement.m_TargetDelta.x:0.###}->{pathElement.m_TargetDelta.y:0.###}|{pathElement.m_Flags}]");
            }

            int remaining = pathElements.Length - startIndex - emitted;
            if (remaining > 0)
            {
                parts.Add($"+{remaining} more");
            }

            return parts.Count == 0
                ? "none"
                : string.Join("; ", parts.ToArray());
        }

        private static string FormatNamedEntityOrNone(
            Entity entity,
            ref SelectedObjectDisplayFormatterContext formatterContext)
        {
            return entity == Entity.Null
                ? "none"
                : SelectedObjectDisplayFormatter.FormatNamedEntity(entity, ref formatterContext);
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
            return entity == Entity.Null ? "none" : entity.ToString();
        }

        private static string FormatRuleFlags(RuleFlags flags)
        {
            return flags == 0 ? "none" : flags.ToString();
        }

        private static string FormatPathfindFlags(PathfindFlags flags)
        {
            return flags == 0 ? "none" : flags.ToString();
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

            string vanillaAllows = hasProfile
                ? PublicTransportLanePolicy.VanillaAllowsAccess(
                    vehicleProfile.m_PublicTransportLaneAccessBits).ToString()
                : "n/a";

            string modAllows = hasProfile
                ? PublicTransportLanePolicy.ModAllowsAccess(
                    vehicleProfile.m_PublicTransportLaneAccessBits).ToString()
                : "n/a";

            string canUsePublicTransportLane = hasProfile
                ? PublicTransportLanePolicy.CanUsePublicTransportLane(
                    vehicleProfile.m_PublicTransportLaneAccessBits,
                    emergency).ToString()
                : "n/a";

            string type = hasProfile
                ? PublicTransportLanePolicy.DescribeType(
                    vehicleProfile.m_PublicTransportLaneAccessBits)
                : "n/a";

            string permissionChangedByMod = hasProfile
                ? PublicTransportLanePolicy.PermissionChangedByMod(
                    vehicleProfile.m_PublicTransportLaneAccessBits).ToString()
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

            string message =
                $"PT_ROUTE_DIAG: vehicle={vehicle}, role={role}, lane={lane}, laneKind={laneKind}, " +
                $"focusedWatch={forceLogging}, " +
                $"publicOnly={publicOnly}, hasResolvedPolicy={hasResolvedPublicTransportLanePolicy}, " +
                $"hasProfile={hasProfile}, allowedOnPublicTransportLane={allowedOnPublicTransportLane}, " +
                $"unauthorizedPublicTransportLane={unauthorizedPublicTransportLane}, engineHasFlag={engineHasFlag}, " +
                $"emergency={emergency}, emergencyOverrideActive={emergencyOverrideActive}, " +
                $"type={type}, vanillaAllows={vanillaAllows}, modAllows={modAllows}, " +
                $"canUsePublicTransportLane={canUsePublicTransportLane}, " +
                $"permissionChangedByMod={permissionChangedByMod}, accessBits={accessBits}";

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

            string comparisonMode = allowPublicTransportLaneComparison
                ? "full"
                : "excluding-unresolved-pt";

            string message =
                $"Pathfinding reroute (estimated): vehicle={vehicle}, role={role}, focusedWatch={focusedWatch}, comparisonMode={comparisonMode}, " +
                $"avoidedPenalty={avoidedPenalty}, " +
                $"fromPenalty={previousComparablePenalty} [{previousComparableBreakdown}], " +
                $"toPenalty={currentComparablePenalty} [{currentComparableBreakdown}], " +
                $"fromTags={previousSnapshot.Tags}, toTags={currentSnapshot.Tags}";

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
            List<Entity> removedVehicles = null;
            foreach (KeyValuePair<Entity, RoutePenaltyInspectionResult> pair in m_LastSnapshots)
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
                m_LastRouteSelectionSnapshots.Remove(removedVehicles[index]);
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

            public RouteSelectionChangeSnapshot(
                RoutePenaltyInspectionResult inspection,
                Entity currentLane,
                bool hasCurrentRoute,
                Entity currentRoute,
                bool hasCurrentTarget,
                Entity currentTarget,
                bool hasPathOwner,
                PathFlags pathFlags)
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
            }
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
