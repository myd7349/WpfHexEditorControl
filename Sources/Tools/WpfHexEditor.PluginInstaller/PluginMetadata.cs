//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

namespace WpfHexEditor.PluginInstaller;

/// <summary>
/// Parsed metadata from a plugin's manifest.json, used by the installer UI.
/// </summary>
public sealed class PluginMetadata
{
    public string Id               { get; init; } = string.Empty;
    public string Name             { get; init; } = string.Empty;
    public string Version          { get; init; } = string.Empty;
    public string Author           { get; init; } = string.Empty;
    public string Description      { get; init; } = string.Empty;
    public string EntryPoint       { get; init; } = string.Empty;
    public string AssemblyFile     { get; init; } = string.Empty;
    public string AssemblySha256   { get; init; } = string.Empty;
    public long   AssemblySize     { get; init; }
    public bool   TrustedPublisher { get; init; }
    public string MinIDEVersion    { get; init; } = string.Empty;
}
