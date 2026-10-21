using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZZZKeyEcho.App.Rendering;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Engine;

namespace ZZZKeyEcho.App;

public sealed class WebOverlayServer : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly AppConfig _config;
    private readonly RecordEngine _engine;
    private readonly IconProvider _icons;
    private readonly string _assetsDirectory;
    private readonly Dictionary<string, string> _iconIndex = new(StringComparer.Ordinal);
    private readonly Dictionary<string, byte[]> _composites = new(StringComparer.Ordinal);
    private readonly DispatcherTimer _timer;
    private HttpListener? _listener;
    private string _state = "{}";
    private string? _lastSnapshot;

    public WebOverlayServer(
        AppConfig config,
        RecordEngine engine,
        IconProvider icons,
        string assetsDirectory)
    {
        _config = config;
        _engine = engine;
        _icons = icons;
        _assetsDirectory = assetsDirectory;
        BuildIconIndex();
        BuildCompositeIcons();
        _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(50),
        };
        _timer.Tick += (_, _) => _state = BuildState();
    }

    private void BuildCompositeIcons()
    {
        _composites.Clear();

        foreach (var id in new[]
                 {
                     ActionCatalog.SwitchPrev,
                     ActionCatalog.SwitchNext,
                     ActionCatalog.ChainLeft,
                     ActionCatalog.ChainRight,
                     ActionCatalog.ChainCancel,
                 })
        {
            if (_icons.GetActionIcon(id) is not BitmapSource bitmap)
            {
                continue;
            }

            try
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using var stream = new MemoryStream();
                encoder.Save(stream);
                _composites[Normalize(id)] = stream.ToArray();
            }
            catch (Exception)
            {
            }
        }
    }

    private static string Normalize(string name) =>
        new(name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private void BuildIconIndex()
    {
        var icons = Path.Combine(_assetsDirectory, "icons");

        if (!Directory.Exists(icons))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(icons, "*.png"))
        {
            _iconIndex[Normalize(Path.GetFileNameWithoutExtension(file))] = file;
        }

        var custom = Path.Combine(icons, "custom");

        if (Directory.Exists(custom))
        {
            foreach (var file in Directory.GetFiles(custom, "*.png"))
            {
                _iconIndex[Normalize(Path.GetFileNameWithoutExtension(file))] = file;
            }
        }
    }

    public bool IsRunning => _listener?.IsListening == true;

    public string Url => $"http://127.0.0.1:{_config.WebOverlay.Port}/";

    public void Sync()
    {
        BuildCompositeIcons();

        if (_config.WebOverlay.Enabled && !IsRunning)
        {
            Start();
        }
        else if (!_config.WebOverlay.Enabled && IsRunning)
        {
            Stop();
        }
    }

    public void Start()
    {
        Stop();

        try
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{_config.WebOverlay.Port}/");
            listener.Start();
            listener.BeginGetContext(OnContext, listener);
            _listener = listener;
            _timer.Start();
        }
        catch (Exception)
        {
            _listener = null;
        }
    }

    public void Stop()
    {
        _timer.Stop();
        _listener?.Close();
        _listener = null;
    }

    private void OnContext(IAsyncResult result)
    {
        if (result.AsyncState is not HttpListener listener || !listener.IsListening)
        {
            return;
        }

        HttpListenerContext context;

        try
        {
            context = listener.EndGetContext(result);
        }
        catch (Exception)
        {
            return;
        }

        try
        {
            Handle(context);
        }
        catch (Exception)
        {
            // 单个请求失败不影响服务。
        }
        finally
        {
            try
            {
                context.Response.Close();
            }
            catch (Exception)
            {
            }

            if (listener.IsListening)
            {
                listener.BeginGetContext(OnContext, listener);
            }
        }
    }

    private void Handle(HttpListenerContext context)
    {
        var path = context.Request.Url?.AbsolutePath ?? "/";

        if (path == "/state")
        {
            Write(context, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(_state));
            return;
        }

        if (path.StartsWith("/icons/", StringComparison.Ordinal))
        {
            var name = Path.GetFileNameWithoutExtension(
                Uri.UnescapeDataString(path["/icons/".Length..]));
            var key = Normalize(name);

            if (_composites.TryGetValue(key, out var composite))
            {
                Write(context, "image/png", composite);
                return;
            }

            if (_iconIndex.TryGetValue(key, out var file)
                || _iconIndex.TryGetValue(Normalize("assist"), out file))
            {
                Write(context, "image/png", File.ReadAllBytes(file));
                return;
            }

            context.Response.StatusCode = 404;
            return;
        }

        var page = Path.Combine(_assetsDirectory, "web", "index.html");

        if (File.Exists(page))
        {
            Write(context, "text/html; charset=utf-8", File.ReadAllBytes(page));
            return;
        }

        context.Response.StatusCode = 404;
    }

    private static void Write(HttpListenerContext context, string contentType, byte[] body)
    {
        context.Response.ContentType = contentType;
        context.Response.ContentLength64 = body.Length;
        context.Response.OutputStream.Write(body, 0, body.Length);
    }

    private string BuildState()
    {
        var records = _engine.Records.Select(ToSnapshot).ToList();
        var state = new
        {
            settings = new
            {
                background = _config.Overlay.BgColor,
                backgroundOpacity = _config.Overlay.BackgroundOpacity,
                iconOpacity = _config.Overlay.IconOpacity,
                iconSize = _config.Records.IconSize,
                cornerRadius = _config.Overlay.CornerRadius,
                padding = _config.Overlay.Padding,
                newestAtBottom = _config.Records.NewestAtBottom,
                durationOnLeft = _config.Records.DurationOnLeft,
                fontScale = _config.Overlay.TextScale,
            },
            records,
        };

        var json = JsonSerializer.Serialize(state, JsonOptions);

        if (json == _lastSnapshot)
        {
            return _state;
        }

        _lastSnapshot = json;
        return json;
    }

    private object ToSnapshot(InputRecord record) => new
    {
        movement = record.Movement.Select(ToIconName).Where(x => x is not null).ToList(),
        actions = record.Actions.ToList(),
        count = record.Count,
        time = FormatDuration(record),
        holding = record.IsHolding,
        neutral = record.IsNeutral,
        opacity = Math.Round(record.DisplayOpacity, 3),
    };

    private string FormatDuration(InputRecord record)
    {
        var milliseconds = record.DisplayDurationMs;

        if (milliseconds <= 0 || _config.Records.HoldDisplay == HoldDisplayMode.Off)
        {
            return string.Empty;
        }

        if (_config.Records.HoldDisplay == HoldDisplayMode.Threshold
            && milliseconds < _config.Records.HoldThresholdMs)
        {
            return string.Empty;
        }

        return _config.Records.HoldFormat == HoldTimeFormat.Frames
            ? $"{Math.Round(milliseconds / 1000 * Math.Max(1, _config.Records.FramesPerSecond)):0}f"
            : $"{milliseconds / 1000:0.000}s";
    }

    private static string? ToIconName(Direction direction) => direction switch
    {
        Direction.Up => "dirUp",
        Direction.Down => "dirDown",
        Direction.Left => "dirLeft",
        Direction.Right => "dirRight",
        Direction.UpLeft => "dirUpLeft",
        Direction.UpRight => "dirUpRight",
        Direction.DownLeft => "dirDownLeft",
        Direction.DownRight => "dirDownRight",
        Direction.Rotate => "dirRotate",
        _ => null,
    };

    public void Dispose() => Stop();
}
