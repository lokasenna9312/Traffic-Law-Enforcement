using System;
using System.Collections.Generic;
using System.Reflection;
using Colossal.UI.Binding;
using Game;
using Game.City;
using Game.SceneFlow;
using Game.Simulation;
using Game.UI;
using Game.UI.InGame;
using HarmonyLib;
using Unity.Entities;

namespace Traffic_Law_Enforcement
{
    internal static class BudgetUIPatches
    {
        internal const int FineIncomePublicTransportLaneSourceIndex = (int)IncomeSource.Count;
        internal const int FineIncomeMidBlockCrossingSourceIndex = (int)IncomeSource.Count + 1;
        internal const int FineIncomeIntersectionMovementSourceIndex = (int)IncomeSource.Count + 2;
        internal const string FineIncomeItemId = "TrafficLawEnforcement";
        internal const string FineIncomeBudgetItemLocaleId = "EconomyPanel.BUDGET_ITEM[TrafficLawEnforcement]";
        internal const string FineIncomeBudgetDescriptionLocaleId = "EconomyPanel.BUDGET_ITEM_DESCRIPTION[TrafficLawEnforcement]";
        internal const string FineIncomePublicTransportLaneSourceId = "TrafficLawEnforcementPublicTransportLane";
        internal const string FineIncomeMidBlockCrossingSourceId = "TrafficLawEnforcementMidBlockCrossing";
        internal const string FineIncomeIntersectionMovementSourceId = "TrafficLawEnforcementIntersectionMovement";
        internal const string FineIncomePublicTransportLaneLocaleId = "EconomyPanel.BUDGET_SUB_ITEM[TrafficLawEnforcementPublicTransportLane]";
        internal const string FineIncomeMidBlockCrossingLocaleId = "EconomyPanel.BUDGET_SUB_ITEM[TrafficLawEnforcementMidBlockCrossing]";
        internal const string FineIncomeIntersectionMovementLocaleId = "EconomyPanel.BUDGET_SUB_ITEM[TrafficLawEnforcementIntersectionMovement]";
        private const string FineIncomeIconPath = "Media/Game/Icons/TransportationOverview.svg";
        private const string HarmonyId = "Traffic_Law_Enforcement.BudgetUIPatches";
        private static readonly MethodInfo s_GetConfigMethod = AccessTools.Method(typeof(BudgetUISystem), "GetConfig");
        private static readonly FieldInfo s_BudgetsActivationsField = AccessTools.Field(typeof(BudgetUISystem), "m_BudgetsActivations");
        private static readonly FieldInfo s_IncomeItemsBindingField = AccessTools.Field(typeof(BudgetUISystem), "m_IncomeItemsBinding");
        private static readonly FieldInfo s_IncomeValuesBindingField = AccessTools.Field(typeof(BudgetUISystem), "m_IncomeValuesBinding");
        private static readonly FieldInfo s_TotalIncomeBindingField = AccessTools.Field(typeof(BudgetUISystem), "m_TotalIncomeBinding");

        private static Harmony s_Harmony;
        private static bool s_LoggedIncomeItemsPatchFailure;
        private static bool s_LoggedIncomeValuesPatchFailure;
        private static bool s_LoggedFineIncomeTemplateMissing;

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            try
            {
                s_Harmony = new Harmony(HarmonyId);
                s_Harmony.PatchAll(typeof(BudgetUIPatches).Assembly);
                RefreshExistingBudgetBindings("apply");
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                Mod.log.Error(ex, "Failed to apply budget UI patches.");
            }
        }

        public static void Remove()
        {
            if (s_Harmony == null)
            {
                return;
            }

            s_Harmony.UnpatchAll(HarmonyId);
            s_Harmony = null;
        }

        private static UIEconomyConfigurationPrefab GetConfig(BudgetUISystem system)
        {
            return s_GetConfigMethod?.Invoke(system, null) as UIEconomyConfigurationPrefab;
        }

        private static Dictionary<string, Func<bool>> GetBudgetActivations(BudgetUISystem system)
        {
            return s_BudgetsActivationsField?.GetValue(system) as Dictionary<string, Func<bool>>;
        }

        private static bool IsRoadIncomeItem(BudgetItem<IncomeSource> item)
        {
            if (item?.m_Sources == null)
            {
                return false;
            }

            bool hasFeeParking = false;
            bool hasFeePublicTransport = false;
            foreach (IncomeSource source in item.m_Sources)
            {
                if (source == IncomeSource.FeeParking)
                {
                    hasFeeParking = true;
                }
                else if (source == IncomeSource.FeePublicTransport)
                {
                    hasFeePublicTransport = true;
                }
            }

            return hasFeeParking && !hasFeePublicTransport;
        }

