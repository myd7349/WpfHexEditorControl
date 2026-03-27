//////////////////////////////////////////////
// Project: WpfHexEditor.HexEditor
// File: PartialClasses/UI/HexEditor.BreadcrumbBar.cs
// Description:
//     Wires the interactive HexBreadcrumbBar into the HexEditor layout.
//     Walks the ParsedFieldViewModel tree to build hierarchical breadcrumb path.
//     Handles navigation via SetPosition when user clicks segments.
//     Configurable via public properties.
//////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfHexEditor.Core.ViewModels;
using WpfHexEditor.HexEditor.Controls;

namespace WpfHexEditor.HexEditor;

public partial class HexEditor
{
    private HexBreadcrumbBar? _breadcrumbBar;

    /// <summary>Shows or hides the breadcrumb bar above the hex viewport.</summary>
    public bool ShowBreadcrumbBar
    {
        get => _breadcrumbBar?.Visibility == Visibility.Visible;
        set
        {
            EnsureBreadcrumbBar();
            _breadcrumbBar!.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>Offset display format: Hex, Decimal, or Both.</summary>
    public BreadcrumbOffsetFormat BreadcrumbOffsetFormat
    {
        get => _breadcrumbBar?.OffsetFormat ?? Controls.BreadcrumbOffsetFormat.Both;
        set { EnsureBreadcrumbBar(); _breadcrumbBar!.OffsetFormat = value; }
    }

    /// <summary>Show format info (name + confidence) in breadcrumb.</summary>
    public bool BreadcrumbShowFormatInfo
    {
        get => _breadcrumbBar?.ShowFormatInfo ?? true;
        set { EnsureBreadcrumbBar(); _breadcrumbBar!.ShowFormatInfo = value; }
    }

    /// <summary>Show field path in breadcrumb.</summary>
    public bool BreadcrumbShowFieldPath
    {
        get => _breadcrumbBar?.ShowFieldPath ?? true;
        set { EnsureBreadcrumbBar(); _breadcrumbBar!.ShowFieldPath = value; }
    }

    /// <summary>Show selection length in breadcrumb.</summary>
    public bool BreadcrumbShowSelectionLength
    {
        get => _breadcrumbBar?.ShowSelectionLength ?? true;
        set { EnsureBreadcrumbBar(); _breadcrumbBar!.ShowSelectionLength = value; }
    }

    /// <summary>Font size for breadcrumb text.</summary>
    public double BreadcrumbFontSize
    {
        get => _breadcrumbBar?.FontSize ?? 11.5;
        set { EnsureBreadcrumbBar(); _breadcrumbBar!.FontSize = value; }
    }

    private void EnsureBreadcrumbBar()
    {
        if (_breadcrumbBar is not null) return;

        _breadcrumbBar = new HexBreadcrumbBar();
        _breadcrumbBar.NavigateRequested += (_, targetOffset) => SetPosition(targetOffset);

        // Insert at the top of the root grid (row 0, before existing content)
        if (Content is Grid rootGrid)
        {
            rootGrid.RowDefinitions.Insert(0, new RowDefinition { Height = GridLength.Auto });

            // Shift all existing children down by 1 row
            foreach (UIElement child in rootGrid.Children)
            {
                int row = Grid.GetRow(child);
                Grid.SetRow(child, row + 1);
            }

            Grid.SetRow(_breadcrumbBar, 0);
            Grid.SetColumnSpan(_breadcrumbBar, rootGrid.ColumnDefinitions.Count > 0
                ? rootGrid.ColumnDefinitions.Count : 1);
            rootGrid.Children.Add(_breadcrumbBar);
        }
    }

    /// <summary>Updates the breadcrumb bar with current state. Call from SelectionChanged handler.</summary>
    internal void UpdateBreadcrumb()
    {
        if (_breadcrumbBar is null || _breadcrumbBar.Visibility != Visibility.Visible) return;

        var offset = SelectionStart >= 0 ? SelectionStart : 0;
        var selLen = (SelectionStop > SelectionStart) ? SelectionStop - SelectionStart + 1 : 0;

        var formatName = _detectedFormat?.FormatName;
        var confidence = (_detectionCandidates?.Count > 0)
            ? (int)(_detectionCandidates[0].ConfidenceScore * 100)
            : 0;

        var segments = BuildBreadcrumbPath(offset, formatName, confidence);
        _breadcrumbBar.SetState(offset, selLen, formatName, confidence, segments);

        // Update bookmarks from parsed fields panel
        var bookmarks = ParsedFieldsPanel?.FormatInfo?.Bookmarks;
        _breadcrumbBar.SetBookmarks(bookmarks);
    }

    // ── Tree walking ──────────────────────────────────────────────────────────

    /// <summary>
    /// Walks the ParsedFieldViewModel tree to build a hierarchical breadcrumb path
    /// from the root format node down to the leaf field containing the given offset.
    /// </summary>
    private List<BreadcrumbSegment> BuildBreadcrumbPath(long offset, string? formatName, int confidence)
    {
        var segments = new List<BreadcrumbSegment>();

        // 1. Root format segment
        if (!string.IsNullOrEmpty(formatName))
        {
            segments.Add(new BreadcrumbSegment
            {
                Name = formatName!,
                Offset = 0,
                Length = (int)System.Math.Min(Length, int.MaxValue),
                IsFormat = true,
                Confidence = confidence,
            });
        }

        // 2. Walk ParsedFields tree to find path to current offset
        var panel = ParsedFieldsPanel;
        if (panel?.ParsedFields != null && BreadcrumbShowFieldPath)
            FindPathInTree(panel.ParsedFields, offset, segments);

        return segments;
    }

    /// <summary>
    /// Recursively finds the node containing the offset and adds each ancestor to the path.
    /// Also collects sibling nodes at each level for the chevron dropdown.
    /// </summary>
    private static void FindPathInTree(
        IEnumerable<ParsedFieldViewModel> nodes,
        long offset,
        List<BreadcrumbSegment> path)
    {
        var nodeList = nodes as IList<ParsedFieldViewModel> ?? nodes.ToList();

        foreach (var node in nodeList)
        {
            if (node.Length <= 0) continue;
            if (offset < node.Offset || offset >= node.Offset + node.Length) continue;

            // Build siblings list (other nodes at this level, excluding current)
            var siblings = nodeList
                .Where(n => n != node && n.Length > 0)
                .Select(n => new BreadcrumbSegment
                {
                    Name = n.IsGroup ? n.GroupLabel : n.Name,
                    Offset = n.Offset,
                    Length = n.Length,
                    IsGroup = n.IsGroup,
                    Color = n.Color,
                })
                .ToList();

            path.Add(new BreadcrumbSegment
            {
                Name = node.IsGroup ? node.GroupLabel : node.Name,
                Offset = node.Offset,
                Length = node.Length,
                IsGroup = node.IsGroup,
                Color = node.Color,
                Siblings = siblings,
            });

            // Recurse into children
            if (node.ChildItems?.Count > 0)
                FindPathInTree(node.ChildItems, offset, path);

            return; // only match first containing node
        }
    }
}
