using System.ComponentModel;

namespace ZZZKeyEcho.Core.Engine;

public sealed class InputRecord : INotifyPropertyChanged
{
    private IReadOnlyList<Direction> _movement;
    private double _durationMs;
    private double _displayDurationMs;
    private double _displayOpacity = 1;
    private double _previousDurationMs;
    private bool _isNeutral;
    private int _movementStamp;
    private int _count = 1;
    private bool _isHolding;

    public InputRecord(
        IReadOnlyList<Direction> movement,
        IReadOnlyList<string> actions,
        string triggerKey,
        double pressedAtMs,
        bool isNeutral = false)
    {
        _movement = movement;
        Actions = actions;
        TriggerKey = triggerKey;
        PressedAtMs = pressedAtMs;
        LastPressAtMs = pressedAtMs;
        _isNeutral = isNeutral;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<Direction> Movement => _movement;

    public IReadOnlyList<string> Actions { get; }

    public string TriggerKey { get; }

    public double PressedAtMs { get; }

    public double LastPressAtMs { get; private set; }

    public bool IsNeutral
    {
        get => _isNeutral;
        private set
        {
            if (_isNeutral == value)
            {
                return;
            }

            _isNeutral = value;
            OnPropertyChanged(nameof(IsNeutral));
        }
    }

    public int MovementStamp
    {
        get => _movementStamp;
        private set
        {
            if (_movementStamp == value)
            {
                return;
            }

            _movementStamp = value;
            OnPropertyChanged(nameof(MovementStamp));
        }
    }

    public double DurationMs
    {
        get => _durationMs;
        private set
        {
            if (Math.Abs(_durationMs - value) < 0.0001)
            {
                return;
            }

            _durationMs = value;
            OnPropertyChanged(nameof(DurationMs));
        }
    }

    public double DisplayDurationMs
    {
        get => _displayDurationMs;
        private set
        {
            if (Math.Abs(_displayDurationMs - value) < 0.0001)
            {
                return;
            }

            _displayDurationMs = value;
            OnPropertyChanged(nameof(DisplayDurationMs));
        }
    }

    public double DisplayOpacity
    {
        get => _displayOpacity;
        private set
        {
            if (Math.Abs(_displayOpacity - value) < 0.01)
            {
                return;
            }

            _displayOpacity = value;
            OnPropertyChanged(nameof(DisplayOpacity));
        }
    }

    public int Count
    {
        get => _count;
        private set
        {
            if (_count == value)
            {
                return;
            }

            _count = value;
            OnPropertyChanged(nameof(Count));
        }
    }

    public bool IsHolding
    {
        get => _isHolding;
        private set
        {
            if (_isHolding == value)
            {
                return;
            }

            _isHolding = value;
            OnPropertyChanged(nameof(IsHolding));
        }
    }

    public string RowKey => BuildRowKey(_movement, Actions, TriggerKey);

    public static string BuildRowKey(
        IEnumerable<Direction> movement,
        IEnumerable<string> actions,
        string triggerKey) =>
        $"{triggerKey}|{string.Join(",", movement)}|{string.Join(",", actions)}";

    public void SetMovement(IReadOnlyList<Direction> movement, bool? isNeutral = null)
    {
        _movement = movement;

        if (isNeutral is { } neutral)
        {
            IsNeutral = neutral;
        }

        MovementStamp++;
        OnPropertyChanged(nameof(Movement));
        OnPropertyChanged(string.Empty);
    }

    public void StartHold(double atMs)
    {
        LastPressAtMs = atMs;
        DurationMs = 0;
        DisplayDurationMs = 0;
        IsHolding = true;
    }

    public void EndHold(double atMs)
    {
        if (!IsHolding)
        {
            return;
        }

        IsHolding = false;
        DurationMs = Math.Max(0, atMs - LastPressAtMs);
        DisplayDurationMs = DurationMs;
    }

    public void Merge(double atMs)
    {
        _previousDurationMs = DurationMs;
        Count++;
        StartHold(atMs);
    }

    public void RemoveLastPress()
    {
        Count--;
        DurationMs = _previousDurationMs;
        DisplayDurationMs = _previousDurationMs;
    }

    public void RefreshDisplay(double nowMs, bool fade)
    {
        DisplayDurationMs = IsHolding ? Math.Max(0, nowMs - LastPressAtMs) : DurationMs;

        if (!fade)
        {
            DisplayOpacity = 1;
            return;
        }

        var ageMs = Math.Max(0, nowMs - LastPressAtMs);
        DisplayOpacity = Math.Clamp(1.0 - ageMs / 60000.0, 0.45, 1.0);
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
