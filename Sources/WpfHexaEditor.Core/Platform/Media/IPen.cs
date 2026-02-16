namespace WpfHexaEditor.Core.Platform.Media
{
    /// <summary>
    /// Platform-agnostic pen interface for drawing lines and outlines.
    /// Implemented by WPF Pen wrapper and Avalonia IPen wrapper.
    /// </summary>
    public interface IPen
    {
        /// <summary>
        /// Gets the brush used to draw the line.
        /// </summary>
        IBrush? Brush { get; }

        /// <summary>
        /// Gets the thickness of the pen.
        /// </summary>
        double Thickness { get; }

        /// <summary>
        /// Gets the underlying platform-specific pen object.
        /// WPF: Returns System.Windows.Media.Pen
        /// Avalonia: Returns Avalonia.Media.IPen
        /// </summary>
        object PlatformPen { get; }
    }
}
