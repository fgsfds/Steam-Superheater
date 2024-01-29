using CommunityToolkit.Mvvm.Input;

namespace Superheater.Avalonia.Core.ViewModels
{
    public interface ISearchBarViewModel
    {
        HashSet<string> TagsComboboxList { get; }

        string SelectedTagFilter { get; set; }

        string SearchBarText { get; set; }

        string ShowPopupStackButtonText { get; }

        bool IsSteamGameMode { get; }

        IRelayCommand ClearSearchCommand { get; }

        IAsyncRelayCommand ShowFiltersPopupCommand { get; }
    }
}
