//////////////////////////////////////////////
// Project      : WpfHexEditor.App
// File         : DockSideOrganizationStrategy.cs
// Description  : Dock-side organization mode — items grouped by where they dock
//                (Left, Right, Bottom, Document Tabs, Floating, Other).
// Architecture : Strategy pattern implementation.
//////////////////////////////////////////////

using WpfHexEditor.Core.Options;

namespace WpfHexEditor.App.Services.ViewMenu;

/// <summary>
/// Groups entries by their <see cref="ViewMenuEntry.DockSide"/> value.
/// Items with no dock side fall into "Other".
/// </summary>
public sealed class DockSideOrganizationStrategy : IViewMenuOrganizationStrategy
{
    private readonly ViewMenuSettings _settings;

    // Ordered dock-side categories with display names and icons
    private static readonly (string DockSide, string DisplayName, string IconGlyph)[] _dockSides =
    [
        ("Left",    "Left Panels",    "\uE76B"),  // ChevronLeft
        ("Right",   "Right Panels",   "\uE76C"),  // ChevronRight
        ("Bottom",  "Bottom Panels",  "\uE76D"),  // ChevronDown
        ("Center",  "Document Tabs",  "\uE8A1"),  // Document
        ("Float",   "Floating",       "\uE8A7"),  // OpenWith
    ];

    private const string OtherDisplayName = "Other";
    private const string OtherIcon        = "\uE712"; // More

    public DockSideOrganizationStrategy(ViewMenuSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public IReadOnlyList<ViewMenuCategory> Organize(IReadOnlyList<ViewMenuEntry> entries)
    {
        var buckets = new Dictionary<string, List<ViewMenuEntry>>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var side = NormalizeSide(entry.DockSide);
            if (!buckets.TryGetValue(side, out var list))
            {
                list = [];
                buckets[side] = list;
            }
            list.Add(entry);
        }

        var result = new List<ViewMenuCategory>();

        // Add known dock sides in order
        foreach (var (dockSide, displayName, icon) in _dockSides)
        {
            if (!buckets.TryGetValue(dockSide, out var items) || items.Count == 0)
            {
                if (!_settings.CollapseEmptyCategories)
                    result.Add(new ViewMenuCategory(displayName, icon, []));
                continue;
            }

            result.Add(new ViewMenuCategory(displayName, icon, items));
        }

        // "Other" bucket for items without a dock side
        if (buckets.TryGetValue(string.Empty, out var other) && other.Count > 0)
            result.Add(new ViewMenuCategory(OtherDisplayName, OtherIcon, other));
        else if (!_settings.CollapseEmptyCategories)
            result.Add(new ViewMenuCategory(OtherDisplayName, OtherIcon, []));

        return result;
    }

    private static string NormalizeSide(string? dockSide)
    {
        if (string.IsNullOrWhiteSpace(dockSide)) return string.Empty;

        // Map "Top" to "Left" (top-docked panels are rare, group with left)
        if (string.Equals(dockSide, "Top", StringComparison.OrdinalIgnoreCase))
            return "Left";

        return dockSide;
    }
}
