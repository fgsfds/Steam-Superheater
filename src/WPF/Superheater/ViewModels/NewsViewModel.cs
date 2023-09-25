using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamFDCommon.Models;
using SteamFDCommon.Entities;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System;
using System.Windows;
using System.IO;
using System.Net.Http;
using SteamFDCommon.Helpers;

namespace SteamFD.ViewModels
{
    public partial class NewsViewModel : ObservableObject
    {
        private readonly NewsModel _newsModel;

        public ObservableCollection<NewsEntity> NewsList { get; set; }

        public string NewsTabHeader { get; private set; }

        public NewsViewModel(NewsModel newsModel)
        {
            NewsList = new();
            NewsTabHeader = "News";
            _newsModel = newsModel;
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
            try
            {
                await _newsModel.UpdateNewsListAsync();
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                MessageBox.Show(
                    "File not found: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK
                    );

                return;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                MessageBox.Show(
                    "Can't connect to GitHub repository",
                    "Error",
                    MessageBoxButton.OK
                    );

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
