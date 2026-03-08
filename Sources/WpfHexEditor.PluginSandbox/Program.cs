//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

// WpfHexEditor.PluginSandbox — Phase 5
// Separate process host for sandbox-isolated plugins.
// Invoked by WpfPluginHost when a plugin declares IsolationMode = Sandbox.
//
// Usage: WpfHexEditor.PluginSandbox.exe <pipeName> <assemblyPath> <entryType>
//
// Phase 5 STUB — full IPC implementation pending.
// Current behaviour: loads plugin in process, starts IPC server loop.

using System.IO.Pipes;
using System.Reflection;
using WpfHexEditor.SDK.Contracts;

namespace WpfHexEditor.PluginSandbox;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: WpfHexEditor.PluginSandbox <pipeName> <assemblyPath> <entryType>");
            return 1;
        }

        var pipeName    = args[0];
        var assemblyPath = args[1];
        var entryType   = args[2];

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        try
        {
            // 1. Connect to host via named pipe
            await using var pipe    = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(cts.Token).ConfigureAwait(false);
            await using var channel = new IpcChannel(pipe);

            // 2. Load plugin assembly
            var assembly = Assembly.LoadFrom(assemblyPath);
            var type     = assembly.GetType(entryType)
                ?? throw new TypeLoadException($"Entry point '{entryType}' not found.");
            var plugin   = (IWpfHexEditorPlugin)(Activator.CreateInstance(type)
                ?? throw new InvalidOperationException("Cannot create plugin instance."));

            // 3. IPC message loop (stub — real implementation in Phase 5)
            await RunMessageLoopAsync(channel, plugin, cts.Token).ConfigureAwait(false);
            return 0;
        }
        catch (OperationCanceledException) { return 0; }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Sandbox] Fatal: {ex.Message}");
            return 2;
        }
    }

    private static async Task RunMessageLoopAsync(IpcChannel channel, IWpfHexEditorPlugin plugin, CancellationToken ct)
    {
        // Phase 5 placeholder — deserialise method calls and invoke on plugin.
        while (!ct.IsCancellationRequested)
        {
            var message = await channel.ReceiveAsync<SandboxMessage>(ct).ConfigureAwait(false);
            if (message is null) break;

            switch (message.Method)
            {
                case "Shutdown":
                    await plugin.ShutdownAsync(ct).ConfigureAwait(false);
                    return;
                default:
                    await channel.SendAsync(new SandboxMessage { Method = "Error", Payload = $"Unknown method: {message.Method}" }, ct).ConfigureAwait(false);
                    break;
            }
        }
    }
}

internal sealed class SandboxMessage
{
    public string Method  { get; set; } = string.Empty;
    public string? Payload { get; set; }
}
