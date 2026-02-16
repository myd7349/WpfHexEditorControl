using WpfHexaEditor.Core.Platform.Media;

namespace WpfHexaEditor.Core.Platform.Rendering
{
    /// <summary>
    /// Platform-agnostic formatted text interface for text rendering.
    /// Wraps WPF FormattedText or Avalonia FormattedText.
    /// </summary>
    public interface IFormattedText
    {
        /// <summary>
        /// Gets the text content.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Gets or sets the foreground brush.
        /// </summary>
        IBrush? Foreground { get; set; }

        /// <summary>
        /// Gets the width of the formatted text.
        /// </summary>
        double Width { get; }

        /// <summary>
        /// Gets the height of the formatted text.
        /// </summary>
        double Height { get; }

        /// <summary>
        /// Gets or sets the maximum text width before wrapping.
        /// </summary>
        double MaxTextWidth { get; set; }

        /// <summary>
        /// Gets or sets the maximum text height.
        /// </summary>
        double MaxTextHeight { get; set; }

        /// <summary>
        /// Gets the underlying platform-specific formatted text object.
        /// WPF: Returns System.Windows.Media.FormattedText
        /// Avalonia: Returns Avalonia.Media.FormattedText
        /// </summary>
        object PlatformText { get; }
    }
}
