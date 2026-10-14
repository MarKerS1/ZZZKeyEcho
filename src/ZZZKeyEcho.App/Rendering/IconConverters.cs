using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ZZZKeyEcho.Core.Engine;

namespace ZZZKeyEcho.App.Rendering;

public sealed record IconItem(ImageSource Image, double Size);

public sealed class MovementIconsConverter : IValueConverter, IMultiValueConverter
{
    public IconProvider? Provider { get; set; }

    public double IconSize { get; set; } = 48;

    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        Convert(values.Length > 0 ? values[0] : null, targetType, parameter, culture);

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not InputRecord record || Provider is null)
        {
            return Array.Empty<IconItem>();
        }

        if (record.IsNeutral)
        {
            return new[] { new IconItem(Provider.GetNeutralIcon(), IconSize) };
        }

        var items = new List<IconItem>(record.Movement.Count);

        foreach (var direction in record.Movement)
        {
            if (Provider.GetDirectionIcon(direction) is { } image)
            {
                items.Add(new IconItem(image, IconSize));
            }
        }

        return items;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class ActionIconsConverter : IValueConverter
{
    private const double SecondaryScale = 0.62;

    public IconProvider? Provider { get; set; }

    public double IconSize { get; set; } = 48;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IReadOnlyList<string> actions || Provider is null)
        {
            return Array.Empty<IconItem>();
        }

        var items = new List<IconItem>(actions.Count);

        for (var index = 0; index < actions.Count; index++)
        {
            if (Provider.GetActionIcon(actions[index]) is { } image)
            {
                items.Add(new IconItem(image, index == 0 ? IconSize : IconSize * SecondaryScale));
            }
        }

        return items;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
