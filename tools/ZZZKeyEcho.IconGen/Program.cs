using System.IO;
using ZZZKeyEcho.App.Rendering;

namespace ZZZKeyEcho.IconGen;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var iconsDirectory = args.Length > 0 ? args[0] : FindDefaultIconsDirectory();

        if (iconsDirectory is null)
        {
            Console.Error.WriteLine("未找到 assets/icons 目录：请在仓库根目录运行，或显式传入输出目录。");
            Console.Error.WriteLine("用法: dotnet run --project tools/ZZZKeyEcho.IconGen [输出目录]");
            return 1;
        }

        foreach (var (fileName, text) in PlaceholderIconFactory.Placeholders)
        {
            var path = Path.Combine(iconsDirectory, fileName);
            PlaceholderIconFactory.WritePng(path, text);
            Console.WriteLine($"已生成 {path}");
        }

        return 0;
    }

    private static string? FindDefaultIconsDirectory()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ZZZKeyEcho.sln")))
            {
                return Path.Combine(directory.FullName, "assets", "icons");
            }

            directory = directory.Parent;
        }

        return null;
    }
}
