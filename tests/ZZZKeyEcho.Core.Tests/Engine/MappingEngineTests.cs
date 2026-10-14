using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Engine;
using ZZZKeyEcho.Core.Input;

namespace ZZZKeyEcho.Core.Tests.Engine;

public class MappingEngineTests
{
    private static MappingEngine CreateEngine(Action<AppConfig>? tweak = null)
    {
        var config = new AppConfig();
        tweak?.Invoke(config);
        return new MappingEngine(config.Profiles);
    }

    [Fact]
    public void Resolve_kbm_E_defaults_to_special_only()
    {
        var engine = CreateEngine();
        Assert.Equal(new[] { "special" }, engine.Resolve(InputDevice.Kbm, "E"));
    }

    [Fact]
    public void Resolve_kbm_mouse_left_defaults_to_attack_only()
    {
        var engine = CreateEngine();
        Assert.Equal(new[] { "attack" }, engine.Resolve(InputDevice.Kbm, "MouseLeft"));
    }

    [Fact]
    public void Resolve_lists_all_enabled_candidates_in_profile_order()
    {
        var engine = CreateEngine(config =>
        {
            config.Profiles.Kbm.Bindings.Single(b => b.Action == "chainRight").Enabled = true;
            config.Profiles.Kbm.Bindings.Single(b => b.Action == "chainLeft").Enabled = true;
        });

        Assert.Equal(new[] { "special", "chainRight" }, engine.Resolve(InputDevice.Kbm, "E"));
        Assert.Equal(new[] { "attack", "chainLeft" }, engine.Resolve(InputDevice.Kbm, "MouseLeft"));
    }

    [Fact]
    public void Resolve_ignores_disabled_bindings()
    {
        var engine = CreateEngine(config =>
            config.Profiles.Kbm.Bindings.Single(b => b.Action == "special").Enabled = false);

        Assert.Empty(engine.Resolve(InputDevice.Kbm, "E"));
    }

    [Fact]
    public void Resolve_xbox_uses_xbox_profile()
    {
        var engine = CreateEngine();
        Assert.Equal(new[] { "switchPrev" }, engine.Resolve(InputDevice.Xbox, "LB"));
        Assert.Equal(new[] { "special" }, engine.Resolve(InputDevice.Xbox, "YButton"));
    }

    [Fact]
    public void Resolve_ps_uses_ps_profile()
    {
        var engine = CreateEngine();
        Assert.Equal(new[] { "dodge" }, engine.Resolve(InputDevice.Ps, "Cross"));
        Assert.Equal(new[] { "switchPrev" }, engine.Resolve(InputDevice.Ps, "L1"));
        Assert.Empty(engine.Resolve(InputDevice.Ps, "L2"));
    }

    [Fact]
    public void Resolve_simulated_uses_kbm_profile()
    {
        var engine = CreateEngine();
        Assert.Equal(new[] { "special" }, engine.Resolve(InputDevice.Simulated, "E"));
    }

    [Fact]
    public void Resolve_unknown_key_returns_empty()
    {
        var engine = CreateEngine();
        Assert.Empty(engine.Resolve(InputDevice.Kbm, "F12"));
    }

    [Fact]
    public void Resolve_deduplicates_duplicate_key_entries_of_same_action()
    {
        var engine = CreateEngine(config =>
            config.Profiles.Kbm.Bindings.Single(b => b.Action == "special").Keys.Add("E"));

        Assert.Equal(new[] { "special" }, engine.Resolve(InputDevice.Kbm, "E"));
    }

    [Fact]
    public void Resolve_shares_key_between_actions_per_profile()
    {
        var engine = CreateEngine(config =>
            config.Profiles.Kbm.Bindings.Single(b => b.Action == "dodge").Keys.Add("E"));

        Assert.Equal(new[] { "special", "dodge" }, engine.Resolve(InputDevice.Kbm, "E"));
    }
}
