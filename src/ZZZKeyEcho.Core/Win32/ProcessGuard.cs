using System.IO;
using System.Threading;
using System.Diagnostics;

namespace ZZZKeyEcho.Core.Win32;

public sealed record ProcessInfo(int Id, DateTime StartedAt);

public static class ProcessGuard
{
    public static IReadOnlyList<ProcessInfo> FindOtherInstances()
    {
        var result = new List<ProcessInfo>();
        var currentPath = Environment.ProcessPath;

        if (string.IsNullOrEmpty(currentPath))
        {
            return result;
        }

        var name = Path.GetFileNameWithoutExtension(currentPath);

        foreach (var process in Process.GetProcessesByName(name))
        {
            try
            {
                if (process.Id == Environment.ProcessId)
                {
                    continue;
                }

                bool matches;

                try
                {
                    matches = string.Equals(
                        process.MainModule?.FileName,
                        currentPath,
                        StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception)
                {
                    matches = true;
                }

                if (!matches)
                {
                    continue;
                }

                DateTime startedAt;

                try
                {
                    startedAt = process.StartTime;
                }
                catch (Exception)
                {
                    startedAt = DateTime.MinValue;
                }

                result.Add(new ProcessInfo(process.Id, startedAt));
            }
            finally
            {
                process.Dispose();
            }
        }

        return result;
    }

    public static bool TerminateAll(TimeSpan grace)
    {
        foreach (var instance in FindOtherInstances())
        {
            try
            {
                using var process = Process.GetProcessById(instance.Id);
                process.CloseMainWindow();
            }
            catch (Exception)
            {
            }
        }

        var deadline = DateTime.UtcNow + grace;

        while (DateTime.UtcNow < deadline && FindOtherInstances().Count > 0)
        {
            Thread.Sleep(100);
        }

        foreach (var instance in FindOtherInstances())
        {
            try
            {
                using var process = Process.GetProcessById(instance.Id);
                process.Kill();
            }
            catch (Exception)
            {
            }
        }

        var killDeadline = DateTime.UtcNow.AddSeconds(2);

        while (DateTime.UtcNow < killDeadline && FindOtherInstances().Count > 0)
        {
            Thread.Sleep(100);
        }

        return FindOtherInstances().Count == 0;
    }
}
