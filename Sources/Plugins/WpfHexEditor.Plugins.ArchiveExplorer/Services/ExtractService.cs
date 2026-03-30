// Project      : WpfHexEditorControl
// File         : Services/ExtractService.cs
// Description  : Handles async extraction of archive entries to a user-chosen folder.
//                Uses Microsoft.Win32.OpenFolderDialog (net8.0) and reports
//                progress through IOutputService.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

using System.IO;
using Microsoft.Win32;
using WpfHexEditor.Plugins.ArchiveExplorer.Models;
using WpfHexEditor.SDK.Contracts.Services;

namespace WpfHexEditor.Plugins.ArchiveExplorer.Services;

/// <summary>
/// Orchestrates archive extraction with folder selection and progress reporting.
/// </summary>
public sealed class ExtractService
{
    private readonly IOutputService _output;

    public ExtractService(IOutputService output)
    {
        _output = output;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Prompts the user to choose a destination folder and extracts the given entries
    /// (or all entries if <paramref name="entries"/> is null).
    /// </summary>
    /// <returns>True when extraction completed successfully; false if cancelled.</returns>
    public async Task<bool> ExtractAsync(
        IArchiveReader            reader,
        IReadOnlyList<ArchiveEntry>? entries,
        CancellationToken         ct = default)
    {
        var dlg = new OpenFolderDialog
        {
            Title = entries is null
                ? $"Extract all — {Path.GetFileName(reader.ArchivePath)}"
                : $"Extract {entries.Count} item(s) — {Path.GetFileName(reader.ArchivePath)}"
        };

        if (dlg.ShowDialog() != true) return false;

        var destFolder = dlg.FolderName;
        _output.Info($"[Archive Explorer] Extracting to: {destFolder}");

        try
        {
            if (entries is null)
            {
                var progress = new Progress<double>(p =>
                    _output.Info($"[Archive Explorer] {p:P0}"));
                await reader.ExtractAllAsync(destFolder, progress, ct).ConfigureAwait(false);
            }
            else
            {
                int done  = 0;
                int total = entries.Count(e => !e.IsDirectory);
                foreach (var entry in entries.Where(e => !e.IsDirectory))
                {
                    ct.ThrowIfCancellationRequested();
                    var destPath = Path.Combine(destFolder,
                        entry.FullPath.Replace('/', Path.DirectorySeparatorChar));
                    await reader.ExtractEntryAsync(entry, destPath, ct).ConfigureAwait(false);
                    _output.Info($"[Archive Explorer] {++done}/{total} — {entry.FullPath}");
                }
            }

            _output.Info($"[Archive Explorer] Done → {destFolder}");
            return true;
        }
        catch (OperationCanceledException)
        {
            _output.Info("[Archive Explorer] Extraction cancelled.");
            return false;
        }
        catch (Exception ex)
        {
            _output.Info($"[Archive Explorer] Extraction error: {ex.Message}");
            return false;
        }
    }
}
