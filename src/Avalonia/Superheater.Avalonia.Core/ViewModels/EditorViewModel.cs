using Avalonia.Platform.Storage;
using Common.Config;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Enums;
using Common.Helpers;
using Common.Models;
using Common.Providers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Superheater.Avalonia.Core.Helpers;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class EditorViewModel : ObservableObject
    {
        public EditorViewModel(
            EditorModel editorModel,
            ConfigProvider config,
            FixesProvider fixesProvider
            )
        {
            _editorModel = editorModel ?? ThrowHelper.ArgumentNullException<EditorModel>(nameof(editorModel));
            _config = config?.Config ?? ThrowHelper.ArgumentNullException<ConfigEntity>(nameof(config));
            _fixesProvider = fixesProvider ?? ThrowHelper.ArgumentNullException<FixesProvider>(nameof(fixesProvider));

            _searchBarText = string.Empty;

            _config.NotifyParameterChanged += NotifyParameterChanged;
        }

        private readonly EditorModel _editorModel;
        private readonly ConfigEntity _config;
        private readonly FixesProvider _fixesProvider;
        private readonly SemaphoreSlim _locker = new(1, 1);


        #region Binding Properties

        public ImmutableList<FixesList> FilteredGamesList => _editorModel.GetFilteredGamesList(SearchBarText);

        public ImmutableList<GameEntity> AvailableGamesList => _editorModel.GetAvailableGamesList();

        public ImmutableList<IFixEntity>? SelectedGameFixesList => SelectedGame?.Fixes.ToImmutableList();

        public ImmutableList<IFixEntity> AvailableDependenciesList => _editorModel.GetListOfAvailableDependencies(SelectedGame, SelectedFix);

        public ImmutableList<IFixEntity> SelectedFixDependenciesList => _editorModel.GetDependenciesForAFix(SelectedGame, SelectedFix);

        public string SelectedFixTags
        {
            get => SelectedFix?.Tags is null ? string.Empty : string.Join(';', SelectedFix.Tags);
            private set
            {
                if (SelectedFix is null) ThrowHelper.NullReferenceException(nameof(SelectedFix));

                SelectedFix.Tags = value.Split(';').Select(x => x.Trim()).ToList();
            }
        }


        public string SelectedFixVariants
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.Variants is not null ? string.Join(';', fileFix.Variants) : string.Empty;
            private set
            {
                if (SelectedFix is null) ThrowHelper.NullReferenceException(nameof(SelectedFix));
                if (SelectedFix is not FileFixEntity fileFix) { ThrowHelper.ArgumentException(nameof(SelectedFix)); return; }

                fileFix.Variants = value.Split(';').Select(x => x.Trim()).ToList();
            }
        }

        public string SelectedFixFilesToDelete
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.FilesToDelete is not null ? string.Join(';', fileFix.FilesToDelete) : string.Empty;
            private set
            {
                if (SelectedFix is null) ThrowHelper.NullReferenceException(nameof(SelectedFix));
                if (SelectedFix is not FileFixEntity fileFix) { ThrowHelper.ArgumentException(nameof(SelectedFix)); return; }

                fileFix.FilesToDelete = value.Split(';').Select(x => x.Trim()).ToList();
            }
        }

        public string SelectedFixFilesToBackup
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.FilesToBackup is not null ? string.Join(';', fileFix.FilesToBackup) : string.Empty;
            private set
            {
                if (SelectedFix is null) ThrowHelper.NullReferenceException(nameof(SelectedFix));
                if (SelectedFix is not FileFixEntity fileFix) { ThrowHelper.ArgumentException(nameof(SelectedFix)); return; }

                fileFix.FilesToBackup = value.Split(';').Select(x => x.Trim()).ToList();
            }
        }

        public string SelectedFixUrl
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.Url is not null ? fileFix.Url : string.Empty;
            private set
            {
                if (SelectedFix is null) ThrowHelper.NullReferenceException(nameof(SelectedFix));
                if (SelectedFix is not FileFixEntity fileFix) { ThrowHelper.ArgumentException(nameof(SelectedFix)); return; }

                if (string.IsNullOrEmpty(value))
                {
                    fileFix.Url = null;
                }
                else
                {
                    fileFix.Url = value;
                }
            }
        }

        public string SelectedFixMD5
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.MD5 is not null ? fileFix.MD5 : string.Empty;
            set
            {
                if (SelectedFix is null) ThrowHelper.NullReferenceException(nameof(SelectedFix));
                if (SelectedFix is not FileFixEntity fileFix) { ThrowHelper.ArgumentException(nameof(SelectedFix)); return; }

                if (string.IsNullOrWhiteSpace(value))
                {
                    fileFix.MD5 = null;
                }
                else
                {
                    fileFix.MD5 = value;
                }
            }
        }


        public static bool IsDeveloperMode => Properties.IsDeveloperMode;

        public bool IsEditingAvailable => SelectedFix is not null;

        public bool IsWindowsChecked
        {
            get => SelectedFix?.SupportedOSes.HasFlag(OSEnum.Windows) ?? false;
            private set
            {
                if (SelectedFix is null) ThrowHelper.NullReferenceException(nameof(SelectedFix));

                if (value)
                {
                    SelectedFix.SupportedOSes = SelectedFix.SupportedOSes.AddFlag(OSEnum.Windows);
                }
                else
                {
                    SelectedFix.SupportedOSes = SelectedFix.SupportedOSes.RemoveFlag(OSEnum.Windows);
                }
            }
        }

        public bool IsLinuxChecked
        {
            get => SelectedFix?.SupportedOSes.HasFlag(OSEnum.Linux) ?? false;
            private set
            {
                if (SelectedFix is null) ThrowHelper.NullReferenceException(nameof(SelectedFix));

                if (value)
                {
                    SelectedFix.SupportedOSes = SelectedFix.SupportedOSes.AddFlag(OSEnum.Linux);
                }
                else
                {
                    SelectedFix.SupportedOSes = SelectedFix.SupportedOSes.RemoveFlag(OSEnum.Linux);
                }
            }
        }


        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddNewGameCommand))]
        private GameEntity? _selectedAvailableGame;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedGameFixesList))]
        [NotifyCanExecuteChangedFor(nameof(AddNewFixCommand))]
        private FixesList? _selectedGame;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AvailableDependenciesList))]
        [NotifyPropertyChangedFor(nameof(SelectedFixDependenciesList))]
        [NotifyPropertyChangedFor(nameof(IsEditingAvailable))]
        [NotifyPropertyChangedFor(nameof(SelectedFixVariants))]
        [NotifyPropertyChangedFor(nameof(SelectedFixFilesToDelete))]
        [NotifyPropertyChangedFor(nameof(SelectedFixFilesToBackup))]
        [NotifyPropertyChangedFor(nameof(IsWindowsChecked))]
        [NotifyPropertyChangedFor(nameof(IsLinuxChecked))]
        [NotifyPropertyChangedFor(nameof(SelectedFixUrl))]
        [NotifyPropertyChangedFor(nameof(SelectedFixTags))]
        [NotifyPropertyChangedFor(nameof(SelectedFixMD5))]
        [NotifyCanExecuteChangedFor(nameof(RemoveFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveFixDownCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveFixUpCommand))]
        [NotifyCanExecuteChangedFor(nameof(UploadFixCommand))]
        private IFixEntity? _selectedFix;

        [ObservableProperty]
        private int _selectedFixIndex;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddDependencyCommand))]
        private int _selectedAvailableDependencyIndex;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveDependencyCommand))]
        private int _selectedDependencyIndex;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ClearSearchCommand))]
        private string _searchBarText;
        partial void OnSearchBarTextChanged(string value) => FillGamesList();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdateGamesCommand))]
        private bool _isInProgress;

        #endregion Binding Properties


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
        private async Task UpdateGamesAsync() => await UpdateAsync(false);
        private bool UpdateGamesCanExecute() => IsInProgress is false;


        /// <summary>
        /// Add new fix for a game
        /// </summary>
        [RelayCommand(CanExecute = nameof(AddNewFixCanExecute))]
        private void AddNewFix()
        {
            if (SelectedGame is null) ThrowHelper.NullReferenceException(nameof(SelectedGame));

            var newFix = EditorModel.AddNewFix(SelectedGame);

            OnPropertyChanged(nameof(SelectedGameFixesList));

            SelectedFix = newFix;
        }
        private bool AddNewFixCanExecute() => SelectedGame is not null;


        /// <summary>
        /// Remove fix from a game
        /// </summary>
        [RelayCommand(CanExecute = nameof(RemoveFixCanExecute))]
        private void RemoveFix()
        {
            if (SelectedGame is null) ThrowHelper.NullReferenceException(nameof(SelectedGame));
            if (SelectedFix is null) ThrowHelper.NullReferenceException(nameof(SelectedFix));

            EditorModel.RemoveFix(SelectedGame, SelectedFix);

            OnPropertyChanged(nameof(SelectedGameFixesList));
        }
        private bool RemoveFixCanExecute() => SelectedFix is not null;


        /// <summary>
        /// Clear search bar
        /// </summary>
        [RelayCommand(CanExecute = nameof(ClearSearchCanExecute))]
        private void ClearSearch() => SearchBarText = string.Empty;
        private bool ClearSearchCanExecute() => !string.IsNullOrEmpty(SearchBarText);


        /// <summary>
        /// Save fixes.xml
        /// </summary>
        [RelayCommand]
        private async Task SaveChangesAsync()
        {
            var result = await _editorModel.SaveFixesListAsync();

            new PopupMessageViewModel(
                result.IsSuccess ? "Success" : "Error",
                result.Message,
                PopupMessageType.OkOnly)
                .Show();
        }


        /// <summary>
        /// Open fixes.xml file
        /// </summary>
        [RelayCommand]
        private void OpenXmlFile() =>
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(_config.LocalRepoPath, Consts.FixesFile),
                UseShellExecute = true
            });


        /// <summary>
        /// Add dependency for a fix
        /// </summary>
        [RelayCommand(CanExecute = nameof(AddDependencyCanExecute))]
        private void AddDependency()
        {
            if (SelectedFix is null) ThrowHelper.NullReferenceException(nameof(SelectedFix));

            EditorModel.AddDependencyForFix(SelectedFix, AvailableDependenciesList.ElementAt(SelectedAvailableDependencyIndex));

            OnPropertyChanged(nameof(AvailableDependenciesList));
            OnPropertyChanged(nameof(SelectedFixDependenciesList));
        }
        private bool AddDependencyCanExecute() => SelectedAvailableDependencyIndex > -1;


        /// <summary>
        /// Remove dependence from a fix
        /// </summary>
        [RelayCommand(CanExecute = nameof(RemoveDependencyCanExecute))]
        private void RemoveDependency()
        {
            if (SelectedFix is null) ThrowHelper.NullReferenceException(nameof(SelectedFix));

            EditorModel.RemoveDependencyForFix(SelectedFix, SelectedFixDependenciesList.ElementAt(SelectedDependencyIndex));

            OnPropertyChanged(nameof(AvailableDependenciesList));
            OnPropertyChanged(nameof(SelectedFixDependenciesList));
        }
        private bool RemoveDependencyCanExecute() => SelectedDependencyIndex > -1;


        /// <summary>
        /// Add new game
        /// </summary>
        [RelayCommand(CanExecute = nameof(AddNewGameCanExecute))]
        private void AddNewGame()
        {
            if (SelectedAvailableGame is null) ThrowHelper.NullReferenceException(nameof(SelectedAvailableGame));

            FixesList? newGame = _editorModel.AddNewGame(SelectedAvailableGame);

            FillGamesList();

            SelectedGame = newGame;
            SelectedFix = newGame.Fixes.First();
        }
        private bool AddNewGameCanExecute() => SelectedAvailableGame is not null;


        /// <summary>
        /// Move fix up in the list
        /// </summary>
        [RelayCommand(CanExecute = nameof(MoveFixUpCanExecute))]
        private void MoveFixUp()
        {
            if (SelectedGame is null) ThrowHelper.NullReferenceException(nameof(SelectedGame));

            EditorModel.MoveFixUp(SelectedGame.Fixes, SelectedFixIndex);

            OnPropertyChanged(nameof(SelectedGameFixesList));
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
            if (SelectedGame is null) ThrowHelper.NullReferenceException(nameof(SelectedGame));

            EditorModel.MoveFixDown(SelectedGame.Fixes, SelectedFixIndex);

            OnPropertyChanged(nameof(SelectedGameFixesList));
            MoveFixDownCommand.NotifyCanExecuteChanged();
            MoveFixUpCommand.NotifyCanExecuteChanged();
        }
        private bool MoveFixDownCanExecute() => SelectedFix is not null && SelectedFixIndex < SelectedGameFixesList?.Count - 1;


        /// <summary>
        /// Upload fix to ftp
        /// </summary>
        [RelayCommand(CanExecute = nameof(UploadFixCanExecute))]
        private async Task UploadFixAsync()
        {
            if (SelectedFix is null) ThrowHelper.NullReferenceException(nameof(SelectedFix));
            if (SelectedGame is null) ThrowHelper.NullReferenceException(nameof(SelectedGame));

            var canUpload = await _editorModel.CheckFixBeforeUploadAsync(SelectedFix);

            if (!canUpload.IsSuccess)
            {
                new PopupMessageViewModel(
                    "Error",
                    canUpload.Message,
                    PopupMessageType.OkOnly)
                    .Show();

                return;
            }

            var result = EditorModel.UploadFix(SelectedGame, SelectedFix);

            new PopupMessageViewModel(
                    result.IsSuccess ? "Success" : "Error",
                    result.Message,
                    PopupMessageType.OkOnly)
                .Show();
        }
        private bool UploadFixCanExecute() => SelectedFix is not null;


        /// <summary>
        /// Open fix file picker
        /// </summary>
        [RelayCommand(CanExecute = nameof(OpenFilePickerCanExecute))]
        private async Task OpenFilePickerAsync()
        {
            if (SelectedFix is null) ThrowHelper.NullReferenceException(nameof(SelectedFix));
            if (SelectedFix is not FileFixEntity fileFix) { return; }

            var topLevel = Properties.TopLevel;

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

            if (files.Count == 0)
            {
                return;
            }

            fileFix.Url = files[0].Path.LocalPath.ToString();
            OnPropertyChanged(nameof(SelectedFixUrl));
        }
        private bool OpenFilePickerCanExecute() => SelectedFix is FileFixEntity;

        #endregion Relay Commands


        /// <summary>
        /// Update games list
        /// </summary>
        /// <param name="useCache">Use cached list</param>
        private async Task UpdateAsync(bool useCache)
        {
            await _locker.WaitAsync();
            IsInProgress = true;

            var result = await _editorModel.UpdateListsAsync(useCache);

            FillGamesList();

            if (!result.IsSuccess)
            {
                new PopupMessageViewModel(
                    "Error",
                    result.Message,
                    PopupMessageType.OkOnly
                    ).Show();
            }

            IsInProgress = false;
            _locker.Release();
        }

        /// <summary>
        /// Fill games and available games lists based on a search bar
        /// </summary>
        private void FillGamesList()
        {
            var selectedGameId = SelectedGame?.GameId;
            var selectedFixGuid = SelectedFix?.Guid;

            OnPropertyChanged(nameof(FilteredGamesList));
            OnPropertyChanged(nameof(AvailableGamesList));

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

        private async void NotifyParameterChanged(string parameterName)
        {
            if (parameterName.Equals(nameof(_config.UseTestRepoBranch)) ||
                parameterName.Equals(nameof(_config.UseLocalRepo)) ||
                parameterName.Equals(nameof(_config.LocalRepoPath)))
            {
                await UpdateAsync(false);
            }
        }
    }
}
