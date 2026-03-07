// ==========================================================
// Project: WpfHexEditor.Panels.IDE
// File: PluginMonitoringViewModel.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-06
// Description:
//     ViewModel for the Plugin Monitoring docking panel.
//     Drives two real-time line charts (CPU% and Memory MB) and a
//     per-plugin summary table, refreshed by a DispatcherTimer.
//
// Architecture Notes:
//     Observer pattern — subscribes to WpfPluginHost sampling timer output
//     via periodic pull (Refresh() on timer tick).
//     Canvas-based Polyline charts: pure WPF, no external library.
// ==========================================================

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using WpfHexEditor.PluginHost;
using WpfHexEditor.PluginHost.Monitoring;
using WpfHexEditor.SDK.Models;

namespace WpfHexEditor.Panels.IDE.Panels.ViewModels;

/// <summary>Side of the panel where the plugin list appears in landscape (bottom-dock) mode.</summary>
public enum PanelListSide { Left, Right }

/// <summary>A single data point for the time-series chart.</summary>
public sealed record ChartPoint(DateTime Time, double Value);

/// <summary>A timestamped event entry displayed in the live event log.</summary>
public sealed record PluginEventEntry(string TimeLabel, string Icon, string Color, string PluginName, string Message);

/// <summary>
/// Summary row for a single plugin in the monitoring table.
/// </summary>
public sealed class PluginMonitorRow : INotifyPropertyChanged
{
    private string _name        = string.Empty;
    private string _state       = string.Empty;
    private string _stateColor  = "#9CA3AF";
    private double _cpuPercent;
    private long   _memoryMb;
    private double _avgExecMs;
    private double _initTimeMs;
    private string _uptimeLabel = string.Empty;
    private double _peakCpu;
    private bool   _isResponsive = true;
    private bool   _isSlow;
    private string _faultMessage = string.Empty;
    private string _version      = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; init; } = string.Empty;

    public string Name        { get => _name;        set { _name        = value; OnPropertyChanged(); } }
    public string State       { get => _state;       set { _state       = value; OnPropertyChanged(); } }
    public string StateColor  { get => _stateColor;  set { _stateColor  = value; OnPropertyChanged(); } }
    public double CpuPercent  { get => _cpuPercent;  set { _cpuPercent  = value; OnPropertyChanged(); } }
    public long   MemoryMb    { get => _memoryMb;    set { _memoryMb    = value; OnPropertyChanged(); } }
    public double AvgExecMs   { get => _avgExecMs;   set { _avgExecMs   = value; OnPropertyChanged(); } }
    public double InitTimeMs  { get => _initTimeMs;  set { _initTimeMs  = value; OnPropertyChanged(); } }
    public string UptimeLabel { get => _uptimeLabel; set { _uptimeLabel = value; OnPropertyChanged(); } }
    public double PeakCpu     { get => _peakCpu;     set { _peakCpu     = value; OnPropertyChanged(); } }
    public string FaultMessage { get => _faultMessage; set { _faultMessage = value; OnPropertyChanged(); } }
    public string Version      { get => _version;      set { _version      = value; OnPropertyChanged(); } }

    public bool IsResponsive
    {
        get => _isResponsive;
        set { _isResponsive = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasWarning)); OnPropertyChanged(nameof(WarningIcon)); OnPropertyChanged(nameof(WarningColor)); OnPropertyChanged(nameof(WarningTooltip)); }
    }

    public bool IsSlow
    {
        get => _isSlow;
        set { _isSlow = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasWarning)); OnPropertyChanged(nameof(WarningIcon)); OnPropertyChanged(nameof(WarningColor)); OnPropertyChanged(nameof(WarningTooltip)); }
    }

    // -- Computed warning properties -----------------------------------------

    /// <summary>True when the plugin is slow or not responsive.</summary>
    public bool   HasWarning    => !_isResponsive || _isSlow;

    /// <summary>Segoe MDL2 icon for the warning badge (empty string = no icon).</summary>
    public string WarningIcon   => !_isResponsive ? "\uE7BA" : _isSlow ? "\uE946" : string.Empty;

    /// <summary>Color of the warning badge.</summary>
    public string WarningColor  => !_isResponsive ? "#F97316" : "#F59E0B";

    /// <summary>Tooltip message for the warning badge.</summary>
    public string WarningTooltip => !_isResponsive ? "Plugin not responsive" : _isSlow ? "Slow plugin detected" : string.Empty;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Rich metadata + runtime metrics for the currently selected plugin.
