//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

namespace WpfHexEditor.SDK.Contracts.Services;

/// <summary>
/// Provides read access to the active IDE solution structure.
/// </summary>
public interface ISolutionExplorerService
{
    /// <summary>Gets whether a solution is currently open.</summary>
    bool HasActiveSolution { get; }

    /// <summary>Gets the path to the active solution file (.whsln), or null if none.</summary>
    string? ActiveSolutionPath { get; }

    /// <summary>Gets the name of the active solution, or null if none.</summary>
    string? ActiveSolutionName { get; }

    /// <summary>
    /// Gets all file paths currently open in the IDE as document tabs.
    /// </summary>
    IReadOnlyList<string> GetOpenFilePaths();

    /// <summary>
    /// Gets all file paths belonging to the active solution (across all projects).
    /// Returns empty list if no solution is open.
    /// </summary>
    IReadOnlyList<string> GetSolutionFilePaths();

    /// <summary>
    /// Opens a file in the IDE (creates a new document tab or activates an existing one).
    /// </summary>
    /// <param name="filePath">Absolute path to the file to open.</param>
    /// <param name="ct">Cancellation token.</param>
    Task OpenFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Raised when the active solution changes (opened, closed, reloaded).
    /// Raised on the UI thread.
    /// </summary>
    event EventHandler SolutionChanged;
}
