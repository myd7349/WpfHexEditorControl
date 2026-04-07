// ==========================================================
// Project: WpfHexEditor.Plugins.XamlDesigner
// File: Services/XamlDesignerServiceImpl.cs
// Created: 2026-04-06
// Description:
//     Implements IXamlDesignerService — the SDK bridge that exposes the active
//     XAML Designer's live element tree to other plugins via ExtensionRegistry.
//
// Architecture Notes:
//     Registered by XamlDesignerPlugin as IXamlDesignerService via ExtensionRegistry.
//     Tracks the active XamlDesignerSplitHost via IFocusContextService.FocusChanged.
//     Builds XamlDesignerNode trees by walking DesignCanvas.DesignRoot with VisualTreeHelper.
//     DesignCanvas.GetUidOf() maps each UIElement to its XamlElementMapper UID.
//     Max depth 32; unnamed children beyond depth 8 are pruned to avoid noise.
// ==========================================================

using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WpfHexEditor.Editor.Core.Documents;
using WpfHexEditor.Editor.XamlDesigner.Controls;
using WpfHexEditor.SDK.Contracts;
using WpfHexEditor.SDK.Contracts.Focus;
using WpfHexEditor.SDK.Contracts.Services;
using WpfHexEditor.SDK.ExtensionPoints.XamlDesigner;

namespace WpfHexEditor.Plugins.XamlDesigner.Services;

/// <summary>
/// Implements <see cref="IXamlDesignerService"/> — exposes the active XAML Designer's
/// element tree to other plugins via the SDK ExtensionRegistry bridge.
/// </summary>
internal sealed class XamlDesignerServiceImpl : IXamlDesignerService
{
    private readonly IFocusContextService    _focusContext;
    private readonly IDocumentHostService    _documentHost;
    private readonly Dispatcher              _dispatcher = Application.Current.Dispatcher;
    private XamlDesignerSplitHost?           _activeHost;

    // Cached on the UI thread so CanProvide() is safe from background threads.
    private volatile bool _isDesignerActive;

    // ── IXamlDesignerService ─────────────────────────────────────────────────

    public bool IsDesignerActive => _isDesignerActive;

    public int SelectedElementUid => _activeHost?.Canvas?.SelectedElementUid ?? -1;

    public event EventHandler?    ElementTreeChanged;
    public event EventHandler<int>? SelectedElementChanged;

    // ── Constructor ──────────────────────────────────────────────────────────

    public XamlDesignerServiceImpl(IFocusContextService focusContext, IDocumentHostService documentHost)
    {
        _focusContext = focusContext;
        _documentHost = documentHost;
        _focusContext.FocusChanged += OnFocusChanged;
    }

    public void Dispose()
    {
        _focusContext.FocusChanged -= OnFocusChanged;
        UnwireHost();
    }

    // ── IXamlDesignerService: GetElementTree ─────────────────────────────────

    public IReadOnlyList<XamlDesignerNode> GetElementTree()
    {
        // Must walk the WPF visual tree on the UI thread — marshal if called from background.
        if (!_dispatcher.CheckAccess())
            return _dispatcher.Invoke(GetElementTree);

        var root = _activeHost?.Canvas?.DesignRoot;
        if (root is null) return [];

        var node = BuildNode(root, _activeHost!.Canvas!, depth: 0);
        return node is not null ? [node] : [];
    }

    public void SelectElement(int uid)
        => _activeHost?.Canvas?.SelectElementByUid(uid);

    // ── Focus tracking ───────────────────────────────────────────────────────

    private void OnFocusChanged(object? sender, FocusChangedEventArgs e)
    {
        var host = ResolveHost(e.ActiveDocument);
        if (ReferenceEquals(host, _activeHost)) return;

        UnwireHost();
        _activeHost = host;
        _isDesignerActive = host?.Canvas?.DesignRoot is not null;
        WireHost(host);

        ElementTreeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void WireHost(XamlDesignerSplitHost? host)
    {
        if (host?.Canvas is not { } canvas) return;
        canvas.DesignRendered      += OnDesignRendered;
        canvas.SelectedElementChanged += OnCanvasSelectionChanged;
    }

    private void UnwireHost()
    {
        if (_activeHost?.Canvas is not { } canvas) return;
        canvas.DesignRendered         -= OnDesignRendered;
        canvas.SelectedElementChanged -= OnCanvasSelectionChanged;
    }

    private void OnDesignRendered(object? sender, UIElement? _)
    {
        _isDesignerActive = _activeHost?.Canvas?.DesignRoot is not null;
        ElementTreeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnCanvasSelectionChanged(object? sender, EventArgs _)
        => SelectedElementChanged?.Invoke(this, _activeHost?.Canvas?.SelectedElementUid ?? -1);

    // ── Host resolution ───────────────────────────────────────────────────────

    private XamlDesignerSplitHost? ResolveHost(IDocument? doc)
    {
        if (doc is null) return null;
        var model = _documentHost.Documents.OpenDocuments
            .FirstOrDefault(d => d.ContentId == doc.ContentId);
        return model?.AssociatedEditor as XamlDesignerSplitHost;
    }

    // ── Tree builder ─────────────────────────────────────────────────────────

    private const int MaxDepth       = 32;
    private const int PruneDepth     = 8;   // unnamed children pruned beyond this depth

    // WPF internal types that add noise without semantic value.
    private static readonly HashSet<string> SkippedTypes = new(StringComparer.Ordinal)
    {
        "AdornerDecorator", "AdornerLayer", "ContentPresenter",
        "ScrollContentPresenter", "TemplatedAdorner",
        "InkPresenter", "GlyphsHostVisual",
    };

    private static XamlDesignerNode? BuildNode(UIElement el, DesignCanvas canvas, int depth)
    {
        if (depth > MaxDepth) return null;

        var typeName = el.GetType().Name;
        if (SkippedTypes.Contains(typeName)) return null;

        var name = el is FrameworkElement fe ? (string.IsNullOrEmpty(fe.Name) ? null : fe.Name) : null;

        // Prune anonymous elements at deep levels to avoid inflating the tree.
        if (depth > PruneDepth && name is null) return null;

        var uid = canvas.GetUidOf(el);

        var children = new List<XamlDesignerNode>();
        int count = VisualTreeHelper.GetChildrenCount(el);
        for (int i = 0; i < count; i++)
        {
            if (VisualTreeHelper.GetChild(el, i) is UIElement child)
            {
                var childNode = BuildNode(child, canvas, depth + 1);
                if (childNode is not null)
                    children.Add(childNode);
            }
        }

        return new XamlDesignerNode
        {
            Uid      = uid,
            TypeName = typeName,
            Name     = name,
            Children = children,
        };
    }
}
