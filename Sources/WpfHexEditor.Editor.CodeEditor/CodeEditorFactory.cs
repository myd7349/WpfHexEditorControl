//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.IO;
using WpfHexEditor.Editor.Core;
using WpfHexEditor.Editor.CodeEditor.Controls;

namespace WpfHexEditor.Editor.CodeEditor;

/// <summary>
/// <see cref="IEditorFactory"/> for the JSON / whfmt editor.
/// Register at application startup:
/// <code>EditorRegistry.Instance.Register(new CodeEditorFactory());</code>
/// </summary>
public sealed class CodeEditorFactory : IEditorFactory
{
    private static readonly IEditorDescriptor _descriptor = new CodeEditorDescriptor();

    public IEditorDescriptor Descriptor => _descriptor;

    public bool CanOpen(string filePath)
    {
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
        return ext is ".json" or ".whfmt" or ".whjson";
    }

    public IDocumentEditor Create() => new Controls.CodeEditor();
}

file sealed class CodeEditorDescriptor : IEditorDescriptor
{
    public string Id          => "json-editor";
    public string DisplayName => "JSON Editor";
    public string Description => "JSON / whfmt format definition editor";
    public IReadOnlyList<string> SupportedExtensions => [".json", ".whfmt", ".whjson"];
}
