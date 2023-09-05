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
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia;

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

        public ObservableCollection<GameEntity> AvailableGamesList { get; set; }

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
        [NotifyPropertyChangedFor(nameof(Name))]
        [NotifyPropertyChangedFor(nameof(Version))]
        [NotifyPropertyChangedFor(nameof(Url))]
        [NotifyCanExecuteChangedFor(nameof(RemovePatchCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveFixDownCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveFixUpCommand))]
        [NotifyCanExecuteChangedFor(nameof(UploadFixCommand))]
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

        public List<FixEntity> AvailableDependencies => _editorModel.GetListOfDependenciesAvailableToAdd(SelectedGame, SelectedFix);

        public List<FixEntity> AddedDependencies => _editorModel.GetDependenciesForAFix(SelectedGame, SelectedFix);

        public string? Name
        {
            get => SelectedFix?.Name;
            set
            {
                SelectedFix.Name = value;
                OnPropertyChanged(nameof(Name));
                UploadFixCommand.NotifyCanExecuteChanged();
            }
        }

        public int? Version
        {
            get => SelectedFix?.Version;
            set
            {
                SelectedFix.Version = value ?? 0;
                OnPropertyChanged(nameof(Version));
                UploadFixCommand.NotifyCanExecuteChanged();
            }
        }

        public string? Url
        {
            get => SelectedFix?.Url;
            set
            {
                SelectedFix.Url = value;
                OnPropertyChanged(nameof(Url));
                UploadFixCommand.NotifyCanExecuteChanged();
            }
        }

        public EditorViewModel(
            EditorModel editorModel,
            ConfigProvider config
            )
        {
            _editorModel = editorModel ?? throw new NullReferenceException(nameof(editorModel));
            _config = config?.Config ?? throw new NullReferenceException(nameof(config));

            FilteredGamesList = new();
            AvailableGamesList = new();

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
            AvailableGamesList.Clear();

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

            AvailableGamesList.AddRange(_editorModel.GetListOfGamesAvailableToAdd());
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

            UploadFixCommand = new RelayCommand(
                execute: async () =>
                {
                    if (string.IsNullOrEmpty(Name) ||
                        string.IsNullOrEmpty(Url) ||
                        Version < 1)
                    {
                        new PopupMessageViewModel(
                            "Error",
                            $"Name, Version and Link to file are required to upload a fix.",
                            PopupMessageType.OkOnly)
                        .Show();

                        return;
                    }

                    if (!Url.StartsWith("http") &&
                        !File.Exists(Url))
                    {
                        new PopupMessageViewModel(
                            "Error",
                            $"{Url} doesn't exist.",
                            PopupMessageType.OkOnly)
                        .Show();

                        return;
                    }

                    if (new FileInfo(Url).Length > 1e+8)
                    {
                        new PopupMessageViewModel(
                            "Error",
                            $"Can't upload file larger than 100Mb.{Environment.NewLine}{Environment.NewLine}Please, upload it to some file hosting.",
                            PopupMessageType.OkOnly)
                        .Show();

                        return;
                    }

                    var fixesList = SelectedGame ?? throw new NullReferenceException(nameof(SelectedFix));
                    var fix = SelectedFix ?? throw new NullReferenceException(nameof(SelectedFix));

                    var result = await _editorModel.UploadFixAsync(fixesList, fix);

                    if (result)
                    {
                        new PopupMessageViewModel(
                            "Success",
                            $"Fix successfully uploaded.{Environment.NewLine}It will be added to the database after developer's review.{Environment.NewLine}{Environment.NewLine}Thank you.",
                            PopupMessageType.OkOnly)
                        .Show();
                    }
                    else
                    {
                        new PopupMessageViewModel(
                            "Error",
                            $"Can't upload fix.{Environment.NewLine}This fix already exists in the database.",
                            PopupMessageType.OkOnly)
                        .Show();
                    }
                },
                 canExecute: () => SelectedFix is not null && !string.IsNullOrEmpty(Url) && !string.IsNullOrEmpty(Name) && Version > 0
                );

            OpenFilePickerCommand = new RelayCommand(
                execute: async () =>
                {
                    if (SelectedFix is null) { throw new NullReferenceException(nameof(SelectedFix)); }

                    var window = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;

                    var topLevel = TopLevel.GetTopLevel(window);

                    var zipType = new FilePickerFileType("Zip")
                    {
                        Patterns = new[] { "*.zip" },
                        MimeTypes = new[] { "application/zip" }
                    };

                    var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Choose fix file",
                        AllowMultiple = false,
                        FileTypeFilter = new List<FilePickerFileType>() { zipType }
                    });

                    if (!files.Any())
                    {
                        return;
                    }

                    Url = files[0].Path.LocalPath.ToString();
                    OnPropertyChanged(nameof(Url));
                }
                );
        }

        public IRelayCommand ClearSearchCommand { get; private set; }

        public IRelayCommand AddNewPatchCommand { get; private set; }

        public IRelayCommand RemovePatchCommand { get; private set; }

        public IRelayCommand SaveChangesCommand { get; private set; }

        public IRelayCommand OpenXmlFileCommand { get; private set; }

        public IRelayCommand AddDependencyCommand { get; private set; }

        public IRelayCommand RemoveDependencyCommand { get; private set; }

        public IRelayCommand AddGameCommand { get; private set; }

        public IRelayCommand MoveFixUpCommand { get; private set; }

        public IRelayCommand MoveFixDownCommand { get; private set; }

        public IRelayCommand UpdateGamesCommand { get; private set; }

        public IRelayCommand UploadFixCommand { get; private set; }

        public IRelayCommand OpenFilePickerCommand { get; private set; }

        #endregion Relay Commands
    }
}
