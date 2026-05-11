// ==========================================================
// Project: WpfHexEditor.App
// File: Refactoring/RefactoringOrchestrator.cs
// Description:
//     Bridges the CodeEditor Refactor menu to the Core.LSP refactoring
//     engine. Builds the RefactoringContext from the active editor, picks
//     the right IRefactoring by kind, shows the preview dialog, and
//     applies the edits if the user confirms.
// ==========================================================

using System.IO;
using System.Windows;
using WpfHexEditor.Core.LSP.Refactoring;
using WpfHexEditor.Editor.CodeEditor.Controls;

namespace WpfHexEditor.App.Refactoring;

/// <summary>Builds and applies refactorings invoked from the CodeEditor.</summary>
public sealed class RefactoringOrchestrator
{
    private readonly RefactoringEngine _engine = new(
    [
        new RenameRefactoring(),
        new ExtractMethodRefactoring(),
        new ExtractClassRefactoring(),
        new IntroduceVariableRefactoring(),
        new InlineMethodRefactoring(),
    ]);

    /// <summary>Subscribes the orchestrator to the CodeEditor's Refactor menu events.</summary>
    public void Attach(WpfHexEditor.Editor.CodeEditor.Controls.CodeEditor editor)
    {
        editor.RefactoringMenuRequested += (s, e) => Handle(editor, e);
    }

    private void Handle(WpfHexEditor.Editor.CodeEditor.Controls.CodeEditor editor, RefactoringMenuRequestedEventArgs e)
    {
        var context = new RefactoringContext
        {
            DocumentText    = e.DocumentText,
            FilePath        = e.FilePath,
            CaretOffset     = e.CaretOffset,
            SelectionStart  = e.SelectionStart > 0 ? e.SelectionStart : e.CaretOffset,
            SelectionLength = e.SelectionLength,
        };

        IRefactoring? refactoring = e.Kind switch
        {
            "rename"             => new RenameRefactoring { NewName = ExtractWordAtCaret(context) + "_renamed" },
            "extract-method"     => new ExtractMethodRefactoring(),
            "extract-class"      => new ExtractClassRefactoring(),
            "introduce-variable" => new IntroduceVariableRefactoring(),
            "inline-method"      => new InlineMethodRefactoring(),
            _                    => null,
        };
        if (refactoring is null || !refactoring.CanApply(context)) return;

        var edits = refactoring.Apply(context);
        if (edits.Count == 0) return;

        var rows = RefactoringPreviewBuilder.Build(edits);
        var dlg  = new RefactoringPreviewDialog(rows)
        {
            Owner = Window.GetWindow(editor),
        };
        if (dlg.ShowDialog() != true) return;

        ApplyEdits(edits, editor, e);
    }

    private static string ExtractWordAtCaret(RefactoringContext context)
    {
        var t = context.DocumentText;
        int s = context.CaretOffset;
        if (s >= t.Length) s = t.Length - 1;
        if (s < 0) return "";
        while (s > 0 && (char.IsLetterOrDigit(t[s - 1]) || t[s - 1] == '_')) s--;
        int e = s;
        while (e < t.Length && (char.IsLetterOrDigit(t[e]) || t[e] == '_')) e++;
        return e > s ? t[s..e] : "";
    }

    private static void ApplyEdits(IReadOnlyList<TextEdit> edits,
                                   WpfHexEditor.Editor.CodeEditor.Controls.CodeEditor editor,
                                   RefactoringMenuRequestedEventArgs activeArgs)
    {
        // Group by file and apply in reverse offset order to preserve indices.
        foreach (var group in edits.GroupBy(e => e.FilePath, StringComparer.OrdinalIgnoreCase))
        {
            var ordered = group.OrderByDescending(e => e.StartOffset).ToList();

            // Active editor's file: rewrite via the file on disk too — the
            // CodeEditor's internal model will reload through its file-watcher.
            if (string.Equals(group.Key, activeArgs.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                var sb = new System.Text.StringBuilder(activeArgs.DocumentText);
                foreach (var ed in ordered)
                {
                    int start = Math.Clamp(ed.StartOffset, 0, sb.Length);
                    int len   = Math.Clamp(ed.Length, 0, sb.Length - start);
                    sb.Remove(start, len);
                    sb.Insert(start, ed.NewText);
                }
                if (File.Exists(activeArgs.FilePath))
                {
                    try { File.WriteAllText(activeArgs.FilePath, sb.ToString()); } catch { }
                }
            }
            else if (File.Exists(group.Key))
            {
                try
                {
                    var disk = File.ReadAllText(group.Key);
                    var sb   = new System.Text.StringBuilder(disk);
                    foreach (var ed in ordered)
                    {
                        int start = Math.Clamp(ed.StartOffset, 0, sb.Length);
                        int len   = Math.Clamp(ed.Length, 0, sb.Length - start);
                        sb.Remove(start, len);
                        sb.Insert(start, ed.NewText);
                    }
                    File.WriteAllText(group.Key, sb.ToString());
                }
                catch { /* silent — preview already validated */ }
            }
        }
    }
}
