using System.Collections.Generic;
using System.Text;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    internal static class FocusedLoggingService
    {
        private static readonly HashSet<Entity> s_WatchedVehicles = new HashSet<Entity>();
        private static readonly List<Entity> s_RemovedVehiclesBuffer = new List<Entity>();
        private static readonly List<Entity> s_SortedWatchedVehiclesBuffer = new List<Entity>();
        private static string s_WatchedVehicleSummary = string.Empty;
        private static bool s_WatchedVehicleSummaryDirty = true;
        private static bool s_WindowVisible;

        internal static bool IsWindowVisible => s_WindowVisible;

        internal static bool HasWatchedVehicles => s_WatchedVehicles.Count > 0;

        internal static int WatchedVehicleCount => s_WatchedVehicles.Count;

        internal static void Reset()
        {
            s_WatchedVehicles.Clear();
            MarkWatchedVehiclesChanged();
            s_WindowVisible = false;
            FocusedRouteDiagnosticsPatchController.Sync();
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
                MarkWatchedVehiclesChanged();
                FocusedRouteDiagnosticsPatchController.Sync();
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
                MarkWatchedVehiclesChanged();
                FocusedRouteDiagnosticsPatchController.Sync();
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

        internal static void CopyWatchedVehicles(ICollection<Entity> destination)
        {
            if (destination == null)
            {
                return;
            }

            foreach (Entity vehicle in s_WatchedVehicles)
            {
                destination.Add(vehicle);
            }
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

            s_RemovedVehiclesBuffer.Clear();
            foreach (Entity vehicle in s_WatchedVehicles)
            {
                if (vehicle != Entity.Null && entityManager.Exists(vehicle))
                {
                    continue;
                }

                s_RemovedVehiclesBuffer.Add(vehicle);
            }

            if (s_RemovedVehiclesBuffer.Count == 0)
            {
                return 0;
            }

            for (int index = 0; index < s_RemovedVehiclesBuffer.Count; index++)
            {
                s_WatchedVehicles.Remove(s_RemovedVehiclesBuffer[index]);
            }

            MarkWatchedVehiclesChanged();
            FocusedRouteDiagnosticsPatchController.Sync();
            Mod.log.Info(
                $"[FocusedLogging] Pruned watched vehicles: removed={s_RemovedVehiclesBuffer.Count}, remaining={s_WatchedVehicles.Count}");

            int removedCount = s_RemovedVehiclesBuffer.Count;
            s_RemovedVehiclesBuffer.Clear();

            return removedCount;
        }

        internal static bool RemoveWatchedVehicleBecauseMissing(
            Entity vehicle,
            string reason)
        {
            if (vehicle == Entity.Null)
            {
                return false;
            }

            bool removed = s_WatchedVehicles.Remove(vehicle);
            if (removed)
            {
                MarkWatchedVehiclesChanged();
                FocusedRouteDiagnosticsPatchController.Sync();
                Mod.log.Info(
                    $"[FocusedLogging] Auto-removed watched vehicle: {FormatEntity(vehicle)}, reason={NormalizeReason(reason)}");
            }

            return removed;
        }

        internal static string DescribeWatchedVehicles(int maxDisplayed = 6)
        {
            if (s_WatchedVehicles.Count == 0)
            {
                s_WatchedVehicleSummary = string.Empty;
                s_WatchedVehicleSummaryDirty = false;
                return string.Empty;
            }

            if (maxDisplayed == 6 && !s_WatchedVehicleSummaryDirty)
            {
                return s_WatchedVehicleSummary;
            }

            s_SortedWatchedVehiclesBuffer.Clear();
            foreach (Entity vehicle in s_WatchedVehicles)
            {
                s_SortedWatchedVehiclesBuffer.Add(vehicle);
            }

            s_SortedWatchedVehiclesBuffer.Sort(CompareEntities);

            StringBuilder summaryBuilder = new StringBuilder(s_SortedWatchedVehiclesBuffer.Count * 14);
            int displayed = 0;
            for (int index = 0; index < s_SortedWatchedVehiclesBuffer.Count; index++)
            {
                if (displayed >= maxDisplayed)
                {
                    break;
                }

                if (displayed > 0)
                {
                    summaryBuilder.Append(", ");
                }

                summaryBuilder.Append(FormatEntity(s_SortedWatchedVehiclesBuffer[index]));
                displayed += 1;
            }

            int remaining = s_SortedWatchedVehiclesBuffer.Count - displayed;
            if (remaining > 0)
            {
                summaryBuilder.Append(" (+").Append(remaining).Append(" more)");
            }

            string summary = summaryBuilder.ToString();

            if (maxDisplayed == 6)
            {
                s_WatchedVehicleSummary = summary;
                s_WatchedVehicleSummaryDirty = false;
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

            MarkWatchedVehiclesChanged();
            FocusedRouteDiagnosticsPatchController.Sync();
            Mod.log.Info(
                $"[FocusedLogging] Cleared watched vehicles: count={cleared}, reason={reason}");
        }

        private static void MarkWatchedVehiclesChanged()
        {
            s_WatchedVehicleSummaryDirty = true;
            if (s_WatchedVehicles.Count == 0)
            {
                s_WatchedVehicleSummary = string.Empty;
            }
        }

        private static int CompareEntities(Entity left, Entity right)
        {
            int indexComparison = left.Index.CompareTo(right.Index);
            return indexComparison != 0
                ? indexComparison
                : left.Version.CompareTo(right.Version);
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason)
                ? "unknown"
                : reason.Trim();
        }
    }
}
