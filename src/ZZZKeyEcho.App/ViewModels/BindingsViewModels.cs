using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Engine;
using ZZZKeyEcho.Core.Input;

namespace ZZZKeyEcho.App.ViewModels;

public sealed class KeyChipViewModel
{
    public KeyChipViewModel(string key, BindingRowViewModel row)
    {
        Key = key;
        RemoveCommand = new RelayCommand(() => row.RemoveKey(key));
    }

    public string Key { get; }

    public IRelayCommand RemoveCommand { get; }
}

public sealed class BindingRowViewModel : ObservableObject
{
    private readonly BindingConfig _binding;
    private readonly Action _apply;
    private readonly Action<string>? _setPrimary;
    private bool _conflicted;

    public BindingRowViewModel(
        string actionId,
        string actionName,
        BindingConfig binding,
        IReadOnlyList<string> availableKeys,
        InputDevice device,
        Action apply,
        Action<string>? setPrimary = null)
    {
        ActionId = actionId;
        ActionName = actionName;
        _binding = binding;
        _apply = apply;
        _setPrimary = setPrimary;
        AvailableKeys = availableKeys;
        Device = device;
        ReloadKeys();
    }

    public string ActionId { get; }

    public string ActionName { get; }

    public IReadOnlyList<string> AvailableKeys { get; }

    public InputDevice Device { get; }

    public ObservableCollection<KeyChipViewModel> Chips { get; } = new();

    public string KeysText => string.Join(" / ", _binding.Keys);

    public bool Enabled
    {
        get => _binding.Enabled;
        set
        {
            if (_binding.Enabled == value)
            {
                return;
            }

            _binding.Enabled = value;
            OnPropertyChanged();
            _apply();
        }
    }

    public bool HasConflict
    {
        get => _conflicted;
        set
        {
            if (_conflicted == value)
            {
                return;
            }

            _conflicted = value;
            OnPropertyChanged();
        }
    }

    public string? PrimaryKey => _binding.Keys.Count > 0 ? _binding.Keys[0] : null;

    public void AddKey(string key)
    {
        if (_binding.Keys.Contains(key))
        {
            return;
        }

        _binding.Keys.Add(key);
        ReloadKeys();
        _apply();
    }

    public void RemoveKey(string key)
    {
        if (!_binding.Keys.Remove(key))
        {
            return;
        }

        ReloadKeys();
        _apply();
    }

    public void SetPrimary()
    {
        _setPrimary?.Invoke(ActionId);
    }

    public void ReloadKeys()
    {
        Chips.Clear();

        foreach (var key in _binding.Keys)
        {
            Chips.Add(new KeyChipViewModel(key, this));
        }

        OnPropertyChanged(nameof(KeysText));
        OnPropertyChanged(nameof(PrimaryKey));
    }
}

public sealed class ProfileEditorViewModel
{
    private readonly AppConfig _config;
    private readonly Func<MappingProfile> _defaultFactory;
    private readonly Action _apply;
    private readonly Func<string, BindingConfig> _ensureBinding;
    private readonly MappingProfile _profile;

    public ProfileEditorViewModel(
        string deviceName,
        InputDevice device,
        IReadOnlyList<string> availableKeys,
        MappingProfile profile,
        Func<MappingProfile> defaultFactory,
        AppConfig config,
        Action apply)
    {
        DeviceName = deviceName;
        Device = device;
        AvailableKeys = availableKeys;
        _profile = profile;
        _defaultFactory = defaultFactory;
        _config = config;
        _apply = apply;
        _ensureBinding = actionId => EnsureBinding(actionId);
        Reload();
    }

    public string DeviceName { get; }

    public InputDevice Device { get; }

    public IReadOnlyList<string> AvailableKeys { get; }

    public ObservableCollection<BindingRowViewModel> Rows { get; } = new();

    public void Reload()
    {
        Rows.Clear();

        foreach (var action in ActionCatalog.All)
        {
            if (action.IconOnly)
            {
                continue;
            }

            Rows.Add(new BindingRowViewModel(
                action.Id,
                action.DisplayName,
                _ensureBinding(action.Id),
                AvailableKeys,
                Device,
                ApplyFromRow,
                SetPrimary));
        }

        RefreshConflicts();
    }

    public void RestoreDefaults()
    {
        var defaults = _defaultFactory();
        _profile.Bindings = defaults.Bindings;
        Reload();
        _apply();
    }

    public void RefreshConflicts()
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var row in Rows)
        {
            if (!row.Enabled)
            {
                continue;
            }

            foreach (var key in _profile.Bindings.Find(b => b.Action == row.ActionId)?.Keys ?? new List<string>())
            {
                counts[key] = counts.GetValueOrDefault(key) + 1;
            }
        }

        foreach (var row in Rows)
        {
            var keys = _profile.Bindings.Find(b => b.Action == row.ActionId)?.Keys;
            row.HasConflict = row.Enabled
                && keys is not null
                && keys.Exists(key => counts.GetValueOrDefault(key) > 1);
        }
    }

    private void SetPrimary(string actionId)
    {
        var binding = _profile.Bindings.Find(b => b.Action == actionId);

        if (binding is null || _profile.Bindings.IndexOf(binding) == 0)
        {
            return;
        }

        _profile.Bindings.Remove(binding);
        _profile.Bindings.Insert(0, binding);
        Reload();
        _apply();
    }

    private void ApplyFromRow()
    {
        RefreshConflicts();
        _apply();
    }

    private BindingConfig EnsureBinding(string actionId)
    {
        var binding = _profile.Bindings.Find(b => b.Action == actionId);

        if (binding is null)
        {
            binding = new BindingConfig { Action = actionId, Enabled = false };
            _profile.Bindings.Add(binding);
        }

        return binding;
    }
}
