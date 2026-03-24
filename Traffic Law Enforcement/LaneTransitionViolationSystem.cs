using Game;
using Game.Net;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public partial class LaneTransitionViolationSystem : GameSystemBase
    {
        private EntityQuery m_CarQuery;
        private EntityQuery m_ChangedTransitionQuery;
        private EntityQuery m_EventBufferQuery;
        private Entity m_EventEntity;
        private ComponentLookup<Car> m_CarData;
        private ComponentLookup<CarLane> m_CarLaneData;
        private ComponentLookup<EdgeLane> m_EdgeLaneData;
        private ComponentLookup<ParkingLane> m_ParkingLaneData;
        private ComponentLookup<GarageLane> m_GarageLaneData;
        private ComponentLookup<ConnectionLane> m_ConnectionLaneData;
        private ComponentLookup<LaneTransitionAnalysisState> m_AnalysisStateData;
        private const int MaxIntersectionTransitionDiagnostics = 32;
        private int m_IntersectionTransitionDiagnosticCount;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CarQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadOnly<VehicleLaneHistory>());
            m_ChangedTransitionQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadOnly<CarCurrentLane>(),
                ComponentType.ReadOnly<VehicleLaneHistory>());
            m_ChangedTransitionQuery.SetChangedVersionFilter(ComponentType.ReadOnly<VehicleLaneHistory>());
            m_EventBufferQuery = GetEntityQuery(
                ComponentType.ReadOnly<LaneTransitionViolationEventBufferTag>(),
                ComponentType.ReadWrite<DetectedLaneTransitionViolation>());

            if (m_EventBufferQuery.IsEmptyIgnoreFilter)
            {
                m_EventEntity = EntityManager.CreateEntity();
                EntityManager.AddComponent<LaneTransitionViolationEventBufferTag>(m_EventEntity);
                EntityManager.AddBuffer<DetectedLaneTransitionViolation>(m_EventEntity);
            }
            else
            {
                m_EventEntity = m_EventBufferQuery.GetSingletonEntity();
            }
            m_CarData = GetComponentLookup<Car>(true);
            m_CarLaneData = GetComponentLookup<CarLane>(true);
            m_EdgeLaneData = GetComponentLookup<EdgeLane>(true);
            m_ParkingLaneData = GetComponentLookup<ParkingLane>(true);
            m_GarageLaneData = GetComponentLookup<GarageLane>(true);
            m_ConnectionLaneData = GetComponentLookup<ConnectionLane>(true);
            m_AnalysisStateData = GetComponentLookup<LaneTransitionAnalysisState>();
            RequireForUpdate(m_CarQuery);
        }

        protected override void OnUpdate()
        {
            if (m_ChangedTransitionQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            m_AnalysisStateData.Update(this);

            bool enforcementActive =
                Mod.IsMidBlockCrossingEnforcementEnabled ||
                Mod.IsIntersectionMovementEnforcementEnabled;

            NativeArray<Entity> vehicles = m_ChangedTransitionQuery.ToEntityArray(Allocator.Temp);
            NativeArray<VehicleLaneHistory> histories =
                m_ChangedTransitionQuery.ToComponentDataArray<VehicleLaneHistory>(Allocator.Temp);

            try
            {
                if (!enforcementActive)
                {
                    for (int index = 0; index < vehicles.Length; index += 1)
                    {
                        SyncAnalysisState(vehicles[index], histories[index]);
                    }

                    return;
                }

                m_CarData.Update(this);
                m_CarLaneData.Update(this);
                m_EdgeLaneData.Update(this);
                m_ParkingLaneData.Update(this);
                m_GarageLaneData.Update(this);
                m_ConnectionLaneData.Update(this);

                if (m_EventEntity == Entity.Null || !EntityManager.Exists(m_EventEntity))
                {
                    m_EventEntity = m_EventBufferQuery.GetSingletonEntity();
                }

                DynamicBuffer<DetectedLaneTransitionViolation> events =
                    EntityManager.GetBuffer<DetectedLaneTransitionViolation>(m_EventEntity);
                events.Clear();

                NativeArray<CarCurrentLane> currentLanes =
                    m_ChangedTransitionQuery.ToComponentDataArray<CarCurrentLane>(Allocator.Temp);

                try
                {
                    for (int index = 0; index < vehicles.Length; index += 1)
                    {
                        ProcessTransition(
                            vehicles[index],
                            currentLanes[index],
                            histories[index],
                            events);
                    }
                }
                finally
                {
                    currentLanes.Dispose();
                }
            }
            finally
            {
                vehicles.Dispose();
                histories.Dispose();
            }
        }

        private void SyncAnalysisState(Entity vehicle, VehicleLaneHistory history)
        {
            if (!m_AnalysisStateData.TryGetComponent(vehicle, out LaneTransitionAnalysisState analysisState))
            {
                analysisState = default;
            }

            if (analysisState.m_LastProcessedLaneChangeCount == history.m_LaneChangeCount)
            {
                return;
            }

            analysisState.m_LastProcessedLaneChangeCount = history.m_LaneChangeCount;
            if (m_AnalysisStateData.HasComponent(vehicle))
            {
                EntityManager.SetComponentData(vehicle, analysisState);
            }
            else
            {
                EntityManager.AddComponentData(vehicle, analysisState);
            }
        }

        private void ProcessTransition(Entity vehicle, CarCurrentLane currentLane, VehicleLaneHistory history, DynamicBuffer<DetectedLaneTransitionViolation> events)
        {
            if (!m_AnalysisStateData.TryGetComponent(vehicle, out LaneTransitionAnalysisState analysisState))
            {
                analysisState = default;
                EntityManager.AddComponentData(vehicle, analysisState);
            }

            if (history.m_LaneChangeCount == analysisState.m_LastProcessedLaneChangeCount || history.m_PreviousLane == Entity.Null)
            {
                return;
            }

            analysisState.m_LastProcessedLaneChangeCount = history.m_LaneChangeCount;
            EntityManager.SetComponentData(vehicle, analysisState);

            if (m_CarData.TryGetComponent(vehicle, out Car car) && EmergencyVehiclePolicy.IsEmergencyVehicle(car))
            {
                return;
            }

            if (TryDetectMidBlockCrossing(history, out LaneTransitionViolationReasonCode reasonCode))
            {
                events.Add(new DetectedLaneTransitionViolation
                {
                    Vehicle = vehicle,
                    Lane = history.m_CurrentLane,
                    Kind = LaneTransitionViolationKind.MidBlockCrossing,
                    ReasonCode = reasonCode,
                    ActualMovement = LaneMovement.None,
                    AllowedMovement = LaneMovement.None,
                });
            }

            bool logIntersectionCandidate =
                EnforcementLoggingPolicy.ShouldLogEnforcementEvents() &&
                Mod.IsIntersectionMovementEnforcementEnabled &&
                (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Connection) != 0 &&
                m_IntersectionTransitionDiagnosticCount < MaxIntersectionTransitionDiagnostics;

            bool hasIntersectionViolation =
                TryDetectIntersectionMovementViolation(
                    history,
                    currentLane,
                    out LaneMovement actualMovement,
                    out LaneMovement allowedMovement);

            if (logIntersectionCandidate)
            {
                m_IntersectionTransitionDiagnosticCount += 1;

                string previousFlagsText = m_CarLaneData.TryGetComponent(history.m_PreviousLane, out CarLane previousCarLane)
                    ? previousCarLane.m_Flags.ToString()
                    : "(no CarLane)";

                string currentFlagsText = m_CarLaneData.TryGetComponent(history.m_CurrentLane, out CarLane currentCarLane)
                    ? currentCarLane.m_Flags.ToString()
                    : "(no CarLane)";

                string connectionFlagsText = m_ConnectionLaneData.TryGetComponent(history.m_CurrentLane, out ConnectionLane connectionLane)
                    ? connectionLane.m_Flags.ToString()
                    : "(no ConnectionLane)";

                Mod.log.Info(
                    $"Intersection transition candidate: vehicle={vehicle}, " +
                    $"fromLane={history.m_PreviousLane}, toLane={history.m_CurrentLane}, " +
                    $"illegal={hasIntersectionViolation}, actual={actualMovement}, allowed={allowedMovement}, " +
                    $"previousFlags={previousFlagsText}, currentFlags={currentFlagsText}, connectionFlags={connectionFlagsText}");
            }

            if (hasIntersectionViolation)
            {
                events.Add(new DetectedLaneTransitionViolation
                {
                    Vehicle = vehicle,
                    Lane = history.m_CurrentLane,
                    Kind = LaneTransitionViolationKind.IntersectionMovement,
                    ReasonCode = LaneTransitionViolationReasonCode.None,
                    ActualMovement = actualMovement,
                    AllowedMovement = allowedMovement,
                });
            }
        }

        private bool TryDetectMidBlockCrossing(VehicleLaneHistory history, out LaneTransitionViolationReasonCode reasonCode)
        {
            reasonCode = LaneTransitionViolationReasonCode.None;

            if (m_EdgeLaneData.HasComponent(history.m_PreviousLane) &&
                m_EdgeLaneData.HasComponent(history.m_CurrentLane))
            {
                if (!m_CarLaneData.TryGetComponent(history.m_PreviousLane, out CarLane previousCarLane) ||
                    !m_CarLaneData.TryGetComponent(history.m_CurrentLane, out CarLane currentCarLane))
                {
                    return false;
                }

                EdgeLane previousEdgeLane = m_EdgeLaneData[history.m_PreviousLane];
                EdgeLane currentEdgeLane = m_EdgeLaneData[history.m_CurrentLane];
                bool sameOwner =
                    history.m_PreviousLaneOwner == history.m_CurrentLaneOwner &&
                    history.m_CurrentLaneOwner != Entity.Null;
                bool oppositeDirections = IsOppositeDirection(previousEdgeLane, currentEdgeLane);
                bool sameCarriageway =
                    previousCarLane.m_CarriagewayGroup == currentCarLane.m_CarriagewayGroup;

                if (sameOwner && oppositeDirections && sameCarriageway)
                {
                    reasonCode = LaneTransitionViolationReasonCode.OppositeFlowSameRoadSegment;
                    return true;
                }
            }

            if (!m_EdgeLaneData.HasComponent(history.m_PreviousLane) ||
                !m_CarLaneData.TryGetComponent(history.m_PreviousLane, out CarLane sourceLane))
            {
                return TryDetectOutboundAccessCrossing(history, out reasonCode);
            }

            if (!LaneAllowsSideAccess(sourceLane))
            {
                if (m_GarageLaneData.HasComponent(history.m_CurrentLane))
                {
                    reasonCode = LaneTransitionViolationReasonCode.EnteredGarageAccessWithoutSideAccess;
                    return true;
                }

                if (m_ParkingLaneData.HasComponent(history.m_CurrentLane))
                {
                    reasonCode = LaneTransitionViolationReasonCode.EnteredParkingAccessWithoutSideAccess;
                    return true;
                }

                if (m_ConnectionLaneData.TryGetComponent(history.m_CurrentLane, out ConnectionLane connectionLane))
                {
                    if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
                    {
                        reasonCode = LaneTransitionViolationReasonCode.EnteredParkingConnectionWithoutSideAccess;
                        return true;
                    }

                    if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0)
                    {
                        reasonCode = LaneTransitionViolationReasonCode.EnteredBuildingAccessConnectionWithoutSideAccess;
                        return true;
                    }
                }
            }

            return TryDetectOutboundAccessCrossing(history, out reasonCode);
        }

        private bool TryDetectOutboundAccessCrossing(VehicleLaneHistory history, out LaneTransitionViolationReasonCode reasonCode)
        {
            reasonCode = LaneTransitionViolationReasonCode.None;

            if (!IsAccessOrigin(history.m_PreviousLane))
            {
                return false;
            }

            if (!m_EdgeLaneData.HasComponent(history.m_CurrentLane) ||
                !m_CarLaneData.TryGetComponent(history.m_CurrentLane, out CarLane targetLane))
            {
                return false;
            }

            if (LaneAllowsSideAccess(targetLane))
            {
                return false;
            }

            if (m_ParkingLaneData.HasComponent(history.m_PreviousLane))
            {
                reasonCode = LaneTransitionViolationReasonCode.ExitedParkingAccessWithoutSideAccess;
                return true;
            }

            if (m_GarageLaneData.HasComponent(history.m_PreviousLane))
            {
                reasonCode = LaneTransitionViolationReasonCode.ExitedGarageAccessWithoutSideAccess;
                return true;
            }

            if (m_ConnectionLaneData.TryGetComponent(history.m_PreviousLane, out ConnectionLane connectionLane))
            {
                if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
                {
                    reasonCode = LaneTransitionViolationReasonCode.ExitedParkingConnectionWithoutSideAccess;
                    return true;
                }

                if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0)
                {
                    reasonCode = LaneTransitionViolationReasonCode.ExitedBuildingAccessConnectionWithoutSideAccess;
                    return true;
                }
            }

            return false;
        }

        private bool TryDetectIntersectionMovementViolation(VehicleLaneHistory history, CarCurrentLane currentLane, out LaneMovement actualMovement, out LaneMovement allowedMovement)
        {
            actualMovement = LaneMovement.None;
            allowedMovement = LaneMovement.None;

            if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Connection) == 0)
            {
                return false;
            }

            return IntersectionMovementPolicy.TryGetIllegalIntersectionMovement(
                m_ConnectionLaneData,
                m_CarLaneData,
                history.m_PreviousLane,
                history.m_CurrentLane,
                out actualMovement,
                out allowedMovement);
        }

        private static bool IsOppositeDirection(EdgeLane previousLane, EdgeLane currentLane)
        {
            float previousDirection = previousLane.m_EdgeDelta.y - previousLane.m_EdgeDelta.x;
            float currentDirection = currentLane.m_EdgeDelta.y - currentLane.m_EdgeDelta.x;
            return previousDirection * currentDirection < 0f;
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

            if (IsAccessConnection(lane))
            {
                return DescribeAccessConnection(lane);
            }

            return "building access";
        }

        private string DescribeAccessConnection(Entity lane)
        {
            if (!m_ConnectionLaneData.TryGetComponent(lane, out ConnectionLane connectionLane))
            {
                return "access connection";
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
            {
                return "parking connection";
            }

            if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0)
            {
                return "building/service access connection";
            }

            return "access connection";
        }

        private static bool LaneAllowsSideAccess(CarLane lane)
        {
            return (lane.m_Flags & (Game.Net.CarLaneFlags.SideConnection | Game.Net.CarLaneFlags.ParkingLeft | Game.Net.CarLaneFlags.ParkingRight)) != 0;
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
