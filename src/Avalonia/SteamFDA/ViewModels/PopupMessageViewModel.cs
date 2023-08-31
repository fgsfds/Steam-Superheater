using CommunityToolkit.Mvvm.ComponentModel;
using SteamFDCommon.DI;
using System;

namespace SteamFDA.ViewModels
{
    public partial class PopupMessageViewModel : ObservableObject
    {
        private readonly MainWindowViewModel _mwvm;

        [ObservableProperty]
        private bool _showPopup;

        public bool IsOkCancel { get; init; }

        public bool IsOkOnly { get; init; }

        public string TitleText { get; init; }

        public string MessageText { get; init; }

        public void CancelCommand()
        {
            ShowPopup = false;
        }

        public Action? OkCommand
        {
            get;
            init;
        }

        /// <summary>
        /// Show popup window
        /// </summary>
        public void Show()
        {
            ShowPopup = true;
        }

        public PopupMessageViewModel(
            string title,
            string message,
            PopupMessageType type,
            Action? okCommand = null
            )
        {
            TitleText = title;

            MessageText = message;

            switch (type)
            {
                case PopupMessageType.OkOnly:
                    IsOkOnly = true;
                    break;
                case PopupMessageType.OkCancel:
                    IsOkCancel = true;
                    break;
            }

            OkCommand = () =>
            {
                okCommand?.Invoke();
                ShowPopup = false;
            };

            _mwvm = BindingsManager.Instance.GetInstance<MainWindowViewModel>();
            _mwvm.PopupDataContext = this;
        }
    }

    public enum PopupMessageType
    {
        OkOnly,
        OkCancel
    }
}
