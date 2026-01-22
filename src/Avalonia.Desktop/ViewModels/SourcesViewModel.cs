using System.Collections.Immutable;
using Avalonia.Desktop.ViewModels.Popups;
using Common.Axiom;
using Common.Axiom.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.Desktop.ViewModels;

internal sealed partial class SourcesViewModel : ObservableObject
{
    private readonly IConfigProvider _configProvider;
    private readonly PopupEditorViewModel _popup;

    public event Action? SourcesChangedEvent;

    public ImmutableList<SourceEntity> SourcesList { get; private set; } = [];

    public SourceEntity? SelectedItem { get; set; }

    public SourcesViewModel(IConfigProvider configProvider, PopupEditorViewModel popup)
    {
        _configProvider = configProvider;
        _popup = popup;

        RefreshSources();
    }

    [RelayCommand]
    private async Task AddSource()
    {
        var addedUrl = await _popup.ShowAndGetResultAsync("asd", string.Empty).ConfigureAwait(true);

        if (addedUrl is null)
        {
            return;
        }

        if (!Uri.TryCreate(addedUrl, new(), out var url) ||
            !url.IsAbsoluteUri)
        {
            return;
        }

        _configProvider.AddSource(url);
        RefreshSources();
        SourcesChangedEvent?.Invoke();
    }

    [RelayCommand]
    private void RemoveSource()
    {
        ArgumentNullException.ThrowIfNull(SelectedItem);
        _configProvider.RemoveSource(SelectedItem.Url);
        RefreshSources();
    }

    private void RefreshSources()
    {
        SourcesList = [.. _configProvider.Sources];
        OnPropertyChanged(nameof(SourcesList));
    }
}

