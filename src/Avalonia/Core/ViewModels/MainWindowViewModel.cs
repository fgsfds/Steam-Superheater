using Common.Client.Config;
using CommunityToolkit.Mvvm.ComponentModel;
using Superheater.Avalonia.Core.ViewModels.Popups;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class MainWindowViewModel : ObservableObject
    {
        private readonly IConfigProvider _config;


        #region Binding Properties

        [ObservableProperty]
        private PopupMessageViewModel? _popupDataContext;

        [ObservableProperty]
        private string _repositoryMessage;

        #endregion Binding Properties


        public MainWindowViewModel(IConfigProvider configProvider)
        {
            _config = configProvider;
            _repositoryMessage = string.Empty;

            _config.ParameterChangedEvent += OnParameterChangedEvent;

            UpdateRepoMessage();
        }


        /// <summary>
        /// Update repository message (only visible in the dev mode)
        /// </summary>
        private void UpdateRepoMessage()
        {
            RepositoryMessage = _config.UseLocalApiAndRepo
                ? $"Local API"
                : $"Online API";
        }

        private void OnParameterChangedEvent(string parameterName)
        {
            if (parameterName.Equals(nameof(_config.UseLocalApiAndRepo)) ||
                parameterName.Equals(nameof(_config.LocalRepoPath)))
            {
                UpdateRepoMessage();
            }
        }
    }
}