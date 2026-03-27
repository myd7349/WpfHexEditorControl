//////////////////////////////////////////////
// Project      : WpfHexEditor.App
// File         : CategorizedOrganizationStrategy.cs
// Description  : Categorized organization mode — items grouped into submenus
//                by functional category (Analysis, Design, Data & Files, etc.).
// Architecture : Strategy pattern implementation. Uses ViewMenuClassifier for assignment.
//////////////////////////////////////////////

using WpfHexEditor.Core.Options;

namespace WpfHexEditor.App.Services.ViewMenu;

/// <summary>
/// Groups entries into functional-category submenus using <see cref="ViewMenuClassifier"/>.
/// Categories are ordered by <see cref="ViewMenuClassifier.GetOrderedCategories"/>.
/// </summary>
public sealed class CategorizedOrganizationStrategy : IViewMenuOrganizationStrategy
{
    private readonly ViewMenuSettings _settings;

    public CategorizedOrganizationStrategy(ViewMenuSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public IReadOnlyList<ViewMenuCategory> Organize(IReadOnlyList<ViewMenuEntry> entries)
    {
        // Classify each entry
        var buckets = new Dictionary<string, List<ViewMenuEntry>>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var category = ViewMenuClassifier.Classify(entry, _settings);
            if (!buckets.TryGetValue(category, out var list))
            {
                list = [];
                buckets[category] = list;
            }
            list.Add(entry);
        }

        // Build result in canonical category order
        var result = new List<ViewMenuCategory>();

        foreach (var catName in ViewMenuClassifier.GetOrderedCategories())
        {
            if (!buckets.TryGetValue(catName, out var items) || items.Count == 0)
            {
                if (!_settings.CollapseEmptyCategories)
                    result.Add(new ViewMenuCategory(catName, ViewMenuClassifier.GetCategoryIcon(catName), []));
                continue;
            }

            result.Add(new ViewMenuCategory(catName, ViewMenuClassifier.GetCategoryIcon(catName), items));
        }

        // Add any custom/unknown categories not in the canonical list
        foreach (var kv in buckets)
        {
            if (ViewMenuClassifier.GetOrderedCategories().Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                continue;
            if (kv.Value.Count == 0 && _settings.CollapseEmptyCategories)
                continue;
            result.Add(new ViewMenuCategory(kv.Key, ViewMenuClassifier.GetCategoryIcon(kv.Key), kv.Value));
        }

        return result;
    }
}
