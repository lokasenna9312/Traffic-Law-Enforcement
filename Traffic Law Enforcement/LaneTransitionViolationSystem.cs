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
        private EntityTypeHandle m_EntityTypeHandle;
        private ComponentTypeHandle<VehicleLaneHistory> m_HistoryTypeHandle;
        private ComponentTypeHandle<CarCurrentLane> m_CurrentLaneTypeHandle;
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
                
            if (m_DeliveryTruckData.HasComponent(vehicle))
            {
                string deliveryTruckMessage =
                    "[TRUCK_EGRESS_DIRECT_TRACE] " +
                    $"vehicle={vehicle} " +
                    $"previousLane={history.m_PreviousLane} " +
                    $"currentLane={history.m_CurrentLane} " +
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

