using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Colossal.Mathematics;
using Game.Net;
using Game.Pathfind;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using PathfindEdge = Game.Pathfind.Edge;

namespace Traffic_Law_Enforcement
{
    internal static class PathfindCandidateProbePatches
    {
        private const string HarmonyId = "Traffic_Law_Enforcement.PathfindCandidateProbePatches";
        private const int MaxCandidateEntriesPerRequest = 8;
        private const int MaxLoggedCandidatesPerRequest = 4;
        private const int MaxDetailedIntersectionLikeLogs = 8;
        private const long LiveProbeHeartbeatInterval = 10000;
        private const string IntersectionProbePrefix = "[IM-AHD-PROBE]";

        private sealed class ProbeRequestInfo
        {
            public Entity Vehicle;
            public int ActionIndex;
            public string RequestKey;
            public string StartPreview;
            public string EndPreview;
        }

        private sealed class CandidateInfo
        {
            public Entity EdgeOwner;
            public EdgeID EdgeId;
            public EdgeID NextEdgeId;
            public bool HasNextEdge;
            public PathMethod Methods;
            public int AccessRequirement;
            public EdgeFlags Flags;
            public RuleFlags Rules;
            public float2 EdgeDelta;
            public float BaseCostBefore;
            public float EdgeCostAdded;
            public float CostFactor;
            public float BaseCostAfter;
            public float TotalCost;

            public string BuildKey()
            {
                StringBuilder builder = new StringBuilder(64);
                builder.Append(EdgeOwner.Index);
                builder.Append('|');
                builder.Append(EdgeId.m_Index);
                builder.Append('|');
                builder.Append(HasNextEdge ? NextEdgeId.m_Index : -1);
                builder.Append('|');
                builder.Append((int)Methods);
                builder.Append('|');
                builder.Append(AccessRequirement);
                builder.Append('|');
                builder.Append((int)Flags);
                builder.Append('|');
                builder.Append((int)Rules);
                builder.Append('|');
                builder.Append(math.round(EdgeDelta.x * 100f));
                builder.Append('|');
                builder.Append(math.round(EdgeDelta.y * 100f));
                return builder.ToString();
            }
        }

        private sealed class ProbeExecutionContext
        {
            private readonly Dictionary<string, CandidateInfo> m_BestCandidates =
                new Dictionary<string, CandidateInfo>(StringComparer.Ordinal);
            private readonly List<CandidateInfo> m_SortedCandidates =
                new List<CandidateInfo>(MaxCandidateEntriesPerRequest);

            public ProbeExecutionContext(ProbeRequestInfo request)
            {
                Request = request;
            }

            public ProbeRequestInfo Request { get; }

            public int CandidateProposalCount { get; private set; }

            public int TransitionSampleCount { get; private set; }

            public int IntersectionLikeSampleCount { get; private set; }

            public int IllegalLikeSampleCount { get; private set; }

            public int IntersectionLikeWithConnectionLaneCount { get; private set; }

            public int IntersectionLikeWithoutConnectionLaneCount { get; private set; }

            public void RecordCandidate(CandidateInfo candidate)
            {
                CandidateProposalCount += 1;

                string key = candidate.BuildKey();
                if (m_BestCandidates.TryGetValue(key, out CandidateInfo existing))
                {
                    if (candidate.TotalCost < existing.TotalCost)
                    {
                        m_BestCandidates[key] = candidate;
                    }

                    return;
                }

                if (m_BestCandidates.Count >= MaxCandidateEntriesPerRequest)
                {
                    string worstCandidateKey = null;
                    float worstCandidateCost = float.MinValue;
                    foreach (KeyValuePair<string, CandidateInfo> pair in m_BestCandidates)
                    {
                        if (pair.Value.TotalCost > worstCandidateCost)
                        {
                            worstCandidateKey = pair.Key;
                            worstCandidateCost = pair.Value.TotalCost;
                        }
                    }

                    if (candidate.TotalCost >= worstCandidateCost)
                    {
                        return;
                    }

                    m_BestCandidates.Remove(worstCandidateKey);
                }

                m_BestCandidates[key] = candidate;
            }

            public void RecordIntersectionSample(IntersectionTransitionSample sample)
            {
                TransitionSampleCount += 1;
                if (sample.IntersectionLike)
                {
                    IntersectionLikeSampleCount += 1;
                    if (sample.HasToConnectionLane)
                    {
                        IntersectionLikeWithConnectionLaneCount += 1;
                    }
                    else
                    {
                        IntersectionLikeWithoutConnectionLaneCount += 1;
                    }
                }

                if (sample.IllegalLike)
                {
                    IllegalLikeSampleCount += 1;
                }
            }

