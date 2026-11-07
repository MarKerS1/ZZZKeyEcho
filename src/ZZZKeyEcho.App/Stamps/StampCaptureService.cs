using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ZZZKeyEcho.App.Stamps;

public sealed record StampCaptureTarget(int Index, Int32Rect Source, bool WantLocal, bool WantWeb);

public sealed class StampFrameEventArgs : EventArgs
{
    public StampFrameEventArgs(int index, BitmapSource frame, byte[]? jpeg)
    {
        Index = index;
        Frame = frame;
        Jpeg = jpeg;
    }

    public int Index { get; }
    public BitmapSource Frame { get; }
    public byte[]? Jpeg { get; }
}

public sealed class StampCaptureService : IDisposable
{
    private const int SrcCopy = 0x00CC0020;
    private const uint DibRgbColors = 0;

    private readonly Func<IReadOnlyList<StampCaptureTarget>> _targets;
    private Thread? _thread;
    private volatile bool _running;
    private int _fps = 30;
    private volatile string? _lastError;

    public StampCaptureService(Func<IReadOnlyList<StampCaptureTarget>> targets)
    {
        _targets = targets;
    }

    public event EventHandler<StampFrameEventArgs>? FrameReady;

    public string? LastError => _lastError;

    public bool IsRunning => _running;

    public int Fps
    {
        get => _fps;
        set => _fps = value is 15 or 30 or 60 ? value : 30;
    }

    public void Start()
    {
        if (_running)
        {
            return;
        }

        _running = true;
        _thread = new Thread(Loop)
        {
            IsBackground = true,
            Name = "ZZZKeyEcho.StampCapture",
        };
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        _thread?.Join(500);
        _thread = null;
    }

    private void Loop()
    {
        while (_running)
        {
            var started = Stopwatch.GetTimestamp();
            IReadOnlyList<StampCaptureTarget> targets;

            try
            {
                targets = _targets();
            }
            catch (Exception)
            {
                targets = Array.Empty<StampCaptureTarget>();
            }

            foreach (var target in targets)
            {
                if (!_running)
                {
                    break;
                }

                if (!target.WantLocal && !target.WantWeb)
                {
                    continue;
                }

                try
                {
                    Capture(target);
                }
                catch (Exception exception)
                {
                    _lastError = exception.Message;
                }
            }

            var elapsed = Stopwatch.GetElapsedTime(started);
            var frameTime = TimeSpan.FromMilliseconds(1000.0 / Math.Max(1, _fps));
            var delay = frameTime - elapsed;

            if (delay > TimeSpan.Zero)
            {
                Thread.Sleep(delay);
            }
        }
    }

    private void Capture(StampCaptureTarget target)
    {
        var rect = target.Source;

        if (rect.Width < 2 || rect.Height < 2)
        {
            return;
        }

        var screenDc = GetDC(IntPtr.Zero);
        if (screenDc == IntPtr.Zero)
        {
            _lastError = "GetDC 失败";
            return;
        }

        var memoryDc = CreateCompatibleDC(screenDc);
        if (memoryDc == IntPtr.Zero)
        {
            ReleaseDC(IntPtr.Zero, screenDc);
            _lastError = "CreateCompatibleDC 失败";
            return;
        }

        var info = new BitmapInfo
        {
            Header = new BitmapInfoHeader
            {
                Size = Marshal.SizeOf<BitmapInfoHeader>(),
                Width = rect.Width,
                Height = -rect.Height,
                Planes = 1,
                BitCount = 32,
                Compression = 0,
            },
        };

        var dib = CreateDIBSection(memoryDc, ref info, DibRgbColors, out var bits, IntPtr.Zero, 0);

        if (dib == IntPtr.Zero || bits == IntPtr.Zero)
        {
            DeleteDC(memoryDc);
            ReleaseDC(IntPtr.Zero, screenDc);
            _lastError = "CreateDIBSection 失败";
            return;
        }

        var previous = SelectObject(memoryDc, dib);

        try
        {
            if (!BitBlt(memoryDc, 0, 0, rect.Width, rect.Height, screenDc, rect.X, rect.Y, SrcCopy))
            {
                _lastError = "BitBlt 失败（可能是不支持截图的独占全屏）";
                return;
            }

            var stride = rect.Width * 4;
            var buffer = new byte[stride * rect.Height];
            Marshal.Copy(bits, buffer, 0, buffer.Length);

            var frame = BitmapSource.Create(
                rect.Width,
                rect.Height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                buffer,
                stride);
            frame.Freeze();

            byte[]? jpeg = null;

            if (target.WantWeb)
            {
                jpeg = EncodeJpeg(frame);
            }

            _lastError = null;
            FrameReady?.Invoke(this, new StampFrameEventArgs(target.Index, frame, jpeg));
        }
        finally
        {
            SelectObject(memoryDc, previous);
            DeleteObject(dib);
            DeleteDC(memoryDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private static byte[] EncodeJpeg(BitmapSource frame)
    {
        var encoder = new JpegBitmapEncoder { QualityLevel = 80 };
        encoder.Frames.Add(BitmapFrame.Create(frame));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    public void Dispose() => Stop();

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public int Size;
        public int Width;
        public int Height;
        public short Planes;
        public short BitCount;
        public int Compression;
        public int SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public int ClrUsed;
        public int ClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateDIBSection(
        IntPtr hdc,
        ref BitmapInfo info,
        uint usage,
        out IntPtr bits,
        IntPtr section,
        uint offset);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(
        IntPtr dest,
        int x,
        int y,
        int width,
        int height,
        IntPtr source,
        int sourceX,
        int sourceY,
        int operation);
}
