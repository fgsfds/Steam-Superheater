using CommunityToolkit.Mvvm.ComponentModel;

namespace SteamFDA.ViewModels
{
    public partial class PopupMessageViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _showPopup;

        public string MessageText { get; init; }

        public void CancelCommand()
        {
            ShowPopup = false;
        }

        public void Show()
        {
            ShowPopup = true;
        }

        public PopupMessageViewModel(string message)
        {
            MessageText = message;
        }
    }
}
