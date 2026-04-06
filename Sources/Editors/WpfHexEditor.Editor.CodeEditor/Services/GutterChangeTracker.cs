// ==========================================================
// Project: WpfHexEditor.Editor.CodeEditor
// File: Services/GutterChangeTracker.cs
// Description:
//     Tracks per-line change state (Added / Modified / Deleted / None) relative
//     to the last file save-point.  Uses O(n) hash comparison — no Myers diff.
//
//     Performance design:
//       - Hot path (every keystroke / undo): Invalidate() restarts the debounce timer.
//         Zero allocations — no line list created on each keystroke.
//       - Debounce tick: reads _linesProvider() once, computes the hash diff.
//       - MarkSavePoint: reads provider, snapshots hashes, clears markers immediately.
// Architecture:
//     Standalone service; no WPF controls dependency; testable in isolation.
// ==========================================================

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows.Threading;
using WpfHexEditor.Editor.CodeEditor.Models;

namespace WpfHexEditor.Editor.CodeEditor.Services;

/// <summary>
/// Computes per-line <see cref="LineChangeKind"/> relative to the last save-point.
/// </summary>
internal sealed class GutterChangeTracker
{
    // ── Constants ────────────────────────────────────────────────────────────

    private const int DebounceMs = 600;

    // ── State ─────────────────────────────────────────────────────────────────

    private ImmutableArray<int>          _savedHashes = ImmutableArray<int>.Empty;
    private Func<IReadOnlyList<string>>? _linesProvider;
    private readonly DispatcherTimer     _debounce;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Raised on the UI thread after the debounce fires and the map is rebuilt.</summary>
    internal event EventHandler<IReadOnlyDictionary<int, LineChangeKind>>? Changed;

    internal GutterChangeTracker()
    {
        _debounce       = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DebounceMs) };
        _debounce.Tick += OnDebounceTick;
    }

    /// <summary>
    /// Provides the delegate used to read current document lines on the debounce tick.
    /// Must be set before the first call to <see cref="Invalidate"/>.
    /// </summary>
    internal void SetLinesProvider(Func<IReadOnlyList<string>> provider)
        => _linesProvider = provider;

    /// <summary>
    /// Snapshots current lines as the save baseline and clears all markers immediately.
    /// Safe to call from any path that represents a "clean" document state (open / save).
    /// </summary>
    internal void MarkSavePoint()
    {
        _debounce.Stop();
        if (_linesProvider is not null)
            _savedHashes = BuildHashes(_linesProvider());
        Changed?.Invoke(this, new Dictionary<int, LineChangeKind>());
    }

    /// <summary>
    /// Schedules a debounced recompute.
    /// Call on every document change (keystroke, undo, redo, paste…).
    /// Zero allocations — just restarts the timer.
    /// </summary>
    internal void Invalidate()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void OnDebounceTick(object? sender, EventArgs e)
    {
        _debounce.Stop();
        if (_linesProvider is null) return;

        var map = ComputeChanges(_linesProvider(), _savedHashes);
        Changed?.Invoke(this, map);
    }

    /// <summary>
    /// Computes the change map by comparing current line hashes to the saved snapshot.
    /// O(n) — result dictionary is the only allocation.
    /// </summary>
    internal static IReadOnlyDictionary<int, LineChangeKind> ComputeChanges(
        IReadOnlyList<string> currentLines,
        ImmutableArray<int>   savedHashes)
    {
        var result = new Dictionary<int, LineChangeKind>();

        int curCount   = currentLines.Count;
        int savedCount = savedHashes.Length;

        for (int i = 0; i < curCount; i++)
        {
            if (i >= savedCount)
                result[i] = LineChangeKind.Added;
            else if (HashLine(currentLines[i]) != savedHashes[i])
                result[i] = LineChangeKind.Modified;
        }

        // Deletion hint on the last existing line when the snapshot had more lines.
        if (savedCount > curCount)
        {
            int pred = Math.Max(0, curCount - 1);
            if (!result.ContainsKey(pred))
                result[pred] = LineChangeKind.Deleted;
        }

        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ImmutableArray<int> BuildHashes(IReadOnlyList<string> lines)
    {
        var b = ImmutableArray.CreateBuilder<int>(lines.Count);
        foreach (var line in lines)
            b.Add(HashLine(line));
        return b.MoveToImmutable();
    }

    private static int HashLine(string text)
        => text.GetHashCode(StringComparison.Ordinal);
}
