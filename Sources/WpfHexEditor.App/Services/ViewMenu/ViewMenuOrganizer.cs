//////////////////////////////////////////////
// Project      : WpfHexEditor.App
// File         : ViewMenuOrganizer.cs
// Description  : Central orchestrator for the dynamic View menu organization system.
//                Collects all View-menu-eligible items (built-in + plugin),
//                applies the active organization strategy, and rebuilds the WPF menu.
// Architecture : Single-responsibility service. Composition of strategies.
//////////////////////////////////////////////

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfHexEditor.Core.Options;
using WpfHexEditor.SDK.Descriptors;

namespace WpfHexEditor.App.Services.ViewMenu;

/// <summary>
/// Orchestrates the dynamic View menu:
/// collects entries, classifies them, applies the active organization strategy,
/// and rebuilds the WPF <see cref="MenuItem"/> tree.
/// </summary>
public sealed class ViewMenuOrganizer
{
    private readonly MenuItem _viewMenuItem;
    private readonly MenuAdapter _menuAdapter;
    private readonly AppSettingsService _settingsService;
    private readonly Func<string, string?> _dockSideResolver;

    // Built-in entries registered by MainWindow.ViewMenu.cs
    private readonly List<ViewMenuEntry> _builtInEntries = [];

    // The special Command Palette entry (always first in menu)
    private ViewMenuEntry? _commandPaletteEntry;

    // Strategy map
    private readonly Dictionary<ViewMenuOrganizationMode, Func<IViewMenuOrganizationStrategy>> _strategyFactories;

    // Segoe MDL2 font for icons
    private static readonly FontFamily SegoeIcons = new("Segoe MDL2 Assets");

    public ViewMenuOrganizer(
        MenuItem viewMenuItem,
        MenuAdapter menuAdapter,
        AppSettingsService settingsService,
        Func<string, string?> dockSideResolver)
    {
        _viewMenuItem     = viewMenuItem ?? throw new ArgumentNullException(nameof(viewMenuItem));
        _menuAdapter      = menuAdapter ?? throw new ArgumentNullException(nameof(menuAdapter));
        _settingsService  = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _dockSideResolver = dockSideResolver ?? throw new ArgumentNullException(nameof(dockSideResolver));

        _strategyFactories = new()
        {
            [ViewMenuOrganizationMode.Flat]        = () => new FlatOrganizationStrategy(),
            [ViewMenuOrganizationMode.Categorized] = () => new CategorizedOrganizationStrategy(Settings),
            [ViewMenuOrganizationMode.ByDockSide]  = () => new DockSideOrganizationStrategy(Settings),
        };
    }

    private ViewMenuSettings Settings => _settingsService.Current.ViewMenu;

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Registers the Command Palette entry (always rendered first, outside categories).
    /// </summary>
    public void RegisterCommandPaletteEntry(ViewMenuEntry entry)
    {
        _commandPaletteEntry = entry;
    }

    /// <summary>
    /// Registers a built-in (hardcoded) View menu entry.
    /// </summary>
    public void RegisterBuiltInEntry(ViewMenuEntry entry)
    {
        _builtInEntries.Add(entry);
    }

    /// <summary>
    /// Fully rebuilds the View menu from scratch using the current organization mode.
    /// Designed to complete in &lt; 16ms for ~40 items.
    /// </summary>
    public void RebuildMenu()
    {
        var settings = Settings;
        var allEntries = CollectAllEntries();
        var strategy = GetStrategy(settings.Mode);
        var categories = strategy.Organize(allEntries);

        // Sort within categories
        categories = ApplySorting(categories, settings.SortOrder);

        // Rebuild WPF menu
        _viewMenuItem.Items.Clear();

        // 1. Command Palette (always first)
        if (_commandPaletteEntry is not null)
        {
            _viewMenuItem.Items.Add(BuildMenuItem(_commandPaletteEntry));
            _viewMenuItem.Items.Add(new Separator());
        }

        // 2. Pinned items (if enabled)
        if (settings.PinFavoritesToTop && settings.PinnedItemIds.Count > 0)
        {
            var pinnedCount = 0;
            foreach (var pinnedId in settings.PinnedItemIds)
            {
                var entry = FindEntry(allEntries, pinnedId);
                if (entry is null) continue;
                var mi = BuildMenuItem(entry);
                AddPinIcon(mi);
                _viewMenuItem.Items.Add(mi);
                pinnedCount++;
            }
            if (pinnedCount > 0)
                _viewMenuItem.Items.Add(new Separator());
        }

        // 3. Main content (mode-dependent)
        if (settings.Mode == ViewMenuOrganizationMode.Flat)
        {
            RenderFlat(categories, allEntries);
        }
        else
        {
            RenderCategorized(categories, settings);
        }

        // 4. Footer: "Organize By…" submenu
        _viewMenuItem.Items.Add(new Separator());
        _viewMenuItem.Items.Add(BuildOrganizeBySubmenu(settings.Mode));
    }

