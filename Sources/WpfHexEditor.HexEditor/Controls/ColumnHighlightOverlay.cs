//////////////////////////////////////////////
// Project: WpfHexEditor.HexEditor
// File: Controls/ColumnHighlightOverlay.cs
// Description:
//     Semi-transparent overlay that highlights the active byte's column
//     (vertical stripe) in the hex viewport. Helps users track which
//     byte position they're editing within a line.
// Architecture:
//     Lightweight FrameworkElement rendered via DrawingContext.
//     Positioned over the HexViewport. Updated on cursor/selection change.
//////////////////////////////////////////////

using System;
using System.Windows;
using System.Windows.Media;

namespace WpfHexEditor.HexEditor.Controls;

/// <summary>
/// Draws a semi-transparent vertical stripe over the active byte column.
/// </summary>
public sealed class ColumnHighlightOverlay : FrameworkElement
{
    private int _activeColumn = -1;
    private double _charWidth;
    private double _hexAreaLeft;
    private int _bytesPerLine = 16;

    private static readonly Brush HighlightBrush = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));

    static ColumnHighlightOverlay()
    {
        HighlightBrush.Freeze();
    }

    public ColumnHighlightOverlay()
    {
        IsHitTestVisible = false; // clicks pass through
    }

    /// <summary>
    /// Updates the highlight position. Call on cursor/selection change.
    /// </summary>
    /// <param name="activeColumn">0-based column index within the line (0 to bytesPerLine-1). -1 to hide.</param>
    /// <param name="charWidth">Width of one hex byte cell (e.g. 2 chars + spacing).</param>
    /// <param name="hexAreaLeft">X offset where the hex area starts.</param>
    /// <param name="bytesPerLine">Number of bytes per line.</param>
    public void SetColumn(int activeColumn, double charWidth, double hexAreaLeft, int bytesPerLine)
    {
        if (_activeColumn == activeColumn && Math.Abs(_charWidth - charWidth) < 0.1)
            return;

        _activeColumn = activeColumn;
        _charWidth    = charWidth;
        _hexAreaLeft  = hexAreaLeft;
        _bytesPerLine = bytesPerLine;
        InvalidateVisual();
    }

    /// <summary>Hides the column highlight.</summary>
    public void Hide()
    {
        if (_activeColumn < 0) return;
        _activeColumn = -1;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (_activeColumn < 0 || _activeColumn >= _bytesPerLine || _charWidth < 1)
            return;

        double x = _hexAreaLeft + _activeColumn * _charWidth;
        double h = ActualHeight;

        dc.DrawRectangle(HighlightBrush, null, new Rect(x, 0, _charWidth, h));
    }
}
