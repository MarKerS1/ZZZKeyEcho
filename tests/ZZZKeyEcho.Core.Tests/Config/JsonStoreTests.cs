using ZZZKeyEcho.Core.Config;

namespace ZZZKeyEcho.Core.Tests.Config;

public class JsonStoreTests
{
    [Fact]
    public void Serialize_uses_camel_case_and_string_enums()
    {
        var json = JsonStore.Serialize(new AppConfig());

        Assert.Contains("\"version\": 2", json);
        Assert.Contains("\"iconOverrides\"", json);
        Assert.Contains("\"repeatWindowMs\"", json);
        Assert.Contains("\"xinputPollHz\"", json);
        Assert.Contains("\"holdDisplay\": \"live\"", json);
        Assert.Contains("\"holdFormat\": \"seconds\"", json);
        Assert.Contains("\"controllerIconStyle\": \"auto\"", json);
        Assert.Contains("\"kbm\"", json);
        Assert.Contains("\"xbox\"", json);
        Assert.Contains("\"ps\"", json);
        Assert.Contains("\"stickDeadzone\": 0.35", json);
    }

    [Fact]
    public void Roundtrip_preserves_modified_values()
    {
        var config = new AppConfig();
        config.Overlay.X = 100;
        config.Overlay.Y = 240;
        config.Overlay.BgColor = "#80112233";
        config.Records.MaxCount = 9;
        config.Records.HoldDisplay = HoldDisplayMode.Threshold;
        config.Records.HoldFormat = HoldTimeFormat.Frames;
        config.Input.ControllerIconStyle = IconStyle.Ps;
        config.Hotkeys.ToggleOverlay = "F12";
        config.Profiles.Kbm.Bindings[0].Keys = new List<string> { "F" };
        config.Profiles.Kbm.Bindings[0].Enabled = false;
        config.IconOverrides["lockOn"] = "assets/icons/custom.png";
        config.Debug.Enabled = true;

        var restored = JsonStore.Deserialize(JsonStore.Serialize(config));

        Assert.Equal(100, restored.Overlay.X);
        Assert.Equal(240, restored.Overlay.Y);
        Assert.Equal("#80112233", restored.Overlay.BgColor);
        Assert.Equal(9, restored.Records.MaxCount);
        Assert.Equal(HoldDisplayMode.Threshold, restored.Records.HoldDisplay);
        Assert.Equal(HoldTimeFormat.Frames, restored.Records.HoldFormat);
        Assert.Equal(IconStyle.Ps, restored.Input.ControllerIconStyle);
        Assert.Equal("F12", restored.Hotkeys.ToggleOverlay);
        Assert.Equal(new List<string> { "F" }, restored.Profiles.Kbm.Bindings[0].Keys);
        Assert.False(restored.Profiles.Kbm.Bindings[0].Enabled);
        Assert.Equal("assets/icons/custom.png", restored.IconOverrides["lockOn"]);
        Assert.True(restored.Debug.Enabled);
    }

    [Fact]
    public void Load_missing_file_returns_defaults()
    {
        var path = Path.Combine(Path.GetTempPath(), $"zzz-missing-{Guid.NewGuid():N}", "config.json");

        var config = JsonStore.Load(path);

        Assert.Equal(48, config.Records.IconSize);
        Assert.Equal("F8", config.Hotkeys.ToggleOverlay);
    }

    [Fact]
    public void Load_corrupt_file_returns_defaults()
    {
        var directory = CreateTempDir();
        try
        {
            var path = Path.Combine(directory, "config.json");
            File.WriteAllText(path, "{ this is not json");

            var config = JsonStore.Load(path);

            Assert.Equal(48, config.Records.IconSize);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Save_then_load_roundtrips_on_disk()
    {
        var directory = CreateTempDir();
        try
        {
            var path = Path.Combine(directory, "config.json");
            var config = new AppConfig();
            config.Overlay.Width = 321;

            JsonStore.Save(path, config);
            var loaded = JsonStore.Load(path);

            Assert.True(File.Exists(path));
            Assert.Equal(321, loaded.Overlay.Width);
            Assert.Equal(config.Profiles.Ps.Bindings.Count, loaded.Profiles.Ps.Bindings.Count);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Deserialize_partial_json_keeps_defaults_for_missing_members()
    {
        var config = JsonStore.Deserialize("""{ "records": { "maxCount": 7 } }""");

        Assert.Equal(7, config.Records.MaxCount);
        Assert.Equal(48, config.Records.IconSize);
        Assert.Equal("F8", config.Hotkeys.ToggleOverlay);
        Assert.Equal(2, config.Version);
    }

    [Fact]
    public void Serialized_json_contains_all_top_level_sections()
    {
        var json = JsonStore.Serialize(new AppConfig());

        Assert.Contains("\"overlay\"", json);
        Assert.Contains("\"records\"", json);
        Assert.Contains("\"input\"", json);
        Assert.Contains("\"hotkeys\"", json);
        Assert.Contains("\"profiles\"", json);
        Assert.Contains("\"debug\"", json);
        Assert.Contains("\"gameProcess\"", json);
        Assert.Contains("\"bindings\"", json);
    }

    private static string CreateTempDir()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ZZZKeyEcho.Tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
