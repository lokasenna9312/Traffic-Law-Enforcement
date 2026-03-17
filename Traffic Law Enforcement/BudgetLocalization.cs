using System.Collections.Generic;
using Colossal;

namespace Traffic_Law_Enforcement
{
    internal sealed class BudgetLocaleEN : IDictionarySource
    {
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { BudgetUIPatches.FineIncomeBudgetItemLocaleId, "Traffic law enforcement" },
                { BudgetUIPatches.FineIncomePublicTransportLaneLocaleId, "Public-transport lane violations" },
                { BudgetUIPatches.FineIncomeMidBlockCrossingLocaleId, "Centerline crossings" },
                { BudgetUIPatches.FineIncomeIntersectionMovementLocaleId, "Intersection rule violations" },
                { BudgetUIPatches.FineIncomeBudgetDescriptionLocaleId, "Fine revenue collected from traffic-law enforcement during the last 1 in-game month." },
            };
        }

        public void Unload()
        {
        }
    }

    internal sealed class BudgetLocaleKO : IDictionarySource
    {
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { BudgetUIPatches.FineIncomeBudgetItemLocaleId, "교통법규 단속" },
                { BudgetUIPatches.FineIncomePublicTransportLaneLocaleId, "대중교통 전용차선 침입" },
                { BudgetUIPatches.FineIncomeMidBlockCrossingLocaleId, "중앙선 침범" },
                { BudgetUIPatches.FineIncomeIntersectionMovementLocaleId, "교차로 통행규칙 위반" },
                { BudgetUIPatches.FineIncomeBudgetDescriptionLocaleId, "최근 1달 동안 교통법규 단속으로 징수된 벌금 수입입니다." },
            };
        }

        public void Unload()
        {
        }
    }
}
