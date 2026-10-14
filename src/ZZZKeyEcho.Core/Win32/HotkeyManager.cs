using System.Windows.Interop;

namespace ZZZKeyEcho.Core.Win32;

public sealed class HotkeyManager : IDisposable
{
    private readonly HwndSource _source;
    private readonly Dictionary<int, string> _registered = new();
    private int _nextId = 0x5A00;
    private bool _disposed;

    public event EventHandler<string>? Pressed;

    public HotkeyManager()
    {
        var parameters = new HwndSourceParameters("ZZZKeyEcho.Hotkeys")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0,
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    public bool Register(string name, string gestureText) =>
        HotkeyGesture.TryParse(gestureText, out var gesture) && Register(name, gesture);

    public bool Register(string name, HotkeyGesture gesture)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Unregister(name);

        var id = _nextId++;
        var modifiers = gesture.Modifiers | NativeMethods.MOD_NOREPEAT;

        if (!NativeMethods.RegisterHotKey(_source.Handle, id, modifiers, gesture.VirtualKey))
        {
            return false;
        }

        _registered[id] = name;
        return true;
    }

    public void Unregister(string name)
    {
        var idsToRemove = new List<int>();

        foreach (var pair in _registered)
        {
            if (pair.Value == name)
            {
                idsToRemove.Add(pair.Key);
            }
        }

        foreach (var id in idsToRemove)
        {
            _registered.Remove(id);
            NativeMethods.UnregisterHotKey(_source.Handle, id);
        }
    }

    public void UnregisterAll()
    {
        var ids = new List<int>(_registered.Keys);
        _registered.Clear();

        foreach (var id in ids)
        {
            NativeMethods.UnregisterHotKey(_source.Handle, id);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UnregisterAll();
        _source.RemoveHook(WndProc);
        _source.Dispose();
        _disposed = true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && _registered.TryGetValue(wParam.ToInt32(), out var name))
        {
            handled = true;
            Pressed?.Invoke(this, name);
        }

        return IntPtr.Zero;
    }
}
