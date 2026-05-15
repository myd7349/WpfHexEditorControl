---
name: xaml-guard
description: |
  INTERNAL DEV WORKFLOW for WpfHexEditor — Claude self-invokes after editing
  any *.xaml, *.resx, or *Resources.Designer.cs file to detect regressions in
  the high-incident XAML/resx/loc domain (hardcoded user-strings, XAML patcher
  corruption, resx malformed, key drift, HexEditor Resources alias, MessageBox
  in code-behind, ContextMenu transparency, DockPanel-row alignment). Skip on:
  pure structural attribute edits (Grid.Row, Margin, Width).
---

# xaml-guard (internal)

Layered guard for the highest-incident surface in project memory: `adr_007_xaml_patcher_bug`, `feedback_resx_satellite_corruption`, `feedback_localization_new_strings`, `feedback_resources_alias_hexeditor`, `bug_contextmenu_transparent`, `feedback_overflow_menu_alignment`.

## When I invoke

| Situation | Run? |
|---|---|
| Edit `*.xaml`, `*.resx`, `*Resources.Designer.cs`, new UserControl/Window/Page | yes |
| Pure structural attribute edit (Grid.Row, Margin, Padding), code-behind `.xaml.cs` only | no |

## Pipeline

1. `scripts/xaml-check.ps1 -Files <paths>` for XAML rules.
2. `scripts/resx-validate.ps1 -File <path>` per resx (parse + Designer parity).
3. → `XAML: <n> issues | LocCoverage~<pct>%` or `OK`

## 10 rules

Full regex + exceptions: `references/xaml-rules.md`.

| Rule | Anchor memory |
|---|---|
| `hardcoded-text` | `feedback_localization_new_strings` |
| `missing-dynamic` | `project_phase6_localization_complete` |
| `patcher-corruption` | `adr_007_xaml_patcher_bug` |
| `xmlns-mangle` | `adr_007_xaml_patcher_bug` |
| `resx-malformed` | `feedback_resx_satellite_corruption` |
| `designer-drift` | detected by resx-validate |
| `hexeditor-resources` | `feedback_resources_alias_hexeditor` |
| `messagebox-show` | `adr_009_themed_messagebox` |
| `static-color-no-alpha` | `bug_contextmenu_transparent` |
| `dockpanel-row-grid` | `feedback_overflow_menu_alignment` |

## Output

```
XAML: 2 hardcoded-text, 1 missing-dynamic | LocCoverage=78%
  MainWindow.xaml:42  hardcoded-text  Header="Settings"
  Toolbar.xaml:15     missing-dynamic Tooltip="Save"
```

`LocCoverage` is approximate (DynamicResource count vs attribute string count). Does not rewrite XAML, translate strings, or verify XAML compiles.

**False positives:** technical values (`Style="{StaticResource …}"`, `x:Key=…`, `FontFamily="Segoe UI"`, brush names) are whitelisted — see `references/xaml-rules.md`.
