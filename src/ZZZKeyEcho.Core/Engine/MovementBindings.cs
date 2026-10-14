namespace ZZZKeyEcho.Core.Engine;

public static class MovementBindings
{
    private static readonly Dictionary<string, Direction> Codes = new(StringComparer.Ordinal)
    {
        ["W"] = Direction.Up,
        ["S"] = Direction.Down,
        ["A"] = Direction.Left,
        ["D"] = Direction.Right,
        ["Up"] = Direction.Up,
        ["Down"] = Direction.Down,
        ["Left"] = Direction.Left,
        ["Right"] = Direction.Right,
    };

    public static bool TryResolve(string code, out Direction direction) =>
        Codes.TryGetValue(code, out direction);

    public static string ToActionId(Direction direction) => direction switch
    {
        Direction.Up => ActionCatalog.DirUp,
        Direction.Down => ActionCatalog.DirDown,
        Direction.Left => ActionCatalog.DirLeft,
        _ => ActionCatalog.DirRight,
    };
}
