// ==========================================================
// Project: WpfHexEditor.App
// File: Services/RoamingDataBackupService.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-05-20
// Description:
//     Backup and restore of the four managed roaming files.
//     Backup layout: %AppData%\WpfHexEditor\Backup\YYYYMMDD-HHmmss\
//     Plugins\ directory is never touched.
// ==========================================================

using System.IO;

namespace WpfHexEditor.App.Services;

/// <summary>
/// Manages timestamped backups of the four roaming data files.
/// Never touches the Plugins\ directory.
/// </summary>
internal static class RoamingDataBackupService
{
    /// <summary>Roaming files managed by this service (relative to <see cref="RoamingRoot"/>).</summary>
    private static readonly string[] ManagedRelativePaths =
    [
        "settings.json",
        "plugin-isolation-overrides.json",
        Path.Combine("App", "layout.json"),
        Path.Combine("App", "session.json"),
    ];

    public static string RoamingRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WpfHexEditor");

    public static string BackupRoot { get; } = Path.Combine(RoamingRoot, "Backup");

    /// <summary>
    /// Copies all existing managed files into a new timestamped backup folder.
    /// Returns the backup folder path, or null if no files existed to copy.
    /// </summary>
    public static string? CreateBackup()
    {
        var stamp      = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var backupDir  = Path.Combine(BackupRoot, stamp);
        var anyCopied  = false;

        foreach (var relative in ManagedRelativePaths)
        {
            var src = Path.Combine(RoamingRoot, relative);
            if (!File.Exists(src)) continue;

            var dst = Path.Combine(backupDir, relative);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                File.Copy(src, dst, overwrite: true);
                anyCopied = true;
            }
            catch (Exception ex)
            {
                OutputLogger.Error($"[RoamingBackup] Failed to backup '{relative}': {ex.Message}");
            }
        }

        if (anyCopied)
        {
            OutputLogger.Info($"[RoamingBackup] Backup created: {backupDir}");
            return backupDir;
        }

        return null;
    }

    /// <summary>
    /// Returns the most recent backup folder, or null if none exists.
    /// </summary>
    public static string? GetLatestBackup()
    {
        if (!Directory.Exists(BackupRoot)) return null;

        return Directory.GetDirectories(BackupRoot)
            .OrderByDescending(d => d)
            .FirstOrDefault();
    }

    /// <summary>
    /// Restores all files from the most recent backup into their original locations.
    /// Returns (restoredCount, errorMessages).
    /// </summary>
    public static (int Restored, List<string> Errors) RestoreLatestBackup()
    {
        var latest = GetLatestBackup();
        if (latest is null) return (0, ["No backup found."]);

        var restored = 0;
        var errors   = new List<string>();

        foreach (var relative in ManagedRelativePaths)
        {
            var src = Path.Combine(latest, relative);
            if (!File.Exists(src)) continue;

            var dst = Path.Combine(RoamingRoot, relative);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                File.Copy(src, dst, overwrite: true);
                restored++;
            }
            catch (Exception ex)
            {
                errors.Add($"{relative}: {ex.Message}");
                OutputLogger.Error($"[RoamingBackup] Failed to restore '{relative}': {ex.Message}");
            }
        }

        OutputLogger.Info($"[RoamingBackup] Restored {restored} file(s) from '{latest}'.");
        return (restored, errors);
    }

    /// <summary>
    /// Deletes all managed files from disk.
    /// Returns (deletedFileNames, errorMessages).
    /// </summary>
    public static (List<string> Deleted, List<string> Errors) DeleteManagedFiles()
    {
        var deleted = new List<string>();
        var errors  = new List<string>();

        foreach (var relative in ManagedRelativePaths)
        {
            var path = Path.Combine(RoamingRoot, relative);
            if (!File.Exists(path)) continue;
            try
            {
                File.Delete(path);
                deleted.Add(Path.GetFileName(path));
            }
            catch (Exception ex)
            {
                errors.Add($"{Path.GetFileName(path)}: {ex.Message}");
                OutputLogger.Error($"[RoamingBackup] Failed to delete '{relative}': {ex.Message}");
            }
        }

        return (deleted, errors);
    }
}
