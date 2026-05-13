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
    public bool            IsSessionActive  => CurrentSession is not null && !_paused && !_stopped;
    public bool            IsPaused         => _paused;

    private DispatcherTimer? _timer;
    private readonly Stopwatch _elapsed = new();
    private int     _frameIndex;
    private bool    _paused;
    private bool    _stopped;
    private IntPtr  _overlayHwnd;

    public void SetOverlayHwnd(IntPtr hwnd) => _overlayHwnd = hwnd;

    public void StartSession(CaptureSession session)
    {
        StopSession();

        CurrentSession = session;
        _frameIndex    = 0;
        _paused        = false;
        _stopped       = false;
        _elapsed.Restart();

        if (session.Mode is RecordingMode.TimedInterval or RecordingMode.Both)
            StartTimer(session.GlobalDelay);
    }

    public void PauseSession()
    {
        if (CurrentSession is null || _stopped) return;
        _paused = !_paused;
        if (_paused) { _timer?.Stop(); _elapsed.Stop(); }
        else         { _timer?.Start(); _elapsed.Start(); }
    }

    public void StopSession()
    {
        _stopped = true;
        _timer?.Stop();
        _timer = null;
        _elapsed.Stop();

        var prev = CurrentSession;
        CurrentSession = null;
        if (prev is not null) SessionStopped?.Invoke(this, EventArgs.Empty);
    }

    public void TriggerManualCapture()
    {
        if (CurrentSession is null || _paused || _stopped) return;
        if (CurrentSession.Mode is RecordingMode.TimedInterval) return;
        _ = CaptureOneFrameAsync();
    }

    public TimeSpan Elapsed => _elapsed.Elapsed;

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

    private void OnTimerTick(object? sender, EventArgs e) => _ = CaptureOneFrameAsync();

    // ── Core capture ──────────────────────────────────────────────────────────

    private async Task CaptureOneFrameAsync()
    {
        if (CurrentSession is null || _paused || _stopped) return;
        if (_frameIndex >= ScreenRecorderOptions.Instance.MaxFrames) { StopSession(); return; }

        var session = CurrentSession;
        var bitmap  = await FrameCaptureEngine.CaptureRegionAsync(session.Region, _overlayHwnd);

        if (_stopped) return; // session cancelled during async gap

        var delay   = session.GlobalDelay;
        var thumb   = FrameCaptureEngine.CreateThumbnail(bitmap);
        var frame   = new CaptureFrame(_frameIndex++, bitmap, delay, null, DateTimeOffset.UtcNow);

        session.AddFrame(frame);
        FrameCaptured?.Invoke(this, frame);
    }
}
