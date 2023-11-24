using Common.Config;
using Common.Entities;
using Common.Helpers;
using Common.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Superheater.Avalonia.Core.Helpers;
using System.Collections.Immutable;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class NewsViewModel : ObservableObject
    {
        public NewsViewModel(
            NewsModel newsModel, 
            ConfigProvider configProvider,
            PopupMessageViewModel popupMessage
            )
        {
            _config = configProvider.Config ?? ThrowHelper.ArgumentNullException<ConfigEntity>(nameof(configProvider));
            _popupMessage = popupMessage ?? ThrowHelper.ArgumentNullException<PopupMessageViewModel>(nameof(popupMessage)); ;

            NewsTabHeader = "News";
            _newsModel = newsModel;

            _config.NotifyParameterChanged += NotifyParameterChanged;
        }

        private static bool IsDeveloperMode => Properties.IsDeveloperMode;
        private readonly NewsModel _newsModel;
        private readonly ConfigEntity _config;
        private readonly PopupMessageViewModel _popupMessage;
        private readonly SemaphoreSlim _locker = new(1);


        #region Binding Properties

        public ImmutableList<NewsEntity> NewsList => _newsModel.News;

        public string NewsTabHeader { get; private set; }

        #endregion Binding Properties


        #region Relay Commands

        /// <summary>
        /// VM initialization
        /// </summary>
        [RelayCommand]
        private async Task InitializeAsync() => await UpdateAsync();

        /// <summary>
        /// Mark all news as read
        /// </summary>
        /// <returns></returns>
        [RelayCommand(CanExecute = (nameof(MarkAllAsReadCanExecute)))]
        private async Task MarkAllAsReadAsync()
        {
            var result = await _newsModel.MarkAllAsReadAsync();

            if (!result.IsSuccess)
            {
                _popupMessage.Show(
                    "Error",
                    result.Message,
                    PopupMessageType.OkOnly
                    );

                return;
            }

            OnPropertyChanged(nameof(NewsList));
            UpdateHeader();
            MarkAllAsReadCommand.NotifyCanExecuteChanged();
        }
        private bool MarkAllAsReadCanExecute() => _newsModel.HasUnreadNews;

        #endregion Relay Commands

        /// <summary>
        /// Update news list and tab header
        /// </summary>
        /// <returns></returns>
        private async Task UpdateAsync()
        {
            await _locker.WaitAsync();
            var result = await _newsModel.UpdateNewsListAsync();

            if (!result.IsSuccess)
            {
                _popupMessage.Show(
                    "Error",
                    result.Message,
                    PopupMessageType.OkOnly
                    );

                return;
            }

            OnPropertyChanged(nameof(NewsList));
            UpdateHeader();
            MarkAllAsReadCommand.NotifyCanExecuteChanged();

            _locker.Release();
        }

        /// <summary>
        /// Update tab header
        /// </summary>
        private void UpdateHeader()
        {
            NewsTabHeader = "News" + (_newsModel.HasUnreadNews
                ? $" ({_newsModel.UnreadNewsCount} unread)"
                : string.Empty);

            OnPropertyChanged(nameof(NewsTabHeader));
        }

        private async void NotifyParameterChanged(string parameterName)
        {
            if (parameterName.Equals(nameof(_config.UseTestRepoBranch)) ||
                parameterName.Equals(nameof(_config.UseLocalRepo)) ||
                parameterName.Equals(nameof(_config.LocalRepoPath)))
            {
                await UpdateAsync();
            }
        }
    }
}
