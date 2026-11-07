using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Stamps;
using ZZZKeyEcho.Core.Win32;

namespace ZZZKeyEcho.App.Stamps;

public partial class StampWindow : Window
{
    private readonly AppConfig _config;
    private StampConfig _stamp;
    private double _scale = 1.0;

    public StampWindow(AppConfig config, StampConfig stamp)
    {
        _config = config;
        _stamp = stamp;

        InitializeComponent();
        SizeChanged += (_, _) => UpdateClip();
        ApplyConfiguration();
    }

    public StampConfig Stamp => _stamp;

    public void UpdateStamp(StampConfig stamp)
    {
        _stamp = stamp;
        ApplyConfiguration();
    }

    public void ApplyConfiguration()
    {
        var monitor = MonitorHelper.GetMonitorAt(_stamp.OutputX + 1, _stamp.OutputY + 1);
        _scale = monitor.Scale <= 0 ? 1.0 : monitor.Scale;

        var width = StampGeometry.OutputWidth(_stamp);
        var height = StampGeometry.OutputHeight(_stamp);
        var border = Math.Max(0, _stamp.BorderThickness);
        var padding = Math.Max(0, _stamp.Padding);
        var inset = border + padding;
        var outerWidth = width + (inset * 2);
        var outerHeight = height + (inset * 2);

        Width = MonitorHelper.PhysicalToDip(outerWidth, _scale);
        Height = MonitorHelper.PhysicalToDip(outerHeight, _scale);
        Left = MonitorHelper.PhysicalToDip(_stamp.OutputX - inset, _scale);
        Top = MonitorHelper.PhysicalToDip(_stamp.OutputY - inset, _scale);

        var color = ParseColor(_stamp.BorderColor, Colors.White);
        var brush = new SolidColorBrush(color)
        {
            Opacity = Math.Clamp(_stamp.BorderOpacity, 0, 1),
        };
        brush.Freeze();

        RootBorder.BorderBrush = brush;
        RootBorder.BorderThickness = new Thickness(
            MonitorHelper.PhysicalToDip(border, _scale));
        RootBorder.Padding = new Thickness(
            MonitorHelper.PhysicalToDip(padding, _scale));
        RootBorder.CornerRadius = new CornerRadius(
            MonitorHelper.PhysicalToDip(Math.Max(0, _stamp.CornerRadius), _scale));
        FrameImage.Opacity = Math.Clamp(_stamp.ContentOpacity, 0, 1);
        OverlayWindowHelper.SetTopmost(this);

        UpdateClip();
    }

    public void SetFrame(BitmapSource frame)
    {
        FrameImage.Source = frame;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        OverlayWindowHelper.Attach(this, true);

        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
        {
            MonitorHelper.SetWindowDisplayAffinity(handle, MonitorHelper.WdaExcludeFromCapture);
        }
    }

    private void UpdateClip()
    {
        if (FrameImage.ActualWidth <= 0 || FrameImage.ActualHeight <= 0)
        {
            return;
        }

        var radius = Math.Max(0, _stamp.CornerRadius);
        var innerRadius = Math.Max(0, radius - Math.Max(0, _stamp.BorderThickness) - Math.Max(0, _stamp.Padding));
        FrameImage.Clip = new RectangleGeometry(
            new Rect(0, 0, FrameImage.ActualWidth, FrameImage.ActualHeight),
            MonitorHelper.PhysicalToDip(innerRadius, _scale),
            MonitorHelper.PhysicalToDip(innerRadius, _scale));
    }

    private static Color ParseColor(string value, Color fallback)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(value);
        }
        catch (Exception)
        {
            return fallback;
        }
    }
}
