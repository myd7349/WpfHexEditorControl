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
    /// Avalonia implementation of global::Avalonia.Media.IPen that wraps Avalonia.Media.global::Avalonia.Media.IPen.
    /// </summary>
    public class AvaloniaPen : Core.Platform.Media.IPen
    {
        /// <summary>
        /// The underlying Avalonia pen.
        /// </summary>
        public global::Avalonia.Media.IPen NativePen { get; }

        /// <summary>
        /// Gets the platform-specific pen object.
        /// </summary>
        public object PlatformPen => NativePen;

        /// <summary>
        /// Gets or sets the brush used to draw the pen.
        /// </summary>
        public Core.Platform.Media.IBrush? Brush
        {
            get => NativePen.Brush != null ? new AvaloniaBrush(NativePen.Brush) : null;
            set
            {
                // Avalonia global::Avalonia.Media.IPen is immutable, so we can't set properties
                // This is a limitation of the Avalonia API
            }
        }

        /// <summary>
        /// Gets or sets the thickness of the pen.
        /// </summary>
        public double Thickness
        {
            get => NativePen.Thickness;
            set
            {
                // Avalonia global::Avalonia.Media.IPen is immutable
            }
        }

        public AvaloniaPen(global::Avalonia.Media.IPen pen)
        {
            NativePen = pen ?? throw new ArgumentNullException(nameof(pen));
        }

        /// <summary>
        /// Creates a new AvaloniaPen from a brush and thickness.
        /// </summary>
        public static AvaloniaPen Create(Core.Platform.Media.IBrush brush, double thickness)
        {
            if (brush is AvaloniaBrush avaloniaBrush)
            {
                return new AvaloniaPen(new Pen(avaloniaBrush.NativeBrush, thickness));
            }
            throw new ArgumentException("Brush must be an AvaloniaBrush", nameof(brush));
        }

        /// <summary>
        /// Converts an Avalonia global::Avalonia.Media.IPen to Core global::Avalonia.Media.IPen.
        /// </summary>

        /// <summary>
        /// Converts Core global::Avalonia.Media.IPen to Avalonia global::Avalonia.Media.IPen.
        /// </summary>
    }
}
