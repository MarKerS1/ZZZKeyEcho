using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Engine;

namespace ZZZKeyEcho.App.Rendering;

public sealed class IconProvider
{
    private const int CompositeSize = 128;
    private const double BadgeScale = 0.45;

    private readonly Dictionary<string, (DateTime Stamp, ImageSource? Image)> _fileCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (DateTime Stamp, ImageSource? Image)> _compositeCache = new(StringComparer.Ordinal);
    private readonly string _iconsDirectory;
    private readonly string _customDirectory;
    private readonly AppConfig _config;
    private ImageSource? _neutralIcon;

    public IconProvider(string iconsDirectory, AppConfig config)
    {
        _iconsDirectory = iconsDirectory;
        _customDirectory = Path.Combine(iconsDirectory, "custom");
        _config = config;
    }

    public string CustomDirectory => _customDirectory;

    public ImageSource? GetActionIcon(string actionId) => ResolveActionIcon(actionId);

    public ImageSource? GetDirectionIcon(Direction direction) => direction switch
    {
        Direction.UpLeft => GetActionIcon("dirUpLeft"),
        Direction.UpRight => GetActionIcon("dirUpRight"),
        Direction.DownLeft => GetActionIcon("dirDownLeft"),
        Direction.DownRight => GetActionIcon("dirDownRight"),
        Direction.Rotate => GetActionIcon("dirRotate"),
        _ => GetActionIcon(MovementBindings.ToActionId(direction)),
    };

    private ImageSource? GetRotated(string fileName, double angle)
    {
        var key = fileName + "@" + angle;

        if (_compositeCache.TryGetValue(key, out var cached))
        {
            return cached.Image;
        }

        if (LoadFile(Path.Combine(_iconsDirectory, fileName)) is not { } source)
        {
            return null;
        }

        var width = source.Width;
        var height = source.Height;
        var longest = Math.Max(width, height);
        const double boost = 1.3;
        var size = (int)Math.Ceiling(longest * boost);
        var scale = size / longest;
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            context.PushTransform(new RotateTransform(angle, size / 2.0, size / 2.0));
            context.DrawImage(
                source,
                new Rect((size - width * scale) / 2, (size - height * scale) / 2, width * scale, height * scale));
            context.Pop();
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        _compositeCache[key] = (DateTime.MinValue, bitmap);
        return bitmap;
    }

    public void EnsureDerivedIconFiles()
    {
        SaveIfMissing("dir_up_left.png", GetRotated("dir_up.png", -45));
        SaveIfMissing("dir_up_right.png", GetRotated("dir_up.png", 45));
        SaveIfMissing("dir_down_left.png", GetRotated("dir_down.png", 45));
        SaveIfMissing("dir_down_right.png", GetRotated("dir_down.png", -45));
        SaveIfMissing("dir_rotate.png", GetRotateIcon());
    }

