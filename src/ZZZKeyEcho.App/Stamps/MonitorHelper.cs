using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace ZZZKeyEcho.App.Stamps;

public readonly record struct MonitorBounds(Int32Rect Bounds, Int32Rect WorkArea, double Scale)
{
    public int X => Bounds.X;
    public int Y => Bounds.Y;
    public int Width => Bounds.Width;
    public int Height => Bounds.Height;
}

public static class MonitorHelper
{
    private const uint MonitorDefaultToNearest = 2;
    private const int EffectiveDpi = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int Size;
        public RECT Monitor;
        public RECT Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT point, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    public static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

    public const uint WdaExcludeFromCapture = 0x00000011;

    public static MonitorBounds GetMonitorAtCursor()
    {
        GetCursorPos(out var point);
        return GetMonitorAt(point.X, point.Y);
    }

    public static MonitorBounds GetPrimary() => GetMonitorAt(0, 0);

    public static MonitorBounds GetMonitorAt(int x, int y)
    {
        var handle = MonitorFromPoint(new POINT { X = x, Y = y }, MonitorDefaultToNearest);
        var info = new MONITORINFO { Size = Marshal.SizeOf<MONITORINFO>() };

        if (handle == IntPtr.Zero || !GetMonitorInfo(handle, ref info))
        {
            return new MonitorBounds(
                new Int32Rect(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight),
                new Int32Rect(0, 0, (int)SystemParameters.WorkArea.Width, (int)SystemParameters.WorkArea.Height),
                1.0);
        }

        var scale = 1.0;

        try
        {
            if (GetDpiForMonitor(handle, EffectiveDpi, out var dpiX, out var dpiY) == 0 && dpiX > 0)
            {
                scale = dpiX / 96.0;
            }
        }
        catch (Exception)
        {
        }

        return new MonitorBounds(
            new Int32Rect(
                info.Monitor.Left,
                info.Monitor.Top,
                info.Monitor.Right - info.Monitor.Left,
                info.Monitor.Bottom - info.Monitor.Top),
            new Int32Rect(
                info.Work.Left,
                info.Work.Top,
                info.Work.Right - info.Work.Left,
                info.Work.Bottom - info.Work.Top),
            scale);
    }

    public static Int32Rect GetGameClientRect(IEnumerable<string> processNames)
    {
        foreach (var raw in processNames)
        {
            var name = Path.GetFileNameWithoutExtension(raw);

            try
            {
                foreach (var process in Process.GetProcessesByName(name))
                {
                    using (process)
                    {
                        var handle = process.MainWindowHandle;

                        if (handle == IntPtr.Zero)
                        {
                            continue;
                        }

                        if (!GetClientRect(handle, out var client))
                        {
                            continue;
                        }

                        var topLeft = new POINT();
                        if (!ClientToScreen(handle, ref topLeft))
                        {
                            continue;
                        }

                        return new Int32Rect(
                            topLeft.X,
                            topLeft.Y,
                            Math.Max(1, client.Right - client.Left),
                            Math.Max(1, client.Bottom - client.Top));
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        return new Int32Rect(0, 0, 0, 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT32
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hwnd, out RECT32 rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hwnd, ref POINT point);

    public static Rect ToDip(Int32Rect rect, double scale) =>
        new(rect.X / scale, rect.Y / scale, rect.Width / scale, rect.Height / scale);

    public static double DipToPhysical(double value, double scale) => value * scale;

    public static double PhysicalToDip(double value, double scale) => value / scale;
}
