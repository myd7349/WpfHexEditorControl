//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

namespace WpfHexEditor.PluginInstaller;

/// <summary>
/// Thrown when a plugin package fails validation or extraction.
/// </summary>
public sealed class PluginInstallException(string message, Exception? inner = null)
    : Exception(message, inner);
