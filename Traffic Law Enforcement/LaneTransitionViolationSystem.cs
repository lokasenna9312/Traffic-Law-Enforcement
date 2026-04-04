using Game;
using Game.Common;
using Game.Net;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public partial class LaneTransitionViolationSystem : GameSystemBase
    {
        private enum OppositeFlowNearMissReason : byte
        {
            None = 0,
            MissingPreviousEdgeLane = 1,
            MissingCurrentEdgeLane = 2,
            MissingPreviousCarLane = 3,
            MissingCurrentCarLane = 4,
            DifferentOwner = 5,
            NotOppositeDirection = 6,
            DifferentCarriagewayGroup = 7,
        }

        private EntityQuery m_CarQuery;
        private EntityQuery m_ChangedTransitionQuery;
        private EntityQuery m_EventBufferQuery;
        private Entity m_EventEntity;
        // The real carry boundary is semantic:
        // stay inside the narrow ordinary-egress intermediate corridor until the
        // first ordinary road arrival. This large budget is only a defensive
        // guardrail against stale carry surviving indefinitely.
        private const byte PendingOrdinaryEgressCorridorFailsafeBudget = 32;
        private EntityTypeHandle m_EntityTypeHandle;
        private ComponentTypeHandle<VehicleLaneHistory> m_HistoryTypeHandle;
        private ComponentTypeHandle<CarCurrentLane> m_CurrentLaneTypeHandle;
        private ComponentLookup<Car> m_CarData;
        private ComponentLookup<DeliveryTruck> m_DeliveryTruckData;
        private ComponentLookup<CarLane> m_CarLaneData;
        private ComponentLookup<EdgeLane> m_EdgeLaneData;
        private ComponentLookup<ParkingLane> m_ParkingLaneData;
        private ComponentLookup<GarageLane> m_GarageLaneData;
        private ComponentLookup<ConnectionLane> m_ConnectionLaneData;
        private ComponentLookup<Owner> m_OwnerData;
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
            m_DeliveryTruckData = GetComponentLookup<DeliveryTruck>(true);
            m_CarLaneData = GetComponentLookup<CarLane>(true);
            m_EdgeLaneData = GetComponentLookup<EdgeLane>(true);
            m_ParkingLaneData = GetComponentLookup<ParkingLane>(true);
            m_GarageLaneData = GetComponentLookup<GarageLane>(true);
            m_ConnectionLaneData = GetComponentLookup<ConnectionLane>(true);
            m_OwnerData = GetComponentLookup<Owner>(true);
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
            m_EntityTypeHandle = GetEntityTypeHandle();
            m_HistoryTypeHandle = GetComponentTypeHandle<VehicleLaneHistory>(true);

            bool enforcementActive =
                Mod.IsMidBlockCrossingEnforcementEnabled ||
                Mod.IsIntersectionMovementEnforcementEnabled;

            NativeArray<ArchetypeChunk> chunks = m_ChangedTransitionQuery.ToArchetypeChunkArray(Allocator.Temp);
            try
            {
                if (!enforcementActive)
                {
                    m_CarLaneData.Update(this);
                    m_EdgeLaneData.Update(this);
                    m_ParkingLaneData.Update(this);
                    m_GarageLaneData.Update(this);
                    m_ConnectionLaneData.Update(this);
                    m_OwnerData.Update(this);

                    for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex += 1)
                    {
                        ArchetypeChunk chunk = chunks[chunkIndex];
                        NativeArray<Entity> vehicles = chunk.GetNativeArray(m_EntityTypeHandle);
                        NativeArray<VehicleLaneHistory> histories = chunk.GetNativeArray(ref m_HistoryTypeHandle);

                        for (int index = 0; index < vehicles.Length; index += 1)
                        {
                            SyncAnalysisState(vehicles[index], histories[index]);
                        }
                    }

                    return;
                }

                m_CurrentLaneTypeHandle = GetComponentTypeHandle<CarCurrentLane>(true);
                m_CarData.Update(this);
                m_DeliveryTruckData.Update(this);
                m_CarLaneData.Update(this);
                m_EdgeLaneData.Update(this);
                m_ParkingLaneData.Update(this);
                m_GarageLaneData.Update(this);
                m_ConnectionLaneData.Update(this);
                m_OwnerData.Update(this);

                if (m_EventEntity == Entity.Null || !EntityManager.Exists(m_EventEntity))
                {
                    m_EventEntity = m_EventBufferQuery.GetSingletonEntity();
                }

                DynamicBuffer<DetectedLaneTransitionViolation> events =
                    EntityManager.GetBuffer<DetectedLaneTransitionViolation>(m_EventEntity);
                if (events.Length > 0)
                {
                    events.Clear();
                }

                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex += 1)
                {
                    ArchetypeChunk chunk = chunks[chunkIndex];
                    NativeArray<Entity> vehicles = chunk.GetNativeArray(m_EntityTypeHandle);
                    NativeArray<VehicleLaneHistory> histories = chunk.GetNativeArray(ref m_HistoryTypeHandle);
                    NativeArray<CarCurrentLane> currentLanes = chunk.GetNativeArray(ref m_CurrentLaneTypeHandle);

                    for (int index = 0; index < vehicles.Length; index += 1)
                    {
                        ProcessTransition(
                            vehicles[index],
                            currentLanes[index],
                            histories[index],
                            events);
                    }
                }
            }
            finally
            {
                chunks.Dispose();
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
            ClearPendingOrdinaryEgress(ref analysisState);
            ClearPendingUndercroftOrdinaryEgressBridge(ref analysisState);
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

            if (m_CarData.TryGetComponent(vehicle, out Car car) && EmergencyVehiclePolicy.IsEmergencyVehicle(car))
            {
                ClearPendingOrdinaryEgress(ref analysisState);
                ClearPendingUndercroftOrdinaryEgressBridge(ref analysisState);
                EntityManager.SetComponentData(vehicle, analysisState);
                return;
            }

            MaybeLogRealizedOppositeFlowNearMiss(vehicle, history);
            MaybeLogRealizedEgressTrace(vehicle, currentLane, history);

            IllegalEgressApplyMode illegalEgressMode = IllegalEgressApplyMode.None;
            Entity illegalEgressOriginLane = Entity.Null;
            Entity illegalEgressRoadLane = Entity.Null;
            bool hasMidBlockViolation = TryDetectMidBlockCrossing(history, out LaneTransitionViolationReasonCode reasonCode);
            if (hasMidBlockViolation)
            {
                CaptureDirectIllegalEgressApplyMarker(
                    vehicle,
                    history,
                    reasonCode,
                    out illegalEgressMode,
                    out illegalEgressOriginLane,
                    out illegalEgressRoadLane);
            }

            if (!hasMidBlockViolation)
            {
                hasMidBlockViolation = TryDetectOrAdvancePendingOrdinaryEgress(
                    vehicle,
                    history,
                    ref analysisState,
                    out reasonCode,
                    out illegalEgressOriginLane);
                if (hasMidBlockViolation)
                {
                    illegalEgressMode = IllegalEgressApplyMode.Carried;
                    illegalEgressRoadLane = history.m_CurrentLane;
                }
            }
            else
            {
                ClearPendingOrdinaryEgress(ref analysisState);
                ClearPendingUndercroftOrdinaryEgressBridge(ref analysisState);
            }

            if (hasMidBlockViolation)
            {
                ClearPendingUndercroftOrdinaryEgressBridge(ref analysisState);
            }

            if (hasMidBlockViolation)
            {
                MaybeLogRealizedAccessDetection(vehicle, history, reasonCode);
                MaybeLogRealizedOppositeFlowDetection(vehicle, history, reasonCode);

                events.Add(new DetectedLaneTransitionViolation
                {
                    Vehicle = vehicle,
                    Lane = history.m_CurrentLane,
                    PreviousLane = history.m_PreviousLane,
                    PreviousOwner = history.m_PreviousLaneOwner,
                    CurrentOwner = history.m_CurrentLaneOwner,
                    Kind = LaneTransitionViolationKind.MidBlockCrossing,
                    ReasonCode = reasonCode,
                    IllegalEgressMode = illegalEgressMode,
                    IllegalEgressOriginLane = illegalEgressOriginLane,
                    IllegalEgressRoadLane = illegalEgressRoadLane,
                    ActualMovement = LaneMovement.None,
                    AllowedMovement = LaneMovement.None,
                });
            }
            else if (!HasPendingOrdinaryEgress(analysisState))
            {
                WritePendingOrdinaryEgressIfNeeded(vehicle, history, ref analysisState);
            }

            EntityManager.SetComponentData(vehicle, analysisState);

            bool logIntersectionCandidate =
                EnforcementLoggingPolicy.ShouldLogVehicleSpecificEnforcementEvent(vehicle) &&
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
                    $"Intersection transition candidate: vehicle={FocusedLoggingService.FormatEntity(vehicle)}, " +
                    $"fromLane={FocusedLoggingService.FormatEntity(history.m_PreviousLane)}, " +
                    $"toLane={FocusedLoggingService.FormatEntity(history.m_CurrentLane)}, " +
                    $"illegal={hasIntersectionViolation}, actual={actualMovement}, allowed={allowedMovement}, " +
                    $"previousFlags={previousFlagsText}, currentFlags={currentFlagsText}, connectionFlags={connectionFlagsText}");
            }

            if (hasIntersectionViolation)
            {
                events.Add(new DetectedLaneTransitionViolation
                {
                    Vehicle = vehicle,
                    Lane = history.m_CurrentLane,
                    PreviousLane = history.m_PreviousLane,
                    PreviousOwner = history.m_PreviousLaneOwner,
                    CurrentOwner = history.m_CurrentLaneOwner,
                    Kind = LaneTransitionViolationKind.IntersectionMovement,
                    ReasonCode = LaneTransitionViolationReasonCode.None,
                    ActualMovement = actualMovement,
                    AllowedMovement = allowedMovement,
                });
            }
        }

        // Debug-only realized-path instrumentation for opposite-flow exact-pair
        // failures. This is emitted only when a realized transition is processed
        // and the pair does not satisfy current OppositeFlowSameRoadSegment rules.
        private void MaybeLogRealizedOppositeFlowNearMiss(
            Entity vehicle,
            VehicleLaneHistory history)
        {
            if (!EnforcementLoggingPolicy.ShouldLogVehicleSpecificEnforcementEvent(vehicle) ||
                !TryGetOppositeFlowNearMissReason(history, out OppositeFlowNearMissReason failReason))
            {
                return;
            }

            int previousCarriagewayGroup =
                TryGetCarriagewayGroup(history.m_PreviousLane, out ushort previousGroup)
                    ? previousGroup
                    : -1;
            int currentCarriagewayGroup =
                TryGetCarriagewayGroup(history.m_CurrentLane, out ushort currentGroup)
                    ? currentGroup
                    : -1;

            int previousDirectionSign = GetDirectionSign(history.m_PreviousLane);
            int currentDirectionSign = GetDirectionSign(history.m_CurrentLane);

            string message =
                "[OPPFLOW_REALIZED_NEARMISS] " +
                $"vehicle={FocusedLoggingService.FormatEntity(vehicle)} " +
                $"previousLane={FocusedLoggingService.FormatEntity(history.m_PreviousLane)} " +
                $"currentLane={FocusedLoggingService.FormatEntity(history.m_CurrentLane)} " +
                $"previousOwner={FocusedLoggingService.FormatEntity(history.m_PreviousLaneOwner)} " +
                $"currentOwner={FocusedLoggingService.FormatEntity(history.m_CurrentLaneOwner)} " +
                $"previousCarriagewayGroup={previousCarriagewayGroup} " +
                $"currentCarriagewayGroup={currentCarriagewayGroup} " +
                $"previousDirectionSign={previousDirectionSign} " +
                $"currentDirectionSign={currentDirectionSign} " +
                $"failReason={failReason}";

            EnforcementLoggingPolicy.RecordEnforcementEvent(message, vehicle);
        }

        // Debug-only realized-path instrumentation for illegal access exact pairs.
        // This confirms that realized ingress/egress was detected even if the
        // route-inspection visibility layer did not later surface it.
        // This does not alter classification or buffering behavior.
        private void MaybeLogRealizedAccessDetection(
            Entity vehicle,
            VehicleLaneHistory history,
            LaneTransitionViolationReasonCode reasonCode)
        {
            if (!MidBlockCrossingPolicy.IsAccessTransitionReason(reasonCode) ||
                !EnforcementLoggingPolicy.ShouldLogVehicleSpecificEnforcementEvent(vehicle))
            {
                return;
            }

            string message =
                "[ACCESS_REALIZED_DETECTED] " +
                $"vehicle={FocusedLoggingService.FormatEntity(vehicle)} " +
                $"reason={reasonCode} " +
                $"previousLane={FocusedLoggingService.FormatEntity(history.m_PreviousLane)} " +
                $"currentLane={FocusedLoggingService.FormatEntity(history.m_CurrentLane)} " +
                $"previousOwner={FocusedLoggingService.FormatEntity(history.m_PreviousLaneOwner)} " +
                $"currentOwner={FocusedLoggingService.FormatEntity(history.m_CurrentLaneOwner)} " +
                $"previousLaneKind={DescribeLaneKind(history.m_PreviousLane)} " +
                $"currentLaneKind={DescribeLaneKind(history.m_CurrentLane)}";

            EnforcementLoggingPolicy.RecordEnforcementEvent(message, vehicle);
        }

        // Debug-only realized egress trace. This stays narrowly scoped to
        // watched vehicle-specific enforcement logging and only reports pairs
        // where the current realized lane is road and the previous lane is
        // either non-road or access-adjacent enough to plausibly be part of
        // a roadside-building egress boundary.
        private void MaybeLogRealizedEgressTrace(
            Entity vehicle,
            CarCurrentLane currentLane,
            VehicleLaneHistory history)
        {
            if (!EnforcementLoggingPolicy.ShouldLogVehicleSpecificEnforcementEvent(vehicle))
            {
                return;
            }

            MidBlockCrossingPolicy.TraceIllegalEgressTransition(
                EntityManager,
                history.m_PreviousLane,
                history.m_CurrentLane,
                out bool previousIsAccessOrigin,
                out bool currentIsRoad,
                out bool egressDetectResult,
                out MidBlockCrossingPolicy.AccessEgressTraceFailReason failReason,
                out _);

            if (m_DeliveryTruckData.HasComponent(vehicle))
            {
                string deliveryTruckMessage =
                    "[TRUCK_EGRESS_DIRECT_TRACE] " +
                    $"vehicle={FocusedLoggingService.FormatEntity(vehicle)} " +
                    $"previousLane={FocusedLoggingService.FormatEntity(history.m_PreviousLane)} " +
                    $"currentLane={FocusedLoggingService.FormatEntity(history.m_CurrentLane)} " +
                    $"previousLaneKind={DescribeLaneKind(history.m_PreviousLane)} " +
                    $"currentLaneKind={DescribeLaneKind(history.m_CurrentLane)} " +
                    $"previousConnectionFlags={FormatConnectionLaneFlags(history.m_PreviousLane)} " +
                    $"currentConnectionFlags={FormatConnectionLaneFlags(history.m_CurrentLane)} " +
                    $"previousIsAccessOrigin={previousIsAccessOrigin} " +
                    $"currentIsRoad={currentIsRoad} " +
                    $"egressDetectResult={egressDetectResult} " +
                    $"failReason={failReason} " +
                    $"currentLaneFlags={currentLane.m_LaneFlags}";
                EnforcementLoggingPolicy.RecordEnforcementEvent(deliveryTruckMessage, vehicle);
            }
            if (!currentIsRoad)
            {
                return;
            }

            bool previousIsRoad =
                m_EdgeLaneData.HasComponent(history.m_PreviousLane) &&
                m_CarLaneData.HasComponent(history.m_PreviousLane);

            if (!previousIsAccessOrigin && previousIsRoad)
            {
                return;
            }

            string vehicleText = FocusedLoggingService.FormatEntity(vehicle);
            string accessTraceMessage =
                "[ACCESS_EGRESS_REALIZED_TRACE] " +
                $"vehicle={vehicleText} " +
                $"vehicleId={vehicleText} " +
                $"previousLane={FocusedLoggingService.FormatEntity(history.m_PreviousLane)} " +
                $"currentLane={FocusedLoggingService.FormatEntity(history.m_CurrentLane)} " +
                $"previousOwner={FocusedLoggingService.FormatEntity(history.m_PreviousLaneOwner)} " +
                $"currentOwner={FocusedLoggingService.FormatEntity(history.m_CurrentLaneOwner)} " +
                $"previousLaneKind={DescribeLaneKind(history.m_PreviousLane)} " +
                $"currentLaneKind={DescribeLaneKind(history.m_CurrentLane)} " +
                $"previousIsAccessOrigin={previousIsAccessOrigin} " +
                $"currentIsRoad={currentIsRoad} " +
                $"egressDetectResult={egressDetectResult} " +
                $"failReason={failReason}";

            EnforcementLoggingPolicy.RecordEnforcementEvent(accessTraceMessage, vehicle);
        }

        private bool TryDetectMidBlockCrossing(
            VehicleLaneHistory history,
            out LaneTransitionViolationReasonCode reasonCode)
        {
            return MidBlockCrossingPolicy.TryGetIllegalTransition(
                EntityManager,
                history.m_PreviousLane,
                history.m_CurrentLane,
                out reasonCode);
        }

        // This keeps the smallest ordinary-car egress carry scoped to:
        // recognized access origin -> narrow degraded non-road corridor ->
        // first ordinary road arrival.
        // The direct exact-pair path remains unchanged and still runs first.
        private bool TryDetectOrAdvancePendingOrdinaryEgress(
            Entity vehicle,
            VehicleLaneHistory history,
            ref LaneTransitionAnalysisState analysisState,
            out LaneTransitionViolationReasonCode reasonCode,
            out Entity originLane)
        {
            reasonCode = LaneTransitionViolationReasonCode.None;
            originLane = Entity.Null;

            if (!HasPendingOrdinaryEgress(analysisState))
            {
                return false;
            }

            Entity pendingOriginLane = analysisState.m_PendingOrdinaryEgressOriginLane;

            if (!IsEligibleForPendingOrdinaryEgress(vehicle))
            {
                ClearPendingOrdinaryEgress(ref analysisState);
                return false;
            }

            if (IsNarrowOrdinaryEgressIntermediate(history.m_PreviousLane))
            {
                if (IsRoadLane(history.m_CurrentLane))
                {
                    ClearPendingOrdinaryEgress(ref analysisState);

                    bool detected = MidBlockCrossingPolicy.TryGetIllegalEgressTransition(
                        EntityManager,
                        pendingOriginLane,
                        history.m_CurrentLane,
                        out reasonCode);
                    if (detected)
                    {
                        originLane = pendingOriginLane;
                    }

                    return detected;
                }

                if (IsNarrowOrdinaryEgressIntermediate(history.m_CurrentLane))
                {
                    AdvancePendingOrdinaryEgressCorridor(
                        vehicle,
                        history,
                        ref analysisState,
                        pendingOriginLane);
                    return false;
                }
            }

            ClearPendingOrdinaryEgress(ref analysisState);
            return false;
        }

        // Debug-only realized-path instrumentation for the exact pair that fired
        // OppositeFlowSameRoadSegment. This does not alter classification.
        private void MaybeLogRealizedOppositeFlowDetection(
            Entity vehicle,
            VehicleLaneHistory history,
            LaneTransitionViolationReasonCode reasonCode)
        {
            if (reasonCode != LaneTransitionViolationReasonCode.OppositeFlowSameRoadSegment ||
                !EnforcementLoggingPolicy.ShouldLogVehicleSpecificEnforcementEvent(vehicle))
            {
                return;
            }

            int previousCarriagewayGroup =
                TryGetCarriagewayGroup(history.m_PreviousLane, out ushort previousGroup)
                    ? previousGroup
                    : -1;
            int currentCarriagewayGroup =
                TryGetCarriagewayGroup(history.m_CurrentLane, out ushort currentGroup)
                    ? currentGroup
                    : -1;

            int previousDirectionSign = GetDirectionSign(history.m_PreviousLane);
            int currentDirectionSign = GetDirectionSign(history.m_CurrentLane);

            string message =
                "[OPPFLOW_REALIZED_DETECTED] " +
                $"vehicle={FocusedLoggingService.FormatEntity(vehicle)} " +
                $"reason={reasonCode} " +
                $"previousLane={FocusedLoggingService.FormatEntity(history.m_PreviousLane)} " +
                $"currentLane={FocusedLoggingService.FormatEntity(history.m_CurrentLane)} " +
                $"previousOwner={FocusedLoggingService.FormatEntity(history.m_PreviousLaneOwner)} " +
                $"currentOwner={FocusedLoggingService.FormatEntity(history.m_CurrentLaneOwner)} " +
                $"previousCarriagewayGroup={previousCarriagewayGroup} " +
                $"currentCarriagewayGroup={currentCarriagewayGroup} " +
                $"previousDirectionSign={previousDirectionSign} " +
                $"currentDirectionSign={currentDirectionSign}";

            EnforcementLoggingPolicy.RecordEnforcementEvent(message, vehicle);
        }

        private bool TryGetCarriagewayGroup(Entity lane, out ushort carriagewayGroup)
        {
            if (m_CarLaneData.TryGetComponent(lane, out CarLane carLane))
            {
                carriagewayGroup = carLane.m_CarriagewayGroup;
                return true;
            }

            carriagewayGroup = 0;
            return false;
        }

        private int GetDirectionSign(Entity lane)
        {
            if (!m_EdgeLaneData.TryGetComponent(lane, out EdgeLane edgeLane))
            {
                return 0;
            }

            float direction = edgeLane.m_EdgeDelta.y - edgeLane.m_EdgeDelta.x;
            if (direction > 0f)
            {
                return 1;
            }

            if (direction < 0f)
            {
                return -1;
            }

            return 0;
        }

        private string DescribeLaneKind(Entity lane)
        {
            if (lane == Entity.Null)
            {
                return "none";
            }

            if (m_ParkingLaneData.HasComponent(lane))
            {
                return "parking-lane";
            }

            if (m_GarageLaneData.HasComponent(lane) &&
                m_ConnectionLaneData.TryGetComponent(lane, out ConnectionLane garageConnectionLane) &&
                (garageConnectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
            {
                return "garage+parking-connection";
            }

            if (m_GarageLaneData.HasComponent(lane))
            {
                return "garage-lane";
            }

            if (m_ConnectionLaneData.TryGetComponent(lane, out ConnectionLane connectionLane))
            {
                if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
                {
                    return "parking-connection";
                }

                if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0)
                {
                    return "road-connection";
                }

                return "access-connection";
            }

            if (m_EdgeLaneData.HasComponent(lane) && m_CarLaneData.HasComponent(lane))
            {
                return "road";
            }

            return "lane";
        }

        private bool TryGetOppositeFlowNearMissReason(
            VehicleLaneHistory history,
            out OppositeFlowNearMissReason failReason)
        {
            failReason = OppositeFlowNearMissReason.None;

            if (!m_EdgeLaneData.HasComponent(history.m_PreviousLane))
            {
                failReason = OppositeFlowNearMissReason.MissingPreviousEdgeLane;
                return true;
            }

            if (!m_EdgeLaneData.HasComponent(history.m_CurrentLane))
            {
                failReason = OppositeFlowNearMissReason.MissingCurrentEdgeLane;
                return true;
            }

            if (!m_CarLaneData.TryGetComponent(history.m_PreviousLane, out CarLane previousCarLane))
            {
                failReason = OppositeFlowNearMissReason.MissingPreviousCarLane;
                return true;
            }

            if (!m_CarLaneData.TryGetComponent(history.m_CurrentLane, out CarLane currentCarLane))
            {
                failReason = OppositeFlowNearMissReason.MissingCurrentCarLane;
                return true;
            }

            if (history.m_PreviousLaneOwner == Entity.Null ||
                history.m_PreviousLaneOwner != history.m_CurrentLaneOwner)
            {
                failReason = OppositeFlowNearMissReason.DifferentOwner;
                return true;
            }

            int previousDirectionSign = GetDirectionSign(history.m_PreviousLane);
            int currentDirectionSign = GetDirectionSign(history.m_CurrentLane);

            if (previousDirectionSign * currentDirectionSign >= 0)
            {
                failReason = OppositeFlowNearMissReason.NotOppositeDirection;
                return true;
            }

            if (previousCarLane.m_CarriagewayGroup != currentCarLane.m_CarriagewayGroup)
            {
                failReason = OppositeFlowNearMissReason.DifferentCarriagewayGroup;
                return true;
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

        private void CaptureDirectIllegalEgressApplyMarker(
            Entity vehicle,
            VehicleLaneHistory history,
            LaneTransitionViolationReasonCode reasonCode,
            out IllegalEgressApplyMode mode,
            out Entity originLane,
            out Entity roadLane)
        {
            mode = IllegalEgressApplyMode.None;
            originLane = Entity.Null;
            roadLane = Entity.Null;

            if (!IsEligibleForPendingOrdinaryEgress(vehicle) ||
                !IsIllegalEgressReason(reasonCode) ||
                !IsRoadLane(history.m_CurrentLane))
            {
                return;
            }

            mode = IllegalEgressApplyMode.Direct;
            originLane = history.m_PreviousLane;
            roadLane = history.m_CurrentLane;
        }

        private void WritePendingOrdinaryEgressIfNeeded(
            Entity vehicle,
            VehicleLaneHistory history,
            ref LaneTransitionAnalysisState analysisState)
        {
            if (TrySeedPendingOrdinaryEgressFromUndercroftBridge(
                    vehicle,
                    history,
                    ref analysisState))
            {
                return;
            }

            bool currentIsNarrowIntermediate =
                IsNarrowOrdinaryEgressIntermediate(history.m_CurrentLane);
            bool previousIsAccessOrigin =
                IsAccessOrigin(history.m_PreviousLane);

            if (TryRememberPendingUndercroftOrdinaryEgressBridge(
                    vehicle,
                    history,
                    ref analysisState))
            {
                return;
            }

            if (m_DeliveryTruckData.HasComponent(vehicle) &&
                previousIsAccessOrigin &&
                currentIsNarrowIntermediate)
            {
                string message =
                    "[TRUCK_EGRESS_CARRY_SKIPPED] " +
                    $"vehicle={FocusedLoggingService.FormatEntity(vehicle)} " +
                    $"previousLane={FocusedLoggingService.FormatEntity(history.m_PreviousLane)} " +
                    $"currentLane={FocusedLoggingService.FormatEntity(history.m_CurrentLane)} " +
                    $"previousLaneKind={DescribeLaneKind(history.m_PreviousLane)} " +
                    $"currentLaneKind={DescribeLaneKind(history.m_CurrentLane)} " +
                    $"previousConnectionFlags={FormatConnectionLaneFlags(history.m_PreviousLane)} " +
                    $"currentConnectionFlags={FormatConnectionLaneFlags(history.m_CurrentLane)} " +
                    $"previousIsAccessOrigin={previousIsAccessOrigin} " +
                    $"currentIsNarrowIntermediate={currentIsNarrowIntermediate} " +
                    $"reason=WouldNeedCarryButDeliveryTruckExcluded";
                EnforcementLoggingPolicy.RecordEnforcementEvent(message, vehicle);
            }

            if (!IsEligibleForPendingOrdinaryEgress(vehicle) ||
                !previousIsAccessOrigin ||
                !currentIsNarrowIntermediate)
            {
                ClearPendingOrdinaryEgress(ref analysisState);
                ClearPendingUndercroftOrdinaryEgressBridge(ref analysisState);
                return;
            }

            ClearPendingUndercroftOrdinaryEgressBridge(ref analysisState);
            analysisState.m_PendingOrdinaryEgressOriginLane = history.m_PreviousLane;
            analysisState.m_PendingOrdinaryEgressCorridorFailsafeBudget =
                PendingOrdinaryEgressCorridorFailsafeBudget;
        }

        private void AdvancePendingOrdinaryEgressCorridor(
            Entity vehicle,
            VehicleLaneHistory history,
            ref LaneTransitionAnalysisState analysisState,
            Entity pendingOriginLane)
        {
            if (analysisState.m_PendingOrdinaryEgressCorridorFailsafeBudget > 0)
            {
                analysisState.m_PendingOrdinaryEgressCorridorFailsafeBudget -= 1;
            }

            if (analysisState.m_PendingOrdinaryEgressCorridorFailsafeBudget == 0)
            {
                ClearPendingOrdinaryEgress(ref analysisState);
            }
        }

        private bool HasPendingOrdinaryEgress(LaneTransitionAnalysisState analysisState)
        {
            return analysisState.m_PendingOrdinaryEgressCorridorFailsafeBudget > 0 &&
                analysisState.m_PendingOrdinaryEgressOriginLane != Entity.Null;
        }

        private bool HasPendingUndercroftOrdinaryEgressBridge(
            LaneTransitionAnalysisState analysisState)
        {
            return analysisState.m_PendingUndercroftOrdinaryEgressBridgeConnectionLane != Entity.Null &&
                analysisState.m_PendingUndercroftOrdinaryEgressBridgeOriginLane != Entity.Null;
        }

        private void ClearPendingOrdinaryEgress(ref LaneTransitionAnalysisState analysisState)
        {
            analysisState.m_PendingOrdinaryEgressCorridorFailsafeBudget = 0;
            analysisState.m_PendingOrdinaryEgressOriginLane = Entity.Null;
        }

        private void ClearPendingUndercroftOrdinaryEgressBridge(
            ref LaneTransitionAnalysisState analysisState)
        {
            analysisState.m_PendingUndercroftOrdinaryEgressBridgeConnectionLane = Entity.Null;
            analysisState.m_PendingUndercroftOrdinaryEgressBridgeOriginLane = Entity.Null;
        }

        private bool TryRememberPendingUndercroftOrdinaryEgressBridge(
            Entity vehicle,
            VehicleLaneHistory history,
            ref LaneTransitionAnalysisState analysisState)
        {
            if (!IsEligibleForPendingOrdinaryEgress(vehicle) ||
                !m_GarageLaneData.HasComponent(history.m_PreviousLane) ||
                !IsQualifyingUndercroftOrdinaryEgressBridgeConnection(history.m_CurrentLane) ||
                history.m_PreviousLaneOwner == Entity.Null ||
                history.m_PreviousLaneOwner != history.m_CurrentLaneOwner)
            {
                return false;
            }

            analysisState.m_PendingUndercroftOrdinaryEgressBridgeOriginLane =
                history.m_PreviousLane;
            analysisState.m_PendingUndercroftOrdinaryEgressBridgeConnectionLane =
                history.m_CurrentLane;
            return true;
        }

        private bool TrySeedPendingOrdinaryEgressFromUndercroftBridge(
            Entity vehicle,
            VehicleLaneHistory history,
            ref LaneTransitionAnalysisState analysisState)
        {
            if (!HasPendingUndercroftOrdinaryEgressBridge(analysisState))
            {
                return false;
            }

            Entity bridgeConnectionLane =
                analysisState.m_PendingUndercroftOrdinaryEgressBridgeConnectionLane;
            Entity bridgeOriginLane =
                analysisState.m_PendingUndercroftOrdinaryEgressBridgeOriginLane;

            if (!IsEligibleForPendingOrdinaryEgress(vehicle) ||
                history.m_PreviousLane != bridgeConnectionLane ||
                !IsNarrowOrdinaryEgressIntermediate(history.m_CurrentLane))
            {
                ClearPendingUndercroftOrdinaryEgressBridge(ref analysisState);
                return false;
            }

            ClearPendingUndercroftOrdinaryEgressBridge(ref analysisState);
            analysisState.m_PendingOrdinaryEgressOriginLane = bridgeOriginLane;
            analysisState.m_PendingOrdinaryEgressCorridorFailsafeBudget =
                PendingOrdinaryEgressCorridorFailsafeBudget;
            return true;
        }

        private bool IsEligibleForPendingOrdinaryEgress(Entity vehicle)
        {
            return !m_DeliveryTruckData.HasComponent(vehicle);
        }

        private bool IsRoadLane(Entity lane)
        {
            return m_EdgeLaneData.HasComponent(lane) && m_CarLaneData.HasComponent(lane);
        }

        private bool IsQualifyingUndercroftOrdinaryEgressBridgeConnection(Entity lane)
        {
            if (!m_ConnectionLaneData.TryGetComponent(lane, out ConnectionLane connectionLane))
            {
                return false;
            }

            ConnectionLaneFlags requiredFlags =
                ConnectionLaneFlags.Road |
                ConnectionLaneFlags.Inside |
                ConnectionLaneFlags.AllowEnter;
            if ((connectionLane.m_Flags & requiredFlags) != requiredFlags)
            {
                return false;
            }

            ConnectionLaneFlags excludedFlags =
                ConnectionLaneFlags.Parking |
                ConnectionLaneFlags.Pedestrian |
                ConnectionLaneFlags.AllowCargo |
                ConnectionLaneFlags.AllowExit;
            return (connectionLane.m_Flags & excludedFlags) == 0;
        }

        private string FormatConnectionLaneFlags(Entity lane)
        {
            return m_ConnectionLaneData.TryGetComponent(lane, out ConnectionLane connectionLane)
                ? connectionLane.m_Flags.ToString()
                : "None";
        }

        private static bool IsIllegalEgressReason(LaneTransitionViolationReasonCode reasonCode)
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

        private bool IsNarrowOrdinaryEgressIntermediate(Entity lane)
        {
            if (lane == Entity.Null ||
                IsRoadLane(lane) ||
                IsAccessOrigin(lane) ||
                m_ConnectionLaneData.HasComponent(lane))
            {
                return false;
            }

            return true;
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