    /// <summary>
    /// Switches the organization mode, persists, and rebuilds.
    /// </summary>
    public void SetMode(ViewMenuOrganizationMode mode)
    {
        if (Settings.Mode == mode) return;
        Settings.Mode = mode;
        _settingsService.Save();
        RebuildMenu();
    }

    /// <summary>
    /// Pins an item to the View menu root level.
    /// </summary>
    public void PinItem(string itemId)
    {
        if (Settings.PinnedItemIds.Contains(itemId, StringComparer.OrdinalIgnoreCase)) return;
        Settings.PinnedItemIds.Add(itemId);
        _settingsService.Save();
        RebuildMenu();
    }

    /// <summary>
    /// Unpins an item from the View menu root level.
    /// </summary>
    public void UnpinItem(string itemId)
    {
        var idx = Settings.PinnedItemIds.FindIndex(
            id => string.Equals(id, itemId, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return;
        Settings.PinnedItemIds.RemoveAt(idx);
        _settingsService.Save();
        RebuildMenu();
    }

    /// <summary>
    /// Returns all View menu entries (for Command Palette "view:" prefix filtering).
    /// </summary>
    public IReadOnlyList<ViewMenuEntry> GetAllEntries()
    {
        var entries = CollectAllEntries();
        if (_commandPaletteEntry is not null)
            entries.Insert(0, _commandPaletteEntry);
        return entries;
    }

    // ── Collection ──────────────────────────────────────────────────────────

    private List<ViewMenuEntry> CollectAllEntries()
    {
        var entries = new List<ViewMenuEntry>(_builtInEntries);

        // Add plugin-contributed View items from MenuAdapter
        foreach (var (uiId, descriptor) in _menuAdapter.GetAllViewMenuItems())
        {
            var dockSide = _dockSideResolver(uiId);
            entries.Add(new ViewMenuEntry(
                Id:               uiId,
                Header:           descriptor.Header,
                GestureText:      descriptor.GestureText,
                IconGlyph:        descriptor.IconGlyph,
                Command:          descriptor.Command,
                CommandParameter: descriptor.CommandParameter,
                Group:            descriptor.Group,
                Category:         descriptor.Category,
                DockSide:         dockSide,
                ToolTip:          descriptor.ToolTip,
                IsBuiltIn:        false
            ));
        }

        return entries;
    }

    // ── Strategy ────────────────────────────────────────────────────────────

    private IViewMenuOrganizationStrategy GetStrategy(ViewMenuOrganizationMode mode)
        => _strategyFactories.TryGetValue(mode, out var factory) ? factory() : new FlatOrganizationStrategy();

    // ── Sorting ─────────────────────────────────────────────────────────────

    private static IReadOnlyList<ViewMenuCategory> ApplySorting(
        IReadOnlyList<ViewMenuCategory> categories,
        ViewMenuSortOrder sortOrder)
    {
        if (sortOrder == ViewMenuSortOrder.Custom)
            return categories; // Custom order preserved as-is

        return categories.Select(cat =>
        {
            if (cat.Items.Count <= 1) return cat;

            var sorted = sortOrder switch
            {
                ViewMenuSortOrder.Alphabetical => cat.Items.OrderBy(e => StripAccessKey(e.Header), StringComparer.OrdinalIgnoreCase).ToList(),
                ViewMenuSortOrder.ByFrequency  => cat.Items.ToList(), // TODO: integrate with CommandPalette execution history
                _                              => cat.Items.ToList(),
            };

            return cat with { Items = sorted };
        }).ToList();
    }

    // ── Rendering ───────────────────────────────────────────────────────────

    private void RenderFlat(IReadOnlyList<ViewMenuCategory> categories, IReadOnlyList<ViewMenuEntry> allEntries)
    {
        if (categories.Count == 0) return;

        var items = categories[0].Items;
        string? lastGroup = null;

        foreach (var entry in items)
        {
            // Insert separator between different groups
            if (entry.Group is not null && !string.Equals(entry.Group, lastGroup, StringComparison.OrdinalIgnoreCase))
            {
                if (lastGroup is not null)
                    _viewMenuItem.Items.Add(new Separator());
                lastGroup = entry.Group;
            }
            else if (entry.IsBuiltIn && lastGroup is null)
            {
                // Built-in items have no Group — detect transition from built-in to plugin
            }

            _viewMenuItem.Items.Add(BuildMenuItem(entry));
        }
    }

    private void RenderCategorized(IReadOnlyList<ViewMenuCategory> categories, ViewMenuSettings settings)
    {
        foreach (var cat in categories)
        {
            if (cat.Items.Count == 0 && settings.CollapseEmptyCategories)
                continue;

            var submenu = new MenuItem { Header = cat.Name };

            if (cat.IconGlyph is not null && settings.ShowIcons)
            {
                submenu.Icon = new TextBlock
                {
                    Text                = cat.IconGlyph,
                    FontFamily          = SegoeIcons,
                    FontSize            = 13,
                    Foreground          = (Brush)Application.Current.FindResource("DockMenuForegroundBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                };
            }

            foreach (var entry in cat.Items)
                submenu.Items.Add(BuildMenuItem(entry));

            _viewMenuItem.Items.Add(submenu);
        }
    }

    // ── MenuItem Factory ────────────────────────────────────────────────────

    private MenuItem BuildMenuItem(ViewMenuEntry entry)
    {
        var settings = Settings;
        var mi = new MenuItem
        {
            Header           = entry.Header,
            Command          = entry.Command,
            CommandParameter = entry.CommandParameter,
            ToolTip          = entry.ToolTip,
            Tag              = entry.Id, // Store ID for pin/unpin lookup
        };

        if (settings.ShowGestureText && !string.IsNullOrEmpty(entry.GestureText))
            mi.InputGestureText = entry.GestureText;

        if (settings.ShowIcons && !string.IsNullOrEmpty(entry.IconGlyph))
        {
            mi.Icon = new TextBlock
            {
                Text                = entry.IconGlyph,
                FontFamily          = SegoeIcons,
                FontSize            = 13,
                Foreground          = (Brush)Application.Current.FindResource("DockMenuForegroundBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            };
        }

        // Context menu for pin/unpin (skip for Command Palette entry)
        if (entry.Id != _commandPaletteEntry?.Id)
        {
            mi.ContextMenu = BuildPinContextMenu(entry.Id);
        }

        return mi;
    }

    // ── Pin Context Menu ────────────────────────────────────────────────────

    private ContextMenu BuildPinContextMenu(string itemId)
    {
        var isPinned = Settings.PinnedItemIds.Contains(itemId, StringComparer.OrdinalIgnoreCase);

        var ctx = new ContextMenu();

        var pinItem = new MenuItem
        {
            Header = isPinned ? "_Unpin from View Menu" : "_Pin to View Menu",
            Icon = new TextBlock
            {
                Text       = isPinned ? "\uE77A" : "\uE718", // Unpin / Pin
                FontFamily = SegoeIcons,
                FontSize   = 12,
            },
        };

        var capturedId = itemId;
        pinItem.Click += (_, _) =>
        {
            if (isPinned)
                UnpinItem(capturedId);
            else
                PinItem(capturedId);
        };

        ctx.Items.Add(pinItem);
        return ctx;
    }

    private static void AddPinIcon(MenuItem mi)
    {
        // Prefix the existing icon with a small pin indicator
        if (mi.Icon is TextBlock existing)
        {
            // Replace with a StackPanel containing pin + original icon
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock
            {
                Text       = "\uE718",
                FontFamily = SegoeIcons,
                FontSize   = 9,
                VerticalAlignment = VerticalAlignment.Center,
                Margin     = new Thickness(0, 0, 2, 0),
                Opacity    = 0.6,
            });
            sp.Children.Add(new TextBlock
            {
                Text                = existing.Text,
                FontFamily          = existing.FontFamily,
                FontSize            = existing.FontSize,
                Foreground          = existing.Foreground,
                HorizontalAlignment = existing.HorizontalAlignment,
                VerticalAlignment   = existing.VerticalAlignment,
            });
            mi.Icon = sp;
        }
    }

    // ── "Organize By…" Submenu ──────────────────────────────────────────────

    private MenuItem BuildOrganizeBySubmenu(ViewMenuOrganizationMode currentMode)
    {
        var submenu = new MenuItem
        {
            Header = "Or_ganize By…",
            Icon = new TextBlock
            {
                Text       = "\uE700", // GlobalNavButton
                FontFamily = SegoeIcons,
                FontSize   = 13,
                Foreground = (Brush)Application.Current.FindResource("DockMenuForegroundBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            },
        };

        submenu.Items.Add(BuildModeItem("_Flat (Classic)", ViewMenuOrganizationMode.Flat, currentMode));
        submenu.Items.Add(BuildModeItem("_Categorized",    ViewMenuOrganizationMode.Categorized, currentMode));
        submenu.Items.Add(BuildModeItem("By _Dock Side",   ViewMenuOrganizationMode.ByDockSide, currentMode));

        return submenu;
    }

    private MenuItem BuildModeItem(string header, ViewMenuOrganizationMode mode, ViewMenuOrganizationMode currentMode)
    {
        var mi = new MenuItem
        {
            Header      = header,
            IsCheckable = true,
            IsChecked   = mode == currentMode,
        };

        mi.Click += (_, _) => SetMode(mode);
        return mi;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ViewMenuEntry? FindEntry(IReadOnlyList<ViewMenuEntry> entries, string id)
        => entries.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));

    private static string StripAccessKey(string header)
        => header.StartsWith('_') ? header[1..] : header;
}
