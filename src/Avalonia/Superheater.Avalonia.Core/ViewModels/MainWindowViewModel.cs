using Common.Config;
using Common.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Superheater.Avalonia.Core.Helpers;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class MainWindowViewModel : ObservableObject
    {
        private readonly ConfigEntity _config;

        public MainWindowViewModel(ConfigProvider configProvider)
        {
            _config = configProvider.Config ?? ThrowHelper.ArgumentNullException<ConfigEntity>(nameof(configProvider));
            _repositoryMessage = string.Empty;

            _config.NotifyParameterChanged += NotifyParameterChanged;

            UpdateRepoMessage();
        }


        #region Binding Properties

        public static bool IsSteamGameMode => CommonProperties.IsInSteamDeckGameMode;

        public static bool IsDeveloperMode => Properties.IsDeveloperMode;

        [ObservableProperty]
        public PopupMessageViewModel? _popupDataContext;

        [ObservableProperty]
        private string _repositoryMessage;

        #endregion Binding Properties


        /// <summary>
        /// Update repository message (only visible in the dev mode)
        /// </summary>
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

        private void NotifyParameterChanged(string parameterName)
        {
            if (parameterName.Equals(nameof(_config.UseLocalRepo)) ||
                parameterName.Equals(nameof(_config.UseTestRepoBranch)) ||
                parameterName.Equals(nameof(_config.LocalRepoPath)))
            {
                UpdateRepoMessage();
            }
        }
    }
}