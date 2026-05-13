// ==========================================================
// Project: WpfHexEditor.Plugins.ScreenRecorder
// File: Overlay/RegionSelectorWindow.xaml.cs
// Description: Rubber-band region selector — full-screen transparent drag canvas.
//              Returns screen-coordinate CaptureRegion on mouse-up; null on ESC.
// ==========================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfHexEditor.Plugins.ScreenRecorder.Models;

namespace WpfHexEditor.Plugins.ScreenRecorder.Overlay;

public partial class RegionSelectorWindow : Window
{
    public CaptureRegion? SelectedRegion { get; private set; }

    private Point _startPoint;
    private bool  _dragging;

    public RegionSelectorWindow()
    {
        InitializeComponent();
        WindowState = WindowState.Maximized;
        Left = SystemParameters.VirtualScreenLeft;
        Top  = SystemParameters.VirtualScreenTop;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        RootCanvas.Focus();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(RootCanvas);
        _dragging   = true;
        RootCanvas.CaptureMouse();

        Canvas.SetLeft(SelectionRect, _startPoint.X);
        Canvas.SetTop(SelectionRect, _startPoint.Y);
        SelectionRect.Width      = 0;
        SelectionRect.Height     = 0;
        SelectionRect.Visibility = Visibility.Visible;
        InstructionText.Visibility = Visibility.Collapsed;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        UpdateRect(e.GetPosition(RootCanvas));
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        RootCanvas.ReleaseMouseCapture();
        UpdateRect(e.GetPosition(RootCanvas));

        SelectedRegion = BuildRegion();
        Close();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { SelectedRegion = null; Close(); }
    }

    private void UpdateRect(Point current)
    {
        var x = Math.Min(_startPoint.X, current.X);
        var y = Math.Min(_startPoint.Y, current.Y);
        var w = Math.Abs(current.X - _startPoint.X);
        var h = Math.Abs(current.Y - _startPoint.Y);

        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width  = w;
        SelectionRect.Height = h;
    }

    private CaptureRegion? BuildRegion()
    {
        var w = (int)SelectionRect.Width;
        var h = (int)SelectionRect.Height;
        if (w < 10 || h < 10) return null;

        // Convert canvas coords to screen coords
        var screenPt = PointToScreen(new Point(Canvas.GetLeft(SelectionRect), Canvas.GetTop(SelectionRect)));
        return new CaptureRegion((int)screenPt.X, (int)screenPt.Y, w, h);
    }
}
