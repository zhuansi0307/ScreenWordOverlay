using System.Windows;
using System.Windows.Controls;

namespace ScreenWordOverlay.Windows;

public partial class SelectionWindow : Window
{
    public SelectionWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 覆盖所有屏幕
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    /// <summary>
    /// 更新选区矩形显示
    /// </summary>
    public void UpdateSelection(double x, double y, double width, double height)
    {
        SelectionRect.Visibility = Visibility.Visible;
        SizeLabel.Visibility = Visibility.Visible;

        Canvas.SetLeft(SelectionRect, x - SystemParameters.VirtualScreenLeft);
        Canvas.SetTop(SelectionRect, y - SystemParameters.VirtualScreenTop);
        SelectionRect.Width = width;
        SelectionRect.Height = height;

        // 尺寸标签显示在选区右下方
        Canvas.SetLeft(SizeLabel, x - SystemParameters.VirtualScreenLeft + width + 4);
        Canvas.SetTop(SizeLabel, y - SystemParameters.VirtualScreenTop + height + 4);
        SizeText.Text = $"{(int)width} × {(int)height}";
    }
}
