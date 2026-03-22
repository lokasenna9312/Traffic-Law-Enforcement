using System;
using System.Collections;
using System.Reflection;
using Game.Pathfind;
using HarmonyLib;
using Unity.Entities;
using Entity = Unity.Entities.Entity;

namespace Traffic_Law_Enforcement
{
    [HarmonyPatch(typeof(CenterlineAccessObsoleteSystem), "ResetDuplicateSuppressionIfPathChanged")]
    internal static class CenterlineAccessRepeatStatePatches
    {
        private static readonly FieldInfo RepeatInvalidationStatesField = AccessTools.Field(
            typeof(CenterlineAccessObsoleteSystem),
            "m_RepeatInvalidationStates");

        private static void Postfix(CenterlineAccessObsoleteSystem __instance, Entity vehicle, PathOwner pathOwner)
        {
            if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Updated)) == 0)
            {
                return;
            }

            if (RepeatInvalidationStatesField?.GetValue(__instance) is IDictionary dictionary && dictionary.Contains(vehicle))
            {
                dictionary.Remove(vehicle);

                if (EnforcementLoggingPolicy.ShouldLogEnforcementEvents())
                {
                    Mod.log.Info($"Reset CENTERLINE repeat invalidation state after path refresh: vehicle={vehicle}, pathState={pathOwner.m_State}");
                }
            }
        }
    }
}
