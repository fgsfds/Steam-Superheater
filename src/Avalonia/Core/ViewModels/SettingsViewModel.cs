using Avalonia;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Common.Config;
using Common.Enums;
using Common.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Superheater.Avalonia.Core.Helpers;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class SettingsViewModel : ObservableObject
    {
        public SettingsViewModel(ConfigProvider config)
        {
            _config = config.Config;

            DeleteArchivesCheckbox = _config.DeleteZipsAfterInstall;
            OpenConfigCheckbox = _config.OpenConfigAfterInstall;
            UseLocalApiCheckbox = _config.UseLocalApiAndRepo;
            PathToLocalRepoTextBox = _config.LocalRepoPath;
            ShowUninstalledGamesCheckbox = _config.ShowUninstalledGames;
            ShowUnsupportedFixesCheckbox = _config.ShowUnsupportedFixes;
            ApiPasswordTextBox = _config.ApiPassword;

            _config.NotifyParameterChanged += NotifyParameterChanged;

            _watcher = new FileSystemWatcher(Directory.GetCurrentDirectory());
            _watcher.NotifyFilter = NotifyFilters.FileName;
            _watcher.Renamed += NotifyFileDownloaded;
            _watcher.EnableRaisingEvents = true;
        }

        private readonly ConfigEntity _config;
        private readonly FileSystemWatcher _watcher;
        private readonly List<string> _archivesExtensions = [".zip", ".7z"];

        #region Binding Properties

        public ImmutableList<string> HiddenTagsList => [.. _config.HiddenTags];

        public bool IsDeveloperMode => Properties.IsDeveloperMode;

        public bool IsDefaultTheme => _config.Theme is ThemeEnum.System;

        public bool IsLightTheme => _config.Theme is ThemeEnum.Light;

        public bool IsDarkTheme => _config.Theme is ThemeEnum.Dark;

        public int? ZipFilesCount
        {
            get
            {
                var files = Directory.GetFiles(Directory.GetCurrentDirectory());

                var count = files.Count(x => _archivesExtensions.Any(x.EndsWith));

                return count > 0 ? count : null;
            }
        }

        public string DeleteButtonText
        {
            get => ZipFilesCount switch
                {
                    null => string.Empty,
                    1 => "Delete 1 downloaded file",
                    _ => $"Delete {ZipFilesCount} downloaded files"
                };
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveLocalRepoPathCommand))]
        private bool _isLocalPathTextboxChanged;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveApiPasswordCommand))]
        private bool _isApiPasswordTextboxChanged;

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
        private bool _useLocalApiCheckbox;
        partial void OnUseLocalApiCheckboxChanged(bool value)
        {
            _config.UseLocalApiAndRepo = value;
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
        private string _pathToLocalRepoTextBox;
        partial void OnPathToLocalRepoTextBoxChanged(string value)
        {
            var configValue = _config.LocalRepoPath;

            if (value.Equals(configValue))
            {
                IsLocalPathTextboxChanged = false;
            }
            else
            {
                IsLocalPathTextboxChanged = true;
            }
        }

        [ObservableProperty]
        private string _apiPasswordTextBox;
        partial void OnApiPasswordTextBoxChanged(string value)
        {
            var configValue = _config.ApiPassword;

            if (value.Equals(configValue))
            {
                IsApiPasswordTextboxChanged = false;
            }
            else
            {
                IsApiPasswordTextboxChanged = true;
            }
        }

        #endregion Binding Properties


        #region Relay Commands

        [RelayCommand(CanExecute = (nameof(SaveLocalRepoPathCanExecute)))]
        private void SaveLocalRepoPath()
        {
            _config.LocalRepoPath = PathToLocalRepoTextBox;

            IsLocalPathTextboxChanged = false;
        }
        private bool SaveLocalRepoPathCanExecute() => IsLocalPathTextboxChanged;

        [RelayCommand(CanExecute = (nameof(SaveApiPasswordCanExecute)))]
        private void SaveApiPassword()
        {
            _config.ApiPassword = ApiPasswordTextBox;

            IsApiPasswordTextboxChanged = false;
        }
        private bool SaveApiPasswordCanExecute() => IsApiPasswordTextboxChanged;

        [RelayCommand]
        private void SetDefaultTheme()
        {
            Application.Current.ThrowIfNull();

            Application.Current.RequestedThemeVariant = ThemeVariant.Default;
            _config.Theme = ThemeEnum.System;
        }

        [RelayCommand]
        private void SetLightTheme()
        {
            Application.Current.ThrowIfNull();

            Application.Current.RequestedThemeVariant = ThemeVariant.Light;
            _config.Theme = ThemeEnum.Light;
        }

        [RelayCommand]
        private void SetDarkTheme()
        {
            Application.Current.ThrowIfNull();

            Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
            _config.Theme = ThemeEnum.Dark;
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
            var files = await Properties.TopLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Choose local repo folder",
                AllowMultiple = false
            }).ConfigureAwait(true);

            if (files.Count == 0)
            {
                return;
            }

            PathToLocalRepoTextBox = files[0].Path.LocalPath;
            _config.LocalRepoPath = PathToLocalRepoTextBox;

            IsLocalPathTextboxChanged = false;
        }


        [RelayCommand]
        private void RemoveTag(string value)
        {
            var tags = HiddenTagsList.Remove(value);
            _config.HiddenTags = [.. tags];
            OnPropertyChanged(nameof(HiddenTagsList));
        }


        [RelayCommand]
        private void DeleteFiles()
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory());

            var archives = files.Where(x => _archivesExtensions.Any(x.EndsWith));

            foreach (var archive in archives)
            {
                File.Delete(archive);
            }

            OnPropertyChanged(nameof(ZipFilesCount));
        }

        #endregion Relay Commands


        private void NotifyParameterChanged(string parameterName)
        {
            if (parameterName.Equals(nameof(_config.HiddenTags)))
            {
                OnPropertyChanged(nameof(HiddenTagsList));
            }
        }

        private void NotifyFileDownloaded(object sender, FileSystemEventArgs e)
        {
            if (!_archivesExtensions.Any(y => e.FullPath.EndsWith(y)))
            {
                return;
            }

            OnPropertyChanged(nameof(ZipFilesCount));
            OnPropertyChanged(nameof(DeleteButtonText));
        }
    }
}