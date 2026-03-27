//////////////////////////////////////////////
// Project      : WpfHexEditor.App
// File         : IViewMenuOrganizationStrategy.cs
// Description  : Strategy interface for View menu organization modes.
// Architecture : Strategy pattern — one implementation per organization mode.
//////////////////////////////////////////////

namespace WpfHexEditor.App.Services.ViewMenu;

/// <summary>
/// Organizes a flat list of <see cref="ViewMenuEntry"/> items into named categories.
/// Each mode (Flat, Categorized, ByDockSide) provides its own implementation.
/// </summary>
public interface IViewMenuOrganizationStrategy
{
    /// <summary>
    /// Groups the provided entries into ordered categories.
    /// </summary>
    /// <param name="entries">All View-menu-eligible entries (built-in + plugin).</param>
    /// <returns>Ordered list of categories, each containing its member entries.</returns>
    IReadOnlyList<ViewMenuCategory> Organize(IReadOnlyList<ViewMenuEntry> entries);
}
