// ==========================================================
// Project: WpfHexEditor.Plugins.Git
// File: Services/GitVersionControlService.cs
// Description:
//     IVersionControlService implementation using git CLI.
//     Composes GitDiffService (WpfHexEditor.Core.Diff) for repo detection.
//     Polls status every 5 seconds via DispatcherTimer.
// Architecture Notes:
//     Pattern: Composition — wraps GitDiffService, does not inherit it.
//     All StatusChanged events are fired on the UI thread.
//     Blame is cached per (filePath, HEAD commit hash).
// ==========================================================

using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using WpfHexEditor.Core.Diff.Services;
using WpfHexEditor.Editor.Core.LSP;

namespace WpfHexEditor.Plugins.Git.Services;

/// <summary>
/// Git-backed implementation of <see cref="IVersionControlService"/>.
/// Requires git on PATH. Degrades gracefully when git is absent or no repo found.
/// </summary>
internal sealed class GitVersionControlService : IVersionControlService, IDisposable
{
    private const int TimeoutMs    = 8_000;
    private const int PollInterval = 5_000;

    private readonly GitDiffService  _diffService = new();
    private readonly DispatcherTimer _timer;
    private readonly Dispatcher      _dispatcher;

    // Blame cache: keyed by filePath (case-insensitive)
    private readonly Dictionary<string, (string HeadHash, IReadOnlyList<BlameEntry> Entries)>
        _blameCache = new(StringComparer.OrdinalIgnoreCase);

    private string? _repoRoot;
    private string? _branchName;
    private bool    _isDirty;

    public GitVersionControlService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;

