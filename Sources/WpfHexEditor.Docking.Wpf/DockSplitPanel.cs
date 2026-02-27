using System.Windows;
using System.Windows.Controls;
using WpfHexEditor.Docking.Core;
using WpfHexEditor.Docking.Core.Nodes;

namespace WpfHexEditor.Docking.Wpf;

/// <summary>
/// WPF projection of <see cref="DockSplitNode"/>: a Grid with GridSplitters between children.
/// </summary>
public class DockSplitPanel : Grid
{
    public DockSplitNode? Node { get; private set; }

    public void Bind(DockSplitNode node, Func<DockNode, UIElement> nodeFactory)
    {
        Node = node;
        Children.Clear();
        ColumnDefinitions.Clear();
        RowDefinitions.Clear();

        if (node.Children.Count == 0) return;

        var isHorizontal = node.Orientation == SplitOrientation.Horizontal;

        for (var i = 0; i < node.Children.Count; i++)
        {
            var ratio = i < node.Ratios.Count ? node.Ratios[i] : 1.0 / node.Children.Count;
            var gridLength = new GridLength(ratio, GridUnitType.Star);

            if (isHorizontal)
                ColumnDefinitions.Add(new ColumnDefinition { Width = gridLength });
            else
                RowDefinitions.Add(new RowDefinition { Height = gridLength });

            var childElement = nodeFactory(node.Children[i]);

            if (isHorizontal)
                SetColumn(childElement, i * 2); // leave room for splitters
            else
                SetRow(childElement, i * 2);

            // Re-do definitions with splitter columns/rows
            // Actually, let's rebuild properly with splitter slots
        }

        // Rebuild properly with splitter slots
        Children.Clear();
        ColumnDefinitions.Clear();
        RowDefinitions.Clear();

        for (var i = 0; i < node.Children.Count; i++)
        {
            var ratio = i < node.Ratios.Count ? node.Ratios[i] : 1.0 / node.Children.Count;
            var gridLength = new GridLength(ratio, GridUnitType.Star);

            if (isHorizontal)
                ColumnDefinitions.Add(new ColumnDefinition { Width = gridLength });
            else
                RowDefinitions.Add(new RowDefinition { Height = gridLength });

            var childElement = nodeFactory(node.Children[i]);

            if (isHorizontal)
                SetColumn(childElement, ColumnDefinitions.Count - 1 + i); // offset by splitters
            else
                SetRow(childElement, RowDefinitions.Count - 1 + i);

            // We need to add splitter columns/rows between content
        }

        // Third approach: clean rebuild with interleaved splitters
        Children.Clear();
        ColumnDefinitions.Clear();
        RowDefinitions.Clear();

        for (var i = 0; i < node.Children.Count; i++)
        {
            // Add splitter before (except for first)
            if (i > 0)
            {
                if (isHorizontal)
                    ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Pixel) });
                else
                    RowDefinitions.Add(new RowDefinition { Height = new GridLength(4, GridUnitType.Pixel) });

                var splitter = new GridSplitter
                {
                    HorizontalAlignment = isHorizontal ? HorizontalAlignment.Stretch : HorizontalAlignment.Stretch,
                    VerticalAlignment = isHorizontal ? VerticalAlignment.Stretch : VerticalAlignment.Stretch,
                    ResizeBehavior = GridResizeBehavior.PreviousAndNext,
                    Background = System.Windows.Media.Brushes.Gray
                };

                if (isHorizontal)
                {
                    splitter.Width = 4;
                    SetColumn(splitter, ColumnDefinitions.Count - 1);
                }
                else
                {
                    splitter.Height = 4;
                    SetRow(splitter, RowDefinitions.Count - 1);
                }

                Children.Add(splitter);
            }

            // Add content column/row
            var ratio = i < node.Ratios.Count ? node.Ratios[i] : 1.0 / node.Children.Count;
            if (isHorizontal)
                ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ratio, GridUnitType.Star) });
            else
                RowDefinitions.Add(new RowDefinition { Height = new GridLength(ratio, GridUnitType.Star) });

            var childElement = nodeFactory(node.Children[i]);

            if (isHorizontal)
                SetColumn(childElement, ColumnDefinitions.Count - 1);
            else
                SetRow(childElement, RowDefinitions.Count - 1);

            Children.Add(childElement);
        }
    }
}