        private static BudgetItem<IncomeSource> GetFineIncomeTemplateItem(UIEconomyConfigurationPrefab config)
        {
            if (config?.m_IncomeItems == null)
            {
                return null;
            }

            foreach (BudgetItem<IncomeSource> budgetItem in config.m_IncomeItems)
            {
                if (IsRoadIncomeItem(budgetItem))
                {
                    return budgetItem;
                }
            }

            return config.m_IncomeItems.Length > 0 ? config.m_IncomeItems[config.m_IncomeItems.Length - 1] : null;
        }

        private static BudgetItem<IncomeSource> CreateFineIncomeItem(UIEconomyConfigurationPrefab config)
        {
            BudgetItem<IncomeSource> template = GetFineIncomeTemplateItem(config);
            if (template == null)
            {
                if (!s_LoggedFineIncomeTemplateMissing)
                {
                    s_LoggedFineIncomeTemplateMissing = true;
                }

                return new BudgetItem<IncomeSource>
                {
                    m_ID = FineIncomeItemId,
                    m_Color = new UnityEngine.Color(0.27f, 0.78f, 0.45f, 1f),
                    m_Icon = FineIncomeIconPath,
                    m_Sources = GetFineIncomeSources()
                };
            }

            return new BudgetItem<IncomeSource>
            {
                m_ID = FineIncomeItemId,
                m_Color = template.m_Color,
                m_Icon = FineIncomeIconPath,
                m_Sources = GetFineIncomeSources()
            };
        }

        private static IncomeSource[] GetFineIncomeSources()
        {
            return new[]
            {
                (IncomeSource)FineIncomePublicTransportLaneSourceIndex,
                (IncomeSource)FineIncomeMidBlockCrossingSourceIndex,
                (IncomeSource)FineIncomeIntersectionMovementSourceIndex
            };
        }

