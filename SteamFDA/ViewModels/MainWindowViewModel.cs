using CommunityToolkit.Mvvm.ComponentModel;

namespace SteamFDA.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        public PopupMessageViewModel _popupDataContext;
    }
}