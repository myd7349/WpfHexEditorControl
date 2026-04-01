// Project: WpfHexEditor.MCP.IDEServers
// File: Base/IIDEMcpServer.cs
// Description: Contract for in-process MCP servers that expose IDE state as tools.

namespace WpfHexEditor.MCP.IDEServers.Base;

public interface IIDEMcpServer : IAsyncDisposable
{
    string ServerId { get; }
    string DisplayName { get; }

    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);

    IReadOnlyList<McpToolDefinition> GetToolDefinitions();
    Task<string> CallToolAsync(string toolName, string argsJson, CancellationToken ct = default);
}

public sealed record McpToolDefinition(
    string Name,
    string Description,
    string InputSchemaJson);
