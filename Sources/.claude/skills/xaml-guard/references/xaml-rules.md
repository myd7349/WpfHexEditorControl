# XAML/resx rules

| Rule                    | Pattern                                                  | Anchor memory                                |
|-------------------------|----------------------------------------------------------|----------------------------------------------|
| `hardcoded-text`        | `Text\|Header\|Title\|Content\|Tooltip="A..."` not `{...}`| `feedback_localization_new_strings`         |
| `missing-dynamic`       | user-visible string with no `DynamicResource`             | `project_phase6_localization_complete`       |
| `patcher-corruption`    | `="{DynamicResource X}"` (no attribute name)              | `adr_007_xaml_patcher_bug`                   |
| `xmlns-mangle`          | `xmlns:alias:Class` (alias-prefixed namespace)            | `adr_007_xaml_patcher_bug`                   |
| `resx-malformed`        | XML parse fails or root != `<root>`                       | `feedback_resx_satellite_corruption`         |
| `designer-drift`        | resx key absent from neighbouring Designer.cs             | implicit                                     |
| `hexeditor-resources`   | `Resources.X` in HexEditor partials without L10n alias    | `feedback_resources_alias_hexeditor`         |
| `messagebox-show`       | `MessageBox.Show(` in code-behind                          | `adr_009_themed_messagebox`                  |
| `static-color-no-alpha` | `Background="#RRGGBB"` on ContextMenu/MenuItem            | `bug_contextmenu_transparent`                |
| `dockpanel-row-grid`    | `<DockPanel>` inside MenuItem/TabItem header              | `feedback_overflow_menu_alignment`           |

## Whitelist (NOT flagged as hardcoded-text)

- All-lowercase values: `Text="left"` (binding key/path).
- All-caps acronyms: `Text="UTF8"`, `Text="OK"`.
- Numeric / punctuation-only: `Text="1.0"`, `Text="..."`.
- Symbol glyphs from Segoe Fluent Icons (codepoint > U+E000, length <= 2).
- Technical attribute names (kept in `xaml-check.ps1` array): `x:Key`,
  `Style`, `Template`, `TargetType`, `Property`, `Path`, `Source`, `Binding`,
  `Color`, `Brush`, `FontFamily`, `x:Name`, `x:Class`, `xmlns`,
  `Storyboard.TargetProperty`, `RoutedEvent`, `Tag`, `Background`,
  `Foreground`, `BorderBrush`, `Fill`, `Stroke`, `Stretch`, `Visibility`,
  `HorizontalAlignment`, `VerticalAlignment`, `Orientation`.

New rule or whitelist entry → update here AND `xaml-check.ps1` in sync.
