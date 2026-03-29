// Project      : WpfHexEditorControl
// File         : Models/ArchiveNode.cs
// Description  : INPC model for a single node in the archive tree view.
//                Represents either a folder or a file entry. Display logic (icons,
//                formatted sizes) is intentionally kept in ArchiveNodeViewModel.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfHexEditor.Plugins.ArchiveExplorer.Models;

/// <summary>
/// Tree node for the Archive Explorer panel.
/// Each node may be a directory (no associated <see cref="Entry"/>) or a file entry.
/// </summary>
public sealed class ArchiveNode : INotifyPropertyChanged
{
    // ── Identity ───────────────────────────────────────────────────────────
    public string Name              { get; init; } = string.Empty;
    public string FullPath          { get; init; } = string.Empty;
    public bool   IsFolder          { get; init; }

    // ── Archive entry data (null for synthetic folder nodes) ───────────────
    public ArchiveEntry? Entry      { get; init; }
    public long          Size       => Entry?.Size           ?? 0;
    public long          CompressedSize => Entry?.CompressedSize ?? 0;
    public string?       Crc        => Entry?.Crc;
    public string?       CompressionMethod => Entry?.CompressionMethod;
    public DateTime?     LastModified => Entry?.LastModified;

    // ── Navigation ─────────────────────────────────────────────────────────
    /// <summary>Parent node; null for root.</summary>
    public ArchiveNode? Parent          { get; set; }

    /// <summary>Path of the archive that contains this node.
    /// Used for nested archive navigation (drill-in).</summary>
    public string SourceArchivePath { get; init; } = string.Empty;

    // ── Tree state ─────────────────────────────────────────────────────────
    public ObservableCollection<ArchiveNode> Children { get; init; } = [];

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set { if (_isExpanded != value) { _isExpanded = value; OnPropertyChanged(); } }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
    }

    // ── INotifyPropertyChanged ─────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
