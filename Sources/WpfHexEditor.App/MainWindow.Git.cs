// ==========================================================
// Project: WpfHexEditor.App
// File: MainWindow.Git.cs
// Description:
//     Git / VCS status bar wiring for MainWindow.
//     Subscribes to GitStatusChangedEvent and GitBlameLoadedEvent
//     published by WpfHexEditor.Plugins.Git (fully decoupled).
// Architecture Notes:
//     Pattern: identical to MainWindow.Debug.cs — subscribe on bus,
//     update XAML controls on Dispatcher.
// ==========================================================

using WpfHexEditor.Core.Events.IDEEvents;
using WpfHexEditor.Editor.Core.LSP;

namespace WpfHexEditor.App;

public partial class MainWindow
{
    // ── Initialization ────────────────────────────────────────────────────────

    private void InitializeGitIntegration()
    {
        if (_ideEventBus is null) return;

        _ideEventBus.Subscribe<GitStatusChangedEvent>(e =>
        {
            Dispatcher.InvokeAsync(() => UpdateGitStatusBar(e));
            return System.Threading.Tasks.Task.CompletedTask;
        });

        _ideEventBus.Subscribe<GitBlameLoadedEvent>(e =>
        {
            Dispatcher.InvokeAsync(() => OnGitBlameLoaded(e));
            return System.Threading.Tasks.Task.CompletedTask;
        });
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private void UpdateGitStatusBar(GitStatusChangedEvent e)
    {
        if (e.Branch is null)
        {
            GitStatusItem.Visibility = System.Windows.Visibility.Collapsed;
            return;
        }

        GitBranchText.Text      = e.Branch;
        GitDirtyDot.Visibility  = e.IsDirty
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
        GitStatusItem.Visibility = System.Windows.Visibility.Visible;
    }

    private void OnGitBlameLoaded(GitBlameLoadedEvent e)
    {
        // Blame data is already cached in GitVersionControlService.
        // The active CodeEditor will pull it via IDEHostContext.VersionControl.GetBlameAsync
        // when the user toggles ShowBlameGutter — nothing to do here from the App layer.
        _ = e;
    }
}
