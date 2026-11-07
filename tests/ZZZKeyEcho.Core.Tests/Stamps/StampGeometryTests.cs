using System.Windows;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Stamps;

namespace ZZZKeyEcho.Core.Tests.Stamps;

public class StampGeometryTests
{
    [Fact]
    public void Normalize_handles_reverse_drag()
    {
        var rect = StampGeometry.Normalize(120, 80, 20, 10);

        Assert.Equal(new Int32Rect(20, 10, 100, 70), rect);
    }

    [Fact]
    public void Small_rect_is_invalid()
    {
        Assert.False(StampGeometry.IsValid(new Int32Rect(0, 0, 15, 40)));
        Assert.True(StampGeometry.IsValid(new Int32Rect(0, 0, 16, 16)));
    }

    [Fact]
    public void Output_size_follows_scale()
    {
        var stamp = new StampConfig { SourceWidth = 200, SourceHeight = 100, Scale = 1.5 };

        Assert.Equal(300, StampGeometry.OutputWidth(stamp));
        Assert.Equal(150, StampGeometry.OutputHeight(stamp));
    }

    [Fact]
    public void Resize_around_center_keeps_center()
    {
        var resized = StampGeometry.ResizeAroundCenter(100, 100, 200, 100, 300, 150);

        Assert.Equal(new Int32Rect(50, 75, 300, 150), resized);
    }

    [Fact]
    public void Clamp_keeps_rect_inside_bounds()
    {
        var bounds = new Int32Rect(0, 0, 1920, 1080);
        var clamped = StampGeometry.Clamp(new Int32Rect(1900, -20, 100, 50), bounds);

        Assert.Equal(new Int32Rect(1820, 0, 100, 50), clamped);
    }

    [Fact]
    public void Create_pair_uses_reference_appearance_and_cascades_output()
    {
        var bounds = new Int32Rect(0, 0, 1920, 1080);
        var reference = new List<StampConfig>
        {
            new()
            {
                BorderThickness = 4,
                BorderOpacity = 0.5,
                BorderColor = "#FF0000",
                CornerRadius = 12,
                Padding = 6,
            },
        };

        var pair = StampGeometry.CreatePair(new Int32Rect(10, 20, 300, 200), bounds, 1, reference);

        Assert.Equal(0.5, pair.BorderOpacity);
        Assert.Equal("#FF0000", pair.BorderColor);
        Assert.Equal(4, pair.BorderThickness);
        Assert.Equal(200, pair.SourceHeight);
        Assert.True(pair.OutputX > bounds.X);
    }
}
