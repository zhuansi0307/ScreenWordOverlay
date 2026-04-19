using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScreenWordOverlay.Models;
using ScreenWordOverlay.Services;

namespace ScreenWordOverlay.Windows;

public partial class OverlayWindow : Window
{
    private readonly SettingsService _settingsService;
    private TranslationRegion? _region;

    /// <summary>
    /// 上次渲染的内容哈希，用于避免不必要的重绘（防闪烁）
    /// </summary>
    private string _lastRenderHash = "";

    public OverlayWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
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
    /// 绑定翻译区域并渲染
    /// </summary>
    public void BindRegion(TranslationRegion region)
    {
        _region = region;
        _lastRenderHash = "";
        RenderTranslations();
    }

    /// <summary>
    /// 计算当前翻译内容的哈希，用于判断是否需要重绘
    /// </summary>
    private string ComputeRenderHash()
    {
        if (_region == null) return "";
        var hash = $"{_region.IsVisible}|{_region.Words.Count}";
        foreach (var word in _region.Words)
        {
            hash += $"|{word.Translation}|{word.Bounds.X}|{word.Bounds.Y}";
        }
        return hash;
    }

    /// <summary>
    /// 根据当前区域状态渲染翻译文字
    /// </summary>
    public void RenderTranslations(bool forceRender = false)
    {
        // 计算内容哈希，如果没变化则跳过重绘（防闪烁）
        var currentHash = ComputeRenderHash();
        if (!forceRender && currentHash == _lastRenderHash) return;
        _lastRenderHash = currentHash;

        OverlayCanvas.Children.Clear();

        if (_region == null || !_region.IsVisible) return;

        var settings = _settingsService.Settings;
        var fontColor = ParseColor(settings.FontColor);
        var bgColor = ParseColor(settings.BackgroundColor);

        // 屏幕虚拟坐标偏移（用于多显示器场景）
        var offsetX = SystemParameters.VirtualScreenLeft;
        var offsetY = SystemParameters.VirtualScreenTop;

        foreach (var word in _region.Words)
        {
            // 跳过无翻译的条目
            if (string.IsNullOrEmpty(word.Translation)) continue;
            // 如果翻译和原文相同（未命中翻译），也跳过不显示覆盖
            if (word.Translation == word.Text) continue;

            var bounds = word.Bounds;

            // 创建翻译文字容器
            var border = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4, 1, 4, 1),
            };

            // 背景底色
            if (settings.ShowBackground)
            {
                border.Background = new SolidColorBrush(Color.FromArgb(bgColor.A, bgColor.R, bgColor.G, bgColor.B))
                {
                    Opacity = settings.OverlayOpacity
                };
            }

            // 翻译文字 - 限制宽度不超过原单词宽度+余量
            var maxWidth = bounds.Width + 6;
            var fontSize = settings.FontSize;

            var textBlock = new TextBlock
            {
                Text = word.Translation,
                Foreground = new SolidColorBrush(Color.FromArgb(fontColor.A, fontColor.R, fontColor.G, fontColor.B)),
                FontSize = fontSize,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontWeight = FontWeights.Normal,
                Opacity = settings.ShowBackground ? 1.0 : settings.OverlayOpacity,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = maxWidth,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            border.Child = textBlock;

            // 定位到原单词位置（减去窗口偏移），居中对齐
            var borderWidth = Math.Min(maxWidth, bounds.Width + 6);
            var left = bounds.X - offsetX + (bounds.Width - borderWidth) / 2;
            var top = bounds.Y - offsetY + (bounds.Height - fontSize) / 2 - 1;

            Canvas.SetLeft(border, left);
            Canvas.SetTop(border, top);

            OverlayCanvas.Children.Add(border);
        }
    }

    /// <summary>
    /// 更新可见性
    /// </summary>
    public void UpdateVisibility(bool visible)
    {
        if (_region == null) return;
        _region.IsVisible = visible;
        RenderTranslations();
    }

    /// <summary>
    /// 强制重绘（设置变更时调用）
    /// </summary>
    public void ForceRender()
    {
        _lastRenderHash = "";
        RenderTranslations();
    }

    /// <summary>
    /// 解析颜色字符串
    /// </summary>
    private static Color ParseColor(string colorString)
    {
        try
        {
            if (colorString.StartsWith("#") && colorString.Length == 9)
            {
                // #AARRGGBB 格式
                var hex = colorString[1..];
                var argb = Convert.ToUInt32(hex, 16);
                var a = (byte)((argb >> 24) & 0xFF);
                var r = (byte)((argb >> 16) & 0xFF);
                var g = (byte)((argb >> 8) & 0xFF);
                var b = (byte)(argb & 0xFF);
                return Color.FromArgb(a, r, g, b);
            }

            var color = (Color)ColorConverter.ConvertFromString(colorString);
            return color;
        }
        catch
        {
            return Colors.White;
        }
    }
}