        _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(PollInterval)
        };
        _timer.Tick += async (_, _) => await PollStatusAsync();
    }

    // ── IVersionControlService ────────────────────────────────────────────────

    public bool    IsRepo     => _repoRoot is not null;
    public string? BranchName => _branchName;
    public bool    IsDirty    => _isDirty;

    public event EventHandler? StatusChanged;

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await Task.Run(() => RefreshInternal(), ct);
        _dispatcher.Invoke(() => StatusChanged?.Invoke(this, EventArgs.Empty));
    }

    public async Task<IReadOnlyList<BlameEntry>> GetBlameAsync(
        string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) return [];

        var headHash = await Task.Run(() => GetHeadHash(filePath), ct);

        // Return cached results if HEAD hasn't moved
        if (_blameCache.TryGetValue(filePath, out var cached) && cached.HeadHash == headHash)
            return cached.Entries;

        var entries = await Task.Run(() => ParseBlame(filePath), ct);
        _blameCache[filePath] = (headHash ?? string.Empty, entries);
        return entries;
    }

    public async Task<IReadOnlyList<GitChangeEntry>> GetChangedFilesAsync(
        CancellationToken ct = default)
    {
        if (_repoRoot is null) return [];
        return await Task.Run(() => ParseStatus(_repoRoot), ct);
    }

    public async Task<string> GetDiffAsync(string filePath, CancellationToken ct = default)
    {
        if (_repoRoot is null) return string.Empty;
        return await Task.Run(() => RunGit(_repoRoot, $"diff HEAD -- \"{filePath}\""), ct);
    }

    public async Task StageAsync(string filePath, CancellationToken ct = default)
    {
        if (_repoRoot is null) return;
        await Task.Run(() => RunGit(_repoRoot, $"add -- \"{filePath}\""), ct);
        await RefreshAsync(ct);
    }

    public async Task UnstageAsync(string filePath, CancellationToken ct = default)
    {
        if (_repoRoot is null) return;
        await Task.Run(() => RunGit(_repoRoot, $"restore --staged -- \"{filePath}\""), ct);
        await RefreshAsync(ct);
    }

    public async Task DiscardAsync(string filePath, CancellationToken ct = default)
    {
        if (_repoRoot is null) return;
        await Task.Run(() => RunGit(_repoRoot, $"checkout -- \"{filePath}\""), ct);
        await RefreshAsync(ct);
    }

    // ── Polling ───────────────────────────────────────────────────────────────

    public void StartPolling() => _timer.Start();
    public void StopPolling()  => _timer.Stop();

    public void SetActiveFile(string? filePath)
    {
        _repoRoot = filePath is not null ? _diffService.GetRepoRoot(filePath) : null;
        _ = RefreshAsync();
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private async Task PollStatusAsync()
    {
        if (_repoRoot is null) return;
        var (branch, dirty) = await Task.Run(() => ReadBranchAndDirty(_repoRoot));
        if (branch == _branchName && dirty == _isDirty) return;

        _branchName = branch;
        _isDirty    = dirty;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshInternal()
    {
        if (_repoRoot is null) { _branchName = null; _isDirty = false; return; }
        (_branchName, _isDirty) = ReadBranchAndDirty(_repoRoot);
    }

    private (string? branch, bool dirty) ReadBranchAndDirty(string repoRoot)
    {
        var branch = RunGit(repoRoot, "branch --show-current").Trim();
        var status = RunGit(repoRoot, "status --porcelain");
        return (branch.Length > 0 ? branch : null, status.Trim().Length > 0);
    }

    private string? GetHeadHash(string filePath)
    {
        var root = _diffService.GetRepoRoot(filePath);
        if (root is null) return null;
        return RunGit(root, "rev-parse HEAD").Trim();
    }

    private IReadOnlyList<BlameEntry> ParseBlame(string filePath)
    {
        var root = _diffService.GetRepoRoot(filePath);
        if (root is null) return [];

        var raw = RunGit(root, $"blame --porcelain \"{filePath}\"");
        if (string.IsNullOrWhiteSpace(raw)) return [];

        var result  = new List<BlameEntry>();
        var commits = new Dictionary<string, (string author, DateTime date, string msg)>(StringComparer.Ordinal);
        var lines   = raw.Split('\n');

        string? currentHash   = null;
        int     currentLine   = 0;
        string  currentAuthor = string.Empty;
        DateTime currentDate  = default;
        string  currentMsg    = string.Empty;

        foreach (var line in lines)
        {
            if (line.Length >= 40 && line[39] == ' ' && IsHex(line, 40))
            {
                // Header line: <hash> <orig-line> <final-line> [<num-lines>]
                currentHash = line[..40];
                var parts = line.Split(' ');
                if (parts.Length >= 3 && int.TryParse(parts[2], out var ln))
                    currentLine = ln;

                if (commits.TryGetValue(currentHash, out var cached))
                {
                    (currentAuthor, currentDate, currentMsg) = cached;
                }
            }
            else if (line.StartsWith("author ", StringComparison.Ordinal))
            {
                currentAuthor = line[7..];
            }
            else if (line.StartsWith("author-time ", StringComparison.Ordinal)
                     && long.TryParse(line[12..], out var ts))
            {
                currentDate = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime;
            }
            else if (line.StartsWith("summary ", StringComparison.Ordinal))
            {
                currentMsg = line[8..];
                if (currentHash is not null)
                    commits[currentHash] = (currentAuthor, currentDate, currentMsg);
            }
            else if (line.StartsWith("\t", StringComparison.Ordinal) && currentHash is not null)
            {
                // Source line — emit the entry
                result.Add(new BlameEntry(currentLine, currentHash, currentAuthor, currentDate, currentMsg));
            }
        }

        return result;
    }

    private IReadOnlyList<GitChangeEntry> ParseStatus(string repoRoot)
    {
        var raw = RunGit(repoRoot, "status --porcelain");
        if (string.IsNullOrWhiteSpace(raw)) return [];

        var result = new List<GitChangeEntry>();
        foreach (var line in raw.Split('\n'))
        {
            if (line.Length < 4) continue;
            var xy   = line[..2];
            var path = line[3..].Trim().Trim('"');

            var kind = xy switch
            {
                " M" or "MM" => GitChangeKind.Modified,
                "M " or "A " => GitChangeKind.Staged,
                " A"         => GitChangeKind.Added,
                " D" or "D " => GitChangeKind.Deleted,
                "R " or " R" => GitChangeKind.Renamed,
                "??"         => GitChangeKind.Untracked,
                _            => GitChangeKind.Modified
            };
            result.Add(new GitChangeEntry(path, kind));
        }
        return result;
    }

    private string RunGit(string workingDir, string args)
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo("git", args)
                {
                    WorkingDirectory       = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                }
            };
            proc.Start();
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(TimeoutMs);
            return output;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool IsHex(string s, int len)
    {
        for (var i = 0; i < Math.Min(len, s.Length); i++)
            if (!Uri.IsHexDigit(s[i])) return false;
        return true;
    }

    public void Dispose()
    {
        _timer.Stop();
        _blameCache.Clear();
    }
}
