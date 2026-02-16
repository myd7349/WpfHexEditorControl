namespace WpfHexaEditor.Core.Platform.Controls
{
    /// <summary>
    /// Platform-agnostic control interface providing common control properties.
    /// Abstracts differences between WPF Control and Avalonia Control.
    /// </summary>
    public interface IPlatformControl
    {
        /// <summary>
        /// Gets or sets the width of the control.
        /// </summary>
        double Width { get; set; }

        /// <summary>
        /// Gets or sets the height of the control.
        /// </summary>
        double Height { get; set; }

        /// <summary>
        /// Gets the actual width of the control after layout.
        /// </summary>
        double ActualWidth { get; }

        /// <summary>
        /// Gets the actual height of the control after layout.
        /// </summary>
        double ActualHeight { get; }

        /// <summary>
        /// Gets or sets whether the control is enabled.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets whether the control is visible.
        /// </summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// Gets whether the control is focused.
        /// </summary>
        bool IsFocused { get; }

        /// <summary>
        /// Attempts to set focus to the control.
        /// </summary>
        /// <returns>True if focus was successfully set; otherwise, false.</returns>
        bool Focus();

        /// <summary>
        /// Invalidates the visual representation of the control, forcing a redraw.
        /// </summary>
        void InvalidateVisual();
    }
}
