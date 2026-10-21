using System.IO;
using System.Globalization;

namespace ZZZKeyEcho.Core.Activity;

public sealed record ActivityRow(double PressedAt, string ActionId, double ReleasedAt)
{
    public double Duration => Math.Max(0, ReleasedAt - PressedAt);
}

public static class ActivityCsv
{
    public const string Header = "pressed_at,action_id,released_at";

    public static IReadOnlyList<ActivityRow> Read(string path)
    {
        var rows = new List<ActivityRow>();

        if (!File.Exists(path))
        {
            return rows;
        }

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("pressed_at", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length < 3)
            {
                continue;
            }

            if (!double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var pressed))
            {
                continue;
            }

            if (!double.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var released))
            {
                continue;
            }

            var action = parts[1].Trim();
            if (action.Length == 0)
            {
                continue;
            }

            rows.Add(new ActivityRow(pressed, action, released));
        }

        rows.Sort((a, b) => a.PressedAt.CompareTo(b.PressedAt));
        return rows;
    }
}
