---
name: solid-guard
description: |
  INTERNAL DEV WORKFLOW for WpfHexEditor — Claude self-invokes after creating
  new C# files OR adding >50 lines to existing classes (especially under
  Services/, Managers/, *Manager.cs, *Service.cs). Heuristics that
  CORRELATE with SOLID violations: SRP (mixed UI+IO concerns, classes
  >300 lines + >15 public methods), OCP (massive switch statements >10
  cases), DIP (newing services instead of injecting, static singleton
  mutation), ISP (fat interfaces, NotImplementedException stubs). LSP is
  out of scope (impossible to detect mechanically). All findings are
  ADVISORY (warn-only). Skip on: Tests/, Samples/, *.Designer.cs, *.g.cs.
---

# solid-guard (internal)

**ADVISORY only.** Heuristics that correlate with SOLID violations — not formal proofs. False positives are expected.

## When I invoke

| Situation | Run? |
|---|---|
| New `.cs` file, edit adds >50 lines, edit under `Services/`/`Managers/`/`*Manager.cs`/`*Service.cs` | yes |
| `Tests/`, `Samples/`, `*.Designer.cs`, `*.g.cs`, comment/rename only | no |

## Pipeline

`scripts/solid-scan.ps1 -Files <paths>` → `SOLID: <summary>` or `OK` + per-issue lines.

## 6 heuristics (all warn-only)

| Rule | Detected via | Letter |
|---|---|---|
| `srp-mixed-concerns` | class touches `System.IO`/`File.*` AND `System.Windows.*` | S |
| `srp-class-too-broad` | class >300 lines AND >15 public methods | S |
| `ocp-massive-switch` | `switch` with >10 cases on a type/enum | O |
| `dip-newing-services` | `new XService(`/`new XManager(` outside a Factory | D |
| `dip-static-deps` | `<Type>.Instance.<Member> = …` (mutable singleton write) | D |
| `isp-fat-interface` | interface >10 members OR `NotImplementedException` stubs | I |

LSP omitted — requires Roslyn semantic analysis. Full heuristic detail: `references/solid-heuristics.md`.

## Suppressions

`// solid-ok: <reason>` silences a rule on that line. Auto-exempt: `*Factory*` files from `dip-newing-services`; `*FileWatcher*`/`*FileMonitor*` from `srp-mixed-concerns`.

**Legitimate firing patterns:**

| Rule | Legitimate case | Annotation |
|---|---|---|
| `srp-mixed-concerns` | `*Adapter.cs` bridges IO→UI Dispatcher | `// solid-ok: bridge adapter` |
| `dip-newing-services` | composition root (App.xaml.cs, `*Module.cs`) | rename to `*Factory` or annotate |
| `ocp-massive-switch` | closed dispatch in parser/interpreter | `// solid-ok: closed enum dispatch` |

## Output

```
SOLID: 1 srp-mixed-concerns, 2 dip-newing-services, 1 isp-fat-interface
  FileMonitorService.cs:1   srp-mixed-concerns  uses File.* AND System.Windows.Threading
  CodeAnalysisRunner.cs:42  dip-newing-services new RoslynDiagnosticsCollector() — inject?
  IDocumentService.cs:14    isp-fat-interface   14 members (consider splitting)
```

## Maintenance

New heuristic → row in `references/solid-heuristics.md` + extend `$rules` in `solid-scan.ps1`.
