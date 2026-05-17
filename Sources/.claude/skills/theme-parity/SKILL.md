---
name: theme-parity
description: |
  INTERNAL DEV WORKFLOW for WpfHexEditor — Claude self-invokes after editing
  Themes/<Theme>/Colors.xaml or *Theme.xaml to verify the 18 themes share the
  same key surface. Detects: theme-key-missing (key in Dark but not in the
  edited theme), theme-key-orphan (key only in this theme), theme-color-no-brush
  (Color without paired SolidColorBrush), theme-merge-order (wrapper merges
  Colors.xaml after Menu.xaml/TabControl.xaml). Skip on: edits outside the
  Themes/ tree.
---

# theme-parity (internal)

Theme switching is a pure ResourceDictionary swap — every theme must expose the same key set. `DarkTheme` is the canonical reference.

## When I invoke

| Situation | Run? |
|---|---|
| Edit `*/Themes/<X>/Colors.xaml` (Shell or Docking) | yes |
| Edit `*Theme.xaml` wrapper (merge order) | yes |
| Add a new theme folder | yes (regen reference first) |
| Edit non-theme XAML | no (`xaml-guard` / `ui-design`) |

## Pipeline

1. If `data/reference-keys.txt` missing: `scripts/theme-parity.ps1 -RefreshReference` (extracts keys from Dark).
2. `scripts/theme-parity.ps1 -Files <paths>` → per-theme summary.
3. Suggest patch (copy-paste values from Dark): `scripts/theme-parity.ps1 -Files <path> -SuggestPatch`

## 4 rules

| Rule | Severity | Detects |
|---|---|---|
| `theme-key-missing` | error | key in Dark reference, absent from edited theme |
| `theme-key-orphan` | warn | key only in edited theme, no other theme has it |
| `theme-color-no-brush` | warn | `<Color x:Key="XColor">` without paired `<SolidColorBrush x:Key="XBrush">` |
| `theme-merge-order` | error | `*Theme.xaml` merges Colors.xaml AFTER Menu.xaml/TabControl.xaml |

## Output

```
Theme: Cyberpunk
  Missing: 4   DockMenuHighlightColor, DockMenuHighlightBrush, DockSplitterColor, DockSplitterBrush
  Orphan:  1   CyberNeonAccentColor
  Color/brush mismatch: 0  |  Merge order: OK
```

Does not validate color aesthetics, auto-apply patches, or check token usage in non-theme XAML (`ui-design` covers that).

## Maintenance

- Adding keys to Dark → regen reference, then `theme-parity.ps1` fires on all other themes to roll the token.
- Promoting token to all themes → add to Dark first, regen, propagate.
