using System.Collections.Generic;
using Game;
using Game.Vehicles;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public enum SelectedVehicleTleApplicability
    {
        NotApplicable,
        ApplicableNoLiveLaneData,
        ApplicableReady,
    }

    public readonly struct SelectedVehicleDebugSnapshot
    {
        public readonly SelectedVehicleResolveState ResolveState;
        public readonly SelectedVehicleKind VehicleKind;
        public readonly SelectedVehicleTleApplicability TleApplicability;
        public readonly Entity SourceSelectedEntity;
        public readonly Entity ResolvedVehicleEntity;
        public readonly bool IsTrailerChild;
        public readonly bool IsVehicle;
        public readonly bool IsCar;
        public readonly bool IsTrain;
        public readonly bool IsParked;
        public readonly bool HasCarCurrentLane;
        public readonly bool HasTrainCurrentLane;
        public readonly bool HasLiveLaneData;

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
            SelectedVehicleKind vehicleKind,
            SelectedVehicleTleApplicability tleApplicability,
            Entity sourceSelectedEntity,
            Entity resolvedVehicleEntity,
            bool isTrailerChild,
            bool isVehicle,
            bool isCar,
            bool isTrain,
            bool isParked,
            bool hasCarCurrentLane,
            bool hasTrainCurrentLane,
            bool hasLiveLaneData,
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
            VehicleKind = vehicleKind;
            TleApplicability = tleApplicability;
            SourceSelectedEntity = sourceSelectedEntity;
            ResolvedVehicleEntity = resolvedVehicleEntity;
            IsTrailerChild = isTrailerChild;
            IsVehicle = isVehicle;
            IsCar = isCar;
            IsTrain = isTrain;
            IsParked = isParked;
            HasCarCurrentLane = hasCarCurrentLane;
            HasTrainCurrentLane = hasTrainCurrentLane;
            HasLiveLaneData = hasLiveLaneData;
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
            SelectedVehicleTleApplicability tleApplicability =
                GetTleApplicability(resolveResult);
            bool tleApplicable =
                tleApplicability != SelectedVehicleTleApplicability.NotApplicable;
            bool tleReady =
                tleApplicability == SelectedVehicleTleApplicability.ApplicableReady;

            bool hasTrafficLawProfile =
                tleApplicable &&
                hasVehicleEntity &&
                m_ProfileData.HasComponent(vehicle);

            Entity currentLaneEntity = Entity.Null;
            if (tleReady &&
                resolveResult.HasCarCurrentLane &&
                m_CurrentLaneData.TryGetComponent(vehicle, out CarCurrentLane currentLane))
            {
                currentLaneEntity = currentLane.m_Lane;
            }

            Entity previousLaneEntity = Entity.Null;
            int laneChangeCount = 0;
            if (tleReady &&
                hasVehicleEntity &&
                m_HistoryData.TryGetComponent(vehicle, out VehicleLaneHistory history))
            {
                previousLaneEntity = history.m_PreviousLane;
                laneChangeCount = history.m_LaneChangeCount;
            }

            bool ptLaneViolationActive =
                tleReady &&
                hasVehicleEntity &&
                m_PublicTransportLaneViolationData.HasComponent(vehicle);

            bool pendingExitActive =
                tleReady &&
                hasVehicleEntity &&
                m_PendingExitData.HasComponent(vehicle);

            string permissionStateSummary = string.Empty;
            if (tleApplicable)
            {
                permissionStateSummary =
                    BuildPermissionStateSummary(vehicle, hasTrafficLawProfile);
            }

            int totalFines = 0;
            int totalViolations = 0;
            string lastReason = string.Empty;

            if (tleApplicable && resolveResult.IsVehicle)
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
                resolveResult.VehicleKind,
                tleApplicability,
                resolveResult.SourceSelectedEntity,
                resolveResult.ResolvedVehicleEntity,
                resolveResult.IsTrailerChild,
                resolveResult.IsVehicle,
                resolveResult.IsCar,
                resolveResult.IsTrain,
                resolveResult.IsParked,
                resolveResult.HasCarCurrentLane,
                resolveResult.HasTrainCurrentLane,
                resolveResult.HasLiveLaneData,
                hasVehicleEntity ? vehicle.Index : -1,
                BuildRoleOrTypeText(resolveResult),
                hasTrafficLawProfile,
                currentLaneEntity,
                previousLaneEntity,
                laneChangeCount,
                ptLaneViolationActive,
                pendingExitActive,
                permissionStateSummary,
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

            switch (resolveResult.VehicleKind)
            {
                case SelectedVehicleKind.RoadCar:
                    return PublicTransportLanePolicy.DescribeVehicleRole(
                        resolveResult.ResolvedVehicleEntity,
                        ref m_TypeLookups);

                case SelectedVehicleKind.ParkedRoadCar:
                    return "Parked road car";

                case SelectedVehicleKind.Train:
                    return "Train";

                case SelectedVehicleKind.ParkedTrain:
                    return "Parked train";

                case SelectedVehicleKind.OtherVehicle:
                    return "Other vehicle";

                default:
                    return "Vehicle";
            }
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

            string lastReason = string.Empty;
            foreach (EnforcementRecord record in recentRecords)
            {
                if (record.VehicleId == vehicleIndex)
                {
                    lastReason = record.Reason ?? string.Empty;
                }
            }

            return lastReason;
        }

        private static SelectedVehicleTleApplicability GetTleApplicability(
            SelectedVehicleResolveResult resolveResult)
        {
            switch (resolveResult.VehicleKind)
            {
                case SelectedVehicleKind.RoadCar:
                    return resolveResult.HasCarCurrentLane
                        ? SelectedVehicleTleApplicability.ApplicableReady
                        : SelectedVehicleTleApplicability.ApplicableNoLiveLaneData;

                case SelectedVehicleKind.ParkedRoadCar:
                    return SelectedVehicleTleApplicability.ApplicableNoLiveLaneData;

                default:
                    return SelectedVehicleTleApplicability.NotApplicable;
            }
        }
    }
}
