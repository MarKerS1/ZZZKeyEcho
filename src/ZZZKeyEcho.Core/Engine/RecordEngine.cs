using System.Collections.ObjectModel;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Input;
using ZZZKeyEcho.Core.Activity;

namespace ZZZKeyEcho.Core.Engine;

public sealed class RecordEngine
{
    private const double MovementDebounceMs = 80;

    private static readonly Direction[] Cardinal =
    {
        Direction.Up,
        Direction.Down,
        Direction.Left,
        Direction.Right,
    };

    private readonly RecordsConfig _settings;
    private MappingEngine _mapping;
    private readonly HashSet<Direction> _held = new();
    private readonly RotationDetector _rotation = new();
    private readonly Dictionary<string, InputRecord> _activeByKey = new(StringComparer.Ordinal);
    private readonly Dictionary<Direction, double> _recentChangeMs = new();
    private double _lastMovementChangeMs = double.MinValue;
    private Direction? _lastMovementValue;
    private double _rotateUntilMs = double.MinValue;
    private InputRecord? _lastMovementRecord;
    private double _lastMovementRecordMs = double.MinValue;

    public RecordEngine(RecordsConfig settings, MappingEngine mapping)
    {
        _settings = settings;
        _mapping = mapping;
    }

    public ObservableCollection<InputRecord> Records { get; } = new();

    public event EventHandler<ActivityEvent>? ActionJournal;

    public void UpdateMapping(MappingEngine mapping) => _mapping = mapping;

    public void Handle(InputEvent inputEvent)
    {
        if (inputEvent.Code == "Rotate")
        {
            if (inputEvent.Kind == InputEventKind.Down)
            {
                _rotateUntilMs = inputEvent.TimestampMs + _settings.ComboWindowMs;
                _lastMovementChangeMs = inputEvent.TimestampMs;
                _lastMovementValue = Direction.Rotate;

                if (_settings.RecordMovement)
                {
                    RecordMovementRow(inputEvent.TimestampMs, Direction.Rotate);
                }
            }

            return;
        }

        if (_mapping.ResolveMovement(inputEvent.Device, inputEvent.Code) is { } resolved)
        {
            HandleDirection(inputEvent, resolved);
        }
        else if (inputEvent.Kind == InputEventKind.Down)
        {
            HandleActionDown(inputEvent);
        }
        else
        {
            HandleActionUp(inputEvent);
        }
    }

    public void Clear()
    {
        Records.Clear();
        _activeByKey.Clear();
        _lastMovementRecord = null;
        _lastMovementRecordMs = double.MinValue;
        _rotation.Clear();
    }

    private void HandleDirection(InputEvent inputEvent, Direction direction)
    {
        var changed = inputEvent.Kind == InputEventKind.Down
            ? _held.Add(direction)
            : _held.Remove(direction);

        if (!changed)
        {
            return;
        }

        ActionJournal?.Invoke(this, new ActivityEvent(ToActionId(direction), inputEvent.Kind == InputEventKind.Down ? ActivityKind.Down : ActivityKind.Up, inputEvent.TimestampMs, inputEvent.Device.ToString(), inputEvent.Code));

        _lastMovementChangeMs = inputEvent.TimestampMs;
        _recentChangeMs[direction] = inputEvent.TimestampMs;

        if (Collapse(includeRecent: false, inputEvent.TimestampMs) is { } current)
        {
            _lastMovementValue = current;
        }

        if (inputEvent.Kind == InputEventKind.Down && _rotation.Register(direction.ToString(), inputEvent.TimestampMs))
        {
            _rotateUntilMs = inputEvent.TimestampMs + _settings.ComboWindowMs;
            _lastMovementValue = Direction.Rotate;
        }

        if (_settings.RecordMovement)
        {
            RecordMovementRow(inputEvent.TimestampMs, Collapse(includeRecent: false, inputEvent.TimestampMs));
        }
    }

    private IReadOnlyList<Direction> ResolveComboMovement(double nowMs)
    {
        if (!_settings.ComboMode)
        {
            return Array.Empty<Direction>();
        }

        var movement = Collapse(includeRecent: true, nowMs);

        return movement is null ? Array.Empty<Direction>() : new[] { movement.Value };
    }

