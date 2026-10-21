using System.IO;
using ZZZKeyEcho.Core.Activity;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Engine;
using ZZZKeyEcho.Core.Input;

namespace ZZZKeyEcho.Core.Tests.Activity;

public class ActivityFeatureTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "zzzkeyecho-activity-" + Guid.NewGuid().ToString("N"));

    private sealed class FakeClock : IClock
    {
        public double NowMs { get; set; }
    }

    [Fact]
    public void Recorder_writes_header_and_relative_times()
    {
        var clock = new FakeClock { NowMs = 5000 };
        var engine = CreateEngine();
        var recorder = new ActivityRecorder(engine, clock, _directory);

        recorder.Start();
        engine.Handle(new InputEvent(InputDevice.Kbm, "MouseLeft", InputEventKind.Down, 5200));
        clock.NowMs = 5300;
        engine.Handle(new InputEvent(InputDevice.Kbm, "MouseLeft", InputEventKind.Up, 5400));
        recorder.Stop();

        var lines = File.ReadAllLines(recorder.CurrentFile!);
        Assert.Equal("pressed_at,action_id,released_at", lines[0]);
        Assert.Equal("0.200,attack,0.400", lines[1]);
        Assert.Equal(1, recorder.RowCount);
    }

    [Fact]
    public void Open_press_is_closed_at_stop()
    {
        var clock = new FakeClock { NowMs = 1000 };
        var engine = CreateEngine();
        var recorder = new ActivityRecorder(engine, clock, _directory);

        recorder.Start();
        engine.Handle(new InputEvent(InputDevice.Kbm, "E", InputEventKind.Down, 1100));
        clock.NowMs = 1600;
        recorder.Stop();

        var row = Assert.Single(ActivityCsv.Read(recorder.CurrentFile!));
        Assert.Equal("special", row.ActionId);
        Assert.Equal(0.1, row.PressedAt, 3);
        Assert.Equal(0.6, row.ReleasedAt, 3);
    }

    [Fact]
    public void Csv_reader_skips_bad_lines_and_sorts()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "sample.csv");

        File.WriteAllLines(path, new[]
        {
            "pressed_at,action_id,released_at",
            "1.000,dodge,1.100",
            "bad,line",
            "0.200,attack,0.300",
            string.Empty,
        });

        var rows = ActivityCsv.Read(path);

        Assert.Equal(2, rows.Count);
        Assert.Equal("attack", rows[0].ActionId);
        Assert.Equal("dodge", rows[1].ActionId);
    }

    private static RecordEngine CreateEngine()
    {
        var config = new AppConfig();
        config.Records.RecordMovement = false;
        return new RecordEngine(config.Records, new MappingEngine(config.Profiles));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }
        catch (Exception)
        {
        }
    }
}
