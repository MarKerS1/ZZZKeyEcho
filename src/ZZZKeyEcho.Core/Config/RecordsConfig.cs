namespace ZZZKeyEcho.Core.Config;

public enum HoldDisplayMode
{
    Live,
    Threshold,
    Off,
}

public enum HoldTimeFormat
{
    Seconds,
    Frames,
}

public sealed class RecordsConfig
{
    public double IconSize { get; set; } = 48;
    public int MaxCount { get; set; } = 15;
    public bool ComboMode { get; set; } = true;
    public int ComboWindowMs { get; set; } = 120;
    public bool RecordMovement { get; set; } = true;
    public bool ShowNeutralRow { get; set; }
    public bool RepeatMerge { get; set; } = true;
    public int RepeatWindowMs { get; set; } = 250;
    public HoldDisplayMode HoldDisplay { get; set; } = HoldDisplayMode.Live;
    public HoldTimeFormat HoldFormat { get; set; } = HoldTimeFormat.Seconds;
    public int FramesPerSecond { get; set; } = 60;
    public int HoldThresholdMs { get; set; } = 300;
    public bool NewestAtBottom { get; set; } = true;
    public bool DurationOnLeft { get; set; }
    public bool Fade { get; set; } = true;
}
