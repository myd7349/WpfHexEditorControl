//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using Avalonia.Threading;
using WpfHexaEditor.Core.Platform.Threading;

namespace WpfHexaEditor.Avalonia.Platform.Threading
{
    /// <summary>
    /// Avalonia implementation of IPlatformTimer that wraps DispatcherTimer.
    /// </summary>
    public class AvaloniaTimer : IPlatformTimer
    {
        private readonly DispatcherTimer _timer;
        private bool _disposed;

        /// <summary>
        /// Gets or sets the time interval between timer ticks.
        /// </summary>
        public TimeSpan Interval
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        /// <summary>
        /// Gets or sets whether the timer is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _timer.IsEnabled;
            set => _timer.IsEnabled = value;
        }

        /// <summary>
        /// Occurs when the timer interval has elapsed.
        /// </summary>
        public event EventHandler? Tick;

        public AvaloniaTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Tick += OnTick;
        }

        public AvaloniaTimer(DispatcherPriority priority)
        {
            _timer = new DispatcherTimer(priority);
            _timer.Tick += OnTick;
        }

        private void OnTick(object? sender, EventArgs e)
        {
            Tick?.Invoke(this, e);
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        public void Start()
        {
            _timer.Start();
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            _timer.Stop();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _timer.Tick -= OnTick;
            _timer.Stop();
            _disposed = true;
        }
    }
}
