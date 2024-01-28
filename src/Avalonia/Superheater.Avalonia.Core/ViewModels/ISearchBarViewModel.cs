using CommunityToolkit.Mvvm.Input;

namespace Superheater.Avalonia.Core.ViewModels
{
    public interface ISearchBarViewModel
    {
        HashSet<string> TagsComboboxList { get; }

        string SelectedTagFilter { get; set; }

        string SearchBarText { get; set; }

        IRelayCommand ClearSearchCommand { get; }
    }
}
