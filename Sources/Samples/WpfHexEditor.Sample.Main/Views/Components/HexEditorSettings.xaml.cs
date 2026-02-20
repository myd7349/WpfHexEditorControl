using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfHexaEditor;
using WpfHexaEditor.Core;
using WpfHexaEditor.Models;

namespace WpfHexEditor.Sample.Main.Views.Components
{
    /// <summary>
    /// Complete HexEditor Settings Panel
    /// Exposes all HexEditor properties for testing
    /// </summary>
    public partial class HexEditorSettings : UserControl
    {
        private global::WpfHexaEditor.HexEditor _hexEditorControl;

        /// <summary>
        /// Reference to the HexEditor control to configure
        /// </summary>
        public global::WpfHexaEditor.HexEditor HexEditorControl
        {
            get => _hexEditorControl;
            set
            {
                _hexEditorControl = value;

                // IMPORTANT: Set DataContext on the content (ScrollViewer), not on the UserControl itself
                // This preserves the UserControl's inherited DataContext for its own bindings (like Visibility)
                if (Content is FrameworkElement contentElement)
                {
                    contentElement.DataContext = value;
                    System.Diagnostics.Debug.WriteLine($"[HexEditorSettings] Set content DataContext. Content = {contentElement.GetType().Name}, HexEditor.ShowByteToolTip = {value?.ShowByteToolTip}");
                }

                // Recreate bindings if control is already loaded, otherwise wait for Loaded event
                if (value != null && IsLoaded)
                {
                    System.Diagnostics.Debug.WriteLine($"[HexEditorSettings] Control already loaded, calling RecreateBindings immediately");
                    RecreateBindings();
                }
                else if (value != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[HexEditorSettings] Control not yet loaded, will call RecreateBindings in Loaded event");
                }
            }
        }

        public HexEditorSettings()
        {
            InitializeComponent();

            // Update bindings when control is loaded
            Loaded += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("[HexEditorSettings] Loaded event fired");
                if (HexEditorControl != null && Content is FrameworkElement contentElement)
                {
                    System.Diagnostics.Debug.WriteLine($"[HexEditorSettings] HexEditorControl exists, Content DataContext = {contentElement.DataContext?.GetType().Name}");

                    // IMPORTANT: Call RecreateBindings here when visual tree is fully loaded
                    System.Diagnostics.Debug.WriteLine($"[HexEditorSettings] Calling RecreateBindings from Loaded event");
                    RecreateBindings();

                    // Also log current state after setup
                    System.Diagnostics.Debug.WriteLine($"[HexEditorSettings] After RecreateBindings: ShowByteToolTip={HexEditorControl.ShowByteToolTip}");
                }
            };

