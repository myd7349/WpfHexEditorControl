//////////////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Project: WpfHexEditor.Editor.StructureEditor
// File: Services/UndoRedoService.cs
// Description: Memento-based undo/redo service storing serialized JSON snapshots.
//////////////////////////////////////////////////////

namespace WpfHexEditor.Editor.StructureEditor.Services;

/// <summary>
/// Snapshot-based undo/redo service for the Structure Editor.
/// Stores serialized JSON strings representing the full editor state.
/// </summary>
internal sealed class UndoRedoService
{
    private const int MaxDepth = 50;

    private readonly List<string> _undoStack = [];
    private readonly List<string> _redoStack = [];

    /// <summary>Raised when <see cref="CanUndo"/> or <see cref="CanRedo"/> changes.</summary>
    internal event EventHandler? StateChanged;

    internal bool CanUndo => _undoStack.Count > 0;
    internal bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Pushes the current state onto the undo stack and clears the redo stack.
    /// Skips if <paramref name="json"/> is identical to the top of the undo stack.
    /// </summary>
    internal void PushState(string json)
    {
        if (_undoStack.Count > 0 && _undoStack[^1] == json)
            return;

        _undoStack.Add(json);
        if (_undoStack.Count > MaxDepth)
            _undoStack.RemoveAt(0);

        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Pops the most recent state from the undo stack.
    /// Pushes <paramref name="currentJson"/> onto the redo stack.
    /// Returns the restored JSON, or <c>null</c> if nothing to undo.
    /// </summary>
    internal string? Undo(string currentJson)
    {
        if (_undoStack.Count == 0) return null;

        _redoStack.Add(currentJson);
        var restored = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);

        StateChanged?.Invoke(this, EventArgs.Empty);
        return restored;
    }

    /// <summary>
    /// Pops the most recent state from the redo stack.
    /// Pushes <paramref name="currentJson"/> onto the undo stack.
    /// Returns the restored JSON, or <c>null</c> if nothing to redo.
    /// </summary>
    internal string? Redo(string currentJson)
    {
        if (_redoStack.Count == 0) return null;

        _undoStack.Add(currentJson);
        var restored = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);

        StateChanged?.Invoke(this, EventArgs.Empty);
        return restored;
    }

    /// <summary>Clears both stacks (e.g., after a fresh file load).</summary>
    internal void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
