// Project      : WpfHexEditorControl
// File         : Models/ArchiveStats.cs
// Description  : Aggregate statistics computed over all entries in an archive.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

namespace WpfHexEditor.Plugins.ArchiveExplorer.Models;

/// <summary>Aggregate statistics for the currently loaded archive.</summary>
public readonly struct ArchiveStats
{
    public int TotalFiles     { get; init; }
    public int TotalFolders   { get; init; }
    public long TotalSize     { get; init; }
    public long CompressedSize { get; init; }

    /// <summary>Compression ratio as a percentage (0–100). Returns 0 when TotalSize is 0.</summary>
    public double CompressionRatioPct =>
        TotalSize > 0 ? (1.0 - (double)CompressedSize / TotalSize) * 100.0 : 0.0;
}
