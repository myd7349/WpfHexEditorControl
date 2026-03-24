// ==========================================================
// Project: WpfHexEditor.Plugins.UnitTesting
// File: ViewModels/UnitTestingViewModel.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-23
// Description:
//     ViewModel for the Unit Testing Panel. Holds the test result list,
//     counters, status text, and commands (Run, Stop, Clear).
// ==========================================================

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WpfHexEditor.Plugins.UnitTesting.Models;

namespace WpfHexEditor.Plugins.UnitTesting.ViewModels;

/// <summary>
/// MVVM ViewModel for the UnitTestingPanel.
/// </summary>
public sealed class UnitTestingViewModel : INotifyPropertyChanged
{
    // ── Observable results ───────────────────────────────────────────────────

    private readonly ObservableCollection<TestResultRow> _results = [];
    public IReadOnlyList<TestResultRow> Results => _results;

    // ── Status ───────────────────────────────────────────────────────────────

    private string _statusText = "Ready";
    private bool   _isRunning;
    private int    _passCount;
    private int    _failCount;
    private int    _skipCount;

    public string StatusText
    {
        get => _statusText;
        set => Set(ref _statusText, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set => Set(ref _isRunning, value);
    }

    public int PassCount { get => _passCount; set => Set(ref _passCount, value); }
    public int FailCount { get => _failCount; set => Set(ref _failCount, value); }
    public int SkipCount { get => _skipCount; set => Set(ref _skipCount, value); }

    // ── Mutation helpers ─────────────────────────────────────────────────────

    /// <summary>Clears all results and resets counters.</summary>
    public void Reset()
    {
        _results.Clear();
        PassCount  = FailCount = SkipCount = 0;
        StatusText = "Ready";
        IsRunning  = false;
    }

    /// <summary>Appends a batch of results and updates counters.</summary>
    public void AddResults(IEnumerable<TestResult> results)
    {
        foreach (var r in results)
        {
            _results.Add(new TestResultRow(r));
            switch (r.Outcome)
            {
                case TestOutcome.Passed:  PassCount++; break;
                case TestOutcome.Failed:  FailCount++; break;
                default:                  SkipCount++; break;
            }
        }
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// Row view-model for a single test result.
/// </summary>
public sealed class TestResultRow
{
    /// <summary>Constructs from a parsed <see cref="TestResult"/>.</summary>
    public TestResultRow(TestResult result)
    {
        Result     = result;
        Display    = result.TestName;
        Outcome    = result.Outcome;
        DurationMs = (int)result.Duration.TotalMilliseconds;
    }

    public TestResult  Result     { get; }
    public string      Display    { get; }
    public TestOutcome Outcome    { get; }
    public int         DurationMs { get; }

    public string OutcomeGlyph => Outcome switch
    {
        TestOutcome.Passed  => "\uE930", // Segoe MDL2 — CheckMark circle
        TestOutcome.Failed  => "\uEB90", // Segoe MDL2 — Error badge
        _                   => "\uE89A", // Segoe MDL2 — Info
    };
}
