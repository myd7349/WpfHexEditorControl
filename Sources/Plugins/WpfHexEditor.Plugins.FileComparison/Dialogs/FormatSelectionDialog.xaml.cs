// Project      : WpfHexEditorControl
// File         : Dialogs/FormatSelectionDialog.xaml.cs
// Description  : Dialog for manual format selection when auto-detection returns multiple candidates.
// Architecture : Plugin dialog — no SDK dependency, references WpfHexEditor.Core.FormatDetection only.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5, Claude Sonnet 4.6

using System.Collections.Generic;
using System.Windows;
using WpfHexEditor.Core.FormatDetection;

namespace WpfHexEditor.Plugins.FileComparison.Dialogs;

/// <summary>
/// Dialog for manual format selection when auto-detection is ambiguous.
/// </summary>
public partial class FormatSelectionDialog : WpfHexEditor.Editor.Core.Views.ThemedDialog
{
    /// <summary>The format candidate selected by the user.</summary>
    public FormatMatchCandidate? SelectedCandidate { get; private set; }

    /// <summary>List of candidates to display.</summary>
    public List<FormatMatchCandidate>? Candidates
    {
        get => CandidatesListView.ItemsSource as List<FormatMatchCandidate>;
        set
        {
            CandidatesListView.ItemsSource = value;
            if (value is { Count: > 0 })
                CandidatesListView.SelectedIndex = 0;
        }
    }

    /// <summary>Custom message to display in the header.</summary>
    public string Message
    {
        get => MessageText.Text;
        set => MessageText.Text = value;
    }

    public FormatSelectionDialog()
    {
        InitializeComponent();
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedCandidate = CandidatesListView.SelectedItem as FormatMatchCandidate;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedCandidate = null;
        DialogResult = false;
        Close();
    }
}