    private void SaveIfMissing(string fileName, ImageSource? image)
    {
        var path = Path.Combine(_iconsDirectory, fileName);

        if (image is not BitmapSource bitmap || File.Exists(path))
        {
            return;
        }

        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var stream = File.Create(path);
            encoder.Save(stream);
        }
        catch (Exception)
        {
            // 目录只读时忽略：界面继续使用内存中生成的图形。
        }
    }

    private ImageSource GetRotateIcon()
    {
        if (_compositeCache.TryGetValue("rotate", out var cached) && cached.Image is { } existing)
        {
            return existing;
        }

        const int size = 96;
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            var brush = new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2));
            brush.Freeze();
            var pen = new Pen(brush, 9)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
            };
            pen.Freeze();

            var center = new Point(size / 2.0, size / 2.0);
            var radius = size / 2.0 - 10;
            var start = PointOnCircle(center, radius, -70);
            var end = PointOnCircle(center, radius, 200);

            var arc = new StreamGeometry();

            using (var geometryContext = arc.Open())
            {
                geometryContext.BeginFigure(start, false, false);
                geometryContext.ArcTo(end, new Size(radius, radius), 0, true, SweepDirection.Clockwise, true, false);
            }

            arc.Freeze();
            context.DrawGeometry(null, pen, arc);

            var tip = PointOnCircle(center, radius, 208);
            var head = new StreamGeometry();

            using (var headContext = head.Open())
            {
                headContext.BeginFigure(tip, true, true);
                headContext.LineTo(PointOnCircle(tip, 13, 140), true, false);
                headContext.LineTo(PointOnCircle(tip, 13, -140), true, false);
            }

            head.Freeze();
            context.DrawGeometry(brush, null, head);
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        _compositeCache["rotate"] = (DateTime.MinValue, bitmap);
        return _compositeCache["rotate"].Image!;
    }

    private static Point PointOnCircle(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180.0;
        return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
    }

    public string OverridePathFor(string actionId) => Path.Combine(_customDirectory, actionId + ".png");

    public ImageSource GetNeutralIcon()
    {
        if (_neutralIcon is not null)
        {
            return _neutralIcon;
        }

        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            var center = new Point(CompositeSize / 2.0, CompositeSize / 2.0);
            var radius = CompositeSize / 2.0 - 4;

            var fill = new SolidColorBrush(Color.FromRgb(0x17, 0x17, 0x1B));
            fill.Freeze();
            var outline = new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2));
            outline.Freeze();

            context.DrawEllipse(fill, new Pen(outline, 4), center, radius, radius);

            var typeface = new Typeface(
                new FontFamily("Segoe UI"),
                FontStyles.Normal,
                FontWeights.Bold,
                FontStretches.Normal);

            var formatted = new FormattedText(
                "N",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                64,
                Brushes.White,
                1.0);

            context.DrawText(formatted, new Point(center.X - formatted.Width / 2, center.Y - formatted.Height / 2));
        }

        var bitmap = new RenderTargetBitmap(CompositeSize, CompositeSize, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        _neutralIcon = bitmap;
        return _neutralIcon;
    }

    public void Invalidate(string actionId)
    {
        _compositeCache.Remove(actionId);
        _fileCache.Clear();
    }

    private ImageSource? ResolveActionIcon(string actionId)
    {
        if (_config.IconOverrides.TryGetValue(actionId, out var overridePath)
            && LoadFile(ResolvePath(overridePath)) is { } overridden)
        {
            return overridden;
        }

        if (LoadFile(OverridePathFor(actionId)) is { } custom)
        {
            return custom;
        }

        return actionId switch
        {
            ActionCatalog.Attack => LoadFile(Path.Combine(_iconsDirectory, "attack.png")),
            ActionCatalog.Special => LoadFile(Path.Combine(_iconsDirectory, "special.png")),
            ActionCatalog.Dodge => LoadFile(Path.Combine(_iconsDirectory, "dodge.png")),
            ActionCatalog.Ultimate => LoadFile(Path.Combine(_iconsDirectory, "ultimate.png")),
            ActionCatalog.Assist => LoadFile(Path.Combine(_iconsDirectory, "assist.png")),
            ActionCatalog.LockOn => LoadFile(Path.Combine(_iconsDirectory, "lock_on.png")),
            ActionCatalog.AgentSkill => LoadFile(Path.Combine(_iconsDirectory, "agent_skill.png")),
            ActionCatalog.DirUp => LoadFile(Path.Combine(_iconsDirectory, "dir_up.png")),
            ActionCatalog.DirDown => LoadFile(Path.Combine(_iconsDirectory, "dir_down.png")),
            ActionCatalog.DirLeft => LoadFile(Path.Combine(_iconsDirectory, "dir_left.png")),
            ActionCatalog.DirRight => LoadFile(Path.Combine(_iconsDirectory, "dir_right.png")),
            "dirUpLeft" => LoadFile(Path.Combine(_iconsDirectory, "dir_up_left.png")) ?? GetRotated("dir_up.png", -45),
            "dirUpRight" => LoadFile(Path.Combine(_iconsDirectory, "dir_up_right.png")) ?? GetRotated("dir_up.png", 45),
            "dirDownLeft" => LoadFile(Path.Combine(_iconsDirectory, "dir_down_left.png")) ?? GetRotated("dir_down.png", 45),
            "dirDownRight" => LoadFile(Path.Combine(_iconsDirectory, "dir_down_right.png")) ?? GetRotated("dir_down.png", -45),
            "dirRotate" => LoadFile(Path.Combine(_iconsDirectory, "dir_rotate.png")) ?? GetRotateIcon(),
            ActionCatalog.SwitchPrev or ActionCatalog.SwitchNext
                or ActionCatalog.ChainLeft or ActionCatalog.ChainRight or ActionCatalog.ChainCancel
                => GetComposite(actionId),
            _ => null,
        };
    }

    private ImageSource? GetComposite(string actionId)
    {
        var badgeFile = actionId switch
        {
            ActionCatalog.SwitchPrev => "dir_up.png",
            ActionCatalog.SwitchNext => "dir_down.png",
            ActionCatalog.ChainLeft => "dir_left.png",
            ActionCatalog.ChainRight => "dir_right.png",
            _ => null,
        };

        var basePath = Path.Combine(_iconsDirectory, "assist.png");
        var stamp = File.GetLastWriteTimeUtc(basePath);

        if (badgeFile is not null)
        {
            stamp = stamp > File.GetLastWriteTimeUtc(Path.Combine(_iconsDirectory, badgeFile))
                ? stamp
                : File.GetLastWriteTimeUtc(Path.Combine(_iconsDirectory, badgeFile));
        }

        if (_compositeCache.TryGetValue(actionId, out var cached) && cached.Stamp == stamp)
        {
            return cached.Image;
        }

        var image = BuildComposite(actionId, basePath, badgeFile);
        _compositeCache[actionId] = (stamp, image);
        return image;
    }

    private static ImageSource? BuildComposite(string actionId, string basePath, string? badgeFile)
    {
        if (!File.Exists(basePath))
        {
            return null;
        }

        var baseImage = LoadBitmap(basePath);
        if (baseImage is null)
        {
            return null;
        }

        var badgeBox = CompositeSize * BadgeScale;
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            context.DrawImage(baseImage, new Rect(0, 0, CompositeSize, CompositeSize));

            switch (actionId)
            {
                case ActionCatalog.SwitchPrev:
                    DrawBadge(context, badgeFile, new Point(CompositeSize - badgeBox, 0), badgeBox);
                    break;
                case ActionCatalog.SwitchNext:
                    DrawBadge(context, badgeFile, new Point(CompositeSize - badgeBox, CompositeSize - badgeBox), badgeBox);
                    break;
                case ActionCatalog.ChainLeft:
                    DrawBadge(context, badgeFile, new Point(0, (CompositeSize - badgeBox) / 2), badgeBox);
                    break;
                case ActionCatalog.ChainRight:
                    DrawBadge(context, badgeFile, new Point(CompositeSize - badgeBox, (CompositeSize - badgeBox) / 2), badgeBox);
                    break;
                case ActionCatalog.ChainCancel:
                    DrawCross(context);
                    break;
            }
        }

        var bitmap = new RenderTargetBitmap(CompositeSize, CompositeSize, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static void DrawBadge(DrawingContext context, string? badgeFile, Point boxTopLeft, double box)
    {
        if (badgeFile is null)
        {
            return;
        }

        var path = Path.Combine(AppContext.BaseDirectory, "assets", "icons", badgeFile);
        var badge = LoadBitmap(path);

        if (badge is null)
        {
            return;
        }

        var scale = Math.Min(box / badge.Width, box / badge.Height);
        var width = badge.Width * scale;
        var height = badge.Height * scale;

        context.DrawImage(
            badge,
            new Rect(
                boxTopLeft.X + (box - width) / 2,
                boxTopLeft.Y + (box - height) / 2,
                width,
                height));
    }

    private static void DrawCross(DrawingContext context)
    {
        var brush = new SolidColorBrush(Color.FromRgb(0xE5, 0x48, 0x4D));
        brush.Freeze();
        var pen = new Pen(brush, CompositeSize * 0.12)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
        };
        pen.Freeze();

        var margin = CompositeSize * 0.26;
        context.DrawLine(pen, new Point(margin, margin), new Point(CompositeSize - margin, CompositeSize - margin));
        context.DrawLine(pen, new Point(CompositeSize - margin, margin), new Point(margin, CompositeSize - margin));
    }

    private static string ResolvePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    private ImageSource? LoadFile(string path)
    {
        var stamp = File.GetLastWriteTimeUtc(path);

        if (_fileCache.TryGetValue(path, out var cached) && cached.Stamp == stamp)
        {
            return cached.Image;
        }

        var image = stamp == default ? null : LoadBitmap(path);
        _fileCache[path] = (stamp, image);
        return image;
    }

    private static ImageSource? LoadBitmap(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
