//////////////////////////////////////////////
// Project: WpfHexEditor.HexEditor
// File: Controls/HexBreadcrumbBar.cs
// Description:
//     Interactive navigable breadcrumb bar for the HexEditor.
//     Click any segment → popup dropdown with all destinations at that level.
//     Segoe MDL2 arrow chevrons between segments. VS-style breadcrumb UX.
//     UpdateOffsetOnly for instant offset updates; SetSegments for path rebuild.
// Architecture:
//     Border-based WPF control. Theme tokens: BC_*.
//////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WpfHexEditor.Core.Interfaces;

namespace WpfHexEditor.HexEditor.Controls;

/// <summary>How the offset is displayed in the breadcrumb bar.</summary>
public enum BreadcrumbOffsetFormat
{
    Hex,
    Decimal,
    Both,
}

/// <summary>A single segment in the breadcrumb path.</summary>
public sealed class BreadcrumbSegment
{
    public string Name { get; init; } = "";
    public long Offset { get; init; }
    public int Length { get; init; }
    public bool IsGroup { get; init; }
    public string? Color { get; init; }
    public List<BreadcrumbSegment>? Siblings { get; init; }
    public bool IsFormat { get; init; }
    public int Confidence { get; init; }
}

/// <summary>Event args for breadcrumb navigation.</summary>
public sealed class BreadcrumbNavigateEventArgs : EventArgs
{
    public long Offset { get; init; }
    public int Length { get; init; }
}

/// <summary>
/// Interactive breadcrumb bar. Click any segment → popup with all destinations.
/// Segoe MDL2 chevron arrows between segments.
/// </summary>
public sealed class HexBreadcrumbBar : Border
{
    // ── Layout ────────────────────────────────────────────────────────────────
    private readonly StackPanel _rootPanel;
    private readonly TextBlock _offsetText;
    private readonly TextBlock _selLenText;
    private readonly Border _separator1;
    private readonly StackPanel _pathPanel;
    private readonly Border _separator2;
    private readonly StackPanel _bookmarkPanel;

    // ── State ─────────────────────────────────────────────────────────────────
    private long _offset;
    private long _selectionLength;
    private List<BreadcrumbSegment> _segments = new();
    private Popup? _activePopup;

    // ── Static ────────────────────────────────────────────────────────────────
    private static readonly Brush SepLineBrush;
    private static readonly FontFamily MdlFont = new("Segoe MDL2 Assets");

    static HexBreadcrumbBar()
    {
        SepLineBrush = new SolidColorBrush(Color.FromArgb(80, 128, 128, 128));
        SepLineBrush.Freeze();
    }

    // ── Configurable ──────────────────────────────────────────────────────────
    public BreadcrumbOffsetFormat OffsetFormat { get; set; } = BreadcrumbOffsetFormat.Both;
    public bool ShowFormatInfo { get; set; } = true;
    public bool ShowFieldPath { get; set; } = true;
    public bool ShowSelectionLength { get; set; } = true;
    public new double FontSize { get; set; } = 11.5;
    public double BarHeight { get => Height; set => Height = value; }

    // ── Events ────────────────────────────────────────────────────────────────
    public event EventHandler<BreadcrumbNavigateEventArgs>? NavigateRequested;

    // ── Constructor ───────────────────────────────────────────────────────────
    public HexBreadcrumbBar()
    {
        Height = 22;
        Padding = new Thickness(6, 0, 6, 0);
        SnapsToDevicePixels = true;
        SetResourceReference(BackgroundProperty, "BC_Background");
        BorderThickness = new Thickness(0, 0, 0, 1);
        BorderBrush = SepLineBrush;

        _offsetText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Consolas"),
            FontSize = FontSize,
        };
        _offsetText.SetResourceReference(TextBlock.ForegroundProperty, "Panel_ToolbarButtonActiveBrush");

