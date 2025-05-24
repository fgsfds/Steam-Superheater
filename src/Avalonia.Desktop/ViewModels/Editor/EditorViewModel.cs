using Avalonia.Controls.Notifications;
using Avalonia.Desktop.Helpers;
using Avalonia.Desktop.ViewModels.Popups;
using Avalonia.Desktop.Windows;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Common;
using Common.Client;
using Common.Client.Models;
using Common.Client.Providers.Interfaces;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.TextFix;
using Common.Enums;
using Common.Helpers;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Avalonia.Desktop.ViewModels.Editor;

internal sealed partial class EditorViewModel : ObservableObject, ISearchBarViewModel, IProgressBarViewModel
{
    private readonly EditorModel _editorModel;
    private readonly MainViewModel _mainViewModel;
    private readonly IFixesProvider _fixesProvider;
    private readonly IConfigProvider _config;
    private readonly PopupMessageViewModel _popupMessage;
    private readonly PopupEditorViewModel _popupEditor;
    private readonly PopupStackViewModel _popupStack;
    private readonly ProgressReport _progressReport;
    private readonly IGamesProvider _gamesProvider;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _locker = new(1);

    private CancellationTokenSource? _cancellationTokenSource;


    #region Binding Properties

    [ObservableProperty]
    private ObservableObject? _fixDataContext;

    public ImmutableList<FixesList> FilteredGamesList => _editorModel.GetFilteredGamesList(SearchBarText, SelectedTagFilter);

    public ImmutableList<GameEntity> AvailableGamesList => _editorModel.AvailableGames;

    public ImmutableList<BaseFixEntity>? SelectedGameFixesList => SelectedGame is null ? null : [.. SelectedGame.Fixes.Where(static x => !x.IsHidden).OrderBy(static x => x.IsDisabled)];

    public ImmutableList<BaseFixEntity>? AvailableDependenciesList => _editorModel.GetListOfAvailableDependencies(SelectedGame, SelectedFix);

    public ImmutableList<BaseFixEntity>? SelectedFixDependenciesList => _editorModel.GetDependenciesForAFix(SelectedGame, SelectedFix);

    public HashSet<string> TagsComboboxList => [Consts.All, Consts.WindowsOnly, Consts.LinuxOnly, Consts.AllSupported];

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
            Guard.IsNotNull(SelectedFix);

