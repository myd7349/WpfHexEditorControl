// Project      : WpfHexEditorControl
// File         : ViewModels/ArchiveNodeViewModel.cs
// Description  : ViewModel wrapper around ArchiveNode that adds display-time
//                properties (MDL2 icon glyph, formatted size, compression ratio,
//                optional format badge) that must not live in the model.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using WpfHexEditor.Plugins.ArchiveExplorer.Models;
using WpfHexEditor.Plugins.ArchiveExplorer.Services;

namespace WpfHexEditor.Plugins.ArchiveExplorer.ViewModels;

/// <summary>
/// Presentation wrapper for <see cref="ArchiveNode"/>, exposed to the TreeView.
/// </summary>
public sealed class ArchiveNodeViewModel : INotifyPropertyChanged
{
    // ── Node reference ─────────────────────────────────────────────────────
    public ArchiveNode Node { get; }

    // ── Pass-through identity ──────────────────────────────────────────────
    public string  Name       => Node.Name;
    public string  FullPath   => Node.FullPath;
    public bool    IsFolder   => Node.IsFolder;
    public bool    IsArchive  => !Node.IsFolder && ArchiveReaderFactory.IsSupported(Node.FullPath);

    // ── Display properties ─────────────────────────────────────────────────
    public string IconGlyph => Node.IsFolder
        ? "\uE838"   // Segoe MDL2: FolderFilled
        : ResolveFileGlyph(Node.Name);

    public string SizeDisplay => Node.IsFolder
        ? string.Empty
        : FormatSize(Node.Size);

    public string CompressionInfo
    {
        get
        {
            if (Node.IsFolder || Node.Size <= 0) return string.Empty;
            var ratio = 1.0 - (double)Node.CompressedSize / Node.Size;
            return $"{ratio:P0}";
        }
    }

    // ── Format badge (populated asynchronously by ArchiveExplorerViewModel) ─
    private string? _formatBadge;
    public string? FormatBadge
    {
        get => _formatBadge;
        set { if (_formatBadge != value) { _formatBadge = value; OnPropertyChanged(); } }
    }

    // ── Tree state ─────────────────────────────────────────────────────────
    public ObservableCollection<ArchiveNodeViewModel> Children { get; } = [];

    public bool IsExpanded
    {
        get => Node.IsExpanded;
        set { Node.IsExpanded = value; OnPropertyChanged(); }
    }

    public bool IsSelected
    {
        get => Node.IsSelected;
        set { Node.IsSelected = value; OnPropertyChanged(); }
    }

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set { if (_isVisible != value) { _isVisible = value; OnPropertyChanged(); } }
    }

    // ── Constructor ────────────────────────────────────────────────────────
    public ArchiveNodeViewModel(ArchiveNode node)
    {
        Node = node;
        foreach (var child in node.Children)
            Children.Add(new ArchiveNodeViewModel(child));
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024L                => $"{bytes} B",
        < 1024L * 1024         => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024  => $"{bytes / 1024.0 / 1024:F1} MB",
        _                      => $"{bytes / 1024.0 / 1024 / 1024:F2} GB",
    };

    private static string ResolveFileGlyph(string name)
    {
        var ext = Path.GetExtension(name).ToLowerInvariant();
        return ext switch
        {
            ".cs" or ".vb" or ".fs" or ".ts" or ".js" or ".jsx" or ".tsx"
            or ".py" or ".rb" or ".java" or ".kt" or ".go" or ".rs"
            or ".c"  or ".cpp" or ".h"                    => "\uE943", // Code
            ".json" or ".xml" or ".xaml" or ".yaml"
            or ".yml" or ".toml" or ".ini" or ".cfg"      => "\uE8F4", // Document/Settings
            ".png"  or ".jpg" or ".jpeg" or ".gif"
            or ".bmp" or ".ico" or ".webp" or ".tiff"     => "\uEB9F", // Photo
            ".mp3"  or ".flac" or ".wav" or ".ogg"        => "\uEC4F", // Music
            ".mp4"  or ".avi"  or ".mkv" or ".mov"        => "\uE8B2", // Video
            ".exe"  or ".dll"  or ".so"                   => "\uE756", // App/PE
            ".zip"  or ".7z"  or ".rar" or ".tar"
            or ".gz" or ".bz2" or ".xz"                   => "\uE7C3", // ZipFolder
            ".md"   or ".txt" or ".log"                   => "\uE8A5", // Document
            _                                             => "\uE8A5", // Document (fallback)
        };
    }

    // ── INotifyPropertyChanged ─────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
