using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Common.Models;
using Common.Entities;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Common.Helpers;
using Superheater.Avalonia.Core.Helpers;
using Common.Config;
using System.Threading;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class NewsViewModel : ObservableObject
    {
        private bool IsDeveloperMode => Properties.IsDeveloperMode;
        private readonly NewsModel _newsModel;
        private readonly ConfigEntity _config;
        private readonly SemaphoreSlim _locker = new(1, 1);

        public ObservableCollection<NewsEntity> NewsList { get; set; }

        public string NewsTabHeader { get; private set; }

        public NewsViewModel(NewsModel newsModel, ConfigProvider configProvider)
        {
            _config = configProvider.Config ?? throw new NullReferenceException(nameof(configProvider));
            NewsList = new();
            NewsTabHeader = "News";
            _newsModel = newsModel;

            _config.NotifyParameterChanged += NotifyParameterChanged;
        }


        #region Relay Commands

        [RelayCommand]
        private async Task InitializeAsync() => await UpdateAsync();

        [RelayCommand(CanExecute = (nameof(MarkAllAsReadCanExecute)))]
        private void MarkAllAsRead()
        {
            _newsModel.MarkAllAsRead();
            FillNewsList();
            UpdateHeader();
        }
        private bool MarkAllAsReadCanExecute() => _newsModel.HasUnreadNews;

        #endregion Relay Commands


        private async Task UpdateAsync()
        {
            await _locker.WaitAsync();

            try
            {
                await _newsModel.UpdateNewsListAsync();
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                new PopupMessageViewModel(
                    "Error",
                    "File not found: " + ex.Message,
                    PopupMessageType.OkOnly
                    ).Show();

                _locker.Release();
                return;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                new PopupMessageViewModel(
                    "Error",
                    "Can't connect to GitHub repository",
                    PopupMessageType.OkOnly
                    ).Show();

                _locker.Release();
                return;
            }

            FillNewsList();
            UpdateHeader();

            _locker.Release();
        }

        private void FillNewsList()
        {
            NewsList.Clear();
            NewsList.AddRange(_newsModel.News);
            MarkAllAsReadCommand.NotifyCanExecuteChanged();
        }

        private void UpdateHeader()
        {
            NewsTabHeader = "News" + (_newsModel.HasUnreadNews ? $" ({_newsModel.UnreadNewsCount} unread)" : string.Empty);
            OnPropertyChanged(nameof(NewsTabHeader));
            MarkAllAsReadCommand.NotifyCanExecuteChanged();
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
