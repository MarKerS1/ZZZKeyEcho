namespace ZZZKeyEcho.Core.Config;

public sealed class DebugConfig
{
    public bool Enabled { get; set; }
    public bool Remember { get; set; }
    public string RehearsalScript { get; set; } = "basic";
    public bool Loop { get; set; }
}
