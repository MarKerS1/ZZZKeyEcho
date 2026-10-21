using System.Windows;
using ZZZKeyEcho.App.Updates;
using ZZZKeyEcho.Core.Update;

namespace ZZZKeyEcho.App.Views;

public partial class SettingsWindow
{
    private DateTime _lastUpdateCheck = DateTime.MinValue;
    private bool _checkingUpdate;

    private async void OnCheckUpdateClick(object sender, RoutedEventArgs e)
    {
        if (_checkingUpdate || (DateTime.UtcNow - _lastUpdateCheck).TotalSeconds < 3)
        {
            return;
        }

        _checkingUpdate = true;
        _lastUpdateCheck = DateTime.UtcNow;
        CheckUpdateButton.IsEnabled = false;
        UpdateStatusText.Text = "检测中…（最多 10 秒）";

        try
        {
            var result = await Task.Run(() => UpdateChecker.CheckAsync(TimeSpan.FromSeconds(10)));

            switch (result.Status)
            {
                case UpdateCheckStatus.UpdateAvailable when result.Update is not null:
                    UpdateStatusText.Text = $"发现新版本 v{result.Update.LatestVersion}";
                    UpdatePrompt.Show(this, result.Update);
                    break;
                case UpdateCheckStatus.UpToDate:
                    UpdateStatusText.Text = $"已是最新版本 v{result.CurrentVersion}";
                    MessageBox.Show(
                        this,
                        $"当前版本 v{result.CurrentVersion}，GitHub 上没有更新的发布。",
                        "检查更新",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    break;
                default:
                    UpdateStatusText.Text = "检测失败：无法连接 GitHub";
                    MessageBox.Show(
                        this,
                        "无法连接 GitHub，本次检测没有完成，不能据此判断是否已是最新版本。\n\n" +
                        "请检查网络（或代理 / VPN）后重试。",
                        "检查更新失败",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    break;
            }
        }
        finally
        {
            _checkingUpdate = false;
            CheckUpdateButton.IsEnabled = true;
        }
    }
}
