using CommunityToolkit.Mvvm.ComponentModel;

namespace SteamFDA.ViewModels
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        public PopupMessageViewModel? _popupDataContext;

        [ObservableProperty]
        public bool _isLocalRepoWarningEnabled;
    }
}