using Common.Config;
using Common.Entities;
using Common.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Superheater.Avalonia.Core.Helpers;
using Superheater.Avalonia.Core.ViewModels.Popups;
using System.Collections.Immutable;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class NewsViewModel(
        NewsModel newsModel,
        ConfigProvider configProvider,
        PopupMessageViewModel popupMessage,
        PopupEditorViewModel popupEditor
        ) : ObservableObject
    {
        private readonly NewsModel _newsModel = newsModel;
        private readonly ConfigEntity _config = configProvider.Config;
        private readonly PopupMessageViewModel _popupMessage = popupMessage;
        private readonly PopupEditorViewModel _popupEditor = popupEditor;
        private readonly SemaphoreSlim _locker = new(1);


        #region Binding Properties

        public ImmutableList<NewsEntity> NewsList => _newsModel.News;

        public bool IsDeveloperMode => Properties.IsDeveloperMode;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MarkAllAsReadCommand))]
        private string _newsTabHeader = "News";

        #endregion Binding Properties


        #region Relay Commands

        /// <summary>
        /// VM initialization
        /// </summary>
        [RelayCommand]
        private Task InitializeAsync() => UpdateAsync();

        /// <summary>
        /// Mark all news as read
        /// </summary>
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
        }
        private bool MarkAllAsReadCanExecute() => _newsModel.HasUnreadNews;

        /// <summary>
        /// Add news
        /// </summary>
        [RelayCommand]
        private async Task AddNews()
        {
            var newContent = await _popupEditor.ShowAndGetResultAsync("Add news entry", string.Empty);

            var result = await _newsModel.AddNewsAsync(newContent!);

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
        }

        /// <summary>
        /// Edit current news content
        /// </summary>
        [RelayCommand]
        private async Task EditNews(DateTime date)
        {
            var news = NewsList.FirstOrDefault(x => x.Date == date);

            if (news is null)
            {
                return;
            }

            var newContent = await _popupEditor.ShowAndGetResultAsync("Edit news entry", news.Content);

            if (newContent is null)
            {
                return;
            }

            var result = await _newsModel.ChangeNewsContentAsync(date, newContent!);

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
        }

        #endregion Relay Commands

        /// <summary>
        /// Update news list and tab header
        /// </summary>
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
        }
    }
}
