---
name: nuget-guard
description: |
  INTERNAL DEV WORKFLOW for WpfHexEditor — Claude self-invokes after editing
  any *.cs or *.csproj that belongs to one of the 13 packages published on
  nuget.org by abbaye (WpfHexEditor.Core.ByteProvider, .BinaryAnalysis,
  WPFHexaEditor, WpfCodeEditor, WpfDocking, WpfColorPicker, WpfTerminal,
  whfmt.Analysis/.Backfill/.CodeGen/.Fuzz/.Validate/.FileFormatCatalog).
  Protects the standalone-mode contract when IDE features are added:
  detects IDE-only references leaking into a NuGet package, TFM drift,
  UseWPF leak on core-xplat packages, WPF/WinForms usings in core-xplat,
  and public-API regressions vs git HEAD (api-removed, api-renamed).
  Extending the API is always allowed; removing/renaming is not. Skip on
  files outside protected packages, Tests/, Samples/, *.Designer.cs, *.g.cs.
---

# nuget-guard (internal)

Protects the standalone contract of the 13 published packages. IDE shell is a consumer — packages must work without it.

**Categories:**
- `core-xplat` (net8.0, zero WPF/WinForms, no IDE refs): ByteProvider, BinaryAnalysis, whfmt.FileFormatCatalog, whfmt.Analysis/Backfill/CodeGen/Fuzz/Validate
- `wpf-control` (net8.0-windows, WPF allowed, no `WpfHexEditor.App/Editor/Plugins.*` refs): WPFHexaEditor, WpfCodeEditor, WpfDocking, WpfColorPicker, WpfTerminal

Full path table: `data/package-policy.json`. `WpfCaret` (other repo) not protected.

## When I invoke

| Situation | Run? |
|---|---|
| Edit `*.cs` or `*.csproj` under a protected package | yes |
| Non-protected csproj, `Tests/`, `Samples/`, `*.Designer.cs`, `*.g.cs`, XAML/resx | no |

## Pipeline

1. Resolve edited file's owning csproj → check `<PackageId>` against policy. Not found → skip.
2. Run `scripts/nuget-guard.ps1 -Files <paths>`.
3. Exit code = ERR count (capped 100). WARN-only ⇒ exit 0.
4. Suppress: `// nuget-ignore: <reason>` (reason mandatory).

## Rules

| Rule | Sev | Detects |
|---|---|---|
| `nuget-api-removed` | error | public type/member in git HEAD removed from working tree |
| `nuget-api-renamed` | error | public signature removed + similar-named added (rename breaks consumers) |
| `nuget-tfm-drift` | error | `<TargetFramework>` doesn't match policy (e.g. ByteProvider must stay `net8.0`) |
| `nuget-usewpf-leak` | error | `<UseWPF>true` or `<UseWindowsForms>true` on a `core-xplat` package |
| `nuget-ide-projref` | error | `<ProjectReference>` to IDE-only assembly (`WpfHexEditor.App/Editor/Plugins.*`; `Editor.*.Core` allowed) |
| `nuget-ide-using` | error | IDE-only type ref (`IDEHostContext`, `IdeMessageBox`, `DockManager`, …) |
| `nuget-wpf-using-in-xplat` | error | `using System.Windows.*` / `System.Windows.Forms` / `WebView2.*` in `core-xplat` |
| `nuget-version-regression` | error | `<Version>` numerically lower than git HEAD |
| `nuget-release-notes-stale` | warn | `<Version>` bumped, `<PackageReleaseNotes>` unchanged |

Extending public API (new types, members, overloads), changing internal/private members, and bumping `<Version>` upward are all allowed.

## Maintenance

- New NuGet package → add entry in `data/package-policy.json`.
- New IDE-only type that must not leak → add to `ide_only_types` in policy.
- TFM rollout → update `tfm` arrays per package in single coordinated commit.
