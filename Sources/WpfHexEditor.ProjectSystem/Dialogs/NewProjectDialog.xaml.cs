//////////////////////////////////////////////
// Apache 2.0  - 2026
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace WpfHexEditor.ProjectSystem.Dialogs;

/// <summary>
/// Dialog for creating a new project (.whproj) inside the active solution.
/// After ShowDialog() returns true, read <see cref="ProjectName"/> and <see cref="ProjectDirectory"/>.
/// </summary>
public partial class NewProjectDialog : Window
{
    public string ProjectName      { get; private set; } = "";
    public string ProjectDirectory { get; private set; } = "";

    /// <param name="suggestedDirectory">Pre-filled location (e.g. the solution directory).</param>
    public NewProjectDialog(string? suggestedDirectory = null)
    {
        InitializeComponent();
        LocationBox.Text = suggestedDirectory
                           ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        NameBox.Focus();
    }

    private void OnNameOrLocationChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => Refresh();

    private void Refresh()
    {
        var name  = NameBox.Text.Trim();
        var loc   = LocationBox.Text.Trim();
        var valid = name.Length > 0 && loc.Length > 0
                    && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

        OkButton.IsEnabled = valid;
        PreviewText.Text   = valid
            ? Path.Combine(loc, name, name + ".whproj")
            : string.Empty;
    }

    private void OnBrowseLocation(object sender, RoutedEventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description  = "Select parent folder for the new project",
            SelectedPath = LocationBox.Text,
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            LocationBox.Text = dlg.SelectedPath;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        ProjectName      = NameBox.Text.Trim();
        ProjectDirectory = LocationBox.Text.Trim();
        DialogResult     = true;
    }
}
