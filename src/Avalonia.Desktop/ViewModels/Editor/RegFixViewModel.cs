using Avalonia.Desktop.ViewModels.Popups;
using Common.Client.Providers.Interfaces;
using Common.Entities.Fixes.RegistryFix;
using Common.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.Desktop.ViewModels.Editor;

internal sealed partial class RegFixViewModel : ObservableObject
{
    private RegistryFixEntity SelectedFix { get; set; }

    private IFixesProvider _fixesProvider;

    private readonly PopupEditorViewModel _popupEditor;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedRegistryFixIndexStr))]
    private int _selectedRegistryFixIndex;

    public string SelectedRegistryFixIndexStr => (SelectedRegistryFixIndex + 1).ToString();

    public List<RegistryEntry>? SelectedRegistryFixEntries => SelectedFix.Entries;


    public RegFixViewModel(
        RegistryFixEntity fix,
        IFixesProvider fixesProvider,
        PopupEditorViewModel popupEditor
        )
    {
        SelectedFix = fix;
        _fixesProvider = fixesProvider;
        _popupEditor = popupEditor;
    }


    /// <summary>
    /// Add entry to registry fix
    /// </summary>
    [RelayCommand]
    private void AddRegFixEntry()
    {
        Guard2.IsOfType<RegistryFixEntity>(SelectedFix, out var regFix);

        regFix.Entries = [.. regFix.Entries.Append(new())];

        OnPropertyChanged(nameof(SelectedRegistryFixEntries));
    }


    /// <summary>
    /// Remove entry from registry fix
    /// </summary>
    [RelayCommand]
    private void DeleteRegFixEntry()
    {
        Guard2.IsOfType<RegistryFixEntity>(SelectedFix, out var regFix);

        if (regFix.Entries.Count < 2)
        {
            return;
        }

        var currentIndex = SelectedRegistryFixIndex;

        if (currentIndex > 0)
        {
            SelectedRegistryFixIndex = currentIndex - 1;
        }

        regFix.Entries.RemoveAt(currentIndex);
        regFix.Entries = [.. regFix.Entries];

        OnPropertyChanged(nameof(SelectedRegistryFixEntries));
    }
}
