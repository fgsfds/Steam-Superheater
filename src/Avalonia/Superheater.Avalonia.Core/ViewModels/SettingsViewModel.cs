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
        public SettingsViewModel(ConfigProvider config)
        {
            _config = config.Config;

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

        #region Binding Properties

        public ImmutableList<string> HiddenTagsList => [.. _config.HiddenTags];

        public bool IsDeveloperMode => Properties.IsDeveloperMode;

        public bool IsDefaultTheme => _config.Theme is ThemeEnum.System;

        public bool IsLightTheme => _config.Theme is ThemeEnum.Light;

        public bool IsDarkTheme => _config.Theme is ThemeEnum.Dark;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveLocalRepoPathCommand))]
        private bool _isLocalPathTextboxChanged;

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
            }
            else
            {
                IsLocalPathTextboxChanged = true;
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

            IsLocalPathTextboxChanged = false;
        }


        [RelayCommand]
        private void RemoveTag(string value)
        {
            var tags = HiddenTagsList.Remove(value);
            _config.HiddenTags = [.. tags];
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