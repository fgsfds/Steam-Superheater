using ClientCommon;
using ClientCommon.Config;
using ClientCommon.Models;
using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Superheater.Avalonia.Core.ViewModels.Popups;
using System.Collections.Immutable;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class NewsViewModel : ObservableObject
    {
        private readonly NewsModel _newsModel;
        private readonly ConfigEntity _config;
        private readonly PopupMessageViewModel _popupMessage;
        private readonly PopupEditorViewModel _popupEditor;
        private readonly SemaphoreSlim _locker = new(1);


        #region Binding Properties

        public ImmutableList<NewsEntity> NewsList => _newsModel.News;

        public bool IsDeveloperMode => ClientProperties.IsDeveloperMode;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MarkAllAsReadCommand))]
        private string _newsTabHeader = "News";

        #endregion Binding Properties


        public NewsViewModel(
            NewsModel newsModel,
            ConfigProvider configProvider,
            PopupMessageViewModel popupMessage,
            PopupEditorViewModel popupEditor
        )
        {
            _newsModel = newsModel;
            _config = configProvider.Config;
            _popupMessage = popupMessage;
            _popupEditor = popupEditor;

            _config.NotifyParameterChanged += NotifyParameterChanged;
        }


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
        private void MarkAllAsRead()
        {
            _newsModel.UpdateConfigLastReadVersion();

            //if (!result.IsSuccess)
            //{
            //    _popupMessage.Show(
            //        "Error",
            //        result.Message,
            //        PopupMessageType.OkOnly
            //        );

            //    return;
            //}

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
            var newContent = await _popupEditor.ShowAndGetResultAsync("Add news entry", string.Empty).ConfigureAwait(true);

            if (newContent is null)
            {
                return;
            }

            var result = await _newsModel.AddNewsAsync(newContent).ConfigureAwait(true);

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

            var newContent = await _popupEditor.ShowAndGetResultAsync("Edit news entry", news.Content).ConfigureAwait(true);

            if (newContent is null)
            {
                return;
            }

            var result = await _newsModel.ChangeNewsContentAsync(date, newContent).ConfigureAwait(true);

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
            await _locker.WaitAsync().ConfigureAwait(true);

            var result = await _newsModel.UpdateNewsListAsync().ConfigureAwait(true);

            if (!result.IsSuccess)
            {
                _popupMessage.Show(
                    "Error",
                    result.Message,
                    PopupMessageType.OkOnly
                    );
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

        private async void NotifyParameterChanged(string parameterName)
        {
            if (parameterName.Equals(nameof(_config.UseLocalApiAndRepo)))
            {
                await UpdateAsync().ConfigureAwait(true);
            }
        }
    }
}
