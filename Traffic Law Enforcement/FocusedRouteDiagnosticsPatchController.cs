namespace Traffic_Law_Enforcement
{
    internal static class FocusedRouteDiagnosticsPatchController
    {
        internal static void Sync()
        {
            Sync(EnforcementLoggingPolicy.ShouldLogFocusedRouteRebuildDiagnostics());
        }

        internal static void Sync(bool enableFocusedRouteDiagnostics)
        {
            bool shouldApplyPatches =
                enableFocusedRouteDiagnostics &&
                FocusedLoggingService.HasWatchedVehicles;
            bool setupPatchWasApplied = PathfindSetupSystemPatches.IsApplied;
            bool candidateProbePatchWasApplied = PathfindCandidateProbePatches.IsApplied;

            if (shouldApplyPatches)
            {
                if (!PathfindCandidateProbePatches.IsApplied)
                {
                    PathfindCandidateProbePatches.Apply();
                }

                if (!PathfindSetupSystemPatches.IsApplied)
                {
                    PathfindSetupSystemPatches.Apply();
                }
            }
            else
            {
                if (PathfindSetupSystemPatches.IsApplied)
                {
                    PathfindSetupSystemPatches.Remove();
                }

                if (PathfindCandidateProbePatches.IsApplied)
                {
                    PathfindCandidateProbePatches.Remove();
                }
            }

            if (setupPatchWasApplied == PathfindSetupSystemPatches.IsApplied &&
                candidateProbePatchWasApplied == PathfindCandidateProbePatches.IsApplied)
            {
                return;
            }

            Mod.log.Info(
                $"Focused route diagnostics patch state updated: requested={enableFocusedRouteDiagnostics}, " +
                $"effective={shouldApplyPatches}, " +
                $"watchedCount={FocusedLoggingService.WatchedVehicleCount}, " +
                $"setupPatch={PathfindSetupSystemPatches.IsApplied}, " +
                $"candidateProbePatch={PathfindCandidateProbePatches.IsApplied}");
        }

        internal static void RemoveAll()
        {
            Sync(enableFocusedRouteDiagnostics: false);
        }
    }
}
