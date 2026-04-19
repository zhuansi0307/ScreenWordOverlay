using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace ScreenWordOverlay.Services;

/// <summary>
/// 全局鼠标钩子服务
/// </summary>
public class MouseHookService : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_MOUSEWHEEL = 0x020A;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private IntPtr _hookId = IntPtr.Zero;
    private HookProc? _hookProc;

    // 事件
    public event Action<int, int>? LeftButtonDown;
    public event Action<int, int>? LeftButtonUp;
    public event Action<int, int>? RightButtonDown;
    public event Action<int, int>? RightButtonUp;
    public event Action<int, int>? MiddleButtonDown;
    public event Action<int, int>? MiddleButtonUp;
    public event Action<int, int>? MouseMove;
    public event Action<int, int, int>? MouseWheel; // x, y, delta

    /// <summary>
    /// 是否启用钩子（禁用时不触发事件）
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    public void Start()
    {
        if (_hookId != IntPtr.Zero) return;

        _hookProc = HookCallback;
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        var moduleHandle = GetModuleHandle(module?.ModuleName ?? "");
        _hookId = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, moduleHandle, 0);
    }

    public void Stop()
    {
        if (_hookId == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && IsEnabled)
        {
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var msg = wParam.ToInt32();

            switch (msg)
            {
                case WM_LBUTTONDOWN:
                    LeftButtonDown?.Invoke(hookStruct.pt.X, hookStruct.pt.Y);
                    break;
                case WM_LBUTTONUP:
                    LeftButtonUp?.Invoke(hookStruct.pt.X, hookStruct.pt.Y);
                    break;
                case WM_RBUTTONDOWN:
                    RightButtonDown?.Invoke(hookStruct.pt.X, hookStruct.pt.Y);
                    break;
                case WM_RBUTTONUP:
                    RightButtonUp?.Invoke(hookStruct.pt.X, hookStruct.pt.Y);
                    break;
                case WM_MBUTTONDOWN:
                    MiddleButtonDown?.Invoke(hookStruct.pt.X, hookStruct.pt.Y);
                    break;
                case WM_MBUTTONUP:
                    MiddleButtonUp?.Invoke(hookStruct.pt.X, hookStruct.pt.Y);
                    break;
                case WM_MOUSEMOVE:
                    MouseMove?.Invoke(hookStruct.pt.X, hookStruct.pt.Y);
                    break;
                case WM_MOUSEWHEEL:
                    var delta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);
                    MouseWheel?.Invoke(hookStruct.pt.X, hookStruct.pt.Y, delta);
                    break;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
