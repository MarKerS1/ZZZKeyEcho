using ZZZKeyEcho.Core.Config;

namespace ZZZKeyEcho.Core.Tests.Config;

public class StampConfigTests
{
    [Fact]
    public void Legacy_config_gets_stamp_defaults()
    {
        var config = JsonStore.Deserialize(
            """{ "version": 3, "overlay": { "width": 300 }, "hotkeys": { "toggleRecording": "F2" } }""");

        Assert.NotNull(config.Stamps);
        Assert.Empty(config.Stamps.Items);
        Assert.False(config.Stamps.FollowGame);
        Assert.Equal(30, config.Stamps.CaptureFps);
        Assert.Equal("F7", config.Hotkeys.HideStamps);
        Assert.Equal(AppConfig.CurrentVersion, config.Version);
    }

    [Fact]
    public void Null_stamp_section_is_repaired()
    {
        var config = JsonStore.Deserialize("""{ "stamps": null }""");

        Assert.NotNull(config.Stamps);
        Assert.NotNull(config.Stamps.Items);
        Assert.Equal(30, config.Stamps.CaptureFps);
    }

    [Fact]
    public void Invalid_fps_falls_back_to_30()
    {
        var config = JsonStore.Deserialize("""{ "stamps": { "captureFps": 17 } }""");

        Assert.Equal(30, config.Stamps.CaptureFps);
    }

    [Fact]
    public void Stamps_roundtrip_with_appearance_and_lists()
    {
        var config = new AppConfig();
        config.Stamps.FollowGame = false;
        config.Stamps.Hidden = true;
        config.Stamps.CaptureFps = 60;
        config.Stamps.Items.Add(new StampConfig
        {
            SourceX = 10,
            SourceY = 20,
            SourceWidth = 300,
            SourceHeight = 200,
            OutputX = 400,
            OutputY = 500,
            Scale = 1.5,
            BorderThickness = 3,
            BorderOpacity = 0.25,
            BorderColor = "#112233",
            CornerRadius = 16,
            Padding = 7,
            HideLocal = true,
            WebEnabled = true,
        });

        var restored = JsonStore.Deserialize(JsonStore.Serialize(config));

        Assert.False(restored.Stamps.FollowGame);
        Assert.True(restored.Stamps.Hidden);
        Assert.Equal(60, restored.Stamps.CaptureFps);
        var stamp = Assert.Single(restored.Stamps.Items);
        Assert.Equal(300, stamp.SourceWidth);
        Assert.Equal(1.5, stamp.Scale);
        Assert.Equal("#112233", stamp.BorderColor);
        Assert.True(stamp.WebEnabled);
    }

    [Fact]
    public void Old_stamp_without_content_opacity_gets_full_opacity()
    {
        var config = JsonStore.Deserialize(
            """{ "stamps": { "items": [ { "sourceWidth": 100, "sourceHeight": 50 } ] } }""");

        var stamp = Assert.Single(config.Stamps.Items);
        Assert.Equal(1.0, stamp.ContentOpacity);
    }

    [Fact]
    public void CopyFrom_deep_copies_stamp_items()
    {
        var source = new AppConfig();
        source.Stamps.Items.Add(new StampConfig { SourceWidth = 100, BorderColor = "#ABCDEF" });

        var target = new AppConfig();
        target.CopyFrom(source);
        target.Stamps.Items[0].BorderColor = "#000000";

        Assert.Equal("#ABCDEF", source.Stamps.Items[0].BorderColor);
    }
}
