//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WpfHexEditor.Docking.Core;
using WpfHexEditor.Docking.Core.Nodes;

namespace WpfHexEditor.Docking.Wpf;

/// <summary>
/// WPF projection of <see cref="DockSplitNode"/>: a Grid with GridSplitters between children.
/// </summary>
public class DockSplitPanel : Grid
{
    private readonly List<int> _contentDefinitionIndices = [];

    public DockSplitNode? Node { get; private set; }

    /// <summary>
    /// Minimum pane size in DIPs. Prevents panels from being collapsed to zero.
    /// </summary>
    public double MinPaneSize { get; set; } = 60;

    public void Bind(DockSplitNode node, Func<DockNode, UIElement> nodeFactory)
    {
        Node = node;
        Children.Clear();
        ColumnDefinitions.Clear();
        RowDefinitions.Clear();
        _contentDefinitionIndices.Clear();

        if (node.Children.Count == 0) return;

        var isHorizontal = node.Orientation == SplitOrientation.Horizontal;

        for (var i = 0; i < node.Children.Count; i++)
        {
            // Add splitter before each child (except the first)
            if (i > 0)
            {
                if (isHorizontal)
                    ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Pixel) });
                else
                    RowDefinitions.Add(new RowDefinition { Height = new GridLength(4, GridUnitType.Pixel) });

                var splitter = new GridSplitter
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
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

                splitter.DragCompleted += OnSplitterDragCompleted;
                Children.Add(splitter);
            }

            // Add content column/row with minimum size constraint
            var ratio = i < node.Ratios.Count ? node.Ratios[i] : 1.0 / node.Children.Count;
            if (isHorizontal)
                ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width    = new GridLength(ratio, GridUnitType.Star),
                    MinWidth = MinPaneSize
                });
            else
                RowDefinitions.Add(new RowDefinition
                {
                    Height    = new GridLength(ratio, GridUnitType.Star),
                    MinHeight = MinPaneSize
                });

            _contentDefinitionIndices.Add(isHorizontal ? ColumnDefinitions.Count - 1 : RowDefinitions.Count - 1);

            var childElement = nodeFactory(node.Children[i]);

            if (isHorizontal)
                SetColumn(childElement, ColumnDefinitions.Count - 1);
            else
                SetRow(childElement, RowDefinitions.Count - 1);

            Children.Add(childElement);
        }
    }

    /// <summary>
    /// Syncs the Grid's actual column/row sizes back to the DockSplitNode ratios after a splitter drag.
    /// </summary>
    private void OnSplitterDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (Node is null || _contentDefinitionIndices.Count != Node.Children.Count) return;

        var isHorizontal = Node.Orientation == SplitOrientation.Horizontal;
        var newRatios = new double[_contentDefinitionIndices.Count];
        var totalStar = 0.0;

        for (var i = 0; i < _contentDefinitionIndices.Count; i++)
        {
            var defIndex = _contentDefinitionIndices[i];
            var star = isHorizontal
                ? ColumnDefinitions[defIndex].Width.Value
                : RowDefinitions[defIndex].Height.Value;
            newRatios[i] = star;
            totalStar += star;
        }

        if (totalStar <= 0) return;

        // Clamp ratios: never allow less than 5% for any pane
        const double minRatio = 0.05;
        for (var i = 0; i < newRatios.Length; i++)
            newRatios[i] = Math.Max(minRatio, newRatios[i] / totalStar);

        // Re-normalize after clamping
        var clampedSum = newRatios.Sum();
        Node.SetRatios(newRatios.Select(r => r / clampedSum).ToArray());
    }
}
