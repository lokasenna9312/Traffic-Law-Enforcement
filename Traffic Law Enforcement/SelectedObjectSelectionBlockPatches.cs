using System;
using System.Reflection;
using Game.UI.InGame;
using HarmonyLib;

namespace Traffic_Law_Enforcement
{
    internal static class SelectedObjectSelectionBlockPatches
    {
        private const string HarmonyId =
            "Traffic_Law_Enforcement.SelectedObjectSelectionBlockPatches";

        private static readonly MethodInfo s_RefreshSelectionMethod =
            AccessTools.Method(typeof(SelectedInfoUISystem), "RefreshSelection");

        private static Harmony s_Harmony;

        public static void Apply()
        {
            if (s_Harmony != null || s_RefreshSelectionMethod == null)
            {
                return;
            }

            try
            {
                s_Harmony = new Harmony(HarmonyId);
                HarmonyMethod prefix = new HarmonyMethod(
                    typeof(SelectedObjectSelectionBlockPatches),
                    nameof(RefreshSelectionPrefix));

                s_Harmony.Patch(s_RefreshSelectionMethod, prefix: prefix);
            }
            catch (Exception ex)
            {
                s_Harmony = null;
                Mod.log.Error(ex, "Failed to apply SelectedObject selection block patch.");
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

        private static bool RefreshSelectionPrefix()
        {
            SelectedObjectPanelView panelView = SelectedObjectPanelView.Instance;
            if (panelView != null && panelView.ShouldBlockWorldSelection())
            {
                return false;
            }

            return true;
        }
    }
}