using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamFDCommon.Models;
using SteamFDCommon.Entities;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using SteamFDCommon.Helpers;

namespace SteamFDA.ViewModels
{
    internal partial class NewsViewModel : ObservableObject
    {
        private readonly NewsModel _newsModel;

        public ObservableCollection<NewsEntity> NewsList { get; set; }

        public string NewsTabHeader { get; private set; }

        public NewsViewModel(NewsModel newsModel)
        {
            NewsList = new();
            _newsModel = newsModel;
        }


        #region Relay Commands

        [RelayCommand]
        async Task InitializeAsync() => await UpdateAsync();

        [RelayCommand(CanExecute = (nameof(MarkAllAsReadCanExecute)))]
        private void MarkAllAsRead()
        {
            _newsModel.MarkAllAsRead();
            FillNewsList();
        }
        private bool MarkAllAsReadCanExecute() => _newsModel.HasUnreadNews;

        #endregion Relay Commands


        private async Task UpdateAsync()
        {
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

                return;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                new PopupMessageViewModel(
                    "Error",
                    "Can't connect to GitHub repository",
                    PopupMessageType.OkOnly
                    ).Show();

                return;
            }

            FillNewsList();
            UpdateHeader();
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
    }
}
