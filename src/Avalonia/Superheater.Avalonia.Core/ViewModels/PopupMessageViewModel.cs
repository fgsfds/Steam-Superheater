using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Common.DI;
using System;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class PopupMessageViewModel : ObservableObject
    {
        /// <summary>
        /// Create new Popup window
        /// </summary>
        /// <param name="title">Title text</param>
        /// <param name="message">Message Text</param>
        /// <param name="type">PopupMessageType</param>
        /// <param name="okAction">Action delegate for OK button</param>
        public PopupMessageViewModel(
            string title,
            string message,
            PopupMessageType type,
            Action? okAction = null
            )
        {
            TitleText = title;
            MessageText = message;
            _okAction = okAction;

            switch (type)
            {
                case PopupMessageType.OkOnly:
                    IsOkOnly = true;
                    break;
                case PopupMessageType.OkCancel:
                    IsOkCancel = true;
                    break;
            }

            _mwvm = BindingsManager.Instance.GetInstance<MainWindowViewModel>();
            _mwvm.PopupDataContext = this;
        }

        private readonly MainWindowViewModel _mwvm;
        private readonly Action? _okAction;

        public bool IsPopupVisible { get; set; }

        public bool IsOkCancel { get; init; }

        public bool IsOkOnly { get; init; }

        public string TitleText { get; init; }

        public string MessageText { get; init; }


        #region Relay Commands

        [RelayCommand]
        private void Cancel()
        {
            IsPopupVisible = false;
            OnPropertyChanged(nameof(IsPopupVisible));
        }

        [RelayCommand]
        private void Ok()
        {
            _okAction?.Invoke();
            IsPopupVisible = false;
            OnPropertyChanged(nameof(IsPopupVisible));
        }

        #endregion Relay Commands


        /// <summary>
        /// Show popup window
        /// </summary>
        public void Show()
        {
            IsPopupVisible = true;
            OnPropertyChanged(nameof(IsPopupVisible));
        }
    }

    public enum PopupMessageType
    {
        OkOnly,
        OkCancel
    }
}