            // Monitor DataContext changes on content
            if (Content is FrameworkElement element)
            {
                element.DataContextChanged += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[HexEditorSettings.Content] DataContextChanged: Old={e.OldValue?.GetType().Name}, New={e.NewValue?.GetType().Name}");
                };
            }
        }

        private void RecreateBindings()
        {
            if (HexEditorControl == null || Content is not FrameworkElement contentRoot)
                return;

            System.Diagnostics.Debug.WriteLine("[HexEditorSettings] RecreateBindings called");

            // Find all CheckBox controls and set up two-way synchronization
            void SetupCheckBoxBindings(DependencyObject element)
            {
                if (element == null) return;

                if (element is CheckBox checkBox)
                {
                    // Get the binding to determine which property to sync
                    var binding = System.Windows.Data.BindingOperations.GetBinding(checkBox, CheckBox.IsCheckedProperty);
                    if (binding != null && binding.Path != null)
                    {
                        var propertyName = binding.Path.Path;
                        System.Diagnostics.Debug.WriteLine($"  Setting up CheckBox for property: {propertyName}");

                        // Set initial value from HexEditor
                        var property = HexEditorControl.GetType().GetProperty(propertyName);
                        if (property != null && property.PropertyType == typeof(bool))
                        {
                            checkBox.IsChecked = (bool)property.GetValue(HexEditorControl);

                            // Remove old handler if exists
                            checkBox.Checked -= CheckBox_Changed;
                            checkBox.Unchecked -= CheckBox_Changed;

                            // Add handler to update HexEditor when checkbox changes
                            checkBox.Checked += CheckBox_Changed;
                            checkBox.Unchecked += CheckBox_Changed;

                            // Store property name in Tag for the handler
                            checkBox.Tag = propertyName;
                        }
                    }
                }

                // Recurse through visual tree
                int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(element);
                for (int i = 0; i < childCount; i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(element, i);
                    SetupCheckBoxBindings(child);
                }
            }

            SetupCheckBoxBindings(contentRoot);

            // Initialize all ColorPickers with current values
            InitializeColorPickers();
        }

        private void InitializeColorPickers()
        {
            if (HexEditorControl == null) return;

            System.Diagnostics.Debug.WriteLine("[HexEditorSettings] InitializeColorPickers called");

            // Selection Colors
            SelectionFirstColorPicker.SelectedColor = HexEditorControl.SelectionFirstColor;
            SelectionSecondColorPicker.SelectedColor = HexEditorControl.SelectionSecondColor;

            // Byte State Colors
            ByteModifiedColorPicker.SelectedColor = HexEditorControl.ByteModifiedColor;
            ByteDeletedColorPicker.SelectedColor = HexEditorControl.ByteDeletedColor;
            ByteAddedColorPicker.SelectedColor = HexEditorControl.ByteAddedColor;

            // General Colors
            HighLightColorPicker.SelectedColor = HexEditorControl.HighLightColor;
            MouseOverColorPicker.SelectedColor = HexEditorControl.MouseOverColor;
            ForegroundSecondColorPicker.SelectedColor = HexEditorControl.ForegroundSecondColor;
            ForegroundContrastPicker.SelectedColor = HexEditorControl.ForegroundContrast;

            // TBL Colors
            TblDteColorPicker.SelectedColor = HexEditorControl.TblDteColor;
            TblMteColorPicker.SelectedColor = HexEditorControl.TblMteColor;
            TblEndBlockColorPicker.SelectedColor = HexEditorControl.TblEndBlockColor;
            TblEndLineColorPicker.SelectedColor = HexEditorControl.TblEndLineColor;
            TblDefaultColorPicker.SelectedColor = HexEditorControl.TblDefaultColor;

            // Bar Chart
            BarChartColorPicker.SelectedColor = HexEditorControl.BarChartColor;
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox || HexEditorControl == null)
                return;

            var propertyName = checkBox.Tag as string;
            if (string.IsNullOrEmpty(propertyName))
                return;

            var property = HexEditorControl.GetType().GetProperty(propertyName);
            if (property != null && property.PropertyType == typeof(bool))
            {
                var newValue = checkBox.IsChecked == true;
                property.SetValue(HexEditorControl, newValue);
                System.Diagnostics.Debug.WriteLine($"[HexEditorSettings] CheckBox changed: {propertyName} = {newValue}");
            }
        }

        private void UpdateBindings()
        {
            // Force all bindings to update from source (HexEditor properties)
            // This is needed because bindings were created before DataContext was set

            System.Diagnostics.Debug.WriteLine("[HexEditorSettings] UpdateBindings called");

            // Start from the content element, not the UserControl itself
            if (Content is not DependencyObject contentRoot)
            {
                System.Diagnostics.Debug.WriteLine("[HexEditorSettings] No content to update");
                return;
            }

            int bindingsUpdated = 0;

            // Helper method to recursively update all bindings in visual tree
            void UpdateBindingsRecursive(DependencyObject element)
            {
                if (element == null) return;

                // Get all locally set properties for this element
                var enumerator = element.GetLocalValueEnumerator();
                while (enumerator.MoveNext())
                {
                    var entry = enumerator.Current;
                    if (System.Windows.Data.BindingOperations.IsDataBound(element, entry.Property))
                    {
                        var bindingExpr = System.Windows.Data.BindingOperations.GetBindingExpression(element, entry.Property);
                        if (bindingExpr != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Updating binding on {element.GetType().Name}.{entry.Property.Name}");
                            bindingExpr.UpdateTarget();
                            bindingsUpdated++;
                        }
                    }
                }

                // Recurse through visual tree
                int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(element);
                for (int i = 0; i < childCount; i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(element, i);
                    UpdateBindingsRecursive(child);
                }
            }

            UpdateBindingsRecursive(contentRoot);
            System.Diagnostics.Debug.WriteLine($"[HexEditorSettings] Updated {bindingsUpdated} bindings");
        }

        private void BytesPerLineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HexEditorControl == null || BytesPerLineComboBox.SelectedItem == null) return;

            var selectedItem = (ComboBoxItem)BytesPerLineComboBox.SelectedItem;
            if (int.TryParse(selectedItem.Tag?.ToString(), out int bytesPerLine))
            {
                HexEditorControl.BytePerLine = bytesPerLine;
            }
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (HexEditorControl == null) return;

            // Apply zoom via ScaleTransform
            var scaleTransform = new ScaleTransform(e.NewValue, e.NewValue);
            HexEditorControl.LayoutTransform = scaleTransform;
        }

        private void EditModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HexEditorControl == null || EditModeComboBox.SelectedItem == null) return;

            var selectedItem = (ComboBoxItem)EditModeComboBox.SelectedItem;
            var editModeString = selectedItem.Tag?.ToString();

            if (Enum.TryParse<EditMode>(editModeString, out var editMode))
            {
                HexEditorControl.EditMode = editMode;
            }
        }

        private void CaretModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HexEditorControl == null || CaretModeComboBox.SelectedItem == null) return;

            var selectedItem = (ComboBoxItem)CaretModeComboBox.SelectedItem;
            var caretModeString = selectedItem.Tag?.ToString();

            if (Enum.TryParse<WpfHexaEditor.Core.CaretMode>(caretModeString, out var caretMode))
            {
                HexEditorControl.VisualCaretMode = caretMode;
            }
        }

        private void CopyModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HexEditorControl == null || CopyModeComboBox.SelectedItem == null) return;

            var selectedItem = (ComboBoxItem)CopyModeComboBox.SelectedItem;
            var copyModeString = selectedItem.Tag?.ToString();

            if (Enum.TryParse<CopyPasteMode>(copyModeString, out var copyMode))
            {
                HexEditorControl.DefaultCopyToClipboardMode = copyMode;
            }
        }

        private void ByteShiftLeftTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (HexEditorControl == null) return;

            if (long.TryParse(ByteShiftLeftTextBox.Text, out long byteShift))
            {
                HexEditorControl.ByteShiftLeft = byteShift;
            }
        }

        private void SaveStateButton_Click(object sender, RoutedEventArgs e)
        {
            if (HexEditorControl == null) return;

            try
            {
                // Create dictionary with all key properties from settings panel
                var settings = new Dictionary<string, object>
                {
                    // Display Settings
                    ["ShowByteToolTip"] = HexEditorControl.ShowByteToolTip,
                    ["ShowOffset"] = HexEditorControl.ShowOffset,
                    ["ShowAscii"] = HexEditorControl.ShowAscii,
                    ["ShowHeader"] = HexEditorControl.ShowHeader,
                    ["ShowColumnSeparator"] = HexEditorControl.ShowColumnSeparator,
                    ["ShowStatusMessage"] = HexEditorControl.ShowStatusMessage,
                    ["BytePerLine"] = HexEditorControl.BytePerLine,
                    ["ZoomLevel"] = HexEditorControl.LayoutTransform is ScaleTransform scale ? scale.ScaleX : 1.0,

                    // Editing Settings
                    ["HideByteDeleted"] = HexEditorControl.HideByteDeleted,
                    ["ReadOnlyMode"] = HexEditorControl.ReadOnlyMode,
                    ["CanInsertAnywhere"] = HexEditorControl.CanInsertAnywhere,
                    ["AllowDeleteByte"] = HexEditorControl.AllowDeleteByte,
                    ["AllowExtend"] = HexEditorControl.AllowExtend,
                    ["AppendNeedConfirmation"] = HexEditorControl.AppendNeedConfirmation,
                    ["EditMode"] = HexEditorControl.EditMode.ToString(),
                    ["VisualCaretMode"] = HexEditorControl.VisualCaretMode.ToString(),

                    // Clipboard & Drag/Drop
                    ["DefaultCopyToClipboardMode"] = HexEditorControl.DefaultCopyToClipboardMode.ToString(),
                    ["AllowFileDrop"] = HexEditorControl.AllowFileDrop,
                    ["FileDroppingConfirmation"] = HexEditorControl.FileDroppingConfirmation,
                    ["AllowTextDrop"] = HexEditorControl.AllowTextDrop,

                    // Selection & Highlighting
                    ["AllowAutoHighLightSelectionByte"] = HexEditorControl.AllowAutoHighLightSelectionByte,
                    ["AllowAutoSelectSameByteAtDoubleClick"] = HexEditorControl.AllowAutoSelectSameByteAtDoubleClick,
                    ["AllowMarkerClickNavigation"] = HexEditorControl.AllowMarkerClickNavigation,

                    // Status Bar
                    ["ShowFileSizeInStatusBar"] = HexEditorControl.ShowFileSizeInStatusBar,
                    ["AllowByteCount"] = HexEditorControl.AllowByteCount,

                    // Keyboard Shortcuts
                    ["AllowBuildinCtrla"] = HexEditorControl.AllowBuildinCtrla,
                    ["AllowBuildinCtrlc"] = HexEditorControl.AllowBuildinCtrlc,
                    ["AllowBuildinCtrlv"] = HexEditorControl.AllowBuildinCtrlv,
                    ["AllowBuildinCtrlz"] = HexEditorControl.AllowBuildinCtrlz,
                    ["AllowBuildinCtrly"] = HexEditorControl.AllowBuildinCtrly,

                    // Context Menu
                    ["AllowContextMenu"] = HexEditorControl.AllowContextMenu,

                    // Byte Spacer
                    ["ByteSpacerPositioning"] = HexEditorControl.ByteSpacerPositioning.ToString(),
                    ["ByteSpacerWidthTickness"] = HexEditorControl.ByteSpacerWidthTickness.ToString(),
                    ["ByteGrouping"] = HexEditorControl.ByteGrouping.ToString(),
                    ["ByteSpacerVisualStyle"] = HexEditorControl.ByteSpacerVisualStyle.ToString(),

                    // Advanced
                    ["ByteShiftLeft"] = HexEditorControl.ByteShiftLeft,

                    // Colors (as hex strings)
                    ["SelectionFirstColor"] = ColorToHex(HexEditorControl.SelectionFirstColor),
                    ["SelectionSecondColor"] = ColorToHex(HexEditorControl.SelectionSecondColor),
                    ["ByteModifiedColor"] = ColorToHex(HexEditorControl.ByteModifiedColor),
                    ["ByteDeletedColor"] = ColorToHex(HexEditorControl.ByteDeletedColor),
                    ["ByteAddedColor"] = ColorToHex(HexEditorControl.ByteAddedColor),
                    ["HighLightColor"] = ColorToHex(HexEditorControl.HighLightColor),
                    ["MouseOverColor"] = ColorToHex(HexEditorControl.MouseOverColor),
                    ["ForegroundSecondColor"] = ColorToHex(HexEditorControl.ForegroundSecondColor),
                    ["ForegroundContrast"] = ColorToHex(HexEditorControl.ForegroundContrast),
                    ["TblDteColor"] = ColorToHex(HexEditorControl.TblDteColor),
                    ["TblMteColor"] = ColorToHex(HexEditorControl.TblMteColor),
                    ["TblEndBlockColor"] = ColorToHex(HexEditorControl.TblEndBlockColor),
                    ["TblEndLineColor"] = ColorToHex(HexEditorControl.TblEndLineColor),
                    ["TblDefaultColor"] = ColorToHex(HexEditorControl.TblDefaultColor),
                    ["BarChartColor"] = ColorToHex(HexEditorControl.BarChartColor),
                    ["AutoHighLiteSelectionByteBrush"] = ColorToHex(HexEditorControl.AutoHighLiteSelectionByteBrush)
                };

                // Serialize to JSON
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

                // Save to Properties.Settings
                Properties.Settings.Default.HexEditorSettings = json;
                Properties.Settings.Default.Save();

                MessageBox.Show(
                    "HexEditor settings saved successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
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
                // Load JSON from Properties.Settings
                var json = Properties.Settings.Default.HexEditorSettings;

                if (string.IsNullOrEmpty(json))
                {
                    MessageBox.Show(
                        "No saved settings found.",
                        "Info",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Deserialize settings
                var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (settings == null) return;

                // Apply settings to HexEditor
                if (settings.TryGetValue("ShowByteToolTip", out var val)) HexEditorControl.ShowByteToolTip = val.GetBoolean();
                if (settings.TryGetValue("ShowOffset", out val)) HexEditorControl.ShowOffset = val.GetBoolean();
                if (settings.TryGetValue("ShowAscii", out val)) HexEditorControl.ShowAscii = val.GetBoolean();
                if (settings.TryGetValue("ShowHeader", out val)) HexEditorControl.ShowHeader = val.GetBoolean();
                if (settings.TryGetValue("ShowColumnSeparator", out val)) HexEditorControl.ShowColumnSeparator = val.GetBoolean();
                if (settings.TryGetValue("ShowStatusMessage", out val)) HexEditorControl.ShowStatusMessage = val.GetBoolean();
                if (settings.TryGetValue("BytePerLine", out val)) HexEditorControl.BytePerLine = val.GetInt32();

                // Apply zoom
                if (settings.TryGetValue("ZoomLevel", out val))
                {
                    var zoom = val.GetDouble();
                    HexEditorControl.LayoutTransform = new ScaleTransform(zoom, zoom);
                }

                // Editing Settings
                if (settings.TryGetValue("HideByteDeleted", out val)) HexEditorControl.HideByteDeleted = val.GetBoolean();
                if (settings.TryGetValue("ReadOnlyMode", out val)) HexEditorControl.ReadOnlyMode = val.GetBoolean();
                if (settings.TryGetValue("CanInsertAnywhere", out val)) HexEditorControl.CanInsertAnywhere = val.GetBoolean();
                if (settings.TryGetValue("AllowDeleteByte", out val)) HexEditorControl.AllowDeleteByte = val.GetBoolean();
                if (settings.TryGetValue("AllowExtend", out val)) HexEditorControl.AllowExtend = val.GetBoolean();
                if (settings.TryGetValue("AppendNeedConfirmation", out val)) HexEditorControl.AppendNeedConfirmation = val.GetBoolean();

                if (settings.TryGetValue("EditMode", out val) && Enum.TryParse<EditMode>(val.GetString(), out var editMode))
                    HexEditorControl.EditMode = editMode;
                if (settings.TryGetValue("VisualCaretMode", out val) && Enum.TryParse<WpfHexaEditor.Core.CaretMode>(val.GetString(), out var caretMode))
                    HexEditorControl.VisualCaretMode = caretMode;

                // Clipboard & Drag/Drop
                if (settings.TryGetValue("DefaultCopyToClipboardMode", out val) && Enum.TryParse<CopyPasteMode>(val.GetString(), out var copyMode))
                    HexEditorControl.DefaultCopyToClipboardMode = copyMode;
                if (settings.TryGetValue("AllowFileDrop", out val)) HexEditorControl.AllowFileDrop = val.GetBoolean();
                if (settings.TryGetValue("FileDroppingConfirmation", out val)) HexEditorControl.FileDroppingConfirmation = val.GetBoolean();
                if (settings.TryGetValue("AllowTextDrop", out val)) HexEditorControl.AllowTextDrop = val.GetBoolean();

                // Selection & Highlighting
                if (settings.TryGetValue("AllowAutoHighLightSelectionByte", out val)) HexEditorControl.AllowAutoHighLightSelectionByte = val.GetBoolean();
                if (settings.TryGetValue("AllowAutoSelectSameByteAtDoubleClick", out val)) HexEditorControl.AllowAutoSelectSameByteAtDoubleClick = val.GetBoolean();
                if (settings.TryGetValue("AllowMarkerClickNavigation", out val)) HexEditorControl.AllowMarkerClickNavigation = val.GetBoolean();

                // Status Bar
                if (settings.TryGetValue("ShowFileSizeInStatusBar", out val)) HexEditorControl.ShowFileSizeInStatusBar = val.GetBoolean();
                if (settings.TryGetValue("AllowByteCount", out val)) HexEditorControl.AllowByteCount = val.GetBoolean();

                // Keyboard Shortcuts
                if (settings.TryGetValue("AllowBuildinCtrla", out val)) HexEditorControl.AllowBuildinCtrla = val.GetBoolean();
                if (settings.TryGetValue("AllowBuildinCtrlc", out val)) HexEditorControl.AllowBuildinCtrlc = val.GetBoolean();
                if (settings.TryGetValue("AllowBuildinCtrlv", out val)) HexEditorControl.AllowBuildinCtrlv = val.GetBoolean();
                if (settings.TryGetValue("AllowBuildinCtrlz", out val)) HexEditorControl.AllowBuildinCtrlz = val.GetBoolean();
                if (settings.TryGetValue("AllowBuildinCtrly", out val)) HexEditorControl.AllowBuildinCtrly = val.GetBoolean();

                // Context Menu
                if (settings.TryGetValue("AllowContextMenu", out val)) HexEditorControl.AllowContextMenu = val.GetBoolean();

                // Byte Spacer
                if (settings.TryGetValue("ByteSpacerPositioning", out val) && Enum.TryParse<ByteSpacerPosition>(val.GetString(), out var spacerPos))
                    HexEditorControl.ByteSpacerPositioning = spacerPos;
                if (settings.TryGetValue("ByteSpacerWidthTickness", out val) && Enum.TryParse<ByteSpacerWidth>(val.GetString(), out var spacerWidth))
                    HexEditorControl.ByteSpacerWidthTickness = spacerWidth;
                if (settings.TryGetValue("ByteGrouping", out val) && Enum.TryParse<ByteSpacerGroup>(val.GetString(), out var spacerGroup))
                    HexEditorControl.ByteGrouping = spacerGroup;
                if (settings.TryGetValue("ByteSpacerVisualStyle", out val) && Enum.TryParse<ByteSpacerVisual>(val.GetString(), out var spacerVisual))
                    HexEditorControl.ByteSpacerVisualStyle = spacerVisual;

                // Advanced
                if (settings.TryGetValue("ByteShiftLeft", out val)) HexEditorControl.ByteShiftLeft = val.GetInt64();

                // Colors
                if (settings.TryGetValue("SelectionFirstColor", out val)) HexEditorControl.SelectionFirstColor = HexToColor(val.GetString());
                if (settings.TryGetValue("SelectionSecondColor", out val)) HexEditorControl.SelectionSecondColor = HexToColor(val.GetString());
                if (settings.TryGetValue("ByteModifiedColor", out val)) HexEditorControl.ByteModifiedColor = HexToColor(val.GetString());
                if (settings.TryGetValue("ByteDeletedColor", out val)) HexEditorControl.ByteDeletedColor = HexToColor(val.GetString());
                if (settings.TryGetValue("ByteAddedColor", out val)) HexEditorControl.ByteAddedColor = HexToColor(val.GetString());
                if (settings.TryGetValue("HighLightColor", out val)) HexEditorControl.HighLightColor = HexToColor(val.GetString());
                if (settings.TryGetValue("MouseOverColor", out val)) HexEditorControl.MouseOverColor = HexToColor(val.GetString());
                if (settings.TryGetValue("ForegroundSecondColor", out val)) HexEditorControl.ForegroundSecondColor = HexToColor(val.GetString());
                if (settings.TryGetValue("ForegroundContrast", out val)) HexEditorControl.ForegroundContrast = HexToColor(val.GetString());
                if (settings.TryGetValue("TblDteColor", out val)) HexEditorControl.TblDteColor = HexToColor(val.GetString());
                if (settings.TryGetValue("TblMteColor", out val)) HexEditorControl.TblMteColor = HexToColor(val.GetString());
                if (settings.TryGetValue("TblEndBlockColor", out val)) HexEditorControl.TblEndBlockColor = HexToColor(val.GetString());
                if (settings.TryGetValue("TblEndLineColor", out val)) HexEditorControl.TblEndLineColor = HexToColor(val.GetString());
                if (settings.TryGetValue("TblDefaultColor", out val)) HexEditorControl.TblDefaultColor = HexToColor(val.GetString());
                if (settings.TryGetValue("BarChartColor", out val)) HexEditorControl.BarChartColor = HexToColor(val.GetString());
                if (settings.TryGetValue("AutoHighLiteSelectionByteBrush", out val)) HexEditorControl.AutoHighLiteSelectionByteBrush = HexToColor(val.GetString());

                // Refresh ColorPickers with new values
                InitializeColorPickers();

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

        private static string ColorToHex(Color color)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static Color HexToColor(string hex)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                return Colors.Black;
            }
        }

        private void AutoHighlightColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.AutoHighLiteSelectionByteBrush = e;
        }

        // Selection Colors
        private void SelectionFirstColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.SelectionFirstColor = e;
        }

        private void SelectionSecondColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.SelectionSecondColor = e;
        }

        // Byte State Colors
        private void ByteModifiedColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.ByteModifiedColor = e;
        }

        private void ByteDeletedColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.ByteDeletedColor = e;
        }

        private void ByteAddedColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.ByteAddedColor = e;
        }

        // General Colors
        private void HighLightColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.HighLightColor = e;
        }

        private void MouseOverColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.MouseOverColor = e;
        }

        private void ForegroundSecondColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.ForegroundSecondColor = e;
        }

        private void ForegroundContrastPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.ForegroundContrast = e;
        }

        // TBL Colors
        private void TblDteColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.TblDteColor = e;
        }

        private void TblMteColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.TblMteColor = e;
        }

        private void TblEndBlockColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.TblEndBlockColor = e;
        }

        private void TblEndLineColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.TblEndLineColor = e;
        }

        private void TblDefaultColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.TblDefaultColor = e;
        }

        // Bar Chart
        private void BarChartColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;
            HexEditorControl.BarChartColor = e;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (HexEditorControl == null) return;

            var result = MessageBox.Show(
                "Reset all settings to defaults?",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Reset to defaults
                HexEditorControl.ShowByteToolTip = false;
                HexEditorControl.HideByteDeleted = false;
                HexEditorControl.BytePerLine = 16;
                HexEditorControl.ReadOnlyMode = false;
                HexEditorControl.CanInsertAnywhere = false;
                HexEditorControl.AllowDeleteByte = true;
                HexEditorControl.AllowExtend = true;
                HexEditorControl.AppendNeedConfirmation = false;
                HexEditorControl.EditMode = EditMode.Overwrite;
                HexEditorControl.VisualCaretMode = WpfHexaEditor.Core.CaretMode.Overwrite;
                HexEditorControl.DefaultCopyToClipboardMode = CopyPasteMode.HexaString;
                HexEditorControl.AllowFileDrop = true;
                HexEditorControl.FileDroppingConfirmation = false;
                HexEditorControl.AllowTextDrop = true;
                HexEditorControl.AllowAutoHighLightSelectionByte = true;
                HexEditorControl.AllowAutoSelectSameByteAtDoubleClick = true;
                HexEditorControl.AllowMarkerClickNavigation = true;
                HexEditorControl.AllowByteCount = true;
                HexEditorControl.ByteShiftLeft = 0;

                // Reset UI controls
                BytesPerLineComboBox.SelectedIndex = 1; // 16 bytes
                ZoomSlider.Value = 1.0;
                EditModeComboBox.SelectedIndex = 0;
                CaretModeComboBox.SelectedIndex = 0;
                CopyModeComboBox.SelectedIndex = 0;
                ByteShiftLeftTextBox.Text = "0";
                AutoHighlightColorPicker.SelectedColor = Color.FromRgb(0x40, 0x40, 0xFF);

                MessageBox.Show(
                    "Settings reset to defaults",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}
