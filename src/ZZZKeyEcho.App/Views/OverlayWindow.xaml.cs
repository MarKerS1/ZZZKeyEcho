using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ZZZKeyEcho.App.Rendering;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Engine;
using ZZZKeyEcho.Core.Input;
using ZZZKeyEcho.Core.Win32;

namespace ZZZKeyEcho.App.Views;

public partial class OverlayWindow : Window
{
    private readonly AppConfig _config;
    private readonly RecordEngine _engine;
    private readonly IClock _clock;
    private readonly IconProvider _icons;
    private readonly DispatcherTimer _liveTimer;
    private Point _dragOrigin;
    private double _dragLeft;
    private double _dragTop;
    private bool _dragging;

    public OverlayWindow(AppConfig config, RecordEngine engine, IClock clock, IconProvider icons)
    {
        _config = config;
        _engine = engine;
        _clock = clock;
        _icons = icons;

        InitializeComponent();
        ConfigureConverters();
        ApplyConfig();

        RecordsHost.ItemsSource = engine.Records;
        engine.Records.CollectionChanged += OnRecordsChanged;
        ApplyRecordOrder();

        _liveTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(100),
        };
        _liveTimer.Tick += (_, _) => RefreshLiveDurations();
        _liveTimer.Start();

        IsVisibleChanged += (_, args) =>
        {
            if (args.NewValue is true)
            {
                Root.BeginAnimation(
                    OpacityProperty,
                    new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120)));
                OverlayCaptureSupport.MakeCapturable(this);
                Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => OverlayCaptureSupport.MakeCapturable(this)));
            }
        };

        Loaded += (_, _) => OverlayCaptureSupport.MakeCapturable(this);

        Closed += (_, _) =>
        {
            _liveTimer.Stop();
            engine.Records.CollectionChanged -= OnRecordsChanged;
        };
    }

    public void SetDebugVisual(bool enabled)
    {
        DebugFrame.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        DebugBadge.Visibility = DebugFrame.Visibility;
    }

    public void SetLocked(bool locked)
    {
        _config.Overlay.Locked = locked;
        OverlayWindowHelper.SetLocked(this, locked);

        UnlockFrame.Visibility = locked ? Visibility.Collapsed : Visibility.Visible;
        ResizeGrip.Visibility = UnlockFrame.Visibility;
    }

    private void OnRootPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_config.Overlay.Locked || IsWithinResizeGrip(e.OriginalSource as DependencyObject))
        {
            return;
        }

        _dragOrigin = PointToScreen(e.GetPosition(this));
        _dragLeft = Left;
        _dragTop = Top;
        _dragging = true;
        Root.CaptureMouse();
        e.Handled = true;
    }

    private void OnRootPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        var current = PointToScreen(e.GetPosition(this));
        var dpi = VisualTreeHelper.GetDpi(this);

        Left = _dragLeft + (current.X - _dragOrigin.X) / dpi.DpiScaleX;
        Top = _dragTop + (current.Y - _dragOrigin.Y) / dpi.DpiScaleY;
        e.Handled = true;
    }

    private void OnRootPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        Root.ReleaseMouseCapture();

        _config.Overlay.X = Left;
        _config.Overlay.Y = Top;
        e.Handled = true;
    }

    private bool IsWithinResizeGrip(DependencyObject? source)
    {
        var current = source;

        while (current is not null)
        {
            if (ReferenceEquals(current, ResizeGrip))
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void OnResizeGripDrag(object sender, DragDeltaEventArgs e)
    {
        Width = Math.Clamp(Width + e.HorizontalChange, 120, 1920);
        Height = Math.Clamp(Height + e.VerticalChange, 120, 2160);

        _config.Overlay.Width = Width;
        _config.Overlay.Height = Height;
    }

    public void ApplyConfiguration() => ApplyConfig();

    public void RefreshRendering()
    {
        ((MovementIconsConverter)FindResource("MovementIcons")).IconSize = _config.Records.IconSize;
        ((ActionIconsConverter)FindResource("ActionIcons")).IconSize = _config.Records.IconSize;
        ((HoldTextConverter)FindResource("HoldText")).Configure(_config.Records);
        ((DurationDockConverter)FindResource("DurationDock")).OnLeft = _config.Records.DurationOnLeft;
        ((DurationDockConverter)FindResource("IconsDock")).OnLeft = !_config.Records.DurationOnLeft;
        ((DurationFlowConverter)FindResource("DurationFlow")).OnLeft = _config.Records.DurationOnLeft;

        ApplyRecordOrder();

        var source = RecordsHost.ItemsSource;
        RecordsHost.ItemsSource = null;
        RecordsHost.ItemsSource = source;
    }

    private void ApplyRecordOrder()
    {
        if (CollectionViewSource.GetDefaultView(_engine.Records) is ListCollectionView view)
        {
            view.CustomSort = _config.Records.NewestAtBottom
                ? null
                : Comparer<object>.Create((left, right) =>
                    ((InputRecord)right).PressedAtMs.CompareTo(((InputRecord)left).PressedAtMs));
        }
    }

    private void ScrollToNewest() =>
        Dispatcher.BeginInvoke(
            new Action(() =>
            {
                if (_config.Records.NewestAtBottom)
                {
                    RecordsScroller.ScrollToEnd();
                }
                else
                {
                    RecordsScroller.ScrollToTop();
                }
            }),
            DispatcherPriority.ContextIdle);

    public void FlashBorder()
    {
        Highlight.Opacity = 1;
        Highlight.BeginAnimation(
            OpacityProperty,
            new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromSeconds(2)));
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        OverlayWindowHelper.Attach(this, _config.Overlay.Locked);
    }

    private void ConfigureConverters()
    {
        var movement = (MovementIconsConverter)FindResource("MovementIcons");
        movement.Provider = _icons;
        movement.IconSize = _config.Records.IconSize;

        var actions = (ActionIconsConverter)FindResource("ActionIcons");
        actions.Provider = _icons;
        actions.IconSize = _config.Records.IconSize;

        ((HoldTextConverter)FindResource("HoldText")).Configure(_config.Records);
        ((DurationDockConverter)FindResource("DurationDock")).OnLeft = _config.Records.DurationOnLeft;
        ((DurationDockConverter)FindResource("IconsDock")).OnLeft = !_config.Records.DurationOnLeft;
        ((DurationFlowConverter)FindResource("DurationFlow")).OnLeft = _config.Records.DurationOnLeft;
    }

    private void OnRecordsChanged(object? sender, NotifyCollectionChangedEventArgs e) => ScrollToNewest();

    private void RefreshLiveDurations()
    {
        var nowMs = _clock.NowMs;
        var fade = _config.Records.Fade;

        foreach (var record in _engine.Records)
        {
            record.RefreshDisplay(nowMs, fade);
        }
    }

    private void ApplyConfig()
    {
        var overlay = _config.Overlay;

        Width = overlay.Width;
        Height = overlay.Height;
        Opacity = overlay.Opacity;

        Left = ClampToVirtualScreen(overlay.X, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width);
        Top = ClampToVirtualScreen(overlay.Y, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height);

        var color = ParseColor(overlay.BgColor);
        var alpha = (byte)Math.Clamp(Math.Round(color.A * Math.Clamp(overlay.BackgroundOpacity, 0, 1)), 1, 255);
        Root.Background = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
        Root.CornerRadius = new CornerRadius(overlay.CornerRadius);
        Root.Padding = new Thickness(overlay.Padding);
        RecordsHost.Opacity = Math.Clamp(overlay.IconOpacity, 0, 1);

        OverlayWindowHelper.SetLocked(this, overlay.Locked);
        OverlayCaptureSupport.MakeCapturable(this);
        UnlockFrame.Visibility = overlay.Locked ? Visibility.Collapsed : Visibility.Visible;
        ResizeGrip.Visibility = UnlockFrame.Visibility;
    }

    private static Color ParseColor(string text)
    {
        try
        {
            if (ColorConverter.ConvertFromString(text) is Color color)
            {
                return color;
            }
        }
        catch (Exception)
        {
        }

        return Color.FromArgb(0xB3, 0x00, 0x00, 0x00);
    }

    private static double ClampToVirtualScreen(double value, double max) =>
        Math.Clamp(
            value,
            SystemParameters.VirtualScreenLeft,
            Math.Max(SystemParameters.VirtualScreenLeft, max));
}
