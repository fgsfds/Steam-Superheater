using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Common;
using Common.CombinedEntities;
using Common.Config;
using Common.Helpers;
using Common.Models;
using Common.Entities;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Collections.Immutable;

namespace Superheater.ViewModels
{
    internal sealed partial class MainViewModel : ObservableObject
    {
        public MainViewModel(
            MainModel mainModel,
            ConfigProvider config
            )
        {
            _mainModel = mainModel ?? throw new NullReferenceException(nameof(mainModel));
            _config = config?.Config ?? throw new NullReferenceException(nameof(config));

            MainTabHeader = "Main";
            LaunchGameButtonText = "Launch game...";
            _search = string.Empty;

            _config.NotifyParameterChanged += NotifyParameterChanged;
        }

        private readonly MainModel _mainModel;
        private readonly ConfigEntity _config;
        private bool _lockButtons;
        private readonly SemaphoreSlim _locker = new(1, 1);

        public string MainTabHeader { get; private set; }

        public float ProgressBarValue { get; set; }

        public string LaunchGameButtonText { get; private set; }

        public static bool IsSteamGameMode => CommonProperties.IsInSteamDeckGameMode;

        /// <summary>
        /// Does selected fix has variants
        /// </summary>
        public bool FixHasVariants => FixVariants is not null && FixVariants.Any();

        /// <summary>
        /// Does selected fix has any updates
        /// </summary>
        public bool SelectedFixHasUpdate => SelectedFix?.HasNewerVersion ?? false;

        private string SelectedFixUrl => _mainModel.GetSelectedFixUrl(SelectedFix);

        public string Requirements => GetRequirementsString();

        public bool SelectedGameRequireAdmin => SelectedGame?.Game is not null && SelectedGame.Game.DoesRequireAdmin();

        /// <summary>
        /// List of games
        /// </summary>
        public ImmutableList<FixFirstCombinedEntity> FilteredGamesList => _mainModel.GetFilteredGamesList(Search);

        /// <summary>
        /// List of fixes for selected game
        /// </summary>
        public ImmutableList<FixEntity>? SelectedGameFixesList => SelectedGame is null ? ImmutableList.Create<FixEntity>() : SelectedGame.FixesList.Fixes.ToImmutableList();

