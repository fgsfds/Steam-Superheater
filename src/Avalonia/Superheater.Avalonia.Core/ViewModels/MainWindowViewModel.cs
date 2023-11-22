using Common.Config;
using Common.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Superheater.Avalonia.Core.Helpers;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class MainWindowViewModel : ObservableObject
    {
        private readonly ConfigEntity _config;
        private readonly CommonProperties _properties;

        public MainWindowViewModel(
            ConfigProvider configProvider,
            CommonProperties properties
            )
        {
            _config = configProvider.Config ?? ThrowHelper.ArgumentNullException<ConfigEntity>(nameof(configProvider));
            _properties = properties ?? ThrowHelper.NullReferenceException<CommonProperties>(nameof(properties));
            _repositoryMessage = string.Empty;

            _config.NotifyParameterChanged += NotifyParameterChanged;

            UpdateRepoMessage();
        }


        #region Binding Properties

        public bool IsSteamGameMode => _properties.IsInSteamDeckGameMode;

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
                RepositoryMessage = $"Online repo: {_properties.CurrentFixesRepo}";
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