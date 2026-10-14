using ZZZKeyEcho.Core.Config;

namespace ZZZKeyEcho.Core.Tests.Config;

public class AppConfigDefaultsTests
{
    [Fact]
    public void Version_defaults_to_2()
    {
        Assert.Equal(2, AppConfig.CurrentVersion);
        Assert.Equal(2, new AppConfig().Version);
    }

    [Fact]
    public void Overlay_defaults_match_spec()
    {
        var overlay = new AppConfig().Overlay;

        Assert.Equal(0, overlay.X);
        Assert.Equal(0, overlay.Y);
        Assert.Equal(260, overlay.Width);
        Assert.Equal(720, overlay.Height);
        Assert.Equal("#B3000000", overlay.BgColor);
        Assert.Equal(12, overlay.CornerRadius);
        Assert.Equal(8, overlay.Padding);
        Assert.Equal(1.0, overlay.Opacity);
        Assert.True(overlay.Locked);
        Assert.True(overlay.FollowGame);
    }

    [Fact]
    public void Records_defaults_match_spec()
    {
        var records = new AppConfig().Records;

        Assert.Equal(48, records.IconSize);
        Assert.Equal(15, records.MaxCount);
        Assert.True(records.ComboMode);
        Assert.Equal(120, records.ComboWindowMs);
        Assert.True(records.RecordMovement);
        Assert.False(records.ShowNeutralRow);
        Assert.True(records.RepeatMerge);
        Assert.Equal(250, records.RepeatWindowMs);
        Assert.Equal(HoldDisplayMode.Live, records.HoldDisplay);
        Assert.Equal(HoldTimeFormat.Seconds, records.HoldFormat);
        Assert.Equal(300, records.HoldThresholdMs);
        Assert.True(records.NewestAtBottom);
        Assert.True(records.Fade);
    }

    [Fact]
    public void Input_defaults_match_spec()
    {
        var input = new AppConfig().Input;

        Assert.Equal(new[] { "ZenlessZoneZero.exe" }, input.GameProcess);
        Assert.True(input.ForegroundOnly);
        Assert.Equal(IconStyle.Auto, input.ControllerIconStyle);
        Assert.Equal(250, input.XInputPollHz);
        Assert.Equal(128, input.TriggerThreshold);
        Assert.Equal(0.35, input.StickDeadzone);
        Assert.Equal(8, input.StickMode);
    }

    [Fact]
    public void Hotkeys_defaults_match_spec()
    {
        var hotkeys = new AppConfig().Hotkeys;

        Assert.Equal("F8", hotkeys.ToggleOverlay);
        Assert.Equal("F9", hotkeys.ToggleLock);
        Assert.Equal("F10", hotkeys.OpenSettings);
        Assert.Equal("F11", hotkeys.DebugMode);
    }

    [Fact]
    public void Debug_defaults_are_off()
    {
        var debug = new AppConfig().Debug;

        Assert.False(debug.Enabled);
        Assert.False(debug.Remember);
        Assert.False(debug.Loop);
    }

    [Fact]
    public void IconOverrides_default_is_empty() =>
        Assert.Empty(new AppConfig().IconOverrides);

    [Fact]
    public void Kbm_profile_defaults_match_spec()
    {
        var bindings = new AppConfig().Profiles.Kbm.Bindings;

        Assert.Equal(15, bindings.Count);
        AssertBinding(bindings, "dirUp", true, "W");
        AssertBinding(bindings, "dirDown", true, "S");
        AssertBinding(bindings, "dirLeft", true, "A");
        AssertBinding(bindings, "dirRight", true, "D");
        AssertBinding(bindings, "attack", true, "MouseLeft");
        AssertBinding(bindings, "special", true, "E");
        AssertBinding(bindings, "dodge", true, "LShift", "MouseRight");
        AssertBinding(bindings, "ultimate", true, "Q");
        AssertBinding(bindings, "switchPrev", true, "C");
        AssertBinding(bindings, "switchNext", true, "Space");
        AssertBinding(bindings, "lockOn", true, "MouseMiddle");
        AssertBinding(bindings, "agentSkill", true, "R");
        AssertBinding(bindings, "chainLeft", false, "Q", "MouseLeft");
        AssertBinding(bindings, "chainRight", false, "E", "MouseRight");
        AssertBinding(bindings, "chainCancel", false, "MouseMiddle");
    }

    [Fact]
    public void Xbox_profile_defaults_match_spec()
    {
        var bindings = new AppConfig().Profiles.Xbox.Bindings;

        Assert.Equal(15, bindings.Count);
        AssertBinding(bindings, "dirUp", true, "Up");
        AssertBinding(bindings, "attack", true, "XButton");
        AssertBinding(bindings, "dodge", true, "AButton");
        AssertBinding(bindings, "special", true, "YButton");
        AssertBinding(bindings, "ultimate", true, "RT");
        AssertBinding(bindings, "switchPrev", true, "LB");
        AssertBinding(bindings, "switchNext", true, "RB");
        AssertBinding(bindings, "lockOn", true, "R3");
        AssertBinding(bindings, "agentSkill", true, "BButton");
        AssertBinding(bindings, "chainLeft", false, "LB");
        AssertBinding(bindings, "chainRight", false, "RB");
        AssertBinding(bindings, "chainCancel", false, "AButton");
    }

    [Fact]
    public void Ps_profile_defaults_match_spec()
    {
        var bindings = new AppConfig().Profiles.Ps.Bindings;

        Assert.Equal(15, bindings.Count);
        AssertBinding(bindings, "dirUp", true, "Up");
        AssertBinding(bindings, "attack", true, "Square");
        AssertBinding(bindings, "dodge", true, "Cross");
        AssertBinding(bindings, "special", true, "Triangle");
        AssertBinding(bindings, "ultimate", true, "R2");
        AssertBinding(bindings, "switchPrev", true, "L1");
        AssertBinding(bindings, "switchNext", true, "R1");
        AssertBinding(bindings, "lockOn", true, "R3");
        AssertBinding(bindings, "agentSkill", true, "Circle");
        AssertBinding(bindings, "chainLeft", false, "L2");
        AssertBinding(bindings, "chainRight", false, "R1");
        AssertBinding(bindings, "chainCancel", false, "Cross");
    }

    [Fact]
    public void Chain_bindings_are_disabled_by_default_in_all_profiles()
    {
        var profiles = new AppConfig().Profiles;

        foreach (var profile in new[] { profiles.Kbm, profiles.Xbox, profiles.Ps })
        {
            Assert.All(
                profile.Bindings.Where(b => b.Action.StartsWith("chain", StringComparison.Ordinal)),
                binding => Assert.False(binding.Enabled));
        }
    }

    private static void AssertBinding(
        List<BindingConfig> bindings, string action, bool enabled, params string[] keys)
    {
        var binding = Assert.Single(bindings.Where(b => b.Action == action));
        Assert.Equal(enabled, binding.Enabled);
        Assert.Equal(keys, binding.Keys);
    }
}
