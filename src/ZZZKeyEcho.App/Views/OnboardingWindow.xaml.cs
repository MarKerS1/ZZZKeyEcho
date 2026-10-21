using System.Windows;
using ZZZKeyEcho.Core.Config;

namespace ZZZKeyEcho.App.Views;

public partial class OnboardingWindow : Window
{
    public OnboardingWindow(AppConfig config)
    {
        InitializeComponent();

        HotkeyText.Text =
            $"· 开始 / 结束操作录制：{Display(config.Hotkeys.ToggleRecording)}\n" +
            $"· 清除悬浮窗记录：{Display(config.Hotkeys.ClearRecords)}\n" +
            $"· 显示 / 隐藏悬浮窗：{Display(config.Hotkeys.ToggleOverlay)}\n" +
            $"· 锁定 / 解锁悬浮窗：{Display(config.Hotkeys.ToggleLock)}\n" +
            $"· 打开 / 关闭设置：{Display(config.Hotkeys.OpenSettings)}\n" +
            $"· 调试模式：{Display(config.Hotkeys.DebugMode)}";
    }

    private static string Display(string hotkey) =>
        string.IsNullOrWhiteSpace(hotkey) ? "（未设置）" : hotkey;

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
