using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using H.NotifyIcon;
using ZZZKeyEcho.App.Rendering;
using ZZZKeyEcho.App.Views;
using ZZZKeyEcho.App.Updates;
using ZZZKeyEcho.App.Stamps;
using ZZZKeyEcho.Core.Activity;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Input;
using ZZZKeyEcho.Core.Update;
using ZZZKeyEcho.Core.Win32;

namespace ZZZKeyEcho.App;

public partial class App : Application
{
    private const string HotkeyToggleOverlay = "toggleOverlay";
    private const string HotkeyToggleLock = "toggleLock";
    private const string HotkeyOpenSettings = "openSettings";
    private const string HotkeyDebugMode = "debugMode";
    private const string HotkeyToggleRecording = "toggleRecording";
    private const string HotkeyClearRecords = "clearRecords";
    private const string HotkeyHideStamps = "hideStamps";

    private AppConfig _config = null!;
    private SystemClock _clock = null!;
    private InputPipeline _pipeline = null!;
    private AppState _state = null!;
    private IconProvider _icons = null!;
    private HotkeyManager _hotkeys = null!;
    private TaskbarIcon _trayIcon = null!;
    private OverlayWindow _overlay = null!;
    private SettingsWindow? _settings;
    private DebugPanel? _debugPanel;
    private WebOverlayServer _webOverlay = null!;
    private ActivityRecorder _activity = null!;
    private RecordButtonWindow? _recordButton;
    private StampManager? _stamps;
    private StampEditorWindow? _stampEditor;
    private ToastWindow? _toast;
    private bool _lastRecordingState;
    private DispatcherTimer? _renderingTimer;
    private bool _debugMode;
    private bool _overlayHiddenByUser;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        TooltipDefaults.Enable();

        var otherInstances = ProcessGuard.FindOtherInstances();

        if (otherInstances.Count > 0)
        {
            var dialog = new AlreadyRunningWindow(otherInstances.Count) { Topmost = true };
            dialog.ShowDialog();

            if (dialog.Result != AlreadyRunningResult.Restart)
            {
                Shutdown();
                return;
            }

            if (!ProcessGuard.TerminateAll(TimeSpan.FromSeconds(2)))
            {
                MessageBox.Show("部分实例无法结束，请用任务管理器关闭后再试。", "ZZZKeyEcho");
            }
        }

        _config = JsonStore.Load();

        var firstRun = !File.Exists(JsonStore.DefaultPath);

        if (firstRun)
        {
            var seed = Path.Combine(AppContext.BaseDirectory, "assets", "default-config.json");

            if (File.Exists(seed))
            {
                try
                {
                    File.Copy(seed, JsonStore.DefaultPath, false);
                    _config = JsonStore.Load();
                }
                catch (Exception)
                {
                }
            }

            JsonStore.Save(_config);
        }

        PlaceholderIconFactory.EnsureAll(Path.Combine(AppContext.BaseDirectory, "assets", "icons"));

        _icons = new IconProvider(Path.Combine(AppContext.BaseDirectory, "assets", "icons"), _config);
        _icons.EnsureDerivedIconFiles();

        _clock = new SystemClock();
        _pipeline = new InputPipeline(_config, _clock);
        _pipeline.Start();

        _activity = new ActivityRecorder(_pipeline.Engine, _clock, Path.Combine(AppContext.BaseDirectory, "ActivityLogs"));
        _activity.Changed += OnActivityChanged;
        _lastRecordingState = _activity.IsRecording;

        _overlay = new OverlayWindow(_config, _pipeline.Engine, _clock, _icons);
        _stamps = new StampManager(_config, () => _pipeline.GameMonitor.IsRunning);
        _webOverlay = new WebOverlayServer(
            _config,
            _pipeline.Engine,
            _icons,
            Path.Combine(AppContext.BaseDirectory, "assets"),
            _stamps,
            () => _pipeline.GameMonitor.IsRunning);
        _webOverlay.Sync();

        if (!_config.WebOverlay.HideLocalOverlay)
        {
            _overlay.Show();
        }

        _pipeline.GameMonitor.Changed += OnGameProcessChanged;

        _state = new AppState(_config, _pipeline);
        _state.Changed += OnStateChanged;

        if (!_config.Debug.Remember)
        {
            _config.Debug.Enabled = false;
        }

