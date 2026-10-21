using System.Windows;

namespace ZZZKeyEcho.App.Views;

public enum AlreadyRunningResult
{
    Close,
    Restart,
}

public partial class AlreadyRunningWindow : Window
{
    public AlreadyRunningWindow(int count)
    {
        InitializeComponent();
        MessageText.Text = $"检测到 {count} 个 ZZZKeyEcho 正在运行。";
    }

    public AlreadyRunningResult Result { get; private set; } = AlreadyRunningResult.Close;

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Result = AlreadyRunningResult.Close;
        DialogResult = false;
    }

    private void OnRestartClick(object sender, RoutedEventArgs e)
    {
        Result = AlreadyRunningResult.Restart;
        DialogResult = true;
    }
}
