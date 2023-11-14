using Avalonia;
using Avalonia.Controls;
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
        public SettingsViewModel(ConfigProvider config, MainWindowViewModel mwvm)
        {
            _config = config.Config;
            _mwvm = mwvm;

            DeleteArchivesCheckbox = _config.DeleteZipsAfterInstall;
            OpenConfigCheckbox = _config.OpenConfigAfterInstall;
            UseLocalRepoCheckbox = _config.UseLocalRepo;
            PathToLocalRepoTextBox = _config.LocalRepoPath;
            ShowUninstalledGamesCheckbox = _config.ShowUninstalledGames;
            UseTestRepoBranchCheckbox = _config.UseTestRepoBranch;
            ShowUnsupportedFixesCheckbox = _config.ShowUnsupportedFixes;

            _config.NotifyParameterChanged += NotifyParameterChanged;
        }

        private readonly ConfigEntity _config;
        private readonly MainWindowViewModel _mwvm;
        private readonly SemaphoreSlim _locker = new(1, 1);

        public bool IsLocalPathTextboxChanged;


        #region Binding Properties

        public ImmutableList<string> HiddenTagsList => _config.HiddenTags.ToImmutableList();


        public bool IsDeveloperMode => Properties.IsDeveloperMode;

        public bool IsDefaultTheme => _config.Theme is ThemeEnum.System;

        public bool IsLightTheme => _config.Theme is ThemeEnum.Light;

        public bool IsDarkTheme => _config.Theme is ThemeEnum.Dark;

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
        private string _pathToLocalRepoTextBox;
        partial void OnPathToLocalRepoTextBoxChanged(string value)
        {
            var configValue = _config.LocalRepoPath;

            if (value.Equals(configValue))
            {
                IsLocalPathTextboxChanged = false;
                OnPropertyChanged(nameof(IsLocalPathTextboxChanged));
            }
            else
            {
                IsLocalPathTextboxChanged = true;
                OnPropertyChanged(nameof(IsLocalPathTextboxChanged));
            }

            SaveLocalRepoPathCommand.NotifyCanExecuteChanged();
        }

        #endregion Binding Properties


        #region Relay Commands

        [RelayCommand(CanExecute = (nameof(SaveLocalRepoPathCanExecute)))]
        private void SaveLocalRepoPath()
        {
            _config.LocalRepoPath = PathToLocalRepoTextBox;

            IsLocalPathTextboxChanged = false;
            OnPropertyChanged(nameof(IsLocalPathTextboxChanged));
            SaveLocalRepoPathCommand.NotifyCanExecuteChanged();
        }
        private bool SaveLocalRepoPathCanExecute() => IsLocalPathTextboxChanged is true;

        [RelayCommand]
        private void SetDefaultTheme()
        {
            if (Application.Current is null) ThrowHelper.NullReferenceException(nameof(Application.Current));

            Application.Current.RequestedThemeVariant = ThemeVariant.Default;
            _config.Theme = ThemeEnum.System;
        }

        [RelayCommand]
        private void SetLightTheme()
        {
            if (Application.Current is null) ThrowHelper.NullReferenceException(nameof(Application.Current));

            Application.Current.RequestedThemeVariant = ThemeVariant.Light;
            _config.Theme = ThemeEnum.Light;
        }

        [RelayCommand]
        private void SetDarkTheme()
        {
            if (Application.Current is null) ThrowHelper.NullReferenceException(nameof(Application.Current));

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
            var topLevel = Properties.TopLevel ?? ThrowHelper.ArgumentNullException<TopLevel>(nameof(Properties.TopLevel));

            var files = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Choose local repo folder",
                AllowMultiple = false
            });

            if (files.Count == 0)
            {
                return;
            }

            PathToLocalRepoTextBox = files[0].Path.LocalPath;
            _config.LocalRepoPath = PathToLocalRepoTextBox;
            OnPropertyChanged(nameof(PathToLocalRepoTextBox));

            IsLocalPathTextboxChanged = false;
            OnPropertyChanged(nameof(IsLocalPathTextboxChanged));
            SaveLocalRepoPathCommand.NotifyCanExecuteChanged();
        }


        [RelayCommand]
        private void RemoveTag(string value)
        {
            var tags = HiddenTagsList.Remove(value);
            _config.HiddenTags = tags.ToList();
            OnPropertyChanged(nameof(HiddenTagsList));
        }

        #endregion Relay Commands


        private void NotifyParameterChanged(string parameterName)
        {
            if (parameterName.Equals(nameof(_config.HiddenTags)))
            {
                OnPropertyChanged(nameof(HiddenTagsList));
            }
        }
    }
}