using System.Text.Json.Serialization;

namespace ZZZKeyEcho.Core.Config;

public enum IconStyle
{
    Auto,
    Xbox,
    Ps,
}

public sealed class InputConfig
{
    public List<string> GameProcess { get; set; } = new() { "ZenlessZoneZero.exe" };
    public bool ForegroundOnly { get; set; } = true;
    public IconStyle ControllerIconStyle { get; set; } = IconStyle.Auto;

    [JsonPropertyName("xinputPollHz")]
    public int XInputPollHz { get; set; } = 250;

    public int TriggerThreshold { get; set; } = 128;
    public double StickDeadzone { get; set; } = 0.35;
    public int StickMode { get; set; } = 8;
}
