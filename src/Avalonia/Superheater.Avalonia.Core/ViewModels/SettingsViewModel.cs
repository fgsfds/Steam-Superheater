using Avalonia;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Superheater.Avalonia.Core.Helpers;
using Common.Config;
using Common.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class SettingsViewModel : ObservableObject
    {
        public SettingsViewModel(ConfigProvider config, MainWindowViewModel mwvm)
        {
            _config = config.Config;
            _mwvm = mwvm;

            DeleteArchivesCheckbox = _config.DeleteZipsAfterInstall;
            OpenConfigCheckbox = _config.OpenConfigAfterInstall;
            UseLocalRepoCheckbox = _config.UseLocalRepo;
            PathToLocalRepo = _config.LocalRepoPath;
            ShowUninstalledGamesCheckbox = _config.ShowUninstalledGames;
            UseTestRepoBranchCheckbox = _config.UseTestRepoBranch;
            ShowUnsupportedFixesCheckbox = _config.ShowUnsupportedFixes;
        }

        private readonly ConfigEntity _config;
        private readonly MainWindowViewModel _mwvm;
        private readonly SemaphoreSlim _locker = new(1, 1);

        public bool LocalPathTextboxChanged;
        public bool IsDeveloperMode => Properties.IsDeveloperMode;
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
        private bool _useLocalRepoCheckbox;
        partial void OnUseLocalRepoCheckboxChanged(bool value)
        {
            _config.UseLocalRepo = value;
        }

        [ObservableProperty]
        private bool _useTestRepoBranchCheckbox;
        partial void OnUseTestRepoBranchCheckboxChanged(bool value)
        {
            _config.UseTestRepoBranch = value;
        }

        [ObservableProperty]
        private bool _showUninstalledGamesCheckbox;
        partial void OnShowUninstalledGamesCheckboxChanged(bool value)
        {
            _config.ShowUninstalledGames = value;
        }

        [ObservableProperty]
        private bool _showUnsupportedFixesCheckbox;
        partial void OnShowUnsupportedFixesCheckboxChanged(bool value)
        {
            _config.ShowUnsupportedFixes = value;
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
            Process.Start(new ProcessStartInfo
            {
                FileName = Consts.ConfigFile,
                UseShellExecute = true
            });
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