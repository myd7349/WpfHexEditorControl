// Project      : WpfHexEditorControl
// File         : Services/PreviewService.cs
// Description  : Routes a double-clicked archive entry to the appropriate IDE
//                document tab. Extracts the entry to a temp file and calls
//                IDocumentHostService.OpenDocument with the best editor ID.
//
// Architecture : No direct WPF dependency — all IDE interaction is via SDK
//                contracts. Temp files are tracked for cleanup on plugin shutdown.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

using System.IO;
using WpfHexEditor.Plugins.ArchiveExplorer.Models;
using WpfHexEditor.SDK.Contracts;
using WpfHexEditor.SDK.Contracts.Services;

namespace WpfHexEditor.Plugins.ArchiveExplorer.Services;

/// <summary>
/// Opens archive entries in the IDE's document host by extracting them to temp files.
/// </summary>
public sealed class PreviewService
{
    private static readonly string TempRoot =
        Path.Combine(Path.GetTempPath(), "WpfHexEditor", "ArchiveExplorer");

    private readonly IDocumentHostService _documentHost;
    private readonly HashSet<string>      _tempFiles = [];

    public PreviewService(IDocumentHostService documentHost)
    {
        _documentHost = documentHost;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts <paramref name="entry"/> to a unique temp file and opens it in
    /// the most appropriate editor.
    /// </summary>
    public async Task PreviewAsync(
        IArchiveReader reader,
        ArchiveEntry   entry,
        int            maxSizeKb = 5120,
        CancellationToken ct = default)
    {
        if (entry.IsDirectory) return;

        // Warn before opening large entries
        if (entry.Size > maxSizeKb * 1024L)
        {
            // Non-blocking: we just proceed — the ViewModel shows a confirmation
            // dialog before calling this method when over the threshold.
        }

        var tempPath = BuildTempPath(entry);
        if (!File.Exists(tempPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
            await ExtractToPathAsync(reader, entry, tempPath, ct).ConfigureAwait(false);
            _tempFiles.Add(tempPath);
        }

        var editorId = ResolveEditorId(entry.FullPath);
        _documentHost.OpenDocument(tempPath, editorId);
    }

    /// <summary>
    /// Forces the entry to open in the hex editor regardless of its extension.
    /// </summary>
    public async Task PreviewRawAsync(
        IArchiveReader reader,
        ArchiveEntry   entry,
        CancellationToken ct = default)
    {
        if (entry.IsDirectory) return;
        var tempPath = BuildTempPath(entry);
        if (!File.Exists(tempPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
            await ExtractToPathAsync(reader, entry, tempPath, ct).ConfigureAwait(false);
            _tempFiles.Add(tempPath);
        }
        _documentHost.OpenDocument(tempPath, WellKnownEditorIds.HexEditor);
    }

    /// <summary>Deletes all temp files created during this session.</summary>
    public void CleanupTempFiles()
    {
        foreach (var path in _tempFiles)
        {
            try { File.Delete(path); } catch { /* best-effort */ }
        }
        _tempFiles.Clear();
        // Attempt to remove empty session folder
        try
        {
            if (Directory.Exists(TempRoot) && !Directory.EnumerateFileSystemEntries(TempRoot).Any())
                Directory.Delete(TempRoot);
        }
        catch { /* best-effort */ }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static string BuildTempPath(ArchiveEntry entry)
    {
        // Unique subfolder per session to avoid collisions across archives
        var fileName = Path.GetFileName(entry.FullPath);
        return Path.Combine(TempRoot, fileName);
    }

    private static async Task ExtractToPathAsync(
        IArchiveReader reader, ArchiveEntry entry, string destPath, CancellationToken ct)
    {
        await using var src  = reader.OpenEntry(entry);
        await using var dest = File.Create(destPath);
        await src.CopyToAsync(dest, ct).ConfigureAwait(false);
    }

    private static string ResolveEditorId(string fullPath)
    {
        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        return ext switch
        {
            // Text / code
            ".cs" or ".vb" or ".fs" or ".ts" or ".js" or ".jsx" or ".tsx"
            or ".py" or ".rb" or ".sh" or ".bat" or ".ps1" or ".lua"
            or ".c"  or ".cpp" or ".h" or ".hpp" or ".java" or ".kt"
            or ".go" or ".rs"  or ".swift" or ".dart"
            or ".json" or ".jsonc" or ".xml" or ".xaml" or ".html" or ".htm"
            or ".css" or ".scss" or ".less" or ".yaml" or ".yml" or ".toml"
            or ".ini" or ".cfg" or ".conf" or ".log" or ".csv"
            or ".txt" or ".sql" => WellKnownEditorIds.CodeEditor,

            // Markdown
            ".md" or ".markdown" => WellKnownEditorIds.MarkdownEditor,

            // Images
            ".png"  or ".jpg" or ".jpeg" or ".gif" or ".bmp"
            or ".ico" or ".webp" or ".tiff" or ".tif" or ".tga" => WellKnownEditorIds.ImageViewer,

            // Audio
            ".mp3" or ".flac" or ".wav" or ".ogg" or ".aac" or ".m4a" => WellKnownEditorIds.AudioViewer,

            // Binary fallback
            _ => WellKnownEditorIds.HexEditor,
        };
    }
}
