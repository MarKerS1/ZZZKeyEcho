namespace ZZZKeyEcho.Core.Win32;

public readonly record struct HotkeyGesture(uint Modifiers, uint VirtualKey)
{
    public static bool TryParse(string? text, out HotkeyGesture gesture)
    {
        gesture = default;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        uint modifiers = 0;
        uint virtualKey = 0;

        foreach (var part in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (TryParseModifier(part, out var modifier))
            {
                modifiers |= modifier;
                continue;
            }

            if (virtualKey != 0 || !TryParseKey(part, out virtualKey))
            {
                return false;
            }
        }

        if (virtualKey == 0)
        {
            return false;
        }

        gesture = new HotkeyGesture(modifiers, virtualKey);
        return true;
    }

    private static bool TryParseModifier(string part, out uint modifier)
    {
        switch (part.ToUpperInvariant())
        {
            case "CTRL":
            case "CONTROL":
            case "LCONTROL":
            case "RCONTROL":
                modifier = NativeMethods.MOD_CONTROL;
                return true;
            case "ALT":
            case "LALT":
            case "RALT":
                modifier = NativeMethods.MOD_ALT;
                return true;
            case "SHIFT":
            case "LSHIFT":
            case "RSHIFT":
                modifier = NativeMethods.MOD_SHIFT;
                return true;
            case "WIN":
            case "WINDOWS":
                modifier = NativeMethods.MOD_WIN;
                return true;
            default:
                modifier = 0;
                return false;
        }
    }

    private static bool TryParseKey(string part, out uint virtualKey)
    {
        virtualKey = 0;

        if (part.Length == 1)
        {
            var character = char.ToUpperInvariant(part[0]);

            if (character is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                virtualKey = character;
                return true;
            }

            return false;
        }

        if (part.Length >= 2
            && char.ToUpperInvariant(part[0]) == 'F'
            && int.TryParse(part.AsSpan(1), out var functionKeyNumber)
            && functionKeyNumber is >= 1 and <= 24)
        {
            virtualKey = (uint)(0x70 + functionKeyNumber - 1);
            return true;
        }

        virtualKey = part.ToUpperInvariant() switch
        {
            "SPACE" => 0x20,
            "ENTER" or "RETURN" => 0x0D,
            "TAB" => 0x09,
            "ESC" or "ESCAPE" => 0x1B,
            "BACKSPACE" => 0x08,
            "DELETE" or "DEL" => 0x2E,
            "INSERT" or "INS" => 0x2D,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" or "PGUP" => 0x21,
            "PAGEDOWN" or "PGDN" => 0x22,
            "UP" => 0x26,
            "DOWN" => 0x28,
            "LEFT" => 0x25,
            "RIGHT" => 0x27,
            _ => 0,
        };

        return virtualKey != 0;
    }
}
