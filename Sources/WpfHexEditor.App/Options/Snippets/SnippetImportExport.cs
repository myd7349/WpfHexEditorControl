// Project      : WpfHexEditor.App
// File         : Options/Snippets/SnippetImportExport.cs
// Description  : Import/export of user snippets via OpenFileDialog / SaveFileDialog.
// Architecture : Static helpers returning result tuples — no popups, no WPF state.

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using WpfHexEditor.Editor.CodeEditor.Properties;
using WpfHexEditor.Editor.CodeEditor.Snippets;

namespace WpfHexEditor.App.Options.Snippets;

public static class SnippetImportExport
{
    private const string Filter = "Snippet files (*.json)|*.json|All files (*.*)|*.*";

    /// <summary>
    /// Opens a file-pick dialog and deserializes snippets from the chosen file.
    /// Returns (true, list, null) on success, (false, [], error) on failure or cancel.
    /// </summary>
    public static (bool Ok, IReadOnlyList<StoredSnippet> Snippets, string? Error)
        TryImport(Window owner)
    {
        var dlg = new OpenFileDialog
        {
            Filter = Filter,
            Title  = CodeEditorResources.Snippets_Page_ImportTitle,
        };
        if (dlg.ShowDialog(owner) != true)
            return (false, [], null);

        return DeserializeFile(dlg.FileName);
    }

    /// <summary>
    /// Opens a save dialog and serializes <paramref name="snippets"/> to the chosen file.
    /// Returns (true, null) on success, (false, error) on failure or cancel.
    /// </summary>
    public static (bool Ok, string? Error) TryExport(
        IReadOnlyList<StoredSnippet> snippets, Window owner)
    {
        var dlg = new SaveFileDialog
        {
            Filter     = Filter,
            Title      = CodeEditorResources.Snippets_Page_ExportTitle,
            DefaultExt = ".json",
            FileName   = "snippets",
        };
        if (dlg.ShowDialog(owner) != true)
            return (false, null);

        return SerializeToFile(snippets, dlg.FileName);
    }

    private static (bool, IReadOnlyList<StoredSnippet>, string?) DeserializeFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<StoredSnippet>>(json, UserSnippetStore.JsonOptions);
            return list is null
                ? (false, [], "File contained no valid snippets.")
                : (true, list, null);
        }
        catch (Exception ex)
        {
            return (false, [], ex.Message);
        }
    }

    private static (bool, string?) SerializeToFile(IReadOnlyList<StoredSnippet> snippets, string path)
    {
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(snippets, UserSnippetStore.JsonOptions));
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