            public string BuildLogLine()
            {
                string candidateSummary;
                if (m_BestCandidates.Count == 0)
                {
                    candidateSummary = "none";
                }
                else
                {
                    StringBuilder builder = new StringBuilder(256);
                    m_SortedCandidates.Clear();
                    foreach (CandidateInfo candidate in m_BestCandidates.Values)
                    {
                        m_SortedCandidates.Add(candidate);
                    }

                    m_SortedCandidates.Sort(CompareCandidateCostAscending);
                    int ordinal = 1;
                    for (int index = 0; index < m_SortedCandidates.Count; index += 1)
                    {
                        if (ordinal > MaxLoggedCandidatesPerRequest)
                        {
                            break;
                        }

                        if (builder.Length > 0)
                        {
                            builder.Append("; ");
                        }

                        builder.Append(FormatCandidate(ordinal, m_SortedCandidates[index]));
                        ordinal += 1;
                    }

                    candidateSummary = builder.ToString();
                }

                return
                    $"FOCUSED_ROUTE_CANDIDATES: vehicle={Request.Vehicle}, " +
                    $"actionIndex={Request.ActionIndex}, " +
                    $"requestKey={Request.RequestKey}, " +
                    $"startTargets={Request.StartPreview}, " +
                    $"endTargets={Request.EndPreview}, " +
                    $"candidateProposals={CandidateProposalCount}, " +
                    $"bestCandidates={candidateSummary}";
            }
            private static int CompareCandidateCostAscending(CandidateInfo left, CandidateInfo right)
            {
                return left.TotalCost.CompareTo(right.TotalCost);
            }

            private static string FormatCandidate(int ordinal, CandidateInfo candidate)
            {
                string nextEdge =
                    candidate.HasNextEdge
                        ? candidate.NextEdgeId.m_Index.ToString()
                        : "none";

                return
                    $"{ordinal}) owner={FocusedLoggingService.FormatEntity(candidate.EdgeOwner)}, " +
                    $"edgeId={candidate.EdgeId.m_Index}, nextEdgeId={nextEdge}, " +
                    $"methods={candidate.Methods}, access={candidate.AccessRequirement}, " +
                    $"flags={candidate.Flags}, rules={candidate.Rules}, " +
                    $"delta=({candidate.EdgeDelta.x:0.###}->{candidate.EdgeDelta.y:0.###}), " +
                    $"baseBefore={candidate.BaseCostBefore:0.###}, edgeCost={candidate.EdgeCostAdded:0.###}, " +
                    $"factor={candidate.CostFactor:0.###}, baseAfter={candidate.BaseCostAfter:0.###}, " +
                    $"total={candidate.TotalCost:0.###}";
            }
        }

        private readonly struct IntersectionTransitionSample
        {
            public readonly EdgeID FromEdge;
            public readonly EdgeID ToEdge;
            public readonly Entity FromOwner;
            public readonly Entity ToOwner;
            public readonly bool HasFromCarLane;
            public readonly CarLaneFlags FromCarLaneFlags;
            public readonly bool HasToCarLane;
            public readonly CarLaneFlags ToCarLaneFlags;
            public readonly bool HasToConnectionLane;
            public readonly ConnectionLaneFlags ToConnectionLaneFlags;
            public readonly bool SideConnection;
            public readonly bool Forbidden;
            public readonly string TurnHint;
            public readonly bool IntersectionLike;
            public readonly bool IllegalLike;

            public IntersectionTransitionSample(
                EdgeID fromEdge,
                EdgeID toEdge,
                Entity fromOwner,
                Entity toOwner,
                bool hasFromCarLane,
                CarLaneFlags fromCarLaneFlags,
                bool hasToCarLane,
                CarLaneFlags toCarLaneFlags,
                bool hasToConnectionLane,
                ConnectionLaneFlags toConnectionLaneFlags,
                bool sideConnection,
                bool forbidden,
                string turnHint,
                bool intersectionLike,
                bool illegalLike)
            {
                FromEdge = fromEdge;
                ToEdge = toEdge;
                FromOwner = fromOwner;
                ToOwner = toOwner;
                HasFromCarLane = hasFromCarLane;
                FromCarLaneFlags = fromCarLaneFlags;
                HasToCarLane = hasToCarLane;
                ToCarLaneFlags = toCarLaneFlags;
                HasToConnectionLane = hasToConnectionLane;
                ToConnectionLaneFlags = toConnectionLaneFlags;
                SideConnection = sideConnection;
                Forbidden = forbidden;
                TurnHint = turnHint ?? "none";
                IntersectionLike = intersectionLike;
                IllegalLike = illegalLike;
            }

            public string BuildLogLine(Entity vehicle)
            {
                return
                    $"{IntersectionProbePrefix} " +
                    $"veh={FocusedLoggingService.FormatEntity(vehicle)} " +
                    $"fromEdge={FromEdge.m_Index} " +
                    $"toEdge={ToEdge.m_Index} " +
                    $"fromOwner={FocusedLoggingService.FormatEntity(FromOwner)} " +
                    $"toOwner={FocusedLoggingService.FormatEntity(ToOwner)} " +
                    $"fromCarLaneFlags={FormatCarLaneFlags(HasFromCarLane, FromCarLaneFlags)} " +
                    $"toCarLaneFlags={FormatCarLaneFlags(HasToCarLane, ToCarLaneFlags)} " +
                    $"sideConnection={SideConnection.ToString().ToLowerInvariant()} " +
                    $"forbidden={Forbidden.ToString().ToLowerInvariant()} " +
                    $"turnHint={TurnHint}";
            }
        }

        private struct PendingCandidate
        {
            public Entity EdgeOwner;
            public EdgeID EdgeId;
            public EdgeID NextEdgeId;
            public bool HasNextEdge;
            public PathMethod Methods;
            public int AccessRequirement;
            public EdgeFlags Flags;
            public RuleFlags Rules;
            public float2 EdgeDelta;
            public float BaseCostBefore;
            public float CostFactor;
            public LocationSpecification Location;
            public Bounds3 EndBounds;
            public float HeuristicCostFactor;
        }

