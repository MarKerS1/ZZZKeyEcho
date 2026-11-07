namespace ZZZKeyEcho.Core.Config;

public sealed class StampConfig
{
    public int SourceX { get; set; }
    public int SourceY { get; set; }
    public int SourceWidth { get; set; }
    public int SourceHeight { get; set; }
    public int OutputX { get; set; }
    public int OutputY { get; set; }
    public double Scale { get; set; } = 1.0;
    public double BorderThickness { get; set; } = 2;
    public double BorderOpacity { get; set; } = 1.0;
    public double ContentOpacity { get; set; } = 1.0;
    public string BorderColor { get; set; } = "#FFFFFF";
    public double CornerRadius { get; set; } = 8;
    public double Padding { get; set; } = 4;
    public bool HideLocal { get; set; }
    public bool WebEnabled { get; set; }

    public StampConfig Clone() => new()
    {
        SourceX = SourceX,
        SourceY = SourceY,
        SourceWidth = SourceWidth,
        SourceHeight = SourceHeight,
        OutputX = OutputX,
        OutputY = OutputY,
        Scale = Scale,
        BorderThickness = BorderThickness,
        BorderOpacity = BorderOpacity,
        ContentOpacity = ContentOpacity,
        BorderColor = BorderColor,
        CornerRadius = CornerRadius,
        Padding = Padding,
        HideLocal = HideLocal,
        WebEnabled = WebEnabled,
    };
}
