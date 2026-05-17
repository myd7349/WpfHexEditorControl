// ==========================================================
// Project: WpfHexEditor.Plugins.ScreenRecorder
// File: Services/ScreenRecorderLocalizedDictionary.cs
// Description: WPF ResourceDictionary exposing all ScreenRecorder localized strings
//              as dynamic resources, updated automatically on culture change.
//              Wire into root XAML via UserControl.Resources/MergedDictionaries.
// ==========================================================

using WpfHexEditor.Core.Localization.Services;
using WpfHexEditor.Plugins.ScreenRecorder.Properties;

namespace WpfHexEditor.Plugins.ScreenRecorder.Services;

public sealed class ScreenRecorderLocalizedDictionary : LocalizedResourceDictionary
{
    public ScreenRecorderLocalizedDictionary()
    {
        RegisterResourceManager(ScreenRecorderResources.ResourceManager);
        LoadResources();
    }
}
