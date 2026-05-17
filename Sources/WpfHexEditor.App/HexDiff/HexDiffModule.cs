// Project      : WpfHexEditor.App
// File         : HexDiff/HexDiffModule.cs
// Description  : App-layer module providing the Hex Diff / Patch panel.
//                Follows the same pattern as BinaryAnalysisModule.
// Architecture : Lazy-init via EnsureActivated(); single panel singleton.

using System.Windows;
using WpfHexEditor.App.HexDiff.Panels;
using WpfHexEditor.SDK.Contracts;

namespace WpfHexEditor.App.HexDiff;

internal sealed class HexDiffModule
{
    public const string ContentId = "panel-hex-diff";

    private IIDEHostContext? _context;
    private bool             _activated;
    private HexDiffPanel?    _panel;

    public System.Threading.Tasks.Task InitializeAsync(IIDEHostContext context)
    {
        _context = context;
        context.HexEditor.FileOpened          += OnFileOpened;
        context.HexEditor.ActiveEditorChanged += OnActiveEditorChanged;
        return System.Threading.Tasks.Task.CompletedTask;
    }

    public void Shutdown()
    {
        if (_context is null) return;
        _context.HexEditor.FileOpened          -= OnFileOpened;
        _context.HexEditor.ActiveEditorChanged -= OnActiveEditorChanged;
        _context = null;
    }

    public UIElement? GetPanel(string contentId)
    {
        if (contentId != ContentId) return null;
        EnsureActivated();
        return _panel;
    }

    private void EnsureActivated()
    {
        if (_activated || _context is null) return;
        _activated = true;
        _panel     = new HexDiffPanel();
        _panel.SetContext(_context);
    }

    private void OnFileOpened(object? sender, EventArgs e)
    {
        if (_activated) _panel?.OnFileOpened();
    }

    private void OnActiveEditorChanged(object? sender, EventArgs e) => OnFileOpened(sender, e);
}
