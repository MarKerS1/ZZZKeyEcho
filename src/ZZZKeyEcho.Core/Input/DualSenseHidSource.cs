using HidSharp;
using ZZZKeyEcho.Core.Config;

namespace ZZZKeyEcho.Core.Input;

public sealed class DualSenseHidSource : IInputSource
{
    private static readonly (int Vendor, int Product, string Name, bool IsDualSense)[] KnownDevices =
    {
        (0x054C, 0x0CE6, "DualSense", true),
        (0x054C, 0x0DF2, "DualSense Edge", true),
        (0x054C, 0x05C4, "DualShock 4", false),
        (0x054C, 0x09CC, "DualShock 4", false),
    };

    private readonly IClock _clock;
    private readonly AppConfig _config;
    private readonly SynchronizationContext? _context;
    private GamepadTranslator _translator;
    private Thread? _thread;
    private volatile bool _running;
    private bool _disposed;
    private double _deadzone;
    private double _threshold;

    public DualSenseHidSource(IClock clock, AppConfig config)
    {
        _clock = clock;
        _config = config;
        _context = SynchronizationContext.Current;
        _deadzone = config.Input.StickDeadzone;
        _threshold = config.Input.TriggerThreshold / 255.0;
        _translator = new GamepadTranslator(_deadzone, _threshold);
    }

    public event EventHandler<InputEvent>? Event;

    public bool IsRunning => _running;

    public bool IsConnected => DeviceName is not null;

    public string? DeviceName { get; private set; }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_running)
        {
            return;
        }

        _running = true;
        _thread = new Thread(ReadLoop)
        {
            IsBackground = true,
            Name = "ZZZKeyEcho.DualSense",
        };
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        _thread = null;
        DeviceName = null;
    }

    public void Dispose()
    {
        _disposed = true;
        Stop();
    }

    private void ReadLoop()
    {
        while (_running)
        {
            if (!TryOpen(out var stream, out var isDualSense, out var name))
            {
                Thread.Sleep(2000);
                continue;
            }

            RefreshSettings();

            using (stream)
            {
                DeviceName = name;
                var buffer = new byte[Math.Max(64, stream.Device.GetMaxInputReportLength())];

                while (_running)
                {
                    int count;

                    try
                    {
                        count = stream.Read(buffer, 0, buffer.Length);
                    }
                    catch (Exception)
                    {
                        break;
                    }

                    if (count <= 0)
                    {
                        continue;
                    }

                    if (!TryParse(buffer, count, isDualSense, out var reading))
                    {
                        continue;
                    }

                    foreach (var (code, kind) in _translator.Translate(reading, GamepadNaming.PlayStation))
                    {
                        Raise(code, kind);
                    }
                }

                foreach (var (code, kind) in _translator.ReleaseAll())
                {
                    Raise(code, kind);
                }
            }

            DeviceName = null;
            Thread.Sleep(1000);
        }
    }

    private bool TryOpen(out HidStream stream, out bool isDualSense, out string name)
    {
        stream = null!;
        isDualSense = false;
        name = string.Empty;

        try
        {
            foreach (var (vendor, product, deviceName, dualSense) in KnownDevices)
            {
                foreach (var device in DeviceList.Local.GetHidDevices(vendor, product))
                {
                    if (!device.TryOpen(out var candidate))
                    {
                        continue;
                    }

                    candidate.ReadTimeout = 1000;
                    stream = candidate;
                    isDualSense = dualSense;
                    name = deviceName;
                    return true;
                }
            }
        }
        catch (Exception)
        {
            return false;
        }

        return false;
    }

    private static bool TryParse(byte[] buffer, int count, bool isDualSense, out GamepadReading reading)
    {
        var report = new byte[count];
        Array.Copy(buffer, report, count);

        return isDualSense
            ? GamepadReportParser.TryParseDualSense(report, out reading)
            : GamepadReportParser.TryParseDs4(report, out reading);
    }

    private void RefreshSettings()
    {
        var deadzone = _config.Input.StickDeadzone;
        var threshold = _config.Input.TriggerThreshold / 255.0;

        if (Math.Abs(deadzone - _deadzone) < 0.001 && Math.Abs(threshold - _threshold) < 0.001)
        {
            return;
        }

        _deadzone = deadzone;
        _threshold = threshold;
        _translator = new GamepadTranslator(deadzone, threshold);
    }

    private void Raise(string code, InputEventKind kind)
    {
        var inputEvent = new InputEvent(InputDevice.Ps, code, kind, _clock.NowMs);

        if (_context is null || _context == SynchronizationContext.Current)
        {
            Event?.Invoke(this, inputEvent);
            return;
        }

        _context.Post(_ => Event?.Invoke(this, inputEvent), null);
    }
}
