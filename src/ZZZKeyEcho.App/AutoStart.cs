using System.Diagnostics;
using Microsoft.Win32;

namespace ZZZKeyEcho.App;

public static class AutoStart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ZZZKeyEcho";
    private const string TaskName = "ZZZKeyEcho";

    public static bool IsEnabled()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("schtasks.exe", $"/Query /TN \"{TaskName}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });

            process?.WaitForExit(5000);
            return process?.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static void Set(bool enabled)
    {
        RemoveLegacyRunEntry();

        if (!enabled)
        {
            Run($"/Delete /TN \"{TaskName}\" /F");
            return;
        }

        var path = Environment.ProcessPath;

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        Run($"/Create /TN \"{TaskName}\" /TR \"\\\"{path}\\\"\" /SC ONLOGON /RL HIGHEST /F");
    }

    private static void RemoveLegacyRunEntry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch (Exception)
        {
        }
    }

    private static void Run(string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo("schtasks.exe", arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
        });

        process?.WaitForExit(8000);
    }
}
