using System.Windows;
using ZZZKeyEcho.Core.Config;

namespace ZZZKeyEcho.Core.Stamps;

public static class StampGeometry
{
    public const int MinimumSize = 16;
    public const double MinimumScale = 0.25;
    public const double MaximumScale = 4.0;

    public static Int32Rect Normalize(int x1, int y1, int x2, int y2)
    {
        var left = Math.Min(x1, x2);
        var top = Math.Min(y1, y2);

        return new Int32Rect(left, top, Math.Abs(x2 - x1), Math.Abs(y2 - y1));
    }

    public static bool IsValid(Int32Rect rect) =>
        rect.Width >= MinimumSize && rect.Height >= MinimumSize;

    public static Int32Rect Clamp(Int32Rect rect, Int32Rect bounds)
    {
        var width = Math.Min(Math.Max(1, rect.Width), Math.Max(1, bounds.Width));
        var height = Math.Min(Math.Max(1, rect.Height), Math.Max(1, bounds.Height));
        var x = Math.Clamp(rect.X, bounds.X, bounds.X + Math.Max(0, bounds.Width - width));
        var y = Math.Clamp(rect.Y, bounds.Y, bounds.Y + Math.Max(0, bounds.Height - height));

        return new Int32Rect(x, y, width, height);
    }

    public static int OutputWidth(StampConfig stamp) =>
        Math.Max(MinimumSize, (int)Math.Round(stamp.SourceWidth * Math.Clamp(stamp.Scale, MinimumScale, MaximumScale)));

    public static int OutputHeight(StampConfig stamp) =>
        Math.Max(MinimumSize, (int)Math.Round(stamp.SourceHeight * Math.Clamp(stamp.Scale, MinimumScale, MaximumScale)));

    public static Int32Rect OutputRect(StampConfig stamp) =>
        new(stamp.OutputX, stamp.OutputY, OutputWidth(stamp), OutputHeight(stamp));

    public static Int32Rect ResizeAroundCenter(
        int x,
        int y,
        int oldWidth,
        int oldHeight,
        int newWidth,
        int newHeight)
    {
        var centerX = x + (oldWidth / 2.0);
        var centerY = y + (oldHeight / 2.0);

        return new Int32Rect(
            (int)Math.Round(centerX - (newWidth / 2.0)),
            (int)Math.Round(centerY - (newHeight / 2.0)),
            newWidth,
            newHeight);
    }

    public static bool Contains(Int32Rect rect, int x, int y) =>
        x >= rect.X && x < rect.X + rect.Width && y >= rect.Y && y < rect.Y + rect.Height;

    public static Int32Rect CenterIn(Int32Rect bounds, int width, int height, int offsetIndex = 0)
    {
        var offset = Math.Max(0, offsetIndex) * 24;
        var x = bounds.X + ((bounds.Width - width) / 2) + offset;
        var y = bounds.Y + ((bounds.Height - height) / 2) + offset;

        return Clamp(new Int32Rect(x, y, width, height), bounds);
    }

    public static StampConfig CreatePair(
        Int32Rect source,
        Int32Rect bounds,
        int existingCount,
        IReadOnlyList<StampConfig>? reference = null)
    {
        var width = Math.Max(MinimumSize, source.Width);
        var height = Math.Max(MinimumSize, source.Height);
        var output = CenterIn(bounds, width, height, existingCount);

        var stamp = new StampConfig
        {
            SourceX = source.X,
            SourceY = source.Y,
            SourceWidth = source.Width,
            SourceHeight = source.Height,
            OutputX = output.X,
            OutputY = output.Y,
        };

        if (reference is { Count: > 0 })
        {
            var last = reference[^1];
            stamp.BorderThickness = last.BorderThickness;
            stamp.BorderOpacity = last.BorderOpacity;
            stamp.BorderColor = last.BorderColor;
            stamp.CornerRadius = last.CornerRadius;
            stamp.Padding = last.Padding;
        }

        return stamp;
    }
}
