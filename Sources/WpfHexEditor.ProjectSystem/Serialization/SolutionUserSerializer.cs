// ==========================================================
// Project: WpfHexEditor.ProjectSystem
// File: SolutionUserSerializer.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-07
// Description:
//     Reads and writes the .whsln.user sidecar file.
//     The sidecar stores per-user UI state (tree expand state, dock layout)
//     that must NOT be committed to version control alongside the .whsln file.
//
// Architecture Notes:
//     Pattern: static async helpers (mirrors SolutionSerializer style).
//     ReadAsync returns null when no sidecar exists — callers must handle gracefully.
//     WriteAsync creates the directory if needed.
//
// ==========================================================

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using WpfHexEditor.ProjectSystem.Dto;

namespace WpfHexEditor.ProjectSystem.Serialization;

/// <summary>
/// Reads and writes the <c>.whsln.user</c> sidecar file that stores per-user
/// UI state (dock layout, Solution Explorer tree expand/collapse) alongside a solution.
/// </summary>
public static class SolutionUserSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented          = true,
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // -- Path helper ------------------------------------------------------

    /// <summary>
    /// Returns the <c>.whsln.user</c> path that corresponds to a given
    /// <c>.whsln</c> file path (same directory, same base name, <c>.user</c> appended).
    /// </summary>
    public static string GetUserFilePath(string solutionFilePath)
        => solutionFilePath + ".user";

    // -- Read -------------------------------------------------------------

    /// <summary>
    /// Reads the Solution Explorer expanded-node keys from the sidecar for
    /// <paramref name="solutionFilePath"/>. Returns <see langword="null"/> when the
    /// sidecar does not exist (first time opening the solution on this machine).
    /// </summary>
    public static async Task<IReadOnlyList<string>?> ReadExpandedKeysAsync(
        string solutionFilePath, CancellationToken ct = default)
    {
        var path = GetUserFilePath(solutionFilePath);
        if (!File.Exists(path)) return null;

        try
        {
            await using var stream = File.OpenRead(path);
            var dto = await JsonSerializer.DeserializeAsync<SolutionUserDto>(stream, _options, ct);
            return dto?.TreeState?.ExpandedKeys is { Count: > 0 } keys ? keys : null;
        }
        catch
        {
            // Corrupt or unreadable sidecar — treat as missing so the app still opens cleanly.
            return null;
        }
    }

    // -- Write ------------------------------------------------------------

    /// <summary>
    /// Writes the Solution Explorer <paramref name="expandedKeys"/> to the sidecar file
    /// alongside <paramref name="solutionFilePath"/>. Creates the directory if needed.
    /// </summary>
    public static async Task WriteTreeStateAsync(
        string solutionFilePath, IReadOnlyList<string> expandedKeys, CancellationToken ct = default)
    {
        var path = GetUserFilePath(solutionFilePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var dto = new SolutionUserDto
        {
            TreeState = new SolutionTreeStateDto { ExpandedKeys = [.. expandedKeys] }
        };

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, dto, _options, ct);
    }
}
