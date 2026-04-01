// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: Connection/NetworkAvailabilityMonitor.cs
// Description: Monitors system network availability; fires event on change.

using System.Net.NetworkInformation;

namespace WpfHexEditor.Plugins.ClaudeAssistant.Connection;

public sealed class NetworkAvailabilityMonitor : IDisposable
{
    public bool IsAvailable { get; private set; } = NetworkInterface.GetIsNetworkAvailable();
    public event EventHandler<bool>? AvailabilityChanged;

    public NetworkAvailabilityMonitor()
    {
        NetworkChange.NetworkAvailabilityChanged += OnNetworkChanged;
    }

    private void OnNetworkChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        IsAvailable = e.IsAvailable;
        AvailabilityChanged?.Invoke(this, e.IsAvailable);
    }

    public void Dispose()
    {
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkChanged;
    }
}
