// ==========================================================
// Project: WpfHexEditor.Plugins.ClassDiagram
// File: Analysis/ClassDiagramAIGenerator.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-04-07
// Description:
//     AI-assisted class diagram generation from natural language.
//     Delegates to whichever provider the user has configured in
//     AI Assistant settings (Anthropic, OpenAI, Azure OpenAI,
//     Gemini, Ollama). Falls back to a pattern skeleton when no
//     provider is reachable.
//
// Architecture Notes:
//     Reads AIAssistant settings.json directly (no inter-plugin
//     reference) to get DefaultProviderId, DefaultModelId, and
//     the DPAPI-encrypted API key for the active provider.
//     Each provider speaks its own HTTP wire format internally;
//     all share the same system prompt and user message.
// ==========================================================

using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WpfHexEditor.Editor.ClassDiagram.Core.Layout;
using WpfHexEditor.Editor.ClassDiagram.Core.Model;
using WpfHexEditor.Editor.ClassDiagram.Core.Parser;
using WpfHexEditor.Plugins.ClassDiagram.Options;

namespace WpfHexEditor.Plugins.ClassDiagram.Analysis;

/// <summary>
/// Generates a <see cref="DiagramDocument"/> from a natural-language description
/// using the AI provider configured in AI Assistant settings.
/// </summary>
public sealed class ClassDiagramAIGenerator : IDisposable
{
    private readonly HttpClient _http = new();
    private bool _disposed;

    // ── System prompt ─────────────────────────────────────────────────────────

    private const string SystemPrompt = """
        You are a UML class diagram DSL generator.
        Given a natural language description, output ONLY the diagram in the exact DSL format shown below.
        No Markdown fences (no ```), no prose, no comments — only valid DSL.

        DSL syntax:
          class ClassName {
            +publicMethod(): ReturnType
            -privateField: Type
            #protectedProp: Type
          }
          interface IName { +method(): void }
          enum EnumName { Value1, Value2 }
          ClassName --> OtherClass
          ClassName implements IName
          ClassName extends BaseClass

        Rules:
        - Output 3–7 classes/interfaces that fully model the described design.
        - Use realistic domain names and types (not "Field1: Type").
        - Every interface mentioned in "implements" must be declared.
        - Every base class mentioned in "extends" must be declared.
        - Use --> for associations (has-a), extends for inheritance, implements for interface realization.
        - Enums are optional; include them only when they add meaning.

        Examples:

        Input: Repository pattern for a User entity
        Output:
        interface IUserRepository {
          +findById(id: int): User
          +findAll(): List<User>
          +save(user: User): void
          +delete(id: int): void
        }
        class UserRepository {
          -_db: DbContext
          +findById(id: int): User
          +findAll(): List<User>
          +save(user: User): void
          +delete(id: int): void
        }
        class User {
          +Id: int
          +Name: string
          +Email: string
        }
        UserRepository implements IUserRepository
        UserRepository --> User

        Input: Observer pattern
        Output:
        interface IObserver {
          +update(event: string): void
        }
        interface ISubject {
          +subscribe(o: IObserver): void
          +unsubscribe(o: IObserver): void
          +notify(): void
        }
        class EventSource {
          -_observers: List<IObserver>
          -_state: string
          +subscribe(o: IObserver): void
          +unsubscribe(o: IObserver): void
          +notify(): void
          +setState(state: string): void
        }
        class ConcreteObserver {
          -_name: string
          +update(event: string): void
        }
        EventSource implements ISubject
        ConcreteObserver implements IObserver
        EventSource --> IObserver
        """;

    // ── Provider config (read from AIAssistant settings.json) ─────────────────

    public sealed record ProviderConfig(
        string ProviderId,
        string ModelId,
        string? ApiKey,
        string? AzureEndpoint,
        string? AzureDeployment,
        string? OllamaBaseUrl);

