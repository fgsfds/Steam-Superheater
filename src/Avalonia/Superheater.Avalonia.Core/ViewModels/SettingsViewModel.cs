using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamFDA.Helpers;
using SteamFDCommon.Config;
using SteamFDCommon.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SteamFDA.ViewModels
{
    internal partial class SettingsViewModel : ObservableObject
    {
        private readonly ConfigEntity _config;
        private readonly MainWindowViewModel _mwvm;

        public bool LocalPathTextboxChanged;
        public bool IsDefaultTheme => _config.Theme.Equals("System");
        public bool IsLightTheme => _config.Theme.Equals("Light");
        public bool IsDarkTheme => _config.Theme.Equals("Dark");

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
            _mwvm.IsLocalRepoWarningEnabled = value;
        }

        [ObservableProperty]
        private bool _showUninstalledGamesCheckbox;
        partial void OnShowUninstalledGamesCheckboxChanged(bool value)
        {
            _config.ShowUninstalledGames = value;
        }

        [ObservableProperty]
        private string _pathToLocalRepo;
        partial void OnPathToLocalRepoChanged(string value)
        {
            var configValue = _config.LocalRepoPath;

            if (value.Equals(configValue))
            {
                LocalPathTextboxChanged = false;
                OnPropertyChanged(nameof(LocalPathTextboxChanged));
            }
            else
            {
                LocalPathTextboxChanged = true;
                OnPropertyChanged(nameof(LocalPathTextboxChanged));
            }

            SaveLocalRepoPathCommand.NotifyCanExecuteChanged();
        }

        public SettingsViewModel(ConfigProvider config, MainWindowViewModel mwvm)
        {
            _config = config.Config;
            _mwvm = mwvm;

            DeleteArchivesCheckbox = _config.DeleteZipsAfterInstall;
            OpenConfigCheckbox = _config.OpenConfigAfterInstall;
            ShowEditorCheckbox = _config.ShowEditorTab;
            UseLocalRepoCheckbox = _config.UseLocalRepo;
            PathToLocalRepo = _config.LocalRepoPath;
            ShowUninstalledGamesCheckbox = _config.ShowUninstalledGames;
        }


        #region Relay Commands

        [RelayCommand(CanExecute = (nameof(SaveLocalRepoPathCanExecute)))]
        private void SaveLocalRepoPath()
        {
            _config.LocalRepoPath = PathToLocalRepo;

            LocalPathTextboxChanged = false;
            OnPropertyChanged(nameof(LocalPathTextboxChanged));
            SaveLocalRepoPathCommand.NotifyCanExecuteChanged();
        }
        private bool SaveLocalRepoPathCanExecute() => LocalPathTextboxChanged is true;

        [RelayCommand]
        private void SetDefaultTheme()
        {
            if (Application.Current is null)
            {
                throw new NullReferenceException(nameof(Application.Current));
            }

            Application.Current.RequestedThemeVariant = ThemeVariant.Default;
            _config.Theme = "System";
        }

        [RelayCommand]
        private void SetLightTheme()
        {
            if (Application.Current is null)
            {
                throw new NullReferenceException(nameof(Application.Current));
            }

            Application.Current.RequestedThemeVariant = ThemeVariant.Light;
            _config.Theme = "Light";
        }

        [RelayCommand]
        private void SetDarkTheme()
        {
            if (Application.Current is null)
            {
                throw new NullReferenceException(nameof(Application.Current));
            }

            Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
            _config.Theme = "Dark";
        }

        [RelayCommand]
        private void OpenConfigXML()
        {
            Process.Start("explorer.exe", Consts.ConfigFile);
        }

        [RelayCommand]
        private async Task OpenFolderPicker()
        {
            var topLevel = Properties.TopLevel;

            if (topLevel is null)
            {
                throw new NullReferenceException(nameof(topLevel));
            }

            var files = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Choose local repo folder",
                AllowMultiple = false
            });

            if (!files.Any())
            {
                return;
            }

            PathToLocalRepo = files[0].Path.LocalPath;
            _config.LocalRepoPath = PathToLocalRepo;
            OnPropertyChanged(nameof(PathToLocalRepo));

            LocalPathTextboxChanged = false;
            OnPropertyChanged(nameof(LocalPathTextboxChanged));
            SaveLocalRepoPathCommand.NotifyCanExecuteChanged();
        }

        #endregion Relay Commands
    }
}