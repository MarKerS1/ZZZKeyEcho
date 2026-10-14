namespace ZZZKeyEcho.Core.Config;

public sealed class ProfilesConfig
{
    public MappingProfile Kbm { get; set; } = MappingProfile.CreateDefaultKbm();
    public MappingProfile Xbox { get; set; } = MappingProfile.CreateDefaultXbox();
    public MappingProfile Ps { get; set; } = MappingProfile.CreateDefaultPs();
}
