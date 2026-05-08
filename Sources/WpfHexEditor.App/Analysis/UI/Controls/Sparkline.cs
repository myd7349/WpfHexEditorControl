// ==========================================================
// Project: WpfHexEditor.App
// File: Analysis/UI/Controls/Sparkline.cs
// Description: Tiny inline line chart (40×16 typical) for trending columns.
//              No labels, no axes — just the shape of the recent values.
// Architecture Notes:
//     Renders directly on the DrawingContext. Bind Values to IList<int>.
// ==========================================================

using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfHexEditor.App.Analysis.UI.Controls;

public sealed class Sparkline : Control
{
    public static readonly DependencyProperty ValuesProperty =
        DependencyProperty.Register(nameof(Values), typeof(IList), typeof(Sparkline),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public IList? Values
    {
        get => (IList?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (Values is null || Values.Count < 2) return;

        var nums = new double[Values.Count];
        int i = 0;
        foreach (var v in Values) nums[i++] = Convert.ToDouble(v);

        double max = nums.Max();
        double min = nums.Min();
        double range = max - min;
        if (range < 1) range = 1;

        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var fig = new PathFigure { IsClosed = false, IsFilled = false };
        for (int k = 0; k < nums.Length; k++)
        {
            double x = k * (w - 1) / (nums.Length - 1);
            double y = h - (nums[k] - min) / range * h;
            var p = new Point(x, y);
            if (k == 0) fig.StartPoint = p;
            else        fig.Segments.Add(new LineSegment(p, true));
        }

        var pen = new Pen((Foreground as SolidColorBrush) ?? Brushes.SteelBlue, 1.5);
        dc.DrawGeometry(null, pen, new PathGeometry { Figures = { fig } });
    }
}
