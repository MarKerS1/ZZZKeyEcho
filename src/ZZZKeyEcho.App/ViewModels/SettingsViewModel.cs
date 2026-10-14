using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using ZZZKeyEcho.App.Rendering;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Engine;
using ZZZKeyEcho.Core.Input;

namespace ZZZKeyEcho.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppConfig _config;
    private readonly Action<SettingsChange> _apply;
    private readonly Func<bool> _getDebugMode;
    private readonly Action<bool> _setDebugMode;

    public SettingsViewModel(
        AppConfig config,
        Action<SettingsChange> apply,
        IconProvider icons,
        Func<bool> getDebugMode,
        Action<bool> setDebugMode)
    {
        _config = config;
        _apply = apply;
        _getDebugMode = getDebugMode;
        _setDebugMode = setDebugMode;

        KbmProfile = new ProfileEditorViewModel("键鼠", InputDevice.Kbm, KeyCatalog.Kbm, config.Profiles.Kbm, MappingProfile.CreateDefaultKbm, config, () => apply(SettingsChange.Mapping));
        XboxProfile = new ProfileEditorViewModel("Xbox 手柄", InputDevice.Xbox, KeyCatalog.Xbox, config.Profiles.Xbox, MappingProfile.CreateDefaultXbox, config, () => apply(SettingsChange.Mapping));
        PsProfile = new ProfileEditorViewModel("PS 手柄", InputDevice.Ps, KeyCatalog.Ps, config.Profiles.Ps, MappingProfile.CreateDefaultPs, config, () => apply(SettingsChange.Mapping));

        var iconRows = new List<IconRowViewModel>();

        foreach (var action in ActionCatalog.All)
        {
            iconRows.Add(new IconRowViewModel(action, icons));
        }

        IconRows = iconRows;
    }

    public ProfileEditorViewModel KbmProfile { get; }

    public ProfileEditorViewModel XboxProfile { get; }

    public ProfileEditorViewModel PsProfile { get; }

    public IReadOnlyList<IconRowViewModel> IconRows { get; }

    public double OverlayWidth { get => _config.Overlay.Width; set => Set(_config.Overlay.Width, value, v => _config.Overlay.Width = v, SettingsChange.OverlayStyle); }

    public double OverlayHeight { get => _config.Overlay.Height; set => Set(_config.Overlay.Height, value, v => _config.Overlay.Height = v, SettingsChange.OverlayStyle); }

    public double OverlayX { get => _config.Overlay.X; set => Set(_config.Overlay.X, value, v => _config.Overlay.X = v, SettingsChange.OverlayStyle); }

    public double OverlayY { get => _config.Overlay.Y; set => Set(_config.Overlay.Y, value, v => _config.Overlay.Y = v, SettingsChange.OverlayStyle); }

    public double IconSize { get => _config.Records.IconSize; set => Set(_config.Records.IconSize, value, v => _config.Records.IconSize = v, SettingsChange.Rendering); }

    public string BackgroundColor { get => _config.Overlay.BgColor; set => Set(_config.Overlay.BgColor, value, v => _config.Overlay.BgColor = v, SettingsChange.OverlayStyle); }

    public double CornerRadius { get => _config.Overlay.CornerRadius; set => Set(_config.Overlay.CornerRadius, value, v => _config.Overlay.CornerRadius = v, SettingsChange.OverlayStyle); }

    public double Padding { get => _config.Overlay.Padding; set => Set(_config.Overlay.Padding, value, v => _config.Overlay.Padding = v, SettingsChange.OverlayStyle); }

    public double BackgroundOpacity { get => _config.Overlay.BackgroundOpacity; set => Set(_config.Overlay.BackgroundOpacity, value, v => _config.Overlay.BackgroundOpacity = v, SettingsChange.OverlayStyle); }

    public double IconOpacity { get => _config.Overlay.IconOpacity; set => Set(_config.Overlay.IconOpacity, value, v => _config.Overlay.IconOpacity = v, SettingsChange.OverlayStyle); }

    public bool Locked { get => _config.Overlay.Locked; set => Set(_config.Overlay.Locked, value, v => _config.Overlay.Locked = v, SettingsChange.OverlayStyle); }

    public bool FollowGame { get => _config.Overlay.FollowGame; set => Set(_config.Overlay.FollowGame, value, v => _config.Overlay.FollowGame = v, SettingsChange.OverlayStyle); }

    public bool WebOverlayEnabled { get => _config.WebOverlay.Enabled; set => Set(_config.WebOverlay.Enabled, value, v => _config.WebOverlay.Enabled = v, SettingsChange.None); }

    public bool HideLocalOverlay { get => _config.WebOverlay.HideLocalOverlay; set => Set(_config.WebOverlay.HideLocalOverlay, value, v => _config.WebOverlay.HideLocalOverlay = v, SettingsChange.OverlayStyle); }

    public int WebOverlayPort { get => _config.WebOverlay.Port; set => Set(_config.WebOverlay.Port, value, v => _config.WebOverlay.Port = v, SettingsChange.None); }

    public double WebOverlayFontScale { get => _config.WebOverlay.FontScale; set => Set(_config.WebOverlay.FontScale, value, v => _config.WebOverlay.FontScale = v, SettingsChange.None); }

    public int MaxCount { get => _config.Records.MaxCount; set => Set(_config.Records.MaxCount, value, v => _config.Records.MaxCount = v, SettingsChange.None); }

    public bool ComboMode { get => _config.Records.ComboMode; set => Set(_config.Records.ComboMode, value, v => _config.Records.ComboMode = v, SettingsChange.None); }

    public int ComboWindowMs { get => _config.Records.ComboWindowMs; set => Set(_config.Records.ComboWindowMs, value, v => _config.Records.ComboWindowMs = v, SettingsChange.None); }

    public bool RecordMovement { get => _config.Records.RecordMovement; set => Set(_config.Records.RecordMovement, value, v => _config.Records.RecordMovement = v, SettingsChange.None); }

    public bool ShowNeutralRow { get => _config.Records.ShowNeutralRow; set => Set(_config.Records.ShowNeutralRow, value, v => _config.Records.ShowNeutralRow = v, SettingsChange.None); }

    public bool RepeatMerge { get => _config.Records.RepeatMerge; set => Set(_config.Records.RepeatMerge, value, v => _config.Records.RepeatMerge = v, SettingsChange.None); }

    public int RepeatWindowMs { get => _config.Records.RepeatWindowMs; set => Set(_config.Records.RepeatWindowMs, value, v => _config.Records.RepeatWindowMs = v, SettingsChange.None); }

    public HoldDisplayMode HoldDisplay { get => _config.Records.HoldDisplay; set => Set(_config.Records.HoldDisplay, value, v => _config.Records.HoldDisplay = v, SettingsChange.Rendering); }

    public HoldTimeFormat HoldFormat { get => _config.Records.HoldFormat; set => Set(_config.Records.HoldFormat, value, v => _config.Records.HoldFormat = v, SettingsChange.Rendering); }

    public int FramesPerSecond { get => _config.Records.FramesPerSecond; set => Set(_config.Records.FramesPerSecond, value, v => _config.Records.FramesPerSecond = v, SettingsChange.Rendering); }

    public int HoldThresholdMs { get => _config.Records.HoldThresholdMs; set => Set(_config.Records.HoldThresholdMs, value, v => _config.Records.HoldThresholdMs = v, SettingsChange.Rendering); }

    public bool NewestAtBottom { get => _config.Records.NewestAtBottom; set => Set(_config.Records.NewestAtBottom, value, v => _config.Records.NewestAtBottom = v, SettingsChange.Rendering); }

    public bool DurationOnLeft { get => _config.Records.DurationOnLeft; set => Set(_config.Records.DurationOnLeft, value, v => _config.Records.DurationOnLeft = v, SettingsChange.Rendering); }

    public bool Fade { get => _config.Records.Fade; set => Set(_config.Records.Fade, value, v => _config.Records.Fade = v, SettingsChange.Rendering); }

    public bool ForegroundOnly { get => _config.Input.ForegroundOnly; set => Set(_config.Input.ForegroundOnly, value, v => _config.Input.ForegroundOnly = v, SettingsChange.None); }

    public IconStyle ControllerIconStyle { get => _config.Input.ControllerIconStyle; set => Set(_config.Input.ControllerIconStyle, value, v => _config.Input.ControllerIconStyle = v, SettingsChange.None); }

    public double StickDeadzone { get => _config.Input.StickDeadzone; set => Set(_config.Input.StickDeadzone, value, v => _config.Input.StickDeadzone = v, SettingsChange.None); }

    public string GameProcessText
    {
        get => string.Join(", ", _config.Input.GameProcess);
        set => Set(string.Join(", ", _config.Input.GameProcess), value, v => _config.Input.GameProcess = SplitProcesses(v), SettingsChange.None);
    }

    public bool SettingsTopMost { get => _config.Ui.SettingsTopMost; set => Set(_config.Ui.SettingsTopMost, value, v => _config.Ui.SettingsTopMost = v, SettingsChange.None); }

    public bool StartWithWindows
    {
        get => AutoStart.IsEnabled();
        set
        {
            if (AutoStart.IsEnabled() == value)
            {
                return;
            }

            AutoStart.Set(value);
            OnPropertyChanged();
        }
    }

    public string ToggleOverlayHotkey { get => _config.Hotkeys.ToggleOverlay; set => Set(_config.Hotkeys.ToggleOverlay, value, v => _config.Hotkeys.ToggleOverlay = v, SettingsChange.None); }

    public string ToggleLockHotkey { get => _config.Hotkeys.ToggleLock; set => Set(_config.Hotkeys.ToggleLock, value, v => _config.Hotkeys.ToggleLock = v, SettingsChange.None); }

    public string OpenSettingsHotkey { get => _config.Hotkeys.OpenSettings; set => Set(_config.Hotkeys.OpenSettings, value, v => _config.Hotkeys.OpenSettings = v, SettingsChange.None); }

    public string DebugModeHotkey { get => _config.Hotkeys.DebugMode; set => Set(_config.Hotkeys.DebugMode, value, v => _config.Hotkeys.DebugMode = v, SettingsChange.None); }

    public bool ToggleOverlayHotkeyEnabled { get => IsHotkeyEnabled(_config.Hotkeys.ToggleOverlay); set => SetHotkeyEnabled(value, v => _config.Hotkeys.ToggleOverlay = v, () => _config.Hotkeys.ToggleOverlay, "F8", nameof(ToggleOverlayHotkey)); }

    public bool ToggleLockHotkeyEnabled { get => IsHotkeyEnabled(_config.Hotkeys.ToggleLock); set => SetHotkeyEnabled(value, v => _config.Hotkeys.ToggleLock = v, () => _config.Hotkeys.ToggleLock, "F9", nameof(ToggleLockHotkey)); }

    public bool OpenSettingsHotkeyEnabled { get => IsHotkeyEnabled(_config.Hotkeys.OpenSettings); set => SetHotkeyEnabled(value, v => _config.Hotkeys.OpenSettings = v, () => _config.Hotkeys.OpenSettings, "F10", nameof(OpenSettingsHotkey)); }

    public bool DebugModeHotkeyEnabled { get => IsHotkeyEnabled(_config.Hotkeys.DebugMode); set => SetHotkeyEnabled(value, v => _config.Hotkeys.DebugMode = v, () => _config.Hotkeys.DebugMode, "F11", nameof(DebugModeHotkey)); }

    public bool DebugMode
    {
        get => _getDebugMode();
        set
        {
            if (_getDebugMode() == value)
            {
                return;
            }

            _setDebugMode(value);
            OnPropertyChanged();
        }
    }

    public bool DebugRemember { get => _config.Debug.Remember; set => Set(_config.Debug.Remember, value, v => _config.Debug.Remember = v, SettingsChange.None); }

    public void Reload()
    {
        KbmProfile.Reload();
        XboxProfile.Reload();
        PsProfile.Reload();

        foreach (var row in IconRows)
        {
            row.Refresh();
        }

        OnPropertyChanged(string.Empty);
        _apply(SettingsChange.All);
    }

    public void SyncOverlayGeometry()
    {
        OnPropertyChanged(nameof(OverlayX));
        OnPropertyChanged(nameof(OverlayY));
        OnPropertyChanged(nameof(OverlayWidth));
        OnPropertyChanged(nameof(OverlayHeight));
    }

    private static bool IsHotkeyEnabled(string gesture) => !string.IsNullOrWhiteSpace(gesture);

    private void SetHotkeyEnabled(bool enabled, Action<string> setter, Func<string> getter, string defaultGesture, string enabledPropertyName)
    {
        if (enabled == IsHotkeyEnabled(getter()))
        {
            return;
        }

        setter(enabled ? defaultGesture : string.Empty);
        OnPropertyChanged(enabledPropertyName);
        _apply(SettingsChange.None);
    }

    private static List<string> SplitProcesses(string text)
    {
        var result = new List<string>();

        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            result.Add(part);
        }

        return result;
    }

    private void Set<T>(T current, T value, Action<T> setter, SettingsChange scope, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value))
        {
            return;
        }

        setter(value);
        OnPropertyChanged(propertyName);
        _apply(scope);
    }
}
