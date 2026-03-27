// ==========================================================
// Project: WpfHexEditor.App
// File: Models/LayoutCustomizationItem.cs
// Description:
//     Data model for the Customize Layout popup items.
//     Supports toggle switches, radio groups, section headers,
//     and layout mode toggles.
//
// Architecture Notes:
//     Pure domain records — no WPF dependencies. Safe for unit testing.
// ==========================================================

namespace WpfHexEditor.App.Models;

/// <summary>
/// Discriminates the kind of item shown in the Customize Layout popup.
/// </summary>
public enum LayoutItemKind
{
    /// <summary>A visibility toggle (eye icon on/off).</summary>
    Toggle,

    /// <summary>A group of mutually exclusive radio-pill options.</summary>
    RadioGroup,

    /// <summary>A visual section header (not interactive).</summary>
    SectionHeader,

    /// <summary>A layout mode toggle (Full Screen, Zen, etc.).</summary>
    ModeToggle
}

/// <summary>
/// Represents a single item in the Customize Layout popup.
/// </summary>
/// <param name="Id">Unique identifier (e.g. "menubar", "toolbar-position", "zen").</param>
/// <param name="DisplayName">User-visible label.</param>
/// <param name="Kind">Visual and interaction type.</param>
/// <param name="GestureText">Keyboard shortcut text (e.g. "Ctrl+B"), or null.</param>
/// <param name="IconGlyph">Segoe MDL2 Assets glyph, or null.</param>
/// <param name="GroupId">For RadioGroup items, the group key (e.g. "toolbar-position").</param>
/// <param name="IsChecked">Current on/off state for toggles and modes.</param>
/// <param name="Description">Optional tooltip or description text.</param>
/// <param name="RadioOptions">For RadioGroup items, the available choices with their labels.</param>
/// <param name="SelectedOption">For RadioGroup items, the currently selected option value.</param>
public sealed record LayoutCustomizationItem(
    string Id,
    string DisplayName,
    LayoutItemKind Kind,
    string? GestureText = null,
    string? IconGlyph = null,
    string? GroupId = null,
    bool IsChecked = false,
    string? Description = null,
    IReadOnlyList<(string Value, string Label)>? RadioOptions = null,
    string? SelectedOption = null);
