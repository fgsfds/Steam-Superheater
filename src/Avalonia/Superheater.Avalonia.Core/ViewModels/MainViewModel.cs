using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamFDCommon;
using SteamFDCommon.CombinedEntities;
using SteamFDCommon.Config;
using SteamFDCommon.Helpers;
using SteamFDCommon.Models;
using SteamFDCommon.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SteamFDA.Helpers;

namespace SteamFDA.ViewModels
{
    internal partial class MainViewModel : ObservableObject
    {
        private readonly MainModel _mainModel;
        private readonly ConfigEntity _config;
        private bool _lockButtons;

        public string MainTabHeader { get; private set; } = "Main";

        public float ProgressBarValue { get; set; }

        /// <summary>
        /// List of games
        /// </summary>
        public ObservableCollection<FixFirstCombinedEntity> FilteredGamesList { get; init; }

        /// <summary>
        /// List of fixes for selected game
        /// </summary>
        public List<FixEntity>? SelectedGameFixesList => SelectedGame?.FixesList.Fixes.ToList();

        /// <summary>
        /// Does selected fix has any updates
        /// </summary>
        public bool SelectedFixHasUpdate => SelectedFix?.HasNewerVersion ?? false;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdateGamesCommand))]
        private bool _isInProgress;
        partial void OnIsInProgressChanged(bool value)
        {
            _lockButtons = value;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Requirements))]
        [NotifyPropertyChangedFor(nameof(SelectedFixHasUpdate))]
        [NotifyCanExecuteChangedFor(nameof(InstallFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(UninstallFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenConfigCommand))]
        [NotifyCanExecuteChangedFor(nameof(UpdateFixCommand))]
        private FixEntity? _selectedFix;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedGameFixesList))]
        [NotifyCanExecuteChangedFor(nameof(OpenGameFolderCommand))]
        [NotifyCanExecuteChangedFor(nameof(ApplyAdminCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenPCGamingWikiCommand))]
        private FixFirstCombinedEntity? _selectedGame;
        partial void OnSelectedGameChanged(FixFirstCombinedEntity? value)
        {
            if (value?.Game is not null &&
                value is not null &&
                value.Game.DoesRequireAdmin)
            {
                RequireAdmin();
            }
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ClearSearchCommand))]
        private string _search;
        partial void OnSearchChanged(string value)
        {
            FillGamesList();
        }

        public string Requirements
        {
            get
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
                    var dependsBy = _mainModel.GetDependantFixes(SelectedGameFixesList, SelectedFix.Guid);

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

        public MainViewModel(
            MainModel mainModel,
            ConfigProvider config
            )
        {
            _mainModel = mainModel ?? throw new NullReferenceException(nameof(mainModel));
            _config = config?.Config ?? throw new NullReferenceException(nameof(config));

            FilteredGamesList = new();
        }


        #region Relay Commands

        /// <summary>
        /// VM initialization
        /// </summary>
        [RelayCommand]
        private async Task InitializeAsync() => await UpdateAsync(true);

        /// <summary>
        /// Install selected fix
        /// </summary>
        [RelayCommand(CanExecute = (nameof(InstallFixCanExecute)))]
        private async Task InstallFix()
        {
            if (SelectedGame is null)
            {
                throw new NullReferenceException(nameof(SelectedGame));
            }
            if (SelectedFix is null)
            {
                throw new NullReferenceException(nameof(SelectedFix));
            }

            _lockButtons = true;

            UpdateGamesCommand.NotifyCanExecuteChanged();

            var selectedFix = SelectedFix;

            ZipTools.Progress.ProgressChanged += Progress_ProgressChanged;

            var result = await _mainModel.InstallFix(SelectedGame.Game, SelectedFix);

            FillGamesList();

            _lockButtons = false;

            UninstallFixCommand.NotifyCanExecuteChanged();
            OpenConfigCommand.NotifyCanExecuteChanged();
            UpdateGamesCommand.NotifyCanExecuteChanged();

            new PopupMessageViewModel("Result", result, PopupMessageType.OkOnly).Show();

            if (selectedFix.ConfigFile is not null &&
                _config.OpenConfigAfterInstall)
            {
                OpenConfigXml();
            }

            ZipTools.Progress.ProgressChanged -= Progress_ProgressChanged;
            ProgressBarValue = 0;
            OnPropertyChanged(nameof(ProgressBarValue));
        }
        private bool InstallFixCanExecute()
        {
            if (SelectedFix is null || SelectedFix.IsInstalled || (SelectedGame is not null && !SelectedGame.IsGameInstalled) || _lockButtons)
            {
                return false;
            }

            var result = !_mainModel.DoesFixHaveUninstalledDependencies(SelectedGame, SelectedFix);

            return result;
        }

        /// <summary>
        /// Uninstall selected fix
        /// </summary>
        [RelayCommand(CanExecute = (nameof(UninstallFixCanExecute)))]
        private void UninstallFix()
        {
            if (SelectedFix is null)
            {
                throw new NullReferenceException(nameof(SelectedFix));
            }

            IsInProgress = true;

            UpdateGamesCommand.NotifyCanExecuteChanged();

            var result = _mainModel.UninstallFix(SelectedGame.Game, SelectedFix);

            FillGamesList();

            IsInProgress = false;

            InstallFixCommand.NotifyCanExecuteChanged();
            OpenConfigCommand.NotifyCanExecuteChanged();
            UpdateGamesCommand.NotifyCanExecuteChanged();

            new PopupMessageViewModel("Result", result, PopupMessageType.OkOnly).Show();
        }
        private bool UninstallFixCanExecute()
        {
            if (SelectedFix is null || !SelectedFix.IsInstalled || SelectedGameFixesList is null || (SelectedGame is not null && !SelectedGame.IsGameInstalled) || _lockButtons)
            {
                return false;
            }

            var result = !_mainModel.DoesFixHaveInstalledDependantFixes(SelectedGameFixesList, SelectedFix.Guid);

            return result;
        }

        /// <summary>
        /// Update selected fix
        /// </summary>
        [RelayCommand(CanExecute = (nameof(UpdateFixCanExecute)))]
        private async Task UpdateFix()
        {
            if (SelectedFix is null)
            {
                throw new NullReferenceException(nameof(SelectedFix));
            }
            if (SelectedGame is null)
            {
                throw new NullReferenceException(nameof(SelectedGame));
            }

            IsInProgress = true;

            InstallFixCommand.NotifyCanExecuteChanged();
            UninstallFixCommand.NotifyCanExecuteChanged();
            OpenConfigCommand.NotifyCanExecuteChanged();
            UpdateGamesCommand.NotifyCanExecuteChanged();

            var selectedFix = SelectedFix;

            var result = await _mainModel.UpdateFix(SelectedGame.Game, SelectedFix);

            FillGamesList();

            IsInProgress = false;

            InstallFixCommand.NotifyCanExecuteChanged();
            UninstallFixCommand.NotifyCanExecuteChanged();
            OpenConfigCommand.NotifyCanExecuteChanged();
            UpdateGamesCommand.NotifyCanExecuteChanged();

            new PopupMessageViewModel("Result", result, PopupMessageType.OkOnly).Show();

            if (selectedFix.ConfigFile is not null &&
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
            if (SelectedGame is null)
            {
                throw new NullReferenceException(nameof(SelectedGame));
            }

            Process.Start(
                "explorer.exe",
                SelectedGame.Game.InstallDir
                );
        }
        private bool OpenGameFolderCanExecute() => SelectedGame is not null && SelectedGame.IsGameInstalled;

        /// <summary>
        /// Update games list
        /// </summary>
        [RelayCommand(CanExecute = (nameof(UpdateGamesCanExecute)))]
        private async Task UpdateGames() => await UpdateAsync(false);
        private bool UpdateGamesCanExecute() => !_lockButtons;

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
            if (SelectedGame is null)
            {
                throw new NullReferenceException(nameof(SelectedGame));
            }

            SelectedGame.Game.SetRunAsAdmin();
        }
        private bool ApplyAdminCanExecute() => SelectedGame?.Game is not null && SelectedGame.Game.DoesRequireAdmin;

        /// <summary>
        /// Open PCGW page for selected game
        /// </summary>
        [RelayCommand(CanExecute = (nameof(OpenPCGamingWikiCanExecute)))]
        private void OpenPCGamingWiki()
        {
            if (SelectedGame is null)
            {
                throw new NullReferenceException(nameof(SelectedGame));
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = Consts.PCGamingWikiUrl + SelectedGame.Game.Id,
                UseShellExecute = true
            });
        }
        private bool OpenPCGamingWikiCanExecute() => SelectedGame is not null;

        /// <summary>
        /// Open PCGW page for selected game
        /// </summary>
        [RelayCommand]
        private async Task UrlCopyToClipboardAsync()
        {
            if (SelectedFix is null)
            {
                throw new NullReferenceException(nameof(SelectedGame));
            }

            await FdaProperties.TopLevel.Clipboard.SetTextAsync(SelectedFix.Url);
        }

        #endregion Relay Commands


        /// <summary>
        /// Update games list
        /// </summary>
        /// <param name="useCache">Use cached list</param>
        private async Task UpdateAsync(bool useCache)
        {
            IsInProgress = true;

            try
            {
                await _mainModel.UpdateGamesListAsync(useCache);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                new PopupMessageViewModel(
                    "Error",
                    "File not found: " + ex.Message,
                    PopupMessageType.OkOnly
                    ).Show();

                IsInProgress = false;

                return;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                new PopupMessageViewModel(
                    "Error",
                    "Can't connect to GitHub repository",
                    PopupMessageType.OkOnly
                    ).Show();

                return;
            }
            finally
            {
                IsInProgress = false;
            }

            FillGamesList();

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
            var selectedGame = SelectedGame;
            var selectedFix = SelectedFix;

            FilteredGamesList.Clear();

            var gamesList = _mainModel.GetFilteredGamesList(Search);

            FilteredGamesList.AddRange(gamesList);

            UpdateHeader();

            if (selectedGame is not null && FilteredGamesList.Contains(selectedGame))
            {
                SelectedGame = selectedGame;

                if (selectedFix is not null &&
                    SelectedGameFixesList is not null &&
                    SelectedGameFixesList.Contains(selectedFix))
                {
                    SelectedFix = selectedFix;
                }
            }
        }

        /// <summary>
        /// Show popup with admin right requirement
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        private void RequireAdmin()
        {
            if (SelectedGame is null)
            {
                throw new NullReferenceException(nameof(SelectedGame));
            }

            new PopupMessageViewModel(
                "Admin privileges required",
                @"This game requires to be run as admin in order to work.

Do you want to set it to always run as admin?",
                PopupMessageType.OkCancel,
                SelectedGame.Game.SetRunAsAdmin
                ).Show();
        }

        /// <summary>
        /// Open config file for selected fix
        /// </summary>
        private void OpenConfigXml()
        {
            if (SelectedFix?.ConfigFile is null)
            {
                throw new NullReferenceException(nameof(SelectedGame));
            }
            if (SelectedGame is null)
            {
                throw new NullReferenceException(nameof(SelectedGame));
            }

            using Process process = new();

            var pathToConfig = Path.Combine(SelectedGame.Game.InstallDir, SelectedFix.ConfigFile);

            if (SelectedFix.ConfigFile.EndsWith(".exe"))
            {
                var dir = Path.GetDirectoryName(pathToConfig) ?? throw new NullReferenceException("dir");

                Directory.SetCurrentDirectory(dir);

                process.StartInfo.FileName = pathToConfig;
            }
            else
            {
                process.StartInfo.FileName = "explorer.exe";
                process.StartInfo.Arguments = Path.Combine(pathToConfig);
            }

            process.Start();
        }

        private void Progress_ProgressChanged(object? sender, float e)
        {
            ProgressBarValue = e;
            OnPropertyChanged(nameof(ProgressBarValue));
        }
    }
}