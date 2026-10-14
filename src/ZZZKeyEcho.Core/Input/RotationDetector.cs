namespace ZZZKeyEcho.Core.Input;

public sealed class RotationDetector
{
    private const double WindowMs = 900;
    private const int RequiredDirections = 4;

    private static readonly Dictionary<string, int> Order = new(StringComparer.Ordinal)
    {
        ["Up"] = 0,
        ["Right"] = 1,
        ["Down"] = 2,
        ["Left"] = 3,
    };

    private readonly List<(int Index, double Time)> _history = new(8);

    public bool Register(string direction, double timestampMs)
    {
        if (!Order.TryGetValue(direction, out var index))
        {
            return false;
        }

        _history.Add((index, timestampMs));
        _history.RemoveAll(entry => timestampMs - entry.Time > WindowMs);

        if (_history.Count < RequiredDirections)
        {
            return false;
        }

        var distinct = new List<int>(RequiredDirections);

        for (var i = _history.Count - 1; i >= 0 && distinct.Count < RequiredDirections; i--)
        {
            if (!distinct.Contains(_history[i].Index))
            {
                distinct.Insert(0, _history[i].Index);
            }
        }

        if (distinct.Count < RequiredDirections)
        {
            return false;
        }

        var clockwise = true;
        var counterClockwise = true;

        for (var i = 0; i < RequiredDirections; i++)
        {
            var current = distinct[i];
            var next = distinct[(i + 1) % RequiredDirections];

            if ((current + 1) % RequiredDirections != next)
            {
                clockwise = false;
            }

            if ((current + RequiredDirections - 1) % RequiredDirections != next)
            {
                counterClockwise = false;
            }
        }

        if (!clockwise && !counterClockwise)
        {
            return false;
        }

        _history.Clear();
        return true;
    }

    public void Clear() => _history.Clear();
}
