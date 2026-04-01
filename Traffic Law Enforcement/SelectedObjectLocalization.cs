using System.Collections.Generic;
using Game;
using Game.SceneFlow;

namespace Traffic_Law_Enforcement
{
    internal static class SelectedObjectLocalization
    {
        private static readonly Dictionary<string, string> s_LocalizedTextCache =
            new Dictionary<string, string>();
        private static string s_CachedLocaleId = string.Empty;
        internal static string ActiveLocaleId =>
            GameManager.instance?.localizationManager?.activeLocaleId ?? string.Empty;

        internal static string LocalizeText(string localeId, string fallback)
        {
            string activeLocaleId = ActiveLocaleId;
            if (activeLocaleId != s_CachedLocaleId)
            {
                s_CachedLocaleId = activeLocaleId;
                s_LocalizedTextCache.Clear();
            }

            if (s_LocalizedTextCache.TryGetValue(localeId, out string cachedValue))
            {
                return cachedValue;
            }

            if (GameManager.instance?.localizationManager?.activeDictionary != null &&
                GameManager.instance.localizationManager.activeDictionary.TryGetValue(localeId, out string value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                s_LocalizedTextCache[localeId] = value;
                return value;
            }

            return fallback;
        }
    }
}