        /// <summary>
        /// List of selected fix's variants
        /// </summary>
        public ImmutableList<string>? FixVariants => SelectedFix?.Variants?.ToImmutableList();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedGameFixesList))]
        [NotifyPropertyChangedFor(nameof(SelectedGameRequireAdmin))]
        [NotifyCanExecuteChangedFor(nameof(OpenGameFolderCommand))]
        [NotifyCanExecuteChangedFor(nameof(ApplyAdminCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenPCGamingWikiCommand))]
        [NotifyCanExecuteChangedFor(nameof(LaunchGameCommand))]
        private FixFirstCombinedEntity? _selectedGame;
        partial void OnSelectedGameChanged(FixFirstCombinedEntity? value)
        {
            if (value?.Game is not null &&
                value.Game.DoesRequireAdmin())
            {
                RequireAdmin();
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Requirements))]
        [NotifyPropertyChangedFor(nameof(SelectedFixHasUpdate))]
        [NotifyPropertyChangedFor(nameof(FixVariants))]
        [NotifyPropertyChangedFor(nameof(FixHasVariants))]
        [NotifyPropertyChangedFor(nameof(SelectedFixUrl))]
        [NotifyCanExecuteChangedFor(nameof(InstallFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(UninstallFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenConfigCommand))]
        [NotifyCanExecuteChangedFor(nameof(UpdateFixCommand))]
        private FixEntity? _selectedFix;

        /// <summary>
        /// Selected fix variant
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(InstallFixCommand))]
        private string? _selectedFixVariant;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdateGamesCommand))]
        private bool _isInProgress;
        partial void OnIsInProgressChanged(bool value) => _lockButtons = value;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ClearSearchCommand))]
        private string _search;
        partial void OnSearchChanged(string value) => FillGamesList();


        #region Relay Commands

        /// <summary>
        /// VM initialization
        /// </summary>
        [RelayCommand]
        private async Task InitializeAsync() => await UpdateAsync(true);


        /// <summary>
        /// Update games list
        /// </summary>
        [RelayCommand(CanExecute = (nameof(UpdateGamesCanExecute)))]
        private async Task UpdateGames() => await UpdateAsync(false);
        private bool UpdateGamesCanExecute() => !_lockButtons;


        /// <summary>
        /// Install selected fix
        /// </summary>
        [RelayCommand(CanExecute = (nameof(InstallFixCanExecute)))]
        private async Task InstallFix()
        {
            if (SelectedGame?.Game is null) throw new NullReferenceException(nameof(SelectedGame));
            if (SelectedFix is null) throw new NullReferenceException(nameof(SelectedFix));

            _lockButtons = true;

            UpdateGamesCommand.NotifyCanExecuteChanged();

            FileTools.Progress.ProgressChanged += Progress_ProgressChanged;

            var result = await _mainModel.InstallFixAsync(SelectedGame.Game, SelectedFix, SelectedFixVariant, true);

            FillGamesList();

            _lockButtons = false;

            UninstallFixCommand.NotifyCanExecuteChanged();
            OpenConfigCommand.NotifyCanExecuteChanged();
            UpdateGamesCommand.NotifyCanExecuteChanged();

            FileTools.Progress.ProgressChanged -= Progress_ProgressChanged;
            ProgressBarValue = 0;
            OnPropertyChanged(nameof(ProgressBarValue));

            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    result.Message,
                    "Error",
                    MessageBoxButton.OK
                    );

                return;
            }

            if (SelectedFix.ConfigFile is not null &&
                _config.OpenConfigAfterInstall)
            {
                MessageBox.Show(
                    result.Message,
                    "Success",
                    MessageBoxButton.OK
                    );
            }
            else
            {
                MessageBox.Show(
                    result.Message,
                    "Success",
                    MessageBoxButton.OK
                    );
            }
        }
        private bool InstallFixCanExecute()
        {
            if (SelectedGame is null ||
                SelectedFix is null ||
                SelectedFix.IsInstalled ||
                !SelectedGame.IsGameInstalled ||
                (FixHasVariants && SelectedFixVariant is null) ||
                _lockButtons)
            {
                return false;
            }

            var result = !_mainModel.DoesFixHaveNotInstalledDependencies(SelectedGame, SelectedFix);

            return result;
        }


        /// <summary>
        /// Uninstall selected fix
        /// </summary>
        [RelayCommand(CanExecute = (nameof(UninstallFixCanExecute)))]
        private void UninstallFix()
        {
            if (SelectedFix is null) throw new NullReferenceException(nameof(SelectedFix));
            if (SelectedGame?.Game is null) throw new NullReferenceException(nameof(SelectedGame));

            IsInProgress = true;

            UpdateGamesCommand.NotifyCanExecuteChanged();

            var result = _mainModel.UninstallFix(SelectedGame.Game, SelectedFix);

            FillGamesList();

            IsInProgress = false;

            InstallFixCommand.NotifyCanExecuteChanged();
            OpenConfigCommand.NotifyCanExecuteChanged();
            UpdateGamesCommand.NotifyCanExecuteChanged();

            MessageBox.Show(
                result.Message,
                result.IsSuccess ? "Success" : "Error",
                MessageBoxButton.OK
                );
        }
        private bool UninstallFixCanExecute()
        {
            if (SelectedFix is null ||
                !SelectedFix.IsInstalled ||
                SelectedGameFixesList is null ||
                (SelectedGame is not null && !SelectedGame.IsGameInstalled) ||
                _lockButtons)
            {
                return false;
            }

            var result = !MainModel.DoesFixHaveInstalledDependentFixes(SelectedGameFixesList, SelectedFix.Guid);

            return result;
        }


        /// <summary>
        /// Update selected fix
        /// </summary>
        [RelayCommand(CanExecute = (nameof(UpdateFixCanExecute)))]
        private async Task UpdateFix()
        {
            if (SelectedFix is null) throw new NullReferenceException(nameof(SelectedFix));
            if (SelectedGame?.Game is null) throw new NullReferenceException(nameof(SelectedGame));

            IsInProgress = true;

            InstallFixCommand.NotifyCanExecuteChanged();
            UninstallFixCommand.NotifyCanExecuteChanged();
            OpenConfigCommand.NotifyCanExecuteChanged();
            UpdateGamesCommand.NotifyCanExecuteChanged();

            var selectedFix = SelectedFix;

            var result = await _mainModel.UpdateFixAsync(SelectedGame.Game, SelectedFix, SelectedFixVariant, true);

            FillGamesList();

            IsInProgress = false;

            InstallFixCommand.NotifyCanExecuteChanged();
            UninstallFixCommand.NotifyCanExecuteChanged();
            OpenConfigCommand.NotifyCanExecuteChanged();
            UpdateGamesCommand.NotifyCanExecuteChanged();

            MessageBox.Show(
                result.Message,
                result.IsSuccess ? "Success" : "Error",
                MessageBoxButton.OK
                );

            if (result.IsSuccess &&
                selectedFix.ConfigFile is not null &&
                _config.OpenConfigAfterInstall)
            {
                OpenConfig();
            }
        }
        public bool UpdateFixCanExecute() => (SelectedGame is not null && SelectedGame.IsGameInstalled) || !_lockButtons;


        /// <summary>
        /// Open selected game install folder
        /// </summary>
        [RelayCommand(CanExecute = (nameof(OpenGameFolderCanExecute)))]
        private void OpenGameFolder()
        {
            if (SelectedGame?.Game is null) throw new NullReferenceException(nameof(SelectedGame));

            Process.Start(new ProcessStartInfo
            {
                FileName = SelectedGame.Game.InstallDir,
                UseShellExecute = true
            });
        }
        private bool OpenGameFolderCanExecute() => SelectedGame is not null && SelectedGame.IsGameInstalled;


        /// <summary>
        /// Clear search bar
        /// </summary>
        [RelayCommand(CanExecute = (nameof(ClearSearchCanExecute)))]
        private void ClearSearch() => Search = string.Empty;
        private bool ClearSearchCanExecute() => !string.IsNullOrEmpty(Search);


        /// <summary>
        /// Open config file for selected fix
        /// </summary>
        [RelayCommand(CanExecute = (nameof(OpenConfigCanExecute)))]
        private void OpenConfig() => OpenConfigXml();
        private bool OpenConfigCanExecute() => SelectedFix?.ConfigFile is not null && SelectedFix.IsInstalled && (SelectedGame is not null && SelectedGame.IsGameInstalled);


        /// <summary>
        /// Apply admin rights for selected game
        /// </summary>
        [RelayCommand(CanExecute = (nameof(ApplyAdminCanExecute)))]
        private void ApplyAdmin()
        {
            if (SelectedGame?.Game is null) throw new NullReferenceException(nameof(SelectedGame));

            SelectedGame.Game.SetRunAsAdmin();

            OnPropertyChanged(nameof(SelectedGameRequireAdmin));
        }
        private bool ApplyAdminCanExecute() => SelectedGameRequireAdmin;


        /// <summary>
        /// Open PCGW page for selected game
        /// </summary>
        [RelayCommand(CanExecute = (nameof(OpenPCGamingWikiCanExecute)))]
        private void OpenPCGamingWiki()
        {
            if (SelectedGame is null) throw new NullReferenceException(nameof(SelectedGame));

            Process.Start(new ProcessStartInfo
            {
                FileName = Consts.PCGamingWikiUrl + SelectedGame.GameId,
                UseShellExecute = true
            });
        }
        private bool OpenPCGamingWikiCanExecute() => SelectedGame is not null;


        /// <summary>
        /// Open PCGW page for selected game
        /// </summary>
        [RelayCommand]
        private void UrlCopyToClipboard()
        {
            if (SelectedFix is null) throw new NullReferenceException(nameof(SelectedGame));

            Clipboard.SetText(SelectedFix.Url);
        }


        /// <summary>
        /// Launch/install game
        /// </summary>
        [RelayCommand(CanExecute = (nameof(LaunchGameCanExecute)))]
        private void LaunchGame()
        {
            if (SelectedGame is null) throw new NullReferenceException(nameof(SelectedGame));

            if (SelectedGame.IsGameInstalled)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"steam://rungameid/{SelectedGame.GameId}",
                    UseShellExecute = true
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"steam://install/{SelectedGame.GameId}",
                    UseShellExecute = true
                });
            }
        }
        private bool LaunchGameCanExecute()
        {
            if (SelectedGame is null)
            {
                return false;
            }

            if (SelectedGame.IsGameInstalled)
            {
                LaunchGameButtonText = "Launch game...";
            }
            else
            {
                LaunchGameButtonText = "Install game...";
            }

            OnPropertyChanged(nameof(LaunchGameButtonText));
            return true;
        }


        /// <summary>
        /// Close app
        /// </summary>
        [RelayCommand]
        private static void CloseApp() => Environment.Exit(0);

        #endregion Relay Commands


        /// <summary>
        /// Update games list
        /// </summary>
        /// <param name="useCache">Use cached list</param>
        private async Task UpdateAsync(bool useCache)
        {
            IsInProgress = true;

            var result = await _mainModel.UpdateGamesListAsync(useCache);

            FillGamesList();

            if (!result.IsSuccess)
            {

                MessageBox.Show(
                    result.Message,
                    "Error",
                    MessageBoxButton.OK
                    );
            }

            IsInProgress = false;
        }

        /// <summary>
        /// Update tab header
        /// </summary>
        private void UpdateHeader()
        {
            MainTabHeader = "Main" + (_mainModel.HasUpdateableGames
                ? $" ({_mainModel.UpdateableGamesCount} {(_mainModel.UpdateableGamesCount < 2
                    ? "update"
                    : "updates")})"
                : string.Empty);

            OnPropertyChanged(nameof(MainTabHeader));
        }

        /// <summary>
        /// Fill games and available games lists based on a search bar
        /// </summary>
        private void FillGamesList()
        {
            var selectedGameId = SelectedGame?.GameId;
            var selectedFixGuid = SelectedFix?.Guid;

            OnPropertyChanged(nameof(FilteredGamesList));

            UpdateHeader();

            if (selectedGameId is not null && FilteredGamesList.Any(x => x.GameId == selectedGameId))
            {
                SelectedGame = FilteredGamesList.First(x => x.GameId == selectedGameId);

                if (selectedFixGuid is not null &&
                    SelectedGameFixesList is not null &&
                    SelectedGameFixesList.Any(x => x.Guid == selectedFixGuid))
                {
                    SelectedFix = SelectedGameFixesList.First(x => x.Guid == selectedFixGuid);
                }
            }
        }

        /// <summary>
        /// Show popup with admin right requirement
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        private void RequireAdmin()
        {
            if (SelectedGame?.Game is null) throw new NullReferenceException(nameof(SelectedGame));

            MessageBox.Show(
                @"This game requires to be run as admin in order to work.

Do you want to set it to always run as admin?",
                "Admin privileges required",
                MessageBoxButton.OK
                );

            OnPropertyChanged(nameof(SelectedGameRequireAdmin));
        }

        /// <summary>
        /// Open config file for selected fix
        /// </summary>
        private void OpenConfigXml()
        {
            if (SelectedFix?.ConfigFile is null) throw new NullReferenceException(nameof(SelectedGame));
            if (SelectedGame?.Game is null) throw new NullReferenceException(nameof(SelectedGame));

            var pathToConfig = Path.Combine(SelectedGame.Game.InstallDir, SelectedFix.ConfigFile);

            var workingDir = SelectedFix.ConfigFile.EndsWith(".exe") ? Path.GetDirectoryName(pathToConfig) : Directory.GetCurrentDirectory();

            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(pathToConfig),
                UseShellExecute = true,
                WorkingDirectory = workingDir
            });
        }

        private void Progress_ProgressChanged(object? sender, float e)
        {
            ProgressBarValue = e;
            OnPropertyChanged(nameof(ProgressBarValue));
        }

        private async void NotifyParameterChanged(string parameterName)
        {
            if (parameterName.Equals(nameof(_config.ShowUninstalledGames)))
            {
                FillGamesList();
            }

            if (parameterName.Equals(nameof(_config.ShowUnsupportedFixes)))
            {
                OnPropertyChanged(nameof(SelectedGameFixesList));
            }

            if (parameterName.Equals(nameof(_config.UseTestRepoBranch)) ||
                parameterName.Equals(nameof(_config.UseLocalRepo)) ||
                parameterName.Equals(nameof(_config.LocalRepoPath)))
            {
                await _locker.WaitAsync();
                await UpdateAsync(false);
                _locker.Release();
            }
        }

        private string GetRequirementsString()
        {
            if (SelectedGameFixesList is null ||
                SelectedFix is null ||
                SelectedGame is null)
            {
                return string.Empty;
            }

            var dependsOn = _mainModel.GetDependenciesForAFix(SelectedGame, SelectedFix);

            string? requires = null;

            if (dependsOn.Any())
            {
                requires = "REQUIRES: ";

                requires += string.Join(", ", dependsOn.Select(x => x.Name));
            }

            string? required = null;

            if (SelectedFix?.Dependencies is not null)
            {
                var dependsBy = MainModel.GetDependentFixes(SelectedGameFixesList, SelectedFix.Guid);

                if (dependsBy.Any())
                {
                    required = "REQUIRED BY: ";

                    required += string.Join(", ", dependsBy.Select(x => x.Name));
                }
            }

            if (requires is not null && required is not null)
            {
                return requires + Environment.NewLine + required;
            }
            else if (requires is not null)
            {
                return requires;
            }
            else if (required is not null)
            {
                return required;
            }

            return string.Empty;
        }
    }
}