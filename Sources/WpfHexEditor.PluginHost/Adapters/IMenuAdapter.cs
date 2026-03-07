//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using WpfHexEditor.SDK.Descriptors;

namespace WpfHexEditor.PluginHost.Adapters;

/// <summary>
/// Abstracts menu management operations for plugin menu contributions.
/// </summary>
public interface IMenuAdapter
{
    /// <summary>
    /// Adds a menu item to the IDE menu hierarchy.
    /// </summary>
    /// <param name="uiId">Unique identifier used for later removal.</param>
    /// <param name="descriptor">Menu item configuration (header, parent path, command, etc.).</param>
    void AddMenuItem(string uiId, MenuItemDescriptor descriptor);

    /// <summary>
    /// Removes a previously added menu item by its UI ID.
    /// </summary>
    /// <param name="uiId">Unique identifier of the menu item to remove.</param>
    void RemoveMenuItem(string uiId);
}
