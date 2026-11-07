using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZZZKeyEcho.Core.Config;
using ZZZKeyEcho.Core.Stamps;

namespace ZZZKeyEcho.App.Stamps;

public partial class StampEditorWindow : Window
{
    private enum EditorMode
    {
        Normal,
        NewSource,
        DeleteSource,
    }

    private enum DragKind
    {
        None,
        Source,
        Output,
        OverlayProxy,
        RecordProxy,
    }

    private readonly AppConfig _config;
    private readonly StampEditorSession _session;
    private readonly Int32Rect _bounds;
    private readonly double _scale;
    private EditorMode _mode;
    private DragKind _dragKind;
    private int _dragIndex = -1;
    private Point _dragStart;
    private Int32Rect _dragStartRect;
    private bool _drawing;
    private bool _dragging;
    private bool _moved;
    private Point _newStart;
    private Int32Rect _previewRect;
    private Border? _previewBorder;
    private int _selectedIndex = -1;
    private bool _allowClose;
    private bool _inspectorUpdating;
    private bool _saved;
    private Rect _hintRect;

    public StampEditorWindow(AppConfig config)
    {
        _config = config;
        var monitor = MonitorHelper.GetMonitorAtCursor();
        _bounds = monitor.Bounds;
        _scale = monitor.Scale <= 0 ? 1.0 : monitor.Scale;
        _session = new StampEditorSession(config, monitor);

        InitializeComponent();

        Left = monitor.Bounds.X / _scale;
        Top = monitor.Bounds.Y / _scale;
        Width = monitor.Bounds.Width / _scale;
        Height = monitor.Bounds.Height / _scale;

        Rebuild();
        SetMode(EditorMode.Normal);
        Loaded += (_, _) => CaptureHintRect();
    }

    public event EventHandler? LayoutSaved;

    public bool WasSaved => _saved;

    private void Rebuild()
    {
        Surface.Children.Clear();
        _previewBorder = null;

        AddBox(
            new Int32Rect(
                (int)Math.Round(_session.OverlayX),
                (int)Math.Round(_session.OverlayY),
                (int)Math.Round(_session.OverlayWidth),
                (int)Math.Round(_session.OverlayHeight)),
            "按键悬浮窗",
            Color.FromRgb(0x8A, 0xB4, 0xF8),
            Color.FromArgb(0x18, 0x8A, 0xB4, 0xF8));

        AddBox(
            new Int32Rect(
                (int)Math.Round(_session.RecordX),
                (int)Math.Round(_session.RecordY),
                (int)Math.Round(_session.RecordWidth),
                (int)Math.Round(_session.RecordHeight)),
            "录制按钮",
            Color.FromRgb(0xFF, 0x8A, 0x80),
            Color.FromArgb(0x18, 0xFF, 0x8A, 0x80));

        for (var index = 0; index < _session.Items.Count; index++)
        {
            var stamp = _session.Items[index];
            var source = new Int32Rect(stamp.SourceX, stamp.SourceY, stamp.SourceWidth, stamp.SourceHeight);
            var output = StampGeometry.OutputRect(stamp);

            AddBox(
                source,
                $"源{index + 1}",
                Color.FromRgb(0x35, 0xD0, 0xFF),
                Color.FromArgb(0x16, 0x35, 0xD0, 0xFF));

            AddOutputBox(stamp, index, output);
        }

        if (_drawing && _previewRect.Width > 0 && _previewRect.Height > 0)
        {
            _previewBorder = AddBox(
                _previewRect,
                "新源",
                Color.FromRgb(0x8A, 0xFF, 0xB0),
                Color.FromArgb(0x22, 0x8A, 0xFF, 0xB0));
        }
    }

