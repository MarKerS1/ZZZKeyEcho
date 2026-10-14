namespace ZZZKeyEcho.Core.Input;

public sealed class GamepadTranslator
{
    private const double TriggerReleaseRatio = 0.75;

    private readonly GamepadButtonTracker _tracker = new();
    private readonly StickProcessor _stick;
    private readonly double _triggerThreshold;
    private readonly List<(double Angle, double Time)> _stickHistory = new(16);
    private bool _leftTriggerPressed;
    private bool _rightTriggerPressed;
    private double _rotationCooldownMs;
    private double _lastStickTimeMs;

    public GamepadTranslator(double stickDeadzone, double triggerThreshold)
    {
        _stick = new StickProcessor(stickDeadzone);
        _triggerThreshold = Math.Clamp(triggerThreshold, 0.05, 0.95);
    }

    public IReadOnlyList<(string Code, InputEventKind Kind)> Translate(GamepadReading reading, GamepadNaming naming)
    {
        var pressed = new List<string>(12);

        if (reading.Cross)
        {
            pressed.Add(GamepadCodes.FaceDown(naming));
        }

        if (reading.Circle)
        {
            pressed.Add(GamepadCodes.FaceRight(naming));
        }

        if (reading.Square)
        {
            pressed.Add(GamepadCodes.FaceLeft(naming));
        }

        if (reading.Triangle)
        {
            pressed.Add(GamepadCodes.FaceUp(naming));
        }

        if (reading.L1)
        {
            pressed.Add(GamepadCodes.ShoulderLeft(naming));
        }

        if (reading.R1)
        {
            pressed.Add(GamepadCodes.ShoulderRight(naming));
        }

        if (reading.L3)
        {
            pressed.Add(GamepadCodes.StickLeft(naming));
        }

        if (reading.R3)
        {
            pressed.Add(GamepadCodes.StickRight(naming));
        }

        if (reading.DPadUp)
        {
            pressed.Add(GamepadCodes.DPad(naming, "Up"));
        }

        if (reading.DPadDown)
        {
            pressed.Add(GamepadCodes.DPad(naming, "Down"));
        }

        if (reading.DPadLeft)
        {
            pressed.Add(GamepadCodes.DPad(naming, "Left"));
        }

        if (reading.DPadRight)
        {
            pressed.Add(GamepadCodes.DPad(naming, "Right"));
        }

        if (UpdateTrigger(reading.LeftTrigger, ref _leftTriggerPressed))
        {
            pressed.Add(GamepadCodes.TriggerLeft(naming));
        }

        if (UpdateTrigger(reading.RightTrigger, ref _rightTriggerPressed))
        {
            pressed.Add(GamepadCodes.TriggerRight(naming));
        }

        foreach (var direction in _stick.Update(reading.LeftStickX, reading.LeftStickY))
        {
            pressed.Add(direction);
        }

        if (UpdateRotation(reading.LeftStickX, reading.LeftStickY))
        {
            pressed.Add("Rotate");
        }

        return _tracker.Update(pressed);
    }

    private bool UpdateRotation(double x, double y)
    {
        var magnitude = Math.Sqrt(x * x + y * y);

        if (magnitude < 0.55)
        {
            _stickHistory.Clear();
            _lastStickTimeMs = 0;
            return false;
        }

        var angle = Math.Atan2(y, x) * 180.0 / Math.PI;
        var now = Environment.TickCount64;
        var delta = _lastStickTimeMs == 0 ? 0 : now - _lastStickTimeMs;
        _lastStickTimeMs = now;

        if (delta > 400 || _stickHistory.Count == 0)
        {
            _stickHistory.Clear();
            _stickHistory.Add((angle, now));
            return false;
        }

        _stickHistory.Add((angle, now));
        _stickHistory.RemoveAll(entry => now - entry.Time > 700);

        var swept = 0.0;

        for (var i = 1; i < _stickHistory.Count; i++)
        {
            var step = _stickHistory[i].Angle - _stickHistory[i - 1].Angle;

            while (step > 180)
            {
                step -= 360;
            }

            while (step < -180)
            {
                step += 360;
            }

            swept += step;
        }

        if (Math.Abs(swept) < 320 || now < _rotationCooldownMs)
        {
            return false;
        }

        _rotationCooldownMs = now + 500;
        _stickHistory.Clear();
        _lastStickTimeMs = 0;
        return true;
    }

    public IReadOnlyList<(string Code, InputEventKind Kind)> ReleaseAll() => _tracker.ReleaseAll();

    private bool UpdateTrigger(double value, ref bool pressed)
    {
        if (pressed)
        {
            if (value < _triggerThreshold * TriggerReleaseRatio)
            {
                pressed = false;
            }
        }
        else if (value >= _triggerThreshold)
        {
            pressed = true;
        }

        return pressed;
    }
}
