using System.Diagnostics;
using System.IO;

namespace ZZZKeyEcho.Core.Win32;

public sealed class GameProcessMonitor : IDisposable
{
    private readonly Func<IReadOnlyList<string>> _names;
    private readonly TimeSpan _interval;
    private Timer? _timer;
    private bool _running;
    private string? _runningName;
    private bool _disposed;

    public GameProcessMonitor(Func<IReadOnlyList<string>> names, TimeSpan? interval = null)
    {
        _names = names;
        _interval = interval ?? TimeSpan.FromSeconds(2);
    }

    public event EventHandler? Changed;

    public bool IsRunning => _running;

    public string? RunningName => _runningName;

    public void Start() => (_timer ??= new Timer(_ => Poll(), null, Timeout.InfiniteTimeSpan, _interval))
        .Change(TimeSpan.Zero, _interval);

    public void Poll()
    {
        if (_disposed)
        {
            return;
        }

        var name = Detect();

        if (string.Equals(name, _runningName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _runningName = name;
        _running = name is not null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private string? Detect()
    {
        foreach (var entry in _names())
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            var processName = Path.GetFileNameWithoutExtension(entry.Trim());

            if (string.IsNullOrEmpty(processName))
            {
                continue;
            }

            try
            {
                var processes = Process.GetProcessesByName(processName);

                try
                {
                    if (processes.Length > 0)
                    {
                        return processes[0].ProcessName + ".exe";
                    }
                }
                finally
                {
                    foreach (var process in processes)
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                // 权限不足或进程已退出，忽略并继续尝试下一个进程名。
            }
        }

        return null;
    }

    public void Dispose()
    {
        _disposed = true;
        _timer?.Dispose();
        _timer = null;
    }
}
