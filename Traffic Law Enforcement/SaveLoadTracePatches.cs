using System;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using Game;
using Game.Assets;
using Game.SceneFlow;
using Game.UI;
using HarmonyLib;

namespace Traffic_Law_Enforcement
{
    internal static class SaveLoadTracePatches
    {
        private const string HarmonyId =
            "Traffic_Law_Enforcement.SaveLoadTrace";

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

        private static readonly MethodInfo s_GameManagerSaveAsyncMethod =
            AccessTools.Method(
                typeof(GameManager),
                nameof(GameManager.Save),
                new[]
                {
                    typeof(string),
                    typeof(SaveInfo),
                    typeof(ILocalAssetDatabase),
                    typeof(ScreenCaptureHelper.AsyncRequest),
                });

        public static void Apply()
        {
            if (s_Harmony != null)
            {
                return;
            }

            try
            {
                s_Harmony = new Harmony(HarmonyId);

                HarmonyMethod loadPrefix =
                    new HarmonyMethod(
                        typeof(SaveLoadTracePatches),
                        nameof(GameManagerLoadPrefix));
                HarmonyMethod savePrefix =
                    new HarmonyMethod(
                        typeof(SaveLoadTracePatches),
                        nameof(GameManagerSavePrefix));

                PatchIfPresent(s_GameManagerLoadMethod, loadPrefix);
                PatchIfPresent(s_GameManagerSaveAsyncMethod, savePrefix);
            }
            catch (Exception ex)
            {
                Mod.log.Error(
                    $"[SAVELOAD] SaveLoadTracePatches.Apply failed: {ex}");
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
                s_Harmony.UnpatchAll(HarmonyId);
            }
            catch (Exception ex)
            {
                Mod.log.Error(
                    $"[SAVELOAD] SaveLoadTracePatches.Remove failed: {ex}");
            }
            finally
            {
                s_Harmony = null;
            }
        }

        private static void PatchIfPresent(
            MethodInfo method,
            HarmonyMethod prefix)
        {
            if (method == null)
            {
                return;
            }

            s_Harmony.Patch(method, prefix: prefix);
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
                    "[SAVELOAD] " +
                    $"action=LoadRequested, source=GameManager.Load, assetType={(asset == null ? "<null>" : asset.GetType().FullName)}, name=unknown, city=unknown, path=unknown, id=unknown");
                return;
            }

            SaveLoadTraceService.CaptureFromSaveMetadata(
                saveGameMetadata,
                "GameManager.Load");

            Mod.log.Info(
                "[SAVELOAD] " +
                $"action=LoadRequested, source=GameManager.Load, name={SaveLoadTraceService.LastRequestedSaveName}, city={SaveLoadTraceService.LastRequestedCityName}, path={SaveLoadTraceService.LastRequestedSavePath}, id={SaveLoadTraceService.LastRequestedSaveId}");
        }

        private static void GameManagerSavePrefix(
            string saveName,
            SaveInfo meta)
        {
            SaveLoadTraceService.CaptureFromSaveInfo(
                meta,
                "GameManager.Save",
                saveName);
            Mod.log.Info(
                "[SAVELOAD] " +
                $"action=SaveRequested, source=GameManager.Save, name={SaveLoadTraceService.LastRequestedSaveName}, city={SaveLoadTraceService.LastRequestedCityName}, path={SaveLoadTraceService.LastRequestedSavePath}, id={SaveLoadTraceService.LastRequestedSaveId}");
        }
    }
}
