using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Engine;

namespace ZZZKeyEcho.App.Views;

public partial class DebugPanel : Window
{
    private const double TapHoldMs = 60;

    private readonly AppConfig _config;
    private readonly InputPipeline _pipeline;
    private readonly HashSet<string> _heldDirections = new(StringComparer.Ordinal);

    public DebugPanel(AppConfig config, InputPipeline pipeline)
    {
        _config = config;
        _pipeline = pipeline;

        InitializeComponent();
        BuildActionButtons();
        BuildMovementButtons();
        BuildHoldOptions();
        UpdateDiagnostics();

        var diagnosticsTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(500),
        };
        diagnosticsTimer.Tick += (_, _) => UpdateDiagnostics();
        diagnosticsTimer.Start();

        Closed += (_, _) => ClearDirections();
    }

    private void BuildActionButtons()
    {
        foreach (var action in ActionCatalog.All)
        {
            if (action.IsDirection)
            {
                continue;
            }

            var info = action;
            var button = new Button
            {
                Content = info.DisplayName,
                ToolTip = $"注入一次「{info.DisplayName}」的按下与松开，观察悬浮窗是否按预期显示。",
                Margin = new Thickness(0, 0, 6, 6),
                Padding = new Thickness(10, 4, 10, 4),
            };
            button.Click += (_, _) => TriggerAction(info);
            ActionPanel.Children.Add(button);
        }
    }

    private void BuildMovementButtons()
    {
        AddMovementButton("↑", () => ToggleDirection("W"));
        AddMovementButton("↓", () => ToggleDirection("S"));
        AddMovementButton("←", () => ToggleDirection("A"));
        AddMovementButton("→", () => ToggleDirection("D"));
        AddMovementButton("↑+←", () => HoldBoth("W", "A"));
        AddMovementButton("↑+→", () => HoldBoth("W", "D"));
        AddMovementButton("↓+←", () => HoldBoth("S", "A"));
        AddMovementButton("↓+→", () => HoldBoth("S", "D"));
        AddMovementButton("清空方向", ClearDirections);
    }

    private void BuildHoldOptions()
    {
        var actions = new List<ActionInfo>();
        foreach (var action in ActionCatalog.All)
        {
            if (!action.IsDirection)
            {
                actions.Add(action);
            }
        }

        HoldActionBox.ItemsSource = actions;
        HoldActionBox.DisplayMemberPath = nameof(ActionInfo.DisplayName);
        HoldActionBox.SelectedIndex = 1;
    }

    private void AddMovementButton(string label, Action onClick)
    {
        var button = new Button
        {
            Content = label,
            ToolTip = "点一下按住这个方向，再点一下松开；配合上面的动作按钮可以验证“移动 + 动作”的组合行。",
            Margin = new Thickness(0, 0, 6, 6),
            Padding = new Thickness(10, 4, 10, 4),
        };
        button.Click += (_, _) => onClick();
        MovementPanel.Children.Add(button);
    }

    private void TriggerAction(ActionInfo action)
    {
        var key = PrimaryKey(action.Id);
        if (key is null)
        {
            StatusText.Text = $"「{action.DisplayName}」当前未启用或未绑定按键，无法触发。";
            return;
        }

        Tap(key);
        StatusText.Text = $"已注入：{action.DisplayName} → 主绑定键 {key}";
    }

    private void Tap(string code)
    {
        _pipeline.Simulated.Down(code);
        Delay(TapHoldMs, () => _pipeline.Simulated.Up(code));
    }

    private void ToggleDirection(string code)
    {
        if (_heldDirections.Contains(code))
        {
            ReleaseDirection(code);
        }
        else
        {
            HoldDirection(code);
        }
    }

    private void HoldBoth(string first, string second)
    {
        HoldDirection(first);
        HoldDirection(second);
    }

    private void HoldDirection(string code)
    {
        if (_heldDirections.Add(code))
        {
            _pipeline.Simulated.Down(code);
        }
    }

    private void ReleaseDirection(string code)
    {
        if (_heldDirections.Remove(code))
        {
            _pipeline.Simulated.Up(code);
        }
    }

    private void ClearDirections()
    {
        foreach (var code in new List<string>(_heldDirections))
        {
            ReleaseDirection(code);
        }
    }

    private void OnHoldClick(object sender, RoutedEventArgs e)
    {
        if (HoldActionBox.SelectedItem is not ActionInfo action)
        {
            return;
        }

        var key = PrimaryKey(action.Id);
        if (key is null)
        {
            StatusText.Text = $"「{action.DisplayName}」当前未启用或未绑定按键，无法触发。";
            return;
        }

        if (!double.TryParse(HoldSecondsBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            || seconds <= 0)
        {
            seconds = 0.8;
        }

        _pipeline.Simulated.Down(key);
        Delay(seconds * 1000, () => _pipeline.Simulated.Up(key));
        StatusText.Text = $"长按模拟：{action.DisplayName} 保持 {seconds:0.0} 秒（观察悬浮窗计时增长后定格）";
    }

    private string? PrimaryKey(string actionId)
    {
        foreach (var binding in _config.Profiles.Kbm.Bindings)
        {
            if (binding.Action == actionId && binding.Enabled && binding.Keys.Count > 0)
            {
                return binding.Keys[0];
            }
        }

        return null;
    }

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

    private void UpdateDiagnostics()
    {
        var held = _heldDirections.Count == 0 ? "无" : string.Join(" ", _heldDirections);

        DiagnosticsText.Text =
            $"钩子：{(_pipeline.IsHookRunning ? "已安装" : "未安装")}\n" +
            "前台门控：已解除（调试模式）\n" +
            $"引擎记录数：{_pipeline.Engine.Records.Count}\n" +
            $"按住方向：{held}";
    }
}
