using System.Runtime.InteropServices;
using ZZZKeyEcho.Core.Win32;

namespace ZZZKeyEcho.Core.Input;

public sealed class KeyboardMouseHookSource : IInputSource
{
    private readonly IClock _clock;
    private readonly NativeMethods.HookProc _keyboardProc;
    private readonly NativeMethods.HookProc _mouseProc;
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

    public bool IsRunning => _keyboardHook != IntPtr.Zero && _mouseHook != IntPtr.Zero;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsRunning)
        {
            return;
        }

        var module = NativeMethods.GetModuleHandle(null);

        _keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _keyboardProc, module, 0);
        _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseProc, module, 0);
    }

    public void Stop()
    {
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
}
