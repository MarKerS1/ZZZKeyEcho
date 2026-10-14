using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ZZZKeyEcho.App.Controls;

public partial class NumericSlider : UserControl
{
    private bool _updatingText;

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(NumericSlider),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum),
        typeof(double),
        typeof(NumericSlider),
        new PropertyMetadata(0d, OnBoundsChanged));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(double),
        typeof(NumericSlider),
        new PropertyMetadata(100d, OnBoundsChanged));

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
        nameof(Step),
        typeof(double),
        typeof(NumericSlider),
        new PropertyMetadata(0d, OnBoundsChanged));

    public static readonly DependencyProperty DecimalsProperty = DependencyProperty.Register(
        nameof(Decimals),
        typeof(int),
        typeof(NumericSlider),
        new PropertyMetadata(0, OnBoundsChanged));

    public static readonly DependencyProperty SuffixProperty = DependencyProperty.Register(
        nameof(Suffix),
        typeof(string),
        typeof(NumericSlider),
        new PropertyMetadata(string.Empty, OnBoundsChanged));

    public NumericSlider()
    {
        InitializeComponent();
        UpdateText();
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Step
    {
        get => (double)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public int Decimals
    {
        get => (int)GetValue(DecimalsProperty);
        set => SetValue(DecimalsProperty, value);
    }

    public string Suffix
    {
        get => (string)GetValue(SuffixProperty);
        set => SetValue(SuffixProperty, value);
    }

    private static object CoerceValue(DependencyObject element, object value)
    {
        var control = (NumericSlider)element;
        var number = (double)value;

        if (double.IsNaN(number))
        {
            number = control.Minimum;
        }

        var minimum = control.Minimum;
        var maximum = Math.Max(minimum, control.Maximum);
        return Math.Clamp(number, minimum, maximum);
    }

    private static void OnValueChanged(DependencyObject element, DependencyPropertyChangedEventArgs args) =>
        ((NumericSlider)element).UpdateText();

    private static void OnBoundsChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        var control = (NumericSlider)element;
        control.CoerceValue(ValueProperty);
        control.SliderPart.IsSnapToTickEnabled = control.Step > 0;
        control.SliderPart.IsMoveToPointEnabled = true;
        control.UpdateText();
    }

    private void UpdateText()
    {
        _updatingText = true;
        var pattern = "0" + (Decimals > 0 ? "." + new string('0', Decimals) : string.Empty);
        TextPart.Text = Value.ToString(pattern, CultureInfo.InvariantCulture);
        SuffixPart.Text = Suffix;
        _updatingText = false;
    }

    private void OnTextKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        CommitText();
        e.Handled = true;
    }

    private void OnTextCommit(object sender, RoutedEventArgs e) => CommitText();

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingText)
        {
            return;
        }

        var text = TextPart.Text.Trim();

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
        {
            var maximum = Math.Max(Minimum, Maximum);
            Value = Math.Clamp(parsed, Minimum, maximum);
        }
    }

    private void CommitText()
    {
        OnTextChanged(this, null!);
        UpdateText();
    }
}
