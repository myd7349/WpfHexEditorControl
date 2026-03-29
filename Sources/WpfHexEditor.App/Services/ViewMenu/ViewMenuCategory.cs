//////////////////////////////////////////////
// Project      : WpfHexEditor.App
// File         : ViewMenuCategory.cs
// Description  : Represents a named category (submenu group) of View menu entries.
// Architecture : Pure data record — no WPF dependencies.
//////////////////////////////////////////////

namespace WpfHexEditor.App.Services.ViewMenu;

/// <summary>
/// A named grouping of View menu entries, rendered as a submenu in Categorized/DockSide modes.
/// </summary>
public sealed record ViewMenuCategory(
    /// <summary>Display name for the category submenu header.</summary>
    string Name,

    /// <summary>Segoe MDL2 Assets glyph for the submenu header icon, or null.</summary>
    string? IconGlyph,

    /// <summary>Ordered list of entries within this category.</summary>
    IReadOnlyList<ViewMenuEntry> Items
);
