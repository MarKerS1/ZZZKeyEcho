using ZZZKeyEcho.Core.Config;

namespace ZZZKeyEcho.Core.Tests.Config;

public class ConfigMigrationTests
{
    [Fact]
    public void Legacy_config_gets_defaults_for_new_fields()
    {
        const string legacy = """
        {
          "version": 2,
          "overlay": { "width": 300 },
          "records": { "iconSize": 40 },
          "hotkeys": {
            "toggleOverlay": "F8",
            "toggleLock": "F9",
            "openSettings": "F10",
            "debugMode": "F11",
            "toggleRecording": "F12"
          }
        }
        """;

        var config = JsonStore.Deserialize(legacy);

        Assert.Equal(300, config.Overlay.Width);
        Assert.Equal(40, config.Records.IconSize);
        Assert.Equal(1.0, config.Overlay.TextScale);
        Assert.Equal("F2", config.Hotkeys.ToggleRecording);
        Assert.Equal("F5", config.Hotkeys.ClearRecords);
        Assert.True(config.Activity.FloatButtonEnabled);
        Assert.Equal(AppConfig.CurrentVersion, config.Version);
    }

    [Fact]
    public void Missing_and_null_sections_are_repaired()
    {
        var config = JsonStore.Deserialize(
            "{ \"overlay\": null, \"activity\": null, \"hotkeys\": null, \"profiles\": null, \"records\": null }");

        Assert.NotNull(config.Overlay);
        Assert.NotNull(config.Activity);
        Assert.NotNull(config.Records);
        Assert.Equal("F8", config.Hotkeys.ToggleOverlay);
        Assert.Equal("F2", config.Hotkeys.ToggleRecording);
        Assert.NotNull(config.Profiles.Kbm);
        Assert.NotNull(config.WebOverlay);
    }

    [Fact]
    public void Custom_hotkey_is_kept_during_migration()
    {
        var config = JsonStore.Deserialize("{ \"version\": 2, \"hotkeys\": { \"toggleRecording\": \"Ctrl+F2\" } }");

        Assert.Equal("Ctrl+F2", config.Hotkeys.ToggleRecording);
    }
}
