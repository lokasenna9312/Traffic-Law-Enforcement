using System.Collections.Generic;
using Game.Debug;
using Unity.Entities;
using UnityEngine.Rendering;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    [DebugContainer]
    public static class TrafficLawEnforcementDebugUI
    {
        [DebugTab("Traffic Law Enforcement", -910)]
        private static List<DebugUI.Widget> BuildTrafficLawEnforcementDebugUI()
        {
            DebugUI.Foldout overview = new DebugUI.Foldout
            {
                displayName = "Enforcement Overview",
                opened = true
            };

            overview.children.Add(new DebugUI.Value
            {
                displayName = "Active PT-lane violators",
                getter = () => EnforcementTelemetry.ActivePublicTransportLaneViolators
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "PT-lane violations total",
                getter = () => EnforcementTelemetry.PublicTransportLaneViolationCount
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "Mid-block crossing total",
                getter = () => EnforcementTelemetry.MidBlockCrossingViolationCount
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "Intersection movement total",
                getter = () => EnforcementTelemetry.IntersectionMovementViolationCount
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "Total fines",
                getter = () => EnforcementTelemetry.TotalFineAmount
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "Configured PT-lane fine",
                getter = () => EnforcementPenaltyService.GetPublicTransportLaneFine()
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "Configured mid-block fine",
                getter = () => EnforcementPenaltyService.GetMidBlockCrossingFine()
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "Configured intersection fine",
                getter = () => EnforcementPenaltyService.GetIntersectionMovementFine()
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "Pathfinding penalty prefabs",
                getter = () => MidBlockPathfindingBiasTelemetry.ModifiedPrefabCount
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "Pathfinding money overrides",
                getter = () => MidBlockPathfindingBiasTelemetry.OverrideSummary
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "Reroute logging enabled",
                getter = () => RerouteLoggingTelemetry.Enabled
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "Reroute snapshot cache",
                getter = () => RerouteLoggingTelemetry.CachedSnapshotCount
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "Reroute candidates (last update)",
                getter = () => RerouteLoggingTelemetry.LastCandidateCount
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "Reroute logs emitted (last update)",
                getter = () => RerouteLoggingTelemetry.LastEmittedLogCount
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "PT-lane repeat policy",
                getter = () => EnforcementPenaltyService.GetRepeatPolicyDebugSummary(EnforcementKinds.PublicTransportLane)
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "Mid-block repeat policy",
                getter = () => EnforcementPenaltyService.GetRepeatPolicyDebugSummary(EnforcementKinds.MidBlockCrossing)
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "Intersection repeat policy",
                getter = () => EnforcementPenaltyService.GetRepeatPolicyDebugSummary(EnforcementKinds.IntersectionMovement)
            });

            DebugUI.Foldout recent = new DebugUI.Foldout
            {
                displayName = "Recent events",
                opened = true
            };
            recent.children.Add(new DebugUI.Value
            {
                displayName = "Latest",
                getter = () => EnforcementTelemetry.RecentEventsText
            });

            DebugUI.Foldout records = new DebugUI.Foldout
            {
                displayName = "Vehicle fine records",
                opened = true
            };
            records.children.Add(new DebugUI.Value
            {
                displayName = "Recent records",
                getter = () => EnforcementTelemetry.RecentRecordsText
            });
            records.children.Add(new DebugUI.Value
            {
                displayName = "Top vehicle totals",
                getter = () => EnforcementTelemetry.VehicleFineTotalsText
            });
            records.children.Add(new DebugUI.Value
            {
                displayName = "Top vehicle violation counts",
                getter = () => EnforcementTelemetry.VehicleViolationCountsText
            });

            DebugUI.Foldout selectedVehicle = BuildSelectedVehicleInspector();

            return new List<DebugUI.Widget>
            {
                new DebugUI.Button
                {
                    displayName = "Refresh",
                    action = delegate
                    {
                        DebugSystem.Rebuild(BuildTrafficLawEnforcementDebugUI);
                    }
                },
                overview,
                selectedVehicle,
                recent,
                records
            };
        }

        private static DebugUI.Foldout BuildSelectedVehicleInspector()
        {
            DebugUI.Foldout selectedVehicle = new DebugUI.Foldout
            {
                displayName = "Selected Vehicle",
                opened = true
            };

            selectedVehicle.children.Add(new DebugUI.Value
            {
                displayName = "Status",
                getter = GetSelectedVehicleStatusText
            });
            selectedVehicle.children.Add(new DebugUI.Value
            {
                displayName = "Classification",
                getter = GetSelectedVehicleClassificationText
            });
            selectedVehicle.children.Add(new DebugUI.Value
            {
                displayName = "TLE status",
                getter = GetTleStatusText
            });
            selectedVehicle.children.Add(new DebugUI.Value
            {
                displayName = "Flags",
                getter = GetSelectedVehicleFlagsText
            });

            DebugUI.Foldout vehicleInfo = new DebugUI.Foldout
            {
                displayName = "Vehicle info",
                opened = true
            };
            vehicleInfo.children.Add(new DebugUI.Value
            {
                displayName = "Vehicle index",
                getter = GetVehicleIndexText
            });
            vehicleInfo.children.Add(new DebugUI.Value
            {
                displayName = "Role / type",
                getter = GetRoleOrTypeText
            });
            vehicleInfo.children.Add(new DebugUI.Value
            {
                displayName = "Has traffic law profile",
                getter = GetHasTrafficLawProfileText
            });
            vehicleInfo.children.Add(new DebugUI.Value
            {
                displayName = "Trailer child",
                getter = GetTrailerChildText
            });

            DebugUI.Foldout tle = new DebugUI.Foldout
            {
                displayName = "Traffic Law Enforcement",
                opened = true
            };
            tle.children.Add(new DebugUI.Value
            {
                displayName = "Status",
                getter = GetTleStatusText
            });

            DebugUI.Foldout lane = new DebugUI.Foldout
            {
                displayName = "Lane",
                opened = true
            };
            lane.children.Add(new DebugUI.Value
            {
                displayName = "Current lane entity",
                getter = GetCurrentLaneEntityText
            });
            lane.children.Add(new DebugUI.Value
            {
                displayName = "Previous lane entity",
                getter = GetPreviousLaneEntityText
            });
            lane.children.Add(new DebugUI.Value
            {
                displayName = "Lane change count",
                getter = GetLaneChangeCountText
            });

            DebugUI.Foldout enforcement = new DebugUI.Foldout
            {
                displayName = "Enforcement",
                opened = true
            };
            enforcement.children.Add(new DebugUI.Value
            {
                displayName = "PT-lane violation active",
                getter = GetPtLaneViolationActiveText
            });
            enforcement.children.Add(new DebugUI.Value
            {
                displayName = "Pending exit active",
                getter = GetPendingExitActiveText
            });
            enforcement.children.Add(new DebugUI.Value
            {
                displayName = "Permission state summary",
                getter = GetPermissionStateSummaryText
            });

            DebugUI.Foldout telemetry = new DebugUI.Foldout
            {
                displayName = "Telemetry",
                opened = true
            };
            telemetry.children.Add(new DebugUI.Value
            {
                displayName = "Total fines",
                getter = GetTotalFinesText
            });
            telemetry.children.Add(new DebugUI.Value
            {
                displayName = "Total violations",
                getter = GetTotalViolationsText
            });
            telemetry.children.Add(new DebugUI.Value
            {
                displayName = "Last reason",
                getter = GetLastReasonText
            });

            tle.children.Add(lane);
            tle.children.Add(enforcement);
            tle.children.Add(telemetry);

            DebugUI.Foldout resolution = new DebugUI.Foldout
            {
                displayName = "Resolution",
                opened = false
            };
            resolution.children.Add(new DebugUI.Value
            {
                displayName = "Source selected entity",
                getter = GetSourceSelectedEntityText
            });
            resolution.children.Add(new DebugUI.Value
            {
                displayName = "Resolved controller/root entity",
                getter = GetResolvedVehicleEntityText
            });
            resolution.children.Add(new DebugUI.Value
            {
                displayName = "Prefab entity",
                getter = GetPrefabEntityText
            });
            resolution.children.Add(new DebugUI.Value
            {
                displayName = "Has prefab ref",
                getter = GetHasPrefabRefText
            });

            DebugUI.Foldout rawClassification = new DebugUI.Foldout
            {
                displayName = "Raw classification",
                opened = false
            };
            rawClassification.children.Add(new DebugUI.Value
            {
                displayName = "Runtime family",
                getter = GetRuntimeFamilyText
            });
            rawClassification.children.Add(new DebugUI.Value
            {
                displayName = "Parked",
                getter = GetRawParkedText
            });
            rawClassification.children.Add(new DebugUI.Value
            {
                displayName = "Has car current lane",
                getter = GetRawHasCarCurrentLaneText
            });
            rawClassification.children.Add(new DebugUI.Value
            {
                displayName = "Has train current lane",
                getter = GetRawHasTrainCurrentLaneText
            });
            rawClassification.children.Add(new DebugUI.Value
            {
                displayName = "Raw TransportType",
                getter = GetRawTransportTypeText
            });
            rawClassification.children.Add(new DebugUI.Value
            {
                displayName = "Raw TrackType",
                getter = GetRawTrackTypeText
            });
            rawClassification.children.Add(new DebugUI.Value
            {
                displayName = "Has PublicTransportVehicleData",
                getter = GetHasPublicTransportVehicleDataText
            });
            rawClassification.children.Add(new DebugUI.Value
            {
                displayName = "Has TrainData",
                getter = GetHasTrainDataText
            });
            rawClassification.children.Add(new DebugUI.Value
            {
                displayName = "Rail subtype source",
                getter = GetRailSubtypeSourceText
            });

            selectedVehicle.children.Add(vehicleInfo);
            selectedVehicle.children.Add(tle);
            selectedVehicle.children.Add(resolution);
            selectedVehicle.children.Add(rawClassification);
            return selectedVehicle;
        }

        private static string GetSelectedVehicleStatusText()
        {
            if (!TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot))
            {
                return "Selected vehicle bridge unavailable";
            }

            switch (snapshot.ResolveState)
            {
                case SelectedVehicleResolveState.None:
                    return "No object selected";

                case SelectedVehicleResolveState.NotVehicle:
                    return "Selected object is not a vehicle";

                case SelectedVehicleResolveState.Vehicle:
                    return "Selected vehicle resolved";

                default:
                    return "Selected vehicle status unavailable";
            }
        }

        private static string GetSelectedVehicleClassificationText()
        {
            if (!TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            switch (snapshot.VehicleKind)
            {
                case SelectedVehicleKind.None:
                    return "Unavailable";

                case SelectedVehicleKind.RoadCar:
                    return "Selected vehicle: road car";

                case SelectedVehicleKind.ParkedRoadCar:
                    return "Selected vehicle: parked road car";

                case SelectedVehicleKind.RailVehicle:
                    return "Selected vehicle: rail vehicle";

                case SelectedVehicleKind.ParkedRailVehicle:
                    return "Selected vehicle: parked rail vehicle";

                case SelectedVehicleKind.Tram:
                    return "Selected vehicle: tram";

                case SelectedVehicleKind.ParkedTram:
                    return "Selected vehicle: parked tram";

                case SelectedVehicleKind.Train:
                    return "Selected vehicle: train";

                case SelectedVehicleKind.ParkedTrain:
                    return "Selected vehicle: parked train";

                case SelectedVehicleKind.Subway:
                    return "Selected vehicle: subway";

                case SelectedVehicleKind.ParkedSubway:
                    return "Selected vehicle: parked subway";

                case SelectedVehicleKind.OtherVehicle:
                    return "Selected vehicle: other vehicle";

                default:
                    return "Selected vehicle: unknown";
            }
        }

        private static string GetSourceSelectedEntityText()
        {
            return TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot)
                ? FormatEntity(snapshot.SourceSelectedEntity)
                : "Unavailable";
        }

        private static string GetResolvedVehicleEntityText()
        {
            return TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot)
                ? FormatEntity(snapshot.ResolvedVehicleEntity)
                : "Unavailable";
        }

        private static string GetPrefabEntityText()
        {
            return TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot)
                ? FormatEntity(snapshot.PrefabEntity)
                : "Unavailable";
        }

        private static string GetHasPrefabRefText()
        {
            if (!TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            return snapshot.ResolveState != SelectedVehicleResolveState.None
                ? snapshot.HasPrefabRef.ToString()
                : "Unavailable";
        }

        private static string GetSelectedVehicleFlagsText()
        {
            if (!TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            return
                $"Vehicle={snapshot.IsVehicle}, " +
                $"Car={snapshot.IsCar}, " +
                $"Train={snapshot.IsTrain}, " +
                $"Parked={snapshot.IsParked}, " +
                $"LiveLane={snapshot.HasLiveLaneData}";
        }

        private static string GetVehicleIndexText()
        {
            return GetVehicleInfoText(
                snapshot => snapshot.VehicleIndex >= 0
                    ? snapshot.VehicleIndex.ToString()
                    : "Unavailable");
        }

        private static string GetRoleOrTypeText()
        {
            return GetVehicleInfoText(
                snapshot => string.IsNullOrWhiteSpace(snapshot.RoleOrTypeText)
                    ? "Unavailable"
                    : snapshot.RoleOrTypeText);
        }

        private static string GetHasTrafficLawProfileText()
        {
            return GetVehicleInfoText(
                snapshot => snapshot.HasTrafficLawProfile.ToString());
        }

        private static string GetTrailerChildText()
        {
            return GetVehicleInfoText(
                snapshot => snapshot.IsTrailerChild.ToString());
        }

        private static string GetRuntimeFamilyText()
        {
            return GetRawClassificationText(
                snapshot => snapshot.RuntimeFamilyText);
        }

        private static string GetRawParkedText()
        {
            return GetRawClassificationText(
                snapshot => snapshot.IsParked.ToString());
        }

        private static string GetRawHasCarCurrentLaneText()
        {
            return GetRawClassificationText(
                snapshot => snapshot.HasCarCurrentLane.ToString());
        }

        private static string GetRawHasTrainCurrentLaneText()
        {
            return GetRawClassificationText(
                snapshot => snapshot.HasTrainCurrentLane.ToString());
        }

        private static string GetRawTransportTypeText()
        {
            return GetRawClassificationText(
                snapshot => snapshot.RawTransportTypeText);
        }

        private static string GetRawTrackTypeText()
        {
            return GetRawClassificationText(
                snapshot => snapshot.RawTrackTypeText);
        }

        private static string GetHasPublicTransportVehicleDataText()
        {
            return GetRawClassificationText(
                snapshot => snapshot.HasPublicTransportVehicleData.ToString());
        }

        private static string GetHasTrainDataText()
        {
            return GetRawClassificationText(
                snapshot => snapshot.HasTrainData.ToString());
        }

        private static string GetRailSubtypeSourceText()
        {
            return GetRawClassificationText(
                snapshot => snapshot.RailSubtypeSourceText);
        }

        private static string GetCurrentLaneEntityText()
        {
            return GetReadyTleText(
                snapshot => FormatEntity(snapshot.CurrentLaneEntity));
        }

        private static string GetPreviousLaneEntityText()
        {
            return GetReadyTleText(
                snapshot => FormatEntity(snapshot.PreviousLaneEntity));
        }

        private static string GetLaneChangeCountText()
        {
            return GetReadyTleText(
                snapshot => snapshot.LaneChangeCount.ToString());
        }

        private static string GetPtLaneViolationActiveText()
        {
            return GetReadyTleText(
                snapshot => snapshot.PtLaneViolationActive.ToString());
        }

        private static string GetPendingExitActiveText()
        {
            return GetReadyTleText(
                snapshot => snapshot.PendingExitActive.ToString());
        }

        private static string GetPermissionStateSummaryText()
        {
            return GetApplicableTleText(
                snapshot => string.IsNullOrWhiteSpace(snapshot.PermissionStateSummary)
                    ? "Unavailable"
                    : snapshot.PermissionStateSummary);
        }

        private static string GetTotalFinesText()
        {
            return GetApplicableTleText(
                snapshot => snapshot.TotalFines.ToString());
        }

        private static string GetTotalViolationsText()
        {
            return GetApplicableTleText(
                snapshot => snapshot.TotalViolations.ToString());
        }

        private static string GetLastReasonText()
        {
            return GetApplicableTleText(
                snapshot => string.IsNullOrWhiteSpace(snapshot.LastReason)
                    ? "None recorded"
                    : snapshot.LastReason);
        }

        private static string GetTleStatusText()
        {
            if (!TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            if (snapshot.ResolveState == SelectedVehicleResolveState.None)
            {
                return "No selected vehicle";
            }

            if (snapshot.ResolveState == SelectedVehicleResolveState.NotVehicle)
            {
                return "Traffic Law Enforcement details are unavailable because the selected object is not a vehicle";
            }

            switch (snapshot.TleApplicability)
            {
                case SelectedVehicleTleApplicability.NotApplicable:
                    return "Traffic Law Enforcement details are not applicable to this vehicle type";

                case SelectedVehicleTleApplicability.ApplicableNoLiveLaneData:
                    return snapshot.VehicleKind == SelectedVehicleKind.ParkedRoadCar
                        ? "Live lane data unavailable for parked road vehicle"
                        : "Live lane data unavailable for selected road vehicle";

                case SelectedVehicleTleApplicability.ApplicableReady:
                    return "Tracking selected road vehicle";

                default:
                    return "Traffic Law Enforcement status unavailable";
            }
        }

        private static string GetApplicableTleText(
            System.Func<SelectedVehicleDebugSnapshot, string> formatter)
        {
            if (!TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            if (snapshot.TleApplicability == SelectedVehicleTleApplicability.NotApplicable)
            {
                return "Not applicable";
            }

            return formatter(snapshot);
        }

        private static string GetVehicleInfoText(
            System.Func<SelectedVehicleDebugSnapshot, string> formatter)
        {
            if (!TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            if (!snapshot.IsVehicle)
            {
                return "Unavailable";
            }

            return formatter(snapshot);
        }

        private static string GetReadyTleText(
            System.Func<SelectedVehicleDebugSnapshot, string> formatter)
        {
            if (!TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            if (snapshot.TleApplicability != SelectedVehicleTleApplicability.ApplicableReady)
            {
                return "Unavailable";
            }

            return formatter(snapshot);
        }

        private static string GetRawClassificationText(
            System.Func<SelectedVehicleDebugSnapshot, string> formatter)
        {
            if (!TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            if (!snapshot.IsVehicle)
            {
                return "Unavailable";
            }

            return formatter(snapshot);
        }

        private static bool TryGetSelectedVehicleSnapshot(
            out SelectedVehicleDebugSnapshot snapshot)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                snapshot = default;
                return false;
            }

            SelectedVehicleBridgeSystem bridgeSystem =
                world.GetExistingSystemManaged<SelectedVehicleBridgeSystem>();

            if (bridgeSystem == null || !bridgeSystem.HasSnapshot)
            {
                snapshot = default;
                return false;
            }

            snapshot = bridgeSystem.CurrentSnapshot;
            return true;
        }

        private static string FormatEntity(Entity entity)
        {
            return entity == Entity.Null
                ? "None"
                : entity.ToString();
        }
    }
}
