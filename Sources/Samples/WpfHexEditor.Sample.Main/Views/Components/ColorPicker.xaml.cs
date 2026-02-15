using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfHexEditor.Sample.Main.Views.Components
{
    /// <summary>
    /// Simple Color Picker Control with rich color palette
    /// </summary>
    public partial class ColorPicker : UserControl
    {
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(
                nameof(SelectedColor),
                typeof(Color),
                typeof(ColorPicker),
                new PropertyMetadata(Color.FromRgb(0x40, 0x40, 0xFF), OnSelectedColorChanged));

        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public event EventHandler<Color> ColorChanged;

        public ColorPicker()
        {
            InitializeComponent();
            UpdateColorDisplay();
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorPicker picker)
            {
                picker.UpdateColorDisplay();
                picker.ColorChanged?.Invoke(picker, (Color)e.NewValue);
            }
        }

        private void UpdateColorDisplay()
        {
            ColorPreview.Background = new SolidColorBrush(SelectedColor);
            HexColorText.Text = $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShowColorPalette();
        }

        private void ShowColorPalette()
        {
            // Create a popup window with color palette
            var paletteWindow = new Window
            {
                Title = "Select Color",
                Width = 320,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3))
            };

            var stackPanel = new StackPanel { Margin = new Thickness(12) };

            // Predefined colors palette
            var colorGroups = new[]
            {
                new { Name = "Highlight Colors", Colors = new[]
                {
                    Color.FromRgb(0xFF, 0xFF, 0x00), // Yellow
                    Color.FromRgb(0xFF, 0xA5, 0x00), // Orange
                    Color.FromRgb(0xFF, 0x00, 0x00), // Red
                    Color.FromRgb(0xFF, 0x00, 0xFF), // Magenta
                    Color.FromRgb(0x40, 0x40, 0xFF), // Blue (default)
                    Color.FromRgb(0x00, 0xFF, 0xFF), // Cyan
                    Color.FromRgb(0x00, 0xFF, 0x00), // Green
                    Color.FromRgb(0x80, 0x00, 0x80)  // Purple
                }},
                new { Name = "Standard Colors", Colors = new[]
                {
                    Colors.Black, Colors.DarkGray, Colors.Gray, Colors.Silver,
                    Colors.LightGray, Colors.White, Colors.Maroon, Colors.Red,
                    Colors.Orange, Colors.Yellow, Colors.Olive, Colors.Green,
                    Colors.Teal, Colors.Cyan, Colors.Navy, Colors.Blue
                }}
            };

            foreach (var group in colorGroups)
            {
                // Group label
                stackPanel.Children.Add(new TextBlock
                {
                    Text = group.Name,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 12, 0, 6),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x32, 0x31, 0x30))
                });

                // Color buttons grid
                var uniformGrid = new UniformGrid
                {
                    Columns = 8,
                    Rows = (group.Colors.Length + 7) / 8
                };

                foreach (var color in group.Colors)
                {
                    var colorButton = new Border
                    {
                        Width = 32,
                        Height = 32,
                        Margin = new Thickness(2),
                        Background = new SolidColorBrush(color),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0xD1, 0xD1, 0xD1)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        Cursor = Cursors.Hand
                    };

                    colorButton.MouseDown += (s, e) =>
                    {
                        SelectedColor = color;
                        paletteWindow.Close();
                    };

                    // Highlight selected color
                    if (color == SelectedColor)
                    {
                        colorButton.BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
                        colorButton.BorderThickness = new Thickness(3);
                    }

                    uniformGrid.Children.Add(colorButton);
                }

                stackPanel.Children.Add(uniformGrid);
            }

            // Custom color button (removed - would require System.Windows.Forms reference)
            // Users can pick from the comprehensive palette above

            var scrollViewer = new ScrollViewer
            {
                Content = stackPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            paletteWindow.Content = scrollViewer;
            paletteWindow.ShowDialog();
        }
    }
}
