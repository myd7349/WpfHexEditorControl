// ==========================================================
// Project: WpfHexEditor.Plugins.ScreenRecorder
// File: Views/PreviewPane.xaml.cs
// Description: Code-behind for PreviewPane — handles Ctrl+MouseWheel zoom.
// ==========================================================

using System.Windows.Input;
using WpfHexEditor.Plugins.ScreenRecorder.ViewModels;

namespace WpfHexEditor.Plugins.ScreenRecorder.Views;

public partial class PreviewPane : System.Windows.Controls.UserControl
{
    public PreviewPane() => InitializeComponent();

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return;
        if (DataContext is not PreviewViewModel vm) return;

        if (e.Delta > 0) vm.ZoomIn(); else vm.ZoomOut();
        e.Handled = true;
    }
}