        _selLenText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = FontSize - 1.5,
            Margin = new Thickness(6, 0, 0, 0),
            Visibility = Visibility.Collapsed,
        };
        _selLenText.SetResourceReference(TextBlock.ForegroundProperty, "BC_Foreground");

        _separator1 = CreateSepBorder();
        _pathPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        _separator2 = CreateSepBorder();
        _separator2.Visibility = Visibility.Collapsed;
        _bookmarkPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        _rootPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        _rootPanel.Children.Add(_offsetText);
        _rootPanel.Children.Add(_selLenText);
        _rootPanel.Children.Add(_separator1);
        _rootPanel.Children.Add(_pathPanel);
        _rootPanel.Children.Add(_separator2);
        _rootPanel.Children.Add(_bookmarkPanel);

        Child = _rootPanel;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Instant offset + selection update (no segment rebuild).</summary>
    public void UpdateOffsetOnly(long offset, long selectionLength)
    {
        _offset = offset;
        _selectionLength = selectionLength;

        _offsetText.FontSize = FontSize;
        _offsetText.Text = OffsetFormat switch
        {
            BreadcrumbOffsetFormat.Hex => $"0x{offset:X8}",
            BreadcrumbOffsetFormat.Decimal => $"{offset:N0}",
            _ => $"0x{offset:X8} ({offset:N0})",
        };

        if (ShowSelectionLength && selectionLength > 1)
        {
            _selLenText.Text = $"[{selectionLength:N0} bytes]";
            _selLenText.FontSize = FontSize - 1.5;
            _selLenText.Visibility = Visibility.Visible;
        }
        else
        {
            _selLenText.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>Rebuild path segments (debounced call from HexEditor).</summary>
    public void SetSegments(List<BreadcrumbSegment>? segments)
    {
        _segments = segments ?? new();
        RenderPathSegments();
    }

    /// <summary>Sets navigation bookmark chips.</summary>
    public void SetBookmarks(IEnumerable<FormatNavigationBookmark>? bookmarks)
    {
        _bookmarkPanel.Children.Clear();
        var list = bookmarks?.ToList();
        if (list == null || list.Count == 0) { _separator2.Visibility = Visibility.Collapsed; return; }

        _separator2.Visibility = Visibility.Visible;
        foreach (var bm in list)
        {
            var chip = new Border
            {
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1, 4, 1),
                Margin = new Thickness(2, 0, 2, 0),
                Cursor = Cursors.Hand,
                ToolTip = $"{bm.Name}\n0x{bm.Offset:X8}",
                Tag = bm.Offset,
                SnapsToDevicePixels = true,
            };
            chip.SetResourceReference(BackgroundProperty, "BC_HoverBackground");

            var ct = new TextBlock
            {
                FontSize = FontSize - 2,
                VerticalAlignment = VerticalAlignment.Center,
                Text = string.IsNullOrEmpty(bm.Icon) ? bm.Name : $"{bm.Icon} {bm.Name}",
            };
            ct.SetResourceReference(TextBlock.ForegroundProperty, "BC_Foreground");
            chip.Child = ct;

            chip.MouseEnter += (_, _) => chip.SetResourceReference(BackgroundProperty, "SE_HoverBrush");
            chip.MouseLeave += (_, _) => chip.SetResourceReference(BackgroundProperty, "BC_HoverBackground");
            chip.MouseLeftButtonDown += (s, ev) =>
            {
                if (s is Border b && b.Tag is long bmOff)
                {
                    NavigateRequested?.Invoke(this, new BreadcrumbNavigateEventArgs { Offset = bmOff });
                    ev.Handled = true;
                }
            };
            _bookmarkPanel.Children.Add(chip);
        }
    }

    // ── Path segment rendering ────────────────────────────────────────────────

    private void RenderPathSegments()
    {
        _pathPanel.Children.Clear();
        if (_segments.Count == 0) { _separator1.Visibility = Visibility.Collapsed; return; }

        _separator1.Visibility = Visibility.Visible;

        for (int i = 0; i < _segments.Count; i++)
        {
            var seg = _segments[i];
            bool isLast = i == _segments.Count - 1;

            // Skip format if ShowFormatInfo is off
            if (seg.IsFormat && !ShowFormatInfo) continue;

            // Arrow separator (Segoe MDL2 ChevronRight) before each segment except first
            if (_pathPanel.Children.Count > 0)
            {
                var arrow = new TextBlock
                {
                    Text = "\uE76C",
                    FontFamily = MdlFont,
                    FontSize = FontSize - 3,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 4, 0),
                };
                arrow.SetResourceReference(TextBlock.ForegroundProperty, "BC_SeparatorForeground");
                _pathPanel.Children.Add(arrow);
            }

            // Segment label — always clickable
            bool hasSiblings = seg.Siblings?.Count > 0;
            string displayText = (!isLast || hasSiblings) ? $"{seg.Name} \u25BE" : seg.Name;

            var lbl = new TextBlock
            {
                Text = displayText,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = FontSize,
                FontWeight = isLast ? FontWeights.SemiBold : FontWeights.Normal,
                Cursor = Cursors.Hand,
                Padding = new Thickness(3, 1, 3, 1),
                Tag = seg,
            };

            if (isLast)
                lbl.SetResourceReference(TextBlock.ForegroundProperty, "Panel_ToolbarButtonActiveBrush");
            else
                lbl.SetResourceReference(TextBlock.ForegroundProperty, "BC_Foreground");

            lbl.MouseEnter += (_, _) => lbl.SetResourceReference(BackgroundProperty, "BC_HoverBackground");
            lbl.MouseLeave += (_, _) => lbl.Background = Brushes.Transparent;
            lbl.MouseLeftButtonDown += OnSegmentClick;

            _pathPanel.Children.Add(lbl);
        }
    }

    // ── Click → Popup ─────────────────────────────────────────────────────────

    private void OnSegmentClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBlock tb && tb.Tag is BreadcrumbSegment seg)
        {
            if (seg.Siblings?.Count > 0)
            {
                // Defer popup to next frame — prevents StaysOpen=false from
                // immediately closing it due to the same click event propagating.
                var anchor = tb;
                var captured = seg;
                Dispatcher.BeginInvoke(new Action(() => OpenSegmentPopup(captured, anchor)),
                    System.Windows.Threading.DispatcherPriority.Input);
            }
            else
            {
                NavigateRequested?.Invoke(this, new BreadcrumbNavigateEventArgs
                    { Offset = seg.Offset, Length = seg.Length });
            }
            e.Handled = true;
        }
    }

    private void OpenSegmentPopup(BreadcrumbSegment seg, UIElement anchor)
    {
        CloseActivePopup();
        if (seg.Siblings == null || seg.Siblings.Count == 0) return;

        var popup = new Popup
        {
            PlacementTarget = anchor,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = true,
        };

        var listBox = new ListBox
        {
            MaxHeight = 350,
            MinWidth = 200,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
        };
        listBox.SetResourceReference(BackgroundProperty, "BC_Background");
        listBox.SetResourceReference(BorderBrushProperty, "BC_SeparatorForeground");

        // Current + siblings sorted by offset
        var allItems = new List<BreadcrumbSegment> { seg };
        allItems.AddRange(seg.Siblings);
        allItems.Sort((a, b) => a.Offset.CompareTo(b.Offset));

        foreach (var item in allItems)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };

            var nameBlock = new TextBlock
            {
                Text = item.Name,
                FontSize = FontSize,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = item.Offset == seg.Offset && item.Name == seg.Name
                    ? FontWeights.SemiBold : FontWeights.Normal,
                MinWidth = 120,
            };
            nameBlock.SetResourceReference(TextBlock.ForegroundProperty, "BC_Foreground");

            var offBlock = new TextBlock
            {
                Text = $"0x{item.Offset:X}",
                FontSize = FontSize - 2,
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.5,
                Margin = new Thickness(8, 0, 0, 0),
            };
            offBlock.SetResourceReference(TextBlock.ForegroundProperty, "BC_Foreground");

            row.Children.Add(nameBlock);
            row.Children.Add(offBlock);

            var lbi = new ListBoxItem
            {
                Content = row,
                Cursor = Cursors.Hand,
                Padding = new Thickness(6, 3, 6, 3),
            };

            if (item.Offset == seg.Offset && item.Name == seg.Name)
                lbi.SetResourceReference(BackgroundProperty, "BC_HoverBackground");

            var captured = item;
            lbi.MouseLeftButtonUp += (_, _) =>
            {
                NavigateRequested?.Invoke(this, new BreadcrumbNavigateEventArgs
                {
                    Offset = captured.Offset,
                    Length = captured.Length,
                });
                popup.IsOpen = false;
            };

            listBox.Items.Add(lbi);
        }

        popup.Child = new Border
        {
            Child = listBox,
            CornerRadius = new CornerRadius(4),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 8, Opacity = 0.3, ShadowDepth = 2,
            },
        };

        popup.Closed += (_, _) => { if (_activePopup == popup) _activePopup = null; };
        _activePopup = popup;
        popup.IsOpen = true;
    }

    private void CloseActivePopup()
    {
        if (_activePopup is { IsOpen: true }) _activePopup.IsOpen = false;
        _activePopup = null;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Border CreateSepBorder() => new()
    {
        Width = 1,
        Margin = new Thickness(8, 3, 8, 3),
        VerticalAlignment = VerticalAlignment.Stretch,
        Background = SepLineBrush,
    };
}
