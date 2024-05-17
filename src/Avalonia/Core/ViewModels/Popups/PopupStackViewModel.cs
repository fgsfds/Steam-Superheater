using Common.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Superheater.Avalonia.Core.ViewModels.Popups
{
    public sealed partial class PopupStackViewModel : ObservableObject, IPopup
    {
        private SemaphoreSlim? _semaphore;
        private string _result = Consts.All;

        public event Action<bool>? PopupShownEvent;


        #region Binding Properties

        public IEnumerable<string>? Items { get; set; }

        [ObservableProperty]
        private bool _isVisible;

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
            IsVisible = true;
            Items = itemsList;

            OnPropertyChanged(nameof(Items));

            _semaphore = new(0);
            await _semaphore.WaitAsync().ConfigureAwait(true);

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
            IsVisible = false;

            _semaphore?.Release();
            _semaphore = null;
        }
    }
}
