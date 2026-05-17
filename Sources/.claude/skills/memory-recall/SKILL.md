---
name: memory-recall
description: |
  INTERNAL DEV WORKFLOW for WpfHexEditor — at the start of a new task turn,
  Claude self-invokes to surface relevant memory entries based on keywords in
  the user prompt (WebView2, dock, tab, XAML patcher, loc, resx, viewport,
  drag/scroll, etc.). Prevents re-discovering documented bugs and re-making
  the mistakes captured in feedback_* memories. Skip on: continuation of an
  in-progress task in the same turn, purely social messages, single-line
  follow-ups.
---

# memory-recall (internal)

Surfaces 1-3 relevant memory IDs from the 40+ entries before planning work.

## When I invoke

| Situation | Run? |
|---|---|
| First message of a new task, user mentions known domain | yes |
| Continuation of same task, pure-conversational reply, single-line follow-up | no |

## Pipeline

1. Extract keywords from prompt (lowercase, no diacritics, ≥3 chars).
2. `scripts/recall.ps1 -Keywords "<words...>"` — greps `data/keyword-map.json` for substring matches, returns top-3 by hit-frequency.
3. Read those memory files BEFORE proposing a PLAN or editing code. Skip silently if empty.

Output: `Recall: <id1>, <id2>, <id3>` (≤80 tokens). Missing file → `(warn: <id>)`.

Does not read memory contents (just identifies), mutate memory, or trigger on tool results.

## Maintenance

`data/keyword-map.json` — 2-4 keyword aliases per memory ID (FR + EN variants). Keep under 200 entries; split by domain if it grows past that.
