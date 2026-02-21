using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfHexaEditor;
using WpfHexaEditor.Core;
using WpfHexaEditor.Core.Settings;
using WpfHexaEditor.Models;
using WpfHexEditor.Sample.Main.Helpers;

namespace WpfHexEditor.Sample.Main.Views.Components
{
    /// <summary>
    /// Complete HexEditor Settings Panel - Auto-generated via Reflection
    /// Uses DynamicSettingsGenerator to create UI from [Category] attributes
    /// </summary>
    public partial class HexEditorSettings : UserControl
    {
        private global::WpfHexaEditor.HexEditor _hexEditorControl;
        private SettingsStateService _stateService;
        private DynamicSettingsGenerator _generator;

        /// <summary>
        /// Reference to the HexEditor control to configure
        /// </summary>
        public global::WpfHexaEditor.HexEditor HexEditorControl
        {
            get => _hexEditorControl;
            set
            {
                _hexEditorControl = value;

                // Generate UI if control is already loaded
                if (value != null && IsLoaded)
                {
                    System.Diagnostics.Debug.WriteLine($"[HexEditorSettings] Control already loaded, regenerating UI immediately");
                    RegenerateUI();
                }
                else if (value != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[HexEditorSettings] Control not yet loaded, will regenerate UI in Loaded event");
                }
            }
        }

        public HexEditorSettings()
        {
            InitializeComponent();

            // Initialize services
            _stateService = new SettingsStateService(typeof(global::WpfHexaEditor.HexEditor));
            _generator = new DynamicSettingsGenerator(typeof(global::WpfHexaEditor.HexEditor));

            // Generate UI when control is loaded
            Loaded += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("[HexEditorSettings] Loaded event fired");
                if (HexEditorControl != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[HexEditorSettings] HexEditorControl exists, calling RegenerateUI from Loaded event");
                    RegenerateUI();
                }
            };
        }

