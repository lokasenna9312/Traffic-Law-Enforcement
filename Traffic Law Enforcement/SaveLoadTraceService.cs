using Game.Assets;

namespace Traffic_Law_Enforcement
{
    public static class SaveLoadTraceService
    {
        public static string LastRequestedSaveName { get; private set; } = "unknown";
        public static string LastRequestedCityName { get; private set; } = "unknown";
        public static string LastRequestedSavePath { get; private set; } = "unknown";
        public static string LastRequestedSaveId { get; private set; } = "unknown";
        public static string LastRequestSource { get; private set; } = "unknown";

        public static void Reset()
        {
            LastRequestedSaveName = "unknown";
            LastRequestedCityName = "unknown";
            LastRequestedSavePath = "unknown";
            LastRequestedSaveId = "unknown";
            LastRequestSource = "unknown";
        }

        public static void CaptureFromSaveMetadata(
            SaveGameMetadata saveGameMetadata,
            string source)
        {
            if (saveGameMetadata == null)
            {
                return;
            }

            SaveInfo saveInfo = saveGameMetadata.target;
            object boxedSaveInfo = saveInfo;
            if (boxedSaveInfo == null)
            {
                return;
            }

            LastRequestedSaveName = FirstNonBlank(
                saveInfo.displayName,
                saveInfo.cityName,
                SafeToString(saveInfo.path),
                SafeToString(saveInfo.id),
                "unknown");

            LastRequestedCityName = FirstNonBlank(
                saveInfo.cityName,
                "unknown");

            LastRequestedSavePath = FirstNonBlank(
                SafeToString(saveInfo.path),
                "unknown");

            LastRequestedSaveId = FirstNonBlank(
                SafeToString(saveInfo.id),
                "unknown");

            LastRequestSource = FirstNonBlank(
                source,
                "unknown");

            EnforcementLoggingPolicy.RecordSaveIdentification(
                $"[SAVELOAD] Pending load captured: " +
                $"source={LastRequestSource}, " +
                $"name={LastRequestedSaveName}, " +
                $"city={LastRequestedCityName}, " +
                $"path={LastRequestedSavePath}, " +
                $"id={LastRequestedSaveId}");
        }

        public static string DescribePendingLoad()
        {
            return
                $"pendingLoadName={LastRequestedSaveName}, " +
                $"pendingLoadCity={LastRequestedCityName}, " +
                $"pendingLoadPath={LastRequestedSavePath}, " +
                $"pendingLoadId={LastRequestedSaveId}, " +
                $"pendingLoadSource={LastRequestSource}";
        }

        private static string FirstNonBlank(params string[] values)
        {
            for (int index = 0; index < values.Length; index += 1)
            {
                string value = values[index];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return "unknown";
        }

        private static string SafeToString(object value)
        {
            return value == null ? null : value.ToString();
        }
    }
}