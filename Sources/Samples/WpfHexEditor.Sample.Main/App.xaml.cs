using System.Windows;
using WpfHexEditor.Sample.Main.Services;

namespace WpfHexEditor.Sample.Main
{
    public partial class App : Application
    {
        public App()
        {
            // CRITICAL: Migrate settings BEFORE any initialization
            MigrateSettings();

            // CRITICAL: Initialize culture BEFORE any WPF initialization
            // DynamicResourceManager handles culture loading from settings and application-wide culture management
            DynamicResourceManager.Initialize();

            System.Diagnostics.Debug.WriteLine($"[App.Constructor] DynamicResourceManager initialized with culture: {DynamicResourceManager.CurrentCulture.Name}");

            // Initialize theme system AFTER culture
            // ThemeManager handles theme loading from settings and application-wide theme management
            ThemeManager.Initialize();

            System.Diagnostics.Debug.WriteLine($"[App.Constructor] ThemeManager initialized with theme: {ThemeManager.CurrentTheme}");
        }

        private void MigrateSettings()
        {
            var settings = Properties.Settings.Default;

            // Check if this is first run or needs migration
            if (settings.SettingsVersion < 2)
            {
                System.Diagnostics.Debug.WriteLine($"[Migration] Migrating from version {settings.SettingsVersion} to version 2");

                // V1 -> V2 Migration
                // BytesPerLine and ShowStatusBar were removed from SettingsPanelViewModel
                // but were never persisted, so no migration needed

                // Mark as migrated
                settings.SettingsVersion = 2;
                settings.Save();

                System.Diagnostics.Debug.WriteLine("[Migration] Migration complete");
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[App.OnStartup] Current UI Culture: {DynamicResourceManager.CurrentCulture.Name}");
            base.OnStartup(e);
        }
    }
}