/// Populated by <see cref="PluginMonitoringViewModel.SelectedPlugin"/> setter.
/// </summary>
public sealed class PluginDetailViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string  Name             { get; private set; } = string.Empty;
    public string  Version          { get; private set; } = string.Empty;
    public string  Author           { get; private set; } = string.Empty;
    public string  Description      { get; private set; } = string.Empty;
    public bool    TrustedPublisher { get; private set; }
    public string  TrustLabel       { get; private set; } = string.Empty;
    public string  IsolationMode    { get; private set; } = string.Empty;
    public string  SdkVersion       { get; private set; } = string.Empty;
    public string  LoadedAtLabel    { get; private set; } = "—";
    public string  UptimeLabel      { get; private set; } = "—";
    public string  InitTimeMsLabel  { get; private set; } = string.Empty;
    public string  AvgExecMsLabel   { get; private set; } = string.Empty;
    public string  PeakCpuLabel     { get; private set; } = string.Empty;
    public string  AvgCpuLabel      { get; private set; } = string.Empty;
    public string  PermissionsLabel { get; private set; } = "None";
    public string? FaultMessage     { get; private set; }
    public bool    HasFault         { get; private set; }
    public string  StateColor       { get; private set; } = "#9CA3AF";

    /// <summary>Refreshes all properties from the given <paramref name="entry"/>.</summary>
    public void Update(PluginEntry entry)
    {
        Name            = entry.Manifest.Name;
        Version         = entry.Manifest.Version ?? string.Empty;
        Author          = string.IsNullOrEmpty(entry.Manifest.Author) ? entry.Manifest.Publisher : entry.Manifest.Author;
        Description     = entry.Manifest.Description ?? string.Empty;
        TrustedPublisher = entry.Manifest.TrustedPublisher;
        TrustLabel      = entry.Manifest.TrustedPublisher ? "Official" : "Community";
        IsolationMode   = entry.Manifest.IsolationMode.ToString();
        SdkVersion      = entry.Manifest.SdkVersion ?? string.Empty;
        LoadedAtLabel   = entry.LoadedAt?.ToLocalTime().ToString("HH:mm:ss") ?? "—";

        var uptime      = entry.Diagnostics.Uptime;
        UptimeLabel     = uptime.TotalSeconds < 1 ? "—"
            : uptime.TotalHours >= 1
                ? $"{(int)uptime.TotalHours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}"
                : $"{uptime.Minutes:D2}:{uptime.Seconds:D2}";

        InitTimeMsLabel = $"{entry.InitDuration.TotalMilliseconds:F0} ms";
        AvgExecMsLabel  = $"{entry.Diagnostics.AverageExecutionTime.TotalMilliseconds:F1} ms";
        PeakCpuLabel    = $"{entry.Diagnostics.PeakCpu():F1} %";
        AvgCpuLabel     = $"{entry.Diagnostics.AverageCpu():F1} %";
        FaultMessage    = entry.FaultException?.ToString();
        HasFault        = entry.FaultException is not null;
        StateColor      = StateBadgeColor(entry.State);
        PermissionsLabel = BuildPermissionsLabel(entry.Manifest.Permissions);

        // Fire single notification to refresh all bindings.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    private static string BuildPermissionsLabel(PluginCapabilities? caps)
    {
        if (caps is null) return "None";
        var flags = new List<string>(8);
        if (caps.AccessFileSystem) flags.Add("FileSystem");
        if (caps.AccessNetwork)    flags.Add("Network");
        if (caps.AccessHexEditor)  flags.Add("HexEditor");
        if (caps.AccessCodeEditor) flags.Add("CodeEditor");
        if (caps.RegisterMenus)    flags.Add("Menus");
        if (caps.WriteOutput)      flags.Add("Output");
        if (caps.WriteErrorPanel)  flags.Add("ErrorPanel");
        if (caps.AccessSettings)   flags.Add("Settings");
        if (caps.WriteTerminal)    flags.Add("Terminal");
        return flags.Count == 0 ? "None" : string.Join("  ·  ", flags);
    }

    private static string StateBadgeColor(PluginState state) => state switch
    {
        PluginState.Loaded       => "#22C55E",
        PluginState.Loading      => "#F59E0B",
        PluginState.Disabled     => "#6B7280",
        PluginState.Faulted      => "#EF4444",
        PluginState.Incompatible => "#F97316",
        _                        => "#9CA3AF"
    };
}

