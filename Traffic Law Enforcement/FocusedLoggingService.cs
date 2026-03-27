using System.Collections.Generic;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    internal static class FocusedLoggingService
    {
        private static readonly HashSet<Entity> s_WatchedVehicles = new HashSet<Entity>();
        private static bool s_WindowVisible;

        internal static bool IsWindowVisible => s_WindowVisible;

        internal static bool HasWatchedVehicles => s_WatchedVehicles.Count > 0;

        internal static int WatchedVehicleCount => s_WatchedVehicles.Count;

        internal static void Reset()
        {
            s_WatchedVehicles.Clear();
            s_WindowVisible = false;
        }

        internal static void SetWindowVisible(bool visible)
        {
            s_WindowVisible = visible;
        }

        internal static void ToggleWindowVisible()
        {
            s_WindowVisible = !s_WindowVisible;
        }

        internal static bool AddWatchedVehicle(Entity vehicle)
        {
            if (vehicle == Entity.Null)
            {
                return false;
            }

            bool added = s_WatchedVehicles.Add(vehicle);
            if (added)
            {
                Mod.log.Info($"[FocusedLogging] Added watched vehicle: {FormatEntity(vehicle)}");
            }

            return added;
        }

        internal static bool RemoveWatchedVehicle(Entity vehicle)
        {
            if (vehicle == Entity.Null)
            {
                return false;
            }

            bool removed = s_WatchedVehicles.Remove(vehicle);
            if (removed)
            {
                Mod.log.Info($"[FocusedLogging] Removed watched vehicle: {FormatEntity(vehicle)}");
            }

            return removed;
        }

        internal static void ClearWatchedVehicles()
        {
            ClearWatchedVehiclesInternal("user request");
        }

        internal static void ClearWatchedVehiclesForRuntimeWorldReset(int runtimeWorldGeneration)
        {
            ClearWatchedVehiclesInternal($"runtime world reset: generation={runtimeWorldGeneration}");
        }

        internal static bool IsWatched(Entity vehicle)
        {
            return vehicle != Entity.Null && s_WatchedVehicles.Contains(vehicle);
        }

        internal static void AppendWatchedVehicles(
            ISet<Entity> destination,
            EntityManager entityManager)
        {
            if (destination == null)
            {
                return;
            }

            PruneMissingVehicles(entityManager);
            foreach (Entity vehicle in s_WatchedVehicles)
            {
                destination.Add(vehicle);
            }
        }

        internal static int PruneMissingVehicles(EntityManager entityManager)
        {
            if (s_WatchedVehicles.Count == 0)
            {
                return 0;
            }

            List<Entity> missingVehicles = null;
            foreach (Entity vehicle in s_WatchedVehicles)
            {
                if (vehicle != Entity.Null && entityManager.Exists(vehicle))
                {
                    continue;
                }

                (missingVehicles ??= new List<Entity>()).Add(vehicle);
            }

            if (missingVehicles == null)
            {
                return 0;
            }

            for (int index = 0; index < missingVehicles.Count; index++)
            {
                s_WatchedVehicles.Remove(missingVehicles[index]);
            }

            Mod.log.Info(
                $"[FocusedLogging] Pruned watched vehicles: removed={missingVehicles.Count}, remaining={s_WatchedVehicles.Count}");

            return missingVehicles.Count;
        }

        internal static string DescribeWatchedVehicles(int maxDisplayed = 6)
        {
            if (s_WatchedVehicles.Count == 0)
            {
                return string.Empty;
            }

            List<Entity> vehicles = new List<Entity>(s_WatchedVehicles);
            vehicles.Sort(CompareEntities);

            List<string> parts = new List<string>(vehicles.Count);
            int displayed = 0;
            for (int index = 0; index < vehicles.Count; index++)
            {
                if (displayed >= maxDisplayed)
                {
                    break;
                }

                parts.Add(FormatEntity(vehicles[index]));
                displayed += 1;
            }

            string summary = string.Join(", ", parts.ToArray());
            int remaining = vehicles.Count - displayed;
            if (remaining > 0)
            {
                summary += $" (+{remaining} more)";
            }

            return summary;
        }

        internal static string FormatEntity(Entity entity)
        {
            return entity == Entity.Null
                ? "None"
                : $"#{entity.Index}:v{entity.Version}";
        }

        private static void ClearWatchedVehiclesInternal(string reason)
        {
            if (s_WatchedVehicles.Count == 0)
            {
                return;
            }

            int cleared = s_WatchedVehicles.Count;
            s_WatchedVehicles.Clear();

            Mod.log.Info(
                $"[FocusedLogging] Cleared watched vehicles: count={cleared}, reason={reason}");
        }

        private static int CompareEntities(Entity left, Entity right)
        {
            int indexComparison = left.Index.CompareTo(right.Index);
            return indexComparison != 0
                ? indexComparison
                : left.Version.CompareTo(right.Version);
        }
    }
}
