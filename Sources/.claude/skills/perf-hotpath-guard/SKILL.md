---
name: perf-hotpath-guard
description: |
  INTERNAL DEV WORKFLOW for WpfHexEditor — Claude self-invokes after editing
  C# in render/layout/scroll hot paths (HexEditor/, CodeEditor/Rendering*,
  *Viewport*, *GlyphRun*, *Renderer*, *ScrollHandler*, OnRender,
  MeasureOverride, ArrangeOverride) to detect allocations and patterns that
  cause jank or GC pressure. Skip on: tests, mocks, designer files, non-render
  code in non-hot directories.
---

# perf-hotpath-guard (internal)

Catches perf anti-patterns from project history (jank, GC pressure in render loops). Memory anchors: `adr_006_zoom_rendering`, `adr_hexeditor_viewport_race`, `bug_drag_scroll_fix`, `feedback_wordwrap_column`, `bug_doc_editor_caret_hittest`.

## When I invoke

Trigger: `.cs` edits matching ANY of:
- Path under `HexEditor/` or `CodeEditor/Rendering`
- Filename contains `Viewport`, `GlyphRun`, `Renderer`, `ScrollHandler`
- Edit modifies `OnRender`, `MeasureOverride`, or `ArrangeOverride`

Skip if path contains `Test`, `Mock`, `*.Designer.cs`, `*.g.cs`, `*.g.i.cs`.

## Pipeline

`scripts/hotpath-scan.ps1 -Files <paths>` (applies rules inside hot-method bodies only) → `HotPath: <n> issues` or `OK`.

## 9 rules

See `references/perf-rules.md` for regex + rationale.

| Rule | Anchor |
|---|---|
| `alloc-in-render` | GC pressure/jank |
| `linq-in-hot` | allocations + delegate creation |
| `string-concat-loop` | O(n²) allocations |
| `boxing` | struct→object passing |
| `dispatcher-cosmetic` | `feedback_wordwrap_column` anti-pattern |
| `formatted-text-recreate` | `bug_doc_editor_caret_hittest` |
| `glyph-run-recreate` | render cost without pool |
| `findvisualchild-render` | O(n) tree walk every frame |
| `regex-in-render` | compile cost on hot path |

**Hot methods** (from `references/hot-methods.md`): `OnRender`, `MeasureOverride`, `ArrangeOverride`, `OnPreviewMouseMove`, `OnScrollChanged`, `Render*`/`Draw*`/`Paint*` in renderer files, any method preceded by `// HOT`. Exclude with `// COLD`.

## Output

```
HotPath: 2 issues
  HexLineRenderer.cs:142  alloc-in-render         OnRender -> new SolidColorBrush(...)
  TextViewport.cs:88      formatted-text-recreate OnRender -> new FormattedText(...)
```

Suppress: `// alloc-ok: <reason>` (alloc-in-render), `// COLD` (exclude method), `// HOT-EXCEPTION: <note>` (document legitimate exception).
Static analysis only — no runtime profiling, no code rewrite.
