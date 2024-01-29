using Common.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Superheater.Avalonia.Core.ViewModels
{
    public sealed partial class PopupStackViewModel : ObservableObject
    {
        private SemaphoreSlim? _semaphore;
        private string _result = ConstStrings.All;

        #region Binding Properties

        public IEnumerable<string>? Items { get; set; }

        [ObservableProperty]
        private bool _isPopupStackVisible;

        [ObservableProperty]
        private string _titleText = string.Empty;

        #endregion Binding Properties


        /// <summary>
        /// Show popup window and return result
        /// </summary>
        /// <returns>Contents of the pressed button</returns>
        public async Task<string> ShowAndGetResultAsync(
            string title,
            IEnumerable<string> itemsList)
        {
            TitleText = title;
            IsPopupStackVisible = true;
            Items = itemsList;

            OnPropertyChanged(nameof(Items));

            _semaphore = new(0);
            await _semaphore.WaitAsync();

            return _result;
        }

        [RelayCommand]
        private void PopupButtonPressed(string str)
        {
            _result = str;

            Reset();
        }

        /// <summary>
        /// Reset popup to its initial state
        /// </summary>
        private void Reset()
        {
            IsPopupStackVisible = false;

            _semaphore?.Release();
            _semaphore = null;
        }
    }
}
