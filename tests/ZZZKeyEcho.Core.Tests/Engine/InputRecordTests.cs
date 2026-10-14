using ZZZKeyEcho.Core.Engine;

namespace ZZZKeyEcho.Core.Tests.Engine;

public class InputRecordTests
{
    [Fact]
    public void RefreshDisplay_tracks_live_time_while_holding()
    {
        var record = new InputRecord(new[] { Direction.Up }, new[] { "attack" }, "MouseLeft", 1000);
        record.StartHold(1000);

        record.RefreshDisplay(1500, false);
        Assert.Equal(500, record.DisplayDurationMs);
        Assert.Equal(0, record.DurationMs);

        record.RefreshDisplay(2200, false);
        Assert.Equal(1200, record.DisplayDurationMs);
    }

    [Fact]
    public void RefreshDisplay_freezes_at_final_duration_after_release()
    {
        var record = new InputRecord(new[] { Direction.Up }, new[] { "attack" }, "MouseLeft", 0);
        record.StartHold(0);
        record.RefreshDisplay(400, false);
        record.EndHold(800);

        Assert.Equal(800, record.DurationMs);
        Assert.Equal(800, record.DisplayDurationMs);

        record.RefreshDisplay(5000, false);
        Assert.Equal(800, record.DisplayDurationMs);
    }

    [Fact]
    public void RefreshDisplay_applies_age_based_opacity_when_fade_enabled()
    {
        var record = new InputRecord(Array.Empty<Direction>(), new[] { "attack" }, "MouseLeft", 0);
        record.StartHold(0);

        record.RefreshDisplay(0, true);
        Assert.Equal(1, record.DisplayOpacity);

        record.RefreshDisplay(30000, true);
        Assert.Equal(0.5, record.DisplayOpacity, 2);

        record.RefreshDisplay(120000, true);
        Assert.Equal(0.45, record.DisplayOpacity, 2);

        record.RefreshDisplay(120000, false);
        Assert.Equal(1, record.DisplayOpacity);
    }

    [Fact]
    public void Merge_resets_live_timer_and_increments_count()
    {
        var record = new InputRecord(Array.Empty<Direction>(), new[] { "attack" }, "MouseLeft", 0);
        record.StartHold(0);
        record.EndHold(30);

        record.Merge(200);

        Assert.Equal(2, record.Count);
        Assert.Equal(0, record.DisplayDurationMs);
        Assert.True(record.IsHolding);

        record.RefreshDisplay(260, false);
        Assert.Equal(60, record.DisplayDurationMs);
    }

    [Fact]
    public void SetMovement_bumps_stamp_and_notifies()
    {
        var record = new InputRecord(Array.Empty<Direction>(), Array.Empty<string>(), "W", 0);
        var changed = new List<string?>();
        record.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        var before = record.MovementStamp;
        record.SetMovement(new[] { Direction.UpLeft });

        Assert.True(record.MovementStamp > before);
        Assert.Contains(nameof(InputRecord.MovementStamp), changed);
        Assert.Equal(new[] { Direction.UpLeft }, record.Movement);
    }

    [Fact]
    public void SetMovement_updates_neutral_and_stamp_together()
    {
        var record = new InputRecord(new[] { Direction.Left }, Array.Empty<string>(), "A", 0);
        var changed = new List<string?>();
        record.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        record.SetMovement(Array.Empty<Direction>(), true);

        Assert.True(record.IsNeutral);
        Assert.Contains(nameof(InputRecord.IsNeutral), changed);
        Assert.Contains(nameof(InputRecord.MovementStamp), changed);
    }
}
