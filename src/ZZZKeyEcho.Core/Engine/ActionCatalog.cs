namespace ZZZKeyEcho.Core.Engine;

public sealed record ActionInfo(string Id, string DisplayName, bool IsDirection, bool IconOnly = false);

public static class ActionCatalog
{
    public const string Attack = "attack";
    public const string Special = "special";
    public const string Dodge = "dodge";
    public const string Ultimate = "ultimate";
    public const string Assist = "assist";
    public const string SwitchPrev = "switchPrev";
    public const string SwitchNext = "switchNext";
    public const string ChainLeft = "chainLeft";
    public const string ChainRight = "chainRight";
    public const string ChainCancel = "chainCancel";
    public const string LockOn = "lockOn";
    public const string AgentSkill = "agentSkill";

    public const string DirUp = "dirUp";
    public const string DirDown = "dirDown";
    public const string DirLeft = "dirLeft";
    public const string DirRight = "dirRight";

    public static readonly IReadOnlyList<ActionInfo> All = new[]
    {
        new ActionInfo(Attack, "普通攻击", false),
        new ActionInfo(Special, "特殊技", false),
        new ActionInfo(Dodge, "闪避", false),
        new ActionInfo(Ultimate, "终结技", false),
        new ActionInfo(SwitchPrev, "角色切换-上一位", false),
        new ActionInfo(SwitchNext, "角色切换-下一位", false),
        new ActionInfo(ChainLeft, "连携技-左", false),
        new ActionInfo(ChainRight, "连携技-右", false),
        new ActionInfo(ChainCancel, "连携技-取消", false),
        new ActionInfo(LockOn, "手动锁定", false),
        new ActionInfo(AgentSkill, "角色特殊能力", false),
        new ActionInfo(DirUp, "上", true),
        new ActionInfo(DirDown, "下", true),
        new ActionInfo(DirLeft, "左", true),
        new ActionInfo(DirRight, "右", true),
        new ActionInfo("dirUpLeft", "左上（斜向）", true, true),
        new ActionInfo("dirUpRight", "右上（斜向）", true, true),
        new ActionInfo("dirDownLeft", "左下（斜向）", true, true),
        new ActionInfo("dirDownRight", "右下（斜向）", true, true),
        new ActionInfo("dirRotate", "转圈", true, true),
    };
}
