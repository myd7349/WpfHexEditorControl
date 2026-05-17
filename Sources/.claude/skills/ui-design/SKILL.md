---
name: ui-design
description: |
  INTERNAL DEV WORKFLOW for WpfHexEditor — Claude self-invokes after editing
  *.xaml under WpfHexEditor.App/, Editors/, Controls/, Plugins/, Core/ to
  enforce the design-system: no hardcoded colors (use {DynamicResource Dock*
  / HexEditor_* / Panel_*}), no unknown tokens, canonical spacing scale,
  canonical FontSize ladder, glyph-only interactive elements need ToolTip,
  no template reinvention. Skip on: Themes/*.xaml (themes ARE the palette),
  ColorPicker (legitimate color literals), generated *.g.cs.
---

# ui-design (internal)

Static guard for 18-theme design system (`Dock*` / `HexEditor_*` / `Panel_*` token surface).

## When I invoke

| Situation | Run? |
|---|---|
| Edit `*.xaml` under `App/`, `Editors/`, `Controls/`, `Plugins/` | yes |
| Edit `*.xaml` in `Themes/`, `*ColorPicker*`, generated `*.g.cs` | no |
| New theme under `Themes/` | run `token-catalog.ps1` instead |

## Pipeline

1. If `data/known-tokens.json` missing or stale: run `scripts/token-catalog.ps1` first.
2. `scripts/ui-check.ps1 -Files <paths>`
3. → `UI: <summary> | TokenCoverage=<pct>%` or `OK`

## 6 rules

| Rule | Severity | Reference |
|---|---|---|
| `hardcoded-color` | error | `references/design-tokens.md` |
| `unknown-token` | error | `data/known-tokens.json` (only keys ending in Brush/Color/Background/Foreground/Border/Fill/Stroke/Highlight/Accent) |
| `non-canonical-spacing` | warn | `references/spacing-grid.md` |
| `non-canonical-fontsize` | warn | `references/typography.md` |
| `glyph-no-tooltip` | warn | `references/glyphs-catalog.md` (skips icon slots + `PanelIconButtonStyle`/`PanelIconToggleStyle`) |
| `reinvented-style` | warn | `references/components.md` |

`hardcoded-color` + `unknown-token` are errors (break theme switching at runtime). Warnings flag drift; resolve when they cluster.

## Output

```
UI: 3 hardcoded-color, 2 unknown-token, 1 non-canonical-fontsize | TokenCoverage=92%
  MainWindow.xaml:122  hardcoded-color  Background="#E81123"
  MainWindow.xaml:1687 unknown-token    {DynamicResource DockAccentBrush}
  Welcome.xaml:330     non-canonical-fontsize  FontSize="15"
```

Does not fix colors, auto-add tooltips, verify compile, or check accessibility beyond glyph-no-tooltip.

## Maintenance

- New theme → `token-catalog.ps1` immediately so `unknown-token` sees new keys.
- New canonical spacing/FontSize → update arrays in `ui-check.ps1` + matching reference file.
- Periodic: `glyph-catalog.ps1` to refresh `data/glyphs.tsv` and spot duplicate glyphs.
