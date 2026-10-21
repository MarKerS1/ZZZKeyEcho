using System.Diagnostics;
using System.Windows;
using ZZZKeyEcho.Core.Update;

namespace ZZZKeyEcho.App.Updates;

public static class UpdatePrompt
{
    public static void Show(Window? owner, UpdateInfo info)
    {
        var text =
            "ZZZKeyEcho 存在版本更新\n\n" +
            $"当前版本：v{info.CurrentVersion}\n" +
            $"最新版本：v{info.LatestVersion}\n\n" +
            "请于 Bilibili 或抖音搜索「老鼠色的猫」获取下载地址，" +
            $"或进入 GitHub 下载：\n{info.Url}\n\n" +
            "是否现在打开 GitHub 发布页？";

        var result = owner is null
            ? MessageBox.Show(text, "ZZZKeyEcho 更新", MessageBoxButton.YesNo, MessageBoxImage.Information)
            : MessageBox.Show(owner, text, "ZZZKeyEcho 更新", MessageBoxButton.YesNo, MessageBoxImage.Information);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(info.Url) { UseShellExecute = true });
        }
        catch (Exception)
        {
        }
    }
}
