//////////////////////////////////////////////
// Apache 2.0  - 2026
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using WpfHexaEditor.ViewModels;

namespace WpfHexaEditor.Views.Panels
{
    /// <summary>
    /// Panel for displaying parsed fields from format definitions
    /// Shows field names, values, offsets, and descriptions
    /// </summary>
    public partial class ParsedFieldsPanel : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<ParsedFieldViewModel> _parsedFields;
        private FormatInfo _formatInfo;

        public ParsedFieldsPanel()
        {
            InitializeComponent();
            DataContext = this;
            ParsedFields = new ObservableCollection<ParsedFieldViewModel>();
            FormatInfo = new FormatInfo();
        }

        /// <summary>
        /// Collection of parsed fields to display
        /// </summary>
        public ObservableCollection<ParsedFieldViewModel> ParsedFields
        {
            get => _parsedFields;
            set
            {
                _parsedFields = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Information about the detected format
        /// </summary>
        public FormatInfo FormatInfo
        {
            get => _formatInfo;
            set
            {
                _formatInfo = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Event fired when a field is selected
        /// </summary>
        public event EventHandler<ParsedFieldViewModel> FieldSelected;

        /// <summary>
        /// Event fired when the refresh button is clicked
        /// </summary>
        public event EventHandler RefreshRequested;

        /// <summary>
        /// Event fired when the formatter selection changes
        /// </summary>
        public event EventHandler<string> FormatterChanged;

        private void FieldsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FieldsListBox.SelectedItem is ParsedFieldViewModel field)
            {
                FieldSelected?.Invoke(this, field);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        private void FormatterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FormatterComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                FormatterChanged?.Invoke(this, tag);
            }
        }

        /// <summary>
        /// Scroll to and select a specific field
        /// </summary>
        public void SelectField(ParsedFieldViewModel field)
        {
            FieldsListBox.SelectedItem = field;
            FieldsListBox.ScrollIntoView(field);
        }

        /// <summary>
        /// Clear all fields
        /// </summary>
        public void Clear()
        {
            ParsedFields.Clear();
            FormatInfo = new FormatInfo();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Information about a detected format
    /// </summary>
    public class FormatInfo : INotifyPropertyChanged
    {
        private bool _isDetected;
        private string _name;
        private string _description;

        public bool IsDetected
        {
            get => _isDetected;
            set
            {
                _isDetected = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
