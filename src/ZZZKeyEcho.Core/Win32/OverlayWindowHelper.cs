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
    }

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
