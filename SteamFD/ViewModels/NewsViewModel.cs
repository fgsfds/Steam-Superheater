using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamFDCommon.Models;
using SteamFDTCommon.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamFD.ViewModels
{
    public partial class NewsViewModel : ObservableObject
    {
        private readonly NewsModel _newsModel;

        public List<NewsEntity> News => _newsModel.News;

        public string NewsTabHeader { get; private set; } = "News";

        public NewsViewModel(NewsModel newsModel)
        {
            _newsModel = newsModel;

            MarkAllAsRead = new RelayCommand(
                execute: () =>
                {
                    _newsModel.MarkAllAsRead();
                    UpdateHeader();
                    OnPropertyChanged(nameof(News));
                    OnPropertyChanged(nameof(NewsTabHeader));
                },
                canExecute: () => _newsModel.HasUnreadNews
                );
        }

        [RelayCommand]
        async Task InitializeAsync() => await UpdateAsync();

        private async Task UpdateAsync()
        {
            await _newsModel.UpdateNewsListAsync();

            UpdateHeader();
        }

        private void UpdateHeader()
        {
            NewsTabHeader = "News" + (_newsModel.HasUnreadNews ? $" ({_newsModel.UnreadNewsCount} unread)" : string.Empty);

            OnPropertyChanged(nameof(NewsTabHeader));

            MarkAllAsRead.NotifyCanExecuteChanged();
        }

        public IRelayCommand MarkAllAsRead { get; private set; }
    }
}
