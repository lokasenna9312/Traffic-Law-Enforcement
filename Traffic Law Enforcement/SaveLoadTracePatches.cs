using System;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using HarmonyLib;
using Game;
using Game.Assets;
using Game.SceneFlow;


namespace Traffic_Law_Enforcement
{
    internal static class SaveLoadTracePatches
    {
        private const string HarmonyId = "Traffic_Law_Enforcement.SaveLoadTrace";

        private static Harmony s_Harmony;

        private static readonly MethodInfo s_GameManagerLoadMethod =
            AccessTools.Method(
                typeof(GameManager),
                nameof(GameManager.Load),
                new[]
                {
                    typeof(GameMode),
                    typeof(Purpose),
                    typeof(IAssetData),
                });

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            try
            {
                if (s_GameManagerLoadMethod == null)
                {
                    Mod.log.Warn("[SAVELOAD] SaveLoadTracePatches: GameManager.Load overload not found.");
                    return;
                }

                s_Harmony = new Harmony(HarmonyId);

                HarmonyMethod prefix =
                    new HarmonyMethod(
                        typeof(SaveLoadTracePatches),
                        nameof(GameManagerLoadPrefix));

                s_Harmony.Patch(
                    s_GameManagerLoadMethod,
                    prefix: prefix);

                Mod.log.Info("[SAVELOAD] SaveLoadTracePatches applied.");
            }
            catch (Exception ex)
            {
                Mod.log.Error($"[SAVELOAD] SaveLoadTracePatches.Apply failed: {ex}");
            }
        }

        public static void Remove()
        {
            if (s_Harmony == null)
            {
                return;
            }

            try
            {
                if (s_GameManagerLoadMethod != null)
                {
                    s_Harmony.Unpatch(s_GameManagerLoadMethod, HarmonyPatchType.All, HarmonyId);
                }
                Mod.log.Info("[SAVELOAD] SaveLoadTracePatches removed.");
            }
            catch (Exception ex)
            {
                Mod.log.Error($"[SAVELOAD] SaveLoadTracePatches.Remove failed: {ex}");
            }
            finally
            {
                s_Harmony = null;
            }
        }

        private static void GameManagerLoadPrefix(
            GameMode mode,
            Purpose purpose,
            IAssetData asset)
        {
            if (purpose != Purpose.LoadGame)
            {
                return;
            }

            SaveGameMetadata saveGameMetadata = asset as SaveGameMetadata;
            if (saveGameMetadata == null)
            {
                Mod.log.Info(
                    $"[SAVELOAD] GameManager.Load prefix saw LoadGame without SaveGameMetadata: " +
                    $"assetType={(asset == null ? "<null>" : asset.GetType().FullName)}");
                return;
            }

            SaveLoadTraceService.CaptureFromSaveMetadata(
                saveGameMetadata,
                "GameManager.Load");
        }
    }
}