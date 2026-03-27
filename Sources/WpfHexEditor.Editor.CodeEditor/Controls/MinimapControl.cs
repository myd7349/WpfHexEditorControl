//////////////////////////////////////////////
// Project: WpfHexEditor.Editor.CodeEditor
// File: Controls/MinimapControl.cs
// Description:
//     VS Code-style code overview minimap. Renders a compressed view of the
//     entire document using tiny colored rectangles per syntax token.
//     Click to navigate, drag to scroll. Shows viewport indicator.
// Architecture:
//     Standalone FrameworkElement — placed beside the CodeEditor in SplitHost.
//     Reads document lines + syntax tokens from the attached CodeEditor.
//     Renders via DrawingContext (zero WPF containers — same pattern as
//     BinaryDiffCanvas and BarChartPanel).
//////////////////////////////////////////////

using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfHexEditor.Editor.CodeEditor.Controls;

/// <summary>
/// Minimap overview of the code document. Each line is rendered as a thin row
/// of colored rectangles matching syntax token colors.
/// </summary>
public sealed class MinimapControl : FrameworkElement
{
    private CodeEditor? _editor;
    private const double RowHeight = 2.0;
    private const double CharWidth = 1.2;
    private const int MaxVisibleChars = 80;

    // Cached brushes
    private static readonly Brush ViewportBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
    private static readonly Brush ViewportBorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
    private static readonly Pen ViewportPen = new(ViewportBorderBrush, 1.0);
    private static readonly Brush DefaultTextBrush = new SolidColorBrush(Color.FromArgb(100, 200, 200, 200));
    private static readonly Brush KeywordBrush = new SolidColorBrush(Color.FromArgb(180, 86, 156, 214));
    private static readonly Brush StringBrush = new SolidColorBrush(Color.FromArgb(180, 206, 145, 120));
    private static readonly Brush CommentBrush = new SolidColorBrush(Color.FromArgb(120, 106, 153, 85));
    private static readonly Brush NumberBrush = new SolidColorBrush(Color.FromArgb(180, 181, 206, 168));
    private static readonly Brush TypeBrush = new SolidColorBrush(Color.FromArgb(180, 78, 201, 176));

    static MinimapControl()
    {
        ViewportBrush.Freeze();
        ViewportBorderBrush.Freeze();
        ViewportPen.Freeze();
        DefaultTextBrush.Freeze();
        KeywordBrush.Freeze();
        StringBrush.Freeze();
        CommentBrush.Freeze();
        NumberBrush.Freeze();
        TypeBrush.Freeze();
    }

    /// <summary>Width of the minimap in pixels.</summary>
    public double MinimapWidth { get; set; } = 100;

    /// <summary>Attaches to a CodeEditor for document and viewport tracking.</summary>
    public void SetEditor(CodeEditor editor)
    {
        _editor = editor;
        InvalidateVisual();
    }

    /// <summary>Called by the host when the document or viewport changes.</summary>
    public void Refresh() => InvalidateVisual();

    protected override Size MeasureOverride(Size availableSize)
    {
        var h = double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height;
        return new Size(MinimapWidth, h);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w < 1 || h < 1 || _editor is null) return;

        // Background
        var bgBrush = TryFindResource("TE_Background") as Brush ?? Brushes.Black;
        dc.DrawRectangle(bgBrush, null, new Rect(0, 0, w, h));

        var doc = _editor.Document;
        if (doc is null || doc.Lines.Count == 0) return;

        int totalLines = doc.Lines.Count;
        double scale = Math.Min(RowHeight, h / totalLines);
        double totalHeight = totalLines * scale;

        // Draw lines as colored rectangles
        for (int i = 0; i < totalLines && i * scale < h; i++)
        {
            var line = doc.Lines[i];
            if (line.Text is null || line.Text.Length == 0) continue;

            double y = i * scale;
            int chars = Math.Min(line.Text.Length, MaxVisibleChars);

            // Simple: draw the whole line as a single rect with opacity based on content density
            double lineWidth = chars * CharWidth;
            var brush = GetLineBrush(line.Text);
            dc.DrawRectangle(brush, null, new Rect(2, y, Math.Min(lineWidth, w - 4), Math.Max(scale, 1)));
        }

        // Viewport indicator
        if (_editor.VirtualizationEngine is { } ve && ve.TotalLines > 0)
        {
            int firstVisible = ve.FirstVisibleLine;
            int visibleCount = ve.VisibleLineCount;

            double vpTop = firstVisible * scale;
            double vpHeight = Math.Max(visibleCount * scale, 10);
            vpHeight = Math.Min(vpHeight, h - vpTop);

            dc.DrawRectangle(ViewportBrush, ViewportPen, new Rect(0, vpTop, w, vpHeight));
        }
    }

    // ── Mouse interaction ────────────────────────────────────────────────────

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        NavigateToY(e.GetPosition(this).Y);
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.LeftButton == MouseButtonState.Pressed && IsMouseCaptured)
            NavigateToY(e.GetPosition(this).Y);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        ReleaseMouseCapture();
    }

    private void NavigateToY(double y)
    {
        if (_editor?.Document is null) return;
        int totalLines = _editor.Document.Lines.Count;
        double scale = Math.Min(RowHeight, ActualHeight / totalLines);
        int targetLine = Math.Clamp((int)(y / scale), 0, totalLines - 1);
        _editor.NavigateToLine(targetLine + 1); // 1-based
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Brush GetLineBrush(string text)
    {
        var trimmed = text.AsSpan().TrimStart();
        if (trimmed.Length == 0) return Brushes.Transparent;
        if (trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("*"))
            return CommentBrush;
        if (trimmed.StartsWith("\"") || trimmed.StartsWith("'") || trimmed.StartsWith("@\"") || trimmed.StartsWith("$\""))
            return StringBrush;
        if (trimmed.StartsWith("using ") || trimmed.StartsWith("namespace ") ||
            trimmed.StartsWith("public ") || trimmed.StartsWith("private ") ||
            trimmed.StartsWith("protected ") || trimmed.StartsWith("internal ") ||
            trimmed.StartsWith("class ") || trimmed.StartsWith("interface ") ||
            trimmed.StartsWith("struct ") || trimmed.StartsWith("enum ") ||
            trimmed.StartsWith("if ") || trimmed.StartsWith("else") ||
            trimmed.StartsWith("for ") || trimmed.StartsWith("foreach ") ||
            trimmed.StartsWith("while ") || trimmed.StartsWith("return ") ||
            trimmed.StartsWith("var ") || trimmed.StartsWith("async ") ||
            trimmed.StartsWith("await "))
            return KeywordBrush;
        return DefaultTextBrush;
    }
}
