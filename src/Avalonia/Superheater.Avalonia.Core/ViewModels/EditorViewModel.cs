using Avalonia.Platform.Storage;
using Common.Config;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.TextFix;
using Common.Enums;
using Common.Helpers;
using Common.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Superheater.Avalonia.Core.Helpers;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class EditorViewModel : ObservableObject, ISearchBarViewModel, IProgressBarViewModel
    {
        public EditorViewModel(
            EditorModel editorModel,
            ConfigProvider config,
            PopupEditorViewModel popupEditor,
            PopupMessageViewModel popupMessage
            )
        {
            _editorModel = editorModel;
            _config = config.Config;
            _popupEditor = popupEditor;
            _popupMessage = popupMessage;

            _searchBarText = string.Empty;

            _config.NotifyParameterChanged += NotifyParameterChanged;
        }

        private readonly EditorModel _editorModel;
        private readonly ConfigEntity _config;
        private readonly PopupEditorViewModel _popupEditor;
        private readonly PopupMessageViewModel _popupMessage;
        private readonly SemaphoreSlim _locker = new(1);


        #region Binding Properties

        public ImmutableList<FixesList> FilteredGamesList => _editorModel.GetFilteredGamesList(SearchBarText);

        public ImmutableList<GameEntity> AvailableGamesList => _editorModel.GetAvailableGamesList();

        public ImmutableList<BaseFixEntity> SelectedGameFixesList => SelectedGame is null ? [] : [.. SelectedGame.Fixes];

        public ImmutableList<BaseFixEntity> AvailableDependenciesList => _editorModel.GetListOfAvailableDependencies(SelectedGame, SelectedFix);

        public ImmutableList<BaseFixEntity> SelectedFixDependenciesList => _editorModel.GetDependenciesForAFix(SelectedGame, SelectedFix);


        public bool IsDeveloperMode => Properties.IsDeveloperMode;

        public bool IsEditingAvailable => SelectedFix is not null;


        public string SelectedFixName
        {
            get => SelectedFix?.Name ?? string.Empty;
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                SelectedFix.Name = value;
                OnPropertyChanged(nameof(SelectedGameFixesList));
            }
        }

        public int SelectedFixVersion
        {
            get => SelectedFix?.Version ?? 0;
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                SelectedFix.Version = value;
                OnPropertyChanged(nameof(SelectedGameFixesList));
            }
        }

        public string SelectedFixTags
        {
            get => SelectedFix?.Tags is null ? string.Empty : string.Join(';', SelectedFix.Tags);
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                SelectedFix.Tags = [.. value.Split(';').Select(static x => x.Trim())];
            }
        }

        public string SelectedFixVariants
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.Variants is not null ? string.Join(';', fileFix.Variants) : string.Empty;
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                if (SelectedFix is not FileFixEntity fileFix)
                {
                    ThrowHelper.ArgumentException(nameof(SelectedFix));
                    return;
                }

                fileFix.Variants = [.. value.Split(';').Select(static x => x.Trim())];
            }
        }

        public string SelectedFixFilesToDelete
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.FilesToDelete is not null ? string.Join(';', fileFix.FilesToDelete) : string.Empty;
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                if (SelectedFix is not FileFixEntity fileFix)
                {
                    ThrowHelper.ArgumentException(nameof(SelectedFix));
                    return;
                }

                fileFix.FilesToDelete = [.. value.Split(';').Select(static x => x.Trim())];
            }
        }

        public string SelectedFixFilesToBackup
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.FilesToBackup is not null ? string.Join(';', fileFix.FilesToBackup) : string.Empty;
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                if (SelectedFix is not FileFixEntity fileFix)
                {
                    ThrowHelper.ArgumentException(nameof(SelectedFix));
                    return;
                }

                fileFix.FilesToBackup = [.. value.Split(';').Select(static x => x.Trim())];
            }
        }

        public string SelectedFixFilesToPatch
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.FilesToPatch is not null ? string.Join(';', fileFix.FilesToPatch) : string.Empty;
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                if (SelectedFix is not FileFixEntity fileFix)
                {
                    ThrowHelper.ArgumentException(nameof(SelectedFix));
                    return;
                }

                fileFix.FilesToPatch = [.. value.Split(';').Select(static x => x.Trim())];
            }
        }

        public string SelectedFixUrl
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.Url is not null ? fileFix.Url : string.Empty;
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                if (SelectedFix is not FileFixEntity fileFix)
                {
                    ThrowHelper.ArgumentException(nameof(SelectedFix));
                    return;
                }

                fileFix.Url = string.IsNullOrWhiteSpace(value)
                    ? null
                    : value;
            }
        }

        public string SelectedFixMD5
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.MD5 is not null ? fileFix.MD5 : string.Empty;
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                if (SelectedFix is not FileFixEntity fileFix)
                {
                    ThrowHelper.ArgumentException(nameof(SelectedFix));
                    return;
                }

                fileFix.MD5 = string.IsNullOrWhiteSpace(value)
                    ? null
                    : value;
            }
        }

        public string SelectedFixEntries
        {
            get => SelectedFix is HostsFixEntity hostsFix ? string.Join(';', hostsFix.Entries) : string.Empty;
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                if (SelectedFix is not HostsFixEntity hostsFix)
                {
                    ThrowHelper.ArgumentException(nameof(SelectedFix));
                    return;
                }

                hostsFix.Entries = [.. value.Split(';')];
            }
        }

        public bool IsWindowsChecked
        {
            get => SelectedFix?.SupportedOSes.HasFlag(OSEnum.Windows) ?? false;
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                SelectedFix.SupportedOSes = value
                    ? SelectedFix.SupportedOSes.AddFlag(OSEnum.Windows)
                    : SelectedFix.SupportedOSes.RemoveFlag(OSEnum.Windows);
            }
        }

        public bool IsLinuxChecked
        {
            get => SelectedFix?.SupportedOSes.HasFlag(OSEnum.Linux) ?? false;
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                SelectedFix.SupportedOSes = value
                    ? SelectedFix.SupportedOSes.AddFlag(OSEnum.Linux)
                    : SelectedFix.SupportedOSes.RemoveFlag(OSEnum.Linux);
            }
        }

        public bool IsFileFixType
        {
            get => SelectedFix is FileFixEntity;
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                if (SelectedGame is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                if (value)
                {
                    EditorModel.ChangeFixType<FileFixEntity>(SelectedGame.Fixes, SelectedFix);

                    var index = SelectedFixIndex;
                    OnPropertyChanged(nameof(SelectedGameFixesList));
                    SelectedFixIndex = index;
                }
            }
        }

        public bool IsRegistryFixType
        {
            get => SelectedFix is RegistryFixEntity;
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                if (SelectedGame is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                if (value)
                {
                    EditorModel.ChangeFixType<RegistryFixEntity>(SelectedGame.Fixes, SelectedFix);

                    var index = SelectedFixIndex;
                    OnPropertyChanged(nameof(SelectedGameFixesList));
                    SelectedFixIndex = index;
                }
            }
        }

        public bool IsHostsFixType
        {
            get => SelectedFix is HostsFixEntity;
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                if (SelectedGame is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                if (value)
                {
                    EditorModel.ChangeFixType<HostsFixEntity>(SelectedGame.Fixes, SelectedFix);

                    var index = SelectedFixIndex;
                    OnPropertyChanged(nameof(SelectedGameFixesList));
                    SelectedFixIndex = index;
                }
            }
        }

        public bool IsTextFixType
        {
            get => SelectedFix is TextFixEntity;
            set
            {
                if (SelectedFix is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                if (SelectedGame is null)
                {
                    ThrowHelper.NullReferenceException(nameof(SelectedFix));
                }

                if (value)
                {
                    EditorModel.ChangeFixType<TextFixEntity>(SelectedGame.Fixes, SelectedFix);

                    var index = SelectedFixIndex;
                    OnPropertyChanged(nameof(SelectedGameFixesList));
                    SelectedFixIndex = index;
                }
            }
        }

        public bool IsStringValueType
        {
            get
            {
                if (SelectedFix is RegistryFixEntity regFix)
                {
                    return regFix.ValueType is RegistryValueTypeEnum.String;
                }

                return false;
            }
            set
            {
                if (value && SelectedFix is RegistryFixEntity regFix)
                {
                    regFix.ValueType = RegistryValueTypeEnum.String;
                    OnPropertyChanged(nameof(IsDwordValueType));
                    return;
                }

                ThrowHelper.ArgumentException(nameof(SelectedFix));
            }
        }

        public bool IsDwordValueType
        {
            get
            {
                if (SelectedFix is RegistryFixEntity regFix)
                {
                    return regFix.ValueType is RegistryValueTypeEnum.Dword;
                }

                return false;
            }
            set
            {
                if (value && SelectedFix is RegistryFixEntity regFix)
                {
                    regFix.ValueType = RegistryValueTypeEnum.Dword;
                    OnPropertyChanged(nameof(IsStringValueType));
                    return;
                }

                ThrowHelper.ArgumentException(nameof(SelectedFix));
            }
        }

        public float ProgressBarValue { get; set; }

        #region Not Implemented
        public HashSet<string> TagsComboboxList => [];
        public string SelectedTagFilter { get; set; } = string.Empty;
        public bool IsTagsComboboxVisible => false;
        #endregion Not Implemented


        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddNewGameCommand))]
        private GameEntity? _selectedAvailableGame;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedGameFixesList))]
        [NotifyCanExecuteChangedFor(nameof(AddNewFixCommand))]
        private FixesList? _selectedGame;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedFixName))]
        [NotifyPropertyChangedFor(nameof(SelectedFixVersion))]
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
        [NotifyPropertyChangedFor(nameof(SelectedFixEntries))]
        [NotifyPropertyChangedFor(nameof(IsRegistryFixType))]
        [NotifyPropertyChangedFor(nameof(IsFileFixType))]
        [NotifyPropertyChangedFor(nameof(IsHostsFixType))]
        [NotifyPropertyChangedFor(nameof(IsTextFixType))]
        [NotifyPropertyChangedFor(nameof(IsStringValueType))]
        [NotifyPropertyChangedFor(nameof(IsDwordValueType))]
        [NotifyPropertyChangedFor(nameof(SelectedFixFilesToPatch))]
        [NotifyCanExecuteChangedFor(nameof(OpenFilePickerCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveFixDownCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveFixUpCommand))]
        [NotifyCanExecuteChangedFor(nameof(UploadFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenTagsEditorCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenFilesToBackupEditorCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenFilesToDeleteEditorCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenVariantsEditorCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenHostsEditorCommand))]
        private BaseFixEntity? _selectedFix;

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
        private Task InitializeAsync() => UpdateAsync(true);


        /// <summary>
        /// Update games list
        /// </summary>
        [RelayCommand(CanExecute = nameof(UpdateGamesCanExecute))]
        private Task UpdateGamesAsync() => UpdateAsync(false);
        private bool UpdateGamesCanExecute() => IsInProgress is false;


        /// <summary>
        /// Add new fix for a game
        /// </summary>
        [RelayCommand(CanExecute = nameof(AddNewFixCanExecute))]
        private void AddNewFix()
        {
            if (SelectedGame is null)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedGame));
            }

            var newFix = EditorModel.AddNewFix(SelectedGame);

            OnPropertyChanged(nameof(SelectedGameFixesList));
            OnPropertyChanged(nameof(FilteredGamesList));

            SelectedFix = newFix;
        }
        private bool AddNewFixCanExecute() => SelectedGame is not null;


        /// <summary>
        /// Remove fix from a game
        /// </summary>
        [RelayCommand(CanExecute = nameof(RemoveFixCanExecute))]
        private void RemoveFix()
        {
            if (SelectedGame is null)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedGame));
            }

            if (SelectedFix is null)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedFix));
            }

            _editorModel.RemoveFix(SelectedGame, SelectedFix);

            OnPropertyChanged(nameof(SelectedGameFixesList));
            OnPropertyChanged(nameof(FilteredGamesList));
            OnPropertyChanged(nameof(AvailableGamesList));
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

            OnPropertyChanged(nameof(SelectedFixMD5));
            OnPropertyChanged(nameof(SelectedFixUrl));

            _popupMessage.Show(
                result.IsSuccess ? "Success" : "Error",
                result.Message,
                PopupMessageType.OkOnly
                );
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
            if (SelectedFix is null)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedFix));
            }

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
            if (SelectedFix is null)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedFix));
            }

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
            if (SelectedAvailableGame is null)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedAvailableGame));
            }

            var newGame = _editorModel.AddNewGame(SelectedAvailableGame);

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
            if (SelectedGame is null)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedGame));
            }

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
            if (SelectedGame is null)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedGame));
            }

            EditorModel.MoveFixDown(SelectedGame.Fixes, SelectedFixIndex);

            OnPropertyChanged(nameof(SelectedGameFixesList));
            MoveFixDownCommand.NotifyCanExecuteChanged();
            MoveFixUpCommand.NotifyCanExecuteChanged();
        }
        private bool MoveFixDownCanExecute() => SelectedFix is not null && SelectedFixIndex < SelectedGameFixesList.Count - 1;


        /// <summary>
        /// Upload fix to ftp
        /// </summary>
        [RelayCommand(CanExecute = nameof(UploadFixCanExecute))]
        private async Task UploadFixAsync()
        {
            if (SelectedFix is null)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedFix));
            }

            if (SelectedGame is null)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedGame));
            }

            var canUpload = await _editorModel.CheckFixBeforeUploadAsync(SelectedFix);

            if (!canUpload.IsSuccess)
            {
                _popupMessage.Show(
                    "Error",
                    canUpload.Message,
                    PopupMessageType.OkOnly
                    );

                return;
            }

            var result = EditorModel.UploadFix(SelectedGame, SelectedFix);

            _popupMessage.Show(
                    result.IsSuccess ? "Success" : "Error",
                    result.Message,
                    PopupMessageType.OkOnly
                    );
        }
        private bool UploadFixCanExecute() => SelectedFix is not null;


        /// <summary>
        /// Open fix file picker
        /// </summary>
        [RelayCommand(CanExecute = nameof(OpenFilePickerCanExecute))]
        private async Task OpenFilePickerAsync()
        {
            if (SelectedFix is null)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedFix));
            }

            if (SelectedFix is not FileFixEntity fileFix)
            {
                return;
            }

            var topLevel = Properties.TopLevel;

            FilePickerFileType zipType = new("Zip")
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

            fileFix.Url = files[0].Path.LocalPath;
            OnPropertyChanged(nameof(SelectedFixUrl));
        }
        private bool OpenFilePickerCanExecute() => SelectedFix is FileFixEntity;


        /// <summary>
        /// Open tags editor
        /// </summary>
        [RelayCommand(CanExecute = nameof(OpenTagsEditorCanExecute))]
        private async Task OpenTagsEditorAsync()
        {
            if (SelectedFix is null)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedFix));
                return;
            }

            var result = await _popupEditor.ShowAndGetResultAsync("Tags", SelectedFix.Tags);

            if (result is not null)
            {
                SelectedFix.Tags = result;
                OnPropertyChanged(nameof(SelectedFixTags));
            }
        }
        private bool OpenTagsEditorCanExecute() => SelectedFix is not null;


        /// <summary>
        /// Open files to delete editor
        /// </summary>
        [RelayCommand(CanExecute = nameof(OpenFilesToDeleteEditorCanExecute))]
        private async Task OpenFilesToDeleteEditorAsync()
        {
            if (SelectedFix is not FileFixEntity fileFix)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedFix));
                return;
            }

            var result = await _popupEditor.ShowAndGetResultAsync("Files to delete", fileFix.FilesToDelete);

            if (result is not null)
            {
                fileFix.FilesToDelete = result;
                OnPropertyChanged(nameof(SelectedFixFilesToDelete));
            }
        }
        private bool OpenFilesToDeleteEditorCanExecute() => SelectedFix is FileFixEntity;


        /// <summary>
        /// Open files to backup editor
        /// </summary>
        [RelayCommand(CanExecute = nameof(OpenFilesToBackupEditorCanExecute))]
        private async Task OpenFilesToBackupEditorAsync()
        {
            if (SelectedFix is not FileFixEntity fileFix)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedFix));
                return;
            }

            var result = await _popupEditor.ShowAndGetResultAsync("Files to backup", fileFix.FilesToBackup);

            if (result is not null)
            {
                fileFix.FilesToBackup = result;
                OnPropertyChanged(nameof(SelectedFixFilesToBackup));
            }
        }
        private bool OpenFilesToBackupEditorCanExecute() => SelectedFix is FileFixEntity;


        /// <summary>
        /// Open files to patch editor
        /// </summary>
        [RelayCommand(CanExecute = nameof(OpenFilesToPatchEditorCanExecute))]
        private async Task OpenFilesToPatchEditorAsync()
        {
            if (SelectedFix is not FileFixEntity fileFix)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedFix));
                return;
            }

            var result = await _popupEditor.ShowAndGetResultAsync("Files to patch", fileFix.FilesToPatch);

            if (result is not null)
            {
                fileFix.FilesToPatch = result;
                OnPropertyChanged(nameof(SelectedFixFilesToPatch));
            }
        }
        private bool OpenFilesToPatchEditorCanExecute() => SelectedFix is FileFixEntity;


        /// <summary>
        /// Open variants editor
        /// </summary>
        [RelayCommand(CanExecute = nameof(OpenVariantsEditorCanExecute))]
        private async Task OpenVariantsEditorAsync()
        {
            if (SelectedFix is not FileFixEntity fileFix)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedFix));
                return;
            }

            var result = await _popupEditor.ShowAndGetResultAsync("Fix variants", fileFix.Variants);

            if (result is not null)
            {
                fileFix.Variants = result;
                OnPropertyChanged(nameof(SelectedFixVariants));
            }
        }
        private bool OpenVariantsEditorCanExecute() => SelectedFix is FileFixEntity;


        /// <summary>
        /// Open hosts editor
        /// </summary>
        [RelayCommand(CanExecute = nameof(OpenHostsEditorCanExecute))]
        private async Task OpenHostsEditorAsync()
        {
            if (SelectedFix is not HostsFixEntity fileFix)
            {
                ThrowHelper.NullReferenceException(nameof(SelectedFix));
                return;
            }

            var result = await _popupEditor.ShowAndGetResultAsync("Hosts entries", fileFix.Entries);

            if (result is not null)
            {
                fileFix.Entries = result;
                OnPropertyChanged(nameof(SelectedFixEntries));
            }
        }
        private bool OpenHostsEditorCanExecute() => SelectedFix is HostsFixEntity;

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
                _popupMessage.Show(
                    "Error",
                    result.Message,
                    PopupMessageType.OkOnly
                    );
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

            OnPropertyChanged(nameof(FilteredGamesList));
            OnPropertyChanged(nameof(AvailableGamesList));

            if (selectedGameId is not null && FilteredGamesList.Exists(x => x.GameId == selectedGameId))
            {
                SelectedGame = FilteredGamesList.First(x => x.GameId == selectedGameId);

                var selectedFixGuid = SelectedFix?.Guid;

                if (selectedFixGuid is not null &&
                    SelectedGameFixesList.Exists(x => x.Guid == selectedFixGuid))
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
