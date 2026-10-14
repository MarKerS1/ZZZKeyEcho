using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ZZZKeyEcho.App;

[ComImport]
[Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
[ClassInterface(ClassInterfaceType.None)]
internal class TaskbarListComObject
{
}

[ComImport]
[Guid("56FDF342-FD6D-11d0-958A-006097C9A090")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITaskbarList
{
    void HrInit();

    void AddTab(IntPtr hwnd);

    void DeleteTab(IntPtr hwnd);

    void ActivateTab(IntPtr hwnd);

    void SetActiveAlt(IntPtr hwnd);
}

public static class OverlayCaptureSupport
{
    private const int GwlExStyle = -20;

    private const long WsExToolWindow = 0x00000080;

    private const uint SwpNoSize = 0x0001;

    private const uint SwpNoMove = 0x0002;

    private const uint SwpNoZOrder = 0x0004;

    private const uint SwpFrameChanged = 0x0020;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hwnd,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    public static void MakeCapturable(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;

        if (handle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();

            if ((style & WsExToolWindow) != 0)
            {
                SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style & ~WsExToolWindow));
                SetWindowPos(
                    handle,
                    IntPtr.Zero,
                    0,
                    0,
                    0,
                    0,
                    SwpNoMove | SwpNoSize | SwpNoZOrder | SwpFrameChanged);
            }
        }
        catch (Exception)
        {
            // 个别精简系统上取不到样式，忽略。
        }

        HideFromTaskbar(window);
    }

    public static void HideFromTaskbar(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;

        if (handle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var list = (ITaskbarList)new TaskbarListComObject();
            list.HrInit();
            list.DeleteTab(handle);
        }
        catch (Exception)
        {
            // 远程会话 / 精简系统里可能没有 Explorer，忽略即可。
        }
    }
}
