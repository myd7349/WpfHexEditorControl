using System.Windows;
using Microsoft.Win32;

namespace RiderSimpleExample;

/// <summary>
/// Simple WpfHexEditor example for JetBrains Rider users
///
/// This demonstrates:
/// - Opening files with HexEditor
/// - Handling events (SelectionChanged, DataCopied)
/// - Using properties (ReadOnlyMode, BytePerLine)
/// - Saving changes
///
/// 💡 Rider Tips:
/// - Press Ctrl+Space to see IntelliSense for HexEditor properties
/// - Use XAML Preview (View -> Tool Windows -> XAML Preview) to see design
/// - Import Live Templates from docs/IDE/WpfHexEditor.DotSettings for quick snippets
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Open a binary file in the hex editor
    /// </summary>
    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select a file to open in hex editor",
            Filter = "All Files (*.*)|*.*|Binary Files (*.bin;*.dat)|*.bin;*.dat|Executable Files (*.exe;*.dll)|*.exe;*.dll",
            CheckFileExists = true,
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                // Set the FileName property to open the file
                HexEditor.FileName = openFileDialog.FileName;

                StatusText.Text = $"📂 Opened: {System.IO.Path.GetFileName(openFileDialog.FileName)} ({HexEditor.Length} bytes)";
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                StatusText.Text = "❌ Error opening file";
            }
        }
    }

    /// <summary>
    /// Save changes to the file
    /// </summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(HexEditor.FileName))
        {
            MessageBox.Show("No file is currently open.",
                "No File",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            // Submit changes to save them
            HexEditor.SubmitChanges();

            StatusText.Text = $"💾 Saved changes to: {System.IO.Path.GetFileName(HexEditor.FileName)}";

            MessageBox.Show("File saved successfully!",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error saving file: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            StatusText.Text = "❌ Error saving file";
        }
    }

    /// <summary>
    /// Handle selection changes in the hex editor
    /// </summary>
    private void HexEditor_OnSelectionChanged(object sender, System.EventArgs e)
    {
        if (HexEditor.SelectionLength > 0)
        {
            SelectionText.Text = $"📍 Selection: {HexEditor.SelectionStart:N0} → {HexEditor.SelectionStop:N0} ({HexEditor.SelectionLength} bytes)";
        }
        else
        {
            SelectionText.Text = "No selection";
        }
    }

    /// <summary>
    /// Handle data copied event
    /// </summary>
    private void HexEditor_OnDataCopied(object sender, System.EventArgs e)
    {
        // Display current selection length as indication of copied data
        var length = HexEditor.SelectionLength > 0 ? HexEditor.SelectionLength : 0;
        StatusText.Text = length > 0
            ? $"📋 Copied {length} bytes to clipboard"
            : "📋 Data copied to clipboard";
    }
}
