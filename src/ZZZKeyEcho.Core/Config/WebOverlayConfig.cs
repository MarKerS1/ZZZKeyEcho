namespace ZZZKeyEcho.Core.Config;

public sealed class WebOverlayConfig
{
    public bool Enabled { get; set; }
    public bool HideLocalOverlay { get; set; }
    public int Port { get; set; } = 15965;
    public double FontScale { get; set; } = 1.0;
}
