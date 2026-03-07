//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace WpfHexEditor.PluginSandbox;

/// <summary>
/// Bidirectional Named Pipe IPC channel using length-prefixed JSON framing.
/// Used between the in-process SandboxPluginProxy and the PluginSandbox.exe process.
/// Phase 5 implementation — currently a stub pending proxy wiring.
/// </summary>
public sealed class IpcChannel : IAsyncDisposable
{
    private readonly PipeStream _pipe;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public IpcChannel(PipeStream pipe) => _pipe = pipe;

    public async Task SendAsync<T>(T message, CancellationToken ct = default)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(message);
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var prefix = BitConverter.GetBytes(json.Length);
            await _pipe.WriteAsync(prefix, ct).ConfigureAwait(false);
            await _pipe.WriteAsync(json, ct).ConfigureAwait(false);
            await _pipe.FlushAsync(ct).ConfigureAwait(false);
        }
        finally { _writeLock.Release(); }
    }

    public async Task<T?> ReceiveAsync<T>(CancellationToken ct = default)
    {
        var prefix = new byte[4];
        var read = await _pipe.ReadAsync(prefix.AsMemory(), ct).ConfigureAwait(false);
        if (read < 4) return default;

        int length = BitConverter.ToInt32(prefix);
        if (length <= 0 || length > 1024 * 1024) return default; // sanity guard (1 MB max)

        var buffer = new byte[length];
        read = await _pipe.ReadAsync(buffer.AsMemory(), ct).ConfigureAwait(false);
        if (read < length) return default;

        return JsonSerializer.Deserialize<T>(buffer);
    }

    public ValueTask DisposeAsync()
    {
        _writeLock.Dispose();
        _pipe.Dispose();
        return ValueTask.CompletedTask;
    }
}
