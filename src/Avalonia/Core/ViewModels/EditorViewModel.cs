using Avalonia.Platform.Storage;
using Common.Client;
using Common.Client.Config;
using Common.Client.Models;
using Common.Client.Providers;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.TextFix;
using Common.Enums;
using Common.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Superheater.Avalonia.Core.Helpers;
using Superheater.Avalonia.Core.ViewModels.Popups;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class EditorViewModel : ObservableObject, ISearchBarViewModel, IProgressBarViewModel
    {
        [ObservableProperty]
        private string _selectedTagFilter;
        partial void OnSelectedTagFilterChanged(string value) => FillGamesList();

        private readonly EditorModel _editorModel;
        private readonly FixesProvider _fixesProvider;
        private readonly ConfigEntity _config;
        private readonly PopupEditorViewModel _popupEditor;
        private readonly PopupMessageViewModel _popupMessage;
        private readonly PopupStackViewModel _popupStack;
        private readonly ProgressReport _progressReport;
        private readonly SemaphoreSlim _locker = new(1);

        private CancellationTokenSource _cancellationTokenSource;


        #region Binding Properties

        public ImmutableList<FixesList> FilteredGamesList => _editorModel.GetFilteredGamesList(SearchBarText, SelectedTagFilter);

        public ImmutableList<GameEntity> AvailableGamesList => _editorModel.GetAvailableGamesList();

        public ImmutableList<BaseFixEntity> SelectedGameFixesList => SelectedGame is null ? [] : [.. SelectedGame.Fixes.Where(static x => !x.IsHidden)];

        public ImmutableList<BaseFixEntity> AvailableDependenciesList => _editorModel.GetListOfAvailableDependencies(SelectedGame, SelectedFix);

        public ImmutableList<BaseFixEntity> SelectedFixDependenciesList => _editorModel.GetDependenciesForAFix(SelectedGame, SelectedFix);

        public ImmutableList<FileFixEntity> SharedFixesList => _fixesProvider.GetSharedFixes();

        public HashSet<string> TagsComboboxList => [Consts.All, Consts.WindowsOnly, Consts.LinuxOnly, Consts.AllSuppoted];


        public bool IsSteamGameMode => ClientProperties.IsInSteamDeckGameMode;

        public bool IsDeveloperMode => ClientProperties.IsDeveloperMode;

        public bool IsEditingAvailable => SelectedFix is not null;

        public bool IsEmpty => FilteredGamesList.Count == 0;

        public string ShowPopupStackButtonText => SelectedTagFilter;

        public string DisableFixButtonText
        {
            get
            {
                if (SelectedFix is null)
                {
                    return "-";
                }
                else if (SelectedFix.IsDisabled)
                {
                    return "Enable fix";
                }
                else
                {
                    return "Disable fix";
                }
            }
        }


        public string SelectedFixName
        {
            get => SelectedFix?.Name ?? string.Empty;
            set
            {
                SelectedFix.ThrowIfNull();

                SelectedFix.Name = value;
                OnPropertyChanged(nameof(SelectedGameFixesList));
            }
        }

        public int SelectedFixVersion
        {
            get => SelectedFix?.Version ?? 0;
            set
            {
                SelectedFix.ThrowIfNull();

                SelectedFix.Version = value;
                OnPropertyChanged(nameof(SelectedGameFixesList));
            }
        }

        public string SelectedFixTags
        {
            get => SelectedFix?.Tags is null
                ? string.Empty
                : string.Join(';', SelectedFix.Tags);
            set
            {
                SelectedFix.ThrowIfNull();

                SelectedFix.Tags = value.SplitSemicolonSeparatedString();
            }
        }

        public string SelectedFixVariants
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.Variants is not null
                ? string.Join(';', fileFix.Variants)
                : string.Empty;
            set
            {
                SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);

                fileFix.Variants = value.SplitSemicolonSeparatedString();
            }
        }

        public string SelectedFixFilesToDelete
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.FilesToDelete is not null
                ? string.Join(';', fileFix.FilesToDelete)
                : string.Empty;
            set
            {
                SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);

                fileFix.FilesToDelete = value.SplitSemicolonSeparatedString();
            }
        }

        public string SelectedFixFilesToBackup
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.FilesToBackup is not null
                ? string.Join(';', fileFix.FilesToBackup)
                : string.Empty;
            set
            {
                SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);

                fileFix.FilesToBackup = value.SplitSemicolonSeparatedString();
            }
        }

        public string SelectedFixFilesToPatch
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.FilesToPatch is not null
                ? string.Join(';', fileFix.FilesToPatch)
                : string.Empty;
            set
            {
                SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);

                fileFix.FilesToPatch = value.SplitSemicolonSeparatedString();
            }
        }

        public string SelectedFixWineDllsOverrides
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.WineDllOverrides is not null
                ? string.Join(';', fileFix.WineDllOverrides)
                : string.Empty;
            set
            {
                SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);

                fileFix.WineDllOverrides = value.SplitSemicolonSeparatedString();
            }
        }

        public string SelectedFixUrl
        {
            get => SelectedFix is FileFixEntity fileFix && fileFix.Url is not null
                ? fileFix.Url
                : string.Empty;
            set
            {
                SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);

                fileFix.Url = string.IsNullOrWhiteSpace(value)
                    ? null
                    : value;
            }
        }

        public string SelectedFixEntries
        {
            get => SelectedFix is HostsFixEntity hostsFix
                ? string.Join(';', hostsFix.Entries)
                : string.Empty;
            set
            {
                SelectedFix.ThrowIfNotType<HostsFixEntity>(out var hostsFix);

                hostsFix.Entries = value.SplitSemicolonSeparatedString() ?? [];
            }
        }

        public string SelectedFixDescription
        {
            get => SelectedFix?.Description ?? string.Empty;
            set
            {
                SelectedFix.ThrowIfNull();

                SelectedFix.Description = value;
                OnPropertyChanged();
            }
        }

        public BaseFixEntity? SelectedSharedFix
        {
            get => SelectedFix is not FileFixEntity fileFix ? null : SharedFixesList.FirstOrDefault(x => x.Guid == fileFix.SharedFixGuid);
            set
            {
                SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);

                fileFix.SharedFixGuid = value?.Guid;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSharedFixSelected));
                ResetSelectedSharedFixCommand.NotifyCanExecuteChanged();
            }
        }

        public bool IsWindowsChecked
        {
            get => SelectedFix?.SupportedOSes.HasFlag(OSEnum.Windows) ?? false;
            set
            {
                SelectedFix.ThrowIfNull();

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
                SelectedFix.ThrowIfNull();

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
                SelectedGame.ThrowIfNull();
                SelectedFix.ThrowIfNull();

                if (value)
                {
                    _editorModel.ChangeFixType<FileFixEntity>(SelectedGame.Fixes, SelectedFix);

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
                SelectedGame.ThrowIfNull();
                SelectedFix.ThrowIfNull();

                if (value)
                {
                    _editorModel.ChangeFixType<RegistryFixEntity>(SelectedGame.Fixes, SelectedFix);

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
                SelectedGame.ThrowIfNull();
                SelectedFix.ThrowIfNull();

                if (value)
                {
                    _editorModel.ChangeFixType<HostsFixEntity>(SelectedGame.Fixes, SelectedFix);

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
                SelectedGame.ThrowIfNull();
                SelectedFix.ThrowIfNull();

                if (value)
                {
                    _editorModel.ChangeFixType<TextFixEntity>(SelectedGame.Fixes, SelectedFix);

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

        public bool IsSharedFixSelected => SelectedSharedFix is not null;


        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddNewGameCommand))]
        private GameEntity? _selectedAvailableGame;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedGameFixesList))]
        [NotifyCanExecuteChangedFor(nameof(AddNewFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenSteamDBCommand))]
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
        [NotifyPropertyChangedFor(nameof(SelectedFixEntries))]
        [NotifyPropertyChangedFor(nameof(IsRegistryFixType))]
        [NotifyPropertyChangedFor(nameof(IsFileFixType))]
        [NotifyPropertyChangedFor(nameof(IsHostsFixType))]
        [NotifyPropertyChangedFor(nameof(IsTextFixType))]
        [NotifyPropertyChangedFor(nameof(IsStringValueType))]
        [NotifyPropertyChangedFor(nameof(IsDwordValueType))]
        [NotifyPropertyChangedFor(nameof(SelectedFixFilesToPatch))]
        [NotifyPropertyChangedFor(nameof(SelectedFixWineDllsOverrides))]
        [NotifyPropertyChangedFor(nameof(SelectedSharedFix))]
        [NotifyPropertyChangedFor(nameof(IsSharedFixSelected))]
        [NotifyPropertyChangedFor(nameof(SelectedFixDescription))]
        [NotifyPropertyChangedFor(nameof(DisableFixButtonText))]
        [NotifyCanExecuteChangedFor(nameof(OpenFilePickerCommand))]
        [NotifyCanExecuteChangedFor(nameof(DisableFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(UploadFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenTagsEditorCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenFilesToBackupEditorCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenFilesToDeleteEditorCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenFilesToPatchEditorCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenWineDllsOverridesEditorCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenVariantsEditorCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenHostsEditorCommand))]
        [NotifyCanExecuteChangedFor(nameof(ResetSelectedSharedFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(AddFixToDbCommand))]
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

        [ObservableProperty]
        private string _progressBarText = string.Empty;

        [ObservableProperty]
        private float _progressBarValue;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
        [NotifyCanExecuteChangedFor(nameof(UploadFixCommand))]
        private bool _lockButtons;

        #endregion Binding Properties


        public EditorViewModel(
            EditorModel editorModel,
            FixesProvider fixesProvider,
            ConfigProvider config,
            PopupEditorViewModel popupEditor,
            PopupMessageViewModel popupMessage,
            PopupStackViewModel popupStack,
            ProgressReport progressReport
            )
        {
            _editorModel = editorModel;
            _fixesProvider = fixesProvider;
            _config = config.Config;
            _popupEditor = popupEditor;
            _popupMessage = popupMessage;
            _popupStack = popupStack;
            _progressReport = progressReport;

            _searchBarText = string.Empty;

            SelectedTagFilter = TagsComboboxList.First();

            _config.NotifyParameterChanged += NotifyParameterChanged;
        }


        #region Relay Commands

        /// <summary>
        /// VM initialization
        /// Do nothing
        /// </summary>
        [RelayCommand]
        private Task InitializeAsync() => Task.CompletedTask;


        /// <summary>
        /// Update games list
        /// </summary>
        [RelayCommand(CanExecute = nameof(UpdateGamesCanExecute))]
        private Task UpdateGamesAsync() => UpdateAsync();
        private bool UpdateGamesCanExecute() => IsInProgress is false;


        /// <summary>
        /// Add new fix for a game
        /// </summary>
        [RelayCommand(CanExecute = nameof(AddNewFixCanExecute))]
        private void AddNewFix()
        {
            SelectedGame.ThrowIfNull();

            var newFix = _editorModel.AddNewFix(SelectedGame);

            OnPropertyChanged(nameof(SelectedGameFixesList));
            OnPropertyChanged(nameof(FilteredGamesList));

            SelectedFix = newFix;
        }
        private bool AddNewFixCanExecute() => SelectedGame is not null;


        /// <summary>
        /// Add new fix for a game
        /// </summary>
        [RelayCommand(CanExecute = nameof(AddFixToDbCanExecute))]
        private async Task AddFixToDbAsync()
        {
            SelectedGame.ThrowIfNull();
            SelectedFix.ThrowIfNull();

            var result = await _fixesProvider.AddFixToDbAsync(SelectedGame.GameId, SelectedGame.GameName, SelectedFix).ConfigureAwait(true);

            _popupMessage.Show(
                result.IsSuccess ? "Success" : "Error",
                result.Message,
                PopupMessageType.OkOnly
                );
        }
        private bool AddFixToDbCanExecute() => SelectedFix is not null;


        /// <summary>
        /// Remove fix from a game
        /// </summary>
        [RelayCommand(CanExecute = nameof(DisableFixCanExecute))]
        private async Task DisableFixAsync()
        {
            SelectedFix.ThrowIfNull();

            var result = await _editorModel.ChangeFixDisabledState(SelectedFix, !SelectedFix.IsDisabled).ConfigureAwait(true);

            OnPropertyChanged(nameof(DisableFixButtonText));
            OnPropertyChanged(nameof(SelectedGameFixesList));

            _popupMessage.Show(
                result.IsSuccess ? "Success" : "Error",
                result.Message,
                PopupMessageType.OkOnly
                );
        }
        private bool DisableFixCanExecute() => SelectedFix is not null;


        /// <summary>
        /// Clear search bar
        /// </summary>
        [RelayCommand(CanExecute = nameof(ClearSearchCanExecute))]
        private void ClearSearch() => SearchBarText = string.Empty;
        private bool ClearSearchCanExecute() => !string.IsNullOrEmpty(SearchBarText);

        /// <summary>
        /// Add dependency for a fix
        /// </summary>
        [RelayCommand(CanExecute = nameof(AddDependencyCanExecute))]
        private void AddDependency()
        {
            SelectedFix.ThrowIfNull();

            _editorModel.AddDependencyForFix(SelectedFix, AvailableDependenciesList.ElementAt(SelectedAvailableDependencyIndex));

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
            SelectedFix.ThrowIfNull();

            _editorModel.RemoveDependencyForFix(SelectedFix, SelectedFixDependenciesList.ElementAt(SelectedDependencyIndex));

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
            SelectedAvailableGame.ThrowIfNull();

            var newGame = _editorModel.AddNewGame(SelectedAvailableGame);

            FillGamesList();

            SelectedGame = newGame;
            SelectedFix = newGame.Fixes.First();
        }
        private bool AddNewGameCanExecute() => SelectedAvailableGame is not null;


        /// <summary>
        /// Upload fix to ftp
        /// </summary>
        [RelayCommand(CanExecute = nameof(UploadFixCanExecute))]
        private async Task UploadFixAsync()
        {
            SelectedGame.ThrowIfNull();
            SelectedFix.ThrowIfNull();

            _progressReport.Progress.ProgressChanged += ProgressChanged;
            _progressReport.NotifyOperationMessageChanged += OperationMessageChanged;

            LockButtons = true;

            var canUpload = await _editorModel.CheckFixBeforeUploadAsync(SelectedFix).ConfigureAwait(true);

            if (!canUpload.IsSuccess)
            {
                _popupMessage.Show(
                    "Error",
                    canUpload.Message,
                    PopupMessageType.OkOnly
                    );


                LockButtons = false;
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            var result = await _editorModel.UploadFixAsync(SelectedGame, SelectedFix, cancellationToken).ConfigureAwait(true);

            _progressReport.Progress.ProgressChanged -= ProgressChanged;
            _progressReport.NotifyOperationMessageChanged -= OperationMessageChanged;

            ProgressBarValue = 0;
            ProgressBarText = string.Empty;
            LockButtons = false;

            if (result.IsSuccess)
            {
                _popupMessage.Show(
                    "Success",
                    """
                    Fix successfully uploaded.
                    It will be added to the database after developer's review.

                    Thank you.
                    """,
                    PopupMessageType.OkOnly
                    );
            }
            else
            {
                _popupMessage.Show(
                    "Error",
                    result.Message,
                    PopupMessageType.OkOnly
                    );
            }

        }
        private bool UploadFixCanExecute() => SelectedFix is not null;


        /// <summary>
        /// Cancel ongoing task
        /// </summary>
        [RelayCommand(CanExecute = (nameof(CancelCanExecute)))]
        private async Task CancelAsync()
        {
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(true);
            _cancellationTokenSource.Dispose();
        }
        private bool CancelCanExecute() => LockButtons;


        /// <summary>
        /// Open fix file picker
        /// </summary>
        [RelayCommand(CanExecute = nameof(OpenFilePickerCanExecute))]
        private async Task OpenFilePickerAsync()
        {
            SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);

            var topLevel = AvaloniaProperties.TopLevel;

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
            }).ConfigureAwait(true);

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
            SelectedFix.ThrowIfNull();

            var result = await _popupEditor.ShowAndGetResultAsync("Tags", SelectedFix.Tags).ConfigureAwait(true);

            if (result is not null)
            {
                SelectedFix.Tags = result.ToListOfString();
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
            SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);

            var result = await _popupEditor.ShowAndGetResultAsync("Files to delete", fileFix.FilesToDelete).ConfigureAwait(true);

            if (result is not null)
            {
                fileFix.FilesToDelete = result.ToListOfString();
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
            SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);

            var result = await _popupEditor.ShowAndGetResultAsync("Files to backup", fileFix.FilesToBackup).ConfigureAwait(true);

            if (result is not null)
            {
                fileFix.FilesToBackup = result.ToListOfString();
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
            SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);

            var result = await _popupEditor.ShowAndGetResultAsync("Files to patch", fileFix.FilesToPatch).ConfigureAwait(true);

            if (result is not null)
            {
                fileFix.FilesToPatch = result.ToListOfString();
                OnPropertyChanged(nameof(SelectedFixFilesToPatch));
            }
        }
        private bool OpenFilesToPatchEditorCanExecute() => SelectedFix is FileFixEntity;


        /// <summary>
        /// Open files to patch editor
        /// </summary>
        [RelayCommand(CanExecute = nameof(OpenWineDllsOverridesEditorCanExecute))]
        private async Task OpenWineDllsOverridesEditorAsync()
        {
            SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);

            var result = await _popupEditor.ShowAndGetResultAsync("Files to patch", fileFix.WineDllOverrides).ConfigureAwait(true);

            if (result is not null)
            {
                fileFix.WineDllOverrides = result.ToListOfString();
                OnPropertyChanged(nameof(SelectedFixWineDllsOverrides));
            }
        }
        private bool OpenWineDllsOverridesEditorCanExecute() => SelectedFix is FileFixEntity;


        /// <summary>
        /// Open variants editor
        /// </summary>
        [RelayCommand(CanExecute = nameof(OpenVariantsEditorCanExecute))]
        private async Task OpenVariantsEditorAsync()
        {
            SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);

            var result = await _popupEditor.ShowAndGetResultAsync("Fix variants", fileFix.Variants).ConfigureAwait(true);

            if (result is not null)
            {
                fileFix.Variants = result.ToListOfString();
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
            SelectedFix.ThrowIfNotType<HostsFixEntity>(out var hostsFix);

            var result = await _popupEditor.ShowAndGetResultAsync("Hosts entries", hostsFix.Entries).ConfigureAwait(true);

            if (result is not null)
            {
                hostsFix.Entries = result.ToListOfString()!;
                OnPropertyChanged(nameof(SelectedFixEntries));
            }
        }
        private bool OpenHostsEditorCanExecute() => SelectedFix is HostsFixEntity;


        /// <summary>
        /// Open hosts editor
        /// </summary>
        [RelayCommand(CanExecute = nameof(ResetSelectedSharedFixCanExecute))]
        private void ResetSelectedSharedFix()
        {
            SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);

            SelectedSharedFix = null;
            fileFix.SharedFixInstallFolder = null;

            OnPropertyChanged(nameof(SelectedSharedFix));
            OnPropertyChanged(nameof(IsSharedFixSelected));
        }
        private bool ResetSelectedSharedFixCanExecute() => SelectedSharedFix is not null;


        /// <summary>
        /// Open hosts editor
        /// </summary>
        [RelayCommand]
        private async Task ShowFiltersPopup()
        {
            var selectedFilter = await _popupStack.ShowAndGetResultAsync("Filter", TagsComboboxList).ConfigureAwait(true);

            SelectedTagFilter = selectedFilter;

            OnPropertyChanged(nameof(ShowPopupStackButtonText));
        }


        /// <summary>
        /// Open SteamDB page for selected game
        /// </summary>
        [RelayCommand(CanExecute = (nameof(OpenSteamDBCanExecute)))]
        private void OpenSteamDB()
        {
            SelectedGame.ThrowIfNull();

            Process.Start(new ProcessStartInfo
            {
                FileName = @$"https://steamdb.info/app/{SelectedGame.GameId}/config/",
                UseShellExecute = true
            });
        }
        private bool OpenSteamDBCanExecute() => SelectedGame is not null;


        /// <summary>
        /// Add new fix from a fix json
        /// </summary>
        [RelayCommand]
        private async Task AddFixFromFileAsync()
        {
            var topLevel = AvaloniaProperties.TopLevel;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Choose fix file",
                AllowMultiple = false
            }
            ).ConfigureAwait(true);

            if (files.Count == 0)
            {
                return;
            }

            var result = _editorModel.AddFixFromFile(files[0].Path.LocalPath);

            if (result.IsSuccess)
            {
                OnPropertyChanged(nameof(FilteredGamesList));

                var game = FilteredGamesList.FirstOrDefault(x => x.GameId == result.ResultObject.Item1);
                SelectedGame = game;
                OnPropertyChanged(nameof(SelectedGameFixesList));

                var fix = SelectedGameFixesList.FirstOrDefault(x => x.Guid == result.ResultObject.Item2);
                SelectedFix = fix;
            }
        }

        #endregion Relay Commands


        /// <summary>
        /// Update games list
        /// </summary>
        private async Task UpdateAsync()
        {
            await _locker.WaitAsync().ConfigureAwait(true);
            IsInProgress = true;

            var result = await _editorModel.UpdateListsAsync().ConfigureAwait(true);

            FillGamesList();

            if (!result.IsSuccess)
            {
                _popupMessage.Show(
                    "Error",
                    result.Message,
                    PopupMessageType.OkOnly
                    );
            }

            OnPropertyChanged(nameof(IsEmpty));

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
            OnPropertyChanged(nameof(SharedFixesList));

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
            if (parameterName.Equals(nameof(_config.UseLocalApiAndRepo)) ||
                parameterName.Equals(nameof(_config.LocalRepoPath)))
            {
                await UpdateAsync().ConfigureAwait(true);
            }
        }


        private void ProgressChanged(object? sender, float e)
        {
            ProgressBarValue = e;
        }

        private void OperationMessageChanged(string message)
        {
            ProgressBarText = message;
        }
    }
}
