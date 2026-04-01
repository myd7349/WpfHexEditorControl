// ==========================================================
// Project: WpfHexEditor.Core.Roslyn
// File: Services/MetadataAsSourceCache.cs
// Contributors: Claude Opus 4.6
// Created: 2026-04-01
// Description:
//     Generates and caches decompiled source for metadata-only symbols.
//     Produces temp .cs/.vb files with type member stubs + XML docs.
//     Used by RoslynNavigationProvider for Ctrl+Click on framework types.
// ==========================================================

using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;

namespace WpfHexEditor.Core.Roslyn.Services;

internal sealed class MetadataAsSourceCache
{
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.Ordinal);
    private readonly string _cacheDir;

    public MetadataAsSourceCache()
    {
        _cacheDir = Path.Combine(Path.GetTempPath(), "WpfHexEditor", "MetadataAsSource");
        Directory.CreateDirectory(_cacheDir);
    }

    /// <summary>
    /// Returns a temp file path containing a stub declaration for the given metadata symbol.
    /// The file is cached by symbol key (assembly + display name).
    /// </summary>
    public Task<string?> GetOrGenerateAsync(ISymbol symbol, string language, CancellationToken ct)
    {
        var key = $"{symbol.ContainingAssembly?.Identity}|{symbol.ToDisplayString()}";
        if (_cache.TryGetValue(key, out var cached) && File.Exists(cached))
            return Task.FromResult<string?>(cached);

        // Navigate to the containing type for member symbols.
        var typeSymbol = symbol as INamedTypeSymbol ?? symbol.ContainingType;
        if (typeSymbol is null)
            return Task.FromResult<string?>(null);

        var ext = language == LanguageNames.VisualBasic ? ".vb" : ".cs";
        var safeName = SanitizeFileName(typeSymbol.ToDisplayString());
        var filePath = Path.Combine(_cacheDir, safeName + ext);

        if (!File.Exists(filePath))
        {
            var source = language == LanguageNames.VisualBasic
                ? GenerateVbStub(typeSymbol)
                : GenerateCSharpStub(typeSymbol);
            File.WriteAllText(filePath, source, Encoding.UTF8);
        }

        _cache[key] = filePath;
        return Task.FromResult<string?>(filePath);
    }

    /// <summary>Finds the 0-based line number of a member in the generated stub.</summary>
    public int FindMemberLine(string filePath, ISymbol symbol)
    {
        if (!File.Exists(filePath)) return 0;
        var searchName = symbol.Name;
        var lines = File.ReadAllLines(filePath);
        for (int i = 0; i < lines.Length; i++)
            if (lines[i].Contains(searchName))
                return i;
        return 0;
    }

    private static string GenerateCSharpStub(INamedTypeSymbol type)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// ──────────────────────────────────────────────────────────────────────");
        sb.AppendLine($"// Metadata source for {type.ContainingAssembly?.Identity.Name}");
        sb.AppendLine($"// Type: {type.ToDisplayString()}");
        sb.AppendLine("// This file is auto-generated from metadata — not editable.");
        sb.AppendLine("// ──────────────────────────────────────────────────────────────────────");
        sb.AppendLine();

        if (type.ContainingNamespace is { IsGlobalNamespace: false } ns)
        {
            sb.AppendLine($"namespace {ns.ToDisplayString()}");
            sb.AppendLine("{");
        }

        AppendXmlDoc(sb, type, "    ");

        var keyword = type.TypeKind switch
        {
            TypeKind.Interface => "interface",
            TypeKind.Struct    => "struct",
            TypeKind.Enum      => "enum",
            TypeKind.Delegate  => "delegate",
            _                  => "class",
        };

        sb.AppendLine($"    public {keyword} {type.Name}{FormatTypeParams(type)}");
        sb.AppendLine("    {");

        foreach (var member in type.GetMembers())
        {
            if (member.DeclaredAccessibility != Microsoft.CodeAnalysis.Accessibility.Public &&
                member.DeclaredAccessibility != Microsoft.CodeAnalysis.Accessibility.Protected) continue;
            if (member.IsImplicitlyDeclared) continue;

            AppendXmlDoc(sb, member, "        ");

            switch (member)
            {
                case IMethodSymbol m when m.MethodKind == MethodKind.Constructor:
                    sb.AppendLine($"        public {type.Name}({FormatParams(m)}) {{ }}");
                    break;
                case IMethodSymbol m when m.MethodKind == MethodKind.Ordinary:
                    sb.AppendLine($"        public {m.ReturnType.ToDisplayString()} {m.Name}{FormatTypeParams(m)}({FormatParams(m)}) => throw null!;");
                    break;
                case IPropertySymbol p:
                    var accessors = p.SetMethod is not null ? "get; set;" : "get;";
                    sb.AppendLine($"        public {p.Type.ToDisplayString()} {p.Name} {{ {accessors} }}");
                    break;
                case IFieldSymbol f:
                    sb.AppendLine($"        public {f.Type.ToDisplayString()} {f.Name};");
                    break;
                case IEventSymbol e:
                    sb.AppendLine($"        public event {e.Type.ToDisplayString()} {e.Name};");
                    break;
            }
        }

        sb.AppendLine("    }");

        if (type.ContainingNamespace is { IsGlobalNamespace: false })
            sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateVbStub(INamedTypeSymbol type)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"' Metadata source for {type.ContainingAssembly?.Identity.Name}");
        sb.AppendLine($"' Type: {type.ToDisplayString()}");
        sb.AppendLine();

        if (type.ContainingNamespace is { IsGlobalNamespace: false } ns)
            sb.AppendLine($"Namespace {ns.ToDisplayString()}");

        var keyword = type.TypeKind switch
        {
            TypeKind.Interface => "Interface",
            TypeKind.Struct    => "Structure",
            TypeKind.Enum      => "Enum",
            _                  => "Class",
        };

        sb.AppendLine($"    Public {keyword} {type.Name}");

        foreach (var member in type.GetMembers())
        {
            if (member.DeclaredAccessibility != Microsoft.CodeAnalysis.Accessibility.Public) continue;
            if (member.IsImplicitlyDeclared) continue;

            switch (member)
            {
                case IMethodSymbol m when m.MethodKind == MethodKind.Ordinary:
                    if (m.ReturnsVoid)
                        sb.AppendLine($"        Public Sub {m.Name}()");
                    else
                        sb.AppendLine($"        Public Function {m.Name}() As {m.ReturnType.ToDisplayString()}");
                    break;
                case IPropertySymbol p:
                    sb.AppendLine($"        Public Property {p.Name} As {p.Type.ToDisplayString()}");
                    break;
            }
        }

        sb.AppendLine($"    End {keyword}");
        if (type.ContainingNamespace is { IsGlobalNamespace: false })
            sb.AppendLine("End Namespace");

        return sb.ToString();
    }

    private static void AppendXmlDoc(StringBuilder sb, ISymbol symbol, string indent)
    {
        var xml = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xml)) return;

        const string startTag = "<summary>";
        const string endTag = "</summary>";
        var start = xml.IndexOf(startTag, StringComparison.Ordinal);
        var end = xml.IndexOf(endTag, StringComparison.Ordinal);
        if (start < 0 || end < 0) return;

        var summary = xml[(start + startTag.Length)..end].Trim()
            .Replace("\r\n", " ").Replace("\n", " ");
        sb.AppendLine($"{indent}/// <summary>{summary}</summary>");
    }

    private static string FormatTypeParams(ISymbol symbol)
    {
        var typeParams = symbol switch
        {
            INamedTypeSymbol t => t.TypeParameters,
            IMethodSymbol m    => m.TypeParameters,
            _                  => default,
        };
        if (typeParams.IsDefaultOrEmpty) return string.Empty;
        return "<" + string.Join(", ", typeParams.Select(p => p.Name)) + ">";
    }

    private static string FormatParams(IMethodSymbol method)
        => string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var result = new char[Math.Min(name.Length, 80)];
        for (int i = 0; i < result.Length; i++)
            result[i] = Array.IndexOf(invalid, name[i]) >= 0 ? '_' : name[i];
        return new string(result);
    }
}
