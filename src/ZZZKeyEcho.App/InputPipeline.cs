using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Engine;
using ZZZKeyEcho.Core.Input;
using ZZZKeyEcho.Core.Win32;

namespace ZZZKeyEcho.App;

public readonly record struct RuntimeStatus(
    bool HookRunning,
    bool GameForeground,
    bool DebugMode,
    bool RecordingEnabled,
    string GamepadName,
    string ForegroundProcess,
    bool GameRunning,
    string GameProcess);

public sealed class InputPipeline : IDisposable
{
    private readonly AppConfig _config;
    private readonly ForegroundMonitor _foreground = new();
    private readonly KeyboardMouseHookSource _hook;
    private readonly SimulatedInputSource _simulated;
    private readonly XInputSource _xinput;
    private readonly DualSenseHidSource _hid;
    private readonly RecordEngine _engine;
    private MappingEngine _mapping;

    public InputPipeline(AppConfig config, IClock clock)
    {
        _config = config;
        _hook = new KeyboardMouseHookSource(clock);
        _simulated = new SimulatedInputSource(clock);
        _xinput = new XInputSource(clock, config);
        _hid = new DualSenseHidSource(clock, config);
        _mapping = new MappingEngine(config.Profiles);
        _engine = new RecordEngine(config.Records, _mapping);
        GameMonitor = new GameProcessMonitor(() => config.Input.GameProcess);

        _hook.Event += OnInput;
        _simulated.Event += OnInput;
        _xinput.Event += OnInput;
        _hid.Event += OnInput;
    }

    public event EventHandler<InputEvent>? RawEvent;

    public RecordEngine Engine => _engine;

    public GameProcessMonitor GameMonitor { get; }

    public SimulatedInputSource Simulated => _simulated;

    public bool IsHookRunning => _hook.IsRunning;

    public bool DebugMode { get; set; }

    public bool Enabled { get; set; } = true;

    public void Start()
    {
        _hook.Start();
        _simulated.Start();
        _xinput.Start();
        _hid.Start();
        GameMonitor.Start();
    }

    public void RebuildMapping()
    {
        _mapping = new MappingEngine(_config.Profiles);
        _engine.UpdateMapping(_mapping);
    }

    public RuntimeStatus GetStatus()
    {
        var matched = _foreground.IsForeground(_config.Input.GameProcess);
        var foreground = _config.Input.ForegroundOnly && matched;

        var gamepad = "键鼠";

        if (_hid.IsConnected)
        {
            gamepad = $"键鼠 / {_hid.DeviceName}（HID 直读）";
        }
        else if (_xinput.IsConnected)
        {
            gamepad = _config.Input.ControllerIconStyle == IconStyle.Ps
                ? "键鼠 / 手柄（XInput 通道，图标风格：PS）"
                : "键鼠 / 手柄（XInput 通道）";
        }
        else if (!_xinput.IsLibraryAvailable)
        {
            gamepad = "键鼠（XInput 不可用）";
        }

        return new RuntimeStatus(
            _hook.IsRunning,
            foreground,
            DebugMode,
            Enabled,
            gamepad,
            _foreground.ForegroundProcessName ?? "（读不到，可能权限不足）",
            GameMonitor.IsRunning,
            GameMonitor.RunningName ?? "未检测到");
    }

    public void Dispose()
    {
        _hook.Event -= OnInput;
        _simulated.Event -= OnInput;
        _xinput.Event -= OnInput;
        _hid.Event -= OnInput;
        _hook.Dispose();
        _simulated.Dispose();
        _xinput.Dispose();
        _hid.Dispose();
        GameMonitor.Dispose();
    }

    private void OnInput(object? sender, InputEvent inputEvent)
    {
        RawEvent?.Invoke(this, inputEvent);

        if (ShouldRecord(inputEvent, ReferenceEquals(sender, _simulated)))
        {
            _engine.Handle(inputEvent);
        }
    }

    private bool ShouldRecord(InputEvent inputEvent, bool isSimulated)
    {
        if (!Enabled)
        {
            return false;
        }

        if (isSimulated || inputEvent.Device == InputDevice.Simulated || DebugMode || !_config.Input.ForegroundOnly)
        {
            return true;
        }

        return _foreground.IsForeground(_config.Input.GameProcess);
    }
}
