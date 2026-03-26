// Project      : WpfHexEditorControl
// File         : Views/DiffViewerDocument.Contributors.cs
// Description  : IStatusBarContributor + IEditorToolbarContributor for the binary diff viewer.
//                Status bar: Differences, Similarity, Algorithm (Mode), Context lines, Zoom.
//                Toolbar: Prev/Next diff, Block-aligned toggle, Recompare, Stats, Zoom in/reset/out.
// Architecture : Partial class on DiffViewerDocument — same pattern as HexEditor.StatusBarContributor.

using System.Collections.ObjectModel;
using System.Windows.Input;
using WpfHexEditor.Editor.Core;
using WpfHexEditor.SDK.Commands;

namespace WpfHexEditor.Plugins.FileComparison.Views;

public sealed partial class DiffViewerDocument : IStatusBarContributor, IEditorToolbarContributor
{
    // ── Status bar ────────────────────────────────────────────────────────────

    private ObservableCollection<StatusBarItem>? _statusBarItems;

    private StatusBarItem _diffCountItem   = null!;
    private StatusBarItem _similarityItem  = null!;
    private StatusBarItem _algorithmItem   = null!;
    private StatusBarItem _contextItem     = null!;
    private StatusBarItem _zoomItem        = null!;

    public ObservableCollection<StatusBarItem> StatusBarItems
        => _statusBarItems ??= BuildStatusBarItems();

    public void RefreshStatusBarItems()
    {
        if (_vm is null) return;

        _diffCountItem.Value  = $"{_vm.TotalDiffCount} \u0394";
        _similarityItem.Value = _vm.SimilarityText;

        // Algorithm active choices
        var choices = _algorithmItem.Choices;
        if (choices.Count == 2)
        {
            choices[0].IsActive = !_vm.UseBlockAlignment;   // "Fast"
            choices[1].IsActive =  _vm.UseBlockAlignment;   // "Block-aligned"
        }

        // Context active choice
        UpdateContextChoiceActive();

        // Zoom
        _zoomItem.Value = $"{(int)((_vm?.ZoomLevel ?? 1.0) * 100)}%";
        UpdateZoomChoiceActive();
    }

    private ObservableCollection<StatusBarItem> BuildStatusBarItems()
    {
        _diffCountItem = new StatusBarItem
        {
            Label   = "Differences",
            Tooltip = "Total number of changed regions",
            Value   = "0 \u0394",
        };

        _similarityItem = new StatusBarItem
        {
            Label   = "Similarity",
            Tooltip = "Estimated similarity between the two files",
            Value   = "\u2014",
        };

        _algorithmItem = new StatusBarItem
        {
            Label   = "Mode",
            Tooltip = "Click to toggle diff algorithm",
            Value   = "Fast",
        };
        _algorithmItem.Choices.Add(new StatusBarChoice
        {
            DisplayName = "Fast (byte-scan)",
            IsActive    = true,
            Command     = new RelayCommand(_ =>
            {
                if (_vm is null) return;
                _vm.UseBlockAlignment = false;
                RefreshStatusBarItems();
            }),
        });
        _algorithmItem.Choices.Add(new StatusBarChoice
        {
            DisplayName = "Block-aligned",
            IsActive    = false,
            Command     = new RelayCommand(_ =>
            {
                if (_vm is null) return;
                _vm.UseBlockAlignment = true;
                RefreshStatusBarItems();
            }),
        });

        _contextItem = new StatusBarItem
        {
            Label   = "Context",
            Tooltip = "Equal rows shown on each side of a diff block",
            Value   = "3 lines",
        };
        foreach (var (display, value) in new[]
        {
            ("0 lines", 0), ("3 lines", 3), ("8 lines", 8), ("All", int.MaxValue),
        })
        {
            var capture = value;
            var displayCapture = display;
            _contextItem.Choices.Add(new StatusBarChoice
            {
                DisplayName = display,
                IsActive    = value == 3,
                Command     = new RelayCommand(_ =>
                {
                    if (_vm is null) return;
                    _vm.BinaryContextLines = capture;
                    _contextItem.Value = displayCapture;
                    UpdateContextChoiceActive();
                }),
            });
        }

        _zoomItem = new StatusBarItem
        {
            Label   = "Zoom",
            Tooltip = "Canvas zoom level (Ctrl+Wheel to change)",
            Value   = "100%",
        };
        foreach (var (display, pct) in new[]
        {
            ("50%", 0.5), ("75%", 0.75), ("100%", 1.0), ("150%", 1.5), ("200%", 2.0),
        })
        {
            var capture = pct;
            var displayCapture = display;
            _zoomItem.Choices.Add(new StatusBarChoice
            {
                DisplayName = display,
                IsActive    = Math.Abs(pct - 1.0) < 0.001,
                Command     = new RelayCommand(_ =>
                {
                    if (_vm is null) return;
                    _vm.ZoomLevel = capture;
                    _zoomItem.Value = displayCapture;
                    UpdateZoomChoiceActive();
                }),
            });
        }

        return [_diffCountItem, _similarityItem, _algorithmItem, _contextItem, _zoomItem];
    }