        private static readonly Type s_PathfindExecutorType =
            AccessTools.Inner(typeof(PathfindJobs), "PathfindExecutor");

        private static readonly Type s_WorkerJobType =
            AccessTools.Inner(typeof(PathfindQueueSystem), "PathfindWorkerJob");

        private static readonly MethodInfo s_WorkerExecuteMethod = AccessTools.FirstMethod(
            s_WorkerJobType,
            method =>
                method.Name == "Execute" &&
                !method.IsStatic &&
                method.GetParameters().Length == 3 &&
                method.GetParameters()[0].ParameterType == typeof(PathfindActionData).MakeByRefType());

        private static readonly MethodInfo[] s_AddHeapDataMethods = GetAddHeapDataMethods();
        private static readonly MethodInfo s_ExactIntersectionAddHeapDataMethod = FindExactIntersectionAddHeapDataMethod();

        private static readonly MethodInfo s_CalculateCostMethod = AccessTools.FirstMethod(
            s_PathfindExecutorType,
            method => method.Name == "CalculateCost" &&
                      method.ReturnType == typeof(float) &&
                      method.GetParameters().Length == 4);

        private static readonly MethodInfo s_ReleaseMethod =
            AccessTools.Method(s_PathfindExecutorType, "Release");

        private static Harmony s_Harmony;
        private static readonly ConcurrentDictionary<string, ConcurrentQueue<ProbeRequestInfo>> s_RequestRegistry =
            new ConcurrentDictionary<string, ConcurrentQueue<ProbeRequestInfo>>(StringComparer.Ordinal);

        private static int s_RequestMatchMissLogCount;
        private static bool s_LoggedFirstWorkerExecuteInvocation;
        private static int s_DetailedIntersectionLikeLogCount;
        private static int s_DisabledFocusedDiagnosticsLogged;
        private static int s_DisabledNoWatchedVehiclesLogged;
        private static int s_LiveFirstHitLogged;
        private static long s_TotalHits;
        private static long s_AfterVehicleFilterHits;
        private static long s_BothSidesHaveOwnerHits;
        private static long s_BothSidesHaveCarLaneHits;
        private static long s_IntersectionLikeHits;
        private static long s_LastSummaryLoggedAtTotalHits;

        [ThreadStatic]
        private static ProbeExecutionContext s_CurrentContext;

        [ThreadStatic]
        private static PendingCandidate? s_PendingCandidate;

        internal static bool IsApplied => s_Harmony != null;

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            try
            {
                Mod.log.Info($"{IntersectionProbePrefix} begin install");

                if (s_PathfindExecutorType == null)
                {
                    LogInstallFailed("resolve-executor", "PathfindExecutor type not found");
                    return;
                }

                if (s_AddHeapDataMethods.Length == 0)
                {
                    LogInstallFailed("reflect-target", "no AddHeapData candidates found");
                    return;
                }

                if (s_ExactIntersectionAddHeapDataMethod == null)
                {
                    LogAddHeapDataCandidates();
                    LogInstallFailed("reflect-target", "exact 11-argument AddHeapData overload not found");
                    return;
                }

                Mod.log.Info(
                    $"{IntersectionProbePrefix} reflected target={DescribeMethod(s_ExactIntersectionAddHeapDataMethod)}");

                if (s_WorkerExecuteMethod == null)
                {
                    LogInstallFailed("resolve-helper", "PathfindWorkerJob.Execute target not found");
                    return;
                }

                if (s_CalculateCostMethod == null)
                {
                    LogInstallFailed("resolve-helper", "CalculateCost helper target not found");
                    return;
                }

                if (s_ReleaseMethod == null)
                {
                    LogInstallFailed("resolve-helper", "Release helper target not found");
                    return;
                }

                s_Harmony = new Harmony(HarmonyId);
                ResetIntersectionProbeDiagnostics();
                s_Harmony.Patch(
                    s_WorkerExecuteMethod,
                    prefix: new HarmonyMethod(typeof(PathfindCandidateProbePatches), nameof(WorkerExecutePrefix)),
                    postfix: new HarmonyMethod(typeof(PathfindCandidateProbePatches), nameof(WorkerExecutePostfix)));

                foreach (MethodInfo addHeapDataMethod in s_AddHeapDataMethods)
                {
                    ParameterInfo[] parameters = addHeapDataMethod.GetParameters();
                    if (parameters.Length == 10)
                    {
                        s_Harmony.Patch(
                            addHeapDataMethod,
                            prefix: new HarmonyMethod(typeof(PathfindCandidateProbePatches), nameof(AddHeapDataSinglePrefix)));
                    }
                    else if (parameters.Length == 11)
                    {
                        s_Harmony.Patch(
                            addHeapDataMethod,
                            prefix: new HarmonyMethod(typeof(PathfindCandidateProbePatches), nameof(AddHeapDataNextPrefix)));
                    }
                }

                s_Harmony.Patch(
                    s_CalculateCostMethod,
                    postfix: new HarmonyMethod(typeof(PathfindCandidateProbePatches), nameof(CalculateCostPostfix)));
                s_Harmony.Patch(
                    s_ReleaseMethod,
                    postfix: new HarmonyMethod(typeof(PathfindCandidateProbePatches), nameof(ReleasePostfix)));

                LogAddHeapDataPatchInfo(s_ExactIntersectionAddHeapDataMethod);
                LogProbeRuntimeState("apply");
                LogDisabledReasonIfNeeded("apply");
                Mod.log.Info(
                    $"{IntersectionProbePrefix} patch applied exact=true");
                Mod.log.Info(
                    $"Pathfind candidate probe patches applied. workerTarget={s_WorkerExecuteMethod}");
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                LogInstallFailed("patch-apply", ex.GetType().Name + ": " + ex.Message);
                Mod.log.Error(ex, "Failed to apply pathfind candidate probe patches.");
            }
        }

