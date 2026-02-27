namespace WpfHexEditor.Docking.Core;

/// <summary>
/// Flags controlling which layout operations are locked.
/// </summary>
[Flags]
public enum DockLockMode
{
    None = 0,
    PreventSplitting = 1,
    PreventUndocking = 2,
    PreventClosing = 4,
    Full = PreventSplitting | PreventUndocking | PreventClosing
}
