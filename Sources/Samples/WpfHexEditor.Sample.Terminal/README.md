# WpfHexEditor.Sample.Terminal

**Type:** Standalone WPF Application (`net8.0-windows`)
**Role:** Minimal standalone demonstration of the `WpfHexEditor.Terminal` panel with multi-tab sessions and theme switching.

---

## Purpose

Shows how to host `TerminalPanelViewModel` + `TerminalPanel` in a standalone WPF app without the full IDE. Covers:

- Multi-tab terminal sessions (HxTerminal / PowerShell / Bash)
- Runtime Dark / Light theme switching (F1 / F2 or View menu)
- Shell mode selection via Terminal menu
- Status bar showing current shell mode (`● HxTerminal`)
- Null-object pattern for IDE host context

---

## Key Classes

| Class | Responsibility |
|-------|---------------|
| `App` | Application entry + `App.SwitchTheme(Uri)` static helper |
| `MainWindow` | `TerminalPanelViewModel` wiring, shell-mode menu sync, theme handlers, status bar update |
| `StandaloneIDEHostContext` | Null-object `IIDEHostContext` with 12 no-op service stubs (allows terminal initialization without full IDE) |

---

## Layout

```
Window
 ├─ MenuBar (File / View / Terminal / Help)
 ├─ TerminalPanel (fills remaining area — multi-tab sessions)
 └─ StatusBar ("● HxTerminal / PowerShell / Bash")
```

---

## Architecture Notes

- `DataContext` set **before** `InitializeComponent()` — tabs render correctly on first paint
- `StandaloneIDEHostContext` uses C# `file` modifier for nested service stubs (no namespace pollution)
- Theme switching replaces ResourceDictionary by Source URI containing `"Theme"`

---

## Dependencies

| Project | Role |
|---------|------|
| `WpfHexEditor.Terminal` | Terminal panel control + ViewModel |