        _hotkeys = new HotkeyManager();
        _hotkeys.Pressed += OnHotkeyPressed;
        _hotkeys.Register(HotkeyToggleOverlay, _config.Hotkeys.ToggleOverlay);
        _hotkeys.Register(HotkeyToggleLock, _config.Hotkeys.ToggleLock);
        _hotkeys.Register(HotkeyOpenSettings, _config.Hotkeys.OpenSettings);
        _hotkeys.Register(HotkeyDebugMode, _config.Hotkeys.DebugMode);
        _hotkeys.Register(HotkeyToggleRecording, _config.Hotkeys.ToggleRecording);
        _hotkeys.Register(HotkeyClearRecords, _config.Hotkeys.ClearRecords);
        _hotkeys.Register(HotkeyHideStamps, _config.Hotkeys.HideStamps);

        _trayIcon = CreateTrayIcon();
        _toast = new ToastWindow();
        CheckForUpdatesOnStartup();
        ApplyActivitySettings();

        if (_config.Debug.Enabled)
        {
            SetDebugMode(true);
        }

        if (firstRun)
        {
            ShowOnboarding();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _activity.Changed -= OnActivityChanged;
        _stampEditor?.Close();
        _toast?.Close();
        _webOverlay.Dispose();
        _stamps?.Dispose();
        _activity?.Stop();
        JsonStore.Save(_config);
        _pipeline.Dispose();
        _hotkeys.Dispose();
        _trayIcon.Dispose();
        base.OnExit(e);
    }

    private TaskbarIcon CreateTrayIcon()
    {
        var menu = new ContextMenu();

        var recordItem = new MenuItem { Header = "开始 / 结束操作录制" };
        recordItem.Click += (_, _) => _activity.Toggle();
        menu.Items.Add(recordItem);

        var clearRecordsItem = new MenuItem { Header = "清除悬浮窗记录" };
        clearRecordsItem.Click += (_, _) => ClearOverlayRecords();
        menu.Items.Add(clearRecordsItem);

        var hideStampsItem = new MenuItem { Header = "显示 / 隐藏印章输出" };
        hideStampsItem.Click += (_, _) => ToggleHideStamps();
        menu.Items.Add(hideStampsItem);

        var showItem = new MenuItem { Header = "显示 / 隐藏按键悬浮窗" };
        showItem.Click += (_, _) => ToggleOverlay();
        menu.Items.Add(showItem);

        var lockItem = new MenuItem { Header = "锁定 / 解锁按键悬浮窗" };
        lockItem.Click += (_, _) => ToggleLock();
        menu.Items.Add(lockItem);

        var settingsItem = new MenuItem { Header = "打开 / 关闭设置" };
        settingsItem.Click += (_, _) => ToggleSettings();
        menu.Items.Add(settingsItem);

        var debugItem = new MenuItem { Header = "调试模式", IsCheckable = true };
        debugItem.Click += (_, _) =>
        {
            ToggleDebugMode();
            debugItem.IsChecked = _debugMode;
        };
        menu.Items.Add(debugItem);

        menu.Items.Add(new Separator());

        var debugPanelItem = new MenuItem { Header = "调试面板" };
        debugPanelItem.Click += (_, _) => ShowDebugPanel();
        menu.Items.Add(debugPanelItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = $"退出（实例 {InstanceNumber()}）" };
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);

