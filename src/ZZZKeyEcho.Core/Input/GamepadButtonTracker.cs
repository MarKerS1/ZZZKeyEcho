namespace ZZZKeyEcho.Core.Input;

public sealed class GamepadButtonTracker
{
    private readonly HashSet<string> _pressed = new(StringComparer.Ordinal);

    public IReadOnlyList<(string Code, InputEventKind Kind)> Update(IReadOnlyCollection<string> current)
    {
        var changes = new List<(string, InputEventKind)>(4);

        foreach (var code in current)
        {
            if (_pressed.Add(code))
            {
                changes.Add((code, InputEventKind.Down));
            }
        }

        List<string>? released = null;

        foreach (var code in _pressed)
        {
            if (!current.Contains(code))
            {
                (released ??= new List<string>()).Add(code);
            }
        }

        if (released is not null)
        {
            foreach (var code in released)
            {
                _pressed.Remove(code);
                changes.Add((code, InputEventKind.Up));
            }
        }

        return changes;
    }

    public IReadOnlyList<(string Code, InputEventKind Kind)> ReleaseAll()
    {
        var changes = new List<(string, InputEventKind)>(_pressed.Count);

        foreach (var code in _pressed)
        {
            changes.Add((code, InputEventKind.Up));
        }

        _pressed.Clear();
        return changes;
    }
}
