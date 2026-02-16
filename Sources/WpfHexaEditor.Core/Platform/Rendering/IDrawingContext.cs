using System;
using WpfHexaEditor.Core.Platform.Media;

namespace WpfHexaEditor.Core.Platform.Rendering
{
    /// <summary>
    /// Platform-agnostic drawing context interface for rendering graphics.
    /// Provides a unified API for drawing operations that works with both WPF DrawingContext and Avalonia DrawingContext.
    /// </summary>
    public interface IDrawingContext : IDisposable
    {
        /// <summary>
        /// Draws a rectangle with the specified brush and pen.
        /// </summary>
        /// <param name="brush">The brush to fill the rectangle (null for no fill).</param>
        /// <param name="pen">The pen to draw the outline (null for no outline).</param>
        /// <param name="x">The X coordinate of the rectangle.</param>
        /// <param name="y">The Y coordinate of the rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        void DrawRectangle(IBrush? brush, IPen? pen, double x, double y, double width, double height);

        /// <summary>
        /// Draws a line between two points.
        /// </summary>
        /// <param name="pen">The pen to draw the line.</param>
        /// <param name="x1">The X coordinate of the start point.</param>
        /// <param name="y1">The Y coordinate of the start point.</param>
        /// <param name="x2">The X coordinate of the end point.</param>
        /// <param name="y2">The Y coordinate of the end point.</param>
        void DrawLine(IPen pen, double x1, double y1, double x2, double y2);

        /// <summary>
        /// Draws formatted text at the specified location.
        /// </summary>
        /// <param name="text">The formatted text to draw.</param>
        /// <param name="x">The X coordinate where the text is drawn.</param>
        /// <param name="y">The Y coordinate where the text is drawn.</param>
        void DrawText(IFormattedText text, double x, double y);

        /// <summary>
        /// Draws an ellipse with the specified brush and pen.
        /// </summary>
        /// <param name="brush">The brush to fill the ellipse (null for no fill).</param>
        /// <param name="pen">The pen to draw the outline (null for no outline).</param>
        /// <param name="centerX">The X coordinate of the center.</param>
        /// <param name="centerY">The Y coordinate of the center.</param>
        /// <param name="radiusX">The horizontal radius.</param>
        /// <param name="radiusY">The vertical radius.</param>
        void DrawEllipse(IBrush? brush, IPen? pen, double centerX, double centerY, double radiusX, double radiusY);

        /// <summary>
        /// Pushes an opacity value onto the drawing context.
        /// </summary>
        /// <param name="opacity">The opacity value (0.0 = transparent, 1.0 = opaque).</param>
        /// <returns>A disposable that pops the opacity when disposed.</returns>
        IDisposable PushOpacity(double opacity);

        /// <summary>
        /// Pushes a clip rectangle onto the drawing context.
        /// </summary>
        /// <param name="x">The X coordinate of the clip rectangle.</param>
        /// <param name="y">The Y coordinate of the clip rectangle.</param>
        /// <param name="width">The width of the clip rectangle.</param>
        /// <param name="height">The height of the clip rectangle.</param>
        /// <returns>A disposable that pops the clip when disposed.</returns>
        IDisposable PushClip(double x, double y, double width, double height);
    }
}