    private void AddOutputBox(StampConfig stamp, int index, Int32Rect rect)
    {
        var selected = index == _selectedIndex;
        var borderColor = selected ? Color.FromRgb(0xFF, 0xD5, 0x4F) : Color.FromRgb(0xFF, 0xA7, 0x3D);
        var fillColor = selected
            ? Color.FromArgb(0x38, 0xFF, 0xD5, 0x4F)
            : Color.FromArgb(0x16, 0xFF, 0xA7, 0x3D);

        var border = Math.Max(0, stamp.BorderThickness);
        var padding = Math.Max(0, stamp.Padding);
        var inset = border + padding;

        if (border > 0)
        {
            var preview = new Border
            {
                BorderThickness = new Thickness(MonitorHelper.PhysicalToDip(border, _scale)),
                BorderBrush = new SolidColorBrush(ParseColor(stamp.BorderColor, Colors.White))
                {
                    Opacity = Math.Clamp(stamp.BorderOpacity, 0, 1),
                },
                CornerRadius = new CornerRadius(MonitorHelper.PhysicalToDip(stamp.CornerRadius, _scale)),
                Background = Brushes.Transparent,
                IsHitTestVisible = false,
            };

            Canvas.SetLeft(preview, (rect.X - inset - _bounds.X) / _scale);
            Canvas.SetTop(preview, (rect.Y - inset - _bounds.Y) / _scale);
            preview.Width = (rect.Width + (inset * 2)) / _scale;
            preview.Height = (rect.Height + (inset * 2)) / _scale;
            Surface.Children.Add(preview);
        }

        var outer = new Border
        {
            BorderBrush = new SolidColorBrush(borderColor),
            BorderThickness = new Thickness(2),
            Background = new SolidColorBrush(fillColor),
            IsHitTestVisible = false,
        };

        var content = new Grid();
        var text = new TextBlock
        {
            Text = $"映{index + 1}",
            Foreground = Brushes.White,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromArgb(0xAA, 0, 0, 0)),
            Padding = new Thickness(6, 2, 6, 2),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        content.Children.Add(text);
        outer.Child = content;

        Canvas.SetLeft(outer, (rect.X - _bounds.X) / _scale);
        Canvas.SetTop(outer, (rect.Y - _bounds.Y) / _scale);
        outer.Width = Math.Max(4, rect.Width / _scale);
        outer.Height = Math.Max(4, rect.Height / _scale);
        Surface.Children.Add(outer);
    }

    private Border AddBox(Int32Rect rect, string label, Color borderColor, Color fillColor)
    {
        var border = new Border
        {
            BorderBrush = new SolidColorBrush(borderColor),
            BorderThickness = new Thickness(2),
            Background = new SolidColorBrush(fillColor),
            IsHitTestVisible = false,
        };

        var text = new TextBlock
        {
            Text = label,
            Foreground = Brushes.White,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromArgb(0xAA, 0, 0, 0)),
            Padding = new Thickness(6, 2, 6, 2),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };

        border.Child = text;
        Canvas.SetLeft(border, (rect.X - _bounds.X) / _scale);
        Canvas.SetTop(border, (rect.Y - _bounds.Y) / _scale);
        border.Width = Math.Max(4, rect.Width / _scale);
        border.Height = Math.Max(4, rect.Height / _scale);
        Surface.Children.Add(border);

