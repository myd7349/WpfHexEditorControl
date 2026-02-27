using System.Windows;
using System.Windows.Controls;
using WpfHexEditor.Docking.Core.Nodes;

namespace WpfHexEditor.Docking.Wpf;

/// <summary>
/// A floating window that contains a dock group (one or more tabbed items).
/// </summary>
public class FloatingWindow : Window
{
    private readonly DockTabControl _tabControl;

    public DockGroupNode? Node { get; private set; }

    /// <summary>
    /// Raised when a tab close is requested.
    /// </summary>
    public event Action<DockItem>? TabCloseRequested;

    /// <summary>
    /// Raised when a tab drag starts (for re-docking).
    /// </summary>
    public event Action<DockItem>? TabDragStarted;

    public FloatingWindow()
    {
        WindowStyle = WindowStyle.ToolWindow;
        ShowInTaskbar = false;
        Width = 400;
        Height = 300;
        ResizeMode = ResizeMode.CanResizeWithGrip;

        _tabControl = new DockTabControl();
        _tabControl.TabCloseRequested += item => TabCloseRequested?.Invoke(item);
        _tabControl.TabDragStarted += item => TabDragStarted?.Invoke(item);

        Content = _tabControl;
    }

    /// <summary>
    /// Binds this floating window to a dock group node.
    /// </summary>
    public void Bind(DockGroupNode node, Func<DockItem, object>? contentFactory = null)
    {
        Node = node;
        _tabControl.Bind(node, contentFactory);

        if (node.ActiveItem is not null)
            Title = node.ActiveItem.Title;
    }
}

/// <summary>
/// Manages creation and lifecycle of floating windows.
/// </summary>
public class FloatingWindowManager
{
    private readonly DockControl _dockControl;
    private readonly List<FloatingWindow> _windows = [];

    public IReadOnlyList<FloatingWindow> Windows => _windows;

    public FloatingWindowManager(DockControl dockControl)
    {
        _dockControl = dockControl;
    }

    /// <summary>
    /// Creates a floating window for the given item.
    /// </summary>
    public FloatingWindow CreateFloatingWindow(DockItem item, Point? position = null)
    {
        var group = new DockGroupNode();
        group.AddItem(item);

        var window = new FloatingWindow();
        window.Bind(group, _dockControl.ContentFactory);

        if (position.HasValue)
        {
            window.Left = position.Value.X;
            window.Top = position.Value.Y;
        }

        window.Closed += (_, _) => _windows.Remove(window);
        window.TabCloseRequested += i => _dockControl.Engine?.Close(i);
        window.TabDragStarted += i =>
        {
            // Re-dock support: start drag from floating window
        };

        _windows.Add(window);
        window.Owner = Window.GetWindow(_dockControl);
        window.Show();

        return window;
    }

    /// <summary>
    /// Closes all floating windows.
    /// </summary>
    public void CloseAll()
    {
        foreach (var window in _windows.ToList())
            window.Close();
    }
}
