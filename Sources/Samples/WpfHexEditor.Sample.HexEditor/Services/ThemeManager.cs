// Project: WpfHexEditor.Sample.HexEditor
// File: Services/ThemeManager.cs
// Description: Swaps application theme at runtime by replacing the top-level MergedDictionary entry.

using System;
using System.Collections.Generic;
using System.Windows;

namespace WpfHexEditor.Sample.HexEditor.Services
{
    public enum AppTheme { Dark, Light }

    public static class ThemeManager
    {
        private static readonly Uri DarkUri  = new("pack://application:,,,/Themes/Dark.xaml",  UriKind.Absolute);
        private static readonly Uri LightUri = new("pack://application:,,,/Themes/Light.xaml", UriKind.Absolute);

        public static AppTheme Current { get; private set; } = AppTheme.Dark;

        // Registered HexEditor instances — called after each theme swap
        private static readonly List<Action> _applyCallbacks = [];

        public static void RegisterHexEditor(Action applyTheme) =>
            _applyCallbacks.Add(applyTheme);

        public static void Apply(AppTheme theme)
        {
            var uri = theme == AppTheme.Dark ? DarkUri : LightUri;
            var dicts = Application.Current.Resources.MergedDictionaries;

            dicts[0] = new ResourceDictionary { Source = uri };
            Current = theme;

            foreach (var cb in _applyCallbacks)
                cb();
        }

        public static void Toggle() =>
            Apply(Current == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
    }
}
