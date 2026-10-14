using ZZZKeyEcho.Core.Engine;

namespace ZZZKeyEcho.Core.Config;

public sealed class MappingProfile
{
    public List<BindingConfig> Bindings { get; set; } = new();

    public static MappingProfile CreateDefaultKbm() => new()
    {
        Bindings =
        {
            Make(ActionCatalog.DirUp, "W"),
            Make(ActionCatalog.DirDown, "S"),
            Make(ActionCatalog.DirLeft, "A"),
            Make(ActionCatalog.DirRight, "D"),
            Make("attack", "MouseLeft"),
            Make("special", "E"),
            Make("dodge", "LShift", "MouseRight"),
            Make("ultimate", "Q"),
            Make("switchPrev", "C"),
            Make("switchNext", "Space"),
            Make("lockOn", "MouseMiddle"),
            Make("agentSkill", "R"),
            Make("chainLeft", false, "Q", "MouseLeft"),
            Make("chainRight", false, "E", "MouseRight"),
            Make("chainCancel", false, "MouseMiddle"),
        },
    };

    public static MappingProfile CreateDefaultXbox() => new()
    {
        Bindings =
        {
            Make(ActionCatalog.DirUp, "Up"),
            Make(ActionCatalog.DirDown, "Down"),
            Make(ActionCatalog.DirLeft, "Left"),
            Make(ActionCatalog.DirRight, "Right"),
            Make("attack", "XButton"),
            Make("dodge", "AButton"),
            Make("special", "YButton"),
            Make("ultimate", "RT"),
            Make("switchPrev", "LB"),
            Make("switchNext", "RB"),
            Make("lockOn", "R3"),
            Make("agentSkill", "BButton"),
            Make("chainLeft", false, "LB"),
            Make("chainRight", false, "RB"),
            Make("chainCancel", false, "AButton"),
        },
    };

    public static MappingProfile CreateDefaultPs() => new()
    {
        Bindings =
        {
            Make(ActionCatalog.DirUp, "Up"),
            Make(ActionCatalog.DirDown, "Down"),
            Make(ActionCatalog.DirLeft, "Left"),
            Make(ActionCatalog.DirRight, "Right"),
            Make("attack", "Square"),
            Make("dodge", "Cross"),
            Make("special", "Triangle"),
            Make("ultimate", "R2"),
            Make("switchPrev", "L1"),
            Make("switchNext", "R1"),
            Make("lockOn", "R3"),
            Make("agentSkill", "Circle"),
            Make("chainLeft", false, "L2"),
            Make("chainRight", false, "R1"),
            Make("chainCancel", false, "Cross"),
        },
    };

    private static BindingConfig Make(string action, params string[] keys) => new()
    {
        Action = action,
        Enabled = true,
        Keys = new List<string>(keys),
    };

    private static BindingConfig Make(string action, bool enabled, params string[] keys) => new()
    {
        Action = action,
        Enabled = enabled,
        Keys = new List<string>(keys),
    };
}
