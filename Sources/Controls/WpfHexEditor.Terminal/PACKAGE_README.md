# WpfTerminal

A standalone WPF terminal emulator UserControl for .NET 8.

```
dotnet add package WpfTerminal
```

## What's New in 0.9.6.2

- **Standalone**: WpfTerminal is now a fully self-contained NuGet package. Core, Core.Events, and SDK assemblies are bundled — no dependency on the WpfHexEditorIDE host is required.

## Quick Start

```xml
<Window xmlns:term="clr-namespace:WpfHexEditor.Terminal;assembly=WpfHexEditor.Terminal">
    <term:TerminalPanel x:Name="Terminal" />
</Window>
```

```csharp
// Add a new PowerShell session
Terminal.ViewModel.AddSession("PowerShell", TerminalShellType.PowerShell);

// Execute a command programmatically
await Terminal.ViewModel.ActiveSession.ExecuteCommandAsync("Get-Process");
```

## Features

- **Multi-tab Shell Sessions** — cmd, PowerShell, bash, Git Bash
- **39 Built-in Commands** — file I/O, navigation, hex access, solution, plugins, diagnostics
- **Macro Recording/Replay** — record command sequences, replay with variables
- **HxScript Scripting** — lightweight `.hxscript` scripting engine for automation
- **Command History** — per-session history with up/down navigation
- **Find in Output** — search through terminal output with highlight
- **Export** — save output to text or HTML
- **Themeable** — uses WPF DynamicResources for seamless dark/light themes
- **MVVM Architecture** — clean separation of UI and logic

## Built-in Commands (selection)

| Command | Description |
|---------|------------|
| `cd <path>` | Change directory |
| `ls` / `dir` | List files |
| `cat <file>` | Display file content |
| `find <pattern>` | Search in files |
| `clear` | Clear output |
| `macro record` | Start recording |
| `macro play` | Replay recorded macro |
| `help` | List all commands |

## Included Assemblies

All bundled inside the package — zero external NuGet dependencies:

| Assembly | Purpose |
|----------|---------|
| WpfHexEditor.Terminal | WPF terminal panel (MVVM, main entry point) |
| WpfHexEditor.Core.Terminal | Command engine, macros, HxScript scripting |
| WpfHexEditor.SDK.Terminal.Abstractions | Terminal plugin contracts |
| WpfHexEditor.Core | Shared services and infrastructure |
| WpfHexEditor.Core.Events | Event bus |
| WpfHexEditor.SDK | Plugin contracts and interfaces |

**Localizations**: ar-SA, de-DE, es-419, es-ES, fr-CA, fr-FR, hi-IN, it-IT, ja-JP, ko-KR, nl-NL, pl-PL, pt-BR, pt-PT, ru-RU, sv-SE, tr-TR, zh-CN

## License

GNU AGPL v3.0 — [GitHub Repository](https://github.com/abbaye/WpfHexEditorIDE)
