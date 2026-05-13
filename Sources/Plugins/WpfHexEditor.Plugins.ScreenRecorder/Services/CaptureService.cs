// ==========================================================
// Project: WpfHexEditor.Plugins.ScreenRecorder
// File: Services/CaptureService.cs
// Description: Orchestrates capture sessions — timer-based and manual keypress modes.
//              Fires FrameCaptured events with frozen BitmapSources.
// Architecture Notes:
//     DispatcherTimer used for UI-thread-safe interval ticking.
//     ManualCapture() can be called from any thread; it posts to Dispatcher internally.
//     OverlayHwnd is provided by CaptureOverlayWindow and injected after Show().
//     MaxFrames cap (from ScreenRecorderOptions) auto-stops the session when reached.
// ==========================================================

using System.Diagnostics;
using System.Windows.Threading;
using WpfHexEditor.Plugins.ScreenRecorder.Models;
using WpfHexEditor.Plugins.ScreenRecorder.Options;

namespace WpfHexEditor.Plugins.ScreenRecorder.Services;

public sealed class CaptureService : IDisposable
{
    public event EventHandler<CaptureFrame>? FrameCaptured;
    public event EventHandler?               SessionStopped;

    public CaptureSession? CurrentSession  { get; private set; }
    public bool            IsSessionActive  => _state == SessionState.Active;
    public bool            IsPaused         => _state == SessionState.Paused;

    private DispatcherTimer? _timer;
    private readonly Stopwatch _elapsed = new();
    private int          _frameIndex;
    private SessionState _state = SessionState.Stopped;
    private bool         _capturingFrame;
    private IntPtr       _overlayHwnd;

    private enum SessionState { Stopped, Active, Paused }

    public void SetOverlayHwnd(IntPtr hwnd) => _overlayHwnd = hwnd;

    public void StartSession(CaptureSession session)
    {
        StopSession();

        CurrentSession  = session;
        _frameIndex     = 0;
        _state          = SessionState.Active;
        _capturingFrame = false;
        _elapsed.Restart();

        if (session.Mode is RecordingMode.TimedInterval or RecordingMode.Both)
            StartTimer(session.GlobalDelay);
    }

    public void PauseSession()
    {
        if (_state == SessionState.Stopped) return;
        if (_state == SessionState.Active)
        {
            _state = SessionState.Paused;
            _timer?.Stop();
            _elapsed.Stop();
        }
        else
        {
            _state = SessionState.Active;
            _timer?.Start();
            _elapsed.Start();
        }
    }

    public void StopSession()
    {
        _state = SessionState.Stopped;
        _timer?.Stop();
        _timer = null;
        _elapsed.Stop();

        var prev = CurrentSession;
        CurrentSession = null;
        if (prev is not null) SessionStopped?.Invoke(this, EventArgs.Empty);
    }

    public void TriggerManualCapture()
    {
        if (_state != SessionState.Active) return;
        if (CurrentSession?.Mode is RecordingMode.TimedInterval) return;
        if (_capturingFrame) return;
        _ = CaptureOneFrameAsync();
    }

    public TimeSpan Elapsed => _state == SessionState.Stopped ? TimeSpan.Zero : _elapsed.Elapsed;

    public void Dispose() => StopSession();

    // ── Timer ─────────────────────────────────────────────────────────────────

    private void StartTimer(int intervalMs)
    {
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(Math.Max(80, intervalMs))
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (!_capturingFrame) _ = CaptureOneFrameAsync();
    }

    private async Task CaptureOneFrameAsync()
    {
        if (_state != SessionState.Active || CurrentSession is null) return;
        if (_frameIndex >= ScreenRecorderOptions.Instance.MaxFrames) { StopSession(); return; }

        _capturingFrame = true;
        try
        {
            var session = CurrentSession;
            var bitmap  = await FrameCaptureEngine.CaptureRegionAsync(session.Region, _overlayHwnd);

            if (_state != SessionState.Active) return;

            var frame = new CaptureFrame(_frameIndex++, bitmap, session.GlobalDelay, null, DateTimeOffset.UtcNow);
            session.AddFrame(frame);
            FrameCaptured?.Invoke(this, frame);
        }
        finally
        {
            _capturingFrame = false;
        }
    }
}