    private void UpdateContextChoiceActive()
    {
        if (_vm is null) return;
        foreach (var c in _contextItem.Choices)
        {
            c.IsActive = c.DisplayName switch
            {
                "All"    => _vm.BinaryContextLines == int.MaxValue,
                "0 lines"=> _vm.BinaryContextLines == 0,
                "3 lines"=> _vm.BinaryContextLines == 3,
                "8 lines"=> _vm.BinaryContextLines == 8,
                _        => false,
            };
        }
    }

    private void UpdateZoomChoiceActive()
    {
        if (_vm is null) return;
        foreach (var c in _zoomItem.Choices)
        {
            var pct = c.DisplayName.TrimEnd('%');
            if (int.TryParse(pct, out var val))
                c.IsActive = Math.Abs(_vm.ZoomLevel - val / 100.0) < 0.01;
        }
    }

    // ── Toolbar ───────────────────────────────────────────────────────────────

    private ObservableCollection<EditorToolbarItem>? _toolbarItems;
    private EditorToolbarItem _blockAlignedItem = null!;

    public ObservableCollection<EditorToolbarItem> ToolbarItems
        => _toolbarItems ??= BuildToolbarItems();

    private ObservableCollection<EditorToolbarItem> BuildToolbarItems()
    {
        _blockAlignedItem = new EditorToolbarItem
        {
            Icon      = "\uE9D5",
            Tooltip   = "Block-aligned diff (detects true byte insertions/deletions)",
            IsToggle  = true,
            IsChecked = _vm?.UseBlockAlignment ?? false,
            Command   = new RelayCommand(_ =>
            {
                if (_vm is null) return;
                _vm.UseBlockAlignment  = !_vm.UseBlockAlignment;
                _blockAlignedItem.IsChecked = _vm.UseBlockAlignment;
                RefreshStatusBarItems();
            }),
        };

        return
        [
            new EditorToolbarItem
            {
                Icon    = "\uE76B",
                Tooltip = "Previous difference (Alt+Up)",
                Command = new RelayCommand(_ => _vm?.PrevDiffCommand.Execute(null),
                                           _ => _vm?.PrevDiffCommand.CanExecute(null) ?? false),
            },
            new EditorToolbarItem
            {
                Icon    = "\uE76C",
                Tooltip = "Next difference (Alt+Down)",
                Command = new RelayCommand(_ => _vm?.NextDiffCommand.Execute(null),
                                           _ => _vm?.NextDiffCommand.CanExecute(null) ?? false),
            },
            new EditorToolbarItem { IsSeparator = true },
            _blockAlignedItem,
            new EditorToolbarItem
            {
                Icon    = "\uE72C",
                Tooltip = "Re-compare files with current settings",
                Command = new RelayCommand(_ => _vm?.RecompareCommand.Execute(null),
                                           _ => _vm?.RecompareCommand.CanExecute(null) ?? false),
            },
            new EditorToolbarItem { IsSeparator = true },
            new EditorToolbarItem
            {
                Icon    = "\uE9D2",
                Tooltip = "Show/hide entropy and frequency stats panel",
                Command = new RelayCommand(_ => _vm?.ToggleStatsCommand.Execute(null)),
            },
            new EditorToolbarItem { IsSeparator = true },
            new EditorToolbarItem
            {
                Icon    = "\uE8A3",
                Tooltip = "Zoom in (Ctrl+Wheel)",
                Command = new RelayCommand(_ =>
                {
                    if (_vm is null) return;
                    _vm.ZoomLevel = Math.Clamp(Math.Round(_vm.ZoomLevel + 0.1, 1), 0.5, 4.0);
                    RefreshStatusBarItems();
                }),
            },
            new EditorToolbarItem
            {
                Icon    = "\uE71E",
                Tooltip = "Reset zoom to 100%",
                Command = new RelayCommand(_ =>
                {
                    if (_vm is null) return;
                    _vm.ZoomLevel = 1.0;
                    RefreshStatusBarItems();
                }),
            },
            new EditorToolbarItem
            {
                Icon    = "\uE71F",
                Tooltip = "Zoom out (Ctrl+Wheel)",
                Command = new RelayCommand(_ =>
                {
                    if (_vm is null) return;
                    _vm.ZoomLevel = Math.Clamp(Math.Round(_vm.ZoomLevel - 0.1, 1), 0.5, 4.0);
                    RefreshStatusBarItems();
                }),
            },
        ];
    }
}
