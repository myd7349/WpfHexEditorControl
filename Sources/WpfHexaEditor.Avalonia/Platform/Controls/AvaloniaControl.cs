//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using Avalonia.Controls;
using WpfHexaEditor.Core.Platform.Controls;

namespace WpfHexaEditor.Avalonia.Platform.Controls
{
    /// <summary>
    /// Avalonia implementation of IPlatformControl that wraps Avalonia.Controls.Control.
    /// </summary>
    public class AvaloniaControl : IPlatformControl
    {
        private readonly Control _control;

        /// <summary>
        /// Gets the underlying Avalonia Control.
        /// </summary>
        public Control NativeControl => _control;

        public AvaloniaControl(Control control)
        {
            _control = control ?? throw new ArgumentNullException(nameof(control));
        }

        /// <summary>
        /// Gets or sets the width of the control.
        /// </summary>
        public double Width
        {
            get => _control.Width;
            set => _control.Width = value;
        }

        /// <summary>
        /// Gets or sets the height of the control.
        /// </summary>
        public double Height
        {
            get => _control.Height;
            set => _control.Height = value;
        }

        /// <summary>
        /// Gets the actual width of the control after layout.
        /// </summary>
        public double ActualWidth => _control.Bounds.Width;

        /// <summary>
        /// Gets the actual height of the control after layout.
        /// </summary>
        public double ActualHeight => _control.Bounds.Height;

        /// <summary>
        /// Gets or sets whether the control is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _control.IsEnabled;
            set => _control.IsEnabled = value;
        }

        /// <summary>
        /// Gets or sets whether the control is visible.
        /// </summary>
        public bool IsVisible
        {
            get => _control.IsVisible;
            set => _control.IsVisible = value;
        }

        /// <summary>
        /// Gets whether the control is focused.
        /// </summary>
        public bool IsFocused => _control.IsFocused;

        /// <summary>
        /// Attempts to set focus to the control.
        /// </summary>
        public bool Focus()
        {
            _control.Focus();
            return _control.IsFocused;
        }

        /// <summary>
        /// Invalidates the visual representation of the control.
        /// </summary>
        public void InvalidateVisual()
        {
            _control.InvalidateVisual();
        }
    }
}
