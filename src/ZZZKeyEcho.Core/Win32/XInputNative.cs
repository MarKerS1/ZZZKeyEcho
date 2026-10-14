using System.Runtime.InteropServices;
using ZZZKeyEcho.Core.Input;

namespace ZZZKeyEcho.Core.Win32;

internal static class XInputNative
{
    public const int ErrorSuccess = 0;

    public const ushort DPadUp = 0x0001;
    public const ushort DPadDown = 0x0002;
    public const ushort DPadLeft = 0x0004;
    public const ushort DPadRight = 0x0008;
    public const ushort ButtonStart = 0x0010;
    public const ushort ButtonBack = 0x0020;
    public const ushort LeftThumb = 0x0040;
    public const ushort RightThumb = 0x0080;
    public const ushort LeftShoulder = 0x0100;
    public const ushort RightShoulder = 0x0200;
    public const ushort ButtonA = 0x1000;
    public const ushort ButtonB = 0x2000;
    public const ushort ButtonX = 0x4000;
    public const ushort ButtonY = 0x8000;

    [StructLayout(LayoutKind.Sequential)]
    public struct XInputGamepad
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XInputState
    {
        public uint dwPacketNumber;
        public XInputGamepad Gamepad;
    }

    private static bool _useModernLibrary = true;
    private static bool _libraryChecked;

    public static bool TryGetState(uint index, out XInputState state)
    {
        state = default;

        if (_useModernLibrary)
        {
            try
            {
                return GetState14(index, out state) == ErrorSuccess;
            }
            catch (DllNotFoundException)
            {
                _useModernLibrary = false;
                _libraryChecked = true;
            }
            catch (EntryPointNotFoundException)
            {
                _useModernLibrary = false;
                _libraryChecked = true;
            }
        }

        try
        {
            return GetState13(index, out state) == ErrorSuccess;
        }
        catch (DllNotFoundException)
        {
            _libraryChecked = true;
            return false;
        }
    }

    public static bool IsLibraryAvailable()
    {
        _libraryChecked = false;
        var available = TryGetState(0, out _);
        return available || !_libraryChecked;
    }

    public static GamepadReading ToReading(XInputState state)
    {
        var buttons = state.Gamepad.wButtons;

        return new GamepadReading(
            Square: (buttons & ButtonX) != 0,
            Cross: (buttons & ButtonA) != 0,
            Circle: (buttons & ButtonB) != 0,
            Triangle: (buttons & ButtonY) != 0,
            L1: (buttons & LeftShoulder) != 0,
            R1: (buttons & RightShoulder) != 0,
            L2: false,
            R2: false,
            L3: (buttons & LeftThumb) != 0,
            R3: (buttons & RightThumb) != 0,
            DPadUp: (buttons & DPadUp) != 0,
            DPadDown: (buttons & DPadDown) != 0,
            DPadLeft: (buttons & DPadLeft) != 0,
            DPadRight: (buttons & DPadRight) != 0,
            LeftStickX: NormalizeAxis(state.Gamepad.sThumbLX),
            LeftStickY: NormalizeAxis(state.Gamepad.sThumbLY),
            LeftTrigger: state.Gamepad.bLeftTrigger / 255.0,
            RightTrigger: state.Gamepad.bRightTrigger / 255.0);
    }

    private static double NormalizeAxis(short value) => Math.Clamp(value / 32767.0, -1, 1);

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern int GetState14(uint dwUserIndex, out XInputState pState);

    [DllImport("xinput1_3.dll", EntryPoint = "XInputGetState")]
    private static extern int GetState13(uint dwUserIndex, out XInputState pState);
}
