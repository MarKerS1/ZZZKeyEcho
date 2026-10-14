using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using ZZZKeyEcho.App.Rendering;
using ZZZKeyEcho.App.ViewModels;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Input;

namespace ZZZKeyEcho.App.Views;

public partial class SettingsWindow : Window
{
    private readonly AppState _state;
    private readonly Func<RuntimeStatus> _status;
    private readonly Action _flashOverlay;
    private readonly IconProvider _icons;
    private readonly Func<string> _applyHotkeys;
    private readonly SettingsViewModel _viewModel;
    private readonly HashSet<string> _hotkeyModifiers = new(StringComparer.Ordinal);
    private BindingRowViewModel? _capturing;
    private string? _capturingHotkey;

    public SettingsWindow(
        AppState state,
        Func<RuntimeStatus> statusProvider,
        Action flashOverlay,
        IconProvider icons,
        Func<string> applyHotkeys,
        Action<bool> setDebugMode)
    {
        _state = state;
        _status = statusProvider;
        _flashOverlay = flashOverlay;
        _icons = icons;
        _applyHotkeys = applyHotkeys;

        _viewModel = new SettingsViewModel(
            state.Config,
            ApplyChanges,
            icons,
            () => state.Pipeline.DebugMode,
            setDebugMode);

        InitializeComponent();
        DataContext = _viewModel;
        RecordingCheckBox.DataContext = _state;
        Topmost = _viewModel.SettingsTopMost;
        UpdateStatus();

        state.Pipeline.RawEvent += OnRawEvent;
        Closed += (_, _) => state.Pipeline.RawEvent -= OnRawEvent;

        var statusTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(500),
        };
        statusTimer.Tick += (_, _) => UpdateStatus();
        statusTimer.Start();
    }

    private void ApplyChanges(SettingsChange change)
    {
        Topmost = _viewModel.SettingsTopMost;
        _state.NotifyChanged(change);
    }

    private void UpdateStatus()
    {
        var status = _status();
        var area = SystemParameters.WorkArea;

        StatusText.Text =
            $"当前设备：{status.GamepadName}\n" +
            $"钩子状态：{(status.HookRunning ? "已安装" : "未安装")}\n" +
            $"记录开关：{(status.RecordingEnabled ? "开启" : "已关闭")}\n" +
            $"游戏进程：{(status.GameRunning ? $"运行中（{status.GameProcess}）" : "未运行")}\n" +
            $"前台游戏：{(status.GameForeground ? "已检测到" : "未启动 / 未在前台")}\n" +
            $"前台进程：{status.ForegroundProcess}\n" +
            (status.GameForeground ? string.Empty : "（若游戏已在前台但这里显示的不是游戏进程，说明读不到进程名——请尝试以管理员身份运行本程序）\n") +
            $"调试模式：{(status.DebugMode ? "开启（前台门控已解除）" : "关闭")}\n" +
            $"屏幕工作区：{area.Width:0} × {area.Height:0}（悬浮窗当前 {_viewModel.OverlayWidth:0} × {_viewModel.OverlayHeight:0}）";

        _viewModel.SyncOverlayGeometry();

        var url = $"http://127.0.0.1:{_viewModel.WebOverlayPort}/";
        if (WebUrlBox.Text != url)
        {
            WebUrlBox.Text = url;
        }

        var size = $"{_viewModel.OverlayWidth:0} × {_viewModel.OverlayHeight:0}";
        if (ObsSizeRun.Text != size)
        {
            ObsSizeRun.Text = size;
        }
    }

    private void OnPerfTest(object sender, RoutedEventArgs e)
    {
        var simulated = _state.Pipeline.Simulated;
        var engine = _state.Pipeline.Engine;
        var codes = new[] { "MouseLeft", "E", "Q", "LShift" };
        const int rounds = 2000;

        engine.Clear();
        var watch = System.Diagnostics.Stopwatch.StartNew();

        for (var i = 0; i < rounds; i++)
        {
            var code = codes[i % codes.Length];
            simulated.Down(code);
            simulated.Up(code);
        }

        watch.Stop();
        var production = engine.Records.Count;
        engine.Clear();

        var events = rounds * 2;
        var perEvent = watch.Elapsed.TotalMilliseconds * 1000 / events;

        PerfText.Text =
            $"合成 {events} 个输入事件：{watch.Elapsed.TotalMilliseconds:0.0} ms（平均 {perEvent:0.0} µs/事件），" +
            $"期间产生 {production} 条记录。\n" +
            $"当前记录上限 {_state.Config.Records.MaxCount} 条、图标 {_state.Config.Records.IconSize:0} px、" +
            $"悬浮窗刷新间隔 30 ms（约 33 FPS）。体会一下：这个数字远小于 1 ms/事件，说明记录不会成为瓶颈。";
    }

    private void OnCopyWebUrl(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(WebUrlBox.Text);
            WebUrlBox.ToolTip = "已复制到剪贴板。";
        }
        catch (Exception)
        {
            WebUrlBox.ToolTip = "复制失败，请手动选中地址复制。";
        }
    }

    private void OnOpenWebUrl(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(WebUrlBox.Text)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception)
        {
        }
    }

    private void OnRawEvent(object? sender, InputEvent inputEvent)
    {
        if (_capturingHotkey is not null)
        {
            HandleHotkeyCapture(inputEvent);
            return;
        }

        if (_capturing is null || inputEvent.Kind != InputEventKind.Down)
        {
            return;
        }

        if (inputEvent.Device != InputDevice.Simulated && inputEvent.Device != _capturing.Device)
        {
            return;
        }

        if (inputEvent.Code == "Escape")
        {
            EndCapture();
            return;
        }

        _capturing.AddKey(inputEvent.Code);
        EndCapture();
    }

    private void StartCapture(BindingRowViewModel row)
    {
        _capturing = row;
        ShowHint($"正在捕获「{row.ActionName}」的按键：请在{DeviceName(row.Device)}上按任意键（Esc 取消）");
    }

    private void EndCapture()
    {
        _capturing = null;
        HideHint();
    }

    private void ShowHint(string text)
    {
        CaptureText.Text = text;
        CaptureText.Visibility = Visibility.Visible;
    }

    private void HideHint() => CaptureText.Visibility = Visibility.Collapsed;

    private void OnChangeHotkeyClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string propertyName)
        {
            return;
        }

        var menu = new ContextMenu();

        var byKeyItem = new MenuItem { Header = "按键输入（按下目标键，可带 Ctrl / Alt / Shift / Win）" };
        byKeyItem.Click += (_, _) => StartHotkeyCapture(propertyName);
        menu.Items.Add(byKeyItem);

        var manualItem = new MenuItem { Header = "手动选择按键 ▸" };
        AddKeyGroups(manualItem, KeyCatalog.Kbm, key =>
        {
            ApplyHotkeyGesture(propertyName, key);
            menu.IsOpen = false;
        });
        menu.Items.Add(manualItem);

        var clearItem = new MenuItem { Header = "清除（禁用该热键）" };
        clearItem.Click += (_, _) =>
        {
            ApplyHotkeyGesture(propertyName, string.Empty);
            menu.IsOpen = false;
        };
        menu.Items.Add(clearItem);

        menu.PlacementTarget = element;
        menu.IsOpen = true;
    }

    private void StartHotkeyCapture(string propertyName)
    {
        _capturingHotkey = propertyName;
        _hotkeyModifiers.Clear();
        ShowHint("正在捕获热键：请按下目标按键（可配合 Ctrl / Alt / Shift / Win，Esc 取消）");
    }

    private void ApplyHotkeyGesture(string propertyName, string gesture)
    {
        switch (propertyName)
        {
            case "ToggleOverlayHotkey":
                _viewModel.ToggleOverlayHotkey = gesture;
                break;
            case "ToggleLockHotkey":
                _viewModel.ToggleLockHotkey = gesture;
                break;
            case "OpenSettingsHotkey":
                _viewModel.OpenSettingsHotkey = gesture;
                break;
            case "DebugModeHotkey":
                _viewModel.DebugModeHotkey = gesture;
                break;
        }

        HotkeysStatusText.Text = _applyHotkeys();
    }

    private void HandleHotkeyCapture(InputEvent inputEvent)
    {
        if (inputEvent.Device is not (InputDevice.Kbm or InputDevice.Simulated))
        {
            return;
        }

        if (CanonicalModifier(inputEvent.Code) is { } modifier)
        {
            if (inputEvent.Kind == InputEventKind.Down)
            {
                _hotkeyModifiers.Add(modifier);
            }
            else
            {
                _hotkeyModifiers.Remove(modifier);
            }

            return;
        }

        if (inputEvent.Kind != InputEventKind.Down)
        {
            return;
        }

        if (inputEvent.Code == "Escape")
        {
            EndHotkeyCapture();
            return;
        }

        var parts = new List<string>();

        foreach (var name in new[] { "Ctrl", "Alt", "Shift", "Win" })
        {
            if (_hotkeyModifiers.Contains(name))
            {
                parts.Add(name);
            }
        }

        parts.Add(inputEvent.Code);
        var gesture = string.Join("+", parts);
        var propertyName = _capturingHotkey;
        EndHotkeyCapture();

        if (propertyName is not null)
        {
            ApplyHotkeyGesture(propertyName, gesture);
        }
    }

    private void EndHotkeyCapture()
    {
        _capturingHotkey = null;
        _hotkeyModifiers.Clear();
        HideHint();
    }

    private static string? CanonicalModifier(string code) => code switch
    {
        "LControl" or "RControl" => "Ctrl",
        "LShift" or "RShift" => "Shift",
        "LAlt" or "RAlt" => "Alt",
        "Win" => "Win",
        _ => null,
    };

    private static string DeviceName(InputDevice device) => device switch
    {
        InputDevice.Kbm => "键鼠",
        InputDevice.Xbox => "Xbox 手柄",
        InputDevice.Ps => "PS 手柄",
        _ => "设备",
    };

    private void OnAddKeyClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not BindingRowViewModel row)
        {
            return;
        }

        var menu = new ContextMenu();

        var byKeyItem = new MenuItem { Header = "按键输入（按下要绑定的键，自动识别）" };
        byKeyItem.Click += (_, _) => StartCapture(row);
        menu.Items.Add(byKeyItem);

        var manualItem = new MenuItem { Header = "手动选择按键 ▸" };
        AddKeyGroups(manualItem, row.AvailableKeys, key =>
        {
            row.AddKey(key);
            menu.IsOpen = false;
        });
        menu.Items.Add(manualItem);

        menu.PlacementTarget = element;
        menu.IsOpen = true;
    }

    private static void AddKeyGroups(MenuItem parent, IReadOnlyList<string> keys, Action<string> onPick)
    {
        var letters = new List<string>();
        var digits = new List<string>();
        var functions = new List<string>();
        var arrows = new List<string>();
        var mouse = new List<string>();
        var others = new List<string>();

        foreach (var key in keys)
        {
            if (key.Length == 1 && char.IsLetter(key[0]))
            {
                letters.Add(key);
            }
            else if (key.Length == 1 && char.IsDigit(key[0]))
            {
                digits.Add(key);
            }
            else if (key.Length > 1 && key[0] == 'F' && char.IsDigit(key[1]))
            {
                functions.Add(key);
            }
            else if (key is "Up" or "Down" or "Left" or "Right")
            {
                arrows.Add(key);
            }
            else if (key.StartsWith("Mouse", StringComparison.Ordinal))
            {
                mouse.Add(key);
            }
            else
            {
                others.Add(key);
            }
        }

        AddKeyGroup(parent, "字母", letters, onPick);
        AddKeyGroup(parent, "数字", digits, onPick);
        AddKeyGroup(parent, "功能键", functions, onPick);
        AddKeyGroup(parent, "方向键", arrows, onPick);
        AddKeyGroup(parent, "其他常用键", others, onPick);
        AddKeyGroup(parent, "鼠标", mouse, onPick);
    }

    private static void AddKeyGroup(MenuItem parent, string header, List<string> keys, Action<string> onPick)
    {
        if (keys.Count == 0)
        {
            return;
        }

        var group = new MenuItem { Header = header };

        foreach (var key in keys)
        {
            var item = new MenuItem { Header = key };
            item.Click += (_, _) => onPick(key);
            group.Items.Add(item);
        }

        parent.Items.Add(group);
    }

    private void OnSetPrimaryClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not BindingRowViewModel row)
        {
            return;
        }

        row.SetPrimary();
        ShowHint($"已把「{row.ActionName}」排到最前——一键多用时它将作为主图标（大图标）显示。");
    }

    private void OnTestActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not BindingRowViewModel row)
        {
            return;
        }

        if (row.PrimaryKey is not { } key || !row.Enabled)
        {
            ShowHint(row.Enabled ? $"「{row.ActionName}」没有绑定按键，无法测试。" : $"「{row.ActionName}」当前已关闭（✅ 未勾选），输入会被完全无视——请先勾选再测试。");
            return;
        }

        _state.Pipeline.Simulated.Down(key, row.Device);
        Delay(60, () => _state.Pipeline.Simulated.Up(key, row.Device));

        CaptureText.Text = $"已注入：{row.ActionName} → {key}（{DeviceName(row.Device)}），请在悬浮窗观察结果";
        CaptureText.Visibility = Visibility.Visible;
    }

    private void OnRestoreKbm(object sender, RoutedEventArgs e) => _viewModel.KbmProfile.RestoreDefaults();

    private void OnRestoreXbox(object sender, RoutedEventArgs e) => _viewModel.XboxProfile.RestoreDefaults();

    private void OnRestorePs(object sender, RoutedEventArgs e) => _viewModel.PsProfile.RestoreDefaults();

    private void OnImportIconClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not IconRowViewModel row)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "PNG 图片 (*.png)|*.png",
            Title = $"为「{row.ActionName}」导入图标",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var target = _icons.OverridePathFor(row.ActionId);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(dialog.FileName, target, overwrite: true);

            _state.Config.IconOverrides.Remove(row.ActionId);
            row.Refresh();
            _state.NotifyChanged(SettingsChange.Rendering);
            FilesStatusText.Text = $"已替换「{row.ActionName}」的图标（custom/{row.CustomFileName}）。";
        }
        catch (Exception exception)
        {
            FilesStatusText.Text = "导入失败：" + exception.Message;
        }
    }

    private void OnRestoreIconClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not IconRowViewModel row)
        {
            return;
        }

        try
        {
            var path = _icons.OverridePathFor(row.ActionId);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            _state.Config.IconOverrides.Remove(row.ActionId);
            row.Refresh();
            _state.NotifyChanged(SettingsChange.Rendering);
            FilesStatusText.Text = $"已恢复「{row.ActionName}」的内置图标。";
        }
        catch (Exception exception)
        {
            FilesStatusText.Text = "恢复失败：" + exception.Message;
        }
    }

    private void OnColorSwatch(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string rgb || rgb.Length != 6)
        {
            return;
        }

        var current = ParseColor(_viewModel.BackgroundColor, Color.FromArgb(0xB3, 0x00, 0x00, 0x00));
        var red = Convert.ToByte(rgb[..2], 16);
        var green = Convert.ToByte(rgb.Substring(2, 2), 16);
        var blue = Convert.ToByte(rgb.Substring(4, 2), 16);
        var color = Color.FromArgb(current.A, red, green, blue);

        _viewModel.BackgroundColor = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static Color ParseColor(string text, Color fallback)
    {
        try
        {
            if (ColorConverter.ConvertFromString(text) is Color color)
            {
                return color;
            }
        }
        catch (Exception)
        {
        }

        return fallback;
    }

    private void OnMoveToCorner(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string corner)
        {
            return;
        }

        var area = SystemParameters.WorkArea;
        var x = corner is "TR" or "BR" ? area.Right - _viewModel.OverlayWidth - 16 : area.Left + 16;
        var y = corner is "BL" or "BR" ? area.Bottom - _viewModel.OverlayHeight - 16 : area.Top + 16;

        _viewModel.OverlayX = Math.Max(area.Left, x);
        _viewModel.OverlayY = Math.Max(area.Top, y);
    }

    private void OnApplyHotkeys(object sender, RoutedEventArgs e) => HotkeysStatusText.Text = _applyHotkeys();

    private void OnExportConfig(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            FileName = "ZZZKeyEcho-config.json",
            Filter = "JSON (*.json)|*.json",
            Title = "导出配置",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        JsonStore.Save(dialog.FileName, _state.Config);
        FilesStatusText.Text = $"已导出配置：{dialog.FileName}";
    }

    private void OnImportConfig(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            Title = "导入配置",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var imported = JsonStore.Load(dialog.FileName);
        _state.Config.CopyFrom(imported);
        _viewModel.Reload();
        FilesStatusText.Text = $"已导入配置：{dialog.FileName}";
    }

    private void OnResetConfig(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            this,
            "将把所有设置与按键映射恢复为默认值，确定继续？",
            "ZZZKeyEcho",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.OK)
        {
            return;
        }

        _state.Config.CopyFrom(new AppConfig());
        _viewModel.Reload();
        FilesStatusText.Text = "已恢复默认设置。";
    }

    private void OnTestVisibility(object sender, RoutedEventArgs e) => _flashOverlay();

    private void OnOpenConfigFolder(object sender, RoutedEventArgs e) =>
        OpenFolder(Path.GetDirectoryName(JsonStore.DefaultPath));

    private void OnOpenIconsFolder(object sender, RoutedEventArgs e) =>
        OpenFolder(Path.Combine(AppContext.BaseDirectory, "assets", "icons"));

    private void Delay(double milliseconds, Action action)
    {
        var timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(milliseconds),
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            action();
        };
        timer.Start();
    }

    private static void OpenFolder(string? path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
