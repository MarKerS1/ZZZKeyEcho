using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Win32;

namespace ZZZKeyEcho.Core.Input;

public sealed class XInputSource : IInputSource
{
    private readonly IClock _clock;
    private readonly AppConfig _config;
    private readonly SynchronizationContext? _context;
    private readonly Dictionary<uint, uint> _packets = new();
    private GamepadTranslator _translator;
    private Thread? _thread;
    private volatile bool _running;
    private bool _disposed;
    private double _deadzone;
    private double _threshold;

    public XInputSource(IClock clock, AppConfig config)
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

    public bool IsConnected { get; private set; }

    public bool IsLibraryAvailable { get; private set; } = true;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_running)
        {
            return;
        }

        _running = true;
        _thread = new Thread(PollLoop)
        {
            IsBackground = true,
            Name = "ZZZKeyEcho.XInput",
        };
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        _thread = null;
        IsConnected = false;
    }

    public void Dispose()
    {
        _disposed = true;
        Stop();
    }

    private void PollLoop()
    {
        while (_running)
        {
            var started = Environment.TickCount64;

            try
            {
                Poll();
            }
            catch (Exception)
            {
                IsConnected = false;
            }

            var interval = Math.Clamp(1000 / Math.Max(30, _config.Input.XInputPollHz), 1, 100);
            var elapsed = (int)(Environment.TickCount64 - started);
            Thread.Sleep(Math.Max(1, interval - elapsed));
        }
    }

    private void Poll()
    {
        RefreshSettings();

        var naming = _config.Input.ControllerIconStyle == IconStyle.Ps ? GamepadNaming.PlayStation : GamepadNaming.Xbox;
        var device = naming == GamepadNaming.PlayStation ? InputDevice.Ps : InputDevice.Xbox;
        var found = false;

        for (uint slot = 0; slot < 4 && !found; slot++)
        {
            if (!XInputNative.TryGetState(slot, out var state))
            {
                continue;
            }

            IsLibraryAvailable = true;

            if (_packets.TryGetValue(slot, out var lastPacket) && lastPacket == state.dwPacketNumber)
            {
                found = true;
                continue;
            }

            _packets[slot] = state.dwPacketNumber;
            found = true;
            IsConnected = true;

            var reading = XInputNative.ToReading(state);

            foreach (var (code, kind) in _translator.Translate(reading, naming))
            {
                Raise(device, code, kind);
            }
        }

        if (found || !IsConnected)
        {
            return;
        }

        foreach (var (code, kind) in _translator.ReleaseAll())
        {
            Raise(device, code, kind);
        }

        IsConnected = false;
        _packets.Clear();
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

    private void Raise(InputDevice device, string code, InputEventKind kind)
    {
        var inputEvent = new InputEvent(device, code, kind, _clock.NowMs);

        if (_context is null || _context == SynchronizationContext.Current)
        {
            Event?.Invoke(this, inputEvent);
            return;
        }

        _context.Post(_ => Event?.Invoke(this, inputEvent), null);
    }
}