    private Direction? Collapse(bool includeRecent, double nowMs)
    {
        bool Held(Direction direction) => _held.Contains(direction);

        bool Active(Direction direction) =>
            Held(direction)
            || (includeRecent
                && nowMs - _recentChangeMs.GetValueOrDefault(direction, double.MinValue) <= _settings.ComboWindowMs);

        if (nowMs <= _rotateUntilMs)
        {
            return Direction.Rotate;
        }

        var up = Active(Direction.Up);
        var down = Active(Direction.Down);
        var left = Active(Direction.Left);
        var right = Active(Direction.Right);

        if (up && left)
        {
            return Direction.UpLeft;
        }

        if (up && right)
        {
            return Direction.UpRight;
        }

        if (down && left)
        {
            return Direction.DownLeft;
        }

        if (down && right)
        {
            return Direction.DownRight;
        }

        if (up)
        {
            return Direction.Up;
        }

        if (down)
        {
            return Direction.Down;
        }

        if (left)
        {
            return Direction.Left;
        }

        return right ? Direction.Right : null;
    }

    private void RecordMovementRow(double nowMs, Direction? movement)
    {
        var neutral = movement is null;

        if (neutral && !_settings.ShowNeutralRow)
        {
            EndMovementRow(nowMs);
            return;
        }

        if (_lastMovementRecord is not null
            && Records.Count > 0
            && ReferenceEquals(Records[^1], _lastMovementRecord))
        {
            _lastMovementRecord.SetMovement(neutral ? Array.Empty<Direction>() : new[] { movement!.Value }, neutral);
            _lastMovementRecord.StartHold(nowMs);
            _lastMovementRecordMs = nowMs;
            return;
        }

        EndMovementRow(nowMs);

        var record = new InputRecord(
            neutral ? Array.Empty<Direction>() : new[] { movement!.Value },
            Array.Empty<string>(),
            string.Empty,
            nowMs,
            neutral);

        record.StartHold(nowMs);
        Add(record);
        _lastMovementRecord = record;
        _lastMovementRecordMs = nowMs;
    }

    private void EndMovementRow(double atMs)
    {
        if (_lastMovementRecord is not null && _lastMovementRecord.IsHolding)
        {
            _lastMovementRecord.EndHold(atMs);
        }

        _lastMovementRecord = null;
    }

    private void HandleActionDown(InputEvent inputEvent)
    {
        if (IsAutoRepeat(inputEvent.Code))
        {
            return;
        }

        var actions = _mapping.Resolve(inputEvent.Device, inputEvent.Code);
        if (actions.Count == 0)
        {
            return;
        }

        foreach (var action in actions)
        {
            ActionJournal?.Invoke(this, new ActivityEvent(action, ActivityKind.Down, inputEvent.TimestampMs, inputEvent.Device.ToString(), inputEvent.Code));
        }

        var movement = ResolveComboMovement(inputEvent.TimestampMs);
        var rowKey = InputRecord.BuildRowKey(movement, actions, inputEvent.Code);

        if (TryMerge(inputEvent, rowKey))
        {
            return;
        }

        var record = new InputRecord(movement, actions, inputEvent.Code, inputEvent.TimestampMs);
        record.StartHold(inputEvent.TimestampMs);
        Add(record);
        _activeByKey[inputEvent.Code] = record;
    }

    private void HandleActionUp(InputEvent inputEvent)
    {
        if (!_activeByKey.Remove(inputEvent.Code, out var record))
        {
            return;
        }

        foreach (var action in record.Actions)
        {
            ActionJournal?.Invoke(this, new ActivityEvent(action, ActivityKind.Up, inputEvent.TimestampMs, inputEvent.Device.ToString(), inputEvent.Code));
        }

        record.EndHold(inputEvent.TimestampMs);

        if (record.Count <= 1 || record.DurationMs < _settings.RepeatWindowMs)
        {
            return;
        }

        var hold = new InputRecord(record.Movement, record.Actions, record.TriggerKey, record.LastPressAtMs);
        hold.StartHold(record.LastPressAtMs);
        hold.EndHold(inputEvent.TimestampMs);
        record.RemoveLastPress();
        Add(hold);
    }

    private bool IsAutoRepeat(string code) => _activeByKey.ContainsKey(code);

    private static string ToActionId(Direction direction) => direction switch
    {
        Direction.Up => "dirUp",
        Direction.Down => "dirDown",
        Direction.Left => "dirLeft",
        Direction.Right => "dirRight",
        _ => direction.ToString(),
    };

    private bool TryMerge(InputEvent inputEvent, string rowKey)
    {
        if (!_settings.RepeatMerge || Records.Count == 0)
        {
            return false;
        }

        var last = Records[^1];

        if (last.RowKey != rowKey || last.DurationMs >= _settings.RepeatWindowMs)
        {
            return false;
        }

        last.Merge(inputEvent.TimestampMs);
        _activeByKey[inputEvent.Code] = last;
        return true;
    }

    private void Add(InputRecord record)
    {
        Records.Add(record);

        var max = Math.Max(1, _settings.MaxCount);
        while (Records.Count > max)
        {
            Records.RemoveAt(0);
        }
    }
}
