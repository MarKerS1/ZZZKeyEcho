namespace ZZZKeyEcho.Core.Activity;

public enum ActivityKind
{
    Down,
    Up,
}

public sealed record ActivityEvent(
    string ActionId,
    ActivityKind Kind,
    double TimestampMs,
    string Device,
    string Code);
