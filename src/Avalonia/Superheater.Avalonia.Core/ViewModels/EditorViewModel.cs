using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Common.Config;
using Common.Helpers;
using Common.Models;
using Common.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Superheater.Avalonia.Core.Helpers;
using Common.Providers;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class EditorViewModel : ObservableObject
    {
        private readonly EditorModel _editorModel;
        private readonly ConfigEntity _config;
        private readonly FixesProvider _fixesProvider;

        public bool IsDeveloperMode => Properties.IsDeveloperMode;

        public ObservableCollection<FixesList> FilteredGamesList { get; init; }

        public ObservableCollection<FixEntity>? SelectedGameFixes => SelectedGame?.Fixes;

        public ObservableCollection<GameEntity> AvailableGamesList { get; set; }

        public string FixVariants
        {
            get => SelectedFix?.Variants is null ? string.Empty : string.Join(";", SelectedFix.Variants);
            set => SelectedFix.Variants = value.Split(";").ToList();
        }

        public int SelectedFixIndex { get; set; } = -1;

        public bool IsEditingAvailable => SelectedFix is not null;

        public List<FixEntity> AvailableDependencies
        {
            get
            {
                if (SelectedGame is null ||
                    SelectedFix is null)
                {
                    return new();
                }

                return _editorModel.GetListOfDependenciesAvailableToAdd(SelectedGame, SelectedFix);
            }
        }

        public List<FixEntity> AddedDependencies
        {
            get
            {
                if (SelectedGame is null ||
                    SelectedFix is null)
                {
                    return new();
                }

                return _editorModel.GetDependenciesForAFix(SelectedGame, SelectedFix);
            }
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdateGamesCommand))]
        private bool _isInProgress;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddGameCommand))]
        public GameEntity _selectedAvailableGame;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedGameFixes))]
        [NotifyCanExecuteChangedFor(nameof(AddNewFixCommand))]
        private FixesList? _selectedGame;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AvailableDependencies))]
        [NotifyPropertyChangedFor(nameof(AddedDependencies))]
        [NotifyPropertyChangedFor(nameof(IsEditingAvailable))]
        [NotifyPropertyChangedFor(nameof(Name))]
        [NotifyPropertyChangedFor(nameof(Version))]
        [NotifyPropertyChangedFor(nameof(Url))]
        [NotifyPropertyChangedFor(nameof(FixVariants))]
        [NotifyCanExecuteChangedFor(nameof(RemoveFixCommand))]
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

        /// <summary>
        /// Name of the fix
        /// </summary>
        public string Name
        {
            get => SelectedFix?.Name ?? string.Empty;
            set
            {
                if (SelectedFix is null)
                {
                    throw new NullReferenceException(nameof(SelectedGameFixes));
                }

                SelectedFix.Name = value ?? string.Empty;
                OnPropertyChanged(nameof(Name));
                UploadFixCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Version of the fix
        /// </summary>
        public int? Version
        {
            get => SelectedFix?.Version;
            set
            {
                if (SelectedFix is null)
                {
                    throw new NullReferenceException(nameof(SelectedGameFixes));
                }

                SelectedFix.Version = value ?? 0;
                OnPropertyChanged(nameof(Version));
                UploadFixCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Link to fix file
        /// </summary>
        public string Url
        {
            get => SelectedFix?.Url ?? string.Empty;
            set
            {
                if (SelectedFix is null)
                {
                    throw new NullReferenceException(nameof(SelectedGameFixes));
                }

                SelectedFix.Url = value ?? string.Empty;
                OnPropertyChanged(nameof(Url));
                UploadFixCommand.NotifyCanExecuteChanged();
            }
        }

        public EditorViewModel(
            EditorModel editorModel,
            ConfigProvider config,
            FixesProvider fixesProvider
            )
        {
            _editorModel = editorModel ?? throw new NullReferenceException(nameof(editorModel));
            _config = config?.Config ?? throw new NullReferenceException(nameof(config));
            _fixesProvider = fixesProvider ?? throw new NullReferenceException(nameof(fixesProvider));

            FilteredGamesList = new();
            AvailableGamesList = new();
        }


        #region Relay Commands

        /// <summary>
        /// VM initialization
        /// </summary>
        [RelayCommand]
        private async Task InitializeAsync() => await UpdateAsync(true);

        /// <summary>
        /// Update games list
        /// </summary>
        [RelayCommand(CanExecute = nameof(UpdateGamesCanExecute))]
        private async Task UpdateGames() => await UpdateAsync(false);
        private bool UpdateGamesCanExecute() => IsInProgress is false;

        /// <summary>
        /// Add new fix for a game
        /// </summary>
        [RelayCommand(CanExecute = nameof(AddNewFixCanExecute))]
        private void AddNewFix()
        {
            if (SelectedGame is null)
            {
                throw new NullReferenceException(nameof(SelectedGame));
            }

            FixEntity newFix = new();

            SelectedGame.Fixes.Add(newFix);

            OnPropertyChanged(nameof(SelectedGameFixes));

            SelectedFix = newFix;
        }
        private bool AddNewFixCanExecute() => SelectedGame is not null;

        /// <summary>
        /// Remove fix from a game
        /// </summary>
        [RelayCommand(CanExecute = nameof(RemoveFixCanExecute))]
        private void RemoveFix()
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
        }
        private bool RemoveFixCanExecute() => SelectedFix is not null;

        /// <summary>
        /// Clear search bar
        /// </summary>
        [RelayCommand(CanExecute = nameof(ClearSearchCanExecute))]
        private void ClearSearch() => Search = string.Empty;
        private bool ClearSearchCanExecute() => !string.IsNullOrEmpty(Search);

        /// <summary>
        /// Save fixes.xml
        /// </summary>
        [RelayCommand]
        private void SaveChanges()
        {
            var result = _editorModel.SaveFixesListAsync();

            new PopupMessageViewModel(
                result.Item1 ? "Success" : "Error",
                result.Item2, 
                PopupMessageType.OkOnly)
                .Show();
        }

        /// <summary>
        /// Open fixes.xml file
        /// </summary>
        [RelayCommand]
        private void OpenXmlFile() => Process.Start("explorer.exe", Path.Combine(_config.LocalRepoPath, Consts.FixesFile));

        /// <summary>
        /// Add dependency for a fix
        /// </summary>
        [RelayCommand(CanExecute = nameof(AddDependencyCanExecute))]
        private void AddDependency()
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
        }
        private bool AddDependencyCanExecute() => SelectedAvailableDependencyIndex > -1;

        /// <summary>
        /// Remove dependence from a fix
        /// </summary>
        [RelayCommand(CanExecute = nameof(RemoveDependencyCanExecute))]
        private void RemoveDependency()
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
        }
        private bool RemoveDependencyCanExecute() => SelectedAddedDependencyIndex > -1;

        /// <summary>
        /// Add new game
        /// </summary>
        [RelayCommand(CanExecute = nameof(AddGameCanExecute))]
        private void AddGame()
        {
            var game = SelectedAvailableGame;

            var newFix = _editorModel.AddNewFix(game);

            FillGamesList();

            SelectedGame = newFix;

            OnPropertyChanged(nameof(AvailableGamesList));
        }
        private bool AddGameCanExecute() => SelectedAvailableGame is not null;

        /// <summary>
        /// Move fix up in the list
        /// </summary>
        [RelayCommand(CanExecute = nameof(MoveFixUpCanExecute))]
        private void MoveFixUp()
        {
            if (SelectedGameFixes is null)
            {
                throw new NullReferenceException(nameof(SelectedGameFixes));
            }

            var newIndex = SelectedFixIndex - 1;

            SelectedGameFixes.Move(SelectedFixIndex, newIndex);
            SelectedFixIndex = newIndex;

            OnPropertyChanged(nameof(SelectedFixIndex));
            MoveFixDownCommand.NotifyCanExecuteChanged();
            MoveFixUpCommand.NotifyCanExecuteChanged();
        }
        private bool MoveFixUpCanExecute() => SelectedFix is not null && SelectedFixIndex > 0;

        /// <summary>
        /// Move fix down in the list
        /// </summary>
        [RelayCommand(CanExecute = nameof(MoveFixDownCanExecute))]
        private void MoveFixDown()
        {
            if (SelectedGameFixes is null)
            {
                throw new NullReferenceException(nameof(SelectedGameFixes));
            }

            var newIndex = SelectedFixIndex + 1;

            SelectedGameFixes.Move(SelectedFixIndex, newIndex);
            SelectedFixIndex = newIndex;

            OnPropertyChanged(nameof(SelectedFixIndex));
            MoveFixDownCommand.NotifyCanExecuteChanged();
            MoveFixUpCommand.NotifyCanExecuteChanged();
        }
        private bool MoveFixDownCanExecute() => SelectedFix is not null && SelectedFixIndex < SelectedGameFixes?.Count - 1;

        /// <summary>
        /// Upload fix to ftp
        /// </summary>
        [RelayCommand(CanExecute = nameof(UploadFixCanExecute))]
        private async Task UploadFix()
        {
            var check = await ChecksBeforeUploadAsync();

            if (!check)
            {
                return;
            }

            var fixesList = SelectedGame ?? throw new NullReferenceException(nameof(SelectedFix));
            var fix = SelectedFix ?? throw new NullReferenceException(nameof(SelectedFix));

            var result = _editorModel.UploadFix(fixesList, fix);

            if (result)
            {
                new PopupMessageViewModel(
                    "Success",
                    @$"Fix successfully uploaded.
It will be added to the database after developer's review.

Thank you.",
                    PopupMessageType.OkOnly)
                .Show();
            }
            else
            {
                new PopupMessageViewModel(
                    "Error",
                    $"Can't upload fix.",
                    PopupMessageType.OkOnly)
                .Show();
            }
        }
        private bool UploadFixCanExecute() => SelectedFix is not null && !string.IsNullOrEmpty(Url) && !string.IsNullOrEmpty(Name) && Version > 0;

        /// <summary>
        /// Open fix file picker
        /// </summary>
        [RelayCommand]
        private async Task OpenFilePicker()
        {
            if (SelectedFix is null) { throw new NullReferenceException(nameof(SelectedFix)); }

            var topLevel = Properties.TopLevel;

            if (topLevel is null)
            {
                throw new NullReferenceException(nameof(topLevel));
            }

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
        }

        /// <summary>
        /// Fill games and available games lists based on a search bar
        /// </summary>
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

        /// <summary>
        /// Check if the file can be uploaded
        /// </summary>
        private async Task<bool> ChecksBeforeUploadAsync()
        {
            var onlineFixes = await _fixesProvider.GetOnlineFixesListAsync();

            foreach (var onlineFix in onlineFixes)
            {
                //if fix already exists in the repo, don't upload it
                if (onlineFix.Fixes.Any(x => x.Guid == SelectedFix.Guid))
                {
                    new PopupMessageViewModel(
                        "Error",
                        @$"Can't upload fix.

This fix already exists in the database.",
                        PopupMessageType.OkOnly)
                    .Show();

                    return false;
                }
            }

            if (string.IsNullOrEmpty(Name) ||
                string.IsNullOrEmpty(Url) ||
                Version < 1)
            {
                new PopupMessageViewModel(
                    "Error",
                    $"Name, Version and Link to file are required to upload a fix.",
                    PopupMessageType.OkOnly)
                .Show();

                return false;
            }

            if (!Url.StartsWith("http"))
            {
                if (!File.Exists(Url))
                {
                    new PopupMessageViewModel(
                        "Error",
                        $"{Url} doesn't exist.",
                        PopupMessageType.OkOnly)
                        .Show();

                    return false;
                }

                else if (new FileInfo(Url).Length > 1e+8)
                {
                    new PopupMessageViewModel(
                        "Error",
                        @$"Can't upload file larger than 100Mb.

Please, upload it to file hosting.",
                        PopupMessageType.OkOnly)
                        .Show();

                    return false;
                }
            }

            return true;
        }
    }
}
