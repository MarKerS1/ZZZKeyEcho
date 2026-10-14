namespace ZZZKeyEcho.Core.Config;

public sealed class BindingConfig
{
    public string Action { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public List<string> Keys { get; set; } = new();
}
