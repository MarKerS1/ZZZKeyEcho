using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Input;

namespace ZZZKeyEcho.Core.Engine;

public sealed class MappingEngine
{
    private static readonly string[] NoActions = Array.Empty<string>();

    private readonly Dictionary<InputDevice, Dictionary<string, string[]>> _index;
    private readonly Dictionary<InputDevice, Dictionary<string, Direction>> _movement;

    public MappingEngine(ProfilesConfig profiles)
    {
        _index = new Dictionary<InputDevice, Dictionary<string, string[]>>
        {
            [InputDevice.Kbm] = BuildIndex(profiles.Kbm),
            [InputDevice.Xbox] = BuildIndex(profiles.Xbox),
            [InputDevice.Ps] = BuildIndex(profiles.Ps),
            [InputDevice.Simulated] = BuildIndex(profiles.Kbm),
        };

        _movement = new Dictionary<InputDevice, Dictionary<string, Direction>>
        {
            [InputDevice.Kbm] = BuildMovement(profiles.Kbm),
            [InputDevice.Xbox] = BuildMovement(profiles.Xbox),
            [InputDevice.Ps] = BuildMovement(profiles.Ps),
            [InputDevice.Simulated] = BuildMovement(profiles.Kbm),
        };
    }

    public IReadOnlyList<string> Resolve(InputDevice device, string code) =>
        _index.TryGetValue(device, out var byKey) && byKey.TryGetValue(code, out var actions)
            ? actions
            : NoActions;

    public Direction? ResolveMovement(InputDevice device, string code) =>
        _movement.TryGetValue(device, out var byKey) && byKey.TryGetValue(code, out var direction)
            ? direction
            : null;

    private static Dictionary<string, Direction> BuildMovement(MappingProfile profile)
    {
        var index = new Dictionary<string, Direction>(StringComparer.Ordinal);

        foreach (var binding in profile.Bindings)
        {
            if (!binding.Enabled)
            {
                continue;
            }

            var direction = binding.Action switch
            {
                ActionCatalog.DirUp => Direction.Up,
                ActionCatalog.DirDown => Direction.Down,
                ActionCatalog.DirLeft => Direction.Left,
                ActionCatalog.DirRight => Direction.Right,
                _ => (Direction?)null,
            };

            if (direction is null)
            {
                continue;
            }

            foreach (var key in binding.Keys)
            {
                index[key] = direction.Value;
            }
        }

        return index;
    }

    private static Dictionary<string, string[]> BuildIndex(MappingProfile profile)
    {
        var index = new Dictionary<string, string[]>(StringComparer.Ordinal);

        foreach (var binding in profile.Bindings)
        {
            if (!binding.Enabled)
            {
                continue;
            }

            foreach (var key in binding.Keys)
            {
                if (!index.TryGetValue(key, out var existing))
                {
                    index[key] = new[] { binding.Action };
                    continue;
                }

                if (Array.IndexOf(existing, binding.Action) < 0)
                {
                    var merged = new string[existing.Length + 1];
                    Array.Copy(existing, merged, existing.Length);
                    merged[^1] = binding.Action;
                    index[key] = merged;
                }
            }
        }

        return index;
    }
}
