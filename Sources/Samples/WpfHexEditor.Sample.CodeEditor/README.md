# WpfHexEditor.Sample.CodeEditor

**Type:** Standalone WPF Application (`net8.0-windows`)
**Role:** Minimal demonstration of the `CodeEditor` control in a standalone window with full toolbar, menu, and status bar.

---

## Purpose

Shows how to host `CodeEditorSplitHost` in a standalone WPF app without the full IDE docking system. Covers:

- File operations (New / Open / Save / Save As)
- Language switching at runtime
- Dark / Light theme switching (F1 / F2 or toolbar toggle)
- Find / Replace panel
- Status bar with language · line/column · encoding · zoom · dirty indicator

---

## Key Classes

| Class | Responsibility |
|-------|---------------|
| `App` | Application entry + `App.SwitchTheme(Uri)` static helper |
| `MainWindow` | File operations, editor event forwarding, keyboard shortcuts |
| `MainViewModel` | Code editor state and commands (MVVM) |

---

## Layout

```
Window
 ├─ MenuBar (File / Edit / View / Find / Language / Help)
 ├─ Toolbar (file ops · find/replace · language selector · theme toggle)
 ├─ CodeEditorSplitHost (fills remaining area)
 └─ StatusBar (Language · Line:Col · Encoding · Zoom · Dirty)
```

---

## Dependencies

| Project | Role |
|---------|------|
| `WpfHexEditor.Editor.CodeEditor` | Code editor control |
| `WpfHexEditor.Shell` | Theme ResourceDictionaries |