            SelectedFix.Name = value;
            OnPropertyChanged(nameof(SelectedGameFixesList));
        }
    }

    public string SelectedFixVersion
    {
        get => SelectedFix?.Version ?? string.Empty;
        set
        {
            Guard.IsNotNull(SelectedFix);

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
            Guard.IsNotNull(SelectedFix);

            SelectedFix.Tags = value.SplitSemicolonSeparatedString();
        }
    }

    public string SelectedFixEntries
    {
        get => SelectedFix is HostsFixEntity hostsFix
            ? string.Join(';', hostsFix.Entries)
            : string.Empty;
        set
        {
            Guard2.IsOfType<HostsFixEntity>(SelectedFix, out var hostsFix);

            hostsFix.Entries = value.SplitSemicolonSeparatedString() ?? [];
        }
    }

    public string SelectedFixDescription
    {
        get => SelectedFix?.Description ?? string.Empty;
        set
        {
            Guard.IsNotNull(SelectedFix);

            SelectedFix.Description = value;
            OnPropertyChanged();
        }
    }

    public string SelectedFixChangelog
    {
        get => SelectedFix?.Changelog ?? string.Empty;
        set
        {
            Guard.IsNotNull(SelectedFix);

            SelectedFix.Changelog = value;
            OnPropertyChanged();
        }
    }

    public bool IsWindowsChecked
    {
        get => SelectedFix?.SupportedOSes.HasFlag(OSEnum.Windows) ?? false;
        set
        {
            Guard.IsNotNull(SelectedFix);

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
            Guard.IsNotNull(SelectedFix);

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
            Guard.IsNotNull(SelectedGame);
            Guard.IsNotNull(SelectedFix);

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
            Guard.IsNotNull(SelectedGame);
            Guard.IsNotNull(SelectedFix);

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
            Guard.IsNotNull(SelectedGame);
            Guard.IsNotNull(SelectedFix);

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
            Guard.IsNotNull(SelectedGame);
            Guard.IsNotNull(SelectedFix);

            if (value)
            {
                _editorModel.ChangeFixType<TextFixEntity>(SelectedGame.Fixes, SelectedFix);

                var index = SelectedFixIndex;
                OnPropertyChanged(nameof(SelectedGameFixesList));
                SelectedFixIndex = index;
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
    [NotifyPropertyChangedFor(nameof(SelectedFixName))]
    [NotifyPropertyChangedFor(nameof(SelectedFixVersion))]
    [NotifyPropertyChangedFor(nameof(AvailableDependenciesList))]
    [NotifyPropertyChangedFor(nameof(SelectedAvailableDependency))]
    [NotifyPropertyChangedFor(nameof(SelectedFixDependenciesList))]
    [NotifyPropertyChangedFor(nameof(SelectedDependency))]
    [NotifyPropertyChangedFor(nameof(IsWindowsChecked))]
    [NotifyPropertyChangedFor(nameof(IsLinuxChecked))]
    [NotifyPropertyChangedFor(nameof(SelectedFixTags))]
    [NotifyPropertyChangedFor(nameof(SelectedFixEntries))]
    [NotifyPropertyChangedFor(nameof(SelectedFixDescription))]
    [NotifyPropertyChangedFor(nameof(SelectedFixChangelog))]
    [NotifyPropertyChangedFor(nameof(DisableFixButtonText))]
    [NotifyPropertyChangedFor(nameof(IsTextFixType))]
    [NotifyPropertyChangedFor(nameof(IsHostsFixType))]
    [NotifyPropertyChangedFor(nameof(IsRegistryFixType))]
    [NotifyPropertyChangedFor(nameof(IsFileFixType))]
    [NotifyCanExecuteChangedFor(nameof(DisableFixCommand))]
    [NotifyCanExecuteChangedFor(nameof(UploadFixCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenTagsEditorCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenHostsEditorCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddFixToDbCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestFixCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddDependencyCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveDependencyCommand))]
    private BaseFixEntity? _selectedFix;
    partial void OnSelectedFixChanged(BaseFixEntity? value)
    {
        if (value is FileFixEntity fileFix)
        {
            FixDataContext = new FileFixViewModel(
                fileFix,
                _fixesProvider,
                _popupEditor
                );
        }
        else if (value is RegistryFixEntity regFix)
        {
            FixDataContext = new RegFixViewModel(regFix);
        }
        else
        {
            FixDataContext = null;
        }
    }

    [ObservableProperty]
    private int _selectedFixIndex;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddDependencyCommand))]
    private BaseFixEntity? _selectedAvailableDependency;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveDependencyCommand))]
    private BaseFixEntity? _selectedDependency;

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

    [ObservableProperty]
    private string _selectedTagFilter;
    partial void OnSelectedTagFilterChanged(string value) => FillGamesList();

    #endregion Binding Properties


    public EditorViewModel(
        EditorModel editorModel,
        MainViewModel mainViewModel,
        IFixesProvider fixesProvider,
        IConfigProvider config,
        PopupMessageViewModel popupMessage,
        PopupEditorViewModel popupEditor,
        PopupStackViewModel popupStack,
        IGamesProvider gamesProvider,
        ProgressReport progressReport,
        ILogger logger
        )
    {
        _editorModel = editorModel;
        _mainViewModel = mainViewModel;
        _fixesProvider = fixesProvider;
        _config = config;
        _popupMessage = popupMessage;
        _popupEditor = popupEditor;
        _popupStack = popupStack;
        _gamesProvider = gamesProvider;
        _progressReport = progressReport;
        _logger = logger;

        _searchBarText = string.Empty;

        SelectedTagFilter = TagsComboboxList.First();

        _config.ParameterChangedEvent += OnParameterChangedEvent;
    }


    #region Relay Commands

    /// <summary>
    /// Update games list
    /// </summary>
    [RelayCommand(CanExecute = nameof(UpdateGamesCanExecute))]
    private Task UpdateGamesAsync()
    {
        return UpdateAsync(true);
    }

    private bool UpdateGamesCanExecute() => IsInProgress is false;


    /// <summary>
    /// Add new fix for a game
    /// </summary>
    [RelayCommand(CanExecute = nameof(AddNewFixCanExecute))]
    private void AddNewFix()
    {
        Guard.IsNotNull(SelectedGame);

        var newFix = _editorModel.AddNewFix(SelectedGame);

        OnPropertyChanged(nameof(SelectedGameFixesList));
        OnPropertyChanged(nameof(FilteredGamesList));

        SelectedFix = newFix;
    }
    private bool AddNewFixCanExecute() => SelectedGame is not null;


    /// <summary>
    /// Add fix to the database
    /// </summary>
    [RelayCommand(CanExecute = nameof(AddFixToDbCanExecute))]
    private async Task AddFixToDbAsync()
    {
        Guard.IsNotNull(SelectedGame);
        Guard.IsNotNull(SelectedFix);

        var result = await _fixesProvider.AddFixToDbAsync(SelectedGame.GameId, SelectedGame.GameName, SelectedFix).ConfigureAwait(true);

        NotificationsHelper.Show(
            result.Message,
            result.IsSuccess ? NotificationType.Success : NotificationType.Error
            );
    }
    private bool AddFixToDbCanExecute() => SelectedFix is not null;


    /// <summary>
    /// Disable fix
    /// </summary>
    [RelayCommand(CanExecute = nameof(DisableFixCanExecute))]
    private async Task DisableFixAsync()
    {
        Guard.IsNotNull(SelectedFix);

        var result = await _editorModel.ChangeFixDisabledState(SelectedFix, !SelectedFix.IsDisabled).ConfigureAwait(true);

        OnPropertyChanged(nameof(DisableFixButtonText));
        OnPropertyChanged(nameof(SelectedGameFixesList));

        NotificationsHelper.Show(
            result.Message,
            result.IsSuccess ? NotificationType.Success : NotificationType.Error
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
        Guard.IsNotNull(SelectedFix);
        Guard.IsNotNull(SelectedAvailableDependency);

        _editorModel.AddDependencyForFix(SelectedFix, SelectedAvailableDependency);

        OnPropertyChanged(nameof(AvailableDependenciesList));
        OnPropertyChanged(nameof(SelectedFixDependenciesList));
    }
    private bool AddDependencyCanExecute() => SelectedAvailableDependency is not null;


    /// <summary>
    /// Remove dependence from a fix
    /// </summary>
    [RelayCommand(CanExecute = nameof(RemoveDependencyCanExecute))]
    private void RemoveDependency()
    {
        Guard.IsNotNull(SelectedFix);
        Guard.IsNotNull(SelectedDependency);

        _editorModel.RemoveDependencyForFix(SelectedFix, SelectedDependency);

        OnPropertyChanged(nameof(AvailableDependenciesList));
        OnPropertyChanged(nameof(SelectedFixDependenciesList));
    }
    private bool RemoveDependencyCanExecute() => SelectedDependency is not null;


    /// <summary>
    /// Add new game
    /// </summary>
    [RelayCommand(CanExecute = nameof(AddNewGameCanExecute))]
    private void AddNewGame()
    {
        Guard.IsNotNull(SelectedAvailableGame);

        var newGame = _editorModel.AddNewGame(SelectedAvailableGame);

        FillGamesList();

        SelectedGame = newGame;
        SelectedFix = newGame.Fixes.First();
    }
    private bool AddNewGameCanExecute() => SelectedAvailableGame is not null;


    /// <summary>
    /// Upload fix to the storage
    /// </summary>
    [RelayCommand(CanExecute = nameof(UploadFixCanExecute))]
    private async Task UploadFixAsync()
    {
        Guard.IsNotNull(SelectedGame);
        Guard.IsNotNull(SelectedFix);

        _progressReport.Progress.ProgressChanged += ProgressChanged;
        _progressReport.NotifyOperationMessageChanged += OperationMessageChanged;

        LockButtons = true;

        var canUpload = await _editorModel.CheckFixBeforeUploadAsync(SelectedFix).ConfigureAwait(true);

        if (!canUpload.IsSuccess)
        {
            NotificationsHelper.Show(
                canUpload.Message,
                NotificationType.Error
                );

            LockButtons = false;
            return;
        }

        _cancellationTokenSource = new();

        var result = await _editorModel.UploadFixAsync(SelectedGame, SelectedFix, _cancellationTokenSource.Token).ConfigureAwait(true);

        _progressReport.Progress.ProgressChanged -= ProgressChanged;
        _progressReport.NotifyOperationMessageChanged -= OperationMessageChanged;

        ProgressBarValue = 0;
        ProgressBarText = string.Empty;
        LockButtons = false;

        if (result.IsSuccess)
        {
            NotificationsHelper.Show(
                $"""
                    Fix successfully uploaded.
                    It will be added to the database after developer's review.

                    Thank you.
                    """,
                NotificationType.Success
                );
        }
        else
        {
            NotificationsHelper.Show(
                result.Message,
                NotificationType.Error
                );
        }

    }
    private bool UploadFixCanExecute() => SelectedFix is not null;


    /// <summary>
    /// Cancel ongoing task
    /// </summary>
    [RelayCommand(CanExecute = nameof(CancelCanExecute))]
    private async Task CancelAsync() => await _cancellationTokenSource!.CancelAsync().ConfigureAwait(true);
    private bool CancelCanExecute() => LockButtons;


    /// <summary>
    /// Open tags editor
    /// </summary>
    [RelayCommand(CanExecute = nameof(OpenTagsEditorCanExecute))]
    private async Task OpenTagsEditorAsync()
    {
        Guard.IsNotNull(SelectedFix);

        var result = await _popupEditor.ShowAndGetResultAsync("Tags", SelectedFix.Tags).ConfigureAwait(true);

        if (result is not null)
        {
            SelectedFix.Tags = result.ToListOfString();
            OnPropertyChanged(nameof(SelectedFixTags));
        }
    }
    private bool OpenTagsEditorCanExecute() => SelectedFix is not null;


    /// <summary>
    /// Open hosts editor
    /// </summary>
    [RelayCommand(CanExecute = nameof(OpenHostsEditorCanExecute))]
    private async Task OpenHostsEditorAsync()
    {
        Guard2.IsOfType<HostsFixEntity>(SelectedFix, out var hostsFix);

        var result = await _popupEditor.ShowAndGetResultAsync("Hosts entries", hostsFix.Entries).ConfigureAwait(true);

        if (result is not null)
        {
            hostsFix.Entries = result.ToListOfString();
            OnPropertyChanged(nameof(SelectedFixEntries));
        }
    }
    private bool OpenHostsEditorCanExecute() => SelectedFix is HostsFixEntity;


    /// <summary>
    /// Show filter popup
    /// </summary>
    [RelayCommand]
    private async Task ShowFiltersPopup()
    {
        var selectedFilter = await _popupStack.ShowAndGetResultAsync("Filter", TagsComboboxList).ConfigureAwait(true);

        SelectedTagFilter = selectedFilter;

        OnPropertyChanged(nameof(ShowPopupStackButtonText));
    }


    /// <summary>
    /// Open selected game install folder
    /// </summary>
    [RelayCommand]
    private async Task OpenGameFolderAsync()
    {
        Guard.IsNotNull(SelectedGame);

        var games = await _gamesProvider.GetGamesListAsync(false).ConfigureAwait(false);
        var game = games.FirstOrDefault(x => x.Id == SelectedGame.GameId);

        if (game is null)
        {
            return;
        }

        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = game.InstallDir,
            UseShellExecute = true
        });
    }


    /// <summary>
    /// Open PCGW page for selected game
    /// </summary>
    [RelayCommand]
    private void OpenPCGamingWiki()
    {
        Guard.IsNotNull(SelectedGame);

        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = Consts.PCGamingWikiUrl + SelectedGame.GameId,
            UseShellExecute = true
        });
    }


    /// <summary>
    /// Open selected game on Steam store
    /// </summary>
    [RelayCommand]
    private void OpenSteamStore()
    {
        Guard.IsNotNull(SelectedGame);

        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = "https://store.steampowered.com/app/" + SelectedGame.GameId,
            UseShellExecute = true
        });
    }


    /// <summary>
    /// Open selected game on Steam client
    /// </summary>
    [RelayCommand]
    private void OpenSteamClient()
    {
        Guard.IsNotNull(SelectedGame);

        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = "steam://nav/games/details/" + SelectedGame.GameId,
            UseShellExecute = true
        });
    }


    /// <summary>
    /// Open SteamDB page for selected game
    /// </summary>
    [RelayCommand]
    private void OpenSteamDB()
    {
        Guard.IsNotNull(SelectedGame);

        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = $"https://steamdb.info/app/{SelectedGame.GameId}/config/",
            UseShellExecute = true
        });
    }


    /// <summary>
    /// Open SteamDB page for selected game
    /// </summary>
    [RelayCommand]
    private Task CopyGameNameAsync()
    {
        Guard.IsNotNull(SelectedGame);

        var clipboard = AvaloniaProperties.TopLevel.Clipboard ?? ThrowHelper.ThrowArgumentNullException<IClipboard>("Error while getting clipboard implementation");
        return clipboard.SetTextAsync(SelectedGame.GameName);
    }


    /// <summary>
    /// Add new fix from a fix.json
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

            var fix = SelectedGameFixesList?.FirstOrDefault(x => x.Guid == result.ResultObject.Item2);
            SelectedFix = fix;
        }
    }


    /// <summary>
    /// Test newly added fix
    /// </summary>
    [RelayCommand(CanExecute = nameof(TestFixCanExecute))]
    private async Task TestFixAsync()
    {
        Guard.IsNotNull(SelectedGame);
        Guard.IsNotNull(SelectedFix);

        _ = _editorModel.CreateFixJson(SelectedGame, SelectedFix, true, out _, out var fixJson);
        await _mainViewModel.TestFixAsync(fixJson).ConfigureAwait(true);

        (AvaloniaProperties.MainWindow).SwitchTab(MainWindowTabsEnum.MainTab);
    }
    private bool TestFixCanExecute() => SelectedFix is not null;


    /// <summary>
    /// Preview resulting json
    /// </summary>
    [RelayCommand]
    private void PreviewJson()
    {
        Guard.IsNotNull(SelectedGame);
        Guard.IsNotNull(SelectedFix);

        _ = _editorModel.CreateFixJson(SelectedGame, SelectedFix, true, out var newFixJson, out _);

        _popupMessage.Show(
            "JSON Preview",
            newFixJson,
            PopupMessageType.OkOnly,
            null,
            HorizontalAlignment.Left
            );
    }


    /// <summary>
    /// Preview resulting json
    /// </summary>
    [RelayCommand]
    private async Task SaveFixesJsonAsync()
    {
        try
        {
            var topLevel = AvaloniaProperties.TopLevel;

            using var file = await topLevel.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Open Text File",
                    DefaultExtension = "json",
                    ShowOverwritePrompt = true,
                    SuggestedFileName = "fixes.json"
                }).ConfigureAwait(true);

            if (file is null)
            {
                return;
            }

            _editorModel.SaveFixesJson(file.Path.AbsolutePath);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while saving json");

            NotificationsHelper.Show(
                "Error while saving json",
                NotificationType.Error
                );
        }
    }

    #endregion Relay Commands


    /// <summary>
    /// Update games list
    /// </summary>
    /// <param name="dropCache">Drop current and create new cache</param>
    private async Task UpdateAsync(bool dropCache)
    {
        await _locker.WaitAsync().ConfigureAwait(true);
        IsInProgress = true;

        var result = await _editorModel.UpdateListsAsync(dropCache).ConfigureAwait(true);

        FillGamesList();

        if (!result.IsSuccess)
        {
            NotificationsHelper.Show(
                result.Message,
                NotificationType.Error
                );
        }

        OnPropertyChanged(nameof(IsEmpty));

        IsInProgress = false;
        _ = _locker.Release();
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
                SelectedGameFixesList is not null &&
                SelectedGameFixesList.Exists(x => x.Guid == selectedFixGuid))
            {
                SelectedFix = SelectedGameFixesList.First(x => x.Guid == selectedFixGuid);
            }
        }
    }

    private void OnParameterChangedEvent(string parameterName)
    {
        if (parameterName.Equals(nameof(_config.UseLocalApiAndRepo)))
        {
            _editorModel.DropFixesList();
            OnPropertyChanged(nameof(FilteredGamesList));
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
