using System.Runtime.InteropServices;
using System.Windows.Interop;
using ZZZKeyEcho.Core.Win32;

namespace ZZZKeyEcho.Core.Input;

public sealed class KeyboardMouseHookSource : IInputSource
{
    private const int WmInput = 0x00FF;
    private const uint RidInput = 0x10000003;
    private const uint RidevInputSink = 0x00000100;
    private const uint RimTypeKeyboard = 1;
    private const ushort RiKeyBreak = 0x0001;

    private readonly IClock _clock;
    private readonly NativeMethods.HookProc _keyboardProc;
    private readonly NativeMethods.HookProc _mouseProc;
    private HwndSource? _rawInputSource;
    private bool _rawInputRegistered;
    private IntPtr _keyboardHook = IntPtr.Zero;
    private IntPtr _mouseHook = IntPtr.Zero;
    private bool _disposed;

    public KeyboardMouseHookSource(IClock clock)
    {
        _clock = clock;
        _keyboardProc = KeyboardProc;
        _mouseProc = MouseProc;
    }

    public event EventHandler<InputEvent>? Event;

    public bool IsRunning =>
        (_rawInputRegistered || _keyboardHook != IntPtr.Zero) && _mouseHook != IntPtr.Zero;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsRunning)
        {
            return;
        }

        if (!StartRawInput())
        {
            var module = NativeMethods.GetModuleHandle(null);
            _keyboardHook = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_KEYBOARD_LL,
                _keyboardProc,
                module,
                0);
        }

        var mouseModule = NativeMethods.GetModuleHandle(null);
        _mouseHook = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL,
            _mouseProc,
            mouseModule,
            0);
    }

    public void Stop()
    {
        StopRawInput();

        if (_keyboardHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }

        if (_mouseHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }

    private bool StartRawInput()
    {
        try
        {
            var parameters = new HwndSourceParameters("ZZZKeyEcho.RawInput")
            {
                Width = 0,
                Height = 0,
                WindowStyle = 0,
            };

            _rawInputSource = new HwndSource(parameters);
            _rawInputSource.AddHook(RawInputProc);

            var device = new RawInputDevice
            {
                UsagePage = 0x01,
                Usage = 0x06,
                Flags = RidevInputSink,
                Target = _rawInputSource.Handle,
            };

            _rawInputRegistered = RegisterRawInputDevices(
                new[] { device },
                1,
                (uint)Marshal.SizeOf<RawInputDevice>());

            if (_rawInputRegistered)
            {
                return true;
            }

            _rawInputSource.RemoveHook(RawInputProc);
            _rawInputSource.Dispose();
            _rawInputSource = null;
            return false;
        }
        catch (Exception)
        {
            _rawInputRegistered = false;
            _rawInputSource?.Dispose();
            _rawInputSource = null;
            return false;
        }
    }

    private void StopRawInput()
    {
        if (!_rawInputRegistered)
        {
            return;
        }

        _rawInputRegistered = false;

        if (_rawInputSource is not null)
        {
            _rawInputSource.RemoveHook(RawInputProc);
            _rawInputSource.Dispose();
            _rawInputSource = null;
        }
    }

    private IntPtr RawInputProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmInput)
        {
            return IntPtr.Zero;
        }

        handled = true;

        try
        {
            HandleRawInput(lParam);
        }
        catch (Exception)
        {
        }

        return IntPtr.Zero;
    }

    private void HandleRawInput(IntPtr rawInputHandle)
    {
        var headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
        var size = 0u;

        if (GetRawInputData(rawInputHandle, RidInput, IntPtr.Zero, ref size, headerSize) != 0 || size == 0)
        {
            return;
        }

        var buffer = Marshal.AllocHGlobal((int)size);

        try
        {
            if (GetRawInputData(rawInputHandle, RidInput, buffer, ref size, headerSize) == uint.MaxValue)
            {
                return;
            }

            var raw = Marshal.PtrToStructure<RawInput>(buffer);

            if (raw.Header.Type != RimTypeKeyboard)
            {
                return;
            }

            var keyboard = raw.Keyboard;
            var down = (keyboard.Flags & RiKeyBreak) == 0;

            if (KeyNames.FromVirtualKey(keyboard.VKey) is { } code)
            {
                Raise(InputDevice.Kbm, code, down ? InputEventKind.Down : InputEventKind.Up);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

            if ((data.flags & NativeMethods.LLKHF_INJECTED) == 0)
            {
                var kind = (int)wParam switch
                {
                    NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN => (InputEventKind?)InputEventKind.Down,
                    NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP => InputEventKind.Up,
                    _ => null,
                };

                if (kind is not null && KeyNames.FromVirtualKey(data.vkCode) is { } code)
                {
                    Raise(InputDevice.Kbm, code, kind.Value);
                }
            }
        }

        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);

            if ((data.flags & NativeMethods.LLMHF_INJECTED) == 0)
            {
                var (code, kind) = (int)wParam switch
                {
                    NativeMethods.WM_LBUTTONDOWN => (KeyNames.MouseLeft, (InputEventKind?)InputEventKind.Down),
                    NativeMethods.WM_LBUTTONUP => (KeyNames.MouseLeft, InputEventKind.Up),
                    NativeMethods.WM_RBUTTONDOWN => (KeyNames.MouseRight, InputEventKind.Down),
                    NativeMethods.WM_RBUTTONUP => (KeyNames.MouseRight, InputEventKind.Up),
                    NativeMethods.WM_MBUTTONDOWN => (KeyNames.MouseMiddle, InputEventKind.Down),
                    NativeMethods.WM_MBUTTONUP => (KeyNames.MouseMiddle, InputEventKind.Up),
                    NativeMethods.WM_XBUTTONDOWN => (FromXButton(data.mouseData), (InputEventKind?)InputEventKind.Down),
                    NativeMethods.WM_XBUTTONUP => (FromXButton(data.mouseData), InputEventKind.Up),
                    _ => (null, null),
                };

                if (code is not null && kind is not null)
                {
                    Raise(InputDevice.Kbm, code, kind.Value);
                }
            }
        }

        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private static string? FromXButton(uint mouseData) =>
        (int)(mouseData >> 16) switch
        {
            NativeMethods.XBUTTON1 => KeyNames.MouseX1,
            NativeMethods.XBUTTON2 => KeyNames.MouseX2,
            _ => null,
        };

    private void Raise(InputDevice device, string code, InputEventKind kind) =>
        Event?.Invoke(this, new InputEvent(device, code, kind, _clock.NowMs));

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDevice
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputHeader
    {
        public uint Type;
        public uint Size;
        public IntPtr Device;
        public IntPtr WParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawKeyboard
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInput
    {
        public RawInputHeader Header;
        public RawKeyboard Keyboard;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(
        RawInputDevice[] devices,
        uint count,
        uint size);

    [DllImport("user32.dll")]
    private static extern uint GetRawInputData(
        IntPtr rawInput,
        uint command,
        IntPtr data,
        ref uint size,
        uint headerSize);
}
