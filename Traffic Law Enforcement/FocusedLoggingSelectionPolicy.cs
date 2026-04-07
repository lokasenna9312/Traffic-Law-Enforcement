using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    internal struct FocusedLoggingSelectionState
    {
        public Entity Vehicle;
        public bool HasSelectedRoadVehicle;
        public bool HasEligibleSelection;
        public bool WatchedVehicle;
        public bool CanWatch;
        public bool CanUnwatch;
    }

    internal static class FocusedLoggingSelectionPolicy
    {
        internal static FocusedLoggingSelectionState Build(
            SelectedObjectDebugSnapshot snapshot)
        {
            bool hasSelectedRoadVehicle =
                TryGetSelectedRoadVehicle(snapshot, out Entity vehicle);
            bool watchedVehicle =
                hasSelectedRoadVehicle &&
                FocusedLoggingService.IsWatched(vehicle);
            bool canWatch =
                hasSelectedRoadVehicle &&
                snapshot.TleApplicability == SelectedObjectTleApplicability.ApplicableReady &&
                !watchedVehicle;
            bool canUnwatch =
                hasSelectedRoadVehicle &&
                watchedVehicle;

            return new FocusedLoggingSelectionState
            {
                Vehicle = vehicle,
                HasSelectedRoadVehicle = hasSelectedRoadVehicle,
                HasEligibleSelection = canWatch || canUnwatch,
                WatchedVehicle = watchedVehicle,
                CanWatch = canWatch,
                CanUnwatch = canUnwatch,
            };
        }

        internal static bool TryGetSelectedRoadVehicle(
            SelectedObjectDebugSnapshot snapshot,
            out Entity vehicle)
        {
            if (IsFocusedLoggingRoadVehicle(snapshot) &&
                snapshot.ResolvedVehicleEntity != Entity.Null)
            {
                vehicle = snapshot.ResolvedVehicleEntity;
                return true;
            }

            vehicle = Entity.Null;
            return false;
        }

        private static bool IsFocusedLoggingRoadVehicle(
            SelectedObjectDebugSnapshot snapshot)
        {
            return snapshot.VehicleKind == SelectedObjectKind.RoadCar ||
                snapshot.VehicleKind == SelectedObjectKind.ParkedRoadCar;
        }
    }
}
