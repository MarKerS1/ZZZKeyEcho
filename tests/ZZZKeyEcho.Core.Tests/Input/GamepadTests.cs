using ZZZKeyEcho.Core.Input;

namespace ZZZKeyEcho.Core.Tests.Input;

public class StickProcessorTests
{
    private static StickProcessor Create(double deadzone = 0.35) => new(deadzone);

    [Fact]
    public void Inside_deadzone_produces_no_directions()
    {
        var stick = Create();
        Assert.Empty(stick.Update(0.2, 0.2));
        Assert.Empty(stick.Update(0, 0));
    }

    [Fact]
    public void Cardinal_directions_map_to_single_codes()
    {
        var stick = Create();
        Assert.Equal(new[] { "Right" }, stick.Update(1, 0));
        Assert.Equal(new[] { "Up" }, stick.Update(0, 1));
        Assert.Equal(new[] { "Left" }, stick.Update(-1, 0));
        Assert.Equal(new[] { "Down" }, stick.Update(0, -1));
    }

    [Fact]
    public void Diagonal_produces_two_directions()
    {
        var stick = Create();
        var directions = stick.Update(0.7071, 0.7071);

        Assert.Equal(2, directions.Count);
        Assert.Contains("Right", directions);
        Assert.Contains("Up", directions);
    }

    [Fact]
    public void Hysteresis_keeps_direction_active_near_sector_edge()
    {
        var stick = Create();
        Assert.Contains("Up", stick.Update(0, 1));

        var directions = stick.Update(0.71, 0.70);

        Assert.Contains("Up", directions);
        Assert.Contains("Right", directions);
    }

    [Fact]
    public void Direction_drops_when_moving_far_from_sector()
    {
        var stick = Create();
        stick.Update(0, 1);

        Assert.DoesNotContain("Up", stick.Update(1, 0));
    }

    [Fact]
    public void Release_uses_smaller_threshold_than_activation()
    {
        var stick = Create(0.5);
        Assert.Empty(stick.Update(0.4, 0));

        Assert.Contains("Right", stick.Update(0.8, 0));
        Assert.Contains("Right", stick.Update(0.47, 0));
        Assert.Empty(stick.Update(0.3, 0));
    }
}

public class GamepadButtonTrackerTests
{
    [Fact]
    public void Emits_down_and_up_only_on_state_changes()
    {
        var tracker = new GamepadButtonTracker();

        var first = tracker.Update(new[] { "AButton", "LB" });
        Assert.Equal(2, first.Count);
        Assert.All(first, change => Assert.Equal(InputEventKind.Down, change.Kind));

        Assert.Empty(tracker.Update(new[] { "AButton", "LB" }));

        var changes = tracker.Update(new[] { "AButton" });
        var single = Assert.Single(changes);
        Assert.Equal("LB", single.Code);
        Assert.Equal(InputEventKind.Up, single.Kind);
    }

    [Fact]
    public void ReleaseAll_releases_every_tracked_button()
    {
        var tracker = new GamepadButtonTracker();
        tracker.Update(new[] { "AButton", "RB" });

        var changes = tracker.ReleaseAll();

        Assert.Equal(2, changes.Count);
        Assert.All(changes, change => Assert.Equal(InputEventKind.Up, change.Kind));
        Assert.Empty(tracker.ReleaseAll());
    }
}

public class GamepadTranslatorTests
{
    private static GamepadReading Empty => new(
        false, false, false, false, false, false, false, false, false, false,
        false, false, false, false, 0, 0, 0, 0);

    [Fact]
    public void Xbox_naming_maps_face_buttons()
    {
        var translator = new GamepadTranslator(0.35, 0.5);
        var reading = Empty with { Cross = true, Triangle = true };

        var changes = translator.Translate(reading, GamepadNaming.Xbox);

        Assert.Contains(changes, change => change.Code == "AButton" && change.Kind == InputEventKind.Down);
        Assert.Contains(changes, change => change.Code == "YButton" && change.Kind == InputEventKind.Down);
    }

    [Fact]
    public void PlayStation_naming_maps_the_same_buttons()
    {
        var translator = new GamepadTranslator(0.35, 0.5);
        var reading = Empty with { Cross = true, Square = true };

        var changes = translator.Translate(reading, GamepadNaming.PlayStation);

        Assert.Contains(changes, change => change.Code == "Cross");
        Assert.Contains(changes, change => change.Code == "Square");
    }

    [Fact]
    public void Triggers_use_threshold_with_hysteresis()
    {
        var translator = new GamepadTranslator(0.35, 0.5);

        Assert.Empty(translator.Translate(Empty with { LeftTrigger = 0.4 }, GamepadNaming.Xbox));

        var pressed = translator.Translate(Empty with { LeftTrigger = 0.6 }, GamepadNaming.Xbox);
        Assert.Contains(pressed, change => change.Code == "LT" && change.Kind == InputEventKind.Down);

        Assert.Empty(translator.Translate(Empty with { LeftTrigger = 0.45 }, GamepadNaming.Xbox));

        var released = translator.Translate(Empty with { LeftTrigger = 0.1 }, GamepadNaming.Xbox);
        Assert.Contains(released, change => change.Code == "LT" && change.Kind == InputEventKind.Up);
    }

    [Fact]
    public void Stick_directions_flow_through_the_same_tracker()
    {
        var translator = new GamepadTranslator(0.35, 0.5);

        var pressed = translator.Translate(Empty with { LeftStickX = 1 }, GamepadNaming.Xbox);
        Assert.Contains(pressed, change => change.Code == "Right" && change.Kind == InputEventKind.Down);

        var released = translator.Translate(Empty, GamepadNaming.Xbox);
        Assert.Contains(released, change => change.Code == "Right" && change.Kind == InputEventKind.Up);
    }
}
