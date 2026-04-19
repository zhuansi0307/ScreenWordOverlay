using System.Windows;
using System.Windows.Media;
using ScreenWordOverlay.Models;
using ScreenWordOverlay.Services;
using ScreenWordOverlay.Windows;

namespace ScreenWordOverlay;

public partial class MainWindow : Window
{
    private readonly MouseHookService _mouseHook;
    private readonly RegionSelectionService _regionSelection;
    private readonly ScreenCaptureService _screenCapture;
    private readonly OcrService _ocrService;
    private readonly TranslationService _translationService;
    private readonly SettingsService _settingsService;

    /// <summary>
    /// 当前活跃的翻译区域列表
    /// </summary>
    private readonly List<TranslationRegion> _regions = new();

    /// <summary>
    /// 当前活跃的覆盖窗口
    /// </summary>
    private readonly List<OverlayWindow> _overlayWindows = new();

    /// <summary>
    /// 右键是否按下（用于检测右键拖拽框选）
    /// </summary>
    private bool _rightButtonDown;

    /// <summary>
    /// 右键按下时的起始坐标
    /// </summary>
    private int _rightDragStartX, _rightDragStartY;

    /// <summary>
    /// 是否已进入右键拖拽框选状态（超过阈值）
    /// </summary>
    private bool _rightDragExceededThreshold;

    /// <summary>
    /// 判定为拖拽的最小移动距离（像素）
    /// </summary>
    private const int DragThreshold = 8;

    /// <summary>
    /// 中键切换显示的状态：true=翻译暂时隐藏，中键点击可恢复
    /// </summary>
    private bool _middleButtonToggleVisible = true;

    public MainWindow()
    {
        _settingsService = App.SettingsService;
        _mouseHook = new MouseHookService();
        _screenCapture = new ScreenCaptureService();
        _ocrService = App.OcrService;
        _translationService = new TranslationService(_settingsService);

        _regionSelection = new RegionSelectionService(_mouseHook);
        _regionSelection.SelectionCompleted += OnSelectionCompleted;

        // 在所有字段初始化后再调用 InitializeComponent
        InitializeComponent();

        // 启动全局鼠标钩子
        _mouseHook.Start();

        // 注册全局鼠标事件 —— 右键拖拽框选 + 中键切换显示
        _mouseHook.RightButtonDown += OnGlobalRightButtonDown;
        _mouseHook.RightButtonUp += OnGlobalRightButtonUp;
        _mouseHook.MouseMove += OnGlobalMouseMove;
        _mouseHook.MiddleButtonDown += OnGlobalMiddleButtonDown;
        _mouseHook.MouseWheel += OnGlobalMouseWheel;

        // 更新状态
        UpdateStatusBar();
        TxtStatus.Text = "就绪 - 右键拖拽框选翻译区域";
    }

    #region 全局鼠标事件 - 交互逻辑

