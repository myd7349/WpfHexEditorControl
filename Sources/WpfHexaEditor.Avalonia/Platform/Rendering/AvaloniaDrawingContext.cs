//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using Avalonia;
using Avalonia.Media;
using WpfHexaEditor.Core.Platform.Media;
using WpfHexaEditor.Core.Platform.Rendering;
using WpfHexaEditor.Avalonia.Platform.Media;

namespace WpfHexaEditor.Avalonia.Platform.Rendering
{
    /// <summary>
    /// Avalonia implementation of IDrawingContext that wraps Avalonia.Media.DrawingContext.
    /// </summary>
    public class AvaloniaDrawingContext : IDrawingContext
    {
        private readonly DrawingContext _drawingContext;
        private bool _disposed;

        /// <summary>
        /// Gets the underlying Avalonia DrawingContext.
        /// </summary>
        public DrawingContext NativeContext => _drawingContext;

        public AvaloniaDrawingContext(DrawingContext drawingContext)
        {
            _drawingContext = drawingContext ?? throw new ArgumentNullException(nameof(drawingContext));
        }

        /// <summary>
        /// Draws a rectangle with the specified brush and pen.
        /// </summary>
        public void DrawRectangle(Core.Platform.Media.IBrush? brush, Core.Platform.Media.IPen? pen, double x, double y, double width, double height)
        {
            var avaloniaBrush = brush is AvaloniaBrush ab ? ab.NativeBrush : null;
            var avaloniaPen = pen is AvaloniaPen ap ? ap.NativePen : null;

            var rect = new Rect(x, y, width, height);
            _drawingContext.DrawRectangle(avaloniaBrush, avaloniaPen, rect);
        }

        /// <summary>
        /// Draws a line between two points.
        /// </summary>
        public void DrawLine(Core.Platform.Media.IPen pen, double x1, double y1, double x2, double y2)
        {
            if (pen is not AvaloniaPen avaloniaPen)
                throw new ArgumentException("Pen must be an AvaloniaPen", nameof(pen));

            var point1 = new Point(x1, y1);
            var point2 = new Point(x2, y2);
            _drawingContext.DrawLine(avaloniaPen.NativePen, point1, point2);
        }

        /// <summary>
        /// Draws formatted text at the specified location.
        /// </summary>
        public void DrawText(IFormattedText text, double x, double y)
        {
            if (text is not AvaloniaFormattedText avaloniaText)
                throw new ArgumentException("Text must be an AvaloniaFormattedText", nameof(text));

            var point = new Point(x, y);

            // Avalonia's DrawText requires a brush, so we use the foreground from formatted text
            global::Avalonia.Media.IBrush? brush = null;
            if (avaloniaText.Foreground is AvaloniaBrush ab)
                brush = ab.NativeBrush;

            if (brush is not null)
            {
                _drawingContext.DrawText(avaloniaText.NativeText, point);
            }
        }

        /// <summary>
        /// Draws an ellipse with the specified brush and pen.
        /// </summary>
        public void DrawEllipse(Core.Platform.Media.IBrush? brush, Core.Platform.Media.IPen? pen, double centerX, double centerY, double radiusX, double radiusY)
        {
            var avaloniaBrush = brush is AvaloniaBrush ab ? ab.NativeBrush : null;
            var avaloniaPen = pen is AvaloniaPen ap ? ap.NativePen : null;

            var center = new Point(centerX, centerY);
            _drawingContext.DrawEllipse(avaloniaBrush, avaloniaPen, center, radiusX, radiusY);
        }

        /// <summary>
        /// Pushes an opacity value onto the drawing context.
        /// </summary>
        public IDisposable PushOpacity(double opacity)
        {
            return _drawingContext.PushOpacity(opacity);
        }

        /// <summary>
        /// Pushes a clip rectangle onto the drawing context.
        /// </summary>
        public IDisposable PushClip(double x, double y, double width, double height)
        {
            var rect = new Rect(x, y, width, height);
            return _drawingContext.PushClip(rect);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _drawingContext.Dispose();
            _disposed = true;
        }
    }
}
