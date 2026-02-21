using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace WpfHexaEditor.TBLEditorModule.Views
{
    /// <summary>
    /// Simple input dialog for getting text input from user
    /// </summary>
    public partial class InputDialog : Window, INotifyPropertyChanged
    {
        private string _title;
        private string _message;
        private string _inputValue;

        public InputDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        public InputDialog(string title, string message, string defaultValue = "") : this()
        {
            Title = title;
            Message = message;
            InputValue = defaultValue;
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public string InputValue
        {
            get => _inputValue;
            set
            {
                _inputValue = value;
                OnPropertyChanged();
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnContentRendered(System.EventArgs e)
        {
            base.OnContentRendered(e);
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Show input dialog and get user input
        /// </summary>
        public static string Show(string message, string title, string defaultValue = "", Window owner = null)
        {
            var dialog = new InputDialog(title, message, defaultValue)
            {
                Owner = owner
            };

            return dialog.ShowDialog() == true ? dialog.InputValue : null;
        }
    }
}