    /// <summary>
    /// 判断鼠标坐标是否在主窗口区域内
    /// </summary>
    private bool IsMouseOverMainWindow(int x, int y)
    {
        try
        {
            var windowLeft = Left;
            var windowTop = Top;
            var windowRight = Left + ActualWidth;
            var windowBottom = Top + ActualHeight;

            return x >= windowLeft && x <= windowRight && y >= windowTop && y <= windowBottom;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 全局右键按下 - 记录起点，准备判断是点击还是拖拽
    /// </summary>
    private void OnGlobalRightButtonDown(int x, int y)
    {
        // 如果鼠标在主窗口区域内，不处理
        if (IsMouseOverMainWindow(x, y)) return;

        _rightButtonDown = true;
        _rightDragStartX = x;
        _rightDragStartY = y;
        _rightDragExceededThreshold = false;
    }

    /// <summary>
    /// 全局鼠标移动 - 如果右键按下且移动超过阈值，进入框选
    /// </summary>
    private void OnGlobalMouseMove(int x, int y)
    {
        if (!_rightButtonDown || _rightDragExceededThreshold) return;

        var dx = Math.Abs(x - _rightDragStartX);
        var dy = Math.Abs(y - _rightDragStartY);

        if (dx > DragThreshold || dy > DragThreshold)
        {
            // 超过阈值，进入右键拖拽框选模式
            _rightDragExceededThreshold = true;

            Dispatcher.Invoke(() =>
            {
                TxtStatus.Text = "框选中...";
                _regionSelection.StartDragSelection(_rightDragStartX, _rightDragStartY);
            });
        }
    }

    /// <summary>
    /// 全局右键松开 - 判断是点击还是拖拽完成
    /// </summary>
    private void OnGlobalRightButtonUp(int x, int y)
    {
        if (!_rightButtonDown) return;
        _rightButtonDown = false;

        if (_rightDragExceededThreshold)
        {
            // 右键拖拽完成 - 由 RegionSelectionService 的 SelectionCompleted 处理
            _regionSelection.FinishDragSelection(x, y);
        }
        else
        {
            // 右键单击 - 关闭鼠标所在位置的翻译区域
            Dispatcher.Invoke(() =>
            {
                for (int i = _regions.Count - 1; i >= 0; i--)
                {
                    var region = _regions[i];
                    if (region.ScreenBounds.Contains(x, y))
                    {
                        // 关闭对应的覆盖窗口
                        if (i < _overlayWindows.Count)
                        {
                            _overlayWindows[i].Close();
                            _overlayWindows.RemoveAt(i);
                        }

                        _regions.RemoveAt(i);
                        UpdateRegionList();
                        TxtStatus.Text = "翻译区域已关闭";
                        break;
                    }
                }
            });
        }

        _rightDragExceededThreshold = false;
    }

    /// <summary>
    /// 全局中键点击 - 切换翻译显示/隐藏
    /// </summary>
    private void OnGlobalMiddleButtonDown(int x, int y)
    {
        // 如果鼠标在主窗口区域内，不处理
        if (IsMouseOverMainWindow(x, y)) return;

        Dispatcher.Invoke(() =>
        {
            // 查找鼠标所在位置的翻译区域
            TranslationRegion? targetRegion = null;
            foreach (var region in _regions)
            {
                if (region.ScreenBounds.Contains(x, y))
                {
                    targetRegion = region;
                    break;
                }
            }

            if (targetRegion != null)
            {
                // 切换该区域的显示/隐藏
                targetRegion.IsVisible = !targetRegion.IsVisible;
                RefreshOverlayForRegion(targetRegion);
                TxtStatus.Text = targetRegion.IsVisible ? "翻译已显示" : "翻译已隐藏";
            }
            else if (_regions.Count > 0)
            {
                // 不在任何翻译区域上 - 切换所有区域的显示/隐藏
                _middleButtonToggleVisible = !_middleButtonToggleVisible;
                foreach (var region in _regions)
                {
                    region.IsVisible = _middleButtonToggleVisible;
                    RefreshOverlayForRegion(region);
                }
                TxtStatus.Text = _middleButtonToggleVisible ? "所有翻译已显示" : "所有翻译已隐藏";
            }
        });
    }

    #endregion

    /// <summary>
    /// 全局鼠标滚轮 - 翻译覆盖层跟随滚动
    /// </summary>
    private void OnGlobalMouseWheel(int x, int y, int delta)
    {
        // 如果鼠标在主窗口区域内，不处理
        if (IsMouseOverMainWindow(x, y)) return;

        Dispatcher.Invoke(() =>
        {
            // 滚动偏移量：delta 为 ±120 的倍数，每次滚动约 3 行（每行约 16 像素）
            var scrollPixels = -delta * 16 * 3 / 120;

            for (int i = 0; i < _regions.Count; i++)
            {
                var region = _regions[i];

                // 判断鼠标是否在该翻译区域附近（扩展检测范围）
                var bounds = region.ScreenBounds;
                var inHorizontalRange = x >= bounds.X - 50 && x <= bounds.X + bounds.Width + 50;
                var inVerticalRange = y >= bounds.Y - 100 && y <= bounds.Y + bounds.Height + 100;

                if (inHorizontalRange && inVerticalRange)
                {
                    // 偏移该区域所有单词的 Y 坐标
                    foreach (var word in region.Words)
                    {
                        var b = word.Bounds;
                        word.Bounds = new ScreenRect(b.X, b.Y + scrollPixels, b.Width, b.Height);
                    }

                    // 偏移区域边界
                    region.ScreenBounds = new ScreenRect(
                        bounds.X, bounds.Y + scrollPixels,
                        bounds.Width, bounds.Height);

                    // 强制重绘覆盖层
                    if (i < _overlayWindows.Count)
                        _overlayWindows[i].ForceRender();
                }
            }
        });
    }

    /// <summary>
    /// 框选完成回调
    /// </summary>
    private void OnSelectionCompleted(ScreenRect selectedRect)
    {
        _rightDragExceededThreshold = false;

        Dispatcher.Invoke(() =>
        {
            TxtStatus.Text = "识别中...";

            try
            {
                // 1. 截图
                var bitmap = _screenCapture.CaptureRegion(
                    (int)selectedRect.X, (int)selectedRect.Y,
                    (int)selectedRect.Width, (int)selectedRect.Height);

                // 2. OCR 识别
                var words = _ocrService.Recognize(
                    bitmap, (int)selectedRect.X, (int)selectedRect.Y);

                if (words.Count == 0)
                {
                    TxtStatus.Text = "未识别到文字，请重试";
                    bitmap.Dispose();
                    return;
                }

                // 3. 逐词翻译（本地词典，不阻塞UI）
                _translationService.TranslateWords(words);

                // 4. 创建翻译区域
                var region = new TranslationRegion
                {
                    ScreenBounds = selectedRect,
                    Words = words,
                    IsVisible = true,
                    LastImageHash = _screenCapture.ComputeImageHash(bitmap)
                };

                _regions.Add(region);

                // 5. 创建覆盖窗口并渲染（先显示本地翻译结果）
                var overlay = new OverlayWindow(_settingsService);
                overlay.BindRegion(region);
                overlay.Show();
                _overlayWindows.Add(overlay);

                // 6. 更新 UI
                UpdateRegionList();
                var localCount = words.Count(w => w.IsTranslated);
                TxtStatus.Text = localCount < words.Count
                    ? $"本地翻译 {localCount}/{words.Count}，在线补全中..."
                    : $"已翻译 {words.Count} 个单词";

                bitmap.Dispose();

                // 7. 后台异步在线翻译未命中的单词
                if (localCount < words.Count)
                {
                    _ = Task.Run(async () =>
                    {
                        await _translationService.TranslateWordsOnlineAsync(words, () =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                overlay.ForceRender();
                                var onlineCount = words.Count(w => w.IsTranslated);
                                TxtStatus.Text = $"已翻译 {onlineCount}/{words.Count} 个单词";
                            });
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"错误: {ex.Message}";
            }
        });
    }

    /// <summary>
    /// 刷新指定区域的覆盖窗口
    /// </summary>
    private void RefreshOverlayForRegion(TranslationRegion region)
    {
        var idx = _regions.IndexOf(region);
        if (idx >= 0 && idx < _overlayWindows.Count)
        {
            _overlayWindows[idx].RenderTranslations();
        }
    }

    #region UI 事件处理

    private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            try { DragMove(); } catch (InvalidOperationException) { }
        }
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtOpacityValue == null || _settingsService == null) return;
        var val = (int)e.NewValue;
        TxtOpacityValue.Text = $"{val}%";
        _settingsService.Settings.OverlayOpacity = val / 100.0;

        foreach (var overlay in _overlayWindows)
            overlay.ForceRender();

        _settingsService.SaveSettings();
    }

    private void SliderFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtFontSizeValue == null || _settingsService == null) return;
        var val = (int)e.NewValue;
        TxtFontSizeValue.Text = $"{val}pt";
        _settingsService.Settings.FontSize = val;

