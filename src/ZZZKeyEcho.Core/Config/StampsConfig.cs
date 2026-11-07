namespace ZZZKeyEcho.Core.Config;

public sealed class StampsConfig
{
    public bool Hidden { get; set; }
    public bool FollowGame { get; set; }
    public int CaptureFps { get; set; } = 30;
    public List<StampConfig> Items { get; set; } = new();

    public StampsConfig Clone() => new()
    {
        Hidden = Hidden,
        FollowGame = FollowGame,
        CaptureFps = CaptureFps,
        Items = Items.Select(item => item.Clone()).ToList(),
    };
}