    /// <summary>
    /// Loads the active provider config from AIAssistant settings.json.
    /// Returns null if the settings file does not exist.
    /// </summary>
    public static ProviderConfig? LoadProviderConfig()
    {
        try
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WpfHexEditor", "AIAssistant", "settings.json");

            if (!File.Exists(path)) return null;

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var json = JsonNode.Parse(File.ReadAllText(path));
            if (json is null) return null;

            string providerId = json["defaultProviderId"]?.GetValue<string>() ?? "anthropic";
            string modelId    = json["defaultModelId"]?.GetValue<string>()    ?? "claude-sonnet-4-6";

            string? apiKey = DecryptKey(providerId switch
            {
                "anthropic"    => json["encryptedAnthropicKey"]?.GetValue<string>(),
                "openai"       => json["encryptedOpenAIKey"]?.GetValue<string>(),
                "azure-openai" => json["encryptedAzureOpenAIKey"]?.GetValue<string>(),
                "gemini"       => json["encryptedGeminiKey"]?.GetValue<string>(),
                _              => null
            });

            string? azureEndpoint   = json["azureOpenAIEndpoint"]?.GetValue<string>();
            string? azureDeployment = json["azureOpenAIDeployment"]?.GetValue<string>();
            string? ollamaUrl       = json["ollamaBaseUrl"]?.GetValue<string>() ?? "http://localhost:11434";

            return new ProviderConfig(providerId, modelId, apiKey, azureEndpoint, azureDeployment, ollamaUrl);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Returns true when a usable provider config is available.</summary>
    public static bool HasProviderConfig()
    {
        var cfg = LoadProviderConfig();
        if (cfg is null) return false;
        return cfg.ProviderId == "ollama" || !string.IsNullOrEmpty(cfg.ApiKey);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public event EventHandler<string>? ProgressChanged;

    public async Task<DiagramDocument> GenerateAsync(
        string              userPrompt,
        ClassDiagramOptions options,
        CancellationToken   ct = default)
    {
        var cfg = LoadProviderConfig();

        if (cfg is null || (cfg.ProviderId != "ollama" && string.IsNullOrEmpty(cfg.ApiKey)))
        {
            ProgressChanged?.Invoke(this, "No AI provider configured — generating skeleton diagram…");
            return GenerateSkeleton(userPrompt, options);
        }

        ProgressChanged?.Invoke(this, $"Calling {cfg.ProviderId} / {cfg.ModelId}…");

        string dsl;
        try
        {
            dsl = await CallProviderAsync(cfg, userPrompt, ct);
        }
        catch (Exception ex)
        {
            ProgressChanged?.Invoke(this, $"API error ({cfg.ProviderId}): {ex.Message}. Generating skeleton…");
            return GenerateSkeleton(userPrompt, options);
        }

        ProgressChanged?.Invoke(this, "Parsing response…");

        var doc = ClassDiagramParser.Parse(dsl).Document;
        if (doc.Classes.Count == 0)
        {
            ProgressChanged?.Invoke(this, "Response produced no classes — generating skeleton…");
            return GenerateSkeleton(userPrompt, options);
        }

        LayoutStrategyFactory.Create(options.LayoutStrategy).Layout(doc);
        ProgressChanged?.Invoke(this, $"Generated {doc.Classes.Count} classes via {cfg.ProviderId}/{cfg.ModelId}.");
        return doc;
    }

    // ── Provider dispatch ─────────────────────────────────────────────────────

    private async Task<string> CallProviderAsync(
        ProviderConfig cfg, string userPrompt, CancellationToken ct)
    {
        return cfg.ProviderId switch
        {
            "anthropic"    => await CallAnthropicAsync(cfg, userPrompt, ct),
            "openai"       => await CallOpenAIAsync(cfg, userPrompt, ct),
            "azure-openai" => await CallAzureOpenAIAsync(cfg, userPrompt, ct),
            "gemini"       => await CallGeminiAsync(cfg, userPrompt, ct),
            "ollama"       => await CallOllamaAsync(cfg, userPrompt, ct),
            _              => throw new NotSupportedException($"Provider '{cfg.ProviderId}' is not supported for diagram generation.")
        };
    }

    // ── Anthropic ─────────────────────────────────────────────────────────────

    private async Task<string> CallAnthropicAsync(
        ProviderConfig cfg, string prompt, CancellationToken ct)
    {
        var body = new JsonObject
        {
            ["model"]      = cfg.ModelId,
            ["max_tokens"] = 4096,
            ["system"]     = SystemPrompt,
            ["messages"]   = new JsonArray
            {
                new JsonObject { ["role"] = "user", ["content"] = prompt }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        req.Headers.Add("x-api-key",         cfg.ApiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = JsonContent(body);

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var node = JsonNode.Parse(await resp.Content.ReadAsStringAsync(ct));
        return node?["content"]?[0]?["text"]?.GetValue<string>() ?? string.Empty;
    }

    // ── OpenAI ────────────────────────────────────────────────────────────────

    private async Task<string> CallOpenAIAsync(
        ProviderConfig cfg, string prompt, CancellationToken ct)
    {
        var body = BuildOpenAIChatBody(cfg.ModelId, prompt);

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        req.Headers.Add("Authorization", $"Bearer {cfg.ApiKey}");
        req.Content = JsonContent(body);

        return await ReadOpenAIResponse(req, ct);
    }

    // ── Azure OpenAI ──────────────────────────────────────────────────────────

    private async Task<string> CallAzureOpenAIAsync(
        ProviderConfig cfg, string prompt, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(cfg.AzureEndpoint) || string.IsNullOrEmpty(cfg.AzureDeployment))
            throw new InvalidOperationException("Azure OpenAI endpoint or deployment not configured.");

        string url = $"{cfg.AzureEndpoint.TrimEnd('/')}/openai/deployments/{cfg.AzureDeployment}/chat/completions?api-version=2024-02-01";
        var body = BuildOpenAIChatBody(cfg.ModelId, prompt);

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("api-key", cfg.ApiKey);
        req.Content = JsonContent(body);

        return await ReadOpenAIResponse(req, ct);
    }

    // ── Ollama (OpenAI-compatible) ────────────────────────────────────────────

    private async Task<string> CallOllamaAsync(
        ProviderConfig cfg, string prompt, CancellationToken ct)
    {
        string baseUrl = (cfg.OllamaBaseUrl ?? "http://localhost:11434").TrimEnd('/');
        var body = BuildOpenAIChatBody(cfg.ModelId, prompt);

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/chat/completions");
        req.Content = JsonContent(body);

        return await ReadOpenAIResponse(req, ct);
    }

    // ── Gemini ────────────────────────────────────────────────────────────────

    private async Task<string> CallGeminiAsync(
        ProviderConfig cfg, string prompt, CancellationToken ct)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{cfg.ModelId}:generateContent?key={cfg.ApiKey}";

        var body = new JsonObject
        {
            ["system_instruction"] = new JsonObject
            {
                ["parts"] = new JsonArray { new JsonObject { ["text"] = SystemPrompt } }
            },
            ["contents"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"]  = "user",
                    ["parts"] = new JsonArray { new JsonObject { ["text"] = prompt } }
                }
            },
            ["generationConfig"] = new JsonObject
            {
                ["maxOutputTokens"] = 4096
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Content = JsonContent(body);

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var node = JsonNode.Parse(await resp.Content.ReadAsStringAsync(ct));
        return node?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>() ?? string.Empty;
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private static JsonObject BuildOpenAIChatBody(string modelId, string prompt) =>
        new()
        {
            ["model"]       = modelId,
            ["max_tokens"]  = 4096,
            ["messages"]    = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = SystemPrompt },
                new JsonObject { ["role"] = "user",   ["content"] = prompt }
            }
        };

    private async Task<string> ReadOpenAIResponse(HttpRequestMessage req, CancellationToken ct)
    {
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var node = JsonNode.Parse(await resp.Content.ReadAsStringAsync(ct));
        return node?["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? string.Empty;
    }

    private static System.Net.Http.StringContent JsonContent(JsonObject body) =>
        new(body.ToJsonString(), Encoding.UTF8, "application/json");

    // ── DPAPI ─────────────────────────────────────────────────────────────────

    private static string? DecryptKey(string? base64)
    {
        if (string.IsNullOrEmpty(base64)) return null;
        try
        {
            byte[] cipher  = Convert.FromBase64String(base64);
            byte[] plain   = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch { return null; }
    }

    // ── Skeleton fallback ─────────────────────────────────────────────────────

    private static readonly Dictionary<string, string> PatternSkeletons =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["repository"] = """
                interface IRepository {
                  +findById(id: int): Entity
                  +findAll(): List<Entity>
                  +save(entity: Entity): void
                  +delete(id: int): void
                }
                class Repository {
                  -_db: DbContext
                  +findById(id: int): Entity
                  +findAll(): List<Entity>
                  +save(entity: Entity): void
                  +delete(id: int): void
                }
                class Entity {
                  +Id: int
                  +Name: string
                }
                Repository implements IRepository
                Repository --> Entity
                """,
            ["factory"] = """
                interface IProduct {
                  +operation(): string
                }
                abstract class Creator {
                  +factoryMethod(): IProduct
                  +create(): string
                }
                class ConcreteProduct {
                  +operation(): string
                }
                class ConcreteCreator {
                  +factoryMethod(): IProduct
                }
                ConcreteProduct implements IProduct
                ConcreteCreator extends Creator
                ConcreteCreator --> ConcreteProduct
                """,
            ["observer"] = """
                interface IObserver {
                  +update(event: string): void
                }
                interface ISubject {
                  +subscribe(o: IObserver): void
                  +unsubscribe(o: IObserver): void
                  +notify(): void
                }
                class EventSource {
                  -_observers: List<IObserver>
                  -_state: string
                  +subscribe(o: IObserver): void
                  +unsubscribe(o: IObserver): void
                  +notify(): void
                  +setState(state: string): void
                }
                class ConcreteObserver {
                  +update(event: string): void
                }
                EventSource implements ISubject
                ConcreteObserver implements IObserver
                EventSource --> IObserver
                """,
            ["strategy"] = """
                interface IStrategy {
                  +execute(data: string): string
                }
                class Context {
                  -_strategy: IStrategy
                  +setStrategy(s: IStrategy): void
                  +run(data: string): string
                }
                class ConcreteStrategyA {
                  +execute(data: string): string
                }
                class ConcreteStrategyB {
                  +execute(data: string): string
                }
                ConcreteStrategyA implements IStrategy
                ConcreteStrategyB implements IStrategy
                Context --> IStrategy
                """,
            ["command"] = """
                interface ICommand {
                  +execute(): void
                  +undo(): void
                }
                class Invoker {
                  -_history: List<ICommand>
                  +execute(cmd: ICommand): void
                  +undo(): void
                }
                class ConcreteCommand {
                  -_receiver: Receiver
                  +execute(): void
                  +undo(): void
                }
                class Receiver {
                  +action(): void
                }
                ConcreteCommand implements ICommand
                ConcreteCommand --> Receiver
                Invoker --> ICommand
                """,
            ["builder"] = """
                interface IBuilder {
                  +setName(name: string): IBuilder
                  +build(): Product
                }
                class ProductBuilder {
                  -_name: string
                  -_value: int
                  +setName(name: string): IBuilder
                  +setValue(v: int): IBuilder
                  +build(): Product
                }
                class Product {
                  +Name: string
                  +Value: int
                }
                class Director {
                  +construct(builder: IBuilder): Product
                }
                ProductBuilder implements IBuilder
                ProductBuilder --> Product
                Director --> IBuilder
                """,
            ["decorator"] = """
                interface IComponent {
                  +operation(): string
                }
                class ConcreteComponent {
                  +operation(): string
                }
                class BaseDecorator {
                  -_inner: IComponent
                  +operation(): string
                }
                class ConcreteDecorator {
                  -_extra: string
                  +operation(): string
                }
                ConcreteComponent implements IComponent
                BaseDecorator implements IComponent
                ConcreteDecorator extends BaseDecorator
                BaseDecorator --> IComponent
                """,
            ["facade"] = """
                class Facade {
                  -_subsystemA: SubsystemA
                  -_subsystemB: SubsystemB
                  +operation(): string
                }
                class SubsystemA {
                  +operationA(): string
                }
                class SubsystemB {
                  +operationB(): string
                }
                Facade --> SubsystemA
                Facade --> SubsystemB
                """,
            ["adapter"] = """
                interface ITarget {
                  +request(): string
                }
                class Adaptee {
                  +specificRequest(): string
                }
                class Adapter {
                  -_adaptee: Adaptee
                  +request(): string
                }
                Adapter implements ITarget
                Adapter --> Adaptee
                """,
            ["service"] = """
                interface IService {
                  +execute(request: string): string
                }
                class ServiceImpl {
                  -_repository: IRepository
                  +execute(request: string): string
                }
                interface IRepository {
                  +find(id: int): Entity
                }
                class Entity {
                  +Id: int
                  +Data: string
                }
                ServiceImpl implements IService
                ServiceImpl --> IRepository
                """,
        };

    private static DiagramDocument GenerateSkeleton(
        string prompt, ClassDiagramOptions options)
    {
        string lower = prompt.ToLowerInvariant();
        string? dsl  = null;
        foreach (var (keyword, skeleton) in PatternSkeletons)
        {
            if (lower.Contains(keyword)) { dsl = skeleton; break; }
        }

        dsl ??= """
            class GeneratedClass {
              +operation(): void
            }
            """;

        var doc = ClassDiagramParser.Parse(dsl).Document;
        LayoutStrategyFactory.Create(options.LayoutStrategy).Layout(doc);
        return doc;
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _http.Dispose();
    }
}
