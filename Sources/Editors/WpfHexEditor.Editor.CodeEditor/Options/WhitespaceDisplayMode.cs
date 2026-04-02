// ==========================================================
// Project: WpfHexEditor.Editor.CodeEditor
// File: Options/WhitespaceDisplayMode.cs
// Description:
//     Controls when whitespace markers (dots for spaces,
//     arrows for tabs) are rendered in the editor.
//
// Architecture Notes:
//     Used by CodeEditorOptions.WhitespaceMode and the
//     context menu "Show Whitespace" submenu.
// ==========================================================

namespace WpfHexEditor.Editor.CodeEditor.Options;

/// <summary>
/// Determines when whitespace characters are visually rendered.
/// </summary>
public enum WhitespaceDisplayMode
{
    /// <summary>Whitespace markers are never shown.</summary>
    None,

    /// <summary>Whitespace markers are shown only within selected text.</summary>
    Selection,

    /// <summary>Whitespace markers are always shown.</summary>
    Always
}
