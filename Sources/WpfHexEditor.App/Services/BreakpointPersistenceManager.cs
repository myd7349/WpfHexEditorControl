// ==========================================================
// Project: WpfHexEditor.App
// File: Services/BreakpointPersistenceManager.cs
// Description:
//     Serializes/deserializes IDE breakpoints to/from AppSettings.
//     Converts between BreakpointLocation (runtime model) and
//     PersistedBreakpoint (settings DTO). Stateless — holds no list.
// Architecture:
//     App layer only. Called by DebuggerServiceImpl after every mutation.
// ==========================================================

using System.IO;
using System.Xml.Linq;
using WpfHexEditor.Core.Debugger.Models;
using WpfHexEditor.Core.Options;

namespace WpfHexEditor.App.Services;

/// <summary>
/// Serializes IDE-managed breakpoints to/from <see cref="AppSettings.DebuggerSettings"/>.
/// </summary>
internal sealed class BreakpointPersistenceManager
{
    private readonly AppSettings _settings;
    private readonly SolutionBreakpointStore _solutionStore = new();

    public BreakpointPersistenceManager(AppSettings settings)
        => _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    /// <summary>Deserializes all persisted breakpoints into runtime <see cref="BreakpointLocation"/> records.</summary>
    public IReadOnlyList<BreakpointLocation> Load() =>
        _settings.Debugger.Breakpoints
            .Select(pb => new BreakpointLocation
            {
                FilePath  = pb.FilePath,
                Line      = pb.Line,
                Condition = pb.Condition,
                IsEnabled = pb.IsEnabled,
            })
            .ToList();

    /// <summary>Overwrites the persisted list with the current runtime breakpoints.</summary>
    public void Save(IEnumerable<BreakpointLocation> breakpoints)
    {
        _settings.Debugger.Breakpoints.Clear();
        _settings.Debugger.Breakpoints.AddRange(
            breakpoints.Select(b => new PersistedBreakpoint
            {
                FilePath  = b.FilePath,
                Line      = b.Line,
                Condition = b.Condition,
                IsEnabled = b.IsEnabled,
            }));
    }

    // ── Solution-aware persistence ──────────────────────────────────────────

    /// <summary>
    /// Load breakpoints for the current context: solution store when available, global fallback.
    /// </summary>
    public IReadOnlyList<BreakpointLocation> LoadForContext(string? solutionFilePath) =>
        !string.IsNullOrEmpty(solutionFilePath)
            ? _solutionStore.Load(solutionFilePath)
            : Load();

    /// <summary>
    /// Save breakpoints for the current context: solution store when available, global fallback.
    /// </summary>
    public void SaveForContext(string? solutionFilePath, IEnumerable<BreakpointLocation> breakpoints)
    {
        // Materialize once for both saves.
        var bpList = breakpoints as IReadOnlyList<BreakpointLocation> ?? breakpoints.ToList();

        if (!string.IsNullOrEmpty(solutionFilePath))
            _solutionStore.Save(solutionFilePath, bpList);
        else
            Save(bpList);

        // Auto-export to VS XML if enabled.
        if (_settings.Debugger.AutoExportVsXml && !string.IsNullOrEmpty(solutionFilePath))
            AutoExportVsXml(solutionFilePath, bpList);
    }

    // ── VS XML auto-export ───────────────────────────────────────────────

    /// <summary>
    /// Writes breakpoints as VS-compatible XML alongside the .whide store.
    /// Silently ignores failures (read-only dir, etc.).
    /// </summary>
    /// <summary>
    /// Attempts to auto-import VS breakpoints from XML files in the .vs/ folder.
    /// Returns imported breakpoints, or empty list if none found.
    /// Only called when .whide store is empty and AutoImportVsBreakpoints is enabled.
    /// </summary>
    public IReadOnlyList<BreakpointLocation> TryAutoImportFromVs(string solutionFilePath)
    {
        if (!_settings.Debugger.AutoImportVsBreakpoints) return [];

        try
        {
            var solutionDir = Path.GetDirectoryName(solutionFilePath);
            if (string.IsNullOrEmpty(solutionDir)) return [];

            var vsDir = Path.Combine(solutionDir, ".vs");
            if (!Directory.Exists(vsDir)) return [];

            // Search for breakpoint*.xml in .vs/ subdirectories.
            string? xmlPath = null;
            foreach (var xml in Directory.EnumerateFiles(vsDir, "breakpoint*.xml", SearchOption.AllDirectories))
            {
                xmlPath = xml;
                break;
            }

            if (xmlPath is null) return [];
            return ImportFromVsXml(xmlPath);
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Parses a VS breakpoint XML file into BreakpointLocation records.
    /// </summary>
    private static IReadOnlyList<BreakpointLocation> ImportFromVsXml(string xmlPath)
    {
        try
        {
            var doc = XDocument.Load(xmlPath);
            var root = doc.Root;
            if (root is null) return [];

            var results = new List<BreakpointLocation>();
            foreach (var el in root.Elements("Breakpoint"))
            {
                var file = (string?)el.Attribute("File");
                var lineStr = (string?)el.Attribute("Line");

                if (string.IsNullOrEmpty(file) || !int.TryParse(lineStr, out int line) || line < 1)
                    continue;

                var condition = (string?)el.Attribute("Condition");
                if (string.IsNullOrEmpty(condition)) condition = string.Empty;

                var enabledStr = (string?)el.Attribute("IsEnabled");
                bool isEnabled = !string.Equals(enabledStr, "false", StringComparison.OrdinalIgnoreCase);

                results.Add(new BreakpointLocation
                {
                    FilePath  = file,
                    Line      = line,
                    Condition = condition,
                    IsEnabled = isEnabled,
                });
            }

            return results;
        }
        catch
        {
            return [];
        }
    }

    private void AutoExportVsXml(string solutionFilePath, IEnumerable<BreakpointLocation> breakpoints)
    {
        try
        {
            var solutionDir = Path.GetDirectoryName(solutionFilePath);
            if (string.IsNullOrEmpty(solutionDir)) return;

            var relativePath = _settings.Debugger.VsExportRelativePath;
            if (string.IsNullOrWhiteSpace(relativePath)) relativePath = ".whide/breakpoints-vs.xml";

            var xmlPath = Path.Combine(solutionDir, relativePath);
            var dir = Path.GetDirectoryName(xmlPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("BreakpointCollection",
                    breakpoints.Select(bp => new XElement("Breakpoint",
                        new XAttribute("File", bp.FilePath),
                        new XAttribute("Line", bp.Line),
                        new XAttribute("Condition", bp.Condition ?? string.Empty),
                        new XAttribute("IsEnabled", bp.IsEnabled)))));

            doc.Save(xmlPath);
        }
        catch
        {
            // Silently fail — auto-export is best-effort.
        }
    }
}
