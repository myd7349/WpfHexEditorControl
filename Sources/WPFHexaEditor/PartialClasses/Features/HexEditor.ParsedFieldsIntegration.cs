//////////////////////////////////////////////
// Apache 2.0  - 2026
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WpfHexaEditor.Core;
using WpfHexaEditor.Core.FormatDetection;
using WpfHexaEditor.Formatters;
using WpfHexaEditor.Interfaces;
using WpfHexaEditor.ViewModels;
using WpfHexaEditor.Views.Panels;

namespace WpfHexaEditor
{
    public partial class HexEditor
    {
        #region Dependency Properties

        /// <summary>
        /// Visibility of the parsed fields panel
        /// </summary>
        public static readonly DependencyProperty ParsedFieldsPanelVisibilityProperty =
            DependencyProperty.Register(
                nameof(ParsedFieldsPanelVisibility),
                typeof(Visibility),
                typeof(HexEditor),
                new PropertyMetadata(Visibility.Visible, OnParsedFieldsPanelVisibilityChanged));

        public Visibility ParsedFieldsPanelVisibility
        {
            get => (Visibility)GetValue(ParsedFieldsPanelVisibilityProperty);
            set => SetValue(ParsedFieldsPanelVisibilityProperty, value);
        }

        private static void OnParsedFieldsPanelVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor editor && e.NewValue is Visibility visibility)
            {
                editor.UpdateParsedFieldsPanelState(visibility);
            }
        }

        #endregion

        #region Fields

        private IFieldValueFormatter _currentFormatter;
        private readonly FieldValueReader _fieldValueReader = new FieldValueReader();
        private FormatDefinition _detectedFormat;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize parsed fields panel integration
        /// Called from HexEditor constructor
        /// </summary>
        private void InitializeParsedFieldsPanel()
        {
            // Set default formatter
            _currentFormatter = new HexValueFormatter();

            // Wire up events
            if (ParsedFieldsPanel != null)
            {
                ParsedFieldsPanel.FieldSelected += ParsedFieldsPanel_FieldSelected;
                ParsedFieldsPanel.RefreshRequested += ParsedFieldsPanel_RefreshRequested;
                ParsedFieldsPanel.FormatterChanged += ParsedFieldsPanel_FormatterChanged;
            }
        }

        private void UpdateParsedFieldsPanelState(Visibility visibility)
        {
            // Additional logic when panel visibility changes
            if (visibility == Visibility.Visible && ParsedFieldsPanel != null)
            {
                // Refresh parsed fields if file is open
                if (Stream != null && _detectedFormat != null)
                {
                    ParseFieldsAsync();
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle field selection in the parsed fields panel
        /// Sync with hex view by highlighting the corresponding bytes
        /// </summary>
        private void ParsedFieldsPanel_FieldSelected(object sender, ParsedFieldViewModel field)
        {
            if (field == null)
                return;

            try
            {
                // Set selection in hex editor to match the field
                // SetPosition automatically scrolls to make position visible
                SetPosition(field.Offset, 1);
                SelectionStart = field.Offset;
                SelectionStop = field.Offset + field.Length - 1;

                // Optionally highlight with a custom background block
                if (field.Color != null)
                {
                    // The CustomBackground system will handle visual highlighting
                    RefreshView();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error selecting field: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle refresh request from the parsed fields panel
        /// Re-parse all fields from the current file
        /// </summary>
        private void ParsedFieldsPanel_RefreshRequested(object sender, EventArgs e)
        {
            ParseFieldsAsync();
        }

        /// <summary>
        /// Handle formatter change from the parsed fields panel
        /// Re-format all field values with the new formatter
        /// </summary>
        private void ParsedFieldsPanel_FormatterChanged(object sender, string formatterType)
        {
            // Update current formatter based on selection
            _currentFormatter = formatterType switch
            {
                "hex" => new HexValueFormatter(),
                "decimal" => new DecimalValueFormatter(),
                "string" => new StringValueFormatter(),
                "mixed" => new HexValueFormatter(), // TODO: Implement mixed formatter
                _ => _currentFormatter
            };

            // Re-format all existing fields
            if (ParsedFieldsPanel?.ParsedFields != null)
            {
                foreach (var field in ParsedFieldsPanel.ParsedFields)
                {
                    FormatFieldValue(field);
                }
            }
        }

        #endregion

        #region Field Parsing

        /// <summary>
        /// Parse fields from the currently open file using the detected format
        /// </summary>
        private async void ParseFieldsAsync()
        {
            if (Stream == null || _detectedFormat == null || ParsedFieldsPanel == null)
                return;

            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    // Clear existing fields
                    ParsedFieldsPanel.ParsedFields.Clear();

                    // Update format info
                    ParsedFieldsPanel.FormatInfo.IsDetected = true;
                    ParsedFieldsPanel.FormatInfo.Name = _detectedFormat.FormatName;
                    ParsedFieldsPanel.FormatInfo.Description = _detectedFormat.Description;

                    // Parse all blocks from the format definition
                    if (_detectedFormat.Blocks != null)
                    {
                        ParseBlocks(_detectedFormat.Blocks, 0);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing fields: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively parse blocks and their nested children
        /// </summary>
        private void ParseBlocks(System.Collections.Generic.List<BlockDefinition> blocks, int depth)
        {
            if (blocks == null || depth > 10) // Prevent infinite recursion
                return;

            foreach (var block in blocks)
            {
                try
                {
                    // Calculate offset (handle variables and calculations)
                    long offset = ResolveOffset(block.Offset);
                    if (offset < 0 || offset >= Length)
                        continue;

                    // Calculate length (handle variables and calculations)
                    int length = ResolveLength(block.Length);
                    if (length <= 0 || offset + length > Length)
                        continue;

                    // Create field view model
                    var fieldVm = ParsedFieldViewModel.FromBlockDefinition(block, offset, length);

                    // Read and format value
                    ReadFieldValue(fieldVm);
                    FormatFieldValue(fieldVm);

                    // Add to panel
                    ParsedFieldsPanel.ParsedFields.Add(fieldVm);

                    // Handle conditional and loop blocks
                    if (block.Type == "conditional" && block.Then != null)
                    {
                        // TODO: Evaluate condition and parse Then/Else blocks
                    }
                    else if (block.Type == "loop" && block.Body != null)
                    {
                        // TODO: Parse loop body with iteration
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing block {block.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Resolve an offset value (handle int, var:name, calc:expression)
        /// </summary>
        private long ResolveOffset(object offsetValue)
        {
            return offsetValue switch
            {
                int intOffset => intOffset,
                long longOffset => longOffset,
                string strOffset when strOffset.StartsWith("var:") => 0, // TODO: Implement variable lookup
                string strOffset when strOffset.StartsWith("calc:") => 0, // TODO: Implement expression evaluation
                _ => 0
            };
        }

        /// <summary>
        /// Resolve a length value (handle int, var:name, calc:expression)
        /// </summary>
        private int ResolveLength(object lengthValue)
        {
            return lengthValue switch
            {
                int intLength => intLength,
                string strLength when strLength.StartsWith("var:") => 0, // TODO: Implement variable lookup
                string strLength when strLength.StartsWith("calc:") => 0, // TODO: Implement expression evaluation
                _ => 1
            };
        }

        /// <summary>
        /// Read the raw value from the file for a given field
        /// </summary>
        private void ReadFieldValue(ParsedFieldViewModel field)
        {
            if (Stream == null || field == null)
                return;

            try
            {
                // Read bytes from stream
                var buffer = new byte[field.Length];
                Stream.Position = field.Offset;
                int bytesRead = Stream.Read(buffer, 0, field.Length);

                if (bytesRead == field.Length)
                {
                    // Use FieldValueReader to parse the value
                    bool bigEndian = FieldValueReader.ShouldUseBigEndian(_detectedFormat?.FormatName);
                    field.RawValue = _fieldValueReader.ReadValue(buffer, 0, field.Length, field.ValueType, bigEndian);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading field value: {ex.Message}");
                field.IsValid = false;
                field.ValidationMessage = ex.Message;
            }
        }

        /// <summary>
        /// Format a field's raw value using the current formatter
        /// </summary>
        private void FormatFieldValue(ParsedFieldViewModel field)
        {
            if (field?.RawValue == null || _currentFormatter == null)
                return;

            try
            {
                if (_currentFormatter.Supports(field.ValueType))
                {
                    field.FormattedValue = _currentFormatter.Format(field.RawValue, field.ValueType, field.Length);
                }
                else
                {
                    // Fallback to hex formatter
                    var hexFormatter = new HexValueFormatter();
                    field.FormattedValue = hexFormatter.Format(field.RawValue, field.ValueType, field.Length);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error formatting field value: {ex.Message}");
                field.FormattedValue = "<format error>";
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Toggle the visibility of the parsed fields panel
        /// </summary>
        public void ToggleParsedFieldsPanel()
        {
            ParsedFieldsPanelVisibility = ParsedFieldsPanelVisibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        /// <summary>
        /// Refresh parsed fields (public API)
        /// </summary>
        public void RefreshParsedFields()
        {
            ParseFieldsAsync();
        }

        #endregion
    }
}
