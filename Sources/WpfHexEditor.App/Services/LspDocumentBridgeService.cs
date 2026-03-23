// ==========================================================
// Project: WpfHexEditor.App
// File: Services/LspDocumentBridgeService.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-23
// Description:
//     Wires DocumentManager document lifecycle events to LspBufferBridge instances.
//     For each registered document whose buffer has a known LanguageId,
//     the service obtains (or reuses) an initialized ILspClient from the registry
//     and creates a bridge so the LSP server is notified of didOpen/didChange/didClose.
//
// Architecture Notes:
//     Pattern: Service / Coordinator
//     - One LspClient per language ID (lazy, initialized once, shared across documents).
//     - One LspBufferBridge per (filePath × languageId) pair.
//     - Best-effort: LSP failures are caught, logged, and do not block the editor.
//     - Must be created on and operate on the WPF Dispatcher thread.
// ==========================================================

using WpfHexEditor.Editor.Core.Documents;
using WpfHexEditor.Editor.Core.LSP;
using WpfHexEditor.LSP.Client.Services;

namespace WpfHexEditor.App.Services;

/// <summary>
/// Coordinates <see cref="LspBufferBridge"/> creation/disposal in response to
/// <see cref="IDocumentManager"/> document lifecycle events.
/// </summary>
internal sealed class LspDocumentBridgeService : IDisposable
{
    private readonly IDocumentManager              _documentManager;
    private readonly ILspServerRegistry            _registry;
    private readonly Action<string>                _log;

    // One initialized LSP client per language ID (created lazily, shared across documents).
    private readonly Dictionary<string, ILspClient>       _clients = new(StringComparer.OrdinalIgnoreCase);

    // One bridge per open document file path.
    private readonly Dictionary<string, LspBufferBridge>  _bridges = new(StringComparer.OrdinalIgnoreCase);

    private bool _disposed;

    // ── Construction ──────────────────────────────────────────────────────────

    internal LspDocumentBridgeService(
        IDocumentManager   documentManager,
        ILspServerRegistry registry,
        Action<string>     log)
    {
        _documentManager = documentManager ?? throw new ArgumentNullException(nameof(documentManager));
        _registry        = registry        ?? throw new ArgumentNullException(nameof(registry));
        _log             = log             ?? (_ => { });

        _documentManager.DocumentRegistered   += OnDocumentRegistered;
        _documentManager.DocumentUnregistered += OnDocumentUnregistered;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnDocumentRegistered(object? sender, DocumentModel doc)
    {
        // Buffer is attached slightly after Register, via AttachEditor — check asynchronously.
        // Use a short background dispatch so the buffer is already set by the time we check.
        System.Windows.Application.Current?.Dispatcher.InvokeAsync(
            () => TryCreateBridge(doc),
            System.Windows.Threading.DispatcherPriority.Background);
    }

    private void TryCreateBridge(DocumentModel doc)
    {
        if (_disposed || doc.Buffer is not { } buffer) return;
        if (string.IsNullOrEmpty(buffer.LanguageId))   return;
        if (_bridges.ContainsKey(buffer.FilePath))     return;  // already bridged

        var entry = _registry.FindByLanguage(buffer.LanguageId);
        if (entry is null || !entry.IsEnabled) return;

        _ = InitBridgeAsync(entry, buffer);
    }

    private async Task InitBridgeAsync(LspServerEntry entry, IDocumentBuffer buffer)
    {
        try
        {
            if (!_clients.TryGetValue(entry.LanguageId, out var client))
            {
                client = _registry.CreateClient(entry);
                await client.InitializeAsync().ConfigureAwait(true);  // true = resume on UI thread
                _clients[entry.LanguageId] = client;
                _log($"[LSP] Server initialized for '{entry.LanguageId}'.");
            }

            if (_disposed || _bridges.ContainsKey(buffer.FilePath)) return;

            var bridge = new LspBufferBridge(client, buffer);
            _bridges[buffer.FilePath] = bridge;
            _log($"[LSP] Bridge created: {System.IO.Path.GetFileName(buffer.FilePath)} → {entry.LanguageId}");
        }
        catch (Exception ex)
        {
            _log($"[LSP] Bridge init failed for '{buffer.FilePath}': {ex.Message}");
        }
    }

    private void OnDocumentUnregistered(object? sender, DocumentModel doc)
    {
        if (doc.Buffer is not { } buffer) return;
        if (_bridges.TryGetValue(buffer.FilePath, out var bridge))
        {
            bridge.Dispose();
            _bridges.Remove(buffer.FilePath);
        }
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _documentManager.DocumentRegistered   -= OnDocumentRegistered;
        _documentManager.DocumentUnregistered -= OnDocumentUnregistered;

        foreach (var bridge in _bridges.Values)
            bridge.Dispose();
        _bridges.Clear();

        foreach (var client in _clients.Values)
            client.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _clients.Clear();
    }
}
