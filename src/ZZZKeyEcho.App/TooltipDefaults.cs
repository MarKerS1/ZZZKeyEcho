using System.Windows;
using System.Windows.Controls;

namespace ZZZKeyEcho.App;

public static class TooltipDefaults
{
    public const int InitialShowDelayMs = 2000;

    private const int ShowDurationMs = 60000;

    public static void Enable()
    {
        EventManager.RegisterClassHandler(
            typeof(FrameworkElement),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnLoaded));
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.ToolTip is null)
        {
            return;
        }

        ToolTipService.SetInitialShowDelay(element, InitialShowDelayMs);
        ToolTipService.SetShowDuration(element, ShowDurationMs);
    }
}
