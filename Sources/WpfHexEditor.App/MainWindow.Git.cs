// ==========================================================
// Project: WpfHexEditor.App
// File: MainWindow.Git.cs
// Description:
//     Git / VCS status bar wiring for MainWindow.
//     Subscribes to GitStatusChangedEvent, GitBlameLoadedEvent,
//     GitOperationStartedEvent, GitOperationCompletedEvent.
//     Branch button click → BranchPickerPopup.
// Architecture Notes:
//     Pattern: identical to MainWindow.Debug.cs — subscribe on bus,
//     update XAML controls on Dispatcher.
// ==========================================================

using WpfHexEditor.Core.Events.IDEEvents;

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

        _ideEventBus.Subscribe<GitOperationStartedEvent>(e =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                GitOperationStatusText.Text       = $"Git: {e.OperationName}…";
                GitOperationStatusText.Visibility = System.Windows.Visibility.Visible;
            });
            return System.Threading.Tasks.Task.CompletedTask;
        });

        _ideEventBus.Subscribe<GitOperationCompletedEvent>(e =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (e.Success)
                {
                    GitOperationStatusText.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    GitOperationStatusText.Text       = $"Git {e.OperationName} failed";
                    GitOperationStatusText.Visibility = System.Windows.Visibility.Visible;
                }
            });
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
        // CodeEditor BlameGutterControl will pull it on next SetContext call.
        _ = e;
    }

    private void OnGitBranchButtonClick(object sender, System.Windows.RoutedEventArgs e)
    {
        // Publish event — GitPlugin subscribes and shows BranchPickerPopup
        // (fully decoupled: App cannot reference Git plugin types directly)
        _ideEventBus?.Publish(new GitBranchClickRequestedEvent(GitBranchButton));
    }
}
