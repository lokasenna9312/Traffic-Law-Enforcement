using System;
using System.Collections.Generic;
using System.IO;
using Colossal;

namespace Traffic_Law_Enforcement
{
    internal sealed class PropertiesLocaleSource : IDictionarySource
    {
        private readonly string m_FilePath;
        private readonly Dictionary<string, string> m_SymbolicToGameKeys;
        private Dictionary<string, string> m_ResolvedEntries;

        public PropertiesLocaleSource(
            string filePath,
            Dictionary<string, string> symbolicToGameKeys)
        {
            m_FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            m_SymbolicToGameKeys = symbolicToGameKeys ?? throw new ArgumentNullException(nameof(symbolicToGameKeys));
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            EnsureLoaded();

            foreach (var pair in m_ResolvedEntries)
            {
                yield return pair;
            }
        }

        public void Unload()
        {
            m_ResolvedEntries = null;
        }

        private void EnsureLoaded()
        {
            if (m_ResolvedEntries != null)
            {
                return;
            }

            Dictionary<string, string> resolved = new Dictionary<string, string>();
            Dictionary<string, string> symbolicEntries = LoadKeyValueFile(m_FilePath);

            foreach (var pair in symbolicEntries)
            {
                if (m_SymbolicToGameKeys.TryGetValue(pair.Key, out string actualLocaleKey))
                {
                    resolved[actualLocaleKey] = pair.Value ?? string.Empty;
                }
            }

            m_ResolvedEntries = resolved;
        }

        public static Dictionary<string, string> LoadKeyValueFile(string filePath)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (string rawLine in File.ReadAllLines(filePath))
            {
                string line = rawLine?.Trim();

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (line.StartsWith("#"))
                {
                    continue;
                }

                int separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, separatorIndex).Trim();
                string value = line.Substring(separatorIndex + 1).Trim();

                result[key] = value;
            }

            return result;
        }
    }
}