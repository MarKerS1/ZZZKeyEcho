using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZZZKeyEcho.Core.Config;

namespace ZZZKeyEcho.App;

[Flags]
public enum SettingsChange
{
    None = 0,
    OverlayStyle = 1,
    Rendering = 2,
    Mapping = 4,
    All = OverlayStyle | Rendering | Mapping,
}

public sealed class AppState : INotifyPropertyChanged
{
    public AppState(AppConfig config, InputPipeline pipeline)
    {
        Config = config;
        Pipeline = pipeline;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<SettingsChange>? Changed;

    public AppConfig Config { get; }

    public InputPipeline Pipeline { get; }

    public bool RecordingEnabled
    {
        get => Pipeline.Enabled;
        set
        {
            if (Pipeline.Enabled == value)
            {
                return;
            }

            Pipeline.Enabled = value;
            OnPropertyChanged();
            NotifyChanged();
        }
    }

    public void NotifyChanged(SettingsChange change = SettingsChange.All)
    {
        Changed?.Invoke(this, change);
        OnPropertyChanged(nameof(Config));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
