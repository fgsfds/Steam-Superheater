using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Common.Config;
using Common.Helpers;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using System.Windows.Controls;

namespace Superheater.ViewModels
{
    public sealed partial class SettingsViewModel : ObservableObject
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
        private bool _useLocalRepoCheckbox;
        partial void OnUseLocalRepoCheckboxChanged(bool value)
        {
            _config.UseLocalRepo = value;
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

            DeleteArchivesCheckbox = _config.DeleteZipsAfterInstall;
            OpenConfigCheckbox = _config.OpenConfigAfterInstall;
            UseLocalRepoCheckbox = _config.UseLocalRepo;
            PathToLocalRepo = _config.LocalRepoPath;
        }


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
        private void OpenConfigXML()
        {
            Process.Start("explorer.exe", Consts.ConfigFile);
        }

        [RelayCommand]
        private void OpenFolderPicker()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Select a Directory",
                Filter = "Directory|*.this.directory",
                FileName = "select"
            };

            if (dialog.ShowDialog() is not true)
            {
                return;
            }

            var path = dialog.FileName;

            if (path is null)
            {
                return;
            }

            path = path.Replace("\\select.this.directory", "");
            path = path.Replace(".this.directory", "");
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            PathToLocalRepo = path;
            _config.LocalRepoPath = PathToLocalRepo;
            OnPropertyChanged(nameof(PathToLocalRepo));

            LocalPathTextboxChanged = false;
            OnPropertyChanged(nameof(LocalPathTextboxChanged));
            SaveLocalRepoPathCommand.NotifyCanExecuteChanged();
        }
    }
}
