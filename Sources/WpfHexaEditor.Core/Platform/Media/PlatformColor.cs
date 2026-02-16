using System;

namespace WpfHexaEditor.Core.Platform.Media
{
    /// <summary>
    /// Platform-agnostic color structure representing ARGB color values.
    /// Can be converted to/from WPF Color or Avalonia Color.
    /// </summary>
    public readonly struct PlatformColor : IEquatable<PlatformColor>
    {
        /// <summary>
        /// Gets the alpha (transparency) component value (0-255).
        /// </summary>
        public byte A { get; }

        /// <summary>
        /// Gets the red component value (0-255).
        /// </summary>
        public byte R { get; }

        /// <summary>
        /// Gets the green component value (0-255).
        /// </summary>
        public byte G { get; }

        /// <summary>
        /// Gets the blue component value (0-255).
        /// </summary>
        public byte B { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="PlatformColor"/> with ARGB values.
        /// </summary>
        public PlatformColor(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// Creates a color from RGB values with full opacity (alpha = 255).
        /// </summary>
        public static PlatformColor FromRgb(byte r, byte g, byte b) => new PlatformColor(255, r, g, b);

        /// <summary>
        /// Creates a color from ARGB values.
        /// </summary>
        public static PlatformColor FromArgb(byte a, byte r, byte g, byte b) => new PlatformColor(a, r, g, b);

        /// <summary>
        /// Creates a color from a 32-bit ARGB value.
        /// </summary>
        public static PlatformColor FromUInt32(uint argb)
        {
            return new PlatformColor(
                (byte)((argb >> 24) & 0xFF),
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF)
            );
        }

        /// <summary>
        /// Converts the color to a 32-bit ARGB value.
        /// </summary>
        public uint ToUInt32() => ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | B;

        /// <summary>
        /// Returns a string representation of the color in ARGB format.
        /// </summary>
        public override string ToString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

        #region Equality

        public bool Equals(PlatformColor other) => A == other.A && R == other.R && G == other.G && B == other.B;

        public override bool Equals(object? obj) => obj is PlatformColor other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + A.GetHashCode();
                hash = hash * 31 + R.GetHashCode();
                hash = hash * 31 + G.GetHashCode();
                hash = hash * 31 + B.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(PlatformColor left, PlatformColor right) => left.Equals(right);

        public static bool operator !=(PlatformColor left, PlatformColor right) => !left.Equals(right);

        #endregion

        #region Predefined Colors

        public static PlatformColor Transparent => FromArgb(0, 0, 0, 0);
        public static PlatformColor Black => FromRgb(0, 0, 0);
        public static PlatformColor White => FromRgb(255, 255, 255);
        public static PlatformColor Red => FromRgb(255, 0, 0);
        public static PlatformColor Green => FromRgb(0, 255, 0);
        public static PlatformColor Blue => FromRgb(0, 0, 255);
        public static PlatformColor Yellow => FromRgb(255, 255, 0);
        public static PlatformColor Cyan => FromRgb(0, 255, 255);
        public static PlatformColor Magenta => FromRgb(255, 0, 255);
        public static PlatformColor Gray => FromRgb(128, 128, 128);
        public static PlatformColor LightGray => FromRgb(211, 211, 211);
        public static PlatformColor DarkGray => FromRgb(64, 64, 64);

        #endregion
    }
}
