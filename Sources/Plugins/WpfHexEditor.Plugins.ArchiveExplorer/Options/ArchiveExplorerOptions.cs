// Project      : WpfHexEditorControl
// File         : Options/ArchiveExplorerOptions.cs
// Description  : Persistent options singleton for the Archive Explorer plugin.
//                Persisted to %AppData%\WpfHexaEditor\Plugins\ArchiveExplorer.json.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WpfHexEditor.Plugins.ArchiveExplorer.Options;

/// <summary>
/// Persistent options for the Archive Explorer plugin.
/// Accessed via the static <see cref="Instance"/> singleton.
/// </summary>
public sealed class ArchiveExplorerOptions
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    private static ArchiveExplorerOptions? _instance;
    public  static ArchiveExplorerOptions  Instance => _instance ??= Load();

    // ── Settings ──────────────────────────────────────────────────────────────

    /// <summary>When true, the panel is shown automatically when a supported archive is opened.</summary>
    public bool AutoShowOnArchiveOpen { get; set; } = true;

    /// <summary>When true, the compression ratio badge is displayed on each tree entry.</summary>
    public bool ShowCompressionRatio { get; set; } = true;

    /// <summary>When true, the whfmt format-detection badge is displayed on each tree entry.</summary>
    public bool ShowFormatBadge { get; set; } = true;

    /// <summary>Maximum entry size (KB) for format detection. Larger entries skip detection.</summary>
    public int MaxFormatDetectionSizeKb { get; set; } = 512;

    /// <summary>Maximum entry size (KB) for in-IDE preview. Larger files prompt for confirmation.</summary>
    public int PreviewMaxSizeKb { get; set; } = 5120;

    /// <summary>Default folder path used by Extract dialogs. Empty string = last used.</summary>
    public string DefaultExtractFolder { get; set; } = string.Empty;

    // ── Persistence ───────────────────────────────────────────────────────────

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WpfHexaEditor", "Plugins", "ArchiveExplorer.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    /// <summary>Loads options from disk; returns defaults on first run or parse error.</summary>
    public static ArchiveExplorerOptions Load()
    {
        if (!File.Exists(FilePath)) return new ArchiveExplorerOptions();
        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<ArchiveExplorerOptions>(json, JsonOpts)
                ?? new ArchiveExplorerOptions();
        }
        catch
        {
            return new ArchiveExplorerOptions();
        }
    }

    /// <summary>Persists current options to disk.</summary>
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOpts));
        }
        catch
        {
            // Silently ignore — disk errors must not crash the IDE.
        }
    }

    /// <summary>Forces a reload from disk and replaces the singleton.</summary>
    public static void Invalidate()
    {
        _instance = null;
    }
}
