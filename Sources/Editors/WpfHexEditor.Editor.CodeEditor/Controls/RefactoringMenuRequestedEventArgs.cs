// ==========================================================
// Project: WpfHexEditor.Editor.CodeEditor
// File: Controls/RefactoringMenuRequestedEventArgs.cs
// Description:
//     Event payload carrying the refactoring kind selected by the user in
//     the CodeEditor context menu (Refactor ▶ submenu). Host listens and
//     dispatches the right refactoring via the RefactoringEngine.
// ==========================================================

namespace WpfHexEditor.Editor.CodeEditor.Controls;

/// <summary>Refactoring identifier raised from the CodeEditor Refactor menu.</summary>
public sealed class RefactoringMenuRequestedEventArgs : EventArgs
{
    /// <summary>One of: "rename", "extract-method", "extract-class", "introduce-variable", "inline-method".</summary>
    public string Kind { get; }

    /// <summary>Document text snapshot when the menu fired.</summary>
    public string DocumentText { get; init; } = string.Empty;

    /// <summary>Absolute file path of the active document.</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>Caret offset (0-based).</summary>
    public int CaretOffset { get; init; }

    /// <summary>Selection start (0-based).</summary>
    public int SelectionStart { get; init; }

    /// <summary>Selection length (0 = no selection).</summary>
    public int SelectionLength { get; init; }

    public RefactoringMenuRequestedEventArgs(string kind) => Kind = kind;
}
