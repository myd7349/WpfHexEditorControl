using System;

namespace WpfHexaEditor.Core.Platform.Threading
{
    /// <summary>
    /// Platform-agnostic timer interface for periodic operations.
    /// Wraps WPF DispatcherTimer or Avalonia DispatcherTimer.
    /// </summary>
    public interface IPlatformTimer : IDisposable
    {
        /// <summary>
        /// Gets or sets the time interval between timer ticks.
        /// </summary>
        TimeSpan Interval { get; set; }

        /// <summary>
        /// Gets or sets whether the timer is enabled.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Occurs when the timer interval has elapsed.
        /// </summary>
        event EventHandler? Tick;

        /// <summary>
        /// Starts the timer.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the timer.
        /// </summary>
        void Stop();
    }
}