        foreach (var overlay in _overlayWindows)
            overlay.ForceRender();

        _settingsService.SaveSettings();
    }

    private void ChkShowBackground_Changed(object sender, RoutedEventArgs e)
    {
        if (ChkShowBackground == null || _settingsService == null) return;
        _settingsService.Settings.ShowBackground = ChkShowBackground.IsChecked ?? true;

        foreach (var overlay in _overlayWindows)
            overlay.ForceRender();

        _settingsService.SaveSettings();
    }

    private void ChkOnlineTranslation_Changed(object sender, RoutedEventArgs e)
    {
        if (ChkOnlineTranslation == null || _settingsService == null) return;
        _settingsService.Settings.UseOnlineTranslation = ChkOnlineTranslation.IsChecked ?? true;

        _settingsService.SaveSettings();
    }

    private void FontColor_Changed(object sender, RoutedEventArgs e)
    {
        if (_settingsService == null) return;

        if (RadioWhite.IsChecked == true)
            _settingsService.Settings.FontColor = "#FFFFFFFF";
        else if (RadioYellow.IsChecked == true)
            _settingsService.Settings.FontColor = "#FFFFFF00";
        else if (RadioCyan.IsChecked == true)
            _settingsService.Settings.FontColor = "#FF00FFFF";

        foreach (var overlay in _overlayWindows)
            overlay.ForceRender();

        _settingsService.SaveSettings();
    }

    private void BtnToggleRegion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn) return;
        if (btn.Tag is not Guid id) return;

        var region = _regions.FirstOrDefault(r => r.Id == id);
        if (region != null)
        {
            region.IsVisible = !region.IsVisible;
            RefreshOverlayForRegion(region);
        }
    }

    private void BtnCloseRegion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn) return;
        if (btn.Tag is not Guid id) return;

        var region = _regions.FirstOrDefault(r => r.Id == id);
        if (region != null)
        {
            var idx = _regions.IndexOf(region);
            if (idx >= 0 && idx < _overlayWindows.Count)
            {
                _overlayWindows[idx].Close();
                _overlayWindows.RemoveAt(idx);
            }
            _regions.Remove(region);
            UpdateRegionList();
        }
    }

    #endregion

    #region UI 更新方法

    private void UpdateRegionList()
    {
        var displayRegions = _regions.Select(r => new
        {
            r.Id,
            Words = r.Words,
            Visible = r.IsVisible
        }).ToList();

        RegionList.ItemsSource = displayRegions;
        TxtNoRegions.Visibility = _regions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateStatusBar()
    {
        TxtOcrStatus.Text = _ocrService.IsAvailable ? "OCR 就绪" : "OCR 不可用";
        OcrStatusDot.Fill = _ocrService.IsAvailable
            ? new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71))
            : new SolidColorBrush(Color.FromRgb(0xF5, 0x63, 0x2D));

        var userTerms = _settingsService.UserTerminology.Count;
        var builtInTerms = _settingsService.BuiltInTerminology.Count;
        var dictWords = _settingsService.Dictionary.Count;
        TxtTermCount.Text = $"术语: {userTerms + builtInTerms} | 词典: {dictWords}";
    }

    #endregion

    protected override void OnClosed(EventArgs e)
    {
        foreach (var overlay in _overlayWindows)
            overlay.Close();

        _mouseHook.RightButtonDown -= OnGlobalRightButtonDown;
        _mouseHook.RightButtonUp -= OnGlobalRightButtonUp;
        _mouseHook.MouseMove -= OnGlobalMouseMove;
        _mouseHook.MiddleButtonDown -= OnGlobalMiddleButtonDown;
        _mouseHook.MouseWheel -= OnGlobalMouseWheel;
        _mouseHook.Dispose();
        _regionSelection.Dispose();
        _ocrService.Dispose();

        _settingsService.SaveSettings();

        base.OnClosed(e);
    }
}
