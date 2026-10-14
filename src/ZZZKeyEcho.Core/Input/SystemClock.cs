using System.Diagnostics;

namespace ZZZKeyEcho.Core.Input;

public sealed class SystemClock : IClock
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public double NowMs => _stopwatch.Elapsed.TotalMilliseconds;
}
