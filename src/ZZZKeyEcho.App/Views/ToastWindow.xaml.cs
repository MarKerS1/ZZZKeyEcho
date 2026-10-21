using System.Windows;
using System.Windows.Threading;

namespace ZZZKeyEcho.App.Views;

public partial class ToastWindow : Window
{
    private readonly DispatcherTimer _timer;

    public ToastWindow()
    {
        InitializeComponent();

        _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher);
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            Hide();
        };
    }

    public void ShowMessage(string message, double seconds = 3)
    {
        MessageText.Text = message;
        PositionCenter();
        Show();

        _timer.Stop();
        _timer.Interval = TimeSpan.FromSeconds(seconds);
        _timer.Start();
    }

    private void PositionCenter()
    {
        UpdateLayout();

        var area = SystemParameters.WorkArea;
        Left = area.Left + ((area.Width - ActualWidth) / 2);
        Top = area.Top + 80;
    }
}
