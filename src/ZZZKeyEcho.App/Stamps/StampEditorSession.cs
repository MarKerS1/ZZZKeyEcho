using System.Windows;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Stamps;

namespace ZZZKeyEcho.App.Stamps;

public sealed class StampEditorSession
{
    private readonly AppConfig _config;

    public StampEditorSession(AppConfig config, MonitorBounds monitor)
    {
        _config = config;
        Monitor = monitor;
        Items = config.Stamps.Items.Select(item => item.Clone()).ToList();

        var scale = monitor.Scale <= 0 ? 1.0 : monitor.Scale;
        OverlayX = config.Overlay.X * scale;
        OverlayY = config.Overlay.Y * scale;
        OverlayWidth = config.Overlay.Width * scale;
        OverlayHeight = config.Overlay.Height * scale;

        var floatSize = Math.Clamp(config.Activity.FloatSize, 48, 200);
        RecordWidth = floatSize * scale;
        RecordHeight = (floatSize + 26) * scale;
        RecordX = config.Activity.FloatX >= 0
            ? config.Activity.FloatX * scale
            : monitor.WorkArea.X + monitor.WorkArea.Width - RecordWidth - (24 * scale);
        RecordY = config.Activity.FloatY >= 0
            ? config.Activity.FloatY * scale
            : monitor.WorkArea.Y + monitor.WorkArea.Height - RecordHeight - (48 * scale);
    }

    public MonitorBounds Monitor { get; }

    public List<StampConfig> Items { get; }

    public double OverlayX { get; set; }
    public double OverlayY { get; set; }
    public double OverlayWidth { get; }
    public double OverlayHeight { get; }
    public double RecordX { get; set; }
    public double RecordY { get; set; }
    public double RecordWidth { get; }
    public double RecordHeight { get; }

    public bool IsDirty { get; set; }

    public StampConfig Add(Int32Rect source)
    {
        var stamp = StampGeometry.CreatePair(source, Monitor.WorkArea, Items.Count, Items);
        Items.Add(stamp);
        IsDirty = true;
        return stamp;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Items.Count)
        {
            return;
        }

        Items.RemoveAt(index);
        IsDirty = true;
    }

    public void SaveTo(AppConfig config)
    {
        var scale = Monitor.Scale <= 0 ? 1.0 : Monitor.Scale;
        config.Overlay.X = OverlayX / scale;
        config.Overlay.Y = OverlayY / scale;
        config.Activity.FloatX = RecordX / scale;
        config.Activity.FloatY = RecordY / scale;
        config.Stamps.Items = Items.Select(item => item.Clone()).ToList();
        IsDirty = false;
    }
}
