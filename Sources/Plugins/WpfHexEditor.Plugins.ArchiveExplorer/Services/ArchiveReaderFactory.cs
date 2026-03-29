// Project      : WpfHexEditorControl
// File         : Services/ArchiveReaderFactory.cs
// Description  : Selects the correct IArchiveReader implementation based on
//                file extension. ZIP uses the BCL; everything else uses SharpCompress.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

using System.IO;

namespace WpfHexEditor.Plugins.ArchiveExplorer.Services;

/// <summary>
/// Creates the appropriate <see cref="IArchiveReader"/> for a given file path.
/// </summary>
public static class ArchiveReaderFactory
{
    // ── ZIP-compatible formats (BCL reader, zero extra dep) ────────────────
    private static readonly HashSet<string> _zipExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".jar", ".war", ".ear",
        ".nupkg", ".xpi", ".epub",
        ".docx", ".xlsx", ".pptx",
        ".odt",  ".ods",  ".odp",
    };

    // ── Non-ZIP formats (SharpCompress reader) ─────────────────────────────
    private static readonly HashSet<string> _sharpExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".7z", ".rar",
        ".tar", ".tgz", ".tbz2",
        ".gz",  ".bz2", ".xz",
    };

    /// <summary>All extensions for which a reader can be created.</summary>
    public static IReadOnlySet<string> SupportedExtensions { get; } =
        _zipExtensions.Union(_sharpExtensions, StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a reader for <paramref name="filePath"/>.
    /// Returns <see langword="null"/> when the extension is not supported.
    /// </summary>
    public static IArchiveReader? CreateReader(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        if (_zipExtensions.Contains(ext))    return new ZipArchiveReader(filePath);
        if (_sharpExtensions.Contains(ext))  return new SharpCompressArchiveReader(filePath);
        return null;
    }

    /// <summary>Returns true when <paramref name="filePath"/>'s extension is supported.</summary>
    public static bool IsSupported(string filePath)
        => SupportedExtensions.Contains(Path.GetExtension(filePath));
}
