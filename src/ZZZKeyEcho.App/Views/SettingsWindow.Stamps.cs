using System.Windows;
using System.Windows.Controls;
using ZZZKeyEcho.App.Stamps;
using ZZZKeyEcho.App.ViewModels;
using ZZZKeyEcho.Core.Config;

namespace ZZZKeyEcho.App.Views;

public partial class SettingsWindow
{
    private readonly Action _enterStampEditor;
    private readonly Func<string> _stampUrl;
    private bool _stampsReady;

    public void RefreshStamps()
    {
        _viewModel.ReloadStamps();
        RefreshStampStatus();
    }

    private void InitializeStamps()
    {
        var fps = _state.Config.Stamps.CaptureFps;
        StampFpsBox.SelectedItem = StampFpsBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag?.ToString() == fps.ToString());

        _stampsReady = true;
        RefreshStampStatus();
    }

    private void RefreshStampStatus()
    {
        var count = _state.Config.Stamps.Items.Count;
        StampStatusText.Text = count == 0
            ? "还没有印章。点击「进入编辑模式」→「新建源输入」开始。"
            : $"共 {count} 个印章；删除或新建请使用编辑模式。OBS 地址：{_stampUrl()}";
    }

    private void OnEnterStampEditorClick(object sender, RoutedEventArgs e)
    {
        _enterStampEditor();
        RefreshStampStatus();
    }

    private static readonly (int SourceX, int SourceY, int SourceWidth, int SourceHeight, int OutputX, int OutputY, double Scale)[] LeftPreset =
    {
        (103, 46, 463, 97, 82, 853, 0.9038461538461541),
        (567, 46, 370, 62, 81, 952, 1.0),
    };

    private static readonly (int SourceX, int SourceY, int SourceWidth, int SourceHeight, int OutputX, int OutputY, double Scale)[] RightPreset =
    {
        (107, 46, 455, 109, 1458, 486, 0.6978021978021958),
        (562, 46, 187, 64, 1493, 574, 1.0),
        (748, 46, 189, 64, 1528, 649, 1.0),
    };

    private void OnImportStampDefaultsClick(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        var left = new MenuItem { Header = "左下角布局" };
        left.Click += (_, _) => ApplyStampPreset(false);
        var right = new MenuItem { Header = "右侧布局" };
        right.Click += (_, _) => ApplyStampPreset(true);
        menu.Items.Add(left);
        menu.Items.Add(right);
        menu.PlacementTarget = (FrameworkElement)sender;
        menu.IsOpen = true;
    }

    private void ApplyStampPreset(bool rightSide)
    {
        if (_state.Config.Stamps.Items.Count > 0
            && MessageBox.Show(
                this,
                "导入默认布局会替换当前所有印章，继续？",
                "ZZZKeyEcho",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        var canvas = MonitorHelper.GetGameClientRect(_state.Config.Input.GameProcess);

        if (canvas.Width < 16 || canvas.Height < 16)
        {
            canvas = MonitorHelper.GetPrimary().Bounds;
        }

        var factorX = canvas.Width / 1920.0;
        var factorY = canvas.Height / 1080.0;
        var visualFactor = Math.Min(factorX, factorY);
        var entries = rightSide ? RightPreset : LeftPreset;
        var items = new List<StampConfig>();

        foreach (var entry in entries)
        {
            var source = new Int32Rect(
                canvas.X + (int)Math.Round(entry.SourceX * factorX),
                canvas.Y + (int)Math.Round(entry.SourceY * factorY),
                Math.Max(16, (int)Math.Round(entry.SourceWidth * factorX)),
                Math.Max(16, (int)Math.Round(entry.SourceHeight * factorY)));

            var outputX = rightSide
                ? canvas.X + canvas.Width - (int)Math.Round((1920 - entry.OutputX) * factorX)
                : canvas.X + (int)Math.Round(entry.OutputX * factorX);
            var outputY = canvas.Y + (int)Math.Round(entry.OutputY * factorY);

            items.Add(new StampConfig
            {
                SourceX = source.X,
                SourceY = source.Y,
                SourceWidth = source.Width,
                SourceHeight = source.Height,
                OutputX = outputX,
                OutputY = outputY,
                Scale = entry.Scale,
                BorderThickness = Math.Max(0, 2 * visualFactor),
                BorderOpacity = 1,
                ContentOpacity = 1,
                BorderColor = "#FFFFFF",
                CornerRadius = 8 * visualFactor,
                Padding = 4 * visualFactor,
                HideLocal = false,
                WebEnabled = false,
            });
        }

        _state.Config.Stamps.Hidden = false;
        _state.Config.Stamps.FollowGame = false;
        _state.Config.Stamps.CaptureFps = 30;
        _state.Config.Stamps.Items = items;
        _viewModel.ReloadStamps();
        _state.NotifyChanged(SettingsChange.Stamps);
        JsonStore.Save(_state.Config);
        RefreshStampStatus();
        StampStatusText.Text = rightSide
            ? "已导入右侧默认布局（3 个印章）。"
            : "已导入左下角默认布局（2 个印章）。";
    }

    private void OnCopyStampUrlClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var url = string.IsNullOrWhiteSpace(StampUrlBox.Text) ? _stampUrl() : StampUrlBox.Text;
            Clipboard.SetText(url);
            StampStatusText.Text = $"已复制 OBS 地址：{url}";
        }
        catch (Exception exception)
        {
            StampStatusText.Text = $"复制失败：{exception.Message}";
        }
    }

    private void OnOpenStampPreviewClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var url = string.IsNullOrWhiteSpace(StampUrlBox.Text) ? _stampUrl() : StampUrlBox.Text;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch (Exception exception)
        {
            StampStatusText.Text = $"打开预览失败：{exception.Message}";
        }
    }

    private void OnStampFpsChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_stampsReady || StampFpsBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        if (int.TryParse(item.Tag?.ToString(), out var fps)
            && fps is 15 or 30 or 60)
        {
            _viewModel.StampsCaptureFps = fps;
            RefreshStampStatus();
        }
    }

    private void OnApplyStampToAllClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: StampItemViewModel source })
        {
            return;
        }

        foreach (var row in _viewModel.StampRows)
        {
            if (!ReferenceEquals(row, source))
            {
                row.ApplyAppearanceFrom(source);
            }
        }

        _state.NotifyChanged(SettingsChange.Stamps);
        StampStatusText.Text = $"已将「{source.DisplayName}」的外观应用到全部印章。";
    }

    private void OnDeleteStampClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: StampItemViewModel row })
        {
            return;
        }

        var result = MessageBox.Show(
            this,
            $"删除「{row.DisplayName}」及其输出框？",
            "ZZZKeyEcho",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (row.Index >= 0 && row.Index < _state.Config.Stamps.Items.Count)
        {
            _state.Config.Stamps.Items.RemoveAt(row.Index);
        }

        _viewModel.ReloadStamps();
        _state.NotifyChanged(SettingsChange.Stamps);
        RefreshStampStatus();
    }
}
