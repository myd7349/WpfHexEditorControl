using System.Windows;
using System.Windows.Controls;
using WpfHexEditor.Docking.Core;
using WpfHexEditor.Docking.Core.Nodes;

namespace WpfHexEditor.Docking.Wpf;

/// <summary>
/// Main WPF control for the docking system.
/// Renders the <see cref="DockLayoutRoot"/> tree as WPF visual elements.
/// </summary>
public class DockControl : ContentControl
{
    private DockEngine? _engine;

    public static readonly DependencyProperty LayoutProperty =
        DependencyProperty.Register(
            nameof(Layout),
            typeof(DockLayoutRoot),
            typeof(DockControl),
            new PropertyMetadata(null, OnLayoutChanged));

    /// <summary>
    /// The dock layout root to render.
    /// </summary>
    public DockLayoutRoot? Layout
    {
        get => (DockLayoutRoot?)GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    /// <summary>
    /// The engine managing the layout. Created automatically when Layout is set.
    /// </summary>
    public DockEngine? Engine => _engine;

    /// <summary>
    /// Factory to create content for a DockItem. If not set, a default placeholder is shown.
    /// </summary>
    public Func<DockItem, object>? ContentFactory { get; set; }

    /// <summary>
    /// Raised when a tab drag starts.
    /// </summary>
    public event Action<DockItem>? TabDragStarted;

    /// <summary>
    /// Raised when a tab close is requested.
    /// </summary>
    public event Action<DockItem>? TabCloseRequested;

    private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockControl control)
        {
            if (e.OldValue is DockLayoutRoot oldLayout && control._engine is not null)
                control._engine.LayoutChanged -= control.OnLayoutTreeChanged;

            if (e.NewValue is DockLayoutRoot newLayout)
            {
                control._engine = new DockEngine(newLayout);
                control._engine.LayoutChanged += control.OnLayoutTreeChanged;
                control.RebuildVisualTree();
            }
            else
            {
                control._engine = null;
                control.Content = null;
            }
        }
    }

    private void OnLayoutTreeChanged()
    {
        Dispatcher.Invoke(RebuildVisualTree);
    }

    /// <summary>
    /// Rebuilds the entire visual tree from the current Layout.
    /// </summary>
    public void RebuildVisualTree()
    {
        if (Layout is null)
        {
            Content = null;
            return;
        }

        Content = CreateVisualForNode(Layout.RootNode);
    }

    private UIElement CreateVisualForNode(DockNode node)
    {
        return node switch
        {
            DockSplitNode split => CreateSplitPanel(split),
            DocumentHostNode docHost => CreateDocumentHost(docHost),
            DockGroupNode group => CreateTabControl(group),
            _ => new TextBlock { Text = $"Unknown node: {node.GetType().Name}" }
        };
    }

    private DockSplitPanel CreateSplitPanel(DockSplitNode split)
    {
        var panel = new DockSplitPanel();
        panel.Bind(split, CreateVisualForNode);
        return panel;
    }

    private DocumentTabHost CreateDocumentHost(DocumentHostNode docHost)
    {
        var host = new DocumentTabHost();

        if (docHost.IsEmpty)
        {
            host.ShowEmptyPlaceholder();
        }
        else
        {
            host.Bind(docHost, ContentFactory);
            host.TabDragStarted += item => TabDragStarted?.Invoke(item);
            host.TabCloseRequested += item => TabCloseRequested?.Invoke(item);
        }

        return host;
    }

    private DockTabControl CreateTabControl(DockGroupNode group)
    {
        var tabControl = new DockTabControl();
        tabControl.Bind(group, ContentFactory);
        tabControl.TabDragStarted += item => TabDragStarted?.Invoke(item);
        tabControl.TabCloseRequested += item => TabCloseRequested?.Invoke(item);
        return tabControl;
    }
}
