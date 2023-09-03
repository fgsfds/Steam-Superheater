using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamFDCommon;
using SteamFDCommon.Config;
using System.Diagnostics;
using System.Linq;

namespace SteamFDA.ViewModels
{
    internal partial class SettingsViewModel : ViewModelBase
    {
        private readonly ConfigEntity _config;
        private readonly MainWindowViewModel _mwvm;

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
            _mwvm.IsLocalRepoWarningEnabled = value;
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

        public SettingsViewModel(ConfigProvider config, MainWindowViewModel mwvm)
        {
            _config = config.Config;
            _mwvm = mwvm;

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

            OpenFolderPickerCommand = new RelayCommand(
                execute: async () =>
                {
                    var window = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;

                    var topLevel = TopLevel.GetTopLevel(window);

                    // Start async operation to open the dialog.
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
                    SaveLocalRepoPathCommand.NotifyCanExecuteChanged();
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

        public IRelayCommand OpenFolderPickerCommand { get; private set; }
    }
}