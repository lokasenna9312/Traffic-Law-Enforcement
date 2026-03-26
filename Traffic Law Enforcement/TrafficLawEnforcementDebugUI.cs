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
                displayName = "Source selected entity",
                getter = GetSourceSelectedEntityText
            });
            selectedVehicle.children.Add(new DebugUI.Value
            {
                displayName = "Resolved vehicle entity",
                getter = GetResolvedVehicleEntityText
            });
            selectedVehicle.children.Add(new DebugUI.Value
            {
                displayName = "Flags",
                getter = GetSelectedVehicleFlagsText
            });

            DebugUI.Foldout identity = new DebugUI.Foldout
            {
                displayName = "Identity",
                opened = true
            };
            identity.children.Add(new DebugUI.Value
            {
                displayName = "Vehicle index",
                getter = GetVehicleIndexText
            });
            identity.children.Add(new DebugUI.Value
            {
                displayName = "Role / type",
                getter = GetRoleOrTypeText
            });
            identity.children.Add(new DebugUI.Value
            {
                displayName = "Has traffic law profile",
                getter = GetHasTrafficLawProfileText
            });
            identity.children.Add(new DebugUI.Value
            {
                displayName = "Trailer child",
                getter = GetTrailerChildText
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

            selectedVehicle.children.Add(identity);
            selectedVehicle.children.Add(lane);
            selectedVehicle.children.Add(enforcement);
            selectedVehicle.children.Add(telemetry);
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
                    return "No vehicle selected";

                case SelectedVehicleResolveState.NotVehicle:
                    return "Selected object is not a vehicle";

                case SelectedVehicleResolveState.VehicleNotSupported:
                    return "Selected vehicle is not a supported road car";

                case SelectedVehicleResolveState.ParkedVehicle:
                    return "Selected vehicle is parked";

                case SelectedVehicleResolveState.RoadCarNoLaneData:
                    return "Road car selected, but live lane data is unavailable";

                case SelectedVehicleResolveState.Ready:
                    return "Tracking selected road car";

                default:
                    return "Selected vehicle status unavailable";
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

        private static string GetSelectedVehicleFlagsText()
        {
            if (!TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            return
                $"Vehicle={snapshot.IsVehicle}, " +
                $"Car={snapshot.IsCar}, " +
                $"Parked={snapshot.IsParked}, " +
                $"Lane={snapshot.HasCarCurrentLane}";
        }

        private static string GetVehicleIndexText()
        {
            return TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot) &&
                snapshot.VehicleIndex >= 0
                ? snapshot.VehicleIndex.ToString()
                : "Unavailable";
        }

        private static string GetRoleOrTypeText()
        {
            if (!TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            return string.IsNullOrWhiteSpace(snapshot.RoleOrTypeText)
                ? "Unavailable"
                : snapshot.RoleOrTypeText;
        }

        private static string GetHasTrafficLawProfileText()
        {
            return TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot)
                ? snapshot.HasTrafficLawProfile.ToString()
                : "Unavailable";
        }

        private static string GetTrailerChildText()
        {
            return TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot)
                ? snapshot.IsTrailerChild.ToString()
                : "Unavailable";
        }

        private static string GetCurrentLaneEntityText()
        {
            return GetReadySelectedVehicleText(
                snapshot => FormatEntity(snapshot.CurrentLaneEntity));
        }

        private static string GetPreviousLaneEntityText()
        {
            return GetReadySelectedVehicleText(
                snapshot => FormatEntity(snapshot.PreviousLaneEntity));
        }

        private static string GetLaneChangeCountText()
        {
            return GetReadySelectedVehicleText(
                snapshot => snapshot.LaneChangeCount.ToString());
        }

        private static string GetPtLaneViolationActiveText()
        {
            return GetReadySelectedVehicleText(
                snapshot => snapshot.PtLaneViolationActive.ToString());
        }

        private static string GetPendingExitActiveText()
        {
            return GetReadySelectedVehicleText(
                snapshot => snapshot.PendingExitActive.ToString());
        }

        private static string GetPermissionStateSummaryText()
        {
            return GetReadySelectedVehicleText(
                snapshot => string.IsNullOrWhiteSpace(snapshot.PermissionStateSummary)
                    ? "Unavailable"
                    : snapshot.PermissionStateSummary);
        }

        private static string GetTotalFinesText()
        {
            return GetReadySelectedVehicleText(
                snapshot => snapshot.TotalFines.ToString());
        }

        private static string GetTotalViolationsText()
        {
            return GetReadySelectedVehicleText(
                snapshot => snapshot.TotalViolations.ToString());
        }

        private static string GetLastReasonText()
        {
            return GetReadySelectedVehicleText(
                snapshot => string.IsNullOrWhiteSpace(snapshot.LastReason)
                    ? "None recorded"
                    : snapshot.LastReason);
        }

        private static string GetReadySelectedVehicleText(
            System.Func<SelectedVehicleDebugSnapshot, string> formatter)
        {
            if (!TryGetSelectedVehicleSnapshot(out SelectedVehicleDebugSnapshot snapshot))
            {
                return "Unavailable";
            }

            if (snapshot.ResolveState != SelectedVehicleResolveState.Ready)
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