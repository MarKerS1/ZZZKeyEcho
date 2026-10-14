namespace ZZZKeyEcho.Core.Input;

public static class KeyCatalog
{
    public static readonly IReadOnlyList<string> Kbm = new[]
    {
        "W", "A", "S", "D", "Q", "E", "R", "F", "G", "V", "X", "Z", "C", "T", "B",
        "1", "2", "3", "4", "5", "6", "7", "8", "9",
        "LShift", "RShift", "LControl", "RControl", "LAlt", "RAlt",
        "Space", "Tab", "Enter", "Backspace", "CapsLock", "Escape",
        "Delete", "Insert", "Home", "End", "PageUp", "PageDown",
        "Up", "Down", "Left", "Right",
        "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
        KeyNames.MouseLeft, KeyNames.MouseRight, KeyNames.MouseMiddle, KeyNames.MouseX1, KeyNames.MouseX2,
    };

    public static readonly IReadOnlyList<string> Xbox = new[]
    {
        "AButton", "BButton", "XButton", "YButton",
        "LB", "RB", "LT", "RT", "L3", "R3",
        "DPadUp", "DPadDown", "DPadLeft", "DPadRight",
    };

    public static readonly IReadOnlyList<string> Ps = new[]
    {
        "Cross", "Circle", "Square", "Triangle",
        "L1", "R1", "L2", "R2", "L3", "R3",
        "DPadUp", "DPadDown", "DPadLeft", "DPadRight",
    };
}
