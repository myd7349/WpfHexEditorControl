// ==========================================================
// Project: WpfHexEditor.Plugins.ScreenRecorder
// File: Overlay/CaptureOverlayWindow.xaml.cs
// Description: Non-activating, transparent overlay drawn around the capture region.
//              Excluded from captures via HWND hide/show in FrameCaptureEngine.
// Architecture Notes:
//     WS_EX_NOACTIVATE + WS_EX_TOOLWINDOW set via HwndSource after Show() to
//     prevent focus theft. WM_MOUSEACTIVATE returns MA_NOACTIVATE.
// ==========================================================

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WpfHexEditor.Plugins.ScreenRecorder.Models;
using WpfHexEditor.Plugins.ScreenRecorder.Options;
using WpfHexEditor.Plugins.ScreenRecorder.ViewModels;

namespace WpfHexEditor.Plugins.ScreenRecorder.Overlay;

public partial class CaptureOverlayWindow : Window
{
    private const int GWL_EXSTYLE       = -20;
    private const int WS_EX_NOACTIVATE  = 0x08000000;
    private const int WS_EX_TOOLWINDOW  = 0x00000080;
    private const int WM_MOUSEACTIVATE  = 0x0021;
    private const int MA_NOACTIVATE     = 3;

    public IntPtr OverlayHwnd { get; private set; }

    public Brush OverlayBrush { get; private set; } =
        ColorConverter.ConvertFromString(ScreenRecorderOptions.Instance.OverlayColor) is Color c
            ? new SolidColorBrush(c)
            : new SolidColorBrush(Colors.Red);

    public CaptureOverlayWindow() => InitializeComponent();

    public void ShowOverlay(CaptureRegion region, CaptureHudViewModel hud)
    {
        Hud.DataContext = hud;
        ApplyRegion(region);

        if (!IsVisible)
        {
            Show();
            ApplyNonActivatingStyle();
        }
    }

    public void UpdateRegion(CaptureRegion region) => ApplyRegion(region);

    public void HideOverlay() => Hide();

    // ── Win32 non-activating setup ────────────────────────────────────────────

    private void ApplyNonActivatingStyle()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        OverlayHwnd = hwnd;

        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);

        HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_MOUSEACTIVATE) { handled = true; return (IntPtr)MA_NOACTIVATE; }
        return IntPtr.Zero;
    }

    private void ApplyRegion(CaptureRegion r)
    {
        Left   = r.X;
        Top    = r.Y;
        Width  = r.Width;
        Height = r.Height;
    }

    [DllImport("user32.dll")] private static extern int  GetWindowLong(IntPtr hwnd, int idx);
    [DllImport("user32.dll")] private static extern int  SetWindowLong(IntPtr hwnd, int idx, int val);
}
