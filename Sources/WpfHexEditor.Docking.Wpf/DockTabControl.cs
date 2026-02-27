using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfHexEditor.Docking.Core.Nodes;

namespace WpfHexEditor.Docking.Wpf;

/// <summary>
/// WPF projection of <see cref="DockGroupNode"/>: a TabControl with draggable tabs.
/// </summary>
public class DockTabControl : TabControl
{
    public DockGroupNode? Node { get; private set; }

    /// <summary>
    /// Raised when a tab drag starts. Provides the DockItem being dragged.
    /// </summary>
    public event Action<DockItem>? TabDragStarted;

    /// <summary>
    /// Raised when a tab close is requested.
    /// </summary>
    public event Action<DockItem>? TabCloseRequested;

    public void Bind(DockGroupNode node, Func<DockItem, object>? contentFactory = null)
    {
        Node = node;
        Items.Clear();

        foreach (var item in node.Items)
        {
            var tabItem = CreateTabItem(item, contentFactory);
            Items.Add(tabItem);
        }

        if (node.ActiveItem is not null)
        {
            var activeIndex = node.Items.ToList().IndexOf(node.ActiveItem);
            if (activeIndex >= 0)
                SelectedIndex = activeIndex;
        }
    }

    private TabItem CreateTabItem(DockItem item, Func<DockItem, object>? contentFactory)
    {
        var header = new DockTabHeader(item);
        header.CloseClicked += () => TabCloseRequested?.Invoke(item);
        header.DragStarted += () => TabDragStarted?.Invoke(item);

        var tabItem = new TabItem
        {
            Header = header,
            Content = contentFactory?.Invoke(item) ?? new TextBlock
            {
                Text = $"Content: {item.Title}",
                Margin = new Thickness(8),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            },
            Tag = item
        };

        return tabItem;
    }
}

/// <summary>
/// Tab header with title, close button, and drag support.
/// </summary>
public class DockTabHeader : StackPanel
{
    private readonly DockItem _item;
    private Point _dragStartPoint;
    private bool _isDragging;

    public event Action? CloseClicked;
    public event Action? DragStarted;

    public DockTabHeader(DockItem item)
    {
        _item = item;
        Orientation = Orientation.Horizontal;

        var titleBlock = new TextBlock
        {
            Text = item.Title,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };
        Children.Add(titleBlock);

        if (item.CanClose)
        {
            var closeButton = new Button
            {
                Content = "\u00D7", // multiplication sign (x)
                FontSize = 10,
                Padding = new Thickness(2, 0, 2, 0),
                Margin = new Thickness(0),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeButton.Click += (_, _) => CloseClicked?.Invoke();
            Children.Add(closeButton);
        }

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
        _isDragging = false;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _isDragging) return;

        var currentPos = e.GetPosition(this);
        var diff = currentPos - _dragStartPoint;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            _isDragging = true;
            ReleaseMouseCapture();
            DragStarted?.Invoke();
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        ReleaseMouseCapture();
    }
}
