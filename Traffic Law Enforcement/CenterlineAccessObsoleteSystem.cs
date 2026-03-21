using System.Collections.Generic;
using Game;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;
using PrefabRef = Game.Prefabs.PrefabRef;
using PrefabSystem = Game.Prefabs.PrefabSystem;

namespace Traffic_Law_Enforcement
{
    public partial class CenterlineAccessObsoleteSystem : GameSystemBase
    {
        private const int MaxStructureSampleLogs = 64;
        private const int ContextSummaryLogInterval = 512;
        private const byte EvaluationNone = 0;
        private const byte EvaluationNoAccessTransition = 1;
        private const byte EvaluationCleanAccessTransition = 2;
        private const byte EvaluationInvalidatedAccessTransition = 3;
        private const byte TransitionFamilyNone = 0;
        private const byte TransitionFamilyParkingLaneIngress = 1;
        private const byte TransitionFamilyGarageLaneIngress = 2;
        private const byte TransitionFamilyParkingConnectionIngress = 3;
        private const byte TransitionFamilyBuildingServiceIngress = 4;
        private const byte TransitionFamilyIllegalEgress = 5;

        private EntityQuery m_VehicleQuery;
        private EntityQuery m_CurrentLaneChangedQuery;
        private EntityQuery m_NavigationLaneChangedQuery;
        private EntityQuery m_AccessOriginWatchQuery;
        private BufferLookup<CarNavigationLane> m_NavigationLaneData;
        private ComponentLookup<CarCurrentLane> m_CurrentLaneData;
        private ComponentLookup<PathOwner> m_PathOwnerData;
        private ComponentLookup<Owner> m_OwnerData;
        private ComponentLookup<PrefabRef> m_PrefabRefData;
        private ComponentLookup<Car> m_CarData;
        private ComponentLookup<CenterlineAccessOriginWatch> m_AccessOriginWatchData;
        private ComponentLookup<CenterlineAccessObsoleteState> m_ObsoleteStateData;
        private ComponentLookup<CarLane> m_CarLaneData;
        private ComponentLookup<EdgeLane> m_EdgeLaneData;
        private ComponentLookup<ParkingLane> m_ParkingLaneData;
        private ComponentLookup<GarageLane> m_GarageLaneData;
        private ComponentLookup<ConnectionLane> m_ConnectionLaneData;
        private readonly HashSet<Entity> m_CandidateVehicles = new HashSet<Entity>();
        private readonly HashSet<string> m_StructureSampleSignatures = new HashSet<string>();
        private PrefabSystem m_PrefabSystem;
        private int m_TotalInvalidationCount;
        private int m_CustomCurrentContextInvalidationCount;
        private int m_PedestrianStreetCurrentContextInvalidationCount;
        private int m_MediumRoadCurrentContextInvalidationCount;
        private int m_PublicTransportLaneCurrentContextInvalidationCount;
        private int m_CustomPedestrianOrPublicTransportCurrentContextInvalidationCount;
        private int m_LastContextSummaryLoggedTotal;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_VehicleQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadOnly<PathOwner>(),
                ComponentType.ReadOnly<CarNavigationLane>());
            m_CurrentLaneChangedQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadOnly<PathOwner>(),
                ComponentType.ReadOnly<CarNavigationLane>());
            m_CurrentLaneChangedQuery.SetChangedVersionFilter(ComponentType.ReadOnly<CarCurrentLane>());
            m_NavigationLaneChangedQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadOnly<PathOwner>(),
                ComponentType.ReadOnly<CarNavigationLane>());
            m_NavigationLaneChangedQuery.SetChangedVersionFilter(ComponentType.ReadOnly<CarNavigationLane>());
            m_AccessOriginWatchQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadOnly<PathOwner>(),
                ComponentType.ReadOnly<CarNavigationLane>(),
                ComponentType.ReadOnly<CenterlineAccessOriginWatch>());
            m_NavigationLaneData = GetBufferLookup<CarNavigationLane>(true);
            m_CurrentLaneData = GetComponentLookup<CarCurrentLane>(true);
            m_PathOwnerData = GetComponentLookup<PathOwner>(true);
            m_OwnerData = GetComponentLookup<Owner>(true);
            m_PrefabRefData = GetComponentLookup<PrefabRef>(true);
            m_CarData = GetComponentLookup<Car>(true);
            m_AccessOriginWatchData = GetComponentLookup<CenterlineAccessOriginWatch>();
            m_ObsoleteStateData = GetComponentLookup<CenterlineAccessObsoleteState>();
            m_CarLaneData = GetComponentLookup<CarLane>(true);
            m_EdgeLaneData = GetComponentLookup<EdgeLane>(true);
            m_ParkingLaneData = GetComponentLookup<ParkingLane>(true);
            m_GarageLaneData = GetComponentLookup<GarageLane>(true);
            m_ConnectionLaneData = GetComponentLookup<ConnectionLane>(true);
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            RequireForUpdate(m_VehicleQuery);
        }

        protected override void OnUpdate()
        {
            if (!Mod.IsMidBlockCrossingEnforcementEnabled)
            {
                return;
            }

            m_NavigationLaneData.Update(this);
            m_CurrentLaneData.Update(this);
            m_PathOwnerData.Update(this);
            m_OwnerData.Update(this);
            m_PrefabRefData.Update(this);
            m_CarData.Update(this);
            m_AccessOriginWatchData.Update(this);
            m_ObsoleteStateData.Update(this);
            m_CarLaneData.Update(this);
            m_EdgeLaneData.Update(this);
            m_ParkingLaneData.Update(this);
            m_GarageLaneData.Update(this);
            m_ConnectionLaneData.Update(this);

            m_CandidateVehicles.Clear();
            CollectCandidateVehicles(m_CurrentLaneChangedQuery);
            CollectCandidateVehicles(m_NavigationLaneChangedQuery);
            CollectCandidateVehicles(m_AccessOriginWatchQuery);

            try
            {
                foreach (Entity vehicle in m_CandidateVehicles)
                {
                    if (!m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLane) ||
                        !m_PathOwnerData.TryGetComponent(vehicle, out PathOwner pathOwner) ||
                        !m_NavigationLaneData.TryGetBuffer(vehicle, out DynamicBuffer<CarNavigationLane> navigationLanes))
                    {
                        continue;
                    }

                    // Exclude emergency vehicles
                    if (EmergencyVehiclePolicy.IsEmergencyVehicle(m_CarData[vehicle]))
                    {
                        continue;
                    }

                    // Exclude vehicles allowed on PT lanes
                    if ((m_CarData[vehicle].m_Flags & CarFlags.UsePublicTransportLanes) != 0)
                    {
                        continue;
                    }

                    SyncAccessOriginWatch(vehicle, currentLane.m_Lane);

                    ResetDuplicateSuppressionIfPathChanged(vehicle, pathOwner);

                    if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Obsolete)) != 0)
                    {
                        continue;
                    }

                    if (navigationLanes.Length == 0)
                    {
                        continue;
                    }

                    if (!TryGetFirstPlannedAccessTransition(currentLane.m_Lane, navigationLanes, out Entity sourceLane, out Entity targetLane, out int transitionIndex, out string transitionKind))
                    {
                        if (!ShouldSuppressObservedSnapshot(vehicle, currentLane.m_Lane, Entity.Null, Entity.Null, -1, EvaluationNoAccessTransition, TransitionFamilyNone))
                        {
                            RecordObservedSnapshot(vehicle, currentLane.m_Lane, Entity.Null, Entity.Null, -1, EvaluationNoAccessTransition, TransitionFamilyNone);
                        }

                        continue;
                    }

                    bool illegalTransition = IsIllegalIngress(sourceLane, targetLane, out string reason) || IsIllegalEgress(sourceLane, targetLane, out reason);
                    byte evaluationResult = illegalTransition ? EvaluationInvalidatedAccessTransition : EvaluationCleanAccessTransition;
                    byte transitionFamily = GetTransitionFamily(sourceLane, targetLane, evaluationResult);
                    if (ShouldSuppressObservedSnapshot(vehicle, currentLane.m_Lane, sourceLane, targetLane, transitionIndex, evaluationResult, transitionFamily))
                    {
                        continue;
                    }

                    if (!illegalTransition)
                    {
                        RecordObservedSnapshot(vehicle, currentLane.m_Lane, sourceLane, targetLane, transitionIndex, evaluationResult, transitionFamily);
                        continue;
                    }

                    pathOwner.m_State |= PathFlags.Obsolete;
                    EntityManager.SetComponentData(vehicle, pathOwner);
                    RecordObservedSnapshot(vehicle, currentLane.m_Lane, sourceLane, targetLane, transitionIndex, evaluationResult, transitionFamily);
                    RecordInvalidationContext(currentLane.m_Lane);

                    if (EnforcementLoggingPolicy.ShouldLogEnforcementEvents())
                    {
                        Mod.log.Info($"Planned center-line access route invalidated: vehicle={vehicle}, fromLane={sourceLane}, toLane={targetLane}, accessIndex={transitionIndex}, transition={transitionKind}, reason={reason}");
                        LogStructureSample(vehicle, currentLane.m_Lane, sourceLane, targetLane, transitionIndex, transitionKind, reason);
                        LogInvalidationContextSummaryIfNeeded();
                    }
                }
            }
            finally
            {
            }
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

        private void SyncAccessOriginWatch(Entity vehicle, Entity currentLane)
        {
            bool shouldWatch = IsAccessOrigin(currentLane);
            bool isWatching = m_AccessOriginWatchData.HasComponent(vehicle);

            if (shouldWatch == isWatching)
            {
                return;
            }

            if (shouldWatch)
            {
                EntityManager.AddComponent<CenterlineAccessOriginWatch>(vehicle);
                return;
            }

            EntityManager.RemoveComponent<CenterlineAccessOriginWatch>(vehicle);
        }

        private bool TryGetFirstPlannedAccessTransition(Entity currentLane, DynamicBuffer<CarNavigationLane> navigationLanes, out Entity sourceLane, out Entity targetLane, out int transitionIndex, out string transitionKind)
        {
            sourceLane = currentLane;
            targetLane = Entity.Null;
            transitionIndex = -1;
            transitionKind = null;

            for (int index = 0; index < navigationLanes.Length; index++)
            {
                Entity nextLane = navigationLanes[index].m_Lane;
                if (nextLane == Entity.Null || nextLane == sourceLane)
                {
                    continue;
                }

                if (!IsAccessTransition(sourceLane, nextLane))
                {
                    sourceLane = nextLane;
                    continue;
                }

                targetLane = nextLane;
                transitionIndex = index;
                transitionKind = DescribeTransitionKind(sourceLane, nextLane);
                return true;
            }

            return false;
        }

        private void LogStructureSample(Entity vehicle, Entity currentLane, Entity sourceLane, Entity targetLane, int transitionIndex, string transitionKind, string reason)
        {
            if (m_StructureSampleSignatures.Count >= MaxStructureSampleLogs)
            {
                return;
            }

            string currentLaneShape = DescribeLaneShape(currentLane);
            string sourceLaneShape = DescribeLaneShape(sourceLane);
            string targetLaneShape = DescribeLaneShape(targetLane);
            string currentOwnerChain = DescribeOwnerChain(currentLane);
            string sourceOwnerChain = DescribeOwnerChain(sourceLane);
            string targetOwnerChain = DescribeOwnerChain(targetLane);
            Entity accessLane = IsAccessOrigin(sourceLane) ? sourceLane : targetLane;
            Entity roadLane = accessLane == sourceLane ? targetLane : sourceLane;
            string accessLaneShape = DescribeLaneShape(accessLane);
            string roadLaneShape = DescribeLaneShape(roadLane);
            string accessOwnerChain = DescribeOwnerChain(accessLane);
            string roadOwnerChain = DescribeOwnerChain(roadLane);
            string signature = $"{transitionKind}|{currentLaneShape}|{sourceLaneShape}|{targetLaneShape}|{currentOwnerChain}|{sourceOwnerChain}|{targetOwnerChain}";
            if (!m_StructureSampleSignatures.Add(signature))
            {
                return;
            }
        }

        private void RecordInvalidationContext(Entity currentLane)
        {
            m_TotalInvalidationCount += 1;

            bool customCurrentContext = CurrentContextContainsPrefab(currentLane, IsWorkshopStyleCustomPrefabName);
            bool pedestrianStreetCurrentContext = CurrentContextContainsPrefab(currentLane, IsPedestrianStreetPrefabName);
            bool mediumRoadCurrentContext = CurrentContextContainsPrefab(currentLane, IsMediumRoadPrefabName);
            bool publicTransportLaneCurrentContext = IsPublicTransportLaneContext(currentLane);

            if (customCurrentContext)
            {
                m_CustomCurrentContextInvalidationCount += 1;
            }

            if (pedestrianStreetCurrentContext)
            {
                m_PedestrianStreetCurrentContextInvalidationCount += 1;
            }

            if (mediumRoadCurrentContext)
            {
                m_MediumRoadCurrentContextInvalidationCount += 1;
            }

            if (publicTransportLaneCurrentContext)
            {
                m_PublicTransportLaneCurrentContextInvalidationCount += 1;
            }

            if (customCurrentContext || pedestrianStreetCurrentContext || publicTransportLaneCurrentContext)
            {
                m_CustomPedestrianOrPublicTransportCurrentContextInvalidationCount += 1;
            }
        }

        private void LogInvalidationContextSummaryIfNeeded()
        {
            if (m_TotalInvalidationCount == 0 || m_TotalInvalidationCount - m_LastContextSummaryLoggedTotal < ContextSummaryLogInterval)
            {
                return;
            }

            m_LastContextSummaryLoggedTotal = m_TotalInvalidationCount;
        }

        private string DescribeLaneShape(Entity lane)
        {
            if (lane == Entity.Null)
            {
                return "null";
            }

            List<string> parts = new List<string>(6);
            if (m_EdgeLaneData.HasComponent(lane))
            {
                parts.Add("edge");
            }

            if (m_CarLaneData.TryGetComponent(lane, out CarLane carLane))
            {
                parts.Add($"car(flags={carLane.m_Flags})");
            }

            if (m_ConnectionLaneData.TryGetComponent(lane, out ConnectionLane connectionLane))
            {
                parts.Add($"connection(flags={connectionLane.m_Flags})");
            }

            if (m_ParkingLaneData.HasComponent(lane))
            {
                parts.Add("parking");
            }

            if (m_GarageLaneData.HasComponent(lane))
            {
                parts.Add("garage");
            }

            string prefabName = TryGetPrefabName(lane);
            if (prefabName != null)
            {
                parts.Add($"prefab={prefabName}");
            }

            return parts.Count == 0 ? "other" : string.Join(", ", parts);
        }

        private string DescribeOwnerChain(Entity entity)
        {
            if (entity == Entity.Null)
            {
                return "null";
            }

            List<string> parts = new List<string>(8);
            Entity current = entity;
            for (int depth = 0; depth < 8 && current != Entity.Null; depth += 1)
            {
                string prefabName = TryGetPrefabName(current);
                bool roadBuilderPrefab = IsRoadBuilderPrefabName(prefabName);
                parts.Add(prefabName != null
                    ? $"{current}[prefab={prefabName}, roadBuilder={roadBuilderPrefab}]"
                    : current.ToString());

                if (!m_OwnerData.TryGetComponent(current, out Owner owner) || owner.m_Owner == Entity.Null || owner.m_Owner == current)
                {
                    break;
                }

                current = owner.m_Owner;
            }

            return string.Join(" -> ", parts);
        }

        private string TryGetPrefabName(Entity entity)
        {
            if (!m_PrefabRefData.TryGetComponent(entity, out PrefabRef prefabRef) || prefabRef.m_Prefab == Entity.Null)
            {
                return null;
            }

            return m_PrefabSystem != null
                ? m_PrefabSystem.GetPrefabName(prefabRef.m_Prefab)
                : prefabRef.m_Prefab.ToString();
        }

        private bool CurrentContextContainsPrefab(Entity entity, System.Func<string, bool> match)
        {
            if (entity == Entity.Null)
            {
                return false;
            }

            Entity current = entity;
            for (int depth = 0; depth < 8 && current != Entity.Null; depth += 1)
            {
                if (match(TryGetPrefabName(current)))
                {
                    return true;
                }

                if (!m_OwnerData.TryGetComponent(current, out Owner owner) || owner.m_Owner == Entity.Null || owner.m_Owner == current)
                {
                    break;
                }

                current = owner.m_Owner;
            }

            return false;
        }

        private bool IsPublicTransportLaneContext(Entity currentLane)
        {
            if (TryGetPrefabName(currentLane) == "Public Transport Lane 3 - Tram")
            {
                return true;
            }

            return CurrentContextContainsPrefab(currentLane, IsPublicTransportRoadPrefabName);
        }

        private static bool IsRoadBuilderPrefabName(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                return false;
            }

            return prefabName.IndexOf("Road Builder", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                prefabName.IndexOf("RB ", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                prefabName.IndexOf("Made with Road Builder", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsWorkshopStyleCustomPrefabName(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                return false;
            }

            return prefabName.Length > 17 &&
                prefabName[0] == 'r' &&
                prefabName.EndsWith("-76561198005833464", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPedestrianStreetPrefabName(string prefabName)
        {
            return string.Equals(prefabName, "Pedestrian Street", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMediumRoadPrefabName(string prefabName)
        {
            return string.Equals(prefabName, "Medium Road", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPublicTransportRoadPrefabName(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                return false;
            }

            return prefabName.IndexOf("Public Transport", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                prefabName.IndexOf("Tram", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatPercent(int numerator, int denominator)
        {
            if (denominator <= 0)
            {
                return "0.0%";
            }

            return ((100.0 * numerator) / denominator).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%";
        }

        private void ResetDuplicateSuppressionIfPathChanged(Entity vehicle, PathOwner pathOwner)
        {
            if (!m_ObsoleteStateData.TryGetComponent(vehicle, out CenterlineAccessObsoleteState state) || state.m_AwaitingPathRefresh == 0)
            {
                return;
            }

            if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Updated)) == 0)
            {
                return;
            }

            state.m_AwaitingPathRefresh = 0;
            m_ObsoleteStateData[vehicle] = state;
        }

        private bool ShouldSuppressObservedSnapshot(Entity vehicle, Entity currentLane, Entity sourceLane, Entity targetLane, int transitionIndex, byte evaluationResult, byte transitionFamily)
        {
            if (!m_ObsoleteStateData.TryGetComponent(vehicle, out CenterlineAccessObsoleteState state))
            {
                return false;
            }

            if (evaluationResult == EvaluationInvalidatedAccessTransition)
            {
                bool sameInvalidationFamily = state.m_LastEvaluationResult == EvaluationInvalidatedAccessTransition &&
                    state.m_LastTransitionFamily == transitionFamily &&
                    transitionFamily != TransitionFamilyNone &&
                    (state.m_LastSourceLane == sourceLane || state.m_LastCurrentLane == currentLane);

                if (!sameInvalidationFamily)
                {
                    return false;
                }

                return state.m_AwaitingPathRefresh != 0;
            }

            bool sameSnapshot = state.m_LastCurrentLane == currentLane &&
                state.m_LastSourceLane == sourceLane &&
                state.m_LastTargetLane == targetLane &&
                state.m_LastAccessIndex == transitionIndex &&
                state.m_LastEvaluationResult == evaluationResult;

            if (!sameSnapshot)
            {
                return false;
            }

            return true;
        }

        private void RecordObservedSnapshot(Entity vehicle, Entity currentLane, Entity sourceLane, Entity targetLane, int transitionIndex, byte evaluationResult, byte transitionFamily)
        {
            CenterlineAccessObsoleteState state = m_ObsoleteStateData.TryGetComponent(vehicle, out CenterlineAccessObsoleteState existingState)
                ? existingState
                : default;

            state.m_LastCurrentLane = currentLane;
            state.m_LastSourceLane = sourceLane;
            state.m_LastTargetLane = targetLane;
            state.m_LastAccessIndex = transitionIndex;
            state.m_LastEvaluationResult = evaluationResult;
            state.m_LastTransitionFamily = transitionFamily;
            state.m_AwaitingPathRefresh = evaluationResult == EvaluationInvalidatedAccessTransition ? (byte)1 : (byte)0;

            if (m_ObsoleteStateData.HasComponent(vehicle))
            {
                m_ObsoleteStateData[vehicle] = state;
            }
            else
            {
                EntityManager.AddComponentData(vehicle, state);
            }
        }

        private bool IsAccessTransition(Entity sourceLane, Entity targetLane)
        {
            return IsAccessOrigin(sourceLane) || IsAccessTarget(targetLane);
        }

        private byte GetTransitionFamily(Entity sourceLane, Entity targetLane, byte evaluationResult)
        {
            if (evaluationResult != EvaluationInvalidatedAccessTransition)
            {
                return TransitionFamilyNone;
            }

            if (IsAccessOrigin(sourceLane))
            {
                return TransitionFamilyIllegalEgress;
            }

            if (m_ParkingLaneData.HasComponent(targetLane))
            {
                return TransitionFamilyParkingLaneIngress;
            }

            if (m_GarageLaneData.HasComponent(targetLane))
            {
                return TransitionFamilyGarageLaneIngress;
            }

            if (!m_ConnectionLaneData.TryGetComponent(targetLane, out ConnectionLane connectionLane))
            {
                return TransitionFamilyNone;
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
            {
                return TransitionFamilyParkingConnectionIngress;
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0)
            {
                return TransitionFamilyBuildingServiceIngress;
            }

            return TransitionFamilyNone;
        }

        private string DescribeTransitionKind(Entity sourceLane, Entity targetLane)
        {
            if (IsAccessOrigin(sourceLane))
            {
                return $"egress:{DescribeAccessOrigin(sourceLane)}";
            }

            if (m_ParkingLaneData.HasComponent(targetLane))
            {
                return "ingress:parking-lane";
            }

            if (m_GarageLaneData.HasComponent(targetLane))
            {
                return "ingress:garage-lane";
            }

            if (!m_ConnectionLaneData.TryGetComponent(targetLane, out ConnectionLane connectionLane))
            {
                return "ingress:other";
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
            {
                return "ingress:parking-connection";
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0)
            {
                return "ingress:building-service-access-connection";
            }

            return "ingress:road-connection";
        }

        private bool IsAccessTarget(Entity lane)
        {
            if (m_ParkingLaneData.HasComponent(lane) || m_GarageLaneData.HasComponent(lane))
            {
                return true;
            }

            if (!m_ConnectionLaneData.TryGetComponent(lane, out ConnectionLane connectionLane))
            {
                return false;
            }

            bool parkingAccess = (connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0;
            bool roadConnection = (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0;
            return parkingAccess || !roadConnection;
        }

        private bool IsIllegalIngress(Entity sourceLane, Entity targetLane, out string reason)
        {
            reason = null;
            if (!TryGetRoadCarLane(sourceLane, out CarLane sourceCarLane) || LaneAllowsSideAccess(sourceCarLane))
            {
                return false;
            }

            if (m_ParkingLaneData.HasComponent(targetLane))
            {
                reason = "planned parking-access ingress from a lane without side-access permission";
                return true;
            }

            if (m_GarageLaneData.HasComponent(targetLane))
            {
                reason = "planned garage-access ingress from a lane without side-access permission";
                return true;
            }

            if (!m_ConnectionLaneData.TryGetComponent(targetLane, out ConnectionLane connectionLane))
            {
                return false;
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
            {
                reason = "planned parking-connection ingress from a lane without side-access permission";
                return true;
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0)
            {
                reason = "planned building/service access ingress from a lane without side-access permission";
                return true;
            }

            return false;
        }

        private bool IsIllegalEgress(Entity sourceLane, Entity targetLane, out string reason)
        {
            reason = null;
            if (!IsAccessOrigin(sourceLane) || !TryGetRoadCarLane(targetLane, out CarLane targetCarLane) || LaneAllowsSideAccess(targetCarLane))
            {
                return false;
            }

            reason = $"planned illegal egress from {DescribeAccessOrigin(sourceLane)} into a lane without side-access permission";
            return true;
        }

        private bool IsAccessOrigin(Entity lane)
        {
            if (m_ParkingLaneData.HasComponent(lane) || m_GarageLaneData.HasComponent(lane))
            {
                return true;
            }

            if (!m_ConnectionLaneData.TryGetComponent(lane, out ConnectionLane connectionLane))
            {
                return false;
            }

            bool parkingAccess = (connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0;
            bool roadConnection = (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0;
            return parkingAccess || !roadConnection;
        }

        private string DescribeAccessOrigin(Entity lane)
        {
            if (m_ParkingLaneData.HasComponent(lane))
            {
                return "parking access";
            }

            if (m_GarageLaneData.HasComponent(lane))
            {
                return "garage access";
            }

            if (!m_ConnectionLaneData.TryGetComponent(lane, out ConnectionLane connectionLane))
            {
                return "building access";
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
            {
                return "parking connection";
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0)
            {
                return "building/service access connection";
            }

            return "building access";
        }

        private bool TryGetRoadCarLane(Entity lane, out CarLane carLane)
        {
            if (m_EdgeLaneData.HasComponent(lane) && m_CarLaneData.TryGetComponent(lane, out carLane))
            {
                return true;
            }

            carLane = default;
            return false;
        }

        private static bool LaneAllowsSideAccess(CarLane lane)
        {
            return (lane.m_Flags & (Game.Net.CarLaneFlags.SideConnection | Game.Net.CarLaneFlags.ParkingLeft | Game.Net.CarLaneFlags.ParkingRight)) != 0;
        }
    }
}