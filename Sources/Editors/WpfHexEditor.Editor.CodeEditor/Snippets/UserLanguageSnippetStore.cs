//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WpfHexEditor.Core.ProjectSystem.Languages;

namespace WpfHexEditor.Editor.CodeEditor.Snippets;

/// <summary>
/// Loads per-language snippet files dropped by the user into
/// <c>%AppData%\WpfHexEditor\Snippets\{languageId}.json</c>.
///
/// Each file is a JSON array of <see cref="LanguageSnippetEntry"/> objects:
/// <code>
/// [ { "trigger": "log", "body": "Console.WriteLine($cursor);", "description": "log" } ]
/// </code>
///
/// Files are scanned once on first access (lazy, thread-safe).
/// This store is read-only — users manage files externally.
/// It populates tier-2b in <see cref="CodeEditorFactory.BuildSnippetManager"/>:
///   DefaultSnippetPack → LanguageDefinition.Snippets → <b>here</b> → UserSnippetStore.
/// </summary>
public sealed class UserLanguageSnippetStore
{
    public static readonly UserLanguageSnippetStore Instance = new();

    private static readonly string SnippetsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WpfHexEditor", "Snippets");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas         = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
    };

    private readonly Lazy<Dictionary<string, List<SnippetDefinition>>> _data;

    private UserLanguageSnippetStore()
    {
        _data = new Lazy<Dictionary<string, List<SnippetDefinition>>>(
            Load, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Returns snippets contributed by the user for <paramref name="languageId"/>.</summary>
    public IEnumerable<SnippetDefinition> GetForLanguage(string languageId)
    {
        if (!_data.Value.TryGetValue(languageId.ToLowerInvariant(), out var list))
            return [];
        return list;
    }

    private static Dictionary<string, List<SnippetDefinition>> Load()
    {
        var result = new Dictionary<string, List<SnippetDefinition>>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(SnippetsDir)) return result;

        foreach (var file in Directory.EnumerateFiles(SnippetsDir, "*.json"))
        {
            var languageId = Path.GetFileNameWithoutExtension(file);
            try
            {
                var json     = File.ReadAllText(file);
                var entries  = JsonSerializer.Deserialize<List<LanguageSnippetEntry>>(json, JsonOptions);
                if (entries is null) continue;

                var defs = entries
                    .Where(e => !string.IsNullOrWhiteSpace(e.Trigger) && !string.IsNullOrWhiteSpace(e.Body))
                    .Select(e => new SnippetDefinition
                    {
                        Trigger     = e.Trigger!,
                        Body        = e.Body!,
                        Description = e.Description ?? string.Empty,
                    })
                    .ToList();

                if (defs.Count > 0)
                    result[languageId] = defs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[UserLanguageSnippetStore] skipped '{file}': {ex.Message}");
            }
        }

        return result;
    }

    // DTO used only during JSON deserialization — avoids coupling to SnippetDefinition's required init.
    private sealed class LanguageSnippetEntry
    {
        [JsonPropertyName("trigger")]     public string? Trigger     { get; set; }
        [JsonPropertyName("body")]        public string? Body        { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
    }
}
