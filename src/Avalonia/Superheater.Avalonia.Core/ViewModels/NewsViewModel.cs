using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Common.Models;
using Common.Entities;
using System;
using System.Threading.Tasks;
using Superheater.Avalonia.Core.Helpers;
using Common.Config;
using System.Threading;
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

        public ImmutableList<NewsEntity> NewsList => _newsModel.News;

        public string NewsTabHeader { get; private set; }


        #region Relay Commands

        [RelayCommand]
        private async Task InitializeAsync() => await UpdateAsync();

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

            OnNewsChanged();
        }
        private bool MarkAllAsReadCanExecute() => _newsModel.HasUnreadNews;

        #endregion Relay Commands


        private async Task UpdateAsync()
        {
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

            OnNewsChanged();
        }

        private void OnNewsChanged()
        {
            OnPropertyChanged(nameof(NewsList));
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
                await _locker.WaitAsync();
                await UpdateAsync();
                _locker.Release();
            }
        }
    }
}
