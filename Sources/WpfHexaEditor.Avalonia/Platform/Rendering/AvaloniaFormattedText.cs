//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using Avalonia.Media;
using WpfHexaEditor.Core.Platform.Media;
using WpfHexaEditor.Core.Platform.Rendering;
using WpfHexaEditor.Avalonia.Platform.Media;

namespace WpfHexaEditor.Avalonia.Platform.Rendering
{
    /// <summary>
    /// Avalonia implementation of IFormattedText that wraps Avalonia.Media.FormattedText.
    /// </summary>
    public class AvaloniaFormattedText : IFormattedText
    {
        private readonly FormattedText _formattedText;
        private readonly string _text;
        private Core.Platform.Media.IBrush? _foreground;

        /// <summary>
        /// Gets the text content.
        /// </summary>
        public string Text => _text;

        /// <summary>
        /// Gets or sets the foreground brush.
        /// </summary>
        public Core.Platform.Media.IBrush? Foreground
        {
            get => _foreground;
            set => _foreground = value;
        }

        /// <summary>
        /// Gets the width of the formatted text.
        /// </summary>
        public double Width => _formattedText.Width;

        /// <summary>
        /// Gets the height of the formatted text.
        /// </summary>
        public double Height => _formattedText.Height;

        /// <summary>
        /// Gets or sets the maximum text width before wrapping.
        /// </summary>
        public double MaxTextWidth
        {
            get => _formattedText.MaxTextWidth;
            set => _formattedText.MaxTextWidth = value;
        }

        /// <summary>
        /// Gets or sets the maximum text height.
        /// </summary>
        public double MaxTextHeight
        {
            get => _formattedText.MaxTextHeight;
            set => _formattedText.MaxTextHeight = value;
        }

        /// <summary>
        /// Gets the underlying platform-specific formatted text object.
        /// </summary>
        public object PlatformText => _formattedText;

        public AvaloniaFormattedText(FormattedText formattedText, string text, Core.Platform.Media.IBrush? foreground = null)
        {
            _formattedText = formattedText ?? throw new ArgumentNullException(nameof(formattedText));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _foreground = foreground;
        }

        /// <summary>
        /// Creates a new AvaloniaFormattedText with the specified parameters.
        /// </summary>
        public static AvaloniaFormattedText Create(
            string text,
            Typeface typeface,
            double fontSize,
            Core.Platform.Media.IBrush? foreground)
        {
            var formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                global::Avalonia.Media.FlowDirection.LeftToRight,
                typeface,
                fontSize,
                null); // Avalonia doesn't use foreground in constructor

            return new AvaloniaFormattedText(formattedText, text, foreground);
        }

        /// <summary>
        /// Gets the underlying Avalonia FormattedText.
        /// </summary>
        public FormattedText NativeText => _formattedText;
    }
}
