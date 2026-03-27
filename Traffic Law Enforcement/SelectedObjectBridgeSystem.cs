using System.Collections.Generic;
using Game;
using Game.Vehicles;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    public enum SelectedObjectTleApplicability
    {
        NotApplicable,
        ApplicableNoLiveLaneData,
        ApplicableReady,
    }

    public readonly struct SelectedObjectDebugSnapshot
    {
        public readonly SelectedObjectResolveState ResolveState;
        public readonly SelectedObjectKind VehicleKind;
        public readonly SelectedObjectTleApplicability TleApplicability;
        public readonly Entity SourceSelectedEntity;
        public readonly Entity ResolvedVehicleEntity;
        public readonly Entity PrefabEntity;
        public readonly bool HasPrefabRef;
        public readonly bool IsTrailerChild;
        public readonly bool IsVehicle;
        public readonly bool IsCar;
        public readonly bool IsTrain;
        public readonly bool IsParked;
        public readonly bool HasCarCurrentLane;
        public readonly bool HasTrainCurrentLane;
        public readonly bool HasLiveLaneData;
        public readonly bool HasPublicTransportVehicleData;
        public readonly bool HasTrainData;
        public readonly string RuntimeFamilyText;
        public readonly string RawTransportTypeText;
        public readonly string RawTrackTypeText;
        public readonly string RailSubtypeSourceText;
        public readonly string SummaryClassificationText;
        public readonly string SummaryTleStatusText;
        public readonly string CompactLastReasonText;

        public readonly int VehicleIndex;
        public readonly string RoleText;
        public readonly string PublicTransportLaneAllowanceText;
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

        public SelectedObjectDebugSnapshot(
            SelectedObjectResolveState resolveState,
            SelectedObjectKind vehicleKind,
            SelectedObjectTleApplicability tleApplicability,
            Entity sourceSelectedEntity,
            Entity resolvedVehicleEntity,
            Entity prefabEntity,
            bool hasPrefabRef,
            bool isTrailerChild,
            bool isVehicle,
            bool isCar,
            bool isTrain,
            bool isParked,
            bool hasCarCurrentLane,
            bool hasTrainCurrentLane,
            bool hasLiveLaneData,
            bool hasPublicTransportVehicleData,
            bool hasTrainData,
            string runtimeFamilyText,
            string rawTransportTypeText,
            string rawTrackTypeText,
            string railSubtypeSourceText,
            string summaryClassificationText,
            string summaryTleStatusText,
            string compactLastReasonText,
            int vehicleIndex,
            string roleText,
            string publicTransportLaneAllowanceText,
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
            PrefabEntity = prefabEntity;
            HasPrefabRef = hasPrefabRef;
            IsTrailerChild = isTrailerChild;
            IsVehicle = isVehicle;
            IsCar = isCar;
            IsTrain = isTrain;
            IsParked = isParked;
            HasCarCurrentLane = hasCarCurrentLane;
            HasTrainCurrentLane = hasTrainCurrentLane;
            HasLiveLaneData = hasLiveLaneData;
            HasPublicTransportVehicleData = hasPublicTransportVehicleData;
            HasTrainData = hasTrainData;
            RuntimeFamilyText = runtimeFamilyText;
            RawTransportTypeText = rawTransportTypeText;
            RawTrackTypeText = rawTrackTypeText;
            RailSubtypeSourceText = railSubtypeSourceText;
            SummaryClassificationText = summaryClassificationText;
            SummaryTleStatusText = summaryTleStatusText;
            CompactLastReasonText = compactLastReasonText;
            VehicleIndex = vehicleIndex;
            RoleText = roleText;
            PublicTransportLaneAllowanceText = publicTransportLaneAllowanceText;
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

    public partial class SelectedObjectBridgeSystem : GameSystemBase
    {
        private SelectedObjectResolver m_SelectedObjectResolver;
        private PublicTransportLaneVehicleTypeLookups m_TypeLookups;

        private ComponentLookup<CarCurrentLane> m_CurrentLaneData;
        private ComponentLookup<VehicleLaneHistory> m_HistoryData;
        private ComponentLookup<VehicleTrafficLawProfile> m_ProfileData;
        private ComponentLookup<PublicTransportLaneViolation> m_PublicTransportLaneViolationData;
        private ComponentLookup<PublicTransportLanePendingExit> m_PendingExitData;
        private ComponentLookup<PublicTransportLanePermissionState> m_PermissionStateData;

        private SelectedObjectDebugSnapshot m_CurrentSnapshot;
        private bool m_HasSnapshot;

        public SelectedObjectDebugSnapshot CurrentSnapshot => m_CurrentSnapshot;
        public bool HasSnapshot => m_HasSnapshot;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SelectedObjectResolver = new SelectedObjectResolver(this);
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
            m_SelectedObjectResolver.Update(this);
            m_TypeLookups.Update(this);
            m_CurrentLaneData.Update(this);
            m_HistoryData.Update(this);
            m_ProfileData.Update(this);
            m_PublicTransportLaneViolationData.Update(this);
            m_PendingExitData.Update(this);
            m_PermissionStateData.Update(this);

            SelectedObjectResolveResult resolveResult =
                m_SelectedObjectResolver.ResolveCurrentSelection();

            m_CurrentSnapshot = BuildSnapshot(resolveResult);
            m_HasSnapshot = true;
        }

        private SelectedObjectDebugSnapshot BuildSnapshot(
            SelectedObjectResolveResult resolveResult)
        {
            Entity vehicle = resolveResult.ResolvedVehicleEntity;
            bool hasVehicleEntity = vehicle != Entity.Null;
            SelectedObjectTleApplicability tleApplicability =
                GetTleApplicability(resolveResult);
            bool tleApplicable =
                tleApplicability != SelectedObjectTleApplicability.NotApplicable;
            bool tleReady =
                tleApplicability == SelectedObjectTleApplicability.ApplicableReady;

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

            return new SelectedObjectDebugSnapshot(
                resolveResult.ResolveState,
                resolveResult.VehicleKind,
                tleApplicability,
                resolveResult.SourceSelectedEntity,
                resolveResult.ResolvedVehicleEntity,
                resolveResult.PrefabEntity,
                resolveResult.HasPrefabRef,
                resolveResult.IsTrailerChild,
                resolveResult.IsVehicle,
                resolveResult.IsCar,
                resolveResult.IsTrain,
                resolveResult.IsParked,
                resolveResult.HasCarCurrentLane,
                resolveResult.HasTrainCurrentLane,
                resolveResult.HasLiveLaneData,
                resolveResult.HasPublicTransportVehicleData,
                resolveResult.HasTrainData,
                BuildRuntimeFamilyText(resolveResult),
                BuildRawTransportTypeText(resolveResult),
                BuildRawTrackTypeText(resolveResult),
                BuildRailSubtypeSourceText(resolveResult),
                BuildSummaryClassificationText(resolveResult),
                BuildSummaryTleStatusText(resolveResult, tleApplicability),
                BuildCompactLastReasonText(tleApplicable, lastReason),
                resolveResult.IsVehicle && hasVehicleEntity ? vehicle.Index : -1,
                BuildRoleText(resolveResult),
                BuildPublicTransportLaneAllowanceText(resolveResult, hasTrafficLawProfile),
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

        private static string BuildRuntimeFamilyText(
            SelectedObjectResolveResult resolveResult)
        {
            if (!resolveResult.IsVehicle)
            {
                return "Unavailable";
            }

            switch (resolveResult.RuntimeFamily)
            {
                case SelectedObjectRuntimeFamily.Car:
                    return "Car";

                case SelectedObjectRuntimeFamily.Train:
                    return "Train";

                case SelectedObjectRuntimeFamily.Other:
                    return "Other";

                default:
                    return "Unavailable";
            }
        }

        private static string BuildRawTransportTypeText(
            SelectedObjectResolveResult resolveResult)
        {
            if (!resolveResult.IsVehicle || !resolveResult.HasPublicTransportVehicleData)
            {
                return "Unavailable";
            }

            return resolveResult.RawTransportType == Game.Prefabs.TransportType.None
                ? "None"
                : resolveResult.RawTransportType.ToString();
        }

        private static string BuildRawTrackTypeText(
            SelectedObjectResolveResult resolveResult)
        {
            if (!resolveResult.IsVehicle || !resolveResult.HasTrainData)
            {
                return "Unavailable";
            }

            Game.Net.TrackTypes trackType = resolveResult.RawTrackType;
            if (trackType == Game.Net.TrackTypes.None)
            {
                return "None";
            }

            bool isTrainTrack = (trackType & Game.Net.TrackTypes.Train) != Game.Net.TrackTypes.None;
            bool isTramTrack = (trackType & Game.Net.TrackTypes.Tram) != Game.Net.TrackTypes.None;
            bool isSubwayTrack = (trackType & Game.Net.TrackTypes.Subway) != Game.Net.TrackTypes.None;

            int matchedTypeCount =
                (isTrainTrack ? 1 : 0) +
                (isTramTrack ? 1 : 0) +
                (isSubwayTrack ? 1 : 0);

            if (matchedTypeCount != 1)
            {
                return "Mixed";
            }

            if (isTramTrack)
            {
                return "Tram";
            }

            if (isSubwayTrack)
            {
                return "Subway";
            }

            return "Train";
        }

        private static string BuildRailSubtypeSourceText(
            SelectedObjectResolveResult resolveResult)
        {
            switch (resolveResult.RailSubtypeSource)
            {
                case SelectedObjectRailSubtypeSource.TransportType:
                    return "TransportType";

                case SelectedObjectRailSubtypeSource.TrackType:
                    return "TrackType";

                case SelectedObjectRailSubtypeSource.Fallback:
                    return "Fallback";

                default:
                    return "None";
            }
        }

        private static string BuildSummaryClassificationText(
            SelectedObjectResolveResult resolveResult)
        {
            switch (resolveResult.VehicleKind)
            {
                case SelectedObjectKind.RoadCar:
                    return "Road car";

                case SelectedObjectKind.ParkedRoadCar:
                    return "Parked road car";

                case SelectedObjectKind.RailVehicle:
                    return "Rail vehicle";

                case SelectedObjectKind.ParkedRailVehicle:
                    return "Parked rail vehicle";

                case SelectedObjectKind.Tram:
                    return "Tram";

                case SelectedObjectKind.ParkedTram:
                    return "Parked tram";

                case SelectedObjectKind.Train:
                    return "Train";

                case SelectedObjectKind.ParkedTrain:
                    return "Parked train";

                case SelectedObjectKind.Subway:
                    return "Subway";

                case SelectedObjectKind.ParkedSubway:
                    return "Parked subway";

                case SelectedObjectKind.OtherVehicle:
                    return "Other vehicle";

                default:
                    return string.Empty;
            }
        }

        private static string BuildSummaryTleStatusText(
            SelectedObjectResolveResult resolveResult,
            SelectedObjectTleApplicability tleApplicability)
        {
            if (resolveResult.ResolveState == SelectedObjectResolveState.None)
            {
                return string.Empty;
            }

            if (resolveResult.ResolveState == SelectedObjectResolveState.NotVehicle)
            {
                return "Selected object is not a vehicle";
            }

            switch (tleApplicability)
            {
                case SelectedObjectTleApplicability.NotApplicable:
                    return "Traffic Law Enforcement not applicable";

                case SelectedObjectTleApplicability.ApplicableNoLiveLaneData:
                    return resolveResult.VehicleKind == SelectedObjectKind.ParkedRoadCar
                        ? "Live lane unavailable for parked road car"
                        : "Live lane unavailable";

                case SelectedObjectTleApplicability.ApplicableReady:
                    return "Tracking selected road vehicle";

                default:
                    return string.Empty;
            }
        }

        private static string BuildCompactLastReasonText(
            bool tleApplicable,
            string lastReason)
        {
            if (!tleApplicable)
            {
                return string.Empty;
            }

            string normalizedReason = string.IsNullOrWhiteSpace(lastReason)
                ? "None recorded"
                : lastReason
                    .Replace('\r', ' ')
                    .Replace('\n', ' ')
                    .Trim();

            const int maxLength = 56;
            if (normalizedReason.Length <= maxLength)
            {
                return normalizedReason;
            }

            return normalizedReason.Substring(0, maxLength - 3) + "...";
        }

        private string BuildRoleText(SelectedObjectResolveResult resolveResult)
        {
            if (!resolveResult.HasSelection)
            {
                return string.Empty;
            }

            if (!resolveResult.IsVehicle)
            {
                return string.Empty;
            }

            switch (resolveResult.VehicleKind)
            {
                case SelectedObjectKind.RoadCar:
                    {
                        Entity vehicle = resolveResult.ResolvedVehicleEntity;
                        return PublicTransportLanePolicy.DescribeVehicleRole(
                            vehicle,
                            ref m_TypeLookups);
                    }

                case SelectedObjectKind.ParkedRoadCar:
                    return "Parked road car";

                case SelectedObjectKind.RailVehicle:
                    return "Rail vehicle";

                case SelectedObjectKind.ParkedRailVehicle:
                    return "Parked rail vehicle";

                case SelectedObjectKind.Tram:
                    return "Tram";

                case SelectedObjectKind.ParkedTram:
                    return "Parked tram";

                case SelectedObjectKind.Train:
                    return "Train";

                case SelectedObjectKind.ParkedTrain:
                    return "Parked train";

                case SelectedObjectKind.Subway:
                    return "Subway";

                case SelectedObjectKind.ParkedSubway:
                    return "Parked subway";

                case SelectedObjectKind.OtherVehicle:
                    return "Other vehicle";

                default:
                    return "Vehicle";
            }
        }

        private string BuildPublicTransportLaneAllowanceText(
            SelectedObjectResolveResult resolveResult,
            bool hasTrafficLawProfile)
        {
            if (!hasTrafficLawProfile ||
                !resolveResult.IsVehicle ||
                (resolveResult.VehicleKind != SelectedObjectKind.RoadCar &&
                 resolveResult.VehicleKind != SelectedObjectKind.ParkedRoadCar))
            {
                return string.Empty;
            }

            Entity vehicle = resolveResult.ResolvedVehicleEntity;
            if (vehicle == Entity.Null ||
                !m_ProfileData.TryGetComponent(vehicle, out VehicleTrafficLawProfile profile))
            {
                return string.Empty;
            }

            PublicTransportLaneAccessBits accessBits = profile.m_PublicTransportLaneAccessBits;
            string type = PublicTransportLanePolicy.DescribeType(accessBits);

            List<string> qualifiers = null;
            if (PublicTransportLanePolicy.ModPrefersLanes(accessBits))
            {
                (qualifiers ??= new List<string>()).Add("PT");
            }

            if (profile.m_EmergencyVehicle != 0)
            {
                (qualifiers ??= new List<string>()).Add("Emergency");
            }

            return qualifiers == null || qualifiers.Count == 0
                ? type
                : $"{type} [{string.Join(", ", qualifiers)}]";
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

            bool emergencyActive =
                permissionState.m_EmergencyActive != 0;

            bool emergencyOverrideActive =
                PublicTransportLanePolicy.HasEmergencyPublicTransportLaneOverride(
                    accessBits,
                    emergencyActive);

            return
                $"type={PublicTransportLanePolicy.DescribeType(accessBits)}, " +
                $"vanillaAllow={PublicTransportLanePolicy.VanillaAllowsAccess(accessBits)}, " +
                $"modAllow={PublicTransportLanePolicy.ModAllowsAccess(accessBits)}, " +
                $"canUsePublicTransportLane={PublicTransportLanePolicy.CanUsePublicTransportLane(accessBits, emergencyActive)}, " +
                $"vanillaPrefer={PublicTransportLanePolicy.VanillaPrefersLanes(accessBits)}, " +
                $"modPrefer={PublicTransportLanePolicy.ModPrefersLanes(accessBits)}, " +
                $"changedByMod={PublicTransportLanePolicy.PermissionChangedByMod(accessBits)}, " +
                $"emergency={emergencyActive}, " +
                $"emergencyOverride={emergencyOverrideActive}, " +
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

        private static SelectedObjectTleApplicability GetTleApplicability(
            SelectedObjectResolveResult resolveResult)
        {
            switch (resolveResult.VehicleKind)
            {
                case SelectedObjectKind.RoadCar:
                    return resolveResult.HasCarCurrentLane
                        ? SelectedObjectTleApplicability.ApplicableReady
                        : SelectedObjectTleApplicability.ApplicableNoLiveLaneData;

                case SelectedObjectKind.ParkedRoadCar:
                    return SelectedObjectTleApplicability.ApplicableNoLiveLaneData;

                default:
                    return SelectedObjectTleApplicability.NotApplicable;
            }
        }
    }
}

