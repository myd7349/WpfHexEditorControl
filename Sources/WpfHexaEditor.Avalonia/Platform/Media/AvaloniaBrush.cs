//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using Avalonia.Media;
using WpfHexaEditor.Core.Platform.Media;

namespace WpfHexaEditor.Avalonia.Platform.Media
{
    /// <summary>
    /// Avalonia implementation of global::Avalonia.Media.IBrush that wraps Avalonia.Media.global::Avalonia.Media.IBrush.
    /// </summary>
    public class AvaloniaBrush : Core.Platform.Media.IBrush
    {
        /// <summary>
        /// The underlying Avalonia brush.
        /// </summary>
        public global::Avalonia.Media.IBrush NativeBrush { get; }

        /// <summary>
        /// Gets the platform-specific brush object.
        /// </summary>
        public object PlatformBrush => NativeBrush;

        public AvaloniaBrush(global::Avalonia.Media.IBrush brush)
        {
            NativeBrush = brush ?? throw new ArgumentNullException(nameof(brush));
        }

        /// <summary>
        /// Creates an AvaloniaBrush from a PlatformColor.
        /// </summary>
        public static AvaloniaBrush FromColor(PlatformColor color)
        {
            var avaloniaColor = Color.FromArgb(color.A, color.R, color.G, color.B);
            return new AvaloniaBrush(new SolidColorBrush(avaloniaColor));
        }

        /// <summary>
        /// Converts an Avalonia global::Avalonia.Media.IBrush to Core global::Avalonia.Media.IBrush.
        /// </summary>

        /// <summary>
        /// Converts Core global::Avalonia.Media.IBrush to Avalonia global::Avalonia.Media.IBrush.
        /// </summary>
    }
}