        private static void RefreshExistingBudgetBindings(string reason)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                return;
            }

            BudgetUISystem budgetSystem = world.GetExistingSystemManaged<BudgetUISystem>();
            if (budgetSystem == null)
            {
                return;
            }

            TryRefreshBudgetBindings(budgetSystem, reason);
        }

        private static void TryRefreshBudgetBindings(BudgetUISystem system, string reason)
        {
            try
            {
                UpdateBinding(s_IncomeItemsBindingField?.GetValue(system) as IUpdateBinding);
                UpdateBinding(s_IncomeValuesBindingField?.GetValue(system) as IUpdateBinding);
                UpdateBinding(s_TotalIncomeBindingField?.GetValue(system) as IUpdateBinding);
            }
            catch (Exception)
            {
            }
        }

        private static void UpdateBinding(IUpdateBinding binding)
        {
            binding?.Update();
        }

        [HarmonyPatch(typeof(BudgetUISystem), "OnCreate")]
        private static class OnCreatePatch
        {
            private static void Postfix(BudgetUISystem __instance)
            {
                TryRefreshBudgetBindings(__instance, "BudgetUISystem.OnCreate");
            }
        }

        [HarmonyPatch(typeof(BudgetUISystem), "BindIncomeItems")]
        private static class BindIncomeItemsPatch
        {
            private static bool Prefix(BudgetUISystem __instance, IJsonWriter writer)
            {
                try
                {
                    UIEconomyConfigurationPrefab config = GetConfig(__instance);
                    if (config == null || config.m_IncomeItems == null)
                    {
                        return true;
                    }

                    Dictionary<string, Func<bool>> activations = GetBudgetActivations(__instance);
                    BudgetItem<IncomeSource> fineIncomeItem = CreateFineIncomeItem(config);
                    writer.ArrayBegin(config.m_IncomeItems.Length + 1);
                    foreach (BudgetItem<IncomeSource> budgetItem in config.m_IncomeItems)
                    {
                        writer.TypeBegin("Game.UI.InGame.BudgetItem");
                        writer.PropertyName("id");
                        writer.Write(budgetItem.m_ID);
                        writer.PropertyName("color");
                        writer.Write(budgetItem.m_Color);
                        writer.PropertyName("icon");
                        writer.Write(budgetItem.m_Icon);
                        writer.PropertyName("active");

                        bool active = true;
                        if (activations != null && activations.TryGetValue(budgetItem.m_ID, out Func<bool> activation))
                        {
                            active = activation();
                        }

                        writer.Write(active);
                        writer.PropertyName("sources");
                        int sourceCount = budgetItem.m_Sources?.Length ?? 0;
                        writer.ArrayBegin(sourceCount);

                        if (budgetItem.m_Sources != null)
                        {
                            foreach (IncomeSource incomeSource in budgetItem.m_Sources)
                            {
                                writer.TypeBegin("Game.UI.InGame.BudgetSource");
                                writer.PropertyName("id");
                                writer.Write(Enum.GetName(typeof(IncomeSource), incomeSource));
                                var sourceName = Enum.GetName(typeof(IncomeSource), incomeSource);
                                writer.PropertyName("index");
                                writer.Write((int)incomeSource);
                                writer.TypeEnd();
                            }
                        }

                        writer.ArrayEnd();
                        writer.TypeEnd();
                    }

                    writer.TypeBegin("Game.UI.InGame.BudgetItem");
                    writer.PropertyName("id");
                    writer.Write(fineIncomeItem.m_ID);
                    writer.PropertyName("color");
                    writer.Write(fineIncomeItem.m_Color);
                    writer.PropertyName("icon");
                    writer.Write(fineIncomeItem.m_Icon);
                    writer.PropertyName("active");
                    writer.Write(true);
                    writer.PropertyName("sources");
                    writer.ArrayBegin(3);
                    WriteFineIncomeSource(writer, FineIncomePublicTransportLaneSourceId, FineIncomePublicTransportLaneSourceIndex);
                    WriteFineIncomeSource(writer, FineIncomeMidBlockCrossingSourceId, FineIncomeMidBlockCrossingSourceIndex);
                    WriteFineIncomeSource(writer, FineIncomeIntersectionMovementSourceId, FineIncomeIntersectionMovementSourceIndex);
                    writer.ArrayEnd();
                    writer.TypeEnd();

                    writer.ArrayEnd();

                    return false;
                }
                catch (Exception ex)
                {
                    if (!s_LoggedIncomeItemsPatchFailure)
                    {
                        s_LoggedIncomeItemsPatchFailure = true;
                        Mod.log.Error(ex, "Failed to patch budget income items. Falling back to vanilla income item binding.");
                    }

                    return true;
                }
            }
        }

        private static void WriteFineIncomeSource(IJsonWriter writer, string sourceId, int sourceIndex)
        {
            writer.TypeBegin("Game.UI.InGame.BudgetSource");
            writer.PropertyName("id");
            writer.Write(sourceId);
            writer.PropertyName("index");
            writer.Write(sourceIndex);
            writer.TypeEnd();
        }


        [HarmonyPatch(typeof(BudgetUISystem), "BindIncomeValues")]
        private static class BindIncomeValuesPatch
        {
            private static bool Prefix(BudgetUISystem __instance, IJsonWriter writer)
            {
                try
                {
                    CityServiceBudgetSystem cityServiceBudgetSystem = __instance.World.GetOrCreateSystemManaged<CityServiceBudgetSystem>();
                    writer.ArrayBegin((uint)(FineIncomeIntersectionMovementSourceIndex + 1));
                    for (int index = 0; index < FineIncomePublicTransportLaneSourceIndex; index += 1)
                    {
                        writer.Write(cityServiceBudgetSystem.GetIncome((IncomeSource)index));
                    }

                    writer.Write(EnforcementBudgetUIService.CurrentPublicTransportLaneFineIncome);
                    writer.Write(EnforcementBudgetUIService.CurrentMidBlockCrossingFineIncome);
                    writer.Write(EnforcementBudgetUIService.CurrentIntersectionMovementFineIncome);
                    writer.ArrayEnd();
                    return false;
                }
                catch (Exception ex)
                {
                    if (!s_LoggedIncomeValuesPatchFailure)
                    {
                        s_LoggedIncomeValuesPatchFailure = true;
                        Mod.log.Error(ex, "Failed to patch budget income values. Falling back to vanilla income value binding.");
                    }

                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(CityServiceBudgetSystem), nameof(CityServiceBudgetSystem.GetTotalIncome), new Type[0])]
        private static class GetTotalIncomePatch
        {
            private static void Postfix(ref int __result)
            {
                __result += EnforcementBudgetUIService.CurrentFineIncome;
            }
        }
    }
}
