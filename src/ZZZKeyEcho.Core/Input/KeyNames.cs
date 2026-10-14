namespace ZZZKeyEcho.Core.Input;

public static class KeyNames
{
    public const string MouseLeft = "MouseLeft";
    public const string MouseRight = "MouseRight";
    public const string MouseMiddle = "MouseMiddle";
    public const string MouseX1 = "MouseX1";
    public const string MouseX2 = "MouseX2";

    public static string? FromVirtualKey(uint virtualKey)
    {
        if (virtualKey is >= 'A' and <= 'Z' or >= '0' and <= '9')
        {
            return ((char)virtualKey).ToString();
        }

        if (virtualKey is >= 0x70 and <= 0x87)
        {
            return "F" + (virtualKey - 0x70 + 1);
        }

        return virtualKey switch
        {
            0x08 => "Backspace",
            0x09 => "Tab",
            0x0D => "Enter",
            0x14 => "CapsLock",
            0x1B => "Escape",
            0x20 => "Space",
            0x21 => "PageUp",
            0x22 => "PageDown",
            0x23 => "End",
            0x24 => "Home",
            0x25 => "Left",
            0x26 => "Up",
            0x27 => "Right",
            0x28 => "Down",
            0x2D => "Insert",
            0x2E => "Delete",
            0x5B or 0x5C => "Win",
            0x60 => "Num0",
            0x61 => "Num1",
            0x62 => "Num2",
            0x63 => "Num3",
            0x64 => "Num4",
            0x65 => "Num5",
            0x66 => "Num6",
            0x67 => "Num7",
            0x68 => "Num8",
            0x69 => "Num9",
            0x6A => "NumMultiply",
            0x6B => "NumAdd",
            0x6D => "NumSubtract",
            0x6E => "NumDecimal",
            0x6F => "NumDivide",
            0x90 => "NumLock",
            0x91 => "ScrollLock",
            0xA0 => "LShift",
            0xA1 => "RShift",
            0xA2 => "LControl",
            0xA3 => "RControl",
            0xA4 => "LAlt",
            0xA5 => "RAlt",
            0xBA => "Semicolon",
            0xBB => "Equals",
            0xBC => "Comma",
            0xBD => "Minus",
            0xBE => "Period",
            0xBF => "Slash",
            0xC0 => "Backquote",
            0xDB => "BracketLeft",
            0xDC => "Backslash",
            0xDD => "BracketRight",
            0xDE => "Quote",
            _ => null,
        };
    }
}
