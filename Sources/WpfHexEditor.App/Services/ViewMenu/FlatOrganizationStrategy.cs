//////////////////////////////////////////////
// Project      : WpfHexEditor.App
// File         : FlatOrganizationStrategy.cs
// Description  : Flat organization mode — all items in a single list with group separators.
//                Reproduces the classic View menu behaviour.
// Architecture : Strategy pattern implementation.
//////////////////////////////////////////////

namespace WpfHexEditor.App.Services.ViewMenu;

/// <summary>
/// Returns all entries as a single unnamed category (flat list).
/// The <see cref="ViewMenuOrganizer"/> renders group separators between distinct
/// <see cref="ViewMenuEntry.Group"/> values.
/// </summary>
public sealed class FlatOrganizationStrategy : IViewMenuOrganizationStrategy
{
    public IReadOnlyList<ViewMenuCategory> Organize(IReadOnlyList<ViewMenuEntry> entries)
    {
        // Order: built-in items first (preserve registration order), then plugin items grouped by Group
        var builtIn = new List<ViewMenuEntry>();
        var pluginGroups = new Dictionary<string, List<ViewMenuEntry>>(StringComparer.OrdinalIgnoreCase);
        var ungrouped = new List<ViewMenuEntry>();

        foreach (var e in entries)
        {
            if (e.IsBuiltIn)
            {
                builtIn.Add(e);
            }
            else if (!string.IsNullOrWhiteSpace(e.Group))
            {
                if (!pluginGroups.TryGetValue(e.Group, out var list))
                {
                    list = [];
                    pluginGroups[e.Group] = list;
                }
                list.Add(e);
            }
            else
            {
                ungrouped.Add(e);
            }
        }

        var ordered = new List<ViewMenuEntry>(entries.Count);
        ordered.AddRange(builtIn);
        foreach (var group in pluginGroups.Values)
            ordered.AddRange(group);
        ordered.AddRange(ungrouped);

        return [new ViewMenuCategory(string.Empty, null, ordered)];
    }
}