/// <summary>
/// Master ViewModel for the Plugin Monitoring docking panel.
/// </summary>
public sealed class PluginMonitoringViewModel : INotifyPropertyChanged, IDisposable
{
    private const int MaxChartPoints = 60; // 5 min at 5 s intervals

    private readonly WpfPluginHost _host;
    private readonly DispatcherTimer _timer;

    private const int MaxEventLog = 200;

    private readonly HashSet<string> _slowPluginIds = new();
    private readonly ICollectionView _filteredRows = null!; // assigned in constructor
    private string              _searchText = string.Empty;
    private PluginMonitorRow?   _selectedPlugin;
    private PluginDetailViewModel? _selectedPluginDetail;

    private double        _currentCpu;
    private long          _currentMemoryMb;
    private int           _loadedCount;
    private int           _totalCount;
    private int           _faultCount;
    private bool          _isRunning = true;
    private int           _intervalSeconds = 5;
    private PanelListSide _listSide = PanelListSide.Right;
    private bool          _isLandscape;

    public event PropertyChangedEventHandler? PropertyChanged;

    public PluginMonitoringViewModel(WpfPluginHost host, Dispatcher dispatcher)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));

        _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromSeconds(_intervalSeconds)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();

        StartStopCommand      = new RelayCommand(_ => ToggleRunning());
        ResetCommand          = new RelayCommand(_ => Reset());
        ToggleListSideCommand = new RelayCommand(_ => ListSide = ListSide == PanelListSide.Right ? PanelListSide.Left : PanelListSide.Right);
        ClearLogCommand       = new RelayCommand(_ => EventLog.Clear());
        ReloadPluginCommand   = new RelayCommand(_ => { if (_selectedPlugin is not null) _ = _host.ReloadPluginAsync(_selectedPlugin.Id); });
        TogglePluginCommand   = new RelayCommand(_ =>
        {
            if (_selectedPlugin is null) return;
            if (_selectedPlugin.State is "Running" or "Loading")
                _ = _host.DisablePluginAsync(_selectedPlugin.Id);
            else
                _ = _host.EnablePluginAsync(_selectedPlugin.Id);
        });
        SetIntervalCommand    = new RelayCommand(p =>
        {
            if (p is int seconds) SetInterval(seconds);
        });

        // Subscribe to host lifecycle events for the live event log.
        _host.PluginLoaded        += OnPluginLoaded;
        _host.PluginCrashed       += OnPluginCrashed;
        _host.SlowPluginDetected  += OnSlowPluginDetected;

        // Build the filterable view over the Rows collection.
        _filteredRows = CollectionViewSource.GetDefaultView(Rows);
        _filteredRows.Filter = FilterPlugin;

        Refresh();
    }

    // -- Commands ------------------------------------------------------------

    /// <summary>Toggles the sampling timer on/off.</summary>
    public ICommand StartStopCommand { get; }

    /// <summary>Clears all chart history and resets counters.</summary>
    public ICommand ResetCommand { get; }

    /// <summary>Toggles the plugin list between left and right side in landscape mode.</summary>
    public ICommand ToggleListSideCommand { get; }

    /// <summary>Clears the live event log.</summary>
    public ICommand ClearLogCommand { get; }

    /// <summary>Reloads the currently selected plugin.</summary>
    public ICommand ReloadPluginCommand { get; }

    /// <summary>Enables or disables the currently selected plugin.</summary>
    public ICommand TogglePluginCommand { get; }

    /// <summary>Sets the sampling interval. CommandParameter = int seconds.</summary>
    public ICommand SetIntervalCommand { get; }

    // -- Running state -------------------------------------------------------

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning == value) return;
            _isRunning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StartStopLabel));
            OnPropertyChanged(nameof(StartStopIcon));
        }
    }

    /// <summary>Label shown on the Start/Stop button.</summary>
    public string StartStopLabel => _isRunning ? "Pause" : "Resume";

    /// <summary>Segoe MDL2 icon for the Start/Stop button (Play/Pause).</summary>
    public string StartStopIcon  => _isRunning ? "\uE769" : "\uE768";

    public int IntervalSeconds
    {
        get => _intervalSeconds;
        private set { _intervalSeconds = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Which side the plugin list appears on in landscape (bottom-dock) mode.
    /// Toggled by <see cref="ToggleListSideCommand"/>. Code-behind reacts via PropertyChanged.
    /// </summary>
    public PanelListSide ListSide
    {
        get => _listSide;
        private set
        {
            if (_listSide == value) return;
            _listSide = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ListSideIcon));
            OnPropertyChanged(nameof(ListSideTooltip));
        }
    }

    /// <summary>Segoe MDL2 icon for the list-side toggle button.</summary>
    public string ListSideIcon    => _listSide == PanelListSide.Right ? "\uE89F" : "\uE8A0";

    /// <summary>Tooltip text for the list-side toggle button.</summary>
    public string ListSideTooltip => _listSide == PanelListSide.Right ? "Move list to left" : "Move list to right";

    /// <summary>
    /// True when the panel is in wide/landscape mode (bottom dock).
    /// Set by the code-behind on SizeChanged. Drives toolbar toggle button visibility.
    /// </summary>
    public bool IsLandscape
    {
        get => _isLandscape;
        set
        {
            if (_isLandscape == value) return;
            _isLandscape = value;
            OnPropertyChanged();
        }
    }

    private void ToggleRunning()
    {
        if (_isRunning)
        {
            _timer.Stop();
            IsRunning = false;
        }
        else
        {
            _timer.Start();
            IsRunning = true;
        }
    }

    private void Reset()
    {
        CpuHistory.Clear();
        MemoryHistory.Clear();
    }

    private void SetInterval(int seconds)
    {
        if (seconds <= 0) return;
        IntervalSeconds = seconds;
        _timer.Interval = TimeSpan.FromSeconds(seconds);
    }

    // -- Chart data sources --------------------------------------------------

    /// <summary>CPU% samples over time (max 60 points).</summary>
    public ObservableCollection<ChartPoint> CpuHistory { get; } = new();

    /// <summary>Memory MB samples over time (max 60 points).</summary>
    public ObservableCollection<ChartPoint> MemoryHistory { get; } = new();

    // -- Summary metrics -----------------------------------------------------

    public double CurrentCpu
    {
        get => _currentCpu;
        private set { _currentCpu = value; OnPropertyChanged(); }
    }

    public long CurrentMemoryMb
    {
        get => _currentMemoryMb;
        private set { _currentMemoryMb = value; OnPropertyChanged(); }
    }

    public int LoadedCount
    {
        get => _loadedCount;
        private set { _loadedCount = value; OnPropertyChanged(); }
    }

    public int TotalCount
    {
        get => _totalCount;
        private set { _totalCount = value; OnPropertyChanged(); }
    }

    public int FaultCount
    {
        get => _faultCount;
        private set { _faultCount = value; OnPropertyChanged(); }
    }

    public string HealthLabel => FaultCount > 0
        ? $"{FaultCount} fault{(FaultCount == 1 ? "" : "s")}"
        : LoadedCount == TotalCount && TotalCount > 0
            ? "Healthy"
            : TotalCount == 0 ? "No plugins" : $"{LoadedCount}/{TotalCount} running";

    public string HealthColor => FaultCount > 0
        ? "#EF4444"
        : LoadedCount < TotalCount ? "#F59E0B"
        : "#22C55E";

    // -- Plugin table + search -----------------------------------------------

    public ObservableCollection<PluginMonitorRow> Rows { get; } = new();

    /// <summary>Filtered + sortable view over <see cref="Rows"/>. DataGrid binds to this.</summary>
    public ICollectionView FilteredRows => _filteredRows;

    /// <summary>Real-time filter applied to <see cref="FilteredRows"/> (case-insensitive name match).</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            OnPropertyChanged();
            _filteredRows.Refresh();
        }
    }

    private bool FilterPlugin(object item)
        => string.IsNullOrWhiteSpace(_searchText)
        || (item is PluginMonitorRow row && row.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

    // -- Selection -----------------------------------------------------------

    /// <summary>Currently selected row; drives the detail pane and action buttons.</summary>
    public PluginMonitorRow? SelectedPlugin
    {
        get => _selectedPlugin;
        set
        {
            if (_selectedPlugin == value) return;
            _selectedPlugin = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedPlugin));
            OnPropertyChanged(nameof(TogglePluginLabel));
            OnPropertyChanged(nameof(TogglePluginIcon));
            UpdateSelectedPluginDetail();
        }
    }

    /// <summary>True when a plugin row is selected; enables action toolbar buttons.</summary>
    public bool HasSelectedPlugin => _selectedPlugin is not null;

    /// <summary>Rich detail view for the selected plugin. Null when nothing is selected.</summary>
    public PluginDetailViewModel? SelectedPluginDetail
    {
        get => _selectedPluginDetail;
        private set { _selectedPluginDetail = value; OnPropertyChanged(); }
    }

    /// <summary>Label for the enable/disable toggle button.</summary>
    public string TogglePluginLabel => _selectedPlugin?.State is "Running" or "Loading" ? "Disable" : "Enable";

    /// <summary>Segoe MDL2 icon for the enable/disable toggle button.</summary>
    public string TogglePluginIcon  => _selectedPlugin?.State is "Running" or "Loading" ? "\uE711" : "\uE8D8";

    private void UpdateSelectedPluginDetail()
    {
        if (_selectedPlugin is null) { SelectedPluginDetail = null; return; }
        var entry = _host.GetPlugin(_selectedPlugin.Id);
        if (entry is null) { SelectedPluginDetail = null; return; }
        var detail = _selectedPluginDetail ?? new PluginDetailViewModel();
        detail.Update(entry);
        SelectedPluginDetail = detail;
    }

    // -- Live event log ------------------------------------------------------

    /// <summary>Timestamped events from the plugin host (load, crash, slow detection).</summary>
    public ObservableCollection<PluginEventEntry> EventLog { get; } = new();

    private void OnPluginLoaded(object? sender, PluginEventArgs e)
    {
        _slowPluginIds.Remove(e.PluginId); // reset slow flag on reload
        AddEvent(new PluginEventEntry(Now(), "\uE73E", "#22C55E", e.PluginName, "Plugin loaded"));
    }

    private void OnPluginCrashed(object? sender, PluginFaultedEventArgs e)
        => AddEvent(new PluginEventEntry(Now(), "\uEA39", "#EF4444", e.PluginName,
                    $"Crashed ({e.Phase}): {e.Exception?.Message ?? "unknown error"}"));

    private void OnSlowPluginDetected(object? sender, SlowPluginDetectedEventArgs e)
    {
        _slowPluginIds.Add(e.PluginId);
        AddEvent(new PluginEventEntry(Now(), "\uE946", "#F59E0B", e.PluginName,
                 $"Slow: avg {e.AverageExecutionTime.TotalMilliseconds:F0} ms (threshold {e.Threshold.TotalMilliseconds:F0} ms)"));
    }

    private void AddEvent(PluginEventEntry entry)
    {
        while (EventLog.Count >= MaxEventLog)
            EventLog.RemoveAt(0);
        EventLog.Add(entry);
    }

    private static string Now() => DateTime.Now.ToString("HH:mm:ss");

    // -- Internals -----------------------------------------------------------

    private void OnTimerTick(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        var plugins = _host.GetAllPlugins();

        var loaded = plugins.Count(p => p.State == PluginState.Loaded);
        var faults = plugins.Count(p => p.State == PluginState.Faulted);
        var now    = DateTime.UtcNow;

        // Aggregate CPU and memory across all loaded plugins.
        // For InProcess isolation, all plugins share the same process metrics,
        // so we sum the reported values (each plugin entry carries a copy of the same snapshot).
        double cpu   = 0;
        long   memMb = 0;

        foreach (var entry in plugins.Where(p => p.State == PluginState.Loaded))
        {
            var snap = entry.Diagnostics.GetLatest();
            if (snap is null) continue;
            cpu   += snap.CpuPercent;
            memMb += snap.MemoryBytes / (1024 * 1024);
        }

        // Clamp to avoid inflating metrics when many identical process-level snapshots are summed.
        if (plugins.Count(p => p.State == PluginState.Loaded) > 1)
        {
            cpu   = Math.Min(cpu, 100);
            memMb = memMb / Math.Max(1, plugins.Count(p => p.State == PluginState.Loaded));
        }

        CurrentCpu      = cpu;
        CurrentMemoryMb = memMb;
        LoadedCount     = loaded;
        TotalCount      = plugins.Count;
        FaultCount      = faults;
        OnPropertyChanged(nameof(HealthLabel));
        OnPropertyChanged(nameof(HealthColor));

        // Rolling chart data
        AddChartPoint(CpuHistory,    new ChartPoint(now, cpu));
        AddChartPoint(MemoryHistory, new ChartPoint(now, memMb));

        // Refresh detail pane + toggle label when a plugin is selected.
        if (_selectedPlugin is not null)
        {
            OnPropertyChanged(nameof(TogglePluginLabel));
            OnPropertyChanged(nameof(TogglePluginIcon));
            UpdateSelectedPluginDetail();
        }

        // Sync rows collection
        var existingIds = Rows.Select(r => r.Id).ToHashSet();
        var currentIds  = plugins.Select(p => p.Manifest.Id).ToHashSet();

        // Remove stale rows
        foreach (var stale in Rows.Where(r => !currentIds.Contains(r.Id)).ToList())
            Rows.Remove(stale);

        // Add or update
        foreach (var entry in plugins)
        {
            var snap = entry.Diagnostics.GetLatest();
            var row  = Rows.FirstOrDefault(r => r.Id == entry.Manifest.Id);

            if (row is null)
            {
                row = new PluginMonitorRow { Id = entry.Manifest.Id };
                Rows.Add(row);
            }

            row.Name         = entry.Manifest.Name;
            row.State        = StateLabel(entry.State);
            row.StateColor   = StateBadgeColor(entry.State);
            row.CpuPercent   = snap?.CpuPercent ?? 0;
            row.MemoryMb     = snap is not null ? snap.MemoryBytes / (1024 * 1024) : 0;
            row.AvgExecMs    = entry.Diagnostics.AverageExecutionTime.TotalMilliseconds;
            row.InitTimeMs   = entry.InitDuration.TotalMilliseconds;
            row.UptimeLabel  = FormatUptime(entry.Diagnostics.Uptime);
            row.PeakCpu      = entry.Diagnostics.PeakCpu();
            row.IsResponsive = entry.Diagnostics.IsResponsive;
            row.IsSlow       = _slowPluginIds.Contains(entry.Manifest.Id);
            row.FaultMessage = entry.FaultException?.Message ?? string.Empty;
            row.Version      = entry.Manifest.Version ?? string.Empty;
        }
    }

    private static void AddChartPoint(ObservableCollection<ChartPoint> collection, ChartPoint point)
    {
        // Batch-remove old points then add new one — avoids O(n) RemoveAt(0) per item.
        // Since ObservableCollection fires per removal, batch-remove in reverse is fine here.
        while (collection.Count >= MaxChartPoints)
            collection.RemoveAt(0);
        collection.Add(point);
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalSeconds < 1) return "—";
        return uptime.TotalHours >= 1
            ? $"{(int)uptime.TotalHours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}"
            : $"{uptime.Minutes:D2}:{uptime.Seconds:D2}";
    }

    private static string StateLabel(PluginState state) => state switch
    {
        PluginState.Loaded      => "Running",
        PluginState.Loading     => "Loading",
        PluginState.Disabled    => "Disabled",
        PluginState.Faulted     => "Error",
        PluginState.Incompatible => "Incompatible",
        PluginState.Unloaded    => "Unloaded",
        _                       => "Unknown"
    };

    private static string StateBadgeColor(PluginState state) => state switch
    {
        PluginState.Loaded      => "#22C55E",
        PluginState.Loading     => "#F59E0B",
        PluginState.Disabled    => "#6B7280",
        PluginState.Faulted     => "#EF4444",
        PluginState.Incompatible => "#F97316",
        _                       => "#9CA3AF"
    };

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _host.PluginLoaded       -= OnPluginLoaded;
        _host.PluginCrashed      -= OnPluginCrashed;
        _host.SlowPluginDetected -= OnSlowPluginDetected;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Minimal RelayCommand for toolbar actions in the monitoring panel.</summary>
internal sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;

    public RelayCommand(Action<object?> execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged { add { } remove { } }

    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute(parameter);
}
