using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamFDCommon;
using SteamFDCommon.CombinedEntities;
using SteamFDCommon.Config;
using SteamFDCommon.Helpers;
using SteamFDCommon.Models;
using SteamFDTCommon.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SteamFDA.ViewModels
{
    internal partial class MainViewModel : ObservableObject
    {
        private readonly MainModel _mainModel;
        private readonly ConfigEntity _config;

        public ObservableCollection<GameFirstCombinedEntity> FilteredGamesList { get; init; }

        public List<FixEntity>? SelectedGameFixesList => SelectedGame?.Fixes;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdateGamesCommand))]
        private bool _isInProgress;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Requirements))]
        [NotifyCanExecuteChangedFor(nameof(InstallFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(UninstallFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenConfigCommand))]
        private FixEntity? _selectedFix;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedGameFixesList))]
        [NotifyCanExecuteChangedFor(nameof(OpenGameFolderCommand))]
        [NotifyCanExecuteChangedFor(nameof(ApplyAdminCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenPCGamingWikiCommand))]
        private GameFirstCombinedEntity? _selectedGame;
        partial void OnSelectedGameChanged(GameFirstCombinedEntity? value)
        {
            if (value is not null &&
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

        public MainViewModel(MainModel mainModel, ConfigProvider config)
        {
            _mainModel = mainModel ?? throw new NullReferenceException(nameof(mainModel));
            _config = config?.Config ?? throw new NullReferenceException(nameof(config));

            FilteredGamesList = new();

            SetRelayCommands();

            //_ = UpdateAsync(true);
        }

        [RelayCommand]
        async Task InitializeAsync() => await UpdateAsync(true);

        private async Task UpdateAsync(bool useCache)
        {
            IsInProgress = true;

            await _mainModel.UpdateGamesListAsync(useCache);

            FillGamesList();

            IsInProgress = false;
        }

        private void FillGamesList()
        {
            var selectedGame = SelectedGame;
            var selectedFix = SelectedFix;

            FilteredGamesList.Clear();

            var gamesList = _mainModel.GetFilteredGamesList(Search);

            FilteredGamesList.AddRange(gamesList);

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

        private void RequireAdmin()
        {
            if (SelectedGame is null)
            {
                throw new NullReferenceException(nameof(SelectedGame));
            }

            //            var result = MessageBox.Show(
            //                    @"This game requires to be run as admin in order to work.

            //Do you want to set it to always run as admin?",
            //                    "Run as admin required",
            //                    MessageBoxButton.YesNo);

            //            if (result == MessageBoxResult.Yes)
            //            {
            //                SelectedGame.Game.SetRunAsAdmin();

            //                MessageBox.Show("Success");
            //            }
        }

        private void OpenConfig()
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

        #region Relay Commands

        private void SetRelayCommands()
        {
            InstallFixCommand = new RelayCommand(
                execute: async () =>
                {
                    if (SelectedGame is null)
                    {
                        throw new NullReferenceException(nameof(SelectedGame));
                    }
                    if (SelectedFix is null)
                    {
                        throw new NullReferenceException(nameof(SelectedFix));
                    }

                    IsInProgress = true;

                    var selectedFix = SelectedFix;

                    var result = await _mainModel.InstallFix(SelectedGame.Game, SelectedFix);

                    IsInProgress = false;

                    InstallFixCommand.NotifyCanExecuteChanged();
                    UninstallFixCommand.NotifyCanExecuteChanged();
                    OpenConfigCommand.NotifyCanExecuteChanged();

                    //MessageBox.Show(result.ToString());

                    if (selectedFix.ConfigFile is not null &&
                        _config.OpenConfigAfterInstall)
                    {
                        OpenConfig();
                    }
                },
                canExecute: () =>
                {
                    if (SelectedFix is null || SelectedFix.IsInstalled)
                    {
                        return false;
                    }
                    if (SelectedGame is null)
                    {
                        throw new NullReferenceException(nameof(SelectedGame));
                    }

                    var result = !_mainModel.DoesFixHaveUninstalledDependencies(SelectedGame, SelectedFix);

                    return result;
                }
                );

            UninstallFixCommand = new RelayCommand(
                execute: () =>
                {
                    if (SelectedFix is null)
                    {
                        throw new NullReferenceException(nameof(SelectedFix));
                    }
                    if (SelectedGame is null)
                    {
                        throw new NullReferenceException(nameof(SelectedGame));
                    }

                    var result = _mainModel.UninstallFix(SelectedGame.Game, SelectedFix);

                    InstallFixCommand.NotifyCanExecuteChanged();
                    UninstallFixCommand.NotifyCanExecuteChanged();
                    OpenConfigCommand.NotifyCanExecuteChanged();

                    //MessageBox.Show(result.ToString());
                },
                canExecute: () =>
                {
                    if (SelectedFix is null || !SelectedFix.IsInstalled)
                    {
                        return false;
                    }
                    if (SelectedGameFixesList is null)
                    {
                        throw new NullReferenceException(nameof(SelectedGameFixesList));
                    }

                    var result = !_mainModel.DoesFixHaveInstalledDependantFixes(SelectedGameFixesList, SelectedFix.Guid);

                    return result;
                }
                );

            OpenGameFolderCommand = new RelayCommand(
                execute: () =>
                {
                    if (SelectedGame is null)
                    {
                        throw new NullReferenceException(nameof(SelectedGame));
                    }

                    Process.Start(
                        "explorer.exe",
                        SelectedGame.Game.InstallDir
                        );
                },
                canExecute: () => SelectedGame is not null
                );

            UpdateGamesCommand = new AsyncRelayCommand(async () => await UpdateAsync(false), () => IsInProgress == false);

            ClearSearchCommand = new RelayCommand(
                execute: () =>
                {
                    Search = string.Empty;
                },
                canExecute: () => !string.IsNullOrEmpty(Search)
                );

            OpenConfigCommand = new RelayCommand(
                execute: () =>
                {
                    OpenConfig();
                },
                canExecute: () => SelectedFix?.ConfigFile is not null && SelectedFix.IsInstalled
                );

            ApplyAdminCommand = new RelayCommand(
                execute: () =>
                {
                    if (SelectedGame is null)
                    {
                        throw new NullReferenceException(nameof(SelectedGame));
                    }

                    SelectedGame.Game.SetRunAsAdmin();

                    //MessageBox.Show("Success");
                },
                canExecute: () => SelectedGame is not null && SelectedGame.Game.DoesRequireAdmin
                );

            OpenPCGamingWikiCommand = new RelayCommand(
                execute: () =>
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
                },
                canExecute: () => SelectedGame is not null
                );
        }

        public IRelayCommand OpenGameFolderCommand { get; private set; }

        public IRelayCommand ClearSearchCommand { get; private set; }

        public IRelayCommand UninstallFixCommand { get; private set; }

        public IRelayCommand InstallFixCommand { get; private set; }

        public IRelayCommand UpdateGamesCommand { get; private set; }

        public IRelayCommand OpenConfigCommand { get; private set; }

        public IRelayCommand ApplyAdminCommand { get; private set; }

        public IRelayCommand OpenPCGamingWikiCommand { get; private set; }

        #endregion Relay Commands
    }
}