        var trayIcon = new TaskbarIcon
        {
            ToolTipText = $"ZZZKeyEcho（实例 {InstanceNumber()}，悬浮窗 {_config.Overlay.Width:0}×{_config.Overlay.Height:0}）",
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? string.Empty)
                ?? TrayIconFactory.Create(),
            ContextMenu = menu,
        };
        trayIcon.TrayMouseDoubleClick += (_, _) => ShowSettings();
        trayIcon.ForceCreate();

        return trayIcon;
    }

    private void OnHotkeyPressed(object? sender, string name)
    {
        if (_stampEditor is not null)
        {
            return;
        }

        switch (name)
        {
            case HotkeyToggleOverlay:
                ToggleOverlay();
                break;
            case HotkeyToggleLock:
                ToggleLock();
                break;
            case HotkeyOpenSettings:
                ToggleSettings();
                break;
            case HotkeyDebugMode:
                ToggleDebugMode();
                break;
            case HotkeyToggleRecording:
                _activity.Toggle();
                break;
            case HotkeyClearRecords:
                ClearOverlayRecords();
                break;
            case HotkeyHideStamps:
                ToggleHideStamps();
                break;
        }
    }

    private void ToggleHideStamps()
    {
        _config.Stamps.Hidden = !_config.Stamps.Hidden;
        _stamps?.ApplyVisibility();
        ShowToast(_config.Stamps.Hidden ? "印章输出已隐藏" : "印章输出已显示");
    }

    private void ShowToast(string message) => _toast?.ShowMessage(message, 3);

    private void ToggleOverlay()
    {
        if (_stampEditor is not null)
        {
            return;
        }

        if (_overlay.IsVisible)
        {
            _overlay.Hide();
            _overlayHiddenByUser = true;
            ShowToast("按键悬浮窗已隐藏");
        }
        else
        {
            _overlay.Show();
            _overlayHiddenByUser = false;
            ShowToast("按键悬浮窗已显示");
        }
    }

    private void ToggleLock()
    {
        _overlay.SetLocked(!_config.Overlay.Locked);
        ShowToast(_config.Overlay.Locked ? "按键悬浮窗已锁定" : "按键悬浮窗已解锁");
    }

    private void ToggleDebugMode() => SetDebugMode(!_debugMode);

    private void SetDebugMode(bool enabled)
    {
        _debugMode = enabled;
        _config.Debug.Enabled = enabled;
        _pipeline.DebugMode = enabled;
        _overlay.SetDebugVisual(enabled);

        if (enabled)
        {
            if (!_overlay.IsVisible)
            {
                _overlay.Show();
            }

            _overlayHiddenByUser = false;
            ShowPanelWindow();
        }
        else
        {
            _debugPanel?.Close();
            _pipeline.Engine.Clear();
        }

        _trayIcon.ShowNotification(
            "ZZZKeyEcho",
            enabled ? "调试模式已开启（前台门控已解除）" : "调试模式已关闭（调试记录已清空）");
        ShowToast(enabled ? "调试模式已开启" : "调试模式已关闭");
    }

    private void ShowDebugPanel()
    {
        if (!_debugMode)
        {
            SetDebugMode(true);
            return;
        }

        ShowPanelWindow();
    }

    private void ShowPanelWindow()
    {
        if (_debugPanel is null)
        {
            _debugPanel = new DebugPanel(_config, _pipeline);
            _debugPanel.Closed += (_, _) => _debugPanel = null;
        }

        _debugPanel.Show();
        _debugPanel.Activate();
    }

    private static int InstanceNumber()
    {
        var self = Environment.ProcessId;
        var earlier = 0;

        try
        {
            foreach (var process in System.Diagnostics.Process.GetProcessesByName("ZZZKeyEcho"))
            {
                using (process)
                {
                    if (process.Id < self)
                    {
                        earlier++;
                    }
                }
            }
        }
        catch (Exception)
        {
        }

        return earlier + 1;
    }

    private void ShowOnboarding()
    {
        var onboarding = new OnboardingWindow(_config);
        onboarding.Show();
        onboarding.Activate();
    }

    private void ToggleSettings()
    {
        if (_settings is { IsVisible: true })
        {
            _settings.Hide();
            ShowToast("设置已关闭");
            return;
        }

        ShowSettings();
        ShowToast("设置已打开");
    }

    private void ShowSettings()
    {
        if (_settings is null)
        {
            _settings = new SettingsWindow(
                _state,
                _pipeline.GetStatus,
                () => _overlay.FlashBorder(),
                _icons,
                ApplyHotkeys,
                SetDebugMode,
                _activity,
                ShowStampEditor,
                () => _stamps?.WebUrl ?? string.Empty);
            _settings.Closed += (_, _) => _settings = null;
        }

        _settings.RefreshStamps();
        _settings.Show();
        _settings.Activate();
    }

    private void ShowStampEditor()
    {
        if (_stampEditor is not null)
        {
            _stampEditor.Activate();
            return;
        }

        _settings?.Hide();
        _overlay.Hide();
        _recordButton?.Hide();
        _stamps?.SetEditing(true);

        var editor = new StampEditorWindow(_config);
        _stampEditor = editor;

        editor.LayoutSaved += (_, _) =>
        {
            _overlay.ApplyConfiguration();
            _stamps?.ApplyAll();
            _webOverlay.Sync();
            _settings?.RefreshStamps();
        };

        editor.Closed += (_, _) =>
        {
            _stampEditor = null;
            _stamps?.SetEditing(false);
            UpdateOverlayVisibility();
            ApplyActivitySettings();
        };

        editor.Show();
        editor.Activate();
    }

    private string ApplyHotkeys()
    {
        _hotkeys.UnregisterAll();

        var failures = new List<string>();

        if (!string.IsNullOrEmpty(_config.Hotkeys.ToggleOverlay)
            && !_hotkeys.Register(HotkeyToggleOverlay, _config.Hotkeys.ToggleOverlay))
        {
            failures.Add($"显示/隐藏（{_config.Hotkeys.ToggleOverlay}）");
        }

        if (!string.IsNullOrEmpty(_config.Hotkeys.ToggleLock)
            && !_hotkeys.Register(HotkeyToggleLock, _config.Hotkeys.ToggleLock))
        {
            failures.Add($"锁定/解锁（{_config.Hotkeys.ToggleLock}）");
        }

        if (!string.IsNullOrEmpty(_config.Hotkeys.OpenSettings)
            && !_hotkeys.Register(HotkeyOpenSettings, _config.Hotkeys.OpenSettings))
        {
            failures.Add($"打开 / 关闭设置（{_config.Hotkeys.OpenSettings}）");
        }

        if (!string.IsNullOrEmpty(_config.Hotkeys.DebugMode)
            && !_hotkeys.Register(HotkeyDebugMode, _config.Hotkeys.DebugMode))
        {
            failures.Add($"调试模式（{_config.Hotkeys.DebugMode}）");
        }

        if (!string.IsNullOrEmpty(_config.Hotkeys.ToggleRecording)
            && !_hotkeys.Register(HotkeyToggleRecording, _config.Hotkeys.ToggleRecording))
        {
            failures.Add($"开始/结束录制（{_config.Hotkeys.ToggleRecording}）");
        }

        if (!string.IsNullOrEmpty(_config.Hotkeys.ClearRecords)
            && !_hotkeys.Register(HotkeyClearRecords, _config.Hotkeys.ClearRecords))
        {
            failures.Add($"清除悬浮窗记录（{_config.Hotkeys.ClearRecords}）");
        }

        if (!string.IsNullOrEmpty(_config.Hotkeys.HideStamps)
            && !_hotkeys.Register(HotkeyHideStamps, _config.Hotkeys.HideStamps))
        {
            failures.Add($"隐藏印章（{_config.Hotkeys.HideStamps}）");
        }

        return failures.Count == 0
            ? "热键已应用，全部注册成功。"
            : "以下热键被其他程序占用，注册失败：" + string.Join("、", failures);
    }

    private void OnActivityChanged(object? sender, EventArgs e)
    {
        var recording = _activity.IsRecording;

        if (recording == _lastRecordingState)
        {
            return;
        }

        _lastRecordingState = recording;
        _toast?.ShowMessage(recording ? "● 开始录制" : "■ 已停止录制", 3);
    }

    private void ClearOverlayRecords()
    {
        _pipeline.Engine.Clear();
        ShowToast("悬浮窗记录已清除");
    }

    private async void CheckForUpdatesOnStartup()
    {
        var result = await Task.Run(() => UpdateChecker.CheckAsync(TimeSpan.FromSeconds(10)));

        if (result.Update is null)
        {
            return;
        }

        UpdatePrompt.Show(null, result.Update);
    }
    private void ApplyActivitySettings()
    {
        if (!_config.Activity.FloatButtonEnabled)
        {
            _recordButton?.Hide();
            return;
        }

        if (_recordButton is null)
        {
            _recordButton = new RecordButtonWindow(_activity, _config);
            _recordButton.Closed += (_, _) => _recordButton = null;
            _recordButton.Show();
            return;
        }

        _recordButton.ApplyConfiguration();
        _recordButton.Show();
    }

    private void OnStateChanged(object? sender, SettingsChange change)
    {
        if (change.HasFlag(SettingsChange.OverlayStyle))
        {
            _overlay.ApplyConfiguration();
        }

        _webOverlay.Sync();

        if (change.HasFlag(SettingsChange.Mapping))
        {
            _pipeline.RebuildMapping();
        }

        if (change.HasFlag(SettingsChange.Rendering))
        {
            ScheduleRenderingRefresh();
        }

        if (change.HasFlag(SettingsChange.Activity))
        {
            ApplyActivitySettings();
        }

        if (change.HasFlag(SettingsChange.Stamps))
        {
            _stamps?.ApplyAll();
        }
        else
        {
            _stamps?.ApplyVisibility();
        }

        UpdateOverlayVisibility();
    }

    private void OnGameProcessChanged(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(() =>
        {
            _stamps?.SetGameRunning(_pipeline.GameMonitor.IsRunning);
            UpdateOverlayVisibility();
        });

    private void UpdateOverlayVisibility()
    {
        var shouldShow = !_overlayHiddenByUser
            && !_config.WebOverlay.HideLocalOverlay
            && _state.RecordingEnabled
            && (!_config.Overlay.FollowGame || _pipeline.GameMonitor.IsRunning || _debugMode);

        if (shouldShow && !_overlay.IsVisible)
        {
            _overlay.Show();
        }
        else if (!shouldShow && _overlay.IsVisible)
        {
            _overlay.Hide();
        }
    }

    private void ScheduleRenderingRefresh()
    {
        if (_renderingTimer is null)
        {
            _renderingTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(150),
            };
            _renderingTimer.Tick += (_, _) =>
            {
                _renderingTimer?.Stop();
                _overlay.RefreshRendering();
            };
        }

        _renderingTimer.Stop();
        _renderingTimer.Start();
    }
}