        return border;
    }

    private void SetMode(EditorMode mode)
    {
        _mode = mode;
        _drawing = false;

        switch (mode)
        {
            case EditorMode.NewSource:
                Cursor = Cursors.Cross;
                HintText.Text = "请框选你要映射的范围，Esc / 右键退出新建模式";
                break;
            case EditorMode.DeleteSource:
                Cursor = Cursors.Hand;
                HintText.Text = "请选择你要删除的源输入框，Esc / 右键退出删除模式";
                break;
            default:
                Cursor = Cursors.Arrow;
                HintText.Text = "拖动源 / 映框调整位置；点击「映」可编辑外观。";
                break;
        }

        Dispatcher.BeginInvoke(() =>
        {
            HintPanel.Visibility = Visibility.Visible;
            CaptureHintRect();
        });
    }

    private void CaptureHintRect()
    {
        if (HintPanel.Visibility != Visibility.Visible || HintPanel.ActualWidth <= 0)
        {
            return;
        }

        var origin = HintPanel.TranslatePoint(new Point(0, 0), this);
        _hintRect = new Rect(origin, new Size(HintPanel.ActualWidth, HintPanel.ActualHeight));
    }

    private void UpdateHintVisibility(Point position)
    {
        if (_hintRect.Width <= 0)
        {
            return;
        }

        var near = _hintRect;
        near.Inflate(60, 30);

        var desired = near.Contains(position) ? Visibility.Collapsed : Visibility.Visible;

        if (HintPanel.Visibility == desired)
        {
            return;
        }

        HintPanel.Visibility = desired;

        if (desired == Visibility.Visible)
        {
            CaptureHintRect();
        }
    }

    private void OnNewClick(object sender, RoutedEventArgs e)
    {
        _selectedIndex = -1;
        InspectorHost.Visibility = Visibility.Collapsed;
        SetMode(EditorMode.NewSource);
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        _selectedIndex = -1;
        InspectorHost.Visibility = Visibility.Collapsed;
        SetMode(EditorMode.DeleteSource);
    }

    private void OnSaveClick(object sender, RoutedEventArgs e) => SaveLayout();

    private void OnExitClick(object sender, RoutedEventArgs e) => RequestClose();

    private void SaveLayout()
    {
        try
        {
            _session.SaveTo(_config);
            JsonStore.Save(_config);
            _saved = true;
            HintText.Text = "印章布局已保存";
            LayoutSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, $"保存失败：{exception.Message}", "ZZZKeyEcho");
        }
    }

    private void RequestClose()
    {
        if (_session.IsDirty)
        {
            var result = MessageBox.Show(
                this,
                "是否保存当前布局？",
                "ZZZKeyEcho",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SaveLayout();
            }
        }

        _allowClose = true;
        Close();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        RequestClose();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        if (_mode != EditorMode.Normal)
        {
            SetMode(EditorMode.Normal);
            e.Handled = true;
            return;
        }

        RequestClose();
        e.Handled = true;
    }

    private void OnMouseRightDown(object sender, MouseButtonEventArgs e)
    {
        if (_mode != EditorMode.Normal)
        {
            SetMode(EditorMode.Normal);
            e.Handled = true;
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (IsButtonSource(e.OriginalSource))
        {
            return;
        }

        var point = ToPhysical(e.GetPosition(Surface));

        if (_mode == EditorMode.NewSource)
        {
            _newStart = e.GetPosition(Surface);
            _drawing = true;
            _previewRect = new Int32Rect(point.X, point.Y, 0, 0);
            Surface.CaptureMouse();
            e.Handled = true;
            return;
        }

        if (_mode == EditorMode.DeleteSource)
        {
            for (var index = _session.Items.Count - 1; index >= 0; index--)
            {
                var source = SourceRect(_session.Items[index]);
                if (!StampGeometry.Contains(source, point.X, point.Y))
                {
                    continue;
                }

                _session.RemoveAt(index);
                _selectedIndex = -1;
                InspectorHost.Visibility = Visibility.Collapsed;
                SetMode(EditorMode.Normal);
                Rebuild();
                HintText.Text = "已删除源输入";
                e.Handled = true;
                return;
            }

            e.Handled = true;
            return;
        }

        _moved = false;
        _dragIndex = -1;
        _dragKind = DragKind.None;

        for (var index = _session.Items.Count - 1; index >= 0; index--)
        {
            var stamp = _session.Items[index];

            if (StampGeometry.Contains(StampGeometry.OutputRect(stamp), point.X, point.Y))
            {
                _dragKind = DragKind.Output;
                _dragIndex = index;
                _dragStartRect = StampGeometry.OutputRect(stamp);
                _selectedIndex = index;
                BuildInspector();
                break;
            }

            if (StampGeometry.Contains(SourceRect(stamp), point.X, point.Y))
            {
                _dragKind = DragKind.Source;
                _dragIndex = index;
                _dragStartRect = SourceRect(stamp);
                break;
            }
        }

        if (_dragKind == DragKind.None
            && StampGeometry.Contains(OverlayProxyRect(), point.X, point.Y))
        {
            _dragKind = DragKind.OverlayProxy;
            _dragStartRect = OverlayProxyRect();
        }
        else if (_dragKind == DragKind.None
                 && StampGeometry.Contains(RecordProxyRect(), point.X, point.Y))
        {
            _dragKind = DragKind.RecordProxy;
            _dragStartRect = RecordProxyRect();
        }

        if (_dragKind == DragKind.None)
        {
            _selectedIndex = -1;
            InspectorHost.Visibility = Visibility.Collapsed;
            Rebuild();
            return;
        }

        _dragStart = new Point(point.X, point.Y);
        _dragging = true;
        Surface.CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        UpdateHintVisibility(e.GetPosition(this));

        var position = e.GetPosition(Surface);
        var point = ToPhysical(position);

        if (_drawing)
        {
            _previewRect = StampGeometry.Normalize(
                (int)Math.Round(_newStart.X * _scale + _bounds.X),
                (int)Math.Round(_newStart.Y * _scale + _bounds.Y),
                point.X,
                point.Y);
            Rebuild();
            return;
        }

        if (!_dragging || _dragKind == DragKind.None)
        {
            return;
        }

        var dx = point.X - (int)Math.Round(_dragStart.X);
        var dy = point.Y - (int)Math.Round(_dragStart.Y);

        if (Math.Abs(dx) + Math.Abs(dy) > 2)
        {
            _moved = true;
        }

        switch (_dragKind)
        {
            case DragKind.Source when _dragIndex >= 0:
                var source = _session.Items[_dragIndex];
                var movedSource = StampGeometry.Clamp(
                    new Int32Rect(
                        _dragStartRect.X + dx,
                        _dragStartRect.Y + dy,
                        source.SourceWidth,
                        source.SourceHeight),
                    _bounds);
                source.SourceX = movedSource.X;
                source.SourceY = movedSource.Y;
                _session.IsDirty = true;
                break;

            case DragKind.Output when _dragIndex >= 0:
                var output = _session.Items[_dragIndex];
                var movedOutput = StampGeometry.Clamp(
                    new Int32Rect(
                        _dragStartRect.X + dx,
                        _dragStartRect.Y + dy,
                        _dragStartRect.Width,
                        _dragStartRect.Height),
                    _bounds);
                output.OutputX = movedOutput.X;
                output.OutputY = movedOutput.Y;
                _session.IsDirty = true;
                break;

            case DragKind.OverlayProxy:
                _session.OverlayX = Math.Clamp(
                    _session.OverlayX + dx,
                    _bounds.X,
                    _bounds.X + _bounds.Width - _session.OverlayWidth);
                _session.OverlayY = Math.Clamp(
                    _session.OverlayY + dy,
                    _bounds.Y,
                    _bounds.Y + _bounds.Height - _session.OverlayHeight);
                _session.IsDirty = true;
                break;

            case DragKind.RecordProxy:
                _session.RecordX = Math.Clamp(
                    _session.RecordX + dx,
                    _bounds.X,
                    _bounds.X + _bounds.Width - _session.RecordWidth);
                _session.RecordY = Math.Clamp(
                    _session.RecordY + dy,
                    _bounds.Y,
                    _bounds.Y + _bounds.Height - _session.RecordHeight);
                _session.IsDirty = true;
                break;
        }

        _dragStart = new Point(point.X, point.Y);
        _dragStartRect = _dragKind switch
        {
            DragKind.Source when _dragIndex >= 0 => SourceRect(_session.Items[_dragIndex]),
            DragKind.Output when _dragIndex >= 0 => StampGeometry.OutputRect(_session.Items[_dragIndex]),
            DragKind.OverlayProxy => OverlayProxyRect(),
            DragKind.RecordProxy => RecordProxyRect(),
            _ => _dragStartRect,
        };

        Rebuild();
        e.Handled = true;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_drawing)
        {
            _drawing = false;
            Surface.ReleaseMouseCapture();
            var source = _previewRect;

            if (StampGeometry.IsValid(source) && _session.Items.Count < 8)
            {
                var stamp = _session.Add(source);
                stamp.OutputX = Math.Clamp(
                    stamp.OutputX,
                    _bounds.X,
                    _bounds.X + _bounds.Width - StampGeometry.OutputWidth(stamp));
                stamp.OutputY = Math.Clamp(
                    stamp.OutputY,
                    _bounds.Y,
                    _bounds.Y + _bounds.Height - StampGeometry.OutputHeight(stamp));
                _selectedIndex = _session.Items.Count - 1;
                SetMode(EditorMode.Normal);
                BuildInspector();
                HintText.Text = $"已新建源{_session.Items.Count} / 映{_session.Items.Count}";
            }
            else
            {
                SetMode(EditorMode.Normal);
                HintText.Text = _session.Items.Count >= 8
                    ? "最多只能创建 8 个印章"
                    : "选框太小，已取消新建";
            }

            Rebuild();
            e.Handled = true;
            return;
        }

        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        Surface.ReleaseMouseCapture();

        if (!_moved && _dragKind == DragKind.Output && _dragIndex >= 0)
        {
            _selectedIndex = _dragIndex;
            BuildInspector();
        }

        Rebuild();
        e.Handled = true;
    }

    private void BuildInspector()
    {
        InspectorPanel.Children.Clear();

        if (_selectedIndex < 0 || _selectedIndex >= _session.Items.Count)
        {
            InspectorHost.Visibility = Visibility.Collapsed;
            return;
        }

        InspectorHost.Visibility = Visibility.Visible;
        var stamp = _session.Items[_selectedIndex];

        var title = new TextBlock
        {
            Text = $"映{_selectedIndex + 1} 设置",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8),
        };
        InspectorPanel.Children.Add(title);

        AddSlider("比例", 0.25, 4.0, stamp.Scale, value =>
        {
            var oldWidth = StampGeometry.OutputWidth(stamp);
            var oldHeight = StampGeometry.OutputHeight(stamp);
            stamp.Scale = Math.Clamp(value, 0.25, 4.0);
            var resized = StampGeometry.ResizeAroundCenter(
                stamp.OutputX,
                stamp.OutputY,
                oldWidth,
                oldHeight,
                StampGeometry.OutputWidth(stamp),
                StampGeometry.OutputHeight(stamp));
            var clamped = StampGeometry.Clamp(resized, _bounds);
            stamp.OutputX = clamped.X;
            stamp.OutputY = clamped.Y;
            _session.IsDirty = true;
            Rebuild();
        }, "x");

        AddSlider("边框粗细", 0, 20, stamp.BorderThickness, value =>
        {
            stamp.BorderThickness = Math.Clamp(value, 0, 20);
            _session.IsDirty = true;
            Rebuild();
        }, "px");

        AddSlider("边框不透明度", 0, 1, stamp.BorderOpacity, value =>
        {
            stamp.BorderOpacity = Math.Clamp(value, 0, 1);
            _session.IsDirty = true;
            Rebuild();
        });

        AddSlider("画面不透明度", 0.1, 1, stamp.ContentOpacity, value =>
        {
            stamp.ContentOpacity = Math.Clamp(value, 0.1, 1);
            _session.IsDirty = true;
            Rebuild();
        });

        AddSlider("圆角", 0, 48, stamp.CornerRadius, value =>
        {
            stamp.CornerRadius = Math.Clamp(value, 0, 48);
            _session.IsDirty = true;
            Rebuild();
        }, "px");

        AddSlider("内边距", 0, 48, stamp.Padding, value =>
        {
            stamp.Padding = Math.Clamp(value, 0, 48);
            _session.IsDirty = true;
            Rebuild();
        }, "px");

        AddColorEditor(stamp);

        var apply = new Button
        {
            Content = "将设置应用到全部印章",
            Margin = new Thickness(0, 8, 0, 0),
            Padding = new Thickness(8, 3, 8, 3),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        apply.Click += (_, _) =>
        {
            foreach (var other in _session.Items)
            {
                other.Scale = stamp.Scale;
                other.BorderThickness = stamp.BorderThickness;
                other.BorderOpacity = stamp.BorderOpacity;
                other.ContentOpacity = stamp.ContentOpacity;
                other.BorderColor = stamp.BorderColor;
                other.CornerRadius = stamp.CornerRadius;
                other.Padding = stamp.Padding;
            }

            _session.IsDirty = true;
            Rebuild();
            HintText.Text = "已应用到全部印章";
        };
        InspectorPanel.Children.Add(apply);

        var delete = new Button
        {
            Content = "删除此印章",
            Margin = new Thickness(0, 6, 0, 0),
            Padding = new Thickness(8, 3, 8, 3),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        delete.Click += (_, _) =>
        {
            _session.RemoveAt(_selectedIndex);
            _selectedIndex = -1;
            InspectorHost.Visibility = Visibility.Collapsed;
            Rebuild();
            HintText.Text = "已删除源输入";
        };
        InspectorPanel.Children.Add(delete);
    }

    private void AddSlider(string label, double min, double max, double value, Action<double> changed, string? suffix = null)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
        var header = new DockPanel();
        var caption = new TextBlock { Text = label, Foreground = Brushes.Gainsboro };
        var valueText = new TextBlock
        {
            Text = suffix is null ? $"{value:0.##}" : $"{value:0.##}{suffix}",
            Foreground = Brushes.Gainsboro,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        DockPanel.SetDock(valueText, Dock.Right);
        header.Children.Add(valueText);
        header.Children.Add(caption);
        panel.Children.Add(header);

        var slider = new Slider
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Clamp(value, min, max),
            TickFrequency = (max - min) / 20,
            IsSnapToTickEnabled = false,
        };
        slider.ValueChanged += (_, args) =>
        {
            if (_inspectorUpdating)
            {
                return;
            }

            _inspectorUpdating = true;
            valueText.Text = suffix is null ? $"{args.NewValue:0.##}" : $"{args.NewValue:0.##}{suffix}";
            changed(args.NewValue);
            _inspectorUpdating = false;
        };
        panel.Children.Add(slider);
        InspectorPanel.Children.Add(panel);
    }

    private void AddColorEditor(StampConfig stamp)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };
        panel.Children.Add(new TextBlock { Text = "边框颜色", Foreground = Brushes.Gainsboro });

        var textBox = new TextBox
        {
            Text = stamp.BorderColor,
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 2, 0, 4),
        };
        textBox.LostFocus += (_, _) => ApplyColor();
        textBox.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Enter)
            {
                ApplyColor();
            }
        };
        panel.Children.Add(textBox);

        var swatches = new WrapPanel();
        foreach (var hex in new[] { "#FFFFFF", "#FFD54F", "#FF8A80", "#80CBC4", "#8AB4F8", "#CE93D8", "#000000" })
        {
            var button = new Button
            {
                Width = 24,
                Height = 24,
                Margin = new Thickness(0, 0, 4, 4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Tag = hex,
            };
            button.Click += (_, _) =>
            {
                stamp.BorderColor = hex;
                textBox.Text = hex;
                _session.IsDirty = true;
                Rebuild();
            };
            swatches.Children.Add(button);
        }

        panel.Children.Add(swatches);
        InspectorPanel.Children.Add(panel);

        void ApplyColor()
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(textBox.Text);
                stamp.BorderColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                textBox.Text = stamp.BorderColor;
                _session.IsDirty = true;
                Rebuild();
            }
            catch (Exception)
            {
                textBox.Text = stamp.BorderColor;
            }
        }
    }

    private void AddCheck(string label, bool value, Action<bool> changed)
    {
        var checkBox = new CheckBox
        {
            Content = label,
            IsChecked = value,
            Foreground = Brushes.Gainsboro,
            Margin = new Thickness(0, 4, 0, 0),
        };
        checkBox.Checked += (_, _) => changed(true);
        checkBox.Unchecked += (_, _) => changed(false);
        InspectorPanel.Children.Add(checkBox);
    }

    private static Color ParseColor(string value, Color fallback)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(value);
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    private static Int32Rect SourceRect(StampConfig stamp) =>
        new(stamp.SourceX, stamp.SourceY, stamp.SourceWidth, stamp.SourceHeight);

    private Int32Rect OverlayProxyRect() => new(
        (int)Math.Round(_session.OverlayX),
        (int)Math.Round(_session.OverlayY),
        (int)Math.Round(_session.OverlayWidth),
        (int)Math.Round(_session.OverlayHeight));

    private Int32Rect RecordProxyRect() => new(
        (int)Math.Round(_session.RecordX),
        (int)Math.Round(_session.RecordY),
        (int)Math.Round(_session.RecordWidth),
        (int)Math.Round(_session.RecordHeight));

    private (int X, int Y) ToPhysical(Point point) => (
        (int)Math.Round(_bounds.X + (point.X * _scale)),
        (int)Math.Round(_bounds.Y + (point.Y * _scale)));

    private static bool IsButtonSource(object source)
    {
        var current = source as DependencyObject;

        while (current is not null)
        {
            if (current is Button)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }
}
