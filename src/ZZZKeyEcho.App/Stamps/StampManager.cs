using System.Windows;
using System.Windows.Threading;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Stamps;

namespace ZZZKeyEcho.App.Stamps;

public sealed record StampWebSnapshot(
    int Index,
    int X,
    int Y,
    int Width,
    int Height,
    double BorderThickness,
    double BorderOpacity,
    double ContentOpacity,
    string BorderColor,
    double CornerRadius,
    double Padding,
    string Key);

public sealed class StampManager : IDisposable
{
    private readonly AppConfig _config;
    private readonly Func<bool> _isGameRunning;
    private readonly Dispatcher _dispatcher;
    private readonly List<StampWindow> _windows = new();
    private readonly Dictionary<int, byte[]> _jpegFrames = new();
    private readonly object _jpegLock = new();
    private readonly StampCaptureService _capture;
    private bool _editing;
    private bool _gameRunning;

    public StampManager(AppConfig config, Func<bool> isGameRunning)
    {
        _config = config;
        _isGameRunning = isGameRunning;
        _dispatcher = Dispatcher.CurrentDispatcher;

        _capture = new StampCaptureService(BuildTargets);
        _capture.FrameReady += OnFrameReady;
        _gameRunning = isGameRunning();
        _capture.Start();
        ApplyAll();
    }

    public string? LastCaptureError => _capture.LastError;

    public Int32Rect WebCanvas
    {
        get
        {
            var game = MonitorHelper.GetGameClientRect(_config.Input.GameProcess);

            if (game.Width >= 16 && game.Height >= 16)
            {
                return game;
            }

            return MonitorHelper.GetPrimary().Bounds;
        }
    }

    public string WebUrl => $"http://127.0.0.1:{_config.WebOverlay.Port}/stamps";

    public bool HasWebStamps => _config.Stamps.Items.Any(stamp => stamp.WebEnabled);

    public void ApplyAll()
    {
        while (_windows.Count > _config.Stamps.Items.Count)
        {
            var last = _windows[^1];
            _windows.RemoveAt(_windows.Count - 1);
            last.Close();
        }

        while (_windows.Count < _config.Stamps.Items.Count)
        {
            var index = _windows.Count;
            var window = new StampWindow(_config, _config.Stamps.Items[index]);
            window.Show();
            _windows.Add(window);
        }

        for (var index = 0; index < _config.Stamps.Items.Count; index++)
        {
            _windows[index].UpdateStamp(_config.Stamps.Items[index]);
        }

        _capture.Fps = _config.Stamps.CaptureFps;
        ApplyVisibility();
    }

    public void ApplyVisibility()
    {
        _gameRunning = _isGameRunning();

        for (var index = 0; index < _windows.Count; index++)
        {
            var stamp = _config.Stamps.Items[index];
            var visible = !_editing
                && !_config.Stamps.Hidden
                && !stamp.HideLocal
                && (!_config.Stamps.FollowGame || _gameRunning);

            _windows[index].Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }
    }

    public void SetEditing(bool editing)
    {
        _editing = editing;
        ApplyVisibility();

        if (editing)
        {
            foreach (var window in _windows)
            {
                window.Visibility = Visibility.Hidden;
            }
        }
    }

    public void SetGameRunning(bool running)
    {
        _gameRunning = running;
        ApplyVisibility();
    }

    public IReadOnlyList<StampWebSnapshot> GetWebSnapshots()
    {
        var result = new List<StampWebSnapshot>();
        var canvas = WebCanvas;

        for (var index = 0; index < _config.Stamps.Items.Count; index++)
        {
            var stamp = _config.Stamps.Items[index];

            if (!stamp.WebEnabled)
            {
                continue;
            }

            result.Add(new StampWebSnapshot(
                index,
                stamp.OutputX - canvas.X,
                stamp.OutputY - canvas.Y,
                StampGeometry.OutputWidth(stamp),
                StampGeometry.OutputHeight(stamp),
                stamp.BorderThickness,
                stamp.BorderOpacity,
                stamp.ContentOpacity,
                stamp.BorderColor,
                stamp.CornerRadius,
                stamp.Padding,
                $"{stamp.SourceX}:{stamp.SourceY}:{stamp.SourceWidth}:{stamp.SourceHeight}"));
        }

        return result;
    }

    public bool TryGetJpeg(int index, out byte[] data)
    {
        lock (_jpegLock)
        {
            return _jpegFrames.TryGetValue(index, out data!);
        }
    }

    private IReadOnlyList<StampCaptureTarget> BuildTargets()
    {
        if (_editing || (_config.Stamps.FollowGame && !_gameRunning))
        {
            return Array.Empty<StampCaptureTarget>();
        }

        var targets = new List<StampCaptureTarget>();

        for (var index = 0; index < _config.Stamps.Items.Count; index++)
        {
            var stamp = _config.Stamps.Items[index];

            if (stamp.SourceWidth < 16 || stamp.SourceHeight < 16)
            {
                continue;
            }

            var local = index < _windows.Count && _windows[index].IsVisible;
            var web = stamp.WebEnabled && _config.WebOverlay.Enabled;

            if (!local && !web)
            {
                continue;
            }

            targets.Add(new StampCaptureTarget(
                index,
                new Int32Rect(stamp.SourceX, stamp.SourceY, stamp.SourceWidth, stamp.SourceHeight),
                local,
                web));
        }

        return targets;
    }

    private void OnFrameReady(object? sender, StampFrameEventArgs e)
    {
        if (e.Jpeg is not null)
        {
            lock (_jpegLock)
            {
                _jpegFrames[e.Index] = e.Jpeg;
            }
        }

        if (_dispatcher.HasShutdownStarted)
        {
            return;
        }

        _dispatcher.BeginInvoke(() =>
        {
            if (e.Index >= 0 && e.Index < _windows.Count)
            {
                _windows[e.Index].SetFrame(e.Frame);
            }
        }, DispatcherPriority.Render);
    }

    public void Dispose()
    {
        _capture.FrameReady -= OnFrameReady;
        _capture.Dispose();

        foreach (var window in _windows)
        {
            window.Close();
        }

        _windows.Clear();
    }
}
