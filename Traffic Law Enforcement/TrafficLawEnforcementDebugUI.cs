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
                displayName = "Burst logging status",
                getter = BurstLoggingService.DescribeStatus
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

            DebugUI.Foldout SelectedObject = BuildSelectedObjectInspector();
            DebugUI.Foldout focusedLogging = BuildFocusedLoggingInspector();

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
                new DebugUI.Button
                {
                    displayName = "Start burst logging (5s)",
                    action = BurstLoggingService.RequestDefaultBurst
                },
                overview,
                focusedLogging,
                SelectedObject,
                recent,
                records
            };
        }

        private static DebugUI.Foldout BuildFocusedLoggingInspector()
        {
            DebugUI.Foldout focusedLogging = new DebugUI.Foldout
            {
                displayName = "Focused logging",
                opened = true
            };

            focusedLogging.children.Add(new DebugUI.Value
            {
                displayName = "Window",
                getter = GetFocusedLoggingWindowStatusText
            });
            focusedLogging.children.Add(new DebugUI.Value
            {
                displayName = "Watched vehicle count",
                getter = GetFocusedLoggingWatchedVehicleCountText
            });
            focusedLogging.children.Add(new DebugUI.Value
            {
                displayName = "Watched vehicles",
                getter = GetFocusedLoggingWatchedVehiclesText
            });
            focusedLogging.children.Add(new DebugUI.Value
            {
                displayName = "Selected vehicle",
                getter = GetFocusedLoggingSelectedVehicleText
            });
            focusedLogging.children.Add(new DebugUI.Value
            {
                displayName = "Selected role",
                getter = GetFocusedLoggingSelectedRoleText
            });
            focusedLogging.children.Add(new DebugUI.Value
            {
                displayName = "Selected watch status",
                getter = GetFocusedLoggingSelectedWatchStatusText
            });
            focusedLogging.children.Add(new DebugUI.Value
            {
                displayName = "Note",
                getter = GetFocusedLoggingNoteText
            });
            focusedLogging.children.Add(new DebugUI.Button
            {
                displayName = "Toggle focused logging window",
                action = FocusedLoggingService.ToggleWindowVisible
            });
            focusedLogging.children.Add(new DebugUI.Button
            {
                displayName = "Watch selected road vehicle",
                action = WatchSelectedRoadVehicle
            });
            focusedLogging.children.Add(new DebugUI.Button
            {
                displayName = "Unwatch selected road vehicle",
                action = UnwatchSelectedRoadVehicle
            });
            focusedLogging.children.Add(new DebugUI.Button
            {
                displayName = "Clear watched vehicles",
                action = FocusedLoggingService.ClearWatchedVehicles
            });

            return focusedLogging;
        }

        private static DebugUI.Foldout BuildSelectedObjectInspector()
        {
            DebugUI.Foldout SelectedObject = new DebugUI.Foldout
            {
                displayName = "Selected Object",
                opened = true
            };

            SelectedObject.children.Add(new DebugUI.Value
            {
                displayName = "Status",
                getter = GetSelectedObjectStatusText
            });
            SelectedObject.children.Add(new DebugUI.Value
            {
                displayName = "Classification",
                getter = GetSelectedObjectClassificationText
            });
            SelectedObject.children.Add(new DebugUI.Value
            {
                displayName = "TLE status",
                getter = GetTleStatusText
            });
            SelectedObject.children.Add(new DebugUI.Value
            {
                displayName = "Flags",
                getter = GetSelectedObjectFlagsText
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
                displayName = "Role",
                getter = GetRoleText
            });
            vehicleInfo.children.Add(new DebugUI.Value
            {
                displayName = "PT lane policy",
                getter = GetPublicTransportLanePolicyText
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
                displayName = "Public transport lane violation active",
                getter = GetPublicTransportLaneViolationActiveText
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

            DebugUI.Foldout routeDiagnostics = new DebugUI.Foldout
            {
                displayName = "Route diagnostics",
                opened = true
            };
            routeDiagnostics.children.Add(new DebugUI.Value
            {
                displayName = "Current target",
                getter = GetRouteDiagnosticsCurrentTargetText
            });
            routeDiagnostics.children.Add(new DebugUI.Value
            {
                displayName = "Current route",
                getter = GetRouteDiagnosticsCurrentRouteText
            });
            routeDiagnostics.children.Add(new DebugUI.Value
            {
                displayName = "Target road",
                getter = GetRouteDiagnosticsTargetRoadText
            });
            routeDiagnostics.children.Add(new DebugUI.Value
            {
                displayName = "Route start road",
                getter = GetRouteDiagnosticsStartOwnerRoadText
            });
            routeDiagnostics.children.Add(new DebugUI.Value
            {
                displayName = "Route end road",
                getter = GetRouteDiagnosticsEndOwnerRoadText
            });
            routeDiagnostics.children.Add(new DebugUI.Value
            {
                displayName = "Current -> target start",
                getter = GetRouteDiagnosticsDirectConnectText
            });
            routeDiagnostics.children.Add(new DebugUI.Value
            {
                displayName = "Full path -> target start",
                getter = GetRouteDiagnosticsFullPathToTargetStartText
            });
            routeDiagnostics.children.Add(new DebugUI.Value
            {
                displayName = "Navigation lanes",
                getter = GetRouteDiagnosticsNavigationLanesText
            });
            routeDiagnostics.children.Add(new DebugUI.Value
            {
                displayName = "Planned penalties",
                getter = GetRouteDiagnosticsPlannedPenaltiesText
            });
            routeDiagnostics.children.Add(new DebugUI.Value
            {
                displayName = "Penalty tags",
                getter = GetRouteDiagnosticsPenaltyTagsText
            });
            routeDiagnostics.children.Add(new DebugUI.Value
            {
                displayName = "Current explanation",
                getter = GetRouteDiagnosticsExplanationText
            });
            routeDiagnostics.children.Add(new DebugUI.Value
            {
                displayName = "Waypoint route lane",
                getter = GetRouteDiagnosticsWaypointRouteLaneText
            });
            routeDiagnostics.children.Add(new DebugUI.Value
            {
                displayName = "Connected stop",
                getter = GetRouteDiagnosticsConnectedStopText
            });

            tle.children.Add(lane);
            tle.children.Add(enforcement);
            tle.children.Add(telemetry);
            tle.children.Add(routeDiagnostics);

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

            SelectedObject.children.Add(vehicleInfo);
            SelectedObject.children.Add(tle);
            SelectedObject.children.Add(resolution);
            SelectedObject.children.Add(rawClassification);
            return SelectedObject;
        }

        private static string GetSelectedObjectStatusText()
        {
            if (!TryGetSelectedObjectSnapshot(out SelectedObjectDebugSnapshot snapshot))
            {
                return "Selected Object bridge unavailable";
            }

            switch (snapshot.ResolveState)
            {
                case SelectedObjectResolveState.None:
                    return "No object selected";

                case SelectedObjectResolveState.NotVehicle:
                    return "Selected object is not a vehicle";

                case SelectedObjectResolveState.Vehicle:
                    return "Selected Object resolved";

                default:
                    return "Selected Object status unavailable";
            }
        }

        private static string GetSelectedObjectClassificationText()
        {
            if (!TryGetSelectedObjectSnapshot(out SelectedObjectDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            switch (snapshot.VehicleKind)
            {
                case SelectedObjectKind.None:
                    return "Unavailable";

                case SelectedObjectKind.RoadCar:
                    return "Selected Object: road car";

                case SelectedObjectKind.ParkedRoadCar:
                    return "Selected Object: parked road car";

                case SelectedObjectKind.RailVehicle:
                    return "Selected Object: rail vehicle";

                case SelectedObjectKind.ParkedRailVehicle:
                    return "Selected Object: parked rail vehicle";

                case SelectedObjectKind.Tram:
                    return "Selected Object: tram";

                case SelectedObjectKind.ParkedTram:
                    return "Selected Object: parked tram";

                case SelectedObjectKind.Train:
                    return "Selected Object: train";

                case SelectedObjectKind.ParkedTrain:
                    return "Selected Object: parked train";

                case SelectedObjectKind.Subway:
                    return "Selected Object: subway";

                case SelectedObjectKind.ParkedSubway:
                    return "Selected Object: parked subway";

                case SelectedObjectKind.OtherVehicle:
                    return "Selected Object: other vehicle";

                default:
                    return "Selected Object: unknown";
            }
        }

        private static string GetSourceSelectedEntityText()
        {
            return TryGetSelectedObjectSnapshot(out SelectedObjectDebugSnapshot snapshot)
                ? FormatEntity(snapshot.SourceSelectedEntity)
                : "Unavailable";
        }

        private static string GetResolvedVehicleEntityText()
        {
            return TryGetSelectedObjectSnapshot(out SelectedObjectDebugSnapshot snapshot)
                ? FormatEntity(snapshot.ResolvedVehicleEntity)
                : "Unavailable";
        }

        private static string GetPrefabEntityText()
        {
            return TryGetSelectedObjectSnapshot(out SelectedObjectDebugSnapshot snapshot)
                ? FormatEntity(snapshot.PrefabEntity)
                : "Unavailable";
        }

        private static string GetHasPrefabRefText()
        {
            if (!TryGetSelectedObjectSnapshot(out SelectedObjectDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            return snapshot.ResolveState != SelectedObjectResolveState.None
                ? snapshot.HasPrefabRef.ToString()
                : "Unavailable";
        }

        private static string GetSelectedObjectFlagsText()
        {
            if (!TryGetSelectedObjectSnapshot(out SelectedObjectDebugSnapshot snapshot))
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

        private static string GetRoleText()
        {
            return GetVehicleInfoText(
                snapshot => string.IsNullOrWhiteSpace(snapshot.RoleText)
                    ? "Unavailable"
                    : snapshot.RoleText);
        }

        private static string GetPublicTransportLanePolicyText()
        {
            return GetVehicleInfoText(
                snapshot => string.IsNullOrWhiteSpace(snapshot.PublicTransportLanePolicyText)
                    ? "Unavailable"
                    : snapshot.PublicTransportLanePolicyText);
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

        private static string GetPublicTransportLaneViolationActiveText()
        {
            return GetReadyTleText(
                snapshot => snapshot.PublicTransportLaneViolationActive.ToString());
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

        private static string GetRouteDiagnosticsCurrentTargetText()
        {
            return GetRouteDiagnosticsText(
                snapshot => snapshot.RouteDiagnosticsCurrentTargetText);
        }

        private static string GetRouteDiagnosticsCurrentRouteText()
        {
            return GetRouteDiagnosticsText(
                snapshot => snapshot.RouteDiagnosticsCurrentRouteText);
        }

        private static string GetRouteDiagnosticsTargetRoadText()
        {
            return GetRouteDiagnosticsOptionalText(
                snapshot => snapshot.RouteDiagnosticsTargetRoadText);
        }

        private static string GetRouteDiagnosticsStartOwnerRoadText()
        {
            return GetRouteDiagnosticsOptionalText(
                snapshot => snapshot.RouteDiagnosticsStartOwnerRoadText);
        }

        private static string GetRouteDiagnosticsEndOwnerRoadText()
        {
            return GetRouteDiagnosticsOptionalText(
                snapshot => snapshot.RouteDiagnosticsEndOwnerRoadText);
        }

        private static string GetRouteDiagnosticsDirectConnectText()
        {
            return GetRouteDiagnosticsOptionalText(
                snapshot => snapshot.RouteDiagnosticsDirectConnectText);
        }

        private static string GetRouteDiagnosticsFullPathToTargetStartText()
        {
            return GetRouteDiagnosticsOptionalText(
                snapshot => snapshot.RouteDiagnosticsFullPathToTargetStartText);
        }

        private static string GetRouteDiagnosticsNavigationLanesText()
        {
            return GetRouteDiagnosticsOptionalText(
                snapshot => snapshot.RouteDiagnosticsNavigationLanesText);
        }

        private static string GetRouteDiagnosticsPlannedPenaltiesText()
        {
            return GetRouteDiagnosticsOptionalText(
                snapshot => snapshot.RouteDiagnosticsPlannedPenaltiesText);
        }

        private static string GetRouteDiagnosticsPenaltyTagsText()
        {
            return GetRouteDiagnosticsOptionalText(
                snapshot => snapshot.RouteDiagnosticsPenaltyTagsText);
        }

        private static string GetRouteDiagnosticsExplanationText()
        {
            return GetRouteDiagnosticsOptionalText(
                snapshot => snapshot.RouteDiagnosticsExplanationText);
        }

        private static string GetRouteDiagnosticsWaypointRouteLaneText()
        {
            return GetRouteDiagnosticsOptionalText(
                snapshot => snapshot.RouteDiagnosticsWaypointRouteLaneText);
        }

        private static string GetRouteDiagnosticsConnectedStopText()
        {
            return GetRouteDiagnosticsOptionalText(
                snapshot => snapshot.RouteDiagnosticsConnectedStopText);
        }

        private static string GetTleStatusText()
        {
            if (!TryGetSelectedObjectSnapshot(out SelectedObjectDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            if (snapshot.ResolveState == SelectedObjectResolveState.None)
            {
                return "No Selected Object";
            }

            if (snapshot.ResolveState == SelectedObjectResolveState.NotVehicle)
            {
                return "Traffic Law Enforcement details are unavailable because the selected object is not a vehicle";
            }

            switch (snapshot.TleApplicability)
            {
                case SelectedObjectTleApplicability.NotApplicable:
                    return "Traffic Law Enforcement details are not applicable to this vehicle type";

                case SelectedObjectTleApplicability.ApplicableNoLiveLaneData:
                    return snapshot.VehicleKind == SelectedObjectKind.ParkedRoadCar
                        ? "Live lane data unavailable for parked road vehicle"
                        : "Live lane data unavailable for selected road vehicle";

                case SelectedObjectTleApplicability.ApplicableReady:
                    return "Tracking selected road vehicle";

                default:
                    return "Traffic Law Enforcement status unavailable";
            }
        }

        private static string GetApplicableTleText(
            System.Func<SelectedObjectDebugSnapshot, string> formatter)
        {
            if (!TryGetSelectedObjectSnapshot(out SelectedObjectDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            if (snapshot.TleApplicability == SelectedObjectTleApplicability.NotApplicable)
            {
                return "Not applicable";
            }

            return formatter(snapshot);
        }

        private static string GetVehicleInfoText(
            System.Func<SelectedObjectDebugSnapshot, string> formatter)
        {
            if (!TryGetSelectedObjectSnapshot(out SelectedObjectDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            if (!snapshot.IsVehicle)
            {
                return "Unavailable";
            }

            return formatter(snapshot);
        }

        private static string GetFocusedLoggingWindowStatusText()
        {
            return FocusedLoggingService.IsWindowVisible
                ? "Visible"
                : "Hidden";
        }

        private static string GetFocusedLoggingWatchedVehicleCountText()
        {
            return FocusedLoggingService.WatchedVehicleCount.ToString();
        }

        private static string GetFocusedLoggingWatchedVehiclesText()
        {
            string summary = FocusedLoggingService.DescribeWatchedVehicles();
            return string.IsNullOrWhiteSpace(summary)
                ? "None"
                : summary;
        }

        private static string GetFocusedLoggingSelectedVehicleText()
        {
            return TryGetSelectedReadyRoadVehicle(
                    out SelectedObjectDebugSnapshot _,
                    out Entity vehicle)
                ? FocusedLoggingService.FormatEntity(vehicle)
                : "None";
        }

        private static string GetFocusedLoggingSelectedRoleText()
        {
            return TryGetSelectedReadyRoadVehicle(
                    out SelectedObjectDebugSnapshot snapshot,
                    out Entity _)
                ? string.IsNullOrWhiteSpace(snapshot.RoleText)
                    ? "Unavailable"
                    : snapshot.RoleText.Trim()
                : "Unavailable";
        }

        private static string GetFocusedLoggingSelectedWatchStatusText()
        {
            return TryGetSelectedReadyRoadVehicle(
                    out SelectedObjectDebugSnapshot _,
                    out Entity vehicle)
                ? FocusedLoggingService.IsWatched(vehicle)
                    ? "Watched"
                    : "Not watched"
                : "Unavailable";
        }

        private static string GetFocusedLoggingNoteText()
        {
            return "Focused logging only records log types enabled in Debug options.";
        }

        private static void WatchSelectedRoadVehicle()
        {
            if (TryGetSelectedReadyRoadVehicle(
                    out SelectedObjectDebugSnapshot _,
                    out Entity vehicle))
            {
                FocusedLoggingService.AddWatchedVehicle(vehicle);
            }
        }

        private static void UnwatchSelectedRoadVehicle()
        {
            if (TryGetSelectedReadyRoadVehicle(
                    out SelectedObjectDebugSnapshot _,
                    out Entity vehicle))
            {
                FocusedLoggingService.RemoveWatchedVehicle(vehicle);
            }
        }

        private static string GetReadyTleText(
            System.Func<SelectedObjectDebugSnapshot, string> formatter)
        {
            if (!TryGetSelectedObjectSnapshot(out SelectedObjectDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            if (snapshot.TleApplicability != SelectedObjectTleApplicability.ApplicableReady)
            {
                return "Unavailable";
            }

            return formatter(snapshot);
        }

        private static string GetRouteDiagnosticsText(
            System.Func<SelectedObjectDebugSnapshot, string> formatter)
        {
            if (!TryGetSelectedObjectSnapshot(out SelectedObjectDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            if (snapshot.TleApplicability != SelectedObjectTleApplicability.ApplicableReady ||
                !snapshot.HasRouteDiagnostics)
            {
                return "Unavailable";
            }

            string text = formatter(snapshot);
            return string.IsNullOrWhiteSpace(text)
                ? "Unavailable"
                : text.Trim();
        }

        private static string GetRouteDiagnosticsOptionalText(
            System.Func<SelectedObjectDebugSnapshot, string> formatter)
        {
            if (!TryGetSelectedObjectSnapshot(out SelectedObjectDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            if (snapshot.TleApplicability != SelectedObjectTleApplicability.ApplicableReady ||
                !snapshot.HasRouteDiagnostics)
            {
                return "Unavailable";
            }

            string text = formatter(snapshot);
            return string.IsNullOrWhiteSpace(text)
                ? "None"
                : text.Trim();
        }

        private static string GetRawClassificationText(
            System.Func<SelectedObjectDebugSnapshot, string> formatter)
        {
            if (!TryGetSelectedObjectSnapshot(out SelectedObjectDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            if (!snapshot.IsVehicle)
            {
                return "Unavailable";
            }

            return formatter(snapshot);
        }

        private static bool TryGetSelectedObjectSnapshot(
            out SelectedObjectDebugSnapshot snapshot)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                snapshot = default;
                return false;
            }

            SelectedObjectBridgeSystem bridgeSystem =
                world.GetExistingSystemManaged<SelectedObjectBridgeSystem>();

            if (bridgeSystem == null || !bridgeSystem.HasSnapshot)
            {
                snapshot = default;
                return false;
            }

            snapshot = bridgeSystem.CurrentSnapshot;
            return true;
        }

        private static bool TryGetSelectedReadyRoadVehicle(
            out SelectedObjectDebugSnapshot snapshot,
            out Entity vehicle)
        {
            vehicle = Entity.Null;
            if (!TryGetSelectedObjectSnapshot(out snapshot) ||
                snapshot.TleApplicability != SelectedObjectTleApplicability.ApplicableReady ||
                snapshot.ResolvedVehicleEntity == Entity.Null)
            {
                return false;
            }

            vehicle = snapshot.ResolvedVehicleEntity;
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

