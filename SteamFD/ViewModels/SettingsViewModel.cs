using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamFDCommon.Config;

namespace SteamFD.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ConfigEntity _config;

        [ObservableProperty]
        private bool _localPathTextboxChanged;

        [ObservableProperty]
        private bool _deleteArchivesCheckbox;
        partial void OnDeleteArchivesCheckboxChanged(bool value)
        {
            _config.DeleteZipsAfterInstall = value;
        }

        [ObservableProperty]
        private bool _openConfigCheckbox;
        partial void OnOpenConfigCheckboxChanged(bool value)
        {
            _config.OpenConfigAfterInstall = value;
        }

        [ObservableProperty]
        private bool _showEditorCheckbox;
        partial void OnShowEditorCheckboxChanged(bool value)
        {
            _config.ShowEditorTab = value;
        }

        [ObservableProperty]
        private bool _useLocalRepoCheckbox;
        partial void OnUseLocalRepoCheckboxChanged(bool value)
        {
            _config.UseLocalRepo = value;
        }

        [ObservableProperty]
        private string _pathToLocalRepo;
        partial void OnPathToLocalRepoChanged(string value)
        {
            var configValue = _config.LocalRepoPath;

            if (value.Equals(configValue))
            {
                LocalPathTextboxChanged = false;
            }
            else
            {
                LocalPathTextboxChanged = true;
            }

            SaveLocalRepoPathCommand.NotifyCanExecuteChanged();
        }

        public SettingsViewModel(ConfigProvider config)
        {
            _config = config.Config;

            SaveLocalRepoPathCommand = new RelayCommand(
                execute: () =>
                {
                    _config.LocalRepoPath = PathToLocalRepo;

                    LocalPathTextboxChanged = false;
                    SaveLocalRepoPathCommand.NotifyCanExecuteChanged();
                },
                canExecute: () => LocalPathTextboxChanged is true
                );

            DeleteArchivesCheckbox = _config.DeleteZipsAfterInstall;
            OpenConfigCheckbox = _config.OpenConfigAfterInstall;
            ShowEditorCheckbox = _config.ShowEditorTab;
            UseLocalRepoCheckbox = _config.UseLocalRepo;
            PathToLocalRepo = _config.LocalRepoPath;
        }

        public IRelayCommand SaveLocalRepoPathCommand { get; private set; }
    }
}
