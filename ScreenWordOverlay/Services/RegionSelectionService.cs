using System.Windows;
using System.Windows.Media;
using ScreenWordOverlay.Models;
using ScreenWordOverlay.Windows;

namespace ScreenWordOverlay.Services;

/// <summary>
/// 区域框选服务：支持右键拖拽框选模式
/// </summary>
public class RegionSelectionService : IDisposable
{
    private readonly MouseHookService _mouseHook;
    private SelectionWindow? _selectionWindow;
    private bool _isSelecting;
    private int _startX;
    private int _startY;

    /// <summary>
    /// 框选完成事件，参数为屏幕矩形区域
    /// </summary>
    public event Action<ScreenRect>? SelectionCompleted;

    /// <summary>
    /// 是否处于框选模式
    /// </summary>
    public bool IsSelectionMode { get; private set; }

    public RegionSelectionService(MouseHookService mouseHook)
    {
        _mouseHook = mouseHook;
    }

    /// <summary>
    /// 开始右键拖拽框选（由 MainWindow 在检测到右键拖拽阈值后调用）
    /// </summary>
    public void StartDragSelection(int startX, int startY)
    {
        if (IsSelectionMode) return;

        IsSelectionMode = true;
        _isSelecting = true;
        _startX = startX;
        _startY = startY;

        // 创建全屏透明选择窗口
        _selectionWindow = new SelectionWindow();
        _selectionWindow.Show();

        // 注册鼠标移动和右键松开事件
        _mouseHook.MouseMove += OnMouseMove;
        _mouseHook.RightButtonUp += OnRightButtonUp;
    }

    /// <summary>
    /// 完成右键拖拽框选（由 MainWindow 在右键松开时调用）
    /// </summary>
    public void FinishDragSelection(int endX, int endY)
    {
        if (!IsSelectionMode || !_isSelecting) return;

        _isSelecting = false;

        var left = Math.Min(_startX, endX);
        var top = Math.Min(_startY, endY);
        var width = Math.Abs(endX - _startX);
        var height = Math.Abs(endY - _startY);

        // 先退出框选模式
        ExitSelectionMode();

        // 选区太小则忽略
        if (width < 10 || height < 10) return;

        var selectedRect = new ScreenRect(left, top, width, height);

        // 触发完成事件
        SelectionCompleted?.Invoke(selectedRect);
    }

    /// <summary>
    /// 取消框选
    /// </summary>
    public void CancelSelection()
    {
        if (!IsSelectionMode) return;
        _isSelecting = false;
        ExitSelectionMode();
    }

    /// <summary>
    /// 退出框选模式
    /// </summary>
    private void ExitSelectionMode()
    {
        if (!IsSelectionMode) return;

        IsSelectionMode = false;
        _isSelecting = false;

        // 注销鼠标事件
        _mouseHook.MouseMove -= OnMouseMove;
        _mouseHook.RightButtonUp -= OnRightButtonUp;

        // 关闭选择窗口
        _selectionWindow?.Close();
        _selectionWindow = null;
    }

    private void OnMouseMove(int x, int y)
    {
        if (!_isSelecting || _selectionWindow == null) return;

        // 更新选择窗口中的选区矩形
        var left = Math.Min(_startX, x);
        var top = Math.Min(_startY, y);
        var width = Math.Abs(x - _startX);
        var height = Math.Abs(y - _startY);

        _selectionWindow.UpdateSelection(left, top, width, height);
    }

    private void OnRightButtonUp(int x, int y)
    {
        // 右键松开时完成框选
        if (!_isSelecting) return;

        _isSelecting = false;

        var left = Math.Min(_startX, x);
        var top = Math.Min(_startY, y);
        var width = Math.Abs(x - _startX);
        var height = Math.Abs(y - _startY);

        ExitSelectionMode();

        if (width < 10 || height < 10) return;

        var selectedRect = new ScreenRect(left, top, width, height);
        SelectionCompleted?.Invoke(selectedRect);
    }

    public void Dispose()
    {
        ExitSelectionMode();
        GC.SuppressFinalize(this);
    }
}
