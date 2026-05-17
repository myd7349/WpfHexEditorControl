---
name: loc-guard
description: |
  INTERNAL DEV WORKFLOW for WpfHexEditor — Claude self-invokes on two scopes:
  (1) PARITY — after editing any *.resx (base or satellite), verifies the
  28-language satellite parity (missing/orphan keys, placeholder drift,
  untranslated). (2) SOURCE — after editing *.xaml or *.cs under App/,
  Editors/, Plugins/, Core/, Controls/, advisory checks for: DynamicResource
  on loc keys (must be StaticResource), hardcoded user-visible string
  literals, IdeMessageBox.Show literals, legacy MessageBox.Show usage
  (ADR-009), and missing LocalizedResourceDictionary wiring (ADR-005).
  All SOURCE findings are warn-only. Distinct from xaml-guard (which only
  checks Designer.cs parity for the base resx). Skip on: Themes/, ColorPicker,
  Designer.cs/g.cs, Tests/, Samples/, pure structural XAML edits.
---

# loc-guard (internal)

56 base `*Resources.resx` files × up to 28 satellites. `xaml-guard` covers base↔Designer.cs; this skill covers base↔satellites (Scope 1) and source advisory (Scope 2).

## When I invoke

### Scope 1 — PARITY

| Situation | Run? |
|---|---|
| Edit a base `*Resources.resx` | yes (scan all satellites) |
| Edit a satellite `*.<lang>.resx` | yes (scan against base only) |

### Scope 2 — SOURCE (advisory)

| Situation | Run? |
|---|---|
| Edit `*.xaml` / `*.cs` under `App/`, `Editors/`, `Plugins/`, `Core/`, `Controls/` | yes |
| Edit `Themes/`, `ColorPicker/`, `*.Designer.cs`, `*.g.cs`, `Tests/`, `Samples/` | no |
| Pure structural edit (Grid.Row, Margin), comment/whitespace only | no |

## Pipeline

**Scope 1:** `scripts/loc-parity.ps1 -Files <paths>`
**Scope 2:** `scripts/loc-source-guard.ps1 -Files <paths>` (always exits 0)
Silence a line: `// loc-ignore: <reason>` (CS) or same-line XML comment (XAML). Tune: `data/allowlist.json`.

## Rules

### Scope 1 — parity (blocking)

| Rule | Severity |
|---|---|
| `satellite-missing-key` | error — key in base, absent from satellite |
| `satellite-orphan-key` | error — key in satellite, absent from base |
| `placeholder-mismatch` | error — format-arg drift (`{0}/{1}` base vs `{0}` sat) |
| `untranslated` | warn — satellite value identical to base (non-en-* only) |
| `satellite-malformed` | error — XML parse fails or root != `<root>` |

### Scope 2 — source (advisory)

| Rule | Detects |
|---|---|
| `loc-static-required` | `{DynamicResource <LocKey>}` → must be `StaticResource` |
| `loc-hardcoded-string` | literal in Text/ToolTip/Header/Content/Title (XAML) or `.Text =`/`.Title =` (CS) |
| `loc-idemessagebox-literal` | `IdeMessageBox.Show("literal", …)` — first arg must be a resource key |
| `loc-messagebox-legacy` | `MessageBox.Show(…)` → use `IdeMessageBox` (ADR-009) |
| `loc-locdict-missing` | assembly has `*Resources.resx` but no `LocalizedResourceDictionary` merge (ADR-005) |

## Output

```
Loc AppResources (28 satellites):
  fr-FR  OK     keys=680
  ja-JP  3 missing-key   APP_NewFile_Title, APP_Recent_Header, PA_FilterTitle
  ar-SA  1 placeholder-mismatch  APP_Status_Loading base="{0}/{1}" sat="{0}"
  ru-RU  124 untranslated  (warn)
```

Language matrix auto-detected per assembly by scanning sibling `<base>.<lang>(-<region>)?.resx` files.
Scope 2 exit 0 (advisory); does not translate, generate, or coordinate sub-agents (see `feedback_localization_agent_strategy`).

## Maintenance

- New language added → auto-picked up.
- Renaming a base resx → update all satellites in same commit (orphan files not detected at filesystem level).
