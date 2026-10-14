using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using ZZZKeyEcho.App.Rendering;
using ZZZKeyEcho.Core.Engine;

namespace ZZZKeyEcho.App.ViewModels;

public sealed class IconRowViewModel : ObservableObject
{
    private readonly IconProvider _provider;

    public IconRowViewModel(ActionInfo action, IconProvider provider)
    {
        ActionId = action.Id;
        ActionName = action.DisplayName;
        _provider = provider;
    }

    public string ActionId { get; }

    public string ActionName { get; }

    public ImageSource? Preview => _provider.GetActionIcon(ActionId);

    public string CustomFileName => ActionId + ".png";

    public void Refresh()
    {
        _provider.Invalidate(ActionId);
        OnPropertyChanged(nameof(Preview));
    }
}
