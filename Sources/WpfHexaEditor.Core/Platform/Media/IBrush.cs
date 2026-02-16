namespace WpfHexaEditor.Core.Platform.Media
{
    /// <summary>
    /// Platform-agnostic brush interface for painting regions.
    /// Implemented by WPF Brush wrapper and Avalonia IBrush wrapper.
    /// </summary>
    public interface IBrush
    {
        /// <summary>
        /// Gets the underlying platform-specific brush object.
        /// WPF: Returns System.Windows.Media.Brush
        /// Avalonia: Returns Avalonia.Media.IBrush
        /// </summary>
        object PlatformBrush { get; }
    }
}