        private static MethodInfo[] GetAddHeapDataMethods()
        {
            if (s_PathfindExecutorType == null)
            {
                return Array.Empty<MethodInfo>();
            }

            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(s_PathfindExecutorType))
            {
                if (method.Name == "AddHeapData")
                {
                    methods.Add(method);
                }
            }

            if (methods.Count == 0)
            {
                return Array.Empty<MethodInfo>();
            }

            MethodInfo[] result = new MethodInfo[methods.Count];
            for (int index = 0; index < methods.Count; index += 1)
            {
                result[index] = methods[index];
            }

            return result;
        }

        public static void Remove()
        {
            if (s_Harmony == null)
            {
                return;
            }

            LogProbeRuntimeState("remove");
            MaybeLogIntersectionProbeSummary(force: true);
            s_Harmony.UnpatchAll(HarmonyId);
            s_Harmony = null;
            s_RequestRegistry.Clear();
            s_RequestMatchMissLogCount = 0;
            s_LoggedFirstWorkerExecuteInvocation = false;
            s_CurrentContext = null;
            s_PendingCandidate = null;
            ResetIntersectionProbeDiagnostics();
        }

        internal static void RegisterWatchedRequest(
            Entity vehicle,
            int actionIndex,
            PathfindParameters parameters,
            UnsafeList<PathTarget> startTargets,
            UnsafeList<PathTarget> endTargets)
        {
            if (!FocusedLoggingService.IsWatched(vehicle))
            {
                return;
            }

            string requestKey = BuildRequestKey(parameters, startTargets, endTargets);
            ProbeRequestInfo request = new ProbeRequestInfo
            {
                Vehicle = vehicle,
                ActionIndex = actionIndex,
                RequestKey = requestKey,
                StartPreview = BuildTargetPreview(startTargets),
                EndPreview = BuildTargetPreview(endTargets),
            };

            ConcurrentQueue<ProbeRequestInfo> queue =
                s_RequestRegistry.GetOrAdd(
                    requestKey,
                    _ => new ConcurrentQueue<ProbeRequestInfo>());
            queue.Enqueue(request);
        }

        private static void WorkerExecutePrefix(ref PathfindActionData actionData)
        {
            s_CurrentContext = null;
            s_PendingCandidate = null;

            if (!s_LoggedFirstWorkerExecuteInvocation)
            {
                s_LoggedFirstWorkerExecuteInvocation = true;
                Mod.log.Info(
                    $"PathfindQueueSystem.PathfindWorkerJob.Execute candidate probe invoked: " +
                    $"focusedDiagnostics={EnforcementLoggingPolicy.EnableFocusedRouteRebuildDiagnosticsLogging}, " +
                    $"watchedCount={FocusedLoggingService.WatchedVehicleCount}");
            }

            if (!EnforcementLoggingPolicy.ShouldLogFocusedRouteRebuildDiagnostics() ||
                !FocusedLoggingService.HasWatchedVehicles)
            {
                LogDisabledReasonIfNeeded("worker-execute");
                return;
            }

            string requestKey = BuildRequestKey(
                actionData.m_Parameters,
                actionData.m_StartTargets,
                actionData.m_EndTargets);
            if (!s_RequestRegistry.TryGetValue(requestKey, out ConcurrentQueue<ProbeRequestInfo> queue) ||
                !queue.TryDequeue(out ProbeRequestInfo request))
            {
                LogRequestMatchMiss(requestKey);
                return;
            }

            if (queue.IsEmpty)
            {
                s_RequestRegistry.TryRemove(requestKey, out _);
            }

            s_CurrentContext = new ProbeExecutionContext(request);
        }

        private static void WorkerExecutePostfix()
        {
            s_CurrentContext = null;
            s_PendingCandidate = null;
        }

        private static void AddHeapDataSinglePrefix(
            EdgeID id,
            PathfindEdge edge,
            EdgeFlags flags,
            RuleFlags rules,
            float baseCost,
            float costFactor,
            float2 edgeDelta,
            Bounds3 ___m_EndBounds,
            float ___m_HeuristicCostFactor)
        {
            CapturePendingCandidate(
                edge,
                id,
                default,
                hasNextEdge: false,
                flags,
                rules,
                baseCost,
                costFactor,
                edgeDelta,
                ___m_EndBounds,
                ___m_HeuristicCostFactor);
        }

        private static void AddHeapDataNextPrefix(
            EdgeID id,
            EdgeID id2,
            PathfindEdge edge,
            EdgeFlags flags,
            RuleFlags rules,
            float baseCost,
            float costFactor,
            float2 edgeDelta,
            UnsafePathfindData ___m_PathfindData,
            Bounds3 ___m_EndBounds,
            float ___m_HeuristicCostFactor)
        {
            long totalHits = System.Threading.Interlocked.Increment(ref s_TotalHits);
            if (System.Threading.Interlocked.CompareExchange(ref s_LiveFirstHitLogged, 1, 0) == 0)
            {
                Mod.log.Info($"{IntersectionProbePrefix} live firstHit=true");
            }

            if (totalHits > 0 && totalHits % LiveProbeHeartbeatInterval == 0)
            {
                Mod.log.Info($"{IntersectionProbePrefix} live totalHits={totalHits}");
            }

            ProbeExecutionContext currentContext = s_CurrentContext;
            if (currentContext != null)
            {
                System.Threading.Interlocked.Increment(ref s_AfterVehicleFilterHits);
            }

            CapturePendingCandidate(
                edge,
                id,
                id2,
                hasNextEdge: true,
                flags,
                rules,
                baseCost,
                costFactor,
                edgeDelta,
                ___m_EndBounds,
                ___m_HeuristicCostFactor);

            CaptureIntersectionTransitionSample(
                currentContext,
                id,
                id2,
                edge,
                ___m_PathfindData);

            MaybeLogIntersectionProbeSummary(force: false, totalHitsOverride: totalHits);
        }

        private static void CapturePendingCandidate(
            PathfindEdge edge,
            EdgeID id,
            EdgeID id2,
            bool hasNextEdge,
            EdgeFlags flags,
            RuleFlags rules,
            float baseCost,
            float costFactor,
            float2 edgeDelta,
            Bounds3 endBounds,
            float heuristicCostFactor)
        {
            if (s_CurrentContext == null)
            {
                return;
            }

            s_PendingCandidate = new PendingCandidate
            {
                EdgeOwner = edge.m_Owner,
                EdgeId = id,
                NextEdgeId = id2,
                HasNextEdge = hasNextEdge,
                Methods = edge.m_Specification.m_Methods,
                AccessRequirement = edge.m_Specification.m_AccessRequirement,
                Flags = flags,
                Rules = rules,
                EdgeDelta = edgeDelta,
                BaseCostBefore = baseCost,
                CostFactor = costFactor,
                Location = edge.m_Location,
                EndBounds = endBounds,
                HeuristicCostFactor = heuristicCostFactor,
            };
        }

        private static void CalculateCostPostfix(ref float __result)
        {
            if (s_CurrentContext == null || !s_PendingCandidate.HasValue)
            {
                return;
            }

            PendingCandidate pending = s_PendingCandidate.Value;
            s_PendingCandidate = null;

            float baseCostAfter = pending.BaseCostBefore + (__result * pending.CostFactor);
            float totalCost = CalculateTotalCost(
                pending.Location,
                baseCostAfter,
                pending.EdgeDelta.y,
                pending.EndBounds,
                pending.HeuristicCostFactor);

            s_CurrentContext.RecordCandidate(
                new CandidateInfo
                {
                    EdgeOwner = pending.EdgeOwner,
                    EdgeId = pending.EdgeId,
                    NextEdgeId = pending.NextEdgeId,
                    HasNextEdge = pending.HasNextEdge,
                    Methods = pending.Methods,
                    AccessRequirement = pending.AccessRequirement,
                    Flags = pending.Flags,
                    Rules = pending.Rules,
                    EdgeDelta = pending.EdgeDelta,
                    BaseCostBefore = pending.BaseCostBefore,
                    EdgeCostAdded = __result,
                    CostFactor = pending.CostFactor,
                    BaseCostAfter = baseCostAfter,
                    TotalCost = totalCost,
                });
        }

        private static void ReleasePostfix()
        {
            if (s_CurrentContext == null)
            {
                return;
            }

            Mod.log.Info(s_CurrentContext.BuildLogLine());
            s_CurrentContext = null;
            s_PendingCandidate = null;
        }

        private static void CaptureIntersectionTransitionSample(
            ProbeExecutionContext currentContext,
            EdgeID id,
            EdgeID id2,
            PathfindEdge edge,
            UnsafePathfindData pathfindData)
        {
            if (currentContext == null)
            {
                return;
            }

            if (id2.m_Index < 0 || id2.m_Index >= pathfindData.m_Edges.Length)
            {
                return;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                return;
            }

            EntityManager entityManager = world.EntityManager;
            Entity fromOwner = edge.m_Owner;
            Entity toOwner = pathfindData.m_Edges[id2.m_Index].m_Owner;
            if (fromOwner == Entity.Null || toOwner == Entity.Null)
            {
                return;
            }

            System.Threading.Interlocked.Increment(ref s_BothSidesHaveOwnerHits);

            bool hasFromCarLane = TryGetCarLane(entityManager, fromOwner, out CarLane fromCarLane);
            bool hasToCarLane = TryGetCarLane(entityManager, toOwner, out CarLane toCarLane);
            bool hasToConnectionLane = TryGetConnectionLane(entityManager, toOwner, out ConnectionLane toConnectionLane);
            if (!hasFromCarLane || !hasToCarLane)
            {
                return;
            }

            System.Threading.Interlocked.Increment(ref s_BothSidesHaveCarLaneHits);

            CarLaneFlags fromCarLaneFlags =
                hasFromCarLane
                    ? fromCarLane.m_Flags
                    : default;
            CarLaneFlags toCarLaneFlags =
                hasToCarLane
                    ? toCarLane.m_Flags
                    : default;
            ConnectionLaneFlags toConnectionLaneFlags =
                hasToConnectionLane
                    ? toConnectionLane.m_Flags
                    : default;

            bool sideConnection =
                hasToCarLane &&
                (toCarLaneFlags & CarLaneFlags.SideConnection) != 0;
            bool forbidden =
                hasToCarLane &&
                (toCarLaneFlags & CarLaneFlags.Forbidden) != 0;
            LaneMovement fromMovement = IntersectionMovementPolicy.GetMovement(fromCarLaneFlags);
            LaneMovement toMovement = IntersectionMovementPolicy.GetMovement(toCarLaneFlags);
            string turnHint = BuildTurnHint(toCarLaneFlags, toMovement);

            bool intersectionLike =
                hasToCarLane &&
                sideConnection &&
                (toMovement != LaneMovement.None ||
                 (toCarLaneFlags & CarLaneFlags.Approach) != 0);
            if (intersectionLike)
            {
                System.Threading.Interlocked.Increment(ref s_IntersectionLikeHits);
            }

            bool illegalLike =
                intersectionLike &&
                (forbidden ||
                 (fromMovement != LaneMovement.None &&
                  toMovement != LaneMovement.None &&
                  (fromMovement & toMovement) == LaneMovement.None));

            IntersectionTransitionSample sample =
                new IntersectionTransitionSample(
                    id,
                    id2,
                    fromOwner,
                    toOwner,
                    hasFromCarLane,
                    fromCarLaneFlags,
                    hasToCarLane,
                    toCarLaneFlags,
                    hasToConnectionLane,
                    toConnectionLaneFlags,
                    sideConnection,
                    forbidden,
                    turnHint,
                    intersectionLike,
                    illegalLike);
            currentContext.RecordIntersectionSample(sample);

            if (intersectionLike)
            {
                TryLogDetailedIntersectionLikeSample(currentContext.Request.Vehicle, sample);
            }
        }

        private static bool TryGetCarLane(
            EntityManager entityManager,
            Entity entity,
            out CarLane carLane)
        {
            carLane = default;
            if (entity == Entity.Null || !entityManager.Exists(entity) || !entityManager.HasComponent<CarLane>(entity))
            {
                return false;
            }

            carLane = entityManager.GetComponentData<CarLane>(entity);
            return true;
        }

        private static bool TryGetConnectionLane(
            EntityManager entityManager,
            Entity entity,
            out ConnectionLane connectionLane)
        {
            connectionLane = default;
            if (entity == Entity.Null || !entityManager.Exists(entity) || !entityManager.HasComponent<ConnectionLane>(entity))
            {
                return false;
            }

            connectionLane = entityManager.GetComponentData<ConnectionLane>(entity);
            return true;
        }

        private static string BuildTurnHint(
            CarLaneFlags flags,
            LaneMovement movement)
        {
            string movementText = IntersectionMovementPolicy.FormatMovement(movement);
            bool approach = (flags & CarLaneFlags.Approach) != 0;
            if (!approach)
            {
                return movementText;
            }

            return movement == LaneMovement.None
                ? "approach"
                : movementText + "+approach";
        }

        private static string FormatCarLaneFlags(bool hasCarLane, CarLaneFlags flags)
        {
            return hasCarLane ? flags.ToString() : "(no CarLane)";
        }

        private static string FormatConnectionLaneFlags(bool hasConnectionLane, ConnectionLaneFlags flags)
        {
            return hasConnectionLane ? flags.ToString() : "(no ConnectionLane)";
        }

        private static void ResetIntersectionProbeDiagnostics()
        {
            s_DetailedIntersectionLikeLogCount = 0;
            s_DisabledFocusedDiagnosticsLogged = 0;
            s_DisabledNoWatchedVehiclesLogged = 0;
            s_LiveFirstHitLogged = 0;
            s_TotalHits = 0;
            s_AfterVehicleFilterHits = 0;
            s_BothSidesHaveOwnerHits = 0;
            s_BothSidesHaveCarLaneHits = 0;
            s_IntersectionLikeHits = 0;
            s_LastSummaryLoggedAtTotalHits = -1;
        }

        private static void TryLogDetailedIntersectionLikeSample(
            Entity vehicle,
            IntersectionTransitionSample sample)
        {
            int logIndex = System.Threading.Interlocked.Increment(ref s_DetailedIntersectionLikeLogCount);
            if (logIndex > MaxDetailedIntersectionLikeLogs)
            {
                return;
            }

            Mod.log.Info(sample.BuildLogLine(vehicle));
        }

        private static void MaybeLogIntersectionProbeSummary(bool force, long? totalHitsOverride = null)
        {
            long totalHits = totalHitsOverride ?? System.Threading.Interlocked.Read(ref s_TotalHits);
            if (!force &&
                totalHits != 1 &&
                totalHits != 64 &&
                (totalHits == 0 || totalHits % 4096 != 0))
            {
                return;
            }

            if (!force)
            {
                long previous = System.Threading.Interlocked.Exchange(ref s_LastSummaryLoggedAtTotalHits, totalHits);
                if (previous == totalHits)
                {
                    return;
                }
            }

            Mod.log.Info(BuildIntersectionProbeSummaryLine());
        }

        private static string BuildIntersectionProbeSummaryLine()
        {
            return
                $"{IntersectionProbePrefix} summary " +
                $"totalHits={System.Threading.Interlocked.Read(ref s_TotalHits)} " +
                $"afterVehicleFilter={System.Threading.Interlocked.Read(ref s_AfterVehicleFilterHits)} " +
                $"bothSidesHaveOwner={System.Threading.Interlocked.Read(ref s_BothSidesHaveOwnerHits)} " +
                $"bothSidesHaveCarLane={System.Threading.Interlocked.Read(ref s_BothSidesHaveCarLaneHits)} " +
                $"intersectionLike={System.Threading.Interlocked.Read(ref s_IntersectionLikeHits)}";
        }

        private static float CalculateTotalCost(
            LocationSpecification location,
            float baseCost,
            float endDelta,
            Bounds3 endBounds,
            float heuristicCostFactor)
        {
            float3 position = MathUtils.Position(location.m_Line, endDelta);
            float3 delta = math.max(endBounds.min - position, position - endBounds.max);
            return baseCost + math.length(math.max(delta, 0f)) * heuristicCostFactor;
        }

        private static string BuildRequestKey(
            PathfindParameters parameters,
            UnsafeList<PathTarget> startTargets,
            UnsafeList<PathTarget> endTargets)
        {
            StringBuilder builder = new StringBuilder(256);
            builder.Append("m=").Append((int)parameters.m_Methods);
            builder.Append("|pf=").Append((int)parameters.m_PathfindFlags);
            builder.Append("|ir=").Append((int)parameters.m_IgnoredRules);
            builder.Append("|tir=").Append((int)parameters.m_TaxiIgnoredRules);
            builder.Append("|park=").Append(FormatEntityForKey(parameters.m_ParkingTarget));
            builder.Append("|w=").Append(parameters.m_Weights.m_Value.x.ToString("0.###")).Append(',')
                .Append(parameters.m_Weights.m_Value.y.ToString("0.###")).Append(',')
                .Append(parameters.m_Weights.m_Value.z.ToString("0.###")).Append(',')
                .Append(parameters.m_Weights.m_Value.w.ToString("0.###"));
            builder.Append("|s=").Append(BuildTargetPreview(startTargets));
            builder.Append("|e=").Append(BuildTargetPreview(endTargets));
            return builder.ToString();
        }

        private static string BuildTargetPreview(UnsafeList<PathTarget> targets, int maxTargets = 4)
        {
            if (!targets.IsCreated || targets.Length == 0)
            {
                return "none";
            }

            int count = math.min(targets.Length, maxTargets);
            StringBuilder builder = new StringBuilder(128);
            builder.Append('[').Append(targets.Length).Append("] ");
            for (int index = 0; index < count; index += 1)
            {
                PathTarget target = targets[index];
                if (index != 0)
                {
                    builder.Append("; ");
                }

                builder.Append(FormatEntityForKey(target.m_Target))
                    .Append("->")
                    .Append(FormatEntityForKey(target.m_Entity))
                    .Append('@')
                    .Append(target.m_Delta.ToString("0.###"))
                    .Append('/')
                    .Append((int)target.m_Flags);
            }

            if (targets.Length > count)
            {
                builder.Append("; ...");
            }

            return builder.ToString();
        }

        private static string FormatEntityForKey(Entity entity)
        {
            return entity == Entity.Null
                ? "null"
                : entity.Index.ToString() + ":" + entity.Version.ToString();
        }

        private static void LogRequestMatchMiss(string requestKey)
        {
            int missCount = System.Threading.Interlocked.Increment(ref s_RequestMatchMissLogCount);
            if (missCount > 5)
            {
                return;
            }

            StringBuilder registeredKeys = new StringBuilder(96);
            int sampleCount = 0;
            foreach (string key in s_RequestRegistry.Keys)
            {
                if (sampleCount >= 3)
                {
                    break;
                }

                if (sampleCount > 0)
                {
                    registeredKeys.Append(" || ");
                }

                registeredKeys.Append(key);
                sampleCount += 1;
            }

            Mod.log.Info(
                $"FOCUSED_ROUTE_CANDIDATE_MISS: requestKey={requestKey}, " +
                $"registeredKeyCount={s_RequestRegistry.Count}, " +
                $"registeredSample={(registeredKeys.Length == 0 ? "none" : registeredKeys.ToString())}");
        }

        private static void LogInstallFailed(string stage, string reason)
        {
            Mod.log.Info(
                $"{IntersectionProbePrefix} install failed stage={stage} reason={reason}");
        }

        private static void LogAddHeapDataCandidates()
        {
            for (int index = 0; index < s_AddHeapDataMethods.Length; index += 1)
            {
                MethodInfo candidate = s_AddHeapDataMethods[index];
                Mod.log.Info(
                    $"{IntersectionProbePrefix} candidate[{index}]={DescribeMethod(candidate)}");
            }
        }

        private static void LogAddHeapDataPatchInfo(MethodInfo targetMethod)
        {
            Patches patchInfo = Harmony.GetPatchInfo(targetMethod);
            Mod.log.Info(
                $"{IntersectionProbePrefix} patchInfo " +
                $"target={DescribeMethod(targetMethod)} " +
                $"prefixes={GetPatchCount(patchInfo?.Prefixes)} " +
                $"postfixes={GetPatchCount(patchInfo?.Postfixes)} " +
                $"transpilers={GetPatchCount(patchInfo?.Transpilers)} " +
                $"finalizers={GetPatchCount(patchInfo?.Finalizers)} " +
                $"owners={FormatPatchOwners(patchInfo)} " +
                $"prefixMethods={FormatPatchMethods(patchInfo?.Prefixes)} " +
                $"postfixMethods={FormatPatchMethods(patchInfo?.Postfixes)}");
        }

        private static void LogProbeRuntimeState(string phase)
        {
            bool requested = EnforcementLoggingPolicy.EnableFocusedRouteRebuildDiagnosticsLogging;
            bool effective = EnforcementLoggingPolicy.ShouldLogFocusedRouteRebuildDiagnostics();
            bool hasWatchedVehicles = FocusedLoggingService.HasWatchedVehicles;
            Mod.log.Info(
                $"{IntersectionProbePrefix} state " +
                $"phase={phase} " +
                $"requested={requested} " +
                $"effective={effective} " +
                $"watchedCount={FocusedLoggingService.WatchedVehicleCount} " +
                $"hasWatchedVehicles={hasWatchedVehicles} " +
                $"requestRegistryCount={s_RequestRegistry.Count} " +
                $"isApplied={(s_Harmony != null)}");
        }

        private static void LogDisabledReasonIfNeeded(string source)
        {
            if (!EnforcementLoggingPolicy.ShouldLogFocusedRouteRebuildDiagnostics() &&
                System.Threading.Interlocked.CompareExchange(ref s_DisabledFocusedDiagnosticsLogged, 1, 0) == 0)
            {
                Mod.log.Info(
                    $"{IntersectionProbePrefix} disabled reason=focused-route-diagnostics=false source={source}");
            }

            if (!FocusedLoggingService.HasWatchedVehicles &&
                System.Threading.Interlocked.CompareExchange(ref s_DisabledNoWatchedVehiclesLogged, 1, 0) == 0)
            {
                Mod.log.Info(
                    $"{IntersectionProbePrefix} disabled reason=no-watched-vehicles source={source}");
            }
        }

        private static MethodInfo FindExactIntersectionAddHeapDataMethod()
        {
            if (s_PathfindExecutorType == null)
            {
                return null;
            }

            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(s_PathfindExecutorType))
            {
                if (!IsExactIntersectionAddHeapDataMethod(method))
                {
                    continue;
                }

                return method;
            }

            return null;
        }

        private static bool IsExactIntersectionAddHeapDataMethod(MethodInfo method)
        {
            if (method == null ||
                method.IsStatic ||
                method.ReturnType != typeof(void) ||
                !string.Equals(method.Name, "AddHeapData", StringComparison.Ordinal))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 11)
            {
                return false;
            }

            return parameters[0].ParameterType == typeof(int) &&
                parameters[1].ParameterType == typeof(EdgeID) &&
                parameters[2].ParameterType == typeof(EdgeID) &&
                parameters[3].ParameterType == typeof(PathfindEdge).MakeByRefType() &&
                parameters[4].ParameterType == typeof(EdgeFlags) &&
                parameters[5].ParameterType == typeof(RuleFlags) &&
                parameters[6].ParameterType == typeof(float) &&
                parameters[7].ParameterType == typeof(float) &&
                parameters[8].ParameterType.Name == "FullNode" &&
                parameters[9].ParameterType == typeof(float2) &&
                parameters[10].ParameterType.Name == "PathfindItemFlags";
        }

        private static string DescribeMethod(MethodInfo method)
        {
            if (method == null)
            {
                return "null";
            }

            ParameterInfo[] parameters = method.GetParameters();
            StringBuilder parameterList = new StringBuilder(parameters.Length * 24);
            for (int index = 0; index < parameters.Length; index += 1)
            {
                if (index > 0)
                {
                    parameterList.Append(", ");
                }

                ParameterInfo parameter = parameters[index];
                parameterList.Append(parameter.ParameterType.Name);
                parameterList.Append(' ');
                parameterList.Append(parameter.Name);
            }

            return $"{method.DeclaringType?.FullName}.{method.Name}({parameterList})";
        }

        private static int GetPatchCount<T>(ICollection<T> patches)
        {
            return patches?.Count ?? 0;
        }

        private static string FormatPatchOwners(Patches patchInfo)
        {
            if (patchInfo?.Owners == null || patchInfo.Owners.Count == 0)
            {
                return "none";
            }

            return string.Join(",", patchInfo.Owners);
        }

        private static string FormatPatchMethods(ICollection<Patch> patches)
        {
            if (patches == null || patches.Count == 0)
            {
                return "none";
            }

            StringBuilder builder = new StringBuilder(patches.Count * 48);
            int index = 0;
            foreach (Patch patch in patches)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                MethodInfo patchMethod = patch?.PatchMethod;
                builder.Append(
                    patchMethod == null
                        ? "null"
                        : $"{patchMethod.DeclaringType?.FullName}.{patchMethod.Name}");
                index += 1;
            }

            return builder.ToString();
        }
    }
}
