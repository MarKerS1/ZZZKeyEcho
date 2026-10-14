using System.Diagnostics;

namespace ZZZKeyEcho.Core.Win32;

public sealed class ForegroundMonitor
{
    private IntPtr _cachedWindow = IntPtr.Zero;
    private bool _cachedResult;
    private string? _cachedName;

    public string? ForegroundProcessName => _cachedName;

    public bool IsForeground(IReadOnlyList<string> processNames)
    {
        try
        {
            var window = NativeMethods.GetForegroundWindow();

            if (window == IntPtr.Zero)
            {
                _cachedWindow = IntPtr.Zero;
                _cachedName = null;
                _cachedResult = false;
                return false;
            }

            if (window == _cachedWindow)
            {
                return _cachedResult;
            }

            _cachedWindow = window;
            _cachedName = TryGetProcessName(window);
            _cachedResult = Matches(_cachedName, processNames);
            return _cachedResult;
        }
        catch (Exception)
        {
            _cachedWindow = IntPtr.Zero;
            _cachedName = null;
            _cachedResult = false;
            return false;
        }
    }

    private static string? TryGetProcessName(IntPtr window)
    {
        try
        {
            NativeMethods.GetWindowThreadProcessId(window, out var processId);

            if (processId == 0)
            {
                return null;
            }

            using var process = System.Diagnostics.Process.GetProcessById((int)processId);
            return process.ProcessName + ".exe";
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool Matches(string? name, IReadOnlyList<string> processNames)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        foreach (var candidate in processNames)
        {
            if (string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
