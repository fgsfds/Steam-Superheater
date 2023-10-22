using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Common.Models;
using Common.Entities;
using Superheater.Avalonia.Core.Helpers;
using Common.Config;
using System.Collections.Immutable;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class NewsViewModel : ObservableObject
    {
        public NewsViewModel(NewsModel newsModel, ConfigProvider configProvider)
        {
            _config = configProvider.Config ?? throw new NullReferenceException(nameof(configProvider));

            NewsTabHeader = "News";
            _newsModel = newsModel;

            _config.NotifyParameterChanged += NotifyParameterChanged;
        }

        private bool IsDeveloperMode => Properties.IsDeveloperMode;
        private readonly NewsModel _newsModel;
        private readonly ConfigEntity _config;
        private readonly SemaphoreSlim _locker = new(1, 1);


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

            if (!result.Item1)
            {
                new PopupMessageViewModel(
                    "Error",
                    result.Item2,
                    PopupMessageType.OkOnly
                    ).Show();

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

            if (!result.Item1)
            {
                new PopupMessageViewModel(
                    "Error",
                    result.Item2,
                    PopupMessageType.OkOnly
                    ).Show();

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
