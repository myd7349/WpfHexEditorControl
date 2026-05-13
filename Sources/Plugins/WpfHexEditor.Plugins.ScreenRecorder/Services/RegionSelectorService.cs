// Project: WpfHexEditor.Plugins.ScreenRecorder
// File: Services/RegionSelectorService.cs
// Description: Shows the full-screen rubber-band selector and returns the chosen region.

using System.Windows;
using WpfHexEditor.Plugins.ScreenRecorder.Models;
using WpfHexEditor.Plugins.ScreenRecorder.Overlay;

namespace WpfHexEditor.Plugins.ScreenRecorder.Services;

public static class RegionSelectorService
{
    public static Task<CaptureRegion?> SelectRegionAsync() =>
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var win = new RegionSelectorWindow();
            win.ShowDialog();
            return win.SelectedRegion;
        }).Task;
}
