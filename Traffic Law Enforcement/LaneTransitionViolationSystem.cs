using Game;
using Game.Net;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public class LaneTransitionViolationSystem : GameSystemBase
    {
        private EntityQuery m_CarQuery;
        private EntityQuery m_ChangedTransitionQuery;
        private EntityQuery m_StatisticsQuery;
        private Entity m_StatisticsEntity;
        private ComponentLookup<Car> m_CarData;
        private ComponentLookup<CarLane> m_CarLaneData;
        private ComponentLookup<EdgeLane> m_EdgeLaneData;
        private ComponentLookup<ParkingLane> m_ParkingLaneData;
        private ComponentLookup<GarageLane> m_GarageLaneData;
        private ComponentLookup<ConnectionLane> m_ConnectionLaneData;
        private ComponentLookup<LaneTransitionAnalysisState> m_AnalysisStateData;

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
            m_StatisticsQuery = GetEntityQuery(ComponentType.ReadWrite<TrafficLawEnforcementStatistics>());
            if (m_StatisticsQuery.IsEmptyIgnoreFilter)
            {
                m_StatisticsEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(m_StatisticsEntity, default(TrafficLawEnforcementStatistics));
            }
            else
            {
                m_StatisticsEntity = m_StatisticsQuery.GetSingletonEntity();
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
            m_CarData.Update(this);
            m_CarLaneData.Update(this);
            m_EdgeLaneData.Update(this);
            m_ParkingLaneData.Update(this);
            m_GarageLaneData.Update(this);
            m_ConnectionLaneData.Update(this);
            m_AnalysisStateData.Update(this);

            NativeArray<Entity> vehicles = m_ChangedTransitionQuery.ToEntityArray(Allocator.Temp);
            NativeArray<CarCurrentLane> currentLanes = m_ChangedTransitionQuery.ToComponentDataArray<CarCurrentLane>(Allocator.Temp);
            NativeArray<VehicleLaneHistory> histories = m_ChangedTransitionQuery.ToComponentDataArray<VehicleLaneHistory>(Allocator.Temp);

            try
            {
                if (!Mod.IsMidBlockCrossingEnforcementEnabled && !Mod.IsIntersectionMovementEnforcementEnabled)
                {
                    for (int index = 0; index < vehicles.Length; index++)
                    {
                        SyncAnalysisState(vehicles[index], histories[index]);
                    }

                    return;
                }

                for (int index = 0; index < vehicles.Length; index++)
                {
                    ProcessTransition(vehicles[index], currentLanes[index], histories[index]);
                }
            }
            finally
            {
                vehicles.Dispose();
                currentLanes.Dispose();
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

        private void ProcessTransition(Entity vehicle, CarCurrentLane currentLane, VehicleLaneHistory history)
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

            if (TryDetectMidBlockCrossing(history, out string midBlockReason))
            {
                IncrementMidBlockStatistic();
                string message = $"Mid-block crossing violation: vehicle={vehicle}, fromLane={history.m_PreviousLane}, toLane={history.m_CurrentLane}, reason={midBlockReason}";
                EnforcementPenaltyService.RecordMidBlockCrossingViolation(vehicle, history.m_CurrentLane, midBlockReason);
                EnforcementLoggingPolicy.RecordEnforcementEvent(message);
            }

            if (TryDetectIntersectionMovementViolation(history, currentLane, out LaneMovement actualMovement, out LaneMovement allowedMovement))
            {
                IncrementIntersectionStatistic();
                string message = $"Intersection movement violation: vehicle={vehicle}, fromLane={history.m_PreviousLane}, connectionLane={history.m_CurrentLane}, actual={actualMovement}, allowed={allowedMovement}";
                EnforcementPenaltyService.RecordIntersectionMovementViolation(vehicle, history.m_CurrentLane, $"actual {actualMovement}, allowed {allowedMovement}");
                EnforcementLoggingPolicy.RecordEnforcementEvent(message);
            }
        }

        private bool TryDetectMidBlockCrossing(VehicleLaneHistory history, out string reason)
        {
            reason = null;

            if (m_EdgeLaneData.HasComponent(history.m_PreviousLane) && m_EdgeLaneData.HasComponent(history.m_CurrentLane))
            {
                if (!m_CarLaneData.TryGetComponent(history.m_PreviousLane, out CarLane previousCarLane) ||
                    !m_CarLaneData.TryGetComponent(history.m_CurrentLane, out CarLane currentCarLane))
                {
                    return false;
                }

                EdgeLane previousEdgeLane = m_EdgeLaneData[history.m_PreviousLane];
                EdgeLane currentEdgeLane = m_EdgeLaneData[history.m_CurrentLane];
                bool sameOwner = history.m_PreviousLaneOwner == history.m_CurrentLaneOwner && history.m_CurrentLaneOwner != Entity.Null;
                bool oppositeDirections = IsOppositeDirection(previousEdgeLane, currentEdgeLane);
                bool sameCarriageway = previousCarLane.m_CarriagewayGroup == currentCarLane.m_CarriagewayGroup;

                if (sameOwner && oppositeDirections && sameCarriageway)
                {
                    reason = "vehicle switched to the opposite flow on the same road segment";
                    return true;
                }
            }

            if (!m_EdgeLaneData.HasComponent(history.m_PreviousLane) || !m_CarLaneData.TryGetComponent(history.m_PreviousLane, out CarLane sourceLane))
            {
                return TryDetectOutboundAccessCrossing(history, out reason);
            }

            if (!LaneAllowsSideAccess(sourceLane))
            {
                if (m_ParkingLaneData.HasComponent(history.m_CurrentLane) || m_GarageLaneData.HasComponent(history.m_CurrentLane))
                {
                    reason = m_GarageLaneData.HasComponent(history.m_CurrentLane)
                        ? "vehicle entered garage access from a lane without side-access permission"
                        : "vehicle entered parking access from a lane without side-access permission";
                    return true;
                }

                if (IsAccessConnection(history.m_CurrentLane))
                {
                    reason = $"vehicle crossed into {DescribeAccessConnection(history.m_CurrentLane)} from a lane without side-access permission";
                    return true;
                }
            }

            return TryDetectOutboundAccessCrossing(history, out reason);
        }

        private bool TryDetectOutboundAccessCrossing(VehicleLaneHistory history, out string reason)
        {
            reason = null;

            if (!IsAccessOrigin(history.m_PreviousLane))
            {
                return false;
            }

            if (!m_EdgeLaneData.HasComponent(history.m_CurrentLane) || !m_CarLaneData.TryGetComponent(history.m_CurrentLane, out CarLane targetLane))
            {
                return false;
            }

            if (LaneAllowsSideAccess(targetLane))
            {
                return false;
            }

            reason = $"vehicle exited {DescribeAccessOrigin(history.m_PreviousLane)} into a lane without side-access permission";
            return true;
        }

        private bool TryDetectIntersectionMovementViolation(VehicleLaneHistory history, CarCurrentLane currentLane, out LaneMovement actualMovement, out LaneMovement allowedMovement)
        {
            actualMovement = LaneMovement.None;
            allowedMovement = LaneMovement.None;

            if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Connection) == 0)
            {
                return false;
            }

            if (!m_ConnectionLaneData.TryGetComponent(history.m_CurrentLane, out ConnectionLane connectionLane))
            {
                return false;
            }

            bool isRoadIntersectionConnection = (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0 && (connectionLane.m_Flags & ConnectionLaneFlags.Parking) == 0;
            if (!isRoadIntersectionConnection)
            {
                return false;
            }

            if (!m_CarLaneData.TryGetComponent(history.m_PreviousLane, out CarLane sourceLane) ||
                !m_CarLaneData.TryGetComponent(history.m_CurrentLane, out CarLane connectionCarLane))
            {
                return false;
            }

            actualMovement = GetMovement(connectionCarLane.m_Flags);
            allowedMovement = GetMovement(sourceLane.m_Flags);
            if (actualMovement == LaneMovement.None || allowedMovement == LaneMovement.None)
            {
                return false;
            }

            return (allowedMovement & actualMovement) == LaneMovement.None;
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

        private void IncrementMidBlockStatistic()
        {
            TrafficLawEnforcementStatistics statistics = EntityManager.GetComponentData<TrafficLawEnforcementStatistics>(m_StatisticsEntity);
            statistics.m_MidBlockCrossingViolationCount += 1;
            EntityManager.SetComponentData(m_StatisticsEntity, statistics);
            EnforcementTelemetry.SetStatistics(statistics);
        }

        private void IncrementIntersectionStatistic()
        {
            TrafficLawEnforcementStatistics statistics = EntityManager.GetComponentData<TrafficLawEnforcementStatistics>(m_StatisticsEntity);
            statistics.m_IntersectionMovementViolationCount += 1;
            EntityManager.SetComponentData(m_StatisticsEntity, statistics);
            EnforcementTelemetry.SetStatistics(statistics);
        }
    }
}
