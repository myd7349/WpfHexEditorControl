//////////////////////////////////////////////
// Apache 2.0  2026
// HexEditor V2 - Modern Main Window
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Ribbon;
using WpfHexEditor.Sample.Main.ViewModels;

namespace WpfHexEditor.Sample.Main.Views
{
    /// <summary>
    /// Modern Main Window - The ultimate 2026 hex editor experience
    /// </summary>
    public partial class ModernMainWindow : RibbonWindow
    {
        private readonly ModernMainWindowViewModel _viewModel;

        public ModernMainWindow()
        {
            InitializeComponent();

            // Initialize ViewModel
            _viewModel = new ModernMainWindowViewModel();
            DataContext = _viewModel;

            // Wire up components
            _viewModel.SetHexEditor(HexEditorControl);

            // Connect HexEditor to Settings Panel for property bindings
            HexEditorSettingsPanel.HexEditorControl = HexEditorControl;

            // Wire up file operations
            _viewModel.FileOpenRequested += OnFileOpenRequested;
            _viewModel.FileSaveRequested += OnFileSaveRequested;

            // Wire up theme changes
            _viewModel.SettingsViewModel.ThemeChanged += OnThemeChanged;

            // Load default theme
            LoadTheme(_viewModel.SettingsViewModel.SelectedTheme);
        }

        private void OnFileOpenRequested(object sender, string filePath)
        {
            try
            {
                HexEditorControl.FileName = filePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open file: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnFileSaveRequested(object sender, EventArgs e)
        {
            try
            {
                HexEditorControl.SubmitChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save file: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnThemeChanged(object sender, string themeName)
        {
            LoadTheme(themeName);
        }

        private void LoadTheme(string themeName)
        {
            try
            {
                // Clear existing theme
                var existingTheme = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/Themes/"));

                if (existingTheme != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(existingTheme);
                }

                // Load new theme
                var themeUri = new Uri($"pack://application:,,,/WpfHexEditor.Sample.Main;component/Resources/Themes/{themeName}.xaml", UriKind.Absolute);
                var themeDictionary = new ResourceDictionary { Source = themeUri };
                Application.Current.Resources.MergedDictionaries.Add(themeDictionary);

                // Force refresh of all bindings
                InvalidateVisual();
                UpdateLayout();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load theme {themeName}: {ex.Message}");
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ThemeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Ribbon.RibbonMenuItem menuItem &&
                menuItem.Tag is string themeName)
            {
                LoadTheme(themeName);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Cleanup
            HexEditorControl?.Close();
            base.OnClosed(e);
        }
    }
}
