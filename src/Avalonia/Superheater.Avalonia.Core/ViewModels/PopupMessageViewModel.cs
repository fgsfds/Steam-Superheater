using Common.DI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
                case PopupMessageType.YesNo:
                    IsYesNo = true;
                    break;
            }

            _mwvm = BindingsManager.Instance.GetInstance<MainWindowViewModel>();
            _mwvm.PopupDataContext = this;
        }

        private readonly Action? _okAction;
        private readonly MainWindowViewModel _mwvm;
        private SemaphoreSlim? _semaphore;
        private bool _result;


        #region Binding Properties

        public bool IsPopupVisible { get; private set; }

        public bool IsYesNo { get; init; }

        public bool IsOkOnly { get; init; }

        public string TitleText { get; init; }

        public string MessageText { get; init; }

        #endregion Binding Properties


        #region Relay Commands

        [RelayCommand]
        private void Cancel()
        {
            _result = false;

            IsPopupVisible = false;
            OnPropertyChanged(nameof(IsPopupVisible));

            _semaphore?.Release();
        }

        [RelayCommand]
        private void Ok()
        {
            _result = true;

            IsPopupVisible = false;
            OnPropertyChanged(nameof(IsPopupVisible));

            _semaphore?.Release();

            _okAction?.Invoke();
        }

        #endregion Relay Commands


        /// <summary>
        /// Show popup window and return result
        /// </summary>
        /// <returns>true if Ok or Yes pressed, false if Cancel pressed</returns>
        public async Task<bool> ShowAndGetResultAsync()
        {
            _semaphore = new(1, 1);
            await _semaphore.WaitAsync();

            IsPopupVisible = true;
            OnPropertyChanged(nameof(IsPopupVisible));

            await _semaphore.WaitAsync();

            return _result;
        }

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
        ///<summary>
        ///OK button that executes action
        ///</summary>
        OkOnly,
        ///<summary>
        ///Yes and No buttons, Yes button executes action
        ///</summary>
        YesNo
    }
}
