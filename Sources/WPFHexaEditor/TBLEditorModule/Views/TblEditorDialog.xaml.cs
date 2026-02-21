//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using WpfHexaEditor.TBLEditorModule.ViewModels;

namespace WpfHexaEditor.TBLEditorModule.Views
{
    /// <summary>
    /// Interaction logic for TblEditorDialog.xaml
    /// </summary>
    public partial class TblEditorDialog : Window
    {
        public TblEditorViewModel ViewModel => DataContext as TblEditorViewModel;

        public TblEditorDialog()
        {
            InitializeComponent();

            // Setup keyboard shortcuts
            SetupKeyboardShortcuts();
        }

        private void SetupKeyboardShortcuts()
        {
            // Ctrl+S: Save
            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Save,
                (s, e) => ViewModel?.SaveCommand?.Execute(null),
                (s, e) => e.CanExecute = ViewModel?.SaveCommand?.CanExecute(null) ?? false
            ));

            // Ctrl+N: Add Entry
            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.New,
                (s, e) => ViewModel?.AddEntryCommand?.Execute(null),
                (s, e) => e.CanExecute = ViewModel?.AddEntryCommand?.CanExecute(null) ?? false
            ));

            // Delete: Delete Entry
            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Delete,
                (s, e) => ViewModel?.DeleteEntryCommand?.Execute(null),
                (s, e) => e.CanExecute = ViewModel?.DeleteEntryCommand?.CanExecute(null) ?? false
            ));

            // Ctrl+Z: Undo
            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Undo,
                (s, e) => ViewModel?.UndoCommand?.Execute(null),
                (s, e) => e.CanExecute = ViewModel?.UndoCommand?.CanExecute(null) ?? false
            ));

            // Ctrl+Y: Redo
            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Redo,
                (s, e) => ViewModel?.RedoCommand?.Execute(null),
                (s, e) => e.CanExecute = ViewModel?.RedoCommand?.CanExecute(null) ?? false
            ));
        }

        private void ApplyToEditor_Click(object sender, RoutedEventArgs e)
        {
            // Sync TBL changes back to HexEditor
            ViewModel?.SyncToTblStream();
            DialogResult = true;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Check for unsaved changes
            if (ViewModel?.IsDirty == true)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save before closing?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    ViewModel.SaveCommand?.Execute(null);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            // Cleanup
            ViewModel?.Dispose();

            base.OnClosing(e);
        }
    }
}
