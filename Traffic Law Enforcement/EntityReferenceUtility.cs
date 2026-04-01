using Game;
using Game.UI;
using System.Text.RegularExpressions;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    internal static class EntityReferenceUtility
    {
        private static readonly Regex s_EntitySelectionPattern =
            new Regex(
                "^\\s*#?(?<index>\\d+)\\s*:\\s*[vV](?<version>\\d+)\\s*$",
                RegexOptions.Compiled);

        internal static bool TryParse(string input, out Entity entity)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                entity = Entity.Null;
                return false;
            }

            string normalized = input.Trim();
            if (URI.TryParseEntity(normalized, out entity))
            {
                return true;
            }

            Match match = s_EntitySelectionPattern.Match(normalized);
            if (!match.Success)
            {
                entity = Entity.Null;
                return false;
            }

            entity = new Entity
            {
                Index = int.Parse(match.Groups["index"].Value),
                Version = int.Parse(match.Groups["version"].Value)
            };
            return true;
        }
    }
}
