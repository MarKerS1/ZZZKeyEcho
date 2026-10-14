namespace ZZZKeyEcho.Core.Input;

public sealed class SimulatedInputSource : IInputSource
{
    private readonly IClock _clock;
    private bool _disposed;

    public SimulatedInputSource(IClock clock)
    {
        _clock = clock;
    }

    public event EventHandler<InputEvent>? Event;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        IsRunning = true;
    }

    public void Stop() => IsRunning = false;

    public void Dispose()
    {
        _disposed = true;
        Stop();
    }

    public void Down(string code, InputDevice device = InputDevice.Simulated) => Raise(code, InputEventKind.Down, device);

    public void Up(string code, InputDevice device = InputDevice.Simulated) => Raise(code, InputEventKind.Up, device);

    private void Raise(string code, InputEventKind kind, InputDevice device)
    {
        if (!IsRunning)
        {
            return;
        }

        Event?.Invoke(this, new InputEvent(device, code, kind, _clock.NowMs));
    }
}
