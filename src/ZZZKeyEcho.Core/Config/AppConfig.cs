namespace ZZZKeyEcho.Core.Config;

public sealed class AppConfig
{
    public const int CurrentVersion = 3;

    public int Version { get; set; } = CurrentVersion;
    public OverlayConfig Overlay { get; set; } = new();
    public RecordsConfig Records { get; set; } = new();
    public InputConfig Input { get; set; } = new();
    public HotkeysConfig Hotkeys { get; set; } = new();
    public ProfilesConfig Profiles { get; set; } = new();
    public Dictionary<string, string> IconOverrides { get; set; } = new();
    public DebugConfig Debug { get; set; } = new();
    public UiConfig Ui { get; set; } = new();
    public WebOverlayConfig WebOverlay { get; set; } = new();
    public ActivityConfig Activity { get; set; } = new();

    public void CopyFrom(AppConfig other)
    {
        Version = other.Version;

        Overlay.X = other.Overlay.X;
        Overlay.Y = other.Overlay.Y;
        Overlay.Width = other.Overlay.Width;
        Overlay.Height = other.Overlay.Height;
        Overlay.BgColor = other.Overlay.BgColor;
        Overlay.CornerRadius = other.Overlay.CornerRadius;
        Overlay.Padding = other.Overlay.Padding;
        Overlay.Opacity = other.Overlay.Opacity;
        Overlay.BackgroundOpacity = other.Overlay.BackgroundOpacity;
        Overlay.IconOpacity = other.Overlay.IconOpacity;
        Overlay.TextScale = other.Overlay.TextScale;
        Overlay.Locked = other.Overlay.Locked;
        Overlay.FollowGame = other.Overlay.FollowGame;

        Records.IconSize = other.Records.IconSize;
        Records.MaxCount = other.Records.MaxCount;
        Records.ComboMode = other.Records.ComboMode;
        Records.ComboWindowMs = other.Records.ComboWindowMs;
        Records.RecordMovement = other.Records.RecordMovement;
        Records.ShowNeutralRow = other.Records.ShowNeutralRow;
        Records.RepeatMerge = other.Records.RepeatMerge;
        Records.RepeatWindowMs = other.Records.RepeatWindowMs;
        Records.HoldDisplay = other.Records.HoldDisplay;
        Records.HoldFormat = other.Records.HoldFormat;
        Records.FramesPerSecond = other.Records.FramesPerSecond;
        Records.HoldThresholdMs = other.Records.HoldThresholdMs;
        Records.NewestAtBottom = other.Records.NewestAtBottom;
        Records.DurationOnLeft = other.Records.DurationOnLeft;
        Records.Fade = other.Records.Fade;

        Input.GameProcess = new List<string>(other.Input.GameProcess);
        Input.ForegroundOnly = other.Input.ForegroundOnly;
        Input.ControllerIconStyle = other.Input.ControllerIconStyle;
        Input.XInputPollHz = other.Input.XInputPollHz;
        Input.TriggerThreshold = other.Input.TriggerThreshold;
        Input.StickDeadzone = other.Input.StickDeadzone;
        Input.StickMode = other.Input.StickMode;

        Hotkeys.ToggleOverlay = other.Hotkeys.ToggleOverlay;
        Hotkeys.ToggleLock = other.Hotkeys.ToggleLock;
        Hotkeys.OpenSettings = other.Hotkeys.OpenSettings;
        Hotkeys.DebugMode = other.Hotkeys.DebugMode;
        Hotkeys.ToggleRecording = other.Hotkeys.ToggleRecording;
        Hotkeys.ClearRecords = other.Hotkeys.ClearRecords;

        Profiles = other.Profiles;
        IconOverrides = new Dictionary<string, string>(other.IconOverrides);

        Debug.Enabled = other.Debug.Enabled;
        Debug.Remember = other.Debug.Remember;
        Debug.RehearsalScript = other.Debug.RehearsalScript;
        Debug.Loop = other.Debug.Loop;

        Ui.SettingsTopMost = other.Ui.SettingsTopMost;

        WebOverlay.Enabled = other.WebOverlay.Enabled;
        WebOverlay.HideLocalOverlay = other.WebOverlay.HideLocalOverlay;
        WebOverlay.Port = other.WebOverlay.Port;

        Activity.FloatButtonEnabled = other.Activity.FloatButtonEnabled;
        Activity.FloatX = other.Activity.FloatX;
        Activity.FloatY = other.Activity.FloatY;
        Activity.FloatSize = other.Activity.FloatSize;
        Activity.FloatOpacity = other.Activity.FloatOpacity;
    }
    public void EnsureDefaults()
    {
        var previousVersion = Version;
        Overlay ??= new OverlayConfig();
        Records ??= new RecordsConfig();
        Input ??= new InputConfig();
        Hotkeys ??= new HotkeysConfig();
        Profiles ??= new ProfilesConfig();
        Debug ??= new DebugConfig();
        Ui ??= new UiConfig();
        WebOverlay ??= new WebOverlayConfig();
        Activity ??= new ActivityConfig();
        IconOverrides ??= new Dictionary<string, string>();
        Input.GameProcess ??= new List<string>();
        Profiles.Kbm ??= MappingProfile.CreateDefaultKbm();
        Profiles.Xbox ??= MappingProfile.CreateDefaultXbox();
        Profiles.Ps ??= MappingProfile.CreateDefaultPs();
        Hotkeys.ToggleOverlay ??= "F8";
        Hotkeys.ToggleLock ??= "F9";
        Hotkeys.OpenSettings ??= "F10";
        Hotkeys.DebugMode ??= "F11";
        Hotkeys.ToggleRecording ??= "F2";
        Hotkeys.ClearRecords ??= "F5";

        if (previousVersion < 3 && Hotkeys.ToggleRecording == "F12")
        {
            Hotkeys.ToggleRecording = "F2";
        }
        Version = CurrentVersion;
    }}
