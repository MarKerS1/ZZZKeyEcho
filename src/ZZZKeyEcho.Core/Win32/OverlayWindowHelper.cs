using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ZZZKeyEcho.Core.Win32;

public static class OverlayWindowHelper
{
    public static void Attach(Window window, bool locked)
    {
        if (PresentationSource.FromVisual(window) is HwndSource source)
        {
            source.AddHook(WndProc);
        }

        SetLocked(window, locked);
        SetTopmost(window);
    }

    public static void SetTopmost(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;

        if (handle != IntPtr.Zero)
        {
            SetWindowPos(
                handle,
                new IntPtr(-1),
                0,
                0,
                0,
                0,
                0x0001 | 0x0002 | 0x0010);
        }
    }

    public static void SetLocked(Window window, bool locked)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var exStyle = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE).ToInt64();
        exStyle |= NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW;

        if (locked)
        {
            exStyle |= NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED;
        }
        else
        {
            exStyle &= ~(long)NativeMethods.WS_EX_TRANSPARENT;
        }

        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle));
        SetTopmost(window);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_MOUSEACTIVATE)
        {
            handled = true;
            return new IntPtr(NativeMethods.MA_NOACTIVATE);
        }

        return IntPtr.Zero;
    }
}
