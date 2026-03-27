//////////////////////////////////////////////
// Project: WpfHexEditor.HexEditor
// File: Controls/HexBreadcrumbBar.cs
// Description:
//     Contextual breadcrumb bar for the HexEditor showing:
//     - Current offset (hex + decimal)
//     - Detected format name + confidence
//     - Active parsed field path (when available)
//     Updates on selection change / cursor move.
// Architecture:
//     Standalone FrameworkElement using DrawingContext for fast rendering.
//     Reads state from the parent HexEditor via SetState() calls.
//////////////////////////////////////////////

using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace WpfHexEditor.HexEditor.Controls;

/// <summary>
/// Breadcrumb bar showing offset, format, and field context for the active HexEditor.
/// </summary>
public sealed class HexBreadcrumbBar : FrameworkElement
{
    private long _offset;
    private string _formatName = "";
    private int _confidence;
    private string _fieldPath = "";

    private static readonly Typeface TypefaceNormal = new("Segoe UI");
    private static readonly Typeface TypefaceMono   = new("Consolas");
    private static readonly Brush SeparatorBrush = new SolidColorBrush(Color.FromArgb(80, 128, 128, 128));
    private static readonly Pen SeparatorPen = new(SeparatorBrush, 1);

    static HexBreadcrumbBar()
    {
        SeparatorBrush.Freeze();
        SeparatorPen.Freeze();
    }

    public HexBreadcrumbBar()
    {
        Height = 22;
        SnapsToDevicePixels = true;
    }

    /// <summary>Updates the displayed state. Call on selection/cursor change.</summary>
    public void SetState(long offset, string? formatName = null, int confidence = 0, string? fieldPath = null)
    {
        _offset     = offset;
        _formatName = formatName ?? "";
        _confidence = confidence;
        _fieldPath  = fieldPath ?? "";
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
        => new(availableSize.Width, 22);

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w < 1) return;

        // Background
        var bg = TryFindResource("DockTabBackgroundBrush") as Brush
              ?? new SolidColorBrush(Color.FromRgb(30, 30, 30));
        dc.DrawRectangle(bg, null, new Rect(0, 0, w, h));

        var fg = TryFindResource("DockMenuForegroundBrush") as Brush ?? Brushes.LightGray;
        var accentFg = TryFindResource("Panel_ToolbarButtonActiveBrush") as Brush ?? Brushes.CornflowerBlue;
        double x = 6;

        // Offset segment
        var offsetText = Format($"0x{_offset:X8}", TypefaceMono, 11.5, accentFg);
        dc.DrawText(offsetText, new Point(x, (h - offsetText.Height) / 2));
        x += offsetText.Width + 4;

        var decText = Format($"({_offset:N0})", TypefaceNormal, 10, fg);
        dc.DrawText(decText, new Point(x, (h - decText.Height) / 2));
        x += decText.Width + 8;

        // Separator
        dc.DrawLine(SeparatorPen, new Point(x, 3), new Point(x, h - 3));
        x += 8;

        // Format segment
        if (!string.IsNullOrEmpty(_formatName))
        {
            var fmtText = Format(_formatName, TypefaceNormal, 11, fg);
            dc.DrawText(fmtText, new Point(x, (h - fmtText.Height) / 2));
            x += fmtText.Width + 4;

            if (_confidence > 0)
            {
                var confText = Format($"{_confidence}%", TypefaceNormal, 9.5,
                    _confidence >= 80 ? Brushes.LimeGreen : Brushes.Orange);
                dc.DrawText(confText, new Point(x, (h - confText.Height) / 2));
                x += confText.Width + 8;
            }

            dc.DrawLine(SeparatorPen, new Point(x, 3), new Point(x, h - 3));
            x += 8;
        }

        // Field path segment
        if (!string.IsNullOrEmpty(_fieldPath))
        {
            var fieldText = Format(_fieldPath, TypefaceNormal, 11, accentFg);
            dc.DrawText(fieldText, new Point(x, (h - fieldText.Height) / 2));
        }
    }

    private static FormattedText Format(string text, Typeface tf, double size, Brush fg)
    {
        return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            tf, size, fg, VisualTreeHelper.GetDpi(new DrawingVisual()).PixelsPerDip);
    }
}
