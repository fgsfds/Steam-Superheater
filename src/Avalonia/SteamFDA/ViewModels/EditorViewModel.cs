using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamFDCommon;
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

namespace SteamFDA.ViewModels
{
    public partial class EditorViewModel : ObservableObject
    {
        private readonly EditorModel _editorModel;
        private readonly ConfigEntity _config;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdateGamesCommand))]
        private bool _isInProgress;

        public ObservableCollection<FixesList> FilteredGamesList { get; init; }

        public ObservableCollection<FixEntity>? SelectedGameFixes => SelectedGame?.Fixes;

        public int SelectedFixIndex { get; set; } = -1;

        public bool IsEditingAvailable => SelectedFix is not null;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddGameCommand))]
        public GameEntity _selectedAvailableGame;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedGameFixes))]
        [NotifyCanExecuteChangedFor(nameof(AddNewPatchCommand))]
        private FixesList? _selectedGame;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AvailableDependencies))]
        [NotifyPropertyChangedFor(nameof(AddedDependencies))]
        [NotifyPropertyChangedFor(nameof(IsEditingAvailable))]
        [NotifyCanExecuteChangedFor(nameof(RemovePatchCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveFixDownCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveFixUpCommand))]
        private FixEntity? _selectedFix;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddDependencyCommand))]
        private int _selectedAvailableDependencyIndex;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveDependencyCommand))]
        private int _selectedAddedDependencyIndex;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ClearSearchCommand))]
        private string _search;
        partial void OnSearchChanged(string value)
        {
            FillGamesList();
        }

        public List<GameEntity> AvailableGamesList => _editorModel.GetListOfGamesAvailableToAdd();

        public List<FixEntity> AvailableDependencies => _editorModel.GetListOfDependenciesAvailableToAdd(SelectedGame, SelectedFix);

        public List<FixEntity> AddedDependencies => _editorModel.GetDependenciesForAFix(SelectedGame, SelectedFix);

        public EditorViewModel(
            EditorModel editorModel,
            ConfigProvider config
            )
        {
            _editorModel = editorModel ?? throw new NullReferenceException(nameof(editorModel));
            _config = config?.Config ?? throw new NullReferenceException(nameof(config));

            FilteredGamesList = new();

            SetRelayCommands();
        }

        [RelayCommand]
        async Task InitializeAsync() => await UpdateAsync(true);

        private async Task UpdateAsync(bool useCache)
        {
            IsInProgress = true;

            try
            {
                await _editorModel.UpdateFixesListAsync(useCache);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                new PopupMessageViewModel(
                    "Error",
                    "File not found: " + ex.Message,
                    PopupMessageType.OkOnly
                    ).Show();

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

        private void FillGamesList()
        {
            var selectedGame = SelectedGame;
            var selectedFix = SelectedFix;

            FilteredGamesList.Clear();

            var gamesList = _editorModel.GetFilteredFixesList(Search);

            FilteredGamesList.AddRange(gamesList);

            if (selectedGame is not null && FilteredGamesList.Contains(selectedGame))
            {
                SelectedGame = selectedGame;

                if (selectedFix is not null &&
                    SelectedGameFixes is not null &&
                    SelectedGameFixes.Contains(selectedFix))
                {
                    SelectedFix = selectedFix;
                }
            }
        }

        #region Relay Commands

        private void SetRelayCommands()
        {
            UpdateGamesCommand = new AsyncRelayCommand(async () => await UpdateAsync(false), () => IsInProgress == false);

            AddNewPatchCommand = new RelayCommand(
                execute: () =>
                {
                    if (SelectedGame is null)
                    {
                        throw new NullReferenceException(nameof(SelectedGame));
                    }

                    FixEntity newFix = new();

                    SelectedGame.Fixes.Add(newFix);

                    OnPropertyChanged(nameof(SelectedGameFixes));

                    SelectedFix = newFix;
                },
                canExecute: () => SelectedGame is not null
                );

            RemovePatchCommand = new RelayCommand(
                execute: () =>
                {
                    if (SelectedGameFixes is null)
                    {
                        throw new NullReferenceException(nameof(SelectedGameFixes));
                    }
                    if (SelectedFix is null)
                    {
                        throw new NullReferenceException(nameof(SelectedFix));
                    }

                    SelectedGameFixes.Remove(SelectedFix);
                },
                canExecute: () => SelectedFix is not null
                );

            ClearSearchCommand = new RelayCommand(
                execute: () =>
                {
                    Search = string.Empty;
                },
                canExecute: () => !string.IsNullOrEmpty(Search)
                );

            SaveChangesCommand = new RelayCommand(
                execute: () =>
                {
                    var result = _editorModel.SaveFixesListAsync();

                    new PopupMessageViewModel(result.Item1 ? "Success" : "Error", result.Item2, PopupMessageType.OkOnly).Show();
                }
                );

            UploadChangesCommand = new RelayCommand(
                execute: async () =>
                {
                    await _editorModel.UploadFixesToGit();
                }
                );

            OpenXmlFileCommand = new RelayCommand(
                execute: () =>
                {
                    using Process process = new();

                    Process.Start(
                        "explorer.exe",
                        Path.Combine(_config.LocalRepoPath, Consts.FixesFile)
                        );
                }
                );

            AddDependencyCommand = new RelayCommand(
                execute: () =>
                {
                    if (SelectedFix is null)
                    {
                        throw new NullReferenceException(nameof(SelectedFix));
                    }
                    if (SelectedGameFixes is null)
                    {
                        throw new NullReferenceException(nameof(SelectedGameFixes));
                    }

                    _editorModel.AddDependencyForFix(SelectedFix, AvailableDependencies.ElementAt(SelectedAvailableDependencyIndex));
                    OnPropertyChanged(nameof(AvailableDependencies));
                    OnPropertyChanged(nameof(AddedDependencies));
                    OnPropertyChanged(nameof(FilteredGamesList));
                },
                canExecute: () => SelectedAvailableDependencyIndex > -1
                );

            RemoveDependencyCommand = new RelayCommand(
                execute: () =>
                {
                    if (SelectedFix is null)
                    {
                        throw new NullReferenceException(nameof(SelectedFix));
                    }
                    if (SelectedGameFixes is null)
                    {
                        throw new NullReferenceException(nameof(SelectedGameFixes));
                    }

                    _editorModel.RemoveDependencyForFix(SelectedFix, AddedDependencies.ElementAt(SelectedAddedDependencyIndex));
                    OnPropertyChanged(nameof(AvailableDependencies));
                    OnPropertyChanged(nameof(AddedDependencies));
                    OnPropertyChanged(nameof(FilteredGamesList));
                },
                canExecute: () => SelectedAddedDependencyIndex > -1
                );

            AddGameCommand = new RelayCommand(
                execute: () =>
                {
                    var game = SelectedAvailableGame;

                    var newFix = _editorModel.AddNewFix(game);

                    FillGamesList();

                    SelectedGame = newFix;

                    OnPropertyChanged(nameof(AvailableGamesList));
                },
                canExecute: () => SelectedAvailableGame is not null
                );

            MoveFixUpCommand = new RelayCommand(
                execute: () =>
                {
                    var newIndex = SelectedFixIndex - 1;

                    SelectedGameFixes.Move(SelectedFixIndex, newIndex);
                    SelectedFixIndex = newIndex;
                    OnPropertyChanged(nameof(SelectedFixIndex));
                    MoveFixDownCommand.NotifyCanExecuteChanged();
                    MoveFixUpCommand.NotifyCanExecuteChanged();
                },
                canExecute: () => SelectedFix is not null && SelectedFixIndex > 0
                );

            MoveFixDownCommand = new RelayCommand(
                execute: () =>
                {
                    var newIndex = SelectedFixIndex + 1;

                    SelectedGameFixes.Move(SelectedFixIndex, newIndex);
                    SelectedFixIndex = newIndex;
                    OnPropertyChanged(nameof(SelectedFixIndex));
                    MoveFixDownCommand.NotifyCanExecuteChanged();
                    MoveFixUpCommand.NotifyCanExecuteChanged();
                },
                canExecute: () => SelectedFix is not null && SelectedFixIndex < SelectedGameFixes?.Count - 1
                );
        }

        public IRelayCommand ClearSearchCommand { get; private set; }

        public IRelayCommand AddNewPatchCommand { get; private set; }

        public IRelayCommand RemovePatchCommand { get; private set; }

        public IRelayCommand SaveChangesCommand { get; private set; }

        public IRelayCommand OpenXmlFileCommand { get; private set; }

        public IRelayCommand UploadChangesCommand { get; private set; }

        public IRelayCommand AddDependencyCommand { get; private set; }

        public IRelayCommand RemoveDependencyCommand { get; private set; }

        public IRelayCommand AddGameCommand { get; private set; }

        public IRelayCommand MoveFixUpCommand { get; private set; }

        public IRelayCommand MoveFixDownCommand { get; private set; }

        public IRelayCommand UpdateGamesCommand { get; private set; }

        #endregion Relay Commands
    }
}
