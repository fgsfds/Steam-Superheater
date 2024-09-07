using CommunityToolkit.Mvvm.Input;

namespace Avalonia.Desktop.ViewModels;

public interface ISearchBarViewModel
{
    /// <summary>
    /// List of tags
    /// </summary>
    HashSet<string> TagsComboboxList { get; }

    /// <summary>
    /// Selected tag
    /// </summary>
    string SelectedTagFilter { get; set; }

    /// <summary>
    /// Search string
    /// </summary>
    string SearchBarText { get; set; }

    /// <summary>
    /// Show tags filter popup
    /// </summary>
    string ShowPopupStackButtonText { get; }

    /// <summary>
    /// Clear search bar text
    /// </summary>
    IRelayCommand ClearSearchCommand { get; }

    /// <summary>
    /// Show tags filter popup
    /// </summary>
    IAsyncRelayCommand ShowFiltersPopupCommand { get; }
}

