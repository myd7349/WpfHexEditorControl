---
name: leak-guard
description: |
  INTERNAL DEV WORKFLOW for WpfHexEditor — Claude self-invokes after editing
  C# files that touch IDisposable, events, FileStream, Process, Timer, or
  files under Services/, *Manager.cs, *Service.cs, *Watcher*, *Adapter.cs.
  Detects: event handlers added without unsubscribe, IDisposable without
  Dispose, missing GC.SuppressFinalize, FileStream without using, Timer not
  stopped, Process not disposed, static event collections, weak-event
  candidates, hardcoded secrets. Skip on: Tests/, Samples/, *.Designer.cs,
  *.g.cs, generated code.
---

# leak-guard (internal)

Static guard for memory/handle/event leaks and secret hygiene (75+ IDisposable implementations, lots of file watching, long-lived LSP handlers).

## When I invoke

| Situation | Run? |
|---|---|
| Edit `.cs` with `IDisposable`/`event`/`+=`, or `FileStream`/`File.Open`/`Process`/`Timer` | yes |
| Edit under `Services/`, `*Manager.cs`, `*Service.cs`, `*Watcher*`, `*Adapter.cs` | yes |
| `Tests/`, `Samples/`, `*.Designer.cs`, `*.g.cs`, rename/comment only | no |

## Pipeline

`scripts/leak-scan.ps1 -Files <paths>` → `Leaks: <summary>` or `OK` + per-issue lines.
Suppress: `// leak-ok: <reason>` on the offending line.

## 9 rules

| Rule | Sev | Detected via |
|---|---|---|
| `event-no-unsubscribe` | warn | `+=` on event without matching `-=` in `Dispose`/`Unloaded` of same class |
| `idisposable-no-dispose` | error | `: IDisposable` without `Dispose()` method body |
| `dispose-no-suppress-finalize` | warn | `~Class()` finalizer + `Dispose()` lacks `GC.SuppressFinalize(this)` |
| `filestream-no-using` | warn | `new FileStream(`/`File.Open(` not in `using`, not a field |
| `timer-no-stop` | warn | `new (DispatcherTimer\|Timer\|System.Timers.Timer)(` field without `Stop()`/`Dispose()` reference |
| `process-no-dispose` | warn | `Process.Start`/`new Process(` without `using`, result assigned to local |
| `static-event-collection` | error | `public static event` OR `static List/Dict/HashSet` mutated by instance methods |
| `weak-event-candidate` | warn | `+=` on `Application.Current.*`, `Dispatcher.*`, or `*.Instance.*` from `UserControl`/`Window` |
| `secret-in-source` | error | `(api[_-]?key\|password\|secret\|token)\s*=\s*"[A-Za-z0-9+/=]{16,}"` |

`secret-in-source` skips `// fixture` lines and `Tests/Fixtures/`. Full rule detail: `references/leak-rules.md`.

## Output

```
Leaks: 2 event-no-unsubscribe, 1 timer-no-stop | 0 secrets
  FileMonitorService.cs:42        event-no-unsubscribe  _watcher.Changed += OnFileChanged (no -= in Dispose)
  HighlightPipelineService.cs:128 timer-no-stop         DispatcherTimer field never .Stop()
```

Static single-file analysis only. No runtime profiling, code rewrite, or cross-file tracking.

## Maintenance

New leak pattern → row in `references/leak-rules.md` + extend `$rules` in `leak-scan.ps1`.
New long-lived host singleton → add to `weak-event-candidate` source list in script.
