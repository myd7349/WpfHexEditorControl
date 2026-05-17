---
name: whfmt-guard
description: |
  INTERNAL DEV WORKFLOW for WpfHexEditor — Claude self-invokes after editing
  any *.whfmt under Core/WpfHexEditor.Core.Definitions/FormatDefinitions/ or
  the whfmt.schema.json. Domain checks NOT covered by build gate
  EmbeddedWhfmt_Tests (parse-only) or nuget-guard (public API): JSONC parse
  with /* */ header tolerated (per feedback_whfmt_guard), version monotone
  vs git HEAD (per feedback_whfmt_version), required schema fields
  (formatName, formatId, extensions, category, description), formatId
  uniqueness across catalog, magic-signature collisions, detection.strength
  enum (catches typos before SignatureStrength converter crash —
  bug_signaturestrength_converter), and placeholder/variables drift.
  Skip on: files outside FormatDefinitions/, Tests/, Samples/, doc-only or
  comment-only edits.
---

# whfmt-guard (internal)

Guards `.whfmt` file content. Complements `nuget-guard` (C# API) and `EmbeddedWhfmt_Tests` build gate (parse-only).

## When I invoke

| Situation | Run? |
|---|---|
| Edit `**/FormatDefinitions/**/*.whfmt` or `**/whfmt.schema.json` | yes |
| Edit `.whfmt` under `Tests/`/`Samples/`, doc/comment only, `.cs` (nuget-guard) | no |

## Pipeline

1. Filter to `.whfmt` under `Core/WpfHexEditor.Core.Definitions/FormatDefinitions/`. Empty → exit 0.
2. Load catalog index once for cross-file checks (R4, R5).
3. Run R1→R9 per changed file. Exit code = ERR count. WARN-only ⇒ exit 0.
4. Suppress: `// whfmt-ignore: <reason>` as JSONC line comment (R1 still parses).

## Rules

| Rule | Sev | Detects |
|---|---|---|
| `whfmt-jsonc-parse` | error | JSONC parse failure; `/* */` header tolerated |
| `whfmt-version-monotone` | error | `version` < git HEAD version; new files exempt |
| `whfmt-schema-required` | error | Missing: formatName/formatId/extensions(≥1)/category/description |
| `whfmt-id-uniqueness` | error | formatId collision across catalog (case-insensitive) |
| `whfmt-magic-collision` | warn | Same sig+offset+ext overlap; silence by adding `detection.validation.note` |
| `whfmt-strength-enum` | warn | `detection.strength` ∉ {None, Weak, Medium, Strong, VeryStrong} |
| `whfmt-placeholder-drift` | warn | `{{var}}` in description/blocks but `var` not in `variables{}` |
| `whfmt-expression-refs` | warn | Identifier in assertions/blocks/forensic expressions not in variables{}/functions{}/built-ins |
| `whfmt-enum-values` | warn | Unrecognized value in closed-set fields: blocks[].type (15 types), blocks[].valueType, assertions[].severity, detection.matchMode, fuzz.strategies[].mutation, repair[].action, forensic.riskLevel, etc. |

**Known deliberate gaps** (dual-shape fields, no false-positive–free rule possible):
- `references` — 24% flat array / 76% named-bucket object; typos in bucket keys silently hide entries in IDE.
- `formatRelationships` — same dual shape (28%/72%).
- `syntaxDefinition.rules[].colorKey` — parsed by DTO but ignored at runtime.

Does not validate block offsets (`whfmt.Validate`), run fuzz (`whfmt.Fuzz`), or check schema JSON itself.

## Maintenance

- New required schema field → add to `whfmt-schema-required` list.
- New `SignatureStrength` enum value → update `whfmt-strength-enum` allow-list + C# enum in lockstep.
