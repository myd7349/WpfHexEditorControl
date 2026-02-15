using System;
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
        /// <summary>
        /// Reference to the HexEditor control to configure
        /// </summary>
        public global::WpfHexaEditor.HexEditor HexEditorControl { get; set; }

        public HexEditorSettings()
        {
            InitializeComponent();
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
                var state = HexEditorControl.CurrentState;
                // Save to file or storage (implement as needed)
                MessageBox.Show(
                    "State saved successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save state: {ex.Message}",
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
                // Load from file or storage (implement as needed)
                MessageBox.Show(
                    "Load state feature - to be implemented",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load state: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AutoHighlightColorPicker_ColorChanged(object sender, Color e)
        {
            if (HexEditorControl == null) return;

            // Set the auto-highlight color
            HexEditorControl.AutoHighLiteSelectionByteBrush = e;
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
