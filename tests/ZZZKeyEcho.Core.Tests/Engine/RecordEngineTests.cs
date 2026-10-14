using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Engine;
using ZZZKeyEcho.Core.Input;

namespace ZZZKeyEcho.Core.Tests.Engine;

public class RecordEngineTests
{
    private static RecordEngine CreateEngine(Action<AppConfig>? tweak = null)
    {
        var config = new AppConfig();
        config.Records.RecordMovement = false;
        tweak?.Invoke(config);
        return new RecordEngine(config.Records, new MappingEngine(config.Profiles));
    }

    private static void Down(RecordEngine engine, string code, double atMs) =>
        engine.Handle(new InputEvent(InputDevice.Kbm, code, InputEventKind.Down, atMs));

    private static void Up(RecordEngine engine, string code, double atMs) =>
        engine.Handle(new InputEvent(InputDevice.Kbm, code, InputEventKind.Up, atMs));

    [Fact]
    public void Action_press_creates_single_record_with_resolved_actions()
    {
        var engine = CreateEngine();
        Down(engine, "E", 100);

        var record = Assert.Single(engine.Records);
        Assert.Equal(new[] { "special" }, record.Actions);
        Assert.Empty(record.Movement);
        Assert.Equal(100, record.PressedAtMs);
        Assert.Equal(1, record.Count);
        Assert.True(record.IsHolding);
    }

    [Fact]
    public void Disabled_action_produces_no_record()
    {
        var engine = CreateEngine(config =>
            config.Profiles.Kbm.Bindings.Single(b => b.Action == "special").Enabled = false);

        Down(engine, "E", 0);

        Assert.Empty(engine.Records);
    }

    [Fact]
    public void Holding_direction_combines_action_into_same_row()
    {
        var engine = CreateEngine();
        Down(engine, "W", 0);
        Down(engine, "MouseLeft", 100);

        var record = Assert.Single(engine.Records);
        Assert.Equal(new[] { Direction.Up }, record.Movement);
        Assert.Equal(new[] { "attack" }, record.Actions);
    }

    [Fact]
    public void Diagonal_hold_collapses_to_single_diagonal_icon()
    {
        var engine = CreateEngine();
        Down(engine, "W", 0);
        Down(engine, "A", 10);
        Down(engine, "MouseLeft", 20);

        var record = Assert.Single(engine.Records);
        Assert.Equal(new[] { Direction.UpLeft }, record.Movement);
    }

    [Fact]
    public void Combo_window_covers_direction_released_just_before_press()
    {
        var engine = CreateEngine();
        Down(engine, "W", 0);
        Up(engine, "W", 50);
        Down(engine, "MouseLeft", 100);

        var record = Assert.Single(engine.Records);
        Assert.Equal(new[] { Direction.Up }, record.Movement);
    }

    [Fact]
    public void No_combo_when_direction_change_is_outside_window()
    {
        var engine = CreateEngine();
        Down(engine, "W", 0);
        Up(engine, "W", 10);
        Down(engine, "MouseLeft", 500);

        var record = Assert.Single(engine.Records);
        Assert.Empty(record.Movement);
    }

    [Fact]
    public void Combo_mode_off_never_combines_movement()
    {
        var engine = CreateEngine(config => config.Records.ComboMode = false);
        Down(engine, "W", 0);
        Down(engine, "MouseLeft", 10);

        var record = Assert.Single(engine.Records);
        Assert.Empty(record.Movement);
    }

    [Fact]
    public void Repeat_press_within_window_merges_count()
    {
        var engine = CreateEngine();
        Down(engine, "MouseLeft", 0);
        Up(engine, "MouseLeft", 30);
        Down(engine, "MouseLeft", 200);
        Up(engine, "MouseLeft", 230);

        var record = Assert.Single(engine.Records);
        Assert.Equal(2, record.Count);
        Assert.Equal(30, record.DurationMs);
    }

    [Fact]
    public void Consecutive_taps_merge_regardless_of_gap()
    {
        var engine = CreateEngine();
        Down(engine, "MouseLeft", 0);
        Up(engine, "MouseLeft", 30);
        Down(engine, "MouseLeft", 2000);
        Up(engine, "MouseLeft", 2030);

        var record = Assert.Single(engine.Records);
        Assert.Equal(2, record.Count);
    }

    [Fact]
    public void Hold_longer_than_threshold_breaks_merge()
    {
        var engine = CreateEngine();
        Down(engine, "MouseLeft", 0);
        Up(engine, "MouseLeft", 30);
        Down(engine, "MouseLeft", 100);
        Up(engine, "MouseLeft", 600);

        Assert.Equal(2, engine.Records.Count);
    }

    [Fact]
    public void Repeat_merge_disabled_creates_new_record()
    {
        var engine = CreateEngine(config => config.Records.RepeatMerge = false);
        Down(engine, "MouseLeft", 0);
        Up(engine, "MouseLeft", 30);
        Down(engine, "MouseLeft", 100);
        Up(engine, "MouseLeft", 130);

        Assert.Equal(2, engine.Records.Count);
    }

