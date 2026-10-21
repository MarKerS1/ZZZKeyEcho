namespace ZZZKeyEcho.Core.Config;

public sealed class OverlayConfig
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 260;
    public double Height { get; set; } = 720;
    public string BgColor { get; set; } = "#B3000000";
    public double CornerRadius { get; set; } = 12;
    public double Padding { get; set; } = 8;
    public double TextScale { get; set; } = 1.0;
    public double Opacity { get; set; } = 1.0;
    public double BackgroundOpacity { get; set; } = 1.0;
    public double IconOpacity { get; set; } = 1.0;
    public bool Locked { get; set; } = true;
    public bool FollowGame { get; set; } = true;
}
