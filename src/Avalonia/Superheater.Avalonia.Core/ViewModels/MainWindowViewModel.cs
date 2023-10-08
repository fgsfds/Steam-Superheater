using Common.Config;
using Common.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Superheater.Avalonia.Core.Helpers;
using System;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class MainWindowViewModel : ObservableObject
    {
        private readonly ConfigEntity _config;

        public MainWindowViewModel(ConfigProvider configProvider)
        {
            _config = configProvider.Config ?? throw new NullReferenceException(nameof(configProvider));
            _config.NotifyParameterChanged += NotifyParameterChanged;

            UpdateRepoMessage();
        }

        public bool IsSteamGameMode => CommonProperties.IsInSteamDeckGameMode;

        public bool IsDeveloperMode => Properties.IsDeveloperMode;

        [ObservableProperty]
        public PopupMessageViewModel? _popupDataContext;

        [ObservableProperty]
        private string _repositoryMessage;

        private void NotifyParameterChanged(string parameterName)
        {
            if (parameterName.Equals(nameof(_config.UseLocalRepo)) ||
                parameterName.Equals(nameof(_config.UseTestRepoBranch)) ||
                parameterName.Equals(nameof(_config.LocalRepoPath)))
            {
                UpdateRepoMessage();
            }
        }

        private void UpdateRepoMessage()
        {
            if (_config.UseLocalRepo)
            {
                RepositoryMessage = $"Local repo: {_config.LocalRepoPath}";
            }
            else
            {
                RepositoryMessage = $"Online repo: {CommonProperties.CurrentFixesRepo}";
            }
        }
    }
}