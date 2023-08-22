using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamFDCommon;
using SteamFDCommon.Config;
using System.Diagnostics;

namespace SteamFDA.ViewModels
{
    internal partial class SettingsViewModel : ViewModelBase
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

        public bool IsDefaultTheme => _config.Theme.Equals("System");
        public bool IsLightTheme => _config.Theme.Equals("Light");
        public bool IsDarkTheme => _config.Theme.Equals("Dark");

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

            SetDefaultTheme = new RelayCommand(
                execute: () =>
                {
                    App.Current.RequestedThemeVariant = ThemeVariant.Default;
                    _config.Theme = "System";
                }
                );

            SetLightTheme = new RelayCommand(
                execute: () =>
                {
                    App.Current.RequestedThemeVariant = ThemeVariant.Light;
                    _config.Theme = "Light";
                }
                );

            SetDarkTheme = new RelayCommand(
                execute: () =>
                {
                    App.Current.RequestedThemeVariant = ThemeVariant.Dark;
                    _config.Theme = "Dark";
                }
                );

            OpenConfigXML = new RelayCommand(
                execute: () =>
                {
                    using Process process = new();

                    Process.Start(
                        "explorer.exe",
                        Consts.ConfigFile
                        );
                }
                );

            DeleteArchivesCheckbox = _config.DeleteZipsAfterInstall;
            OpenConfigCheckbox = _config.OpenConfigAfterInstall;
            ShowEditorCheckbox = _config.ShowEditorTab;
            UseLocalRepoCheckbox = _config.UseLocalRepo;
            PathToLocalRepo = _config.LocalRepoPath;
        }

        public IRelayCommand SaveLocalRepoPathCommand { get; private set; }

        public IRelayCommand SetDefaultTheme { get; private set; }

        public IRelayCommand SetLightTheme { get; private set; }

        public IRelayCommand SetDarkTheme { get; private set; }

        public IRelayCommand OpenConfigXML { get; private set; }
    }
}