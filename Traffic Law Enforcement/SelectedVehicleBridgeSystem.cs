using System.Collections.Generic;
using Game;
using Game.Vehicles;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public readonly struct SelectedVehicleDebugSnapshot
    {
        public readonly SelectedVehicleResolveState ResolveState;
        public readonly Entity SourceSelectedEntity;
        public readonly Entity ResolvedVehicleEntity;
        public readonly bool IsTrailerChild;
        public readonly bool IsVehicle;
        public readonly bool IsCar;
        public readonly bool IsParked;
        public readonly bool HasCarCurrentLane;

        public readonly int VehicleIndex;
        public readonly string RoleOrTypeText;
        public readonly bool HasTrafficLawProfile;

        public readonly Entity CurrentLaneEntity;
        public readonly Entity PreviousLaneEntity;
        public readonly int LaneChangeCount;

        public readonly bool PtLaneViolationActive;
        public readonly bool PendingExitActive;
        public readonly string PermissionStateSummary;

        public readonly int TotalFines;
        public readonly int TotalViolations;
        public readonly string LastReason;

        public SelectedVehicleDebugSnapshot(
            SelectedVehicleResolveState resolveState,
            Entity sourceSelectedEntity,
            Entity resolvedVehicleEntity,
            bool isTrailerChild,
            bool isVehicle,
            bool isCar,
            bool isParked,
            bool hasCarCurrentLane,
            int vehicleIndex,
            string roleOrTypeText,
            bool hasTrafficLawProfile,
            Entity currentLaneEntity,
            Entity previousLaneEntity,
            int laneChangeCount,
            bool ptLaneViolationActive,
            bool pendingExitActive,
            string permissionStateSummary,
            int totalFines,
            int totalViolations,
            string lastReason)
        {
            ResolveState = resolveState;
            SourceSelectedEntity = sourceSelectedEntity;
            ResolvedVehicleEntity = resolvedVehicleEntity;
            IsTrailerChild = isTrailerChild;
            IsVehicle = isVehicle;
            IsCar = isCar;
            IsParked = isParked;
            HasCarCurrentLane = hasCarCurrentLane;
            VehicleIndex = vehicleIndex;
            RoleOrTypeText = roleOrTypeText;
            HasTrafficLawProfile = hasTrafficLawProfile;
            CurrentLaneEntity = currentLaneEntity;
            PreviousLaneEntity = previousLaneEntity;
            LaneChangeCount = laneChangeCount;
            PtLaneViolationActive = ptLaneViolationActive;
            PendingExitActive = pendingExitActive;
            PermissionStateSummary = permissionStateSummary;
            TotalFines = totalFines;
            TotalViolations = totalViolations;
            LastReason = lastReason;
        }
    }

    public partial class SelectedVehicleBridgeSystem : GameSystemBase
    {
        private SelectedVehicleResolver m_SelectedVehicleResolver;
        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;

        private ComponentLookup<CarCurrentLane> m_CurrentLaneData;
        private ComponentLookup<VehicleLaneHistory> m_HistoryData;
        private ComponentLookup<VehicleTrafficLawProfile> m_ProfileData;
        private ComponentLookup<PublicTransportLaneViolation> m_PublicTransportLaneViolationData;
        private ComponentLookup<PublicTransportLanePendingExit> m_PendingExitData;
        private ComponentLookup<PublicTransportLanePermissionState> m_PermissionStateData;

        private SelectedVehicleDebugSnapshot m_CurrentSnapshot;
        private bool m_HasSnapshot;

        public SelectedVehicleDebugSnapshot CurrentSnapshot => m_CurrentSnapshot;
        public bool HasSnapshot => m_HasSnapshot;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SelectedVehicleResolver = new SelectedVehicleResolver(this);
            m_TypeLookups = PublicTransportLaneVehicleTypeLookups.Create(this);
            m_CurrentLaneData = GetComponentLookup<CarCurrentLane>(true);
            m_HistoryData = GetComponentLookup<VehicleLaneHistory>(true);
            m_ProfileData = GetComponentLookup<VehicleTrafficLawProfile>(true);
            m_PublicTransportLaneViolationData =
                GetComponentLookup<PublicTransportLaneViolation>(true);
            m_PendingExitData = GetComponentLookup<PublicTransportLanePendingExit>(true);
            m_PermissionStateData =
                GetComponentLookup<PublicTransportLanePermissionState>(true);
        }

        protected override void OnUpdate()
        {
            m_SelectedVehicleResolver.Update(this);
            m_TypeLookups.Update(this);
            m_CurrentLaneData.Update(this);
            m_HistoryData.Update(this);
            m_ProfileData.Update(this);
            m_PublicTransportLaneViolationData.Update(this);
            m_PendingExitData.Update(this);
            m_PermissionStateData.Update(this);

            SelectedVehicleResolveResult resolveResult =
                m_SelectedVehicleResolver.ResolveCurrentSelection();

            m_CurrentSnapshot = BuildSnapshot(resolveResult);
            m_HasSnapshot = true;
        }

        private SelectedVehicleDebugSnapshot BuildSnapshot(
            SelectedVehicleResolveResult resolveResult)
        {
            Entity vehicle = resolveResult.ResolvedVehicleEntity;
            bool hasVehicleEntity = vehicle != Entity.Null;

            bool hasTrafficLawProfile =
                hasVehicleEntity &&
                m_ProfileData.HasComponent(vehicle);

            Entity currentLaneEntity = Entity.Null;
            if (resolveResult.HasCarCurrentLane &&
                m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLane))
            {
                currentLaneEntity = currentLane.m_Lane;
            }

            Entity previousLaneEntity = Entity.Null;
            int laneChangeCount = 0;
            if (hasVehicleEntity &&
                m_HistoryData.TryGetComponent(vehicle, out VehicleLaneHistory history))
            {
                previousLaneEntity = history.m_PreviousLane;
                laneChangeCount = history.m_LaneChangeCount;
            }

            bool ptLaneViolationActive =
                hasVehicleEntity &&
                m_PublicTransportLaneViolationData.HasComponent(vehicle);

            bool pendingExitActive =
                hasVehicleEntity &&
                m_PendingExitData.HasComponent(vehicle);

            int totalFines = 0;
            int totalViolations = 0;
            string lastReason = string.Empty;

            if (resolveResult.IsVehicle)
            {
                int vehicleIndex = vehicle.Index;
                totalViolations = EnforcementTelemetry.GetVehicleViolationCount(vehicleIndex);

                IReadOnlyDictionary<int, (int violationCount, int fineTotal)> penaltySnapshot =
                    EnforcementTelemetry.GetVehiclePenaltySnapshot();

                if (penaltySnapshot.TryGetValue(vehicleIndex, out (int violationCount, int fineTotal) totals))
                {
                    totalViolations = totals.violationCount;
                    totalFines = totals.fineTotal;
                }

                lastReason = FindLastReason(vehicleIndex);
            }

            return new SelectedVehicleDebugSnapshot(
                resolveResult.ResolveState,
                resolveResult.SourceSelectedEntity,
                resolveResult.ResolvedVehicleEntity,
                resolveResult.IsTrailerChild,
                resolveResult.IsVehicle,
                resolveResult.IsCar,
                resolveResult.IsParked,
                resolveResult.HasCarCurrentLane,
                hasVehicleEntity ? vehicle.Index : -1,
                BuildRoleOrTypeText(resolveResult),
                hasTrafficLawProfile,
                currentLaneEntity,
                previousLaneEntity,
                laneChangeCount,
                ptLaneViolationActive,
                pendingExitActive,
                BuildPermissionStateSummary(vehicle, hasTrafficLawProfile),
                totalFines,
                totalViolations,
                lastReason);
        }

        private string BuildRoleOrTypeText(SelectedVehicleResolveResult resolveResult)
        {
            if (!resolveResult.HasSelection)
            {
                return string.Empty;
            }

            if (!resolveResult.IsVehicle)
            {
                return "Not a vehicle";
            }

            if (resolveResult.IsParked)
            {
                return "Parked vehicle";
            }

            if (!resolveResult.IsCar)
            {
                return "Unsupported vehicle type";
            }

            return PublicTransportLanePolicy.DescribeVehicleRole(
                resolveResult.ResolvedVehicleEntity,
                ref m_TypeLookups);
        }

        private string BuildPermissionStateSummary(
            Entity vehicle,
            bool hasTrafficLawProfile)
        {
            if (vehicle == Entity.Null)
            {
                return string.Empty;
            }

            if (!m_PermissionStateData.TryGetComponent(
                    vehicle,
                    out PublicTransportLanePermissionState permissionState))
            {
                return hasTrafficLawProfile
                    ? "No active permission state"
                    : "Not tracked by Traffic Law Enforcement";
            }

            PublicTransportLaneAccessBits accessBits =
                permissionState.m_PublicTransportLaneAccessBits;

            return
                $"vanillaAllow={PublicTransportLanePolicy.VanillaAllowsAccess(accessBits)}, " +
                $"modAllow={PublicTransportLanePolicy.ModAllowsAccess(accessBits)}, " +
                $"vanillaPrefer={PublicTransportLanePolicy.VanillaPrefersLanes(accessBits)}, " +
                $"modPrefer={PublicTransportLanePolicy.ModPrefersLanes(accessBits)}, " +
                $"changedByMod={PublicTransportLanePolicy.PermissionChangedByMod(accessBits)}, " +
                $"emergency={permissionState.m_EmergencyActive != 0}, " +
                $"graceConsumed={permissionState.m_ImmediateEntryGraceConsumed != 0}";
        }

        private static string FindLastReason(int vehicleIndex)
        {
            IReadOnlyCollection<EnforcementRecord> recentRecords =
                EnforcementTelemetry.GetRecentRecordsSnapshot();

            if (recentRecords == null || recentRecords.Count == 0)
            {
                return string.Empty;
            }

            List<EnforcementRecord> records = new List<EnforcementRecord>(recentRecords);
            for (int index = records.Count - 1; index >= 0; index -= 1)
            {
                EnforcementRecord record = records[index];
                if (record.VehicleId == vehicleIndex)
                {
                    return record.Reason ?? string.Empty;
                }
            }

            return string.Empty;
        }
    }
}
