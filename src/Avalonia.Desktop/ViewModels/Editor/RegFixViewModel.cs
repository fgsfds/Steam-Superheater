using Common.Entities.Fixes.RegistryFix;
using Common.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.Desktop.ViewModels.Editor;

internal sealed partial class RegFixViewModel : ObservableObject
{
    private RegistryFixEntity SelectedFix { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedRegistryFixIndexStr))]
    private int _selectedRegistryFixIndex;

    public string SelectedRegistryFixIndexStr => (SelectedRegistryFixIndex + 1).ToString();

    public List<RegistryEntry>? SelectedRegistryFixEntries => SelectedFix.Entries;


    public RegFixViewModel(RegistryFixEntity fix)
    {
        SelectedFix = fix;
    }


    /// <summary>
    /// Add entry to registry fix
    /// </summary>
    [RelayCommand]
    private void AddRegFixEntry()
    {
        SelectedFix.Entries = [..SelectedFix.Entries.Append(new())];

        OnPropertyChanged(nameof(SelectedRegistryFixEntries));
    }


    /// <summary>
    /// Remove entry from registry fix
    /// </summary>
    [RelayCommand]
    private void DeleteRegFixEntry()
    {
        if (SelectedFix.Entries.Count < 2)
        {
            return;
        }

        var currentIndex = SelectedRegistryFixIndex;

        if (currentIndex > 0)
        {
            SelectedRegistryFixIndex = currentIndex - 1;
        }

        SelectedFix.Entries.RemoveAt(currentIndex);
        SelectedFix.Entries = [.. SelectedFix.Entries];

        OnPropertyChanged(nameof(SelectedRegistryFixEntries));
    }
}
