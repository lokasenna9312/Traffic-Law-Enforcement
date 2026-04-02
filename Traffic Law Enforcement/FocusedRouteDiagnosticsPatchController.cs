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

            if (shouldApplyPatches)
            {
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
            }

            if (setupPatchWasApplied == PathfindSetupSystemPatches.IsApplied)
            {
                return;
            }

            Mod.log.Info(
                $"Focused route diagnostics patch state updated: requested={enableFocusedRouteDiagnostics}, " +
                $"effective={shouldApplyPatches}, " +
                $"watchedCount={FocusedLoggingService.WatchedVehicleCount}, " +
                $"setupPatch={PathfindSetupSystemPatches.IsApplied}");
        }

        internal static void RemoveAll()
        {
            Sync(enableFocusedRouteDiagnostics: false);
        }
    }
}