        /// <summary>
        /// Regenerates the entire settings panel UI using reflection-based discovery.
        /// Called when HexEditorControl is set or when Loaded event fires.
        /// </summary>
        private void RegenerateUI()
        {
            if (HexEditorControl == null)
            {
                System.Diagnostics.Debug.WriteLine("[HexEditorSettings] RegenerateUI called but HexEditorControl is null");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("[HexEditorSettings] Generating dynamic settings panel...");

                // Generate the complete UI
                var panel = _generator.GenerateSettingsPanel();
                panel.DataContext = HexEditorControl;

                // Set as ScrollViewer content FIRST so visual tree is built
                SettingsScrollViewer.Content = panel;

                // Force layout update to ensure visual tree is constructed
                SettingsScrollViewer.UpdateLayout();

                // NOW replace ColorPicker placeholders with actual ColorPicker controls
                // IMPORTANT: Search from ScrollViewer (visual tree root), not panel (logical tree)
                ReplaceColorPickerPlaceholders(SettingsScrollViewer);

                // Wire up button handlers
                WireUpButtonHandlers(panel);

                System.Diagnostics.Debug.WriteLine("[HexEditorSettings] Dynamic UI generation complete");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HexEditorSettings] Error generating UI: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Replaces Border placeholders with actual ColorPicker controls.
        /// DynamicSettingsGenerator creates Border with Tag="ColorPicker:PropertyName" for color properties.
        /// </summary>
        private void ReplaceColorPickerPlaceholders(DependencyObject root)
        {
            var placeholders = FindColorPickerPlaceholders(root);

            foreach (var (border, propertyName) in placeholders)
            {
                try
                {
                    // Create ColorPicker
                    var colorPicker = new ColorPicker
                    {
                        Margin = border.Margin
                    };

                    // Bind SelectedColor to HexEditor property
                    var binding = new System.Windows.Data.Binding(propertyName)
                    {
                        Mode = System.Windows.Data.BindingMode.TwoWay,
                        UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                    };
                    colorPicker.SetBinding(ColorPicker.SelectedColorProperty, binding);

                    // Replace Border with ColorPicker in parent
                    if (border.Parent is Panel parent)
                    {
                        int index = parent.Children.IndexOf(border);
                        parent.Children.RemoveAt(index);
                        parent.Children.Insert(index, colorPicker);
                        System.Diagnostics.Debug.WriteLine($"  Replaced ColorPicker placeholder for {propertyName}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"  Failed to replace ColorPicker for {propertyName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Recursively finds all Border elements tagged as ColorPicker placeholders.
        /// </summary>
        private List<(Border border, string propertyName)> FindColorPickerPlaceholders(DependencyObject parent)
        {
            var result = new List<(Border, string)>();

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Border border && border.Tag is string tag && tag.StartsWith("ColorPicker:"))
                {
                    string propertyName = tag.Substring("ColorPicker:".Length);
                    result.Add((border, propertyName));
                }

                // Recurse
                result.AddRange(FindColorPickerPlaceholders(child));
            }

            return result;
        }

        /// <summary>
        /// Wires up button click handlers for Save/Load/Reset buttons.
        /// </summary>
        private void WireUpButtonHandlers(Panel rootPanel)
        {
            // Find buttons by name
            var saveButton = FindElementByName<Button>(rootPanel, "SaveStateButton");
            var loadButton = FindElementByName<Button>(rootPanel, "LoadStateButton");
            var resetButton = FindElementByName<Button>(rootPanel, "ResetButton");

            if (saveButton != null)
            {
                saveButton.Click += SaveStateButton_Click;
                System.Diagnostics.Debug.WriteLine("  Wired SaveStateButton");
            }

            if (loadButton != null)
            {
                loadButton.Click += LoadStateButton_Click;
                System.Diagnostics.Debug.WriteLine("  Wired LoadStateButton");
            }

            if (resetButton != null)
            {
                resetButton.Click += ResetButton_Click;
                System.Diagnostics.Debug.WriteLine("  Wired ResetButton");
            }
        }

        /// <summary>
        /// Finds a control by name in the visual tree.
        /// </summary>
        private T FindElementByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T element && element.Name == name)
                    return element;

                var result = FindElementByName<T>(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }



        /// <summary>
        /// Auto-saves HexEditor settings silently (called from MainWindow.Closing)
        /// </summary>
        public void AutoSaveState()
        {
            if (HexEditorControl == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("[AutoSave] Saving settings on application close...");
                SaveStateButton_Click(null, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutoSave] Failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Auto-loads HexEditor settings silently (called from MainWindow.Loaded)
        /// </summary>
        public void AutoLoadState()
        {
            if (HexEditorControl == null) return;

            try
            {
                var json = Properties.Settings.Default.HexEditorSettings;
                if (!string.IsNullOrEmpty(json))
                {
                    System.Diagnostics.Debug.WriteLine($"[AutoLoad] Loading settings on application start... ({json.Length} chars)");
                    _stateService.LoadState(HexEditorControl, json);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AutoLoad] No saved settings found.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutoLoad] Failed: {ex.Message}");
            }
        }

        private void SaveStateButton_Click(object sender, RoutedEventArgs e)
        {
            if (HexEditorControl == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("[SaveState] Using automatic SaveState via SettingsStateService...");

                // Use automatic save via reflection
                var json = _stateService.SaveState(HexEditorControl);
                System.Diagnostics.Debug.WriteLine($"[SaveState] JSON length: {json.Length} chars");

                // Save to Properties.Settings
                Properties.Settings.Default.HexEditorSettings = json;
                Properties.Settings.Default.Save();

                System.Diagnostics.Debug.WriteLine("[SaveState] Settings saved successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveState] ERROR: {ex.Message}");
                MessageBox.Show(
                    $"Failed to save settings:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadStateButton_Click(object sender, RoutedEventArgs e)
        {
            if (HexEditorControl == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("[LoadState] Starting load...");

                // Load JSON from Properties.Settings
                var json = Properties.Settings.Default.HexEditorSettings;
                System.Diagnostics.Debug.WriteLine($"[LoadState] JSON length: {json?.Length ?? 0} chars");

                if (string.IsNullOrEmpty(json))
                {
                    MessageBox.Show(
                        "No saved settings found.\n\nClick 'Save State' first to save your configuration.",
                        "Info",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Use automatic load via reflection
                _stateService.LoadState(HexEditorControl, json);

                MessageBox.Show(
                    "HexEditor settings loaded successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load settings:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (HexEditorControl == null) return;

            var result = MessageBox.Show(
                "Reset all settings to defaults?\n\nThis will reset all properties to their DependencyProperty default values.",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Get all properties with [Category] attribute
                    var discoveryService = new PropertyDiscoveryService(typeof(global::WpfHexaEditor.HexEditor));
                    var properties = discoveryService.DiscoverProperties();

                    // Reset each property to its default value
                    foreach (var metadata in properties)
                    {
                        try
                        {
                            var propInfo = HexEditorControl.GetType().GetProperty(metadata.PropertyName);
                            if (propInfo != null && propInfo.CanWrite && metadata.DefaultValue != null)
                            {
                                propInfo.SetValue(HexEditorControl, metadata.DefaultValue);
                                System.Diagnostics.Debug.WriteLine($"  Reset {metadata.PropertyName} to {metadata.DefaultValue}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Failed to reset {metadata.PropertyName}: {ex.Message}");
                        }
                    }

                    MessageBox.Show(
                        "Settings reset to defaults",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to reset settings:\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
    }
}
