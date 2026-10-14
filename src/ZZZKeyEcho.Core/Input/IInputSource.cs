namespace ZZZKeyEcho.Core.Input;

public interface IInputSource : IDisposable
{
    event EventHandler<InputEvent>? Event;

    bool IsRunning { get; }

    void Start();

    void Stop();
}
