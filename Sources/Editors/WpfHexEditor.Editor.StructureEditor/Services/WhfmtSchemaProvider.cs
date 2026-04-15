//////////////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Project: WpfHexEditor.Editor.StructureEditor
// File: Services/WhfmtSchemaProvider.cs
// Description: Parses whfmt.schema.json and exposes descriptions, enum values,
//              defaults, and constraints for use in the Structure Editor UI.
//////////////////////////////////////////////////////

using System.IO;
using System.Text.Json;

namespace WpfHexEditor.Editor.StructureEditor.Services;

/// <summary>
/// Reads the whfmt.schema.json file and provides property metadata
/// (descriptions, enum values, defaults, constraints) for the Structure Editor.
/// </summary>
internal sealed class WhfmtSchemaProvider
{
    private JsonElement _root;
    private JsonElement _defs;
    private bool _loaded;

    internal static WhfmtSchemaProvider Instance { get; } = new();

    /// <summary>
    /// Loads the schema from the embedded resource path.
    /// Safe to call multiple times — only loads once.
    /// </summary>
    internal void EnsureLoaded()
    {
        if (_loaded) return;

        var schemaPath = FindSchemaFile();
        if (schemaPath is null) return;

        try
        {
            var json = File.ReadAllText(schemaPath);
            var doc = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
            _root = doc.RootElement;
            if (_root.TryGetProperty("$defs", out var defs))
                _defs = defs;
            _loaded = true;
        }
        catch
        {
            // Schema not available — degrade gracefully
        }
    }

    /// <summary>
    /// Gets the description for a top-level property (e.g., "formatName", "category").
    /// </summary>
    internal string? GetPropertyDescription(string propertyName)
    {
        if (!_loaded) return null;
        return GetDescriptionFromElement(_root, "properties", propertyName);
    }

    /// <summary>
    /// Gets the description for a property inside a $defs type (e.g., "BlockDefinition.color").
    /// </summary>
    internal string? GetDefDescription(string defName, string propertyName)
    {
        if (!_loaded || _defs.ValueKind != JsonValueKind.Object) return null;
        if (!_defs.TryGetProperty(defName, out var def)) return null;
        return GetDescriptionFromElement(def, "properties", propertyName);
    }

    /// <summary>
    /// Gets enum values for a top-level property or $defs property.
    /// </summary>
    internal string[]? GetEnumValues(string propertyName)
    {
        if (!_loaded) return null;
        return GetEnumFromElement(_root, "properties", propertyName);
    }

    /// <summary>
    /// Gets enum values for a property inside a $defs type.
    /// </summary>
    internal string[]? GetDefEnumValues(string defName, string propertyName)
    {
        if (!_loaded || _defs.ValueKind != JsonValueKind.Object) return null;
        if (!_defs.TryGetProperty(defName, out var def)) return null;
        return GetEnumFromElement(def, "properties", propertyName);
    }

    /// <summary>
    /// Gets the default value for a property inside a $defs type.
    /// </summary>
    internal string? GetDefDefault(string defName, string propertyName)
    {
        if (!_loaded || _defs.ValueKind != JsonValueKind.Object) return null;
        if (!_defs.TryGetProperty(defName, out var def)) return null;
        if (!def.TryGetProperty("properties", out var props)) return null;
        if (!props.TryGetProperty(propertyName, out var prop)) return null;
        if (!prop.TryGetProperty("default", out var dflt)) return null;
        return dflt.ToString();
    }

    /// <summary>
    /// Gets the description for a $defs type itself (e.g., "DetectionRule", "BlockDefinition").
    /// </summary>
    internal string? GetDefTypeDescription(string defName)
    {
        if (!_loaded || _defs.ValueKind != JsonValueKind.Object) return null;
        if (!_defs.TryGetProperty(defName, out var def)) return null;
        if (!def.TryGetProperty("description", out var desc)) return null;
        return desc.GetString();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? GetDescriptionFromElement(JsonElement parent, string containerProp, string propertyName)
    {
        if (!parent.TryGetProperty(containerProp, out var container)) return null;
        if (!container.TryGetProperty(propertyName, out var prop)) return null;
        if (!prop.TryGetProperty("description", out var desc)) return null;
        return desc.GetString();
    }

    private static string[]? GetEnumFromElement(JsonElement parent, string containerProp, string propertyName)
    {
        if (!parent.TryGetProperty(containerProp, out var container)) return null;
        if (!container.TryGetProperty(propertyName, out var prop)) return null;
        if (!prop.TryGetProperty("enum", out var enumArr)) return null;
        if (enumArr.ValueKind != JsonValueKind.Array) return null;

        var result = new List<string>();
        foreach (var item in enumArr.EnumerateArray())
            if (item.GetString() is { } s) result.Add(s);
        return result.Count > 0 ? [.. result] : null;
    }

    private static string? FindSchemaFile()
    {
        // Walk up from executing assembly to find whfmt.schema.json
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        var candidate = Path.Combine(dir, "whfmt.schema.json");
        if (File.Exists(candidate)) return candidate;

        // Fallback: search from source tree (dev scenario)
        var current = dir;
        for (var i = 0; i < 8; i++)
        {
            var parent = Directory.GetParent(current)?.FullName;
            if (parent is null) break;
            candidate = Path.Combine(parent, "Core", "WpfHexEditor.Core.Definitions", "whfmt.schema.json");
            if (File.Exists(candidate)) return candidate;
            current = parent;
        }

        return null;
    }
}
