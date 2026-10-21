using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ZZZKeyEcho.Core.Activity;
using ZZZKeyEcho.Core.Config;

namespace ZZZKeyEcho.App.Views;

public partial class RecordButtonWindow : Window
{
    private readonly ActivityRecorder _recorder;
    private readonly AppConfig _config;
    private readonly DispatcherTimer _timer;
    private Point _origin;
    private double _startLeft;
    private double _startTop;
    private bool _dragging;
    private bool _moved;

    public RecordButtonWindow(ActivityRecorder recorder, AppConfig config)
    {
        _recorder = recorder;
        _config = config;

        InitializeComponent();
        ApplyConfiguration();

        _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(250),
        };

        _timer.Tick += (_, _) => UpdateState();
        _timer.Start();
        UpdateState();

        Closed += (_, _) => _timer.Stop();
    }

    public void ApplyConfiguration()
    {
        var activity = _config.Activity;
        var size = Math.Clamp(activity.FloatSize, 48, 200);

        Width = size;
        Height = size + 26;
        BallHost.Width = size;
        BallHost.Height = size;
        Opacity = Math.Clamp(activity.FloatOpacity, 0.2, 1);

        var area = SystemParameters.WorkArea;

        Left = activity.FloatX >= 0 ? activity.FloatX : area.Right - Width - 24;
        Top = activity.FloatY >= 0 ? activity.FloatY : area.Bottom - Height - 48;

        Visibility = activity.FloatButtonEnabled ? Visibility.Visible : Visibility.Collapsed;
        UpdateState();
    }

    private void UpdateState()
    {
        var recording = _recorder.IsRecording;

        GlyphText.Text = recording ? "■" : "●";
        GlyphText.Foreground = recording
            ? new SolidColorBrush(Color.FromRgb(0xFF, 0x9E, 0x9E))
            : new SolidColorBrush(Color.FromRgb(0x9A, 0xA4, 0xAD));

        var elapsed = TimeSpan.FromSeconds(_recorder.ElapsedSec);
        TimeText.Text = recording ? $"{elapsed.Minutes:00}:{elapsed.Seconds:00}" : "--:--";

        StatusUnder.Text = recording ? "录制中" : "未录制";
        StatusUnder.Foreground = recording
            ? new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B))
            : new SolidColorBrush(Color.FromRgb(0x9A, 0xA4, 0xAD));

        Ball.Stroke = recording
            ? new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0x5A, 0x5A))
            : new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        _origin = PointToScreen(e.GetPosition(this));
        _startLeft = Left;
        _startTop = Top;
        _dragging = true;
        _moved = false;
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_dragging)
        {
            return;
        }

        var current = PointToScreen(e.GetPosition(this));
        var dpi = VisualTreeHelper.GetDpi(this);
        var dx = (current.X - _origin.X) / dpi.DpiScaleX;
        var dy = (current.Y - _origin.Y) / dpi.DpiScaleY;

        if (Math.Abs(dx) + Math.Abs(dy) > 3)
        {
            _moved = true;
        }

        Left = _startLeft + dx;
        Top = _startTop + dy;
        _config.Activity.FloatX = Left;
        _config.Activity.FloatY = Top;
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        ReleaseMouseCapture();
        e.Handled = true;

        if (_moved)
        {
            _config.Activity.FloatX = Left;
            _config.Activity.FloatY = Top;
            return;
        }

        _recorder.Toggle();
        UpdateState();
    }
}
