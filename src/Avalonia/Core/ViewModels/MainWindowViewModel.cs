using ClientCommon;
using ClientCommon.Config;
using CommunityToolkit.Mvvm.ComponentModel;
using Superheater.Avalonia.Core.ViewModels.Popups;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel(ConfigProvider configProvider)
        {
            _config = configProvider.Config;
            _repositoryMessage = string.Empty;

            _config.NotifyParameterChanged += NotifyParameterChanged;

            UpdateRepoMessage();
        }

        private readonly ConfigEntity _config;


        #region Binding Properties

        public bool IsSteamGameMode => ClientProperties.IsInSteamDeckGameMode;

        public bool IsDeveloperMode => ClientProperties.IsDeveloperMode;

        [ObservableProperty]
        private PopupMessageViewModel? _popupDataContext;

        [ObservableProperty]
        private string _repositoryMessage;

        #endregion Binding Properties


        /// <summary>
        /// Update repository message (only visible in the dev mode)
        /// </summary>
        private void UpdateRepoMessage()
        {
            RepositoryMessage = _config.UseLocalApiAndRepo
                ? $"Local API"
                : $"Online API";
        }

        private void NotifyParameterChanged(string parameterName)
        {
            if (parameterName.Equals(nameof(_config.UseLocalApiAndRepo)) ||
                parameterName.Equals(nameof(_config.LocalRepoPath)))
            {
                UpdateRepoMessage();
            }
        }
    }
}