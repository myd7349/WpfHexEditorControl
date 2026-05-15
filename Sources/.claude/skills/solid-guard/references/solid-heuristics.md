# SOLID heuristics

All findings warn-only. Suppress with `// solid-ok: <reason>`. New exemption path → edit `solid-scan.ps1`.

## 6 heuristics

| Rule | Detection | Correlates with |
|---|---|---|
| `srp-mixed-concerns` | class touches `System.IO`/`File.*` AND `System.Windows.*` | Class glues IO to UI without Adapter |
| `srp-class-too-broad` | >300 lines AND >15 public methods | Likely doing too much — split candidates |
| `ocp-massive-switch` | switch with >10 cases on a type/enum | Adding a case modifies the class — use polymorphism |
| `dip-newing-services` | `new XService(` outside Factory/composition-root | Binds to concrete impl — inject instead |
| `dip-static-deps` | `<Type>.Instance.<Member> = …` | Mutable singleton — hidden global state |
| `isp-fat-interface` | interface >10 members OR `throw new NotImplementedException` | Implementations forced to provide unrelated members |

## Not detected

LSP (requires semantic analysis), cross-file SRP, DI container misuse, implicit static deps via static methods.

## Memory anchors

- ADR-009 — `IDialogService` injection via `IDEHostContext.Dialogs`: example of correct DIP.
- ADR-010/011 — module integration via SDK extensibility, not direct `new`.

When a new ADR documents a SOLID-relevant decision, update the exemption regex in `solid-scan.ps1` and add it here.
