using System.Collections.Generic;
using Game;
using Game.Pathfind;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public static class PublicTransportLaneExitPressureTelemetry
    {
        private static readonly HashSet<Entity> s_AwaitingPathRequestVehicles = new HashSet<Entity>();
        private static int s_AppliedCount;
        private static int s_SkippedAlreadyObsoleteCount;
        private static int s_SkippedPendingPathOwnerCount;
        private static int s_SkippedMissingPathOwnerCount;
        private static int s_CorrelatedPathRequestCount;
        private static int s_LastObservedRuntimeWorldGeneration = int.MinValue;

        public static int AppliedCount
        {
            get
            {
                EnsureCurrentWorld();
                return s_AppliedCount;
            }
        }

        public static int SkippedAlreadyObsoleteCount
        {
            get
            {
                EnsureCurrentWorld();
                return s_SkippedAlreadyObsoleteCount;
            }
        }

        public static int SkippedPendingPathOwnerCount
        {
            get
            {
                EnsureCurrentWorld();
                return s_SkippedPendingPathOwnerCount;
            }
        }

        public static int SkippedMissingPathOwnerCount
        {
            get
            {
                EnsureCurrentWorld();
                return s_SkippedMissingPathOwnerCount;
            }
        }

        public static int CorrelatedPathRequestCount
        {
            get
            {
                EnsureCurrentWorld();
                return s_CorrelatedPathRequestCount;
            }
        }

        public static int AwaitingPathRequestCount
        {
            get
            {
                EnsureCurrentWorld();
                return s_AwaitingPathRequestVehicles.Count;
            }
        }

        public static void Reset()
        {
            ResetForGeneration(EnforcementSaveDataSystem.RuntimeWorldGeneration);
        }

        public static void RecordApplied(Entity vehicle)
        {
            EnsureCurrentWorld();
            s_AppliedCount += 1;
            if (vehicle != Entity.Null)
            {
                s_AwaitingPathRequestVehicles.Add(vehicle);
            }
        }

        public static void RecordSkippedAlreadyObsolete()
        {
            EnsureCurrentWorld();
            s_SkippedAlreadyObsoleteCount += 1;
        }

        public static void RecordSkippedPendingPathOwner()
        {
            EnsureCurrentWorld();
            s_SkippedPendingPathOwnerCount += 1;
        }

        public static void RecordSkippedMissingPathOwner()
        {
            EnsureCurrentWorld();
            s_SkippedMissingPathOwnerCount += 1;
        }

        public static void TryRecordSubsequentPathRequest(Entity vehicle)
        {
            EnsureCurrentWorld();
            if (vehicle != Entity.Null &&
                s_AwaitingPathRequestVehicles.Remove(vehicle))
            {
                s_CorrelatedPathRequestCount += 1;
            }
        }

        private static void EnsureCurrentWorld()
        {
            int currentGeneration = EnforcementSaveDataSystem.RuntimeWorldGeneration;
            if (s_LastObservedRuntimeWorldGeneration != currentGeneration)
            {
                ResetForGeneration(currentGeneration);
            }
        }

        private static void ResetForGeneration(int generation)
        {
            s_LastObservedRuntimeWorldGeneration = generation;
            s_AppliedCount = 0;
            s_SkippedAlreadyObsoleteCount = 0;
            s_SkippedPendingPathOwnerCount = 0;
            s_SkippedMissingPathOwnerCount = 0;
            s_CorrelatedPathRequestCount = 0;
            s_AwaitingPathRequestVehicles.Clear();
        }
    }

    public partial class PublicTransportLaneExitPressureSystem : GameSystemBase
    {
        private EntityQuery m_ViolationQuery;
        private EntityQuery m_ChangedViolationQuery;
        private EntityTypeHandle m_EntityTypeHandle;
        private ComponentTypeHandle<Car> m_CarTypeHandle;
        private ComponentLookup<CarCurrentLane> m_CurrentLaneData;
        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;
        private ComponentLookup<PathOwner> m_PathOwnerData;
        private ComponentLookup<PublicTransportLaneViolation> m_ViolationData;
        private long m_NextPressureEvaluationDayTicks = long.MaxValue;
        private long m_LastThresholdDayTicks = long.MinValue;
        protected override void OnCreate()
        {
            base.OnCreate();
            m_ViolationQuery = GetEntityQuery(
                ComponentType.ReadOnly<Car>(),
                ComponentType.ReadWrite<PathOwner>(),
                ComponentType.ReadWrite<PublicTransportLaneViolation>());
            m_ChangedViolationQuery = GetEntityQuery(
                ComponentType.ReadOnly<PublicTransportLaneViolation>());
            m_ChangedViolationQuery.SetChangedVersionFilter(
                ComponentType.ReadOnly<PublicTransportLaneViolation>());
            RequireForUpdate(m_ViolationQuery);
            m_CurrentLaneData = GetComponentLookup<CarCurrentLane>(true);
            m_PathOwnerData = GetComponentLookup<PathOwner>();
            m_ViolationData = GetComponentLookup<PublicTransportLaneViolation>();
            m_TypeLookups = PublicTransportLaneVehicleTypeLookups.Create(this);
        }

        protected override void OnUpdate()
        {
            if (!ShouldApplyPublicTransportLaneIntervention() ||
                !EnforcementGameTime.IsInitialized)
            {
                return;
            }

            long thresholdDayTicks = GetExitPressureThresholdDayTicks();
            long currentDayTicks = EnforcementGameTime.CurrentTimestampDayTicks;

            if (CanSkipExitPressureUpdate(thresholdDayTicks, currentDayTicks))
            {
                return;
            }

            m_PathOwnerData.Update(this);
            m_ViolationData.Update(this);
            m_EntityTypeHandle = GetEntityTypeHandle();
            m_CarTypeHandle = GetComponentTypeHandle<Car>(true);

            NativeArray<ArchetypeChunk> chunks = m_ViolationQuery.ToArchetypeChunkArray(Allocator.Temp);
            long nextPressureEvaluationDayTicks = long.MaxValue;
            bool currentLaneDataReady = false;
            bool typeLookupsReady = false;
            try
            {
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex += 1)
                {
                    ArchetypeChunk chunk = chunks[chunkIndex];
                    NativeArray<Entity> vehicles = chunk.GetNativeArray(m_EntityTypeHandle);
                    NativeArray<Car> cars = chunk.GetNativeArray(ref m_CarTypeHandle);

                    for (int index = 0; index < vehicles.Length; index += 1)
                    {
                        Entity vehicle = vehicles[index];
                        Car car = cars[index];

                        if (EmergencyVehiclePolicy.IsEmergencyVehicle(car))
                        {
                            continue;
                        }

                        if (!m_ViolationData.TryGetComponent(vehicle, out PublicTransportLaneViolation violation))
                        {
                            continue;
                        }

                        TrackNextPressureEvaluation(
                            violation,
                            thresholdDayTicks,
                            ref nextPressureEvaluationDayTicks);

                        if (!ShouldApplyExitPressure(
                                violation,
                                currentDayTicks,
                                thresholdDayTicks,
                                out long elapsedDayTicks))
                        {
                            continue;
                        }

                        if (!TryGetEligiblePathOwner(vehicle, out PathOwner pathOwner))
                        {
                            continue;
                        }

                        if (TryApplyExitPressurePathObsolete(
                                vehicle,
                                ref pathOwner,
                                out PathFlags stateBefore))
                        {
                            RecordExitPressurePathObsoleteTrace(
                                vehicle,
                                car,
                                violation,
                                elapsedDayTicks,
                                thresholdDayTicks,
                                stateBefore,
                                pathOwner.m_State,
                                ref currentLaneDataReady,
                                ref typeLookupsReady);
                            TryLogExitPressureApplication(
                                vehicle,
                                violation.m_Lane,
                                elapsedDayTicks,
                                thresholdDayTicks);
                        }

                        MarkExitPressureApplied(vehicle, ref violation);
                    }
                }
            }
            finally
            {
                chunks.Dispose();
            }

            m_LastThresholdDayTicks = thresholdDayTicks;
            m_NextPressureEvaluationDayTicks = nextPressureEvaluationDayTicks;
        }

        private static bool ShouldApplyPublicTransportLaneIntervention()
        {
            return Mod.IsPublicTransportLaneEnforcementEnabled;
        }

        private static long GetExitPressureThresholdDayTicks()
        {
            return (long)System.Math.Round(
                System.Math.Max(
                    0f,
                    EnforcementGameplaySettingsService.Current
                        .PublicTransportLaneExitPressureThresholdDays) *
                EnforcementGameTime.DayTicksPerDay);
        }

        private bool CanSkipExitPressureUpdate(
            long thresholdDayTicks,
            long currentDayTicks)
        {
            return thresholdDayTicks == m_LastThresholdDayTicks &&
                currentDayTicks < m_NextPressureEvaluationDayTicks &&
                m_ChangedViolationQuery.IsEmptyIgnoreFilter;
        }

        private static void TrackNextPressureEvaluation(
            PublicTransportLaneViolation violation,
            long thresholdDayTicks,
            ref long nextPressureEvaluationDayTicks)
        {
            if (violation.m_ExitPressureApplied)
            {
                return;
            }

            long vehiclePressureEvaluationDayTicks =
                violation.m_StartDayTicks + thresholdDayTicks;
            if (vehiclePressureEvaluationDayTicks < nextPressureEvaluationDayTicks)
            {
                nextPressureEvaluationDayTicks = vehiclePressureEvaluationDayTicks;
            }
        }

        private static bool ShouldApplyExitPressure(
            PublicTransportLaneViolation violation,
            long currentDayTicks,
            long thresholdDayTicks,
            out long elapsedDayTicks)
        {
            elapsedDayTicks = 0L;
            if (violation.m_ExitPressureApplied)
            {
                return false;
            }

            elapsedDayTicks = currentDayTicks - violation.m_StartDayTicks;
            return elapsedDayTicks >= thresholdDayTicks;
        }

        private bool TryGetEligiblePathOwner(
            Entity vehicle,
            out PathOwner pathOwner)
        {
            if (!m_PathOwnerData.TryGetComponent(vehicle, out pathOwner))
            {
                PublicTransportLaneExitPressureTelemetry.RecordSkippedMissingPathOwner();
                return false;
            }

            if ((pathOwner.m_State & PathFlags.Pending) != 0)
            {
                PublicTransportLaneExitPressureTelemetry.RecordSkippedPendingPathOwner();
                return false;
            }

            return true;
        }

        private bool TryApplyExitPressurePathObsolete(
            Entity vehicle,
            ref PathOwner pathOwner,
            out PathFlags stateBefore)
        {
            stateBefore = pathOwner.m_State;
            if ((pathOwner.m_State & PathFlags.Obsolete) != 0)
            {
                PublicTransportLaneExitPressureTelemetry.RecordSkippedAlreadyObsolete();
                return false;
            }

            pathOwner.m_State |= PathFlags.Obsolete;
            EntityManager.SetComponentData(vehicle, pathOwner);
            PublicTransportLaneExitPressureTelemetry.RecordApplied(vehicle);
            return true;
        }

        private void MarkExitPressureApplied(
            Entity vehicle,
            ref PublicTransportLaneViolation violation)
        {
            violation.m_ExitPressureApplied = true;
            EntityManager.SetComponentData(vehicle, violation);
        }

        private void RecordExitPressurePathObsoleteTrace(
            Entity vehicle,
            Car car,
            PublicTransportLaneViolation violation,
            long elapsedDayTicks,
            long thresholdDayTicks,
            PathFlags stateBefore,
            PathFlags stateAfter,
            ref bool currentLaneDataReady,
            ref bool typeLookupsReady)
        {
            if (!EnforcementLoggingPolicy.ShouldLogVehicleSpecificPathObsoleteSource(
                    vehicle))
            {
                return;
            }

            if (!currentLaneDataReady)
            {
                m_CurrentLaneData.Update(this);
                currentLaneDataReady = true;
            }

            Entity currentLane =
                m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLaneData)
                    ? currentLaneData.m_Lane
                    : violation.m_Lane;

            if (!typeLookupsReady)
            {
                m_TypeLookups.Update(this);
                typeLookupsReady = true;
            }

            string role =
                PublicTransportLanePolicy.DescribeVehicleRole(
                    vehicle,
                    ref m_TypeLookups);
            string extra =
                $"violationLane={violation.m_Lane}, elapsedDayTicks={elapsedDayTicks}, " +
                $"thresholdDayTicks={thresholdDayTicks}, exitPressureAppliedBefore={violation.m_ExitPressureApplied}";

            PathObsoleteTraceLogging.Record(
                "PT_EXIT_PRESSURE",
                vehicle,
                currentLane,
                stateBefore,
                stateAfter,
                "PT-lane-exit-pressure-threshold-reached",
                car,
                role,
                extra);
        }

        private static void TryLogExitPressureApplication(
            Entity vehicle,
            Entity lane,
            long elapsedDayTicks,
            long thresholdDayTicks)
        {
            if (!EnforcementLoggingPolicy.ShouldLogVehicleSpecificEnforcementEvent(
                    vehicle))
            {
                return;
            }

            Mod.log.Info(
                $"Applied PT-lane exit pressure: vehicle={vehicle}, lane={lane}, " +
                $"elapsedDayTicks={elapsedDayTicks}, thresholdDayTicks={thresholdDayTicks}");
        }
    }
}
