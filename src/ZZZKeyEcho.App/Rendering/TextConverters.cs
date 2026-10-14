using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Engine;

namespace ZZZKeyEcho.App.Rendering;

public sealed class HoldTextConverter : IValueConverter
{
    public HoldDisplayMode DisplayMode { get; set; } = HoldDisplayMode.Live;

    public HoldTimeFormat Format { get; set; } = HoldTimeFormat.Seconds;

    public double ThresholdMs { get; set; } = 300;

    public int FramesPerSecond { get; set; } = 60;

    public void Configure(RecordsConfig settings)
    {
        DisplayMode = settings.HoldDisplay;
        Format = settings.HoldFormat;
        ThresholdMs = settings.HoldThresholdMs;
        FramesPerSecond = Math.Max(1, settings.FramesPerSecond);
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double milliseconds || milliseconds <= 0)
        {
            return string.Empty;
        }

        if (DisplayMode == HoldDisplayMode.Off)
        {
            return string.Empty;
        }

        if (DisplayMode == HoldDisplayMode.Threshold && milliseconds < ThresholdMs)
        {
            return string.Empty;
        }

        return Format == HoldTimeFormat.Frames
            ? $"{Math.Round(milliseconds / 1000 * FramesPerSecond)}f"
            : $"{milliseconds / 1000:0.000}s";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class DurationDockConverter : IValueConverter
{
    public bool OnLeft { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        OnLeft ? Dock.Left : Dock.Right;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class DurationFlowConverter : IValueConverter
{
    public bool OnLeft { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        OnLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class CountBadgeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int count && count > 1 ? $"×{count}" : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class PlusVisibilityConverter : IValueConverter, IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        Convert(values.Length > 0 ? values[0] : null, targetType, parameter, culture);

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is InputRecord record && record.Movement.Count > 0 && record.Actions.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
