using System.Collections.Generic;
using Game.Debug;
using UnityEngine.Rendering;

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
                getter = () => PathfindingPenaltyTelemetry.ModifiedPrefabCount
            });
            overview.children.Add(new DebugUI.Value
            {
                displayName = "Pathfinding money overrides",
                getter = () => PathfindingPenaltyTelemetry.OverrideSummary
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
                recent,
                records
            };
        }
    }
}
