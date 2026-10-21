using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace ZZZKeyEcho.Core.Update;

public enum UpdateCheckStatus
{
    UpToDate,
    UpdateAvailable,
    Failed,
}

public sealed record UpdateInfo(string CurrentVersion, string LatestVersion, string Url);

public sealed record UpdateCheckResult(
    UpdateCheckStatus Status,
    string CurrentVersion,
    UpdateInfo? Update,
    string? Message);

public static class UpdateChecker
{
    public const string ApiUrl = "https://api.github.com/repos/MarKerS1/ZZZKeyEcho/releases/latest";
    public const string ReleasesUrl = "https://github.com/MarKerS1/ZZZKeyEcho/releases/latest";

    public static string CurrentVersion
    {
        get
        {
            var assembly = Assembly.GetEntryAssembly() ?? typeof(UpdateChecker).Assembly;
            var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var version = string.IsNullOrWhiteSpace(informational)
                ? (assembly.GetName().Version is { } build ? $"{build.Major}.{build.Minor}.{build.Build}" : "0.0.0")
                : informational;

            return Normalize(version ?? "0.0.0");
        }
    }

    public static async Task<UpdateCheckResult> CheckAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);

            using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ZZZKeyEcho");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            var json = await client.GetStringAsync(ApiUrl, timeoutSource.Token).ConfigureAwait(false);
            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("tag_name", out var tag))
            {
                return new UpdateCheckResult(
                    UpdateCheckStatus.Failed,
                    CurrentVersion,
                    null,
                    "GitHub 返回的数据里没有 tag_name。");
            }

            var latest = Normalize(tag.GetString());

            if (!Version.TryParse(latest, out var latestVersion)
                || !Version.TryParse(CurrentVersion, out var currentVersion))
            {
                return new UpdateCheckResult(
                    UpdateCheckStatus.Failed,
                    CurrentVersion,
                    null,
                    $"无法解析版本号（当前 {CurrentVersion}，最新 {latest}）。");
            }

            return latestVersion > currentVersion
                ? new UpdateCheckResult(
                    UpdateCheckStatus.UpdateAvailable,
                    CurrentVersion,
                    new UpdateInfo(CurrentVersion, latest, ReleasesUrl),
                    null)
                : new UpdateCheckResult(UpdateCheckStatus.UpToDate, CurrentVersion, null, null);
        }
        catch (OperationCanceledException)
        {
            return new UpdateCheckResult(
                UpdateCheckStatus.Failed,
                CurrentVersion,
                null,
                "连接 GitHub 超时（10 秒）。");
        }
        catch (Exception exception)
        {
            return new UpdateCheckResult(
                UpdateCheckStatus.Failed,
                CurrentVersion,
                null,
                exception.Message);
        }
    }

    private static string Normalize(string? value)
    {
        var text = (value ?? string.Empty).Trim().TrimStart('v', 'V');
        var plus = text.IndexOf('+');
        return plus > 0 ? text[..plus] : text;
    }
}
