//////////////////////////////////////////////
// Project      : WpfHexEditor.App
// File         : CommandPaletteEntry.cs
// Description  : Immutable entry representing a single command in the Command Palette.
//                No WPF dependency — safe for unit testing.
// Architecture : Domain record; consumed by CommandPaletteService and CommandPaletteWindow.
//////////////////////////////////////////////

using System.Windows.Input;

namespace WpfHexEditor.App.Models;

/// <summary>
/// Represents a single command exposed in the Command Palette (Ctrl+Shift+P).
/// </summary>
/// <param name="Name">Display name shown as the primary label.</param>
/// <param name="Category">Grouping category (e.g. "File", "Edit", "Plugins").</param>
/// <param name="GestureText">Optional keyboard shortcut text (e.g. "Ctrl+S").</param>
/// <param name="IconGlyph">Optional Segoe MDL2 Assets character (e.g. "\uE74E").</param>
/// <param name="Command">Command to execute; null entries are display-only.</param>
/// <param name="CommandParameter">Optional parameter forwarded to <see cref="Command"/>.</param>
public sealed record CommandPaletteEntry(
    string Name,
    string Category,
    string? GestureText,
    string? IconGlyph,
    ICommand? Command,
    object? CommandParameter = null);
