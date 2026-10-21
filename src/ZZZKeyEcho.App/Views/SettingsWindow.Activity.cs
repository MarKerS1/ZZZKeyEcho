using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using ZZZKeyEcho.App.Activity;
using ZZZKeyEcho.Core.Activity;

namespace ZZZKeyEcho.App.Views;

public partial class SettingsWindow
{
    private ActivityRecorder? _recorder;
    private DispatcherTimer? _activityTimer;

    private void InitializeActivity(ActivityRecorder recorder)
    {
        _recorder = recorder;
        recorder.Changed += OnRecorderChanged;

        _activityTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(250),
        };

        _activityTimer.Tick += (_, _) => UpdateActivityStatus();
        _activityTimer.Start();

        Closed += (_, _) =>
        {
            _activityTimer?.Stop();
            recorder.Changed -= OnRecorderChanged;
        };

        RefreshRecordings();
        UpdateActivityStatus();
    }

    private void OnRecorderChanged(object? sender, EventArgs e) => UpdateActivityStatus();

    private void UpdateActivityStatus()
    {
        if (_recorder is null)
        {
            return;
        }

        if (_recorder.IsRecording)
        {
            var elapsed = TimeSpan.FromSeconds(_recorder.ElapsedSec);
            ActivityToggleButton.Content = "结束录制";
            ActivityStatusText.Text =
                $"● 录制中 {elapsed.Minutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds:000} · 已记录 {_recorder.RowCount} 条\n{_recorder.CurrentFile}";
        }
        else
        {
            ActivityToggleButton.Content = "开始录制";
            ActivityStatusText.Text = "未录制。点击开始，或按录制热键（默认 F2）。";
        }
    }

    private void OnToggleRecording(object sender, RoutedEventArgs e)
    {
        _recorder?.Toggle();
        UpdateActivityStatus();

        if (_recorder is { IsRecording: false })
        {
            RefreshRecordings();
        }
    }

    private void OnClearOverlayRecords(object sender, RoutedEventArgs e) => _state.Pipeline.Engine.Clear();

    private void OnOpenLogsFolder(object sender, RoutedEventArgs e)
    {
        if (_recorder is null)
        {
            return;
        }

        Directory.CreateDirectory(_recorder.Directory);
        Process.Start("explorer.exe", _recorder.Directory);
    }

    private void OnRefreshRecordings(object sender, RoutedEventArgs e) => RefreshRecordings();

    private void RefreshRecordings()
    {
        if (_recorder is null)
        {
            return;
        }

        var selected = RecordingFiles.SelectedItem as string;
        RecordingFiles.Items.Clear();

        if (!Directory.Exists(_recorder.Directory))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(_recorder.Directory, "*.csv").OrderByDescending(path => path))
        {
            RecordingFiles.Items.Add(Path.GetFileName(file));
        }

        if (selected is not null && RecordingFiles.Items.Contains(selected))
        {
            RecordingFiles.SelectedItem = selected;
        }
        else if (RecordingFiles.Items.Count > 0)
        {
            RecordingFiles.SelectedIndex = 0;
        }
    }

    private string? SelectedRecordingPath()
    {
        if (_recorder is null || RecordingFiles.SelectedItem is not string name)
        {
            return null;
        }

        return Path.Combine(_recorder.Directory, name);
    }

    private void OnDeleteRecording(object sender, RoutedEventArgs e)
    {
        var path = SelectedRecordingPath();

        if (path is null)
        {
            MessageBox.Show(this, "请先在列表里选择一个 CSV 文件。", "操作录制");
            return;
        }

        if (_recorder is { IsRecording: true }
            && string.Equals(_recorder.CurrentFile, path, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, "该文件正在录制中，请先结束录制再删除。", "操作录制");
            return;
        }

        var confirm = MessageBox.Show(
            this,
            "确定删除选中的录制文件吗？\n" + Path.GetFileName(path),
            "删除录制文件",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            File.Delete(path);
            RefreshRecordings();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "删除失败");
        }
    }
    private void OnExportRecordingHtml(object sender, RoutedEventArgs e)
    {
        var csvPath = SelectedRecordingPath();

        if (csvPath is null)
        {
            MessageBox.Show(this, "请先在列表里选择一个 CSV 文件。", "操作录制");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "HTML 文件|*.html",
            FileName = Path.GetFileNameWithoutExtension(csvPath) + ".html",
            InitialDirectory = Path.GetDirectoryName(csvPath),
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            ActivityHtmlExporter.Export(csvPath, dialog.FileName, _icons);
            MessageBox.Show(this, "已导出：\n" + dialog.FileName, "操作录制");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "导出失败");
        }
    }


}
