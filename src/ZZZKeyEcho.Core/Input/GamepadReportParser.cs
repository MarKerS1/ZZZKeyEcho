namespace ZZZKeyEcho.Core.Input;

public static class GamepadReportParser
{
    public const int DualSenseUsbReportId = 0x01;
    public const int DualSenseBluetoothReportId = 0x31;
    public const int Ds4UsbReportId = 0x01;
    public const int Ds4BluetoothReportId = 0x11;

    private static readonly Dictionary<int, string> DPadDirections = new()
    {
        [0] = "Up",
        [1] = "UpRight",
        [2] = "Right",
        [3] = "DownRight",
        [4] = "Down",
        [5] = "DownLeft",
        [6] = "Left",
        [7] = "UpLeft",
    };

    public static bool TryParseDualSense(byte[] report, out GamepadReading reading)
    {
        reading = default;

        if (report.Length < 11)
        {
            return false;
        }

        var offset = report[0] == DualSenseBluetoothReportId ? 1 : 0;

        if (offset == 1 && report.Length < 12)
        {
            return false;
        }

        var buttons1 = report[8 + offset];
        var buttons2 = report[9 + offset];
        var buttons3 = report[10 + offset];
        var dpad = DPadDirections.GetValueOrDefault(buttons1 & 0x0F);

        reading = new GamepadReading(
            Square: (buttons1 & 0x10) != 0,
            Cross: (buttons1 & 0x20) != 0,
            Circle: (buttons1 & 0x40) != 0,
            Triangle: (buttons1 & 0x80) != 0,
            L1: (buttons2 & 0x01) != 0,
            R1: (buttons2 & 0x02) != 0,
            L2: (buttons2 & 0x04) != 0,
            R2: (buttons2 & 0x08) != 0,
            L3: (buttons2 & 0x40) != 0,
            R3: (buttons2 & 0x80) != 0,
            DPadUp: dpad is "Up" or "UpRight" or "UpLeft",
            DPadDown: dpad is "Down" or "DownRight" or "DownLeft",
            DPadLeft: dpad is "Left" or "UpLeft" or "DownLeft",
            DPadRight: dpad is "Right" or "UpRight" or "DownRight",
            LeftStickX: NormalizeAxis(report[1 + offset] - 128),
            LeftStickY: NormalizeAxis(128 - report[2 + offset]),
            LeftTrigger: report[5 + offset] / 255.0,
            RightTrigger: report[6 + offset] / 255.0);

        _ = buttons3;
        return true;
    }

    public static bool TryParseDs4(byte[] report, out GamepadReading reading)
    {
        reading = default;

        if (report.Length < 8)
        {
            return false;
        }

        var offset = report[0] == Ds4BluetoothReportId ? 1 : 0;

        if (offset == 1 && report.Length < 9)
        {
            return false;
        }

        var buttons1 = report[5 + offset];
        var buttons2 = report[6 + offset];
        var dpad = DPadDirections.GetValueOrDefault(buttons1 & 0x0F);

        reading = new GamepadReading(
            Square: (buttons1 & 0x10) != 0,
            Cross: (buttons1 & 0x20) != 0,
            Circle: (buttons1 & 0x40) != 0,
            Triangle: (buttons1 & 0x80) != 0,
            L1: (buttons2 & 0x01) != 0,
            R1: (buttons2 & 0x02) != 0,
            L2: (buttons2 & 0x04) != 0,
            R2: (buttons2 & 0x08) != 0,
            L3: (buttons2 & 0x40) != 0,
            R3: (buttons2 & 0x80) != 0,
            DPadUp: dpad is "Up" or "UpRight" or "UpLeft",
            DPadDown: dpad is "Down" or "DownRight" or "DownLeft",
            DPadLeft: dpad is "Left" or "UpLeft" or "DownLeft",
            DPadRight: dpad is "Right" or "UpRight" or "DownRight",
            LeftStickX: NormalizeAxis(report[1 + offset] - 128),
            LeftStickY: NormalizeAxis(128 - report[2 + offset]),
            LeftTrigger: report[8 + offset] / 255.0,
            RightTrigger: report[9 + offset] / 255.0);

        return true;
    }

    private static double NormalizeAxis(int value) => Math.Clamp(value / 128.0, -1, 1);
}
