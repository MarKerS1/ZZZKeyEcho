namespace ZZZKeyEcho.Core.Input;

public enum InputDevice
{
    Kbm,
    Xbox,
    Ps,
    Simulated,
}

public enum InputEventKind
{
    Down,
    Up,
}

public readonly record struct InputEvent(
    InputDevice Device,
    string Code,
    InputEventKind Kind,
    double TimestampMs);
