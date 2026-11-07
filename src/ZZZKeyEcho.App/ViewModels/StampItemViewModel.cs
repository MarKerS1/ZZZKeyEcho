using CommunityToolkit.Mvvm.ComponentModel;
using ZZZKeyEcho.App.Stamps;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Stamps;

namespace ZZZKeyEcho.App.ViewModels;

public sealed class StampItemViewModel : ObservableObject
{
    private readonly StampConfig _stamp;
    private readonly Action _changed;

    public StampItemViewModel(StampConfig stamp, int index, Action changed)
    {
        _stamp = stamp;
        Index = index;
        _changed = changed;
    }

    public int Index { get; }

    public string DisplayName => $"源{Index + 1} → 映{Index + 1}";

    public string SizeText =>
        $"{_stamp.SourceWidth}×{_stamp.SourceHeight}  →  {StampGeometry.OutputWidth(_stamp)}×{StampGeometry.OutputHeight(_stamp)}";

    public double Scale
    {
        get => _stamp.Scale;
        set
        {
            var clamped = Math.Clamp(value, 0.25, 4.0);

            if (Math.Abs(_stamp.Scale - clamped) < 0.0001)
            {
                return;
            }

            var oldWidth = StampGeometry.OutputWidth(_stamp);
            var oldHeight = StampGeometry.OutputHeight(_stamp);
            _stamp.Scale = clamped;
            var resized = StampGeometry.ResizeAroundCenter(
                _stamp.OutputX,
                _stamp.OutputY,
                oldWidth,
                oldHeight,
                StampGeometry.OutputWidth(_stamp),
                StampGeometry.OutputHeight(_stamp));
            var bounds = MonitorHelper.GetMonitorAt(_stamp.OutputX + 1, _stamp.OutputY + 1).Bounds;
            var clampedRect = StampGeometry.Clamp(resized, bounds);
            _stamp.OutputX = clampedRect.X;
            _stamp.OutputY = clampedRect.Y;

            OnPropertyChanged();
            OnPropertyChanged(nameof(SizeText));
            _changed();
        }
    }

    public double BorderThickness
    {
        get => _stamp.BorderThickness;
        set => SetNumber(value, () => _stamp.BorderThickness, v => _stamp.BorderThickness = Math.Clamp(v, 0, 20));
    }

    public double BorderOpacity
    {
        get => _stamp.BorderOpacity;
        set => SetNumber(value, () => _stamp.BorderOpacity, v => _stamp.BorderOpacity = Math.Clamp(v, 0, 1));
    }

    public double ContentOpacity
    {
        get => _stamp.ContentOpacity;
        set => SetNumber(value, () => _stamp.ContentOpacity, v => _stamp.ContentOpacity = Math.Clamp(v, 0.1, 1));
    }

    public string BorderColor
    {
        get => _stamp.BorderColor;
        set
        {
            if (_stamp.BorderColor == value)
            {
                return;
            }

            _stamp.BorderColor = string.IsNullOrWhiteSpace(value) ? "#FFFFFF" : value;
            OnPropertyChanged();
            _changed();
        }
    }

    public double CornerRadius
    {
        get => _stamp.CornerRadius;
        set => SetNumber(value, () => _stamp.CornerRadius, v => _stamp.CornerRadius = Math.Clamp(v, 0, 48));
    }

    public double Padding
    {
        get => _stamp.Padding;
        set => SetNumber(value, () => _stamp.Padding, v => _stamp.Padding = Math.Clamp(v, 0, 48));
    }

    public bool HideLocal
    {
        get => _stamp.HideLocal;
        set
        {
            if (_stamp.HideLocal == value)
            {
                return;
            }

            _stamp.HideLocal = value;
            OnPropertyChanged();
            _changed();
        }
    }

    public bool WebEnabled
    {
        get => _stamp.WebEnabled;
        set
        {
            if (_stamp.WebEnabled == value)
            {
                return;
            }

            _stamp.WebEnabled = value;
            OnPropertyChanged();
            _changed();
        }
    }

    public void ApplyAppearanceFrom(StampItemViewModel source)
    {
        _stamp.Scale = source.Scale;
        _stamp.BorderThickness = source.BorderThickness;
        _stamp.BorderOpacity = source.BorderOpacity;
        _stamp.ContentOpacity = source.ContentOpacity;
        _stamp.BorderColor = source.BorderColor;
        _stamp.CornerRadius = source.CornerRadius;
        _stamp.Padding = source.Padding;
        OnPropertyChanged(string.Empty);
        _changed();
    }

    public void SaveTo(StampConfig target)
    {
        target.Scale = _stamp.Scale;
        target.BorderThickness = _stamp.BorderThickness;
        target.BorderOpacity = _stamp.BorderOpacity;
        target.ContentOpacity = _stamp.ContentOpacity;
        target.BorderColor = _stamp.BorderColor;
        target.CornerRadius = _stamp.CornerRadius;
        target.Padding = _stamp.Padding;
        target.HideLocal = _stamp.HideLocal;
        target.WebEnabled = _stamp.WebEnabled;
    }

    private void SetNumber(double value, Func<double> getter, Action<double> setter)
    {
        var clamped = value;

        if (Math.Abs(getter() - clamped) < 0.0001)
        {
            return;
        }

        setter(clamped);
        OnPropertyChanged();
        _changed();
    }
}
