using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    internal static class CurrentRouteClickTraceLogging
    {
        private static int s_NextTraceId;
        private static int s_ActiveTraceId;
        private static int s_PostClickUiUpdatesRemaining;
        private static Entity s_ActiveVehicle;
        private static string s_LastBuilderSignature = string.Empty;

        internal static int BeginClickTrace(Entity vehicle)
        {
            s_ActiveTraceId = ++s_NextTraceId;
            s_PostClickUiUpdatesRemaining = 2;
            s_ActiveVehicle = vehicle;
            s_LastBuilderSignature = string.Empty;
            return s_ActiveTraceId;
        }

        internal static bool TryConsumePostClickUiUpdate(out int traceId)
        {
            if (s_PostClickUiUpdatesRemaining <= 0)
            {
                traceId = 0;
                return false;
            }

            traceId = s_ActiveTraceId;
            s_PostClickUiUpdatesRemaining -= 1;
            return true;
        }

        internal static void LogUi(int traceId, string stage, string message)
        {
            Mod.log.Info($"[CurrentRouteClickTrace #{traceId}] {stage}: {message}");
        }

        internal static void LogBuilderStateIfChanged(
            Entity vehicle,
            bool hasCurrentRoute,
            Entity rawCurrentRoute,
            Entity resolvedCurrentRoute,
            string resolutionReason)
        {
            string signature =
                $"{vehicle.Index}:{vehicle.Version}|{hasCurrentRoute}|{rawCurrentRoute.Index}:{rawCurrentRoute.Version}|{resolvedCurrentRoute.Index}:{resolvedCurrentRoute.Version}|{resolutionReason}";

            if (string.Equals(signature, s_LastBuilderSignature, System.StringComparison.Ordinal))
            {
                return;
            }

            s_LastBuilderSignature = signature;

            string prefix =
                vehicle == s_ActiveVehicle && s_ActiveTraceId > 0
                    ? $"[CurrentRouteClickTrace #{s_ActiveTraceId}][Builder]"
                    : "[CurrentRouteClickTrace][Builder]";

            Mod.log.Info(
                $"{prefix} vehicle={FormatEntity(vehicle)}, hasCurrentRoute={hasCurrentRoute}, rawCurrentRoute={FormatEntity(rawCurrentRoute)}, resolvedClickableCandidate={FormatEntity(resolvedCurrentRoute)}, resolution={resolutionReason}");
        }

        internal static string FormatEntity(Entity entity)
        {
            return FocusedLoggingService.FormatEntity(entity);
        }
    }
}
