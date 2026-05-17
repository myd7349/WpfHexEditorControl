---
name: code-analysis
description: |
  INTERNAL DEV WORKFLOW for WpfHexEditor — Claude self-invokes this skill BEFORE
  any non-trivial edit AND AFTER multi-file changes, to enforce CLAUDE.md rules
  (functions <=25 lines, classes <=300, 1-file-1-responsibility, module
  boundaries, forbidden patterns). Auto-trigger on: edits >50 lines, new files,
  refactors, module-crossing changes, plugin/SDK touches, performance-sensitive
  paths (HexEditor buffer, CodeEditor render, viewport, drag/scroll), Roslyn
  diagnostics work. Skip on: doc-only, resx-only, comment fixes, single-line bug
  fixes, single-file <20-line edits.
---

# code-analysis (internal)

Pre/post guard for CLAUDE.md thresholds and forbidden patterns. Not a replacement for the in-app `CodeAnalysisRunner` (deep Roslyn/Halstead).

## When I invoke

| Situation | Phases |
|---|---|
| Non-trivial edit, 2+ modules, new file | P1, P3 |
| HexEditor/ or CodeEditor/ render/buffer, plugin/SDK | P1, P3 (mandatory) |
| Multi-file completion | P3 |
| Single-file <20 lines, no new symbol, resx/md/comment only | skip |

## Phases

**P1 — pre-edit** `scripts/pre-edit-check.ps1 -File <path>`
→ `LOC=X | Funcs>25=N | Class>300=M | Callers=K | Module=<id> | Risk=LOW|MED|HIGH`
HIGH risk → propose PLAN, do not edit.

**P2 — during edit (mental checklist from `references/forbidden-patterns.md`)**
Refuse: `using ICSharpCode`/AvalonEdit, `MessageBox.Show(` (use `IDialogService`), `.md` in `Sources/`, hardcoded user-strings in XAML/VMs, `Resources.X` in HexEditor partials (use `L10n`), background fix without root cause, new mutable static on hot paths.

**P3 — post-edit** `scripts/post-edit-audit.ps1 -Files <paths> -Baseline HEAD`
→ `OK` or `VIOLATIONS: <list>`
VIOLATIONS ≤2 mechanical → fix immediately. VIOLATIONS large/architectural → stop, propose PLAN.

**Scope-impact (on demand):** `scripts/scope-impact.ps1 -Symbol <name>` — enumerate referencing files (cap 50, depth 2) before public symbol rename/move.

## Performance budget

P1 ≤150 tokens | P3 ≤200 tokens | scope = changed files only | cache valid <5 min + unchanged mtime.

## Failure modes to avoid

| Mode | Guard |
|---|---|
| Sur-déclenchement on typo fixes | Honor skip rules above |
| Cache staleness | P3 must not use P1 cache for a just-edited file |
| PowerShell edition mismatch | Scripts exit with clear message; fall back to Grep/Read |

Does not replace `CodeAnalysisRunner`, modify source, run `dotnet build`, or invoke external services.
