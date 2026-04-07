using Game.Assets;

namespace Traffic_Law_Enforcement
{
    internal enum SaveLoadTraceRequestKind
    {
        None = 0,
        Load = 1,
        Save = 2,
    }

    internal static class SaveLoadTraceService
    {
        private const string Unknown = "unknown";

        public static SaveLoadTraceRequestKind LastRequestKind { get; private set; }
        public static string LastRequestedSaveName { get; private set; } = Unknown;
        public static string LastRequestedCityName { get; private set; } = Unknown;
        public static string LastRequestedSavePath { get; private set; } = Unknown;
        public static string LastRequestedSaveId { get; private set; } = Unknown;
        public static string LastRequestSource { get; private set; } = Unknown;

        public static void Reset()
        {
            LastRequestKind = SaveLoadTraceRequestKind.None;
            LastRequestedSaveName = Unknown;
            LastRequestedCityName = Unknown;
            LastRequestedSavePath = Unknown;
            LastRequestedSaveId = Unknown;
            LastRequestSource = Unknown;
        }

        public static void CaptureFromSaveMetadata(
            SaveGameMetadata metadata,
            string source)
        {
            if (metadata == null)
            {
                return;
            }

            SaveInfo saveInfo = metadata.target;
            Capture(
                SaveLoadTraceRequestKind.Load,
                FirstNonBlank(
                    saveInfo.displayName,
                    saveInfo.cityName,
                    metadata.name,
                    SafeToString(saveInfo.path),
                    SafeToString(saveInfo.id),
                    Unknown),
                FirstNonBlank(
                    saveInfo.cityName,
                    Unknown),
                FirstNonBlank(
                    SafeToString(saveInfo.path),
                    SafeToString(metadata.path),
                    Unknown),
                FirstNonBlank(
                    SafeToString(saveInfo.id),
                    metadata.identifier,
                    SafeToString(metadata.id),
                    Unknown),
                source);
        }

        public static void CaptureFromSaveInfo(
            SaveInfo saveInfo,
            string source)
        {
            CaptureFromSaveInfo(saveInfo, source, null);
        }

        public static void CaptureFromSaveInfo(
            SaveInfo saveInfo,
            string source,
            string fallbackName)
        {
            Capture(
                SaveLoadTraceRequestKind.Save,
                FirstNonBlank(
                    saveInfo.displayName,
                    fallbackName,
                    saveInfo.cityName,
                    SafeToString(saveInfo.path),
                    SafeToString(saveInfo.id),
                    Unknown),
                FirstNonBlank(
                    saveInfo.cityName,
                    Unknown),
                FirstNonBlank(
                    SafeToString(saveInfo.path),
                    Unknown),
                FirstNonBlank(
                    SafeToString(saveInfo.id),
                    Unknown),
                source);
        }

        public static string FormatIdentitySuffix(
            SaveLoadTraceRequestKind expectedKind)
        {
            if (expectedKind == SaveLoadTraceRequestKind.None ||
                LastRequestKind != expectedKind)
            {
                return string.Empty;
            }

            return
                $", requestKind={LastRequestKind}" +
                $", requestSource={LastRequestSource}" +
                $", name={LastRequestedSaveName}" +
                $", city={LastRequestedCityName}" +
                $", path={LastRequestedSavePath}" +
                $", id={LastRequestedSaveId}";
        }

        public static void ClearIfKind(
            SaveLoadTraceRequestKind expectedKind)
        {
            if (LastRequestKind == expectedKind)
            {
                Reset();
            }
        }

        private static void Capture(
            SaveLoadTraceRequestKind kind,
            string name,
            string city,
            string path,
            string id,
            string source)
        {
            LastRequestKind = kind;
            LastRequestedSaveName = FirstNonBlank(name, Unknown);
            LastRequestedCityName = FirstNonBlank(city, Unknown);
            LastRequestedSavePath = FirstNonBlank(path, Unknown);
            LastRequestedSaveId = FirstNonBlank(id, Unknown);
            LastRequestSource = FirstNonBlank(source, Unknown);
        }

        private static string FirstNonBlank(
            params string[] values)
        {
            for (int index = 0; index < values.Length; index += 1)
            {
                string value = values[index];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return Unknown;
        }

        private static string SafeToString(object value)
        {
            return value == null ? null : value.ToString();
        }
    }
}
