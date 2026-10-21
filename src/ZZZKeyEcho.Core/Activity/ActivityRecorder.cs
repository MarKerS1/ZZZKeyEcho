using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using ZZZKeyEcho.Core.Engine;
using ZZZKeyEcho.Core.Input;

namespace ZZZKeyEcho.Core.Activity;

public sealed class ActivityRecorder
{
    private readonly RecordEngine _engine;
    private readonly IClock _clock;
    private readonly Dictionary<string, double> _open = new(StringComparer.Ordinal);
    private StreamWriter? _writer;
    private double _startMs;
    private int _rowCount;

    public ActivityRecorder(RecordEngine engine, IClock clock, string directory)
    {
        _engine = engine;
        _clock = clock;
        Directory = directory;
        _engine.ActionJournal += OnActionEvent;
    }

    public string Directory { get; }

    public bool IsRecording => _writer is not null;

    public string? CurrentFile { get; private set; }

    public int RowCount => _rowCount;

    public double ElapsedSec => IsRecording ? Math.Max(0, (_clock.NowMs - _startMs) / 1000.0) : 0;

    public event EventHandler? Changed;

    public void Toggle()
    {
        if (IsRecording)
        {
            Stop();
        }
        else
        {
            Start();
        }
    }

    public void Start(string prefix = "activity")
    {
        if (IsRecording)
        {
            return;
        }

        System.IO.Directory.CreateDirectory(Directory);

        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var path = Path.Combine(Directory, $"{prefix}_{stamp}.csv");
        var index = 2;

        while (File.Exists(path))
        {
            path = Path.Combine(Directory, $"{prefix}_{stamp}_{index++}.csv");
        }

        _writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true,
        };

        _writer.WriteLine(ActivityCsv.Header);
        _startMs = _clock.NowMs;
        _rowCount = 0;
        _open.Clear();
        CurrentFile = path;
        RaiseChanged();
    }

    public void Stop()
    {
        if (_writer is null)
        {
            return;
        }

        var now = _clock.NowMs;
        foreach (var pair in _open.OrderBy(p => p.Value))
        {
            WriteRow(pair.Value, pair.Key.Split('|')[0], now);
        }

        _open.Clear();
        _writer.Dispose();
        _writer = null;
        RaiseChanged();
    }

    private void OnActionEvent(object? sender, ActivityEvent e)
    {
        if (_writer is null)
        {
            return;
        }

        var key = $"{e.ActionId}|{e.Code}";

        if (e.Kind == ActivityKind.Down)
        {
            _open[key] = e.TimestampMs;
            return;
        }

        if (_open.Remove(key, out var pressed))
        {
            WriteRow(pressed, e.ActionId, e.TimestampMs);
            RaiseChanged();
        }
    }

    private void WriteRow(double pressedMs, string actionId, double releasedMs)
    {
        _writer?.WriteLine(string.Format(
            CultureInfo.InvariantCulture,
            "{0:0.000},{1},{2:0.000}",
            (pressedMs - _startMs) / 1000.0,
            actionId,
            (releasedMs - _startMs) / 1000.0));

        _rowCount++;
    }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
