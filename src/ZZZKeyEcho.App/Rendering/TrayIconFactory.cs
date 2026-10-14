using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace ZZZKeyEcho.App.Rendering;

public static class TrayIconFactory
{
    public static Icon Create(int size = 32)
    {
        using var bitmap = new Bitmap(size, size);

        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            using var background = new SolidBrush(Color.FromArgb(0x17, 0x17, 0x1B));
            using var accent = new SolidBrush(Color.FromArgb(0xF2, 0xF2, 0xF2));
            using var outlinePen = new Pen(accent, size / 16f);

            var center = size / 2f;
            var radius = size / 2f - 1;
            graphics.FillEllipse(background, center - radius, center - radius, radius * 2, radius * 2);
            graphics.DrawEllipse(outlinePen, center - radius, center - radius, radius * 2, radius * 2);

            using var font = new Font("Microsoft YaHei", size * 0.44f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            graphics.DrawString("键", font, accent, new RectangleF(0, 0, size, size), format);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);
}
