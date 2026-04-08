using System.Text;
using Game;
using Game.Common;
using Game.Net;
using Game.Pathfind;
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
        private ComponentLookup<PathInformation> m_PathInformationData;
        private BufferLookup<Game.Simulation.ServiceDispatch> m_ServiceDispatchData;
        private ComponentLookup<PersonalCar> m_PersonalCarData;
        private ComponentLookup<Target> m_TargetData;
        private ComponentLookup<PathOwner> m_PathOwnerData;
        private ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;
        private ComponentLookup<GarbageTruck> m_GarbageTruckData;
        private ComponentLookup<MaintenanceVehicle> m_MaintenanceVehicleData;
        private ComponentLookup<PostVan> m_PostVanData;
        private ComponentLookup<LaneTransitionAnalysisState> m_AnalysisStateData;
        private PublicTransportLaneVehicleTypeLookups m_IllegalAccessTypeLookups;
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
            m_PathInformationData = GetComponentLookup<PathInformation>(true);
            m_ServiceDispatchData = GetBufferLookup<Game.Simulation.ServiceDispatch>(true);
            m_PersonalCarData = GetComponentLookup<PersonalCar>(true);
            m_TargetData = GetComponentLookup<Target>(true);
            m_PathOwnerData = GetComponentLookup<PathOwner>(true);
            m_SpawnLocationData = GetComponentLookup<Game.Objects.SpawnLocation>(true);
            m_GarbageTruckData = GetComponentLookup<GarbageTruck>(true);
            m_MaintenanceVehicleData = GetComponentLookup<MaintenanceVehicle>(true);
            m_PostVanData = GetComponentLookup<PostVan>(true);
            m_AnalysisStateData = GetComponentLookup<LaneTransitionAnalysisState>();
            m_IllegalAccessTypeLookups = PublicTransportLaneVehicleTypeLookups.Create(this);
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
                m_PathInformationData.Update(this);
                m_ServiceDispatchData.Update(this);
                m_PersonalCarData.Update(this);
                m_TargetData.Update(this);
                m_PathOwnerData.Update(this);
                m_SpawnLocationData.Update(this);
                m_GarbageTruckData.Update(this);
                m_MaintenanceVehicleData.Update(this);
                m_PostVanData.Update(this);
                m_IllegalAccessTypeLookups.Update(this);

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

        private void MaybeLogRealizedIngressTrace(
            Entity vehicle,
            CarCurrentLane currentLane,
            VehicleLaneHistory history)
        {
            if (!EnforcementLoggingPolicy.ShouldLogVehicleSpecificEnforcementEvent(vehicle))
            {
                return;
            }

            MidBlockCrossingPolicy.TraceIllegalIngressTransition(
                EntityManager,
                history.m_PreviousLane,
                history.m_CurrentLane,
                out bool previousIsRoad,
                out bool currentIsAccessTarget,
                out bool ingressDetectResult,
                out MidBlockCrossingPolicy.AccessIngressTraceFailReason failReason,
                out LaneTransitionViolationReasonCode reasonCode);

            bool currentIsConnection =
                m_ConnectionLaneData.HasComponent(history.m_CurrentLane);

            bool currentIsParkingFamily =
                m_ParkingLaneData.HasComponent(history.m_CurrentLane) ||
                m_GarageLaneData.HasComponent(history.m_CurrentLane) ||
                (m_ConnectionLaneData.TryGetComponent(history.m_CurrentLane, out ConnectionLane currentConnection) &&
                (currentConnection.m_Flags & ConnectionLaneFlags.Parking) != 0);

            bool currentIsRoad =
                IsRoadLane(history.m_CurrentLane);

            bool ownerChanged =
                history.m_PreviousLaneOwner != Entity.Null &&
                history.m_PreviousLaneOwner != history.m_CurrentLaneOwner;

            bool isLateNonParkingIngressSeam =
                previousIsRoad &&
                !currentIsConnection &&
                !currentIsRoad &&
                !currentIsParkingFamily;

            if (isLateNonParkingIngressSeam)
            {
                EnforcementLoggingPolicy.RecordEnforcementEvent(
                    "[NON_PARKING_BUILDING_INGRESS_LATE_SEAM_PROBE] " +
                    $"vehicle={vehicle} " +
                    $"isDeliveryTruck={m_DeliveryTruckData.HasComponent(vehicle)} " +
                    $"previousLane={history.m_PreviousLane} " +
                    $"currentLane={history.m_CurrentLane} " +
                    $"previousOwner={history.m_PreviousLaneOwner} " +
                    $"currentOwner={history.m_CurrentLaneOwner} " +
                    $"ownerChanged={ownerChanged} " +
                    $"previousLaneKind={DescribeLaneKind(history.m_PreviousLane)} " +
                    $"currentLaneKind={DescribeLaneKind(history.m_CurrentLane)} " +
                    $"previousConnectionFlags={FormatConnectionLaneFlags(history.m_PreviousLane)} " +
                    $"currentConnectionFlags={FormatConnectionLaneFlags(history.m_CurrentLane)} " +
                    $"previousIsRoad={previousIsRoad} " +
                    $"currentIsRoad={currentIsRoad} " +
                    $"currentIsAccessTarget={currentIsAccessTarget} " +
                    $"ingressDetectResult={ingressDetectResult} " +
                    $"failReason={failReason} " +
                    $"reasonCode={reasonCode}",
                    vehicle);

                MaybeLogOrdinaryCarSemanticLateSeam(vehicle, currentLane, "IngressLate");
                MaybeLogServiceVehicleSemanticLateSeam(vehicle, "IngressLate");
            }

            if (!previousIsRoad ||
                !currentIsConnection ||
                currentIsParkingFamily)
            {
                return;
            }

            EnforcementLoggingPolicy.RecordEnforcementEvent(
                "[NON_PARKING_BUILDING_INGRESS_TARGET_PROBE] " +
                $"vehicle={vehicle} " +
                $"isDeliveryTruck={m_DeliveryTruckData.HasComponent(vehicle)} " +
                $"previousLane={history.m_PreviousLane} " +
                $"currentLane={history.m_CurrentLane} " +
                $"previousOwner={history.m_PreviousLaneOwner} " +
                $"currentOwner={history.m_CurrentLaneOwner} " +
                $"previousLaneKind={DescribeLaneKind(history.m_PreviousLane)} " +
                $"currentLaneKind={DescribeLaneKind(history.m_CurrentLane)} " +
                $"previousConnectionFlags={FormatConnectionLaneFlags(history.m_PreviousLane)} " +
                $"currentConnectionFlags={FormatConnectionLaneFlags(history.m_CurrentLane)} " +
                $"previousIsRoad={previousIsRoad} " +
                $"currentIsAccessTarget={currentIsAccessTarget} " +
                $"ingressDetectResult={ingressDetectResult} " +
                $"failReason={failReason} " +
                $"reasonCode={reasonCode}",
                vehicle);
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
            ClearPendingGarageConnectionEgressBridge(ref analysisState);
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

            if (!m_CarData.TryGetComponent(vehicle, out Car car))
            {
                return;
            }

            if (EmergencyVehiclePolicy.IsEmergencyVehicle(car))
            {
                ClearPendingOrdinaryEgress(ref analysisState);
                ClearPendingGarageConnectionEgressBridge(ref analysisState);
                EntityManager.SetComponentData(vehicle, analysisState);
                return;
            }

            MaybeLogRealizedOppositeFlowNearMiss(vehicle, history);
            MaybeLogRealizedEgressTrace(vehicle, currentLane, history);
            MaybeLogRealizedIngressTrace(vehicle, currentLane, history);

            IllegalEgressApplyMode illegalEgressMode = IllegalEgressApplyMode.None;
            Entity illegalEgressOriginLane = Entity.Null;
            Entity illegalEgressRoadLane = Entity.Null;
            bool accessExcluded =
                IllegalAccessEnforcementExclusionPolicy.IsExcluded(
                    vehicle,
                    car,
                    ref m_IllegalAccessTypeLookups);

            if (accessExcluded)
            {
                ClearPendingOrdinaryEgress(ref analysisState);
                ClearPendingGarageConnectionEgressBridge(ref analysisState);
            }

            bool hasMidBlockViolation = false;
            LaneTransitionViolationReasonCode reasonCode = LaneTransitionViolationReasonCode.None;

            if (!accessExcluded)
            {
                hasMidBlockViolation =
                    TryDetectIllegalAccessMidBlockCrossing(
                        history,
                        out reasonCode);

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
                else
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
            }

            if (!hasMidBlockViolation)
            {
                hasMidBlockViolation =
                    TryDetectOppositeFlowMidBlockCrossing(
                        history,
                        out reasonCode);
            }

            if (hasMidBlockViolation)
            {
                ClearPendingOrdinaryEgress(ref analysisState);
                ClearPendingGarageConnectionEgressBridge(ref analysisState);
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
            else if (!accessExcluded && !HasPendingOrdinaryEgress(analysisState))
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
                    $"Intersection transition candidate: vehicle={vehicle}, " +
                    $"fromLane={history.m_PreviousLane}, " +
                    $"toLane={history.m_CurrentLane}, " +
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
                $"vehicle={vehicle} " +
                $"previousLane={history.m_PreviousLane} " +
                $"currentLane={history.m_CurrentLane} " +
                $"previousOwner={history.m_PreviousLaneOwner} " +
                $"currentOwner={history.m_CurrentLaneOwner} " +
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
                $"vehicle={vehicle} " +
                $"reason={reasonCode} " +
                $"previousLane={history.m_PreviousLane} " +
                $"currentLane={history.m_CurrentLane} " +
                $"previousOwner={history.m_PreviousLaneOwner} " +
                $"currentOwner={history.m_CurrentLaneOwner} " +
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

                bool previousIsConnection =
                    m_ConnectionLaneData.HasComponent(history.m_PreviousLane);

                bool previousIsParkingFamily =
                    m_ParkingLaneData.HasComponent(history.m_PreviousLane) ||
                    m_GarageLaneData.HasComponent(history.m_PreviousLane) ||
                    (m_ConnectionLaneData.TryGetComponent(history.m_PreviousLane, out ConnectionLane previousConnection) &&
                    (previousConnection.m_Flags & ConnectionLaneFlags.Parking) != 0);

                bool ownerChanged =
                    history.m_PreviousLaneOwner != Entity.Null &&
                    history.m_PreviousLaneOwner != history.m_CurrentLaneOwner;

                bool isLateNonParkingEgressSeam =
                    !previousIsAccessOrigin &&
                    !IsRoadLane(history.m_PreviousLane) &&
                    currentIsRoad;

                if (isLateNonParkingEgressSeam)
                {
                    EnforcementLoggingPolicy.RecordEnforcementEvent(
                        "[NON_PARKING_BUILDING_EGRESS_LATE_SEAM_PROBE] " +
                        $"vehicle={vehicle} " +
                        $"isDeliveryTruck={m_DeliveryTruckData.HasComponent(vehicle)} " +
                        $"previousLane={history.m_PreviousLane} " +
                        $"currentLane={history.m_CurrentLane} " +
                        $"previousOwner={history.m_PreviousLaneOwner} " +
                        $"currentOwner={history.m_CurrentLaneOwner} " +
                        $"ownerChanged={ownerChanged} " +
                        $"previousLaneKind={DescribeLaneKind(history.m_PreviousLane)} " +
                        $"currentLaneKind={DescribeLaneKind(history.m_CurrentLane)} " +
                        $"previousConnectionFlags={FormatConnectionLaneFlags(history.m_PreviousLane)} " +
                        $"currentConnectionFlags={FormatConnectionLaneFlags(history.m_CurrentLane)} " +
                        $"previousIsAccessOrigin={previousIsAccessOrigin} " +
                        $"currentIsRoad={currentIsRoad} " +
                        $"egressDetectResult={egressDetectResult} " +
                        $"failReason={failReason}",
                        vehicle);

                    MaybeLogOrdinaryCarSemanticLateSeam(vehicle, currentLane, "EgressLate");
                    MaybeLogServiceVehicleSemanticLateSeam(vehicle, "EgressLate");
                }

                if (previousIsConnection && !previousIsParkingFamily)
                {
                    string nonParkingSourceProbeMessage =
                        "[NON_PARKING_BUILDING_EGRESS_SOURCE_PROBE] " +
                        $"vehicle={vehicle} " +
                        $"isDeliveryTruck={m_DeliveryTruckData.HasComponent(vehicle)} " +
                        $"previousLane={history.m_PreviousLane} " +
                        $"currentLane={history.m_CurrentLane} " +
                        $"previousLaneKind={DescribeLaneKind(history.m_PreviousLane)} " +
                        $"currentLaneKind={DescribeLaneKind(history.m_CurrentLane)} " +
                        $"previousConnectionFlags={FormatConnectionLaneFlags(history.m_PreviousLane)} " +
                        $"currentConnectionFlags={FormatConnectionLaneFlags(history.m_CurrentLane)} " +
                        $"previousIsAccessOrigin={previousIsAccessOrigin} " +
                        $"currentIsRoad={currentIsRoad} " +
                        $"egressDetectResult={egressDetectResult} " +
                        $"failReason={failReason}";

                    EnforcementLoggingPolicy.RecordEnforcementEvent(nonParkingSourceProbeMessage, vehicle);
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

            string vehicleText = vehicle.ToString();
            string accessTraceMessage =
                "[ACCESS_EGRESS_REALIZED_TRACE] " +
                $"vehicle={vehicleText} " +
                $"vehicleId={vehicleText} " +
                $"previousLane={history.m_PreviousLane} " +
                $"currentLane={history.m_CurrentLane} " +
                $"previousOwner={history.m_PreviousLaneOwner} " +
                $"currentOwner={history.m_CurrentLaneOwner} " +
                $"previousLaneKind={DescribeLaneKind(history.m_PreviousLane)} " +
                $"currentLaneKind={DescribeLaneKind(history.m_CurrentLane)} " +
                $"previousIsAccessOrigin={previousIsAccessOrigin} " +
                $"currentIsRoad={currentIsRoad} " +
                $"egressDetectResult={egressDetectResult} " +
                $"failReason={failReason}";

            EnforcementLoggingPolicy.RecordEnforcementEvent(accessTraceMessage, vehicle);
        }

        private void MaybeLogOrdinaryCarSemanticLateSeam(
            Entity vehicle,
            CarCurrentLane currentLane,
            string seamKind)
        {
            if (!m_PersonalCarData.TryGetComponent(vehicle, out PersonalCar personalCar))
            {
                return;
            }

            bool transporting =
                (personalCar.m_State & PersonalCarFlags.Transporting) != 0;
            bool boarding =
                (personalCar.m_State & PersonalCarFlags.Boarding) != 0;
            bool disembarking =
                (personalCar.m_State & PersonalCarFlags.Disembarking) != 0;
            bool homeTarget =
                (personalCar.m_State & PersonalCarFlags.HomeTarget) != 0;

            Entity targetEntity = Entity.Null;
            if (m_TargetData.TryGetComponent(vehicle, out Target target))
            {
                targetEntity = target.m_Target;
            }

            bool hasPathOwner =
                m_PathOwnerData.TryGetComponent(vehicle, out PathOwner pathOwner);
            bool parkingSpaceReached =
                hasPathOwner && VehicleUtils.ParkingSpaceReached(currentLane, pathOwner);
            bool pathEndReached =
                VehicleUtils.PathEndReached(currentLane);
            bool hasSpawnLocation =
                currentLane.m_Lane != Entity.Null &&
                m_SpawnLocationData.HasComponent(currentLane.m_Lane);

            string hypothesis =
                pathEndReached && hasSpawnLocation && (transporting || disembarking) && !parkingSpaceReached
                    ? "TransportDropoffLike"
                    : parkingSpaceReached || boarding
                        ? "ParkingAdjacent"
                        : "Unresolved";

            string message =
                "[ORDINARY_CAR_ACCESS_LATE_SEMANTIC_PROBE] " +
                $"vehicle={vehicle} " +
                $"seamKind={seamKind} " +
                $"target={targetEntity} " +
                $"transporting={transporting} " +
                $"boarding={boarding} " +
                $"disembarking={disembarking} " +
                $"homeTarget={homeTarget} " +
                $"hasPathOwner={hasPathOwner} " +
                $"parkingSpaceReached={parkingSpaceReached} " +
                $"pathEndReached={pathEndReached} " +
                $"hasSpawnLocation={hasSpawnLocation} " +
                $"hypothesis={hypothesis}";

            EnforcementLoggingPolicy.RecordEnforcementEvent(message, vehicle);
        }

        private void MaybeLogServiceVehicleSemanticLateSeam(
            Entity vehicle,
            string seamKind)
        {
            bool isDeliveryTruck =
                m_DeliveryTruckData.TryGetComponent(vehicle, out DeliveryTruck deliveryTruck);
            bool isGarbageTruck =
                m_GarbageTruckData.TryGetComponent(vehicle, out GarbageTruck garbageTruck);
            bool isMaintenanceVehicle =
                m_MaintenanceVehicleData.TryGetComponent(vehicle, out MaintenanceVehicle maintenanceVehicle);
            bool isPostVan =
                m_PostVanData.TryGetComponent(vehicle, out PostVan postVan);

            if (!isDeliveryTruck &&
                !isGarbageTruck &&
                !isMaintenanceVehicle &&
                !isPostVan)
            {
                return;
            }

            Entity targetEntity = Entity.Null;
            if (m_TargetData.TryGetComponent(vehicle, out Target target))
            {
                targetEntity = target.m_Target;
            }

            Entity ownerEntity = Entity.Null;
            if (m_OwnerData.TryGetComponent(vehicle, out Owner owner))
            {
                ownerEntity = owner.m_Owner;
            }

            bool hasPathInformation =
                m_PathInformationData.TryGetComponent(vehicle, out PathInformation pathInformation);
            Entity pathOrigin =
                hasPathInformation ? pathInformation.m_Origin : Entity.Null;
            Entity pathDestination =
                hasPathInformation ? pathInformation.m_Destination : Entity.Null;

            bool hasServiceDispatch =
                m_ServiceDispatchData.HasBuffer(vehicle);
            int serviceDispatchCount =
                hasServiceDispatch ? m_ServiceDispatchData[vehicle].Length : 0;

            bool deliveryReturning =
                isDeliveryTruck &&
                (deliveryTruck.m_State & DeliveryTruckFlags.Returning) != 0;
            bool deliveryDelivering =
                isDeliveryTruck &&
                (deliveryTruck.m_State & DeliveryTruckFlags.Delivering) != 0;

            bool garbageReturning =
                isGarbageTruck &&
                (garbageTruck.m_State & GarbageTruckFlags.Returning) != 0;

            bool maintenanceReturning =
                isMaintenanceVehicle &&
                (maintenanceVehicle.m_State & MaintenanceVehicleFlags.Returning) != 0;
            bool maintenanceTransformTarget =
                isMaintenanceVehicle &&
                (maintenanceVehicle.m_State & MaintenanceVehicleFlags.TransformTarget) != 0;
            bool maintenanceEdgeTarget =
                isMaintenanceVehicle &&
                (maintenanceVehicle.m_State & MaintenanceVehicleFlags.EdgeTarget) != 0;

            bool postReturning =
                isPostVan &&
                (postVan.m_State & PostVanFlags.Returning) != 0;
            bool postDelivering =
                isPostVan &&
                (postVan.m_State & PostVanFlags.Delivering) != 0;
            bool postCollecting =
                isPostVan &&
                (postVan.m_State & PostVanFlags.Collecting) != 0;

            bool returnLike =
                deliveryReturning ||
                garbageReturning ||
                maintenanceReturning ||
                postReturning;

            bool workLike =
                deliveryDelivering ||
                maintenanceTransformTarget ||
                maintenanceEdgeTarget ||
                postDelivering ||
                postCollecting;

            bool hasCommonServiceContext =
                targetEntity != Entity.Null ||
                ownerEntity != Entity.Null ||
                hasPathInformation ||
                hasServiceDispatch;

            string hypothesis =
                returnLike
                    ? "ReturnLike"
                    : workLike
                        ? "WorkLike"
                        : hasCommonServiceContext
                            ? "ServiceContextOnly"
                            : "Unresolved";

            StringBuilder message = new StringBuilder(512);
            message.Append("[NON_PARKING_SERVICE_ACCESS_LATE_SEMANTIC_PROBE] ");
            message.Append($"vehicle={vehicle} ");
            message.Append($"seamKind={seamKind} ");
            message.Append($"target={targetEntity} ");
            message.Append($"owner={ownerEntity} ");
            message.Append($"hasPathInformation={hasPathInformation} ");
            message.Append($"pathOrigin={pathOrigin} ");
            message.Append($"pathDestination={pathDestination} ");
            message.Append($"hasServiceDispatch={hasServiceDispatch} ");
            message.Append($"serviceDispatchCount={serviceDispatchCount}");

            if (isDeliveryTruck)
            {
                message.Append($" isDeliveryTruck=true");
                message.Append($" deliveryReturning={deliveryReturning}");
                message.Append($" deliveryDelivering={deliveryDelivering}");
            }

            if (isGarbageTruck)
            {
                message.Append($" isGarbageTruck=true");
                message.Append($" garbageReturning={garbageReturning}");
            }

            if (isMaintenanceVehicle)
            {
                message.Append($" isMaintenanceVehicle=true");
                message.Append($" maintenanceReturning={maintenanceReturning}");
                message.Append($" maintenanceTransformTarget={maintenanceTransformTarget}");
                message.Append($" maintenanceEdgeTarget={maintenanceEdgeTarget}");
            }

            if (isPostVan)
            {
                message.Append($" isPostVan=true");
                message.Append($" postReturning={postReturning}");
                message.Append($" postDelivering={postDelivering}");
                message.Append($" postCollecting={postCollecting}");
            }

            message.Append($" hypothesis={hypothesis}");

            EnforcementLoggingPolicy.RecordEnforcementEvent(message.ToString(), vehicle);
        }

        private bool TryDetectIllegalAccessMidBlockCrossing(
            VehicleLaneHistory history,
            out LaneTransitionViolationReasonCode reasonCode)
        {
            return MidBlockCrossingPolicy.TryGetIllegalAccessTransition(
                EntityManager,
                history.m_PreviousLane,
                history.m_CurrentLane,
                out reasonCode);
        }

        private bool TryDetectOppositeFlowMidBlockCrossing(
            VehicleLaneHistory history,
            out LaneTransitionViolationReasonCode reasonCode)
        {
            return MidBlockCrossingPolicy.TryGetOppositeFlowSameRoadSegmentTransition(
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
                $"vehicle={vehicle} " +
                $"reason={reasonCode} " +
                $"previousLane={history.m_PreviousLane} " +
                $"currentLane={history.m_CurrentLane} " +
                $"previousOwner={history.m_PreviousLaneOwner} " +
                $"currentOwner={history.m_CurrentLaneOwner} " +
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

            if (!IsIllegalEgressReason(reasonCode) ||
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
            if (TrySeedPendingOrdinaryEgressFromGarageConnectionBridge(
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

            if (TryRememberPendingGarageConnectionEgressBridge(
                    vehicle,
                    history,
                    ref analysisState))
            {
                return;
            }

            if (!previousIsAccessOrigin ||
                !currentIsNarrowIntermediate)
            {
                ClearPendingOrdinaryEgress(ref analysisState);
                ClearPendingGarageConnectionEgressBridge(ref analysisState);
                return;
            }

            ClearPendingGarageConnectionEgressBridge(ref analysisState);
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

        private bool HasPendingGarageConnectionEgressBridge(
            LaneTransitionAnalysisState analysisState)
        {
            return analysisState.m_PendingGarageConnectionEgressBridgeConnectionLane != Entity.Null &&
                analysisState.m_PendingGarageConnectionEgressBridgeOriginLane != Entity.Null;
        }

        private void ClearPendingOrdinaryEgress(ref LaneTransitionAnalysisState analysisState)
        {
            analysisState.m_PendingOrdinaryEgressCorridorFailsafeBudget = 0;
            analysisState.m_PendingOrdinaryEgressOriginLane = Entity.Null;
        }

        private void ClearPendingGarageConnectionEgressBridge(
            ref LaneTransitionAnalysisState analysisState)
        {
            analysisState.m_PendingGarageConnectionEgressBridgeConnectionLane = Entity.Null;
            analysisState.m_PendingGarageConnectionEgressBridgeOriginLane = Entity.Null;
        }

        private bool TryRememberPendingGarageConnectionEgressBridge(
            Entity vehicle,
            VehicleLaneHistory history,
            ref LaneTransitionAnalysisState analysisState)
        {
            if (!CanUseLegacyGarageConnectionEgressBridgeQuarantine(vehicle) ||
                !m_GarageLaneData.HasComponent(history.m_PreviousLane) ||
                !IsQualifyingGarageConnectionEgressBridgeConnection(history.m_CurrentLane) ||
                history.m_PreviousLaneOwner == Entity.Null ||
                history.m_PreviousLaneOwner != history.m_CurrentLaneOwner)
            {
                return false;
            }

            analysisState.m_PendingGarageConnectionEgressBridgeOriginLane =
                history.m_PreviousLane;
            analysisState.m_PendingGarageConnectionEgressBridgeConnectionLane =
                history.m_CurrentLane;
            return true;
        }

        private bool TrySeedPendingOrdinaryEgressFromGarageConnectionBridge(
            Entity vehicle,
            VehicleLaneHistory history,
            ref LaneTransitionAnalysisState analysisState)
        {
            if (!HasPendingGarageConnectionEgressBridge(analysisState))
            {
                return false;
            }

            Entity bridgeConnectionLane =
                analysisState.m_PendingGarageConnectionEgressBridgeConnectionLane;
            Entity bridgeOriginLane =
                analysisState.m_PendingGarageConnectionEgressBridgeOriginLane;

            if (!CanUseLegacyGarageConnectionEgressBridgeQuarantine(vehicle) ||
                history.m_PreviousLane != bridgeConnectionLane ||
                !IsNarrowOrdinaryEgressIntermediate(history.m_CurrentLane))
            {
                ClearPendingGarageConnectionEgressBridge(ref analysisState);
                return false;
            }

            ClearPendingGarageConnectionEgressBridge(ref analysisState);
            analysisState.m_PendingOrdinaryEgressOriginLane = bridgeOriginLane;
            analysisState.m_PendingOrdinaryEgressCorridorFailsafeBudget =
                PendingOrdinaryEgressCorridorFailsafeBudget;
            return true;
        }

        private bool CanUseLegacyGarageConnectionEgressBridgeQuarantine(Entity vehicle)
        {
            return !m_DeliveryTruckData.HasComponent(vehicle);
        }

        private bool IsRoadLane(Entity lane)
        {
            return m_EdgeLaneData.HasComponent(lane) && m_CarLaneData.HasComponent(lane);
        }

        private bool IsQualifyingGarageConnectionEgressBridgeConnection(Entity lane)
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

