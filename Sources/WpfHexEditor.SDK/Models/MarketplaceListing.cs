//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

namespace WpfHexEditor.SDK.Models;

/// <summary>
/// Represents a plugin listing from the WpfHexEditor marketplace.
/// </summary>
public sealed class MarketplaceListing
{
    /// <summary>Unique marketplace listing identifier.</summary>
    public string ListingId { get; init; } = string.Empty;

    /// <summary>Plugin display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Plugin description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Publisher name.</summary>
    public string Publisher { get; init; } = string.Empty;

    /// <summary>Latest available version.</summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>Category (e.g. "Data Visualization", "Analysis").</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Searchable tags.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>License identifier (e.g. "Apache-2.0", "MIT").</summary>
    public string License { get; init; } = string.Empty;

    /// <summary>Source repository URL (optional).</summary>
    public string? RepositoryUrl { get; init; }

    /// <summary>Icon URL or embedded base64 (optional).</summary>
    public string? Icon { get; init; }

    /// <summary>Whether this listing has been officially verified.</summary>
    public bool Verified { get; init; }

    /// <summary>Download URL for the .whxplugin package.</summary>
    public string? DownloadUrl { get; init; }
}
