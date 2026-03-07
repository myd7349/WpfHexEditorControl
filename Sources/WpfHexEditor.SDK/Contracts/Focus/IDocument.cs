//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

namespace WpfHexEditor.SDK.Contracts.Focus;

/// <summary>
/// Represents an active document visible to plugins.
/// </summary>
public interface IDocument
{
    /// <summary>Gets the document unique content identifier.</summary>
    string ContentId { get; }

    /// <summary>Gets the document title displayed in the tab.</summary>
    string Title { get; }

    /// <summary>Gets the file path if the document is backed by a file; otherwise null.</summary>
    string? FilePath { get; }

    /// <summary>Gets the document type category (e.g. "hex", "code", "image").</summary>
    string DocumentType { get; }

    /// <summary>Gets whether the document is currently dirty (has unsaved changes).</summary>
    bool IsDirty { get; }
}
