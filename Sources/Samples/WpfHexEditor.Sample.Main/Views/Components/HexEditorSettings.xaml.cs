//////////////////////////////////////////////
// Apache 2.0  - 2026
// HexEditor V2 - Complete Settings Panel
// All HexEditor properties exposed for testing
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

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

                // VERIFY: Check Tags immediately after panel generation
                System.Diagnostics.Debug.WriteLine("🔍 [VERIFY] Tags immediately after GenerateSettingsPanel:");
                VerifyBorderTags(panel);

                panel.DataContext = HexEditorControl;

                // VERIFY: Check Tags after DataContext set
                System.Diagnostics.Debug.WriteLine("🔍 [VERIFY] Tags after DataContext set:");
                VerifyBorderTags(panel);

                // Set as ScrollViewer content FIRST so visual tree is built
                SettingsScrollViewer.Content = panel;

                // VERIFY: Check Tags after adding to ScrollViewer
                System.Diagnostics.Debug.WriteLine("🔍 [VERIFY] Tags after adding to ScrollViewer (logical tree):");
                VerifyBorderTags(panel);

                // Force layout update to ensure visual tree is constructed
                SettingsScrollViewer.UpdateLayout();

                // VERIFY: Check Tags after UpdateLayout (logical tree - this is where they are!)
                System.Diagnostics.Debug.WriteLine("🔍 [VERIFY] Tags after UpdateLayout (logical tree - where they actually exist):");
                VerifyBorderTags(panel);

                // NOW replace ColorPicker placeholders with actual ColorPicker controls
                // IMPORTANT: Search the LOGICAL tree from panel, not visual tree!
                // Reason: Expander ControlTemplate creates new visual elements, losing our Tags
                ReplaceColorPickerPlaceholders(panel);

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
        /// Debug helper: Verify Border Tags in logical tree
        /// </summary>
        private void VerifyBorderTags(DependencyObject parent)
        {
            int count = 0;
            var borders = FindBordersInLogicalTree(parent);
            foreach (var border in borders)
            {
                var tag = border.Tag as string;
                if (tag != null && tag.StartsWith("ColorPicker:"))
                {
                    count++;
                    System.Diagnostics.Debug.WriteLine($"  ✓ Found Border with Tag='{tag}'");
                }
            }
            System.Diagnostics.Debug.WriteLine($"  Total ColorPicker Borders found in logical tree: {count}");
        }

        /// <summary>
        /// Debug helper: Verify Border Tags in visual tree
        /// </summary>
        private void VerifyBorderTagsVisualTree(DependencyObject parent)
        {
            int count = 0;
            var borders = FindBordersInVisualTree(parent);
            foreach (var border in borders)
            {
                var tag = border.Tag as string;
                if (tag != null && tag.StartsWith("ColorPicker:"))
                {
                    count++;
                    System.Diagnostics.Debug.WriteLine($"  ✓ Found Border with Tag='{tag}'");
                }
            }
            System.Diagnostics.Debug.WriteLine($"  Total ColorPicker Borders found in visual tree: {count}");
        }

        /// <summary>
        /// Find all Borders in logical tree
        /// </summary>
        private List<Border> FindBordersInLogicalTree(DependencyObject parent)
        {
            var result = new List<Border>();

            if (parent is Border border)
                result.Add(border);

            foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
            {
                result.AddRange(FindBordersInLogicalTree(child));
            }

            return result;
        }

        /// <summary>
        /// Find all Borders in visual tree
        /// </summary>
        private List<Border> FindBordersInVisualTree(DependencyObject parent)
        {
            var result = new List<Border>();

            if (parent is Border border)
                result.Add(border);

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                result.AddRange(FindBordersInVisualTree(child));
            }

            return result;
        }

        /// <summary>
        /// Replaces Border placeholders with actual ColorPicker controls.
        /// DynamicSettingsGenerator creates Border with Tag="ColorPicker:PropertyName" for color properties.
        /// IMPORTANT: Searches logical tree because Expander ControlTemplate creates separate visual tree.
        /// </summary>
        private void ReplaceColorPickerPlaceholders(DependencyObject root)
        {
            System.Diagnostics.Debug.WriteLine($"[ColorPicker] Starting logical tree search from {root.GetType().Name}");
            var placeholders = FindColorPickerPlaceholders(root);
            System.Diagnostics.Debug.WriteLine($"[ColorPicker] Found {placeholders.Count} placeholders");

            foreach (var (border, propertyName) in placeholders)
            {
                try
                {
                    // Get current color value from HexEditor
                    var propInfo = HexEditorControl.GetType().GetProperty(propertyName);
                    if (propInfo == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"  ✗ Property {propertyName} not found on HexEditor");
                        continue;
                    }

                    var currentValue = propInfo.GetValue(HexEditorControl);
                    if (!(currentValue is Color currentColor))
                    {
                        System.Diagnostics.Debug.WriteLine($"  ✗ Property {propertyName} is not a Color (is {currentValue?.GetType().Name ?? "null"})");
                        continue;
                    }

                    System.Diagnostics.Debug.WriteLine($"  [ColorPicker] {propertyName} current value: {currentColor}");

                    // Create ColorPicker with initial value
                    var colorPicker = new ColorPicker
                    {
                        Margin = border.Margin,
                        SelectedColor = currentColor // Set initial value BEFORE binding
                    };

                    // Bind SelectedColor to HexEditor property
                    // IMPORTANT: Use explicit Source to bypass ColorPicker's internal DataContext
                    var binding = new System.Windows.Data.Binding(propertyName)
                    {
                        Source = HexEditorControl, // Explicitly bind to HexEditor instead of inherited DataContext
                        Mode = System.Windows.Data.BindingMode.TwoWay,
                        UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                    };
                    colorPicker.SetBinding(ColorPicker.SelectedColorProperty, binding);

                    // Get the DependencyProperty field (e.g., "SelectionFirstColor" → "SelectionFirstColorProperty")
                    var dpFieldName = $"{propertyName}Property";
                    var dpField = HexEditorControl.GetType().GetField(dpFieldName,
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                    if (dpField == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"  ✗ DependencyProperty field {dpFieldName} not found");
                        continue;
                    }

                    var dependencyProperty = dpField.GetValue(null) as DependencyProperty;
                    if (dependencyProperty == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"  ✗ {dpFieldName} is not a DependencyProperty");
                        continue;
                    }

                    // Wire up ColorChanged event using proper DependencyProperty.SetValue()
                    // This triggers PropertyChangedCallbacks which update Resources and repaint
                    var dpForHandler = dependencyProperty; // Capture for closure
                    var propNameForHandler = propertyName;
                    colorPicker.ColorChanged += (s, e) =>
                    {
                        try
                        {
                            var newColor = ((ColorPicker)s).SelectedColor;
                            var oldColor = (Color)HexEditorControl.GetValue(dpForHandler);

                            System.Diagnostics.Debug.WriteLine($"\n[ColorChanged] {propNameForHandler}: {oldColor} → {newColor}");

                            // Use DependencyObject.SetValue() to trigger PropertyChangedCallbacks
                            HexEditorControl.SetValue(dpForHandler, newColor);

                            // Verify the value was actually set
                            var verifyColor = (Color)HexEditorControl.GetValue(dpForHandler);
                            System.Diagnostics.Debug.WriteLine($"[Verified] {propNameForHandler} = {verifyColor}");

                            // Check Resources that might have been updated
                            var resourceMap = new Dictionary<string, string>
                            {
                                { "SelectionFirstColor", "SelectionBrush" },
                                { "ByteModifiedColor", "ModifiedBrush" },
                                { "ByteDeletedColor", "DeletedBrush" },
                                { "ByteAddedColor", "AddedBrush" },
                                { "MouseOverColor", "ByteHoverBrush" },
                                { "ForegroundSecondColor", "AlternateByteForegroundBrush" },
                                { "ForegroundOffSetHeaderColor", "OffsetBrush" }
                            };

                            if (resourceMap.TryGetValue(propNameForHandler, out var resourceKey))
                            {
                                if (HexEditorControl.Resources.Contains(resourceKey))
                                {
                                    var brush = HexEditorControl.Resources[resourceKey] as SolidColorBrush;
                                    System.Diagnostics.Debug.WriteLine($"[Resource] {resourceKey} = {brush?.Color}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[Resource] {resourceKey} NOT FOUND in Resources");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ColorChanged ERROR] {ex.Message}\n{ex.StackTrace}");
                        }
                    };

                    // Replace Border with ColorPicker in parent
                    if (border.Parent is Panel parent)
                    {
                        int index = parent.Children.IndexOf(border);
                        parent.Children.RemoveAt(index);
                        parent.Children.Insert(index, colorPicker);
                        System.Diagnostics.Debug.WriteLine($"  ✓ Replaced ColorPicker for {propertyName} with color {currentColor}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"  ✗ Failed to replace ColorPicker for {propertyName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Recursively finds all Border elements tagged as ColorPicker placeholders in LOGICAL tree.
        /// We use logical tree because visual tree is created by ControlTemplate and loses our Tags.
        /// </summary>
        private List<(Border border, string propertyName)> FindColorPickerPlaceholders(DependencyObject parent)
        {
            var result = new List<(Border, string)>();

            // Check if this element is a Border with ColorPicker tag
            if (parent is Border border)
            {
                var tag = border.Tag as string;
                if (tag != null && tag.StartsWith("ColorPicker:"))
                {
                    string propertyName = tag.Substring("ColorPicker:".Length);
                    System.Diagnostics.Debug.WriteLine($"  [ColorPicker] ✓ Found placeholder: {propertyName}");
                    result.Add((border, propertyName));
                }
            }

            // Recurse through logical tree children
            foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
            {
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