    [Fact]
    public void Repeat_with_different_movement_does_not_merge()
    {
        var engine = CreateEngine();
        Down(engine, "A", 0);
        Down(engine, "MouseLeft", 50);
        Up(engine, "MouseLeft", 70);
        Up(engine, "A", 100);
        Down(engine, "W", 110);
        Down(engine, "MouseLeft", 130);
        Up(engine, "MouseLeft", 150);

        Assert.Equal(2, engine.Records.Count);
        Assert.Equal(new[] { Direction.Left }, engine.Records[0].Movement);
        Assert.Equal(new[] { Direction.UpLeft }, engine.Records[1].Movement);
    }

    [Fact]
    public void Hold_duration_is_finalized_on_release()
    {
        var engine = CreateEngine();
        Down(engine, "E", 0);

        var record = Assert.Single(engine.Records);
        Assert.True(record.IsHolding);
        Assert.Equal(0, record.DurationMs);

        Up(engine, "E", 1200);

        Assert.False(record.IsHolding);
        Assert.Equal(1200, record.DurationMs);
    }

    [Fact]
    public void Cap_removes_oldest_records()
    {
        var engine = CreateEngine(config => config.Records.MaxCount = 3);
        Down(engine, "C", 0);
        Down(engine, "Space", 10);
        Down(engine, "Q", 20);
        Down(engine, "R", 30);
        Down(engine, "MouseMiddle", 40);

        Assert.Equal(3, engine.Records.Count);
        Assert.Equal(20, engine.Records[0].PressedAtMs);
        Assert.Equal(40, engine.Records[^1].PressedAtMs);
    }

    [Fact]
    public void Record_movement_creates_rows_with_debounce()
    {
        var engine = CreateEngine(config => config.Records.RecordMovement = true);
        Down(engine, "W", 0);

        var first = Assert.Single(engine.Records);
        Assert.Equal(new[] { Direction.Up }, first.Movement);
        Assert.Empty(first.Actions);

        Down(engine, "A", 30);
        Assert.Single(engine.Records);
        Assert.Equal(new[] { Direction.UpLeft }, engine.Records[0].Movement);

        Down(engine, "D", 500);
        Assert.Single(engine.Records);
    }

    [Fact]
    public void Movement_release_keeps_row_as_neutral_when_enabled()
    {
        var engine = CreateEngine(config =>
        {
            config.Records.RecordMovement = true;
            config.Records.ShowNeutralRow = true;
        });
        Down(engine, "W", 0);
        Up(engine, "W", 100);

        Assert.Single(engine.Records);
        Assert.True(engine.Records[0].IsNeutral);
        Assert.True(engine.Records[0].IsHolding);
    }

    [Fact]
    public void Movement_release_does_not_create_row_by_default()
    {
        var engine = CreateEngine(config => config.Records.RecordMovement = true);
        Down(engine, "W", 0);
        Up(engine, "W", 100);

        var record = Assert.Single(engine.Records);
        Assert.Equal(new[] { Direction.Up }, record.Movement);
    }

    [Fact]
    public void Neutral_row_is_finalized_when_direction_pressed_again()
    {
        var engine = CreateEngine(config =>
        {
            config.Records.RecordMovement = true;
            config.Records.ShowNeutralRow = true;
        });
        Down(engine, "W", 0);
        Up(engine, "W", 100);
        Down(engine, "A", 300);

        Assert.Single(engine.Records);
        Assert.Equal(new[] { Direction.Left }, engine.Records[0].Movement);
    }

    [Fact]
    public void Autorepeat_key_down_events_do_not_create_or_merge_records()
    {
        var engine = CreateEngine();
        Down(engine, "E", 0);
        Down(engine, "E", 500);
        Down(engine, "E", 533);
        Down(engine, "E", 566);
        Up(engine, "E", 1200);

        var record = Assert.Single(engine.Records);
        Assert.Equal(1, record.Count);
        Assert.Equal(1200, record.DurationMs);
        Assert.False(record.IsHolding);
    }

    [Fact]
    public void Autorepeat_is_ignored_for_each_key_independently()
    {
        var engine = CreateEngine();
        Down(engine, "Q", 0);
        Down(engine, "Q", 100);
        Down(engine, "R", 120);
        Up(engine, "Q", 200);
        Up(engine, "R", 220);

        Assert.Equal(2, engine.Records.Count);
        Assert.Equal(1, engine.Records[0].Count);
        Assert.Equal(1, engine.Records[1].Count);
    }

    [Fact]
    public void Movement_only_input_without_record_movement_produces_nothing()
    {
        var engine = CreateEngine();
        Down(engine, "W", 0);
        Down(engine, "A", 10);
        Up(engine, "W", 20);

        Assert.Empty(engine.Records);
    }

    [Fact]
    public void Multiple_keys_held_create_separate_rows()
    {
        var engine = CreateEngine();
        Down(engine, "E", 0);
        Down(engine, "Q", 20);

        Assert.Equal(2, engine.Records.Count);
        Assert.Equal(new[] { "special" }, engine.Records[0].Actions);
        Assert.Equal(new[] { "ultimate" }, engine.Records[1].Actions);
    }

    [Fact]
    public void Simulated_events_follow_kbm_mapping()
    {
        var engine = CreateEngine();
        engine.Handle(new InputEvent(InputDevice.Simulated, "E", InputEventKind.Down, 0));
        engine.Handle(new InputEvent(InputDevice.Simulated, "E", InputEventKind.Up, 100));

        var record = Assert.Single(engine.Records);
        Assert.Equal(new[] { "special" }, record.Actions);
        Assert.Equal(100, record.DurationMs);
    }
}
