using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Common.Models;
using Common.Entities;
using System.Threading.Tasks;
using System;
using System.Windows;
using System.IO;
using System.Net.Http;
using System.Collections.Immutable;

namespace Superheater.ViewModels
{
    public sealed partial class NewsViewModel : ObservableObject
    {
        private readonly NewsModel _newsModel;

        public ImmutableList<NewsEntity> NewsList => _newsModel.News;

        public string NewsTabHeader { get; private set; }

        public NewsViewModel(NewsModel newsModel)
        {
            NewsTabHeader = "News";
            _newsModel = newsModel;
        }

        #region Relay Commands

        [RelayCommand]
        private async Task InitializeAsync() => await UpdateAsync();

        [RelayCommand(CanExecute = (nameof(MarkAllAsReadCanExecute)))]
        private async Task MarkAllAsReadAsync()
        {
            await _newsModel.MarkAllAsReadAsync();
            OnNewsChanged();
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

            OnNewsChanged();
        }

        private void OnNewsChanged()
        {
            OnPropertyChanged(nameof(NewsList));
            NewsTabHeader = "News" + (_newsModel.HasUnreadNews ? $" ({_newsModel.UnreadNewsCount} unread)" : string.Empty);
            OnPropertyChanged(nameof(NewsTabHeader));
            MarkAllAsReadCommand.NotifyCanExecuteChanged();
        }
    }
}
