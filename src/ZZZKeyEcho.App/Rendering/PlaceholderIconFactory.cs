using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ZZZKeyEcho.App.Rendering;

public static class PlaceholderIconFactory
{
    public const int Size = 128;

    public static readonly IReadOnlyList<(string FileName, string Text)> Placeholders = new[]
    {
        ("lock_on.png", "锁定"),
        ("agent_skill.png", "能力"),
    };

    public static BitmapSource Create(string text)
    {
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            var center = new Point(Size / 2.0, Size / 2.0);
            var radius = Size / 2.0 - 4;

            var fill = new SolidColorBrush(Color.FromRgb(0x17, 0x17, 0x1B));
            fill.Freeze();
            var outline = new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2));
            outline.Freeze();

            context.DrawEllipse(fill, new Pen(outline, 4), center, radius, radius);

            var typeface = new Typeface(
                new FontFamily("Microsoft YaHei, Segoe UI"),
                FontStyles.Normal,
                FontWeights.Bold,
                FontStretches.Normal);

            var formatted = new FormattedText(
                text,
                CultureInfo.GetCultureInfo("zh-CN"),
                FlowDirection.LeftToRight,
                typeface,
                44,
                Brushes.White,
                1.0);

            context.DrawText(formatted, new Point(center.X - formatted.Width / 2, center.Y - formatted.Height / 2));
        }

        var bitmap = new RenderTargetBitmap(Size, Size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    public static void WritePng(string path, string text)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(Create(text)));

        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    public static void EnsureAll(string iconsDirectory)
    {
        foreach (var (fileName, text) in Placeholders)
        {
            var path = Path.Combine(iconsDirectory, fileName);
            if (!File.Exists(path))
            {
                WritePng(path, text);
            }
        }
    }
}
