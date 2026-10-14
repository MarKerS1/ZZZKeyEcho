namespace ZZZKeyEcho.Core.Input;

public sealed class StickProcessor
{
    private const double HysteresisDegrees = 5;
    private const double SectorHalfWidthDegrees = 45;
    private const double ReleaseRatio = 0.9;

    private readonly double _deadzone;
    private readonly HashSet<string> _active = new(StringComparer.Ordinal);

    public StickProcessor(double deadzone)
    {
        _deadzone = Math.Clamp(deadzone, 0.05, 0.9);
    }

    public IReadOnlyList<string> Update(double x, double y)
    {
        var magnitude = Math.Sqrt(x * x + y * y);
        var threshold = _active.Count > 0 ? _deadzone * ReleaseRatio : _deadzone;

        if (magnitude < threshold)
        {
            _active.Clear();
            return Array.Empty<string>();
        }

        var angle = Math.Atan2(y, x) * 180.0 / Math.PI;

        var result = new List<string>(2);

        AddIfWithin(result, GamepadCodes.Right, 0, angle);
        AddIfWithin(result, GamepadCodes.Up, 90, angle);
        AddIfWithin(result, GamepadCodes.Left, 180, angle);
        AddIfWithin(result, GamepadCodes.Down, -90, angle);

        _active.Clear();

        foreach (var code in result)
        {
            _active.Add(code);
        }

        return result;
    }

    private void AddIfWithin(List<string> result, string code, double center, double angle)
    {
        var distance = Math.Abs(Normalize(angle - center));
        var halfWidth = SectorHalfWidthDegrees + (_active.Contains(code) ? HysteresisDegrees : 0);

        if (distance <= halfWidth)
        {
            result.Add(code);
        }
    }

    private static double Normalize(double angle)
    {
        while (angle > 180)
        {
            angle -= 360;
        }

        while (angle < -180)
        {
            angle += 360;
        }

        return angle;
    }
}
