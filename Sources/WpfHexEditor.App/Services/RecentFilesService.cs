//////////////////////////////////////////////
// Project: WpfHexEditor.App
// File: Services/RecentFilesService.cs
// Description:
//     Tracks recently opened files and persists them to JSON.
//     Used by the File menu to show a "Recent Files" submenu.
//////////////////////////////////////////////

using System.IO;
using System.Text.Json;

namespace WpfHexEditor.App.Services;

/// <summary>
/// Manages a list of recently opened file paths (max 10).
/// Persisted to %AppData%/WpfHexEditor/recent-files.json.
/// </summary>
internal sealed class RecentFilesService
{
    private const int MaxEntries = 10;

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WpfHexEditor", "recent-files.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
    };

    private readonly List<string> _entries = [];

    /// <summary>Ordered list of recent file paths (most recent first).</summary>
    public IReadOnlyList<string> Entries => _entries;

    /// <summary>Raised when the list changes (add/remove/clear).</summary>
    public event Action? Changed;

    /// <summary>Adds a file to the top of the recent list. Deduplicates.</summary>
    public void Add(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        // Remove existing entry (case-insensitive on Windows)
        _entries.RemoveAll(e => e.Equals(filePath, StringComparison.OrdinalIgnoreCase));

        // Insert at top
        _entries.Insert(0, filePath);

        // Trim to max
        while (_entries.Count > MaxEntries)
            _entries.RemoveAt(_entries.Count - 1);

        Save();
        Changed?.Invoke();
    }

    /// <summary>Removes a specific entry.</summary>
    public void Remove(string filePath)
    {
        if (_entries.RemoveAll(e => e.Equals(filePath, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            Save();
            Changed?.Invoke();
        }
    }

    /// <summary>Clears all recent files.</summary>
    public void Clear()
    {
        if (_entries.Count == 0) return;
        _entries.Clear();
        Save();
        Changed?.Invoke();
    }

    /// <summary>Loads entries from disk.</summary>
    public void Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            var json = File.ReadAllText(FilePath);
            var list = JsonSerializer.Deserialize<List<string>>(json, JsonOpts);
            if (list is not null)
            {
                _entries.Clear();
                // Only keep files that still exist
                foreach (var path in list.Take(MaxEntries))
                {
                    if (File.Exists(path))
                        _entries.Add(path);
                }
            }
        }
        catch { /* corrupt file — ignore */ }
    }

    /// <summary>Saves entries to disk.</summary>
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_entries, JsonOpts));
        }
        catch { /* best-effort */ }
    }
}
