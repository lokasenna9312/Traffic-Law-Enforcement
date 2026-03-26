using System;
using System.IO;
using System.Threading;

namespace Traffic_Law_Enforcement
{
    internal static class SettingsFileProtectionService
    {
        private const string BackupFileName = "Settings.coc.tle.guard.bak";
        private static readonly object s_FileGate = new object();
        private static bool s_LoggedMissingHealthyBackup;

        public static void BackupHealthySettingsFile(string reason)
        {
            lock (s_FileGate)
            {
                try
                {
                    if (!File.Exists(SettingsFilePath))
                    {
                        return;
                    }

                    if (!LooksHealthy(SettingsFilePath))
                    {
                        Mod.log.Warn(
                            $"[KEYBIND_BACKUP] Skipped backup because current Settings.coc does not look healthy. reason={reason}, path={SettingsFilePath}");
                        return;
                    }

                    Directory.CreateDirectory(SettingsDirectoryPath);
                    File.Copy(SettingsFilePath, BackupFilePath, overwrite: true);
                    s_LoggedMissingHealthyBackup = false;
                    Mod.log.Info(
                        $"[KEYBIND_BACKUP] Backed up healthy Settings.coc. reason={reason}, path={BackupFilePath}");
                }
                catch (Exception ex)
                {
                    Mod.log.Error(ex, $"[KEYBIND_BACKUP] Failed to back up Settings.coc. reason={reason}");
                }
            }
        }

        public static void RestoreBackupIfCurrentLooksCorrupted(string reason)
        {
            lock (s_FileGate)
            {
                try
                {
                    if (!File.Exists(BackupFilePath))
                    {
                        if (!s_LoggedMissingHealthyBackup)
                        {
                            s_LoggedMissingHealthyBackup = true;
                            Mod.log.Warn(
                                $"[KEYBIND_BACKUP] No healthy backup is available. restoreReason={reason}, expectedBackup={BackupFilePath}");
                        }

                        return;
                    }

                    if (!LooksHealthy(BackupFilePath))
                    {
                        Mod.log.Warn(
                            $"[KEYBIND_BACKUP] Existing backup also looks unhealthy. restoreReason={reason}, backupPath={BackupFilePath}");
                        return;
                    }

                    if (File.Exists(SettingsFilePath) && LooksHealthy(SettingsFilePath))
                    {
                        return;
                    }

                    Directory.CreateDirectory(SettingsDirectoryPath);

                    if (File.Exists(SettingsFilePath))
                    {
                        string corruptSnapshotPath = Path.Combine(
                            SettingsDirectoryPath,
                            $"Settings.coc.tle.corrupt.{DateTime.Now:yyyyMMdd_HHmmss}.bak");
                        SafeCopyWithRetries(SettingsFilePath, corruptSnapshotPath, overwrite: true);
                    }

                    SafeCopyWithRetries(BackupFilePath, SettingsFilePath, overwrite: true);
                    Mod.log.Warn(
                        $"[KEYBIND_BACKUP] Restored Settings.coc from backup. reason={reason}, backupPath={BackupFilePath}");
                }
                catch (Exception ex)
                {
                    Mod.log.Error(ex, $"[KEYBIND_BACKUP] Failed to restore Settings.coc from backup. reason={reason}");
                }
            }
        }

        private static string SettingsDirectoryPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "AppData",
                "LocalLow",
                "Colossal Order",
                "Cities Skylines II");

        private static string SettingsFilePath => Path.Combine(SettingsDirectoryPath, "Settings.coc");

        private static string BackupFilePath => Path.Combine(SettingsDirectoryPath, BackupFileName);

        private static bool LooksHealthy(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            FileInfo info = new FileInfo(path);
            if (info.Length < 512)
            {
                return false;
            }

            string text = File.ReadAllText(path);
            return
                text.IndexOf("Input Settings", StringComparison.Ordinal) >= 0 &&
                text.IndexOf("Graphics Settings", StringComparison.Ordinal) >= 0 &&
                text.IndexOf("General Settings", StringComparison.Ordinal) >= 0;
        }

        private static void SafeCopyWithRetries(string sourcePath, string destinationPath, bool overwrite)
        {
            const int maxAttempts = 5;
            for (int attempt = 1; attempt <= maxAttempts; attempt += 1)
            {
                try
                {
                    File.Copy(sourcePath, destinationPath, overwrite);
                    return;
                }
                catch when (attempt < maxAttempts)
                {
                    Thread.Sleep(50);
                }
            }

            File.Copy(sourcePath, destinationPath, overwrite);
        }
    }
}
