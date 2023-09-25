using CommunityToolkit.Mvvm.ComponentModel;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        public PopupMessageViewModel? _popupDataContext;

        [ObservableProperty]
        public bool _isLocalRepoWarningEnabled;
    }
}