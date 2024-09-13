using Api.Common.Interface;
using Avalonia.Controls.Notifications;
using Avalonia.Desktop.Helpers;
using Avalonia.Desktop.ViewModels.Popups;
using Avalonia.Input.Platform;
using Common;
using Common.Client;
using Common.Client.FixTools;
using Common.Client.Models;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.TextFix;
using Common.Helpers;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Avalonia.Desktop.ViewModels;

internal sealed partial class MainViewModel : ObservableObject, ISearchBarViewModel, IProgressBarViewModel
{
    private readonly MainModel _mainModel;
    private readonly ApiInterface _apiInterface;
    private readonly FixManager _fixManager;
    private readonly IConfigProvider _config;
    private readonly PopupMessageViewModel _popupMessage;
    private readonly PopupEditorViewModel _popupEditor;
    private readonly PopupStackViewModel _popupStack;
    private readonly ProgressReport _progressReport;
    private readonly SemaphoreSlim _locker = new(1);

    private CancellationTokenSource? _cancellationTokenSource;
    private FixesList? _additionalFix;


    #region Binding Properties

    [ObservableProperty]
    private ImmutableList<FixesList>? _filteredGamesList;

    [ObservableProperty]
    private ImmutableList<BaseFixEntity>? _selectedGameFixesList;

    public ImmutableList<string>? SelectedFixTags => SelectedFix?.Tags is null ? null : [.. SelectedFix.Tags.Where(x => !_config.HiddenTags.Contains(x))];

    public HashSet<string> TagsComboboxList => _mainModel.GetListOfTags();

    public ImmutableList<string>? SelectedFixVariants => SelectedFix is FileFixEntity fileFix && fileFix.Variants is not null ? [.. fileFix.Variants] : null;

    public bool IsAdminMessageVisible => SelectedFix is HostsFixEntity && !ClientProperties.IsAdmin;

    public string ShowVariantsPopupButtonText => SelectedFixVariant is null ? "Select variant..." : SelectedFixVariant;

    public string SelectedFixUrl => _mainModel.GetFileFixUrl(SelectedFix) ?? string.Empty;

    public string ShowPopupStackButtonText => SelectedTagFilter;

    public string LaunchGameButtonText => SelectedGame is null || !SelectedGame.IsGameInstalled ? "Install game..." : "Launch game...";

    public string MainTabHeader =>
        "Main" + (_mainModel.HasUpdateableGames
        ? $" ({_mainModel.UpdateableGamesCount} {(_mainModel.UpdateableGamesCount < 2
            ? "update"
            : "updates")})"
        : string.Empty);

    public string InstallButtonText
    {
        get
        {
            if (SelectedFix is HostsFixEntity &&
                !ClientProperties.IsAdmin)
            {
                return "Restart as admin...";
            }

            if (SelectedFix is not FileFixEntity fileFix)
            {
                return "Install";
            }

            if (fileFix.Url is null)
            {
                return "Install";
            }

            if (SelectedFixVariants is not null && SelectedFixVariant is null)
            {
                return "<- Select fix variant";
            }

            var pathToArchive = _config.UseLocalApiAndRepo
            ? Path.Combine(_config.LocalRepoPath, "fixes", Path.GetFileName(fileFix.Url))
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(fileFix.Url));

            if (File.Exists(pathToArchive))
            {
                return "Install";
            }

            if (fileFix.Url is null ||
                fileFix.FileSize is null)
            {
                return $"Download and install";
            }

            var size = fileFix.FileSize;

            if (fileFix.SharedFix?.FileSize is not null)
            {
                size += fileFix.SharedFix.FileSize;
            }

            return $"Download ({size.ToSizeString()}) and install";
        }
    }

    public string? SelectedFixDescription
    {
        get
        {
            if (SelectedFix is null)
            {
                return string.Empty;
            }

            if (SelectedFix is FileFixEntity fileFix &&
                fileFix.SharedFix is not null)
            {
                return fileFix.SharedFix.Description + Environment.NewLine + Environment.NewLine + fileFix.Description;
            }

            return SelectedFix.Description;
        }
    }

    public string? SelectedFixChangelog
    {
        get
        {
            if (SelectedFix is null)
            {
                return string.Empty;
            }

            return SelectedFix.Changelog;
        }
    }

    public string? SelectedFixNumberOfInstalls
    {
        get
        {
            if (SelectedFix?.Installs is null)
            {
                return null;
            }

            if (SelectedFix.Installs == 1)
            {
                return "1 install";
            }

            return $"{SelectedFix.Installs} installs";
        }
    }

    public string SelectedFixScore
    {
        get
        {
            if (SelectedFix?.Score is null)
            {
                return "-";
            }

            return SelectedFix.Score.ToString()!;
        }
    }

    public bool IsSelectedFixUpvoted
    {
        get
        {
            if (SelectedFix is null)
            {
                return false;
            }

            var hasUpvote = _config.Upvotes.TryGetValue(SelectedFix.Guid, out var isUpvote);

            if (hasUpvote && isUpvote)
            {
                return true;
            }

            return false;
        }
    }

    public bool IsSelectedFixDownvoted
    {
        get
        {
            if (SelectedFix is null)
            {
                return false;
            }

            var hasUpvote = _config.Upvotes.TryGetValue(SelectedFix.Guid, out var isUpvote);

            if (hasUpvote && !isUpvote)
            {
                return true;
            }

            return false;
        }
    }

    public bool IsStatsVisible
    {
        get
        {
            if (SelectedFix is null ||
                SelectedFix.IsTestFix)
            {
                return false;
            }

            if (SelectedGame is null)
            {
                return false;
            }

            return true;
        }
    }

    [ObservableProperty]
    private List<string>? _selectedFixRequirements;

    [ObservableProperty]
    private List<string>? _selectedFixDependencies;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateGamesCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallFixCommand))]
    [NotifyCanExecuteChangedFor(nameof(UpdateFixCommand))]
    [NotifyCanExecuteChangedFor(nameof(UninstallFixCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenConfigCommand))]
    [NotifyCanExecuteChangedFor(nameof(LaunchGameCommand))]
    private bool _lockButtons;

    [ObservableProperty]
    private string _progressBarText = string.Empty;

    [ObservableProperty]
    private float _progressBarValue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LaunchGameButtonText))]
    [NotifyCanExecuteChangedFor(nameof(LaunchGameCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenGameFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenPCGamingWikiCommand))]
    private FixesList? _selectedGame;
    partial void OnSelectedGameChanged(FixesList? oldValue, FixesList? newValue)
    {
        UpdateSelectedGameFixesList();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedFixVariants))]
    [NotifyPropertyChangedFor(nameof(SelectedFixUrl))]
    [NotifyPropertyChangedFor(nameof(SelectedFixTags))]
    [NotifyPropertyChangedFor(nameof(InstallButtonText))]
    [NotifyPropertyChangedFor(nameof(IsAdminMessageVisible))]
    [NotifyPropertyChangedFor(nameof(SelectedFixDescription))]
    [NotifyPropertyChangedFor(nameof(SelectedFixNumberOfInstalls))]
    [NotifyPropertyChangedFor(nameof(SelectedFixScore))]
    [NotifyPropertyChangedFor(nameof(IsSelectedFixUpvoted))]
    [NotifyPropertyChangedFor(nameof(IsSelectedFixDownvoted))]
    [NotifyPropertyChangedFor(nameof(IsStatsVisible))]
    [NotifyCanExecuteChangedFor(nameof(InstallFixCommand))]
    [NotifyCanExecuteChangedFor(nameof(UninstallFixCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenConfigCommand))]
    [NotifyCanExecuteChangedFor(nameof(UpdateFixCommand))]
    [NotifyCanExecuteChangedFor(nameof(LaunchGameCommand))]
    private BaseFixEntity? _selectedFix;
    partial void OnSelectedFixChanged(BaseFixEntity? oldValue, BaseFixEntity? newValue)
    {
        GetRequirementsString();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowVariantsPopupButtonText))]
    [NotifyPropertyChangedFor(nameof(InstallButtonText))]
    [NotifyCanExecuteChangedFor(nameof(InstallFixCommand))]
    private string? _selectedFixVariant;

    [ObservableProperty]
    private string _selectedTagFilter;
    async partial void OnSelectedTagFilterChanging(string value)
    {
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        if (_selectedTagFilter is null ||
            value is null)
        {
            return;
        }

        _selectedTagFilter = value;
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field

        await FillGamesListAsync().ConfigureAwait(true);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ClearSearchCommand))]
    private string _searchBarText;
    async partial void OnSearchBarTextChanged(string value) => await FillGamesListAsync().ConfigureAwait(true);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateGamesCommand))]
    private bool _isInProgress;
    partial void OnIsInProgressChanged(bool value) => LockButtons = value;

    #endregion Binding Properties


    public MainViewModel(
        MainModel mainModel,
        ApiInterface apiInterface,
        FixManager fixManager,
        IConfigProvider config,
        PopupMessageViewModel popupMessage,
        PopupEditorViewModel popupEditor,
        ProgressReport progressReport,
        PopupStackViewModel popupStack
        )
    {
        _mainModel = mainModel;
        _apiInterface = apiInterface;
        _fixManager = fixManager;
        _config = config;
        _popupMessage = popupMessage;
        _popupEditor = popupEditor;
        _progressReport = progressReport;
        _popupStack = popupStack;

        _searchBarText = string.Empty;

        SelectedTagFilter = TagsComboboxList.First();

        _config.ParameterChangedEvent += OnParameterChangedEvent;
    }


    #region Relay Commands

    /// <summary>
    /// VM initialization
    /// </summary>
    [RelayCommand]
    private Task InitializeAsync() => UpdateAsync();


    /// <summary>
    /// Update games list
    /// </summary>
    [RelayCommand(CanExecute = (nameof(UpdateGamesCanExecute)))]
    private async Task UpdateGamesAsync()
    {
        await UpdateAsync().ConfigureAwait(true);

        InstallFixCommand.NotifyCanExecuteChanged();
        UninstallFixCommand.NotifyCanExecuteChanged();
        UpdateFixCommand.NotifyCanExecuteChanged();
    }
    private bool UpdateGamesCanExecute() => !LockButtons;


    /// <summary>
    /// Install selected fix
    /// </summary>
    [RelayCommand(CanExecute = (nameof(InstallFixCanExecute)))]
    private Task<Result> InstallFixAsync()
    {
        Guard.IsNotNull(SelectedGame);
        Guard.IsNotNull(SelectedFix);

        var installResult = InstallUpdateFixAsync(
            SelectedGame,
            SelectedFix,
            SelectedFixVariant,
            false);

        return installResult;
    }
    private bool InstallFixCanExecute()
    {
        if (SelectedGame is null ||
            SelectedFix is null ||
            SelectedFix is TextFixEntity ||
            SelectedFix.IsInstalled ||
            !SelectedGame.IsGameInstalled ||
            (SelectedFixVariants is not null && SelectedFixVariant is null) ||
            LockButtons
            )
        {
            return false;
        }

        return true;
    }


    /// <summary>
    /// Cancel ongoing task
    /// </summary>
    [RelayCommand(CanExecute = (nameof(CancelCanExecute)))]
    private async Task CancelAsync() => await _cancellationTokenSource!.CancelAsync().ConfigureAwait(true);
    private bool CancelCanExecute() => LockButtons;


    /// <summary>
    /// Update selected fix
    /// </summary>
    [RelayCommand(CanExecute = (nameof(UpdateFixCanExecute)))]
    private Task<Result> UpdateFixAsync()
    {
        Guard.IsNotNull(SelectedGame);
        Guard.IsNotNull(SelectedFix);

        var installResult = InstallUpdateFixAsync(SelectedGame, SelectedFix, SelectedFixVariant, false);
        return installResult;
    }
    public bool UpdateFixCanExecute() => SelectedFix is not null && SelectedFix.IsOutdated && !LockButtons;


    /// <summary>
    /// Uninstall selected fix
    /// </summary>
    [RelayCommand(CanExecute = (nameof(UninstallFixCanExecute)))]
    private async Task UninstallFixAsync()
    {
        Guard.IsNotNull(SelectedFix);
        Guard.IsNotNull(SelectedGame?.Game);

        var dependantFixes = _mainModel.GetInstalledDependentFixes(SelectedGame.Fixes, SelectedFix.Guid);

        if (dependantFixes is not null)
        {
            var res = await _popupMessage.ShowAndGetResultAsync("Required fixes",
                $"""
                            The following fixes require this fix to work.
                            Do you want to uninstall them?

                            {string.Join(Environment.NewLine, dependantFixes)}
                            """,
                PopupMessageType.YesNo).ConfigureAwait(true);

            if (!res)
            {
                return;
            }

            foreach (var dep in dependantFixes)
            {
                var result = _fixManager.UninstallFix(SelectedGame.Game, dep);

                if (!result.IsSuccess)
                {
                    var length2 = App.Random.Next(1, 100);
                    var repeatedString2 = new string('\u200B', length2);

                    App.NotificationManager.Show(
                        result.Message + repeatedString2,
                        NotificationType.Error
                        );

                    return;
                }
            }
        }

        IsInProgress = true;

        var fixUninstallResult = _fixManager.UninstallFix(SelectedGame.Game, SelectedFix);

        await FillGamesListAsync().ConfigureAwait(true);

        IsInProgress = false;

        var length = App.Random.Next(1, 100);
        var repeatedString = new string('\u200B', length);

        App.NotificationManager.Show(
            fixUninstallResult.Message + repeatedString,
            fixUninstallResult.IsSuccess ? NotificationType.Success : NotificationType.Error
            );
    }
    private bool UninstallFixCanExecute()
    {
        if (SelectedFix is null ||
            !SelectedFix.IsInstalled ||
            (SelectedGame is not null && !SelectedGame.IsGameInstalled) ||
            LockButtons)
        {
            return false;
        }

        //var result = !_mainModel.DoesFixHaveInstalledDependentFixes(SelectedGameFixesList, SelectedFix.Guid);

        //return result;

        return true;
    }


    /// <summary>
    /// Clear search bar
    /// </summary>
    [RelayCommand(CanExecute = (nameof(ClearSearchCanExecute)))]
    private void ClearSearch() => SearchBarText = string.Empty;
    private bool ClearSearchCanExecute() => !string.IsNullOrEmpty(SearchBarText);


    /// <summary>
    /// Open config file for selected fix
    /// </summary>
    [RelayCommand(CanExecute = (nameof(OpenConfigCanExecute)))]
    private void OpenConfig()
    {
        OpenConfigFileAsync(SelectedGame?.Game, SelectedFix);
    }

    private bool OpenConfigCanExecute() => SelectedFix is FileFixEntity fileFix && fileFix.ConfigFile is not null && fileFix.IsInstalled && SelectedGame is not null && SelectedGame.IsGameInstalled;


    /// <summary>
    /// Open selected game install folder
    /// </summary>
    [RelayCommand(CanExecute = (nameof(OpenGameFolderCanExecute)))]
    private void OpenGameFolder()
    {
        Guard.IsNotNull(SelectedGame?.Game);

        _ = Process.Start(new ProcessStartInfo
        {
            FileName = SelectedGame.Game.InstallDir,
            UseShellExecute = true
        });
    }
    private bool OpenGameFolderCanExecute() => SelectedGame is not null && SelectedGame.IsGameInstalled;


    /// <summary>
    /// Open PCGW page for selected game
    /// </summary>
    [RelayCommand]
    private void OpenPCGamingWiki()
    {
        Guard.IsNotNull(SelectedGame);

        _ = Process.Start(new ProcessStartInfo
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

        _ = Process.Start(new ProcessStartInfo
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

        _ = Process.Start(new ProcessStartInfo
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

        _ = Process.Start(new ProcessStartInfo
        {
            FileName = @$"https://steamdb.info/app/{SelectedGame.GameId}/config/",
            UseShellExecute = true
        });
    }


    /// <summary>
    /// Copy file URL to clipboard
    /// </summary>
    [RelayCommand]
    private Task UrlCopyToClipboardAsync()
    {
        var clipboard = AvaloniaProperties.TopLevel.Clipboard ?? ThrowHelper.ThrowArgumentNullException<IClipboard>("Error while getting clipboard implementation");
        return clipboard.SetTextAsync(SelectedFixUrl);
    }


    /// <summary>
    /// Launch/install game
    /// </summary>
    [RelayCommand(CanExecute = (nameof(LaunchGameCanExecute)))]
    private void LaunchGame()
    {
        Guard.IsNotNull(SelectedGame);

        if (SelectedGame.IsGameInstalled)
        {
            _ = Process.Start(new ProcessStartInfo
            {
                FileName = $"steam://rungameid/{SelectedGame.GameId}",
                UseShellExecute = true
            });
        }
        else
        {
            _ = Process.Start(new ProcessStartInfo
            {
                FileName = $"steam://install/{SelectedGame.GameId}",
                UseShellExecute = true
            });
        }
    }
    private bool LaunchGameCanExecute() => SelectedGame is not null && !LockButtons;


    /// <summary>
    /// Close app
    /// </summary>
    [RelayCommand]
    private void CloseApp() => AvaloniaProperties.MainWindow.Close();


    /// <summary>
    /// Hide selected tag
    /// </summary>
    [RelayCommand]
    private void HideTag(string tag) => _mainModel.HideTag(tag);


    /// <summary>
    /// Show popup with filters
    /// </summary>
    [RelayCommand]
    private async Task ShowFiltersPopup()
    {
        var selectedTag = await _popupStack.ShowAndGetResultAsync("Tags", TagsComboboxList).ConfigureAwait(true);

        SelectedTagFilter = selectedTag;

        OnPropertyChanged(nameof(ShowPopupStackButtonText));
    }


    /// <summary>
    /// Show popup with fix variants
    /// </summary>
    [RelayCommand]
    private async Task ShowVariantsPopup()
    {
        Guard.IsNotNull(SelectedFixVariants);

        var selectedTag = await _popupStack.ShowAndGetResultAsync("Variants", SelectedFixVariants).ConfigureAwait(true);

        SelectedFixVariant = selectedTag;

        OnPropertyChanged(nameof(ShowPopupStackButtonText));
    }


    /// <summary>
    /// Upvote fix
    /// </summary>
    [RelayCommand]
    private async Task Upvote()
    {
        Guard.IsNotNull(SelectedFix);

        _ = await _mainModel.ChangeVoteAsync(SelectedFix, true).ConfigureAwait(true);
        OnPropertyChanged(nameof(SelectedFixScore));
        OnPropertyChanged(nameof(IsSelectedFixUpvoted));
        OnPropertyChanged(nameof(IsSelectedFixDownvoted));
    }


    /// <summary>
    /// Downvote fix
    /// </summary>
    [RelayCommand]
    private async Task Downvote()
    {
        Guard.IsNotNull(SelectedFix);

        _ = await _mainModel.ChangeVoteAsync(SelectedFix, false).ConfigureAwait(true);
        OnPropertyChanged(nameof(SelectedFixScore));
        OnPropertyChanged(nameof(IsSelectedFixUpvoted));
        OnPropertyChanged(nameof(IsSelectedFixDownvoted));
    }


    /// <summary>
    /// Report fix
    /// </summary>
    [RelayCommand]
    private async Task ReportFix()
    {
        Guard.IsNotNull(SelectedFix);

        var reportText = await _popupEditor.ShowAndGetResultAsync(
            "Report fix",
            string.Empty
            ).ConfigureAwait(true);

        if (reportText is null)
        {
            return;
        }

        var result = await _apiInterface.ReportFixAsync(SelectedFix.Guid, reportText).ConfigureAwait(true);

        var length = App.Random.Next(1, 100);
        var repeatedString = new string('\u200B', length);

        App.NotificationManager.Show(
            result.Message + repeatedString,
            result.IsSuccess ? NotificationType.Success : NotificationType.Error
            );
    }

    #endregion Relay Commands


    public async Task UpdateFilteredGamesListAsync(FixesList? additionalFix)
    {
        var list = await _mainModel.GetFilteredGamesListAsync(SearchBarText, SelectedTagFilter, additionalFix).ConfigureAwait(true);
        FilteredGamesList = list;
    }


    public void UpdateSelectedGameFixesList()
    {
        if (SelectedGame is null)
        {
            SelectedGameFixesList = null;
            return;
        }

        var fixes = SelectedGame.Fixes.Where(static x => !x.IsHidden);

        SelectedGameFixesList = [.. fixes];
    }


    public async Task TestFixAsync(FixesList newFix)
    {
        SearchBarText = string.Empty;
        SelectedTagFilter = Consts.All;

        _additionalFix = newFix;

        await UpdateFilteredGamesListAsync(_additionalFix).ConfigureAwait(true);

        var game = FilteredGamesList?.FirstOrDefault(x => x.GameId == newFix.GameId);
        SelectedGame = game;

        var fix = SelectedGameFixesList?.FirstOrDefault(x => x.Guid == newFix.Fixes[0].Guid);
        SelectedFix = fix;
    }


    /// <summary>
    /// Install or update fix
    /// </summary>
    /// <param name="fixesList">List of fixes</param>
    /// <param name="fix">Fix to install</param>
    /// <param name="fixVariant">Fix variant</param>
    /// <param name="skipDependencies">Don't install or update dependencies</param>
    /// 
    private async Task<Result> InstallUpdateFixAsync(FixesList fixesList, BaseFixEntity fix, string? fixVariant, bool skipDependencies)
    {
        Guard.IsNotNull(fixesList.Game);

        var isUpdate = fix.IsInstalled;

        if (!isUpdate && !skipDependencies)
        {
            var dependantFixes = _mainModel.GetNotInstalledDependencies(fixesList, fix);

            if (dependantFixes is not null)
            {
                var res = await _popupMessage.ShowAndGetResultAsync("Required fixes",
                    $"""
                            The following fixes are required for this fix to work.
                            Do you want to install them?

                            {string.Join(Environment.NewLine, dependantFixes)}
                            """,
                    PopupMessageType.YesNo).ConfigureAwait(true);

                if (!res)
                {
                    return new(ResultEnum.Cancelled, string.Empty);
                }

                foreach (var dep in dependantFixes)
                {
                    var depInstallResult = await InstallUpdateFixAsync(fixesList, dep, fixVariant, true).ConfigureAwait(true);

                    if (!depInstallResult.IsSuccess)
                    {
                        return depInstallResult;
                    }
                }
            }
        }

        LockButtons = true;

        _progressReport.Progress.ProgressChanged += ProgressChanged;
        _progressReport.NotifyOperationMessageChanged += OperationMessageChanged;

        _cancellationTokenSource = new();

        await _mainModel.IncreaseInstalls(fix).ConfigureAwait(true);

        Result result;

        if (isUpdate)
        {
            result = await _fixManager.UpdateFixAsync(fixesList.Game, fix, fixVariant, false, _cancellationTokenSource.Token).ConfigureAwait(true);
        }
        else
        {
            result = await _fixManager.InstallFixAsync(fixesList.Game, fix, fixVariant, false, _cancellationTokenSource.Token).ConfigureAwait(true);
        }

        if (result == ResultEnum.MD5Error)
        {
            var popupResult = await _popupMessage.ShowAndGetResultAsync(
                "Warning",
                @"MD5 of the file doesn't match the database. This file wasn't verified by the maintainer.

Do you still want to install the fix?",
                PopupMessageType.YesNo
                ).ConfigureAwait(true);

            if (popupResult)
            {
                result = await _fixManager.InstallFixAsync(fixesList.Game, fix, fixVariant, true, _cancellationTokenSource.Token).ConfigureAwait(true);
            }
        }

        LockButtons = false;

        _progressReport.Progress.ProgressChanged -= ProgressChanged;
        _progressReport.NotifyOperationMessageChanged -= OperationMessageChanged;

        ProgressBarValue = 0;
        ProgressBarText = string.Empty;

        if (!result.IsSuccess)
        {
            var length = App.Random.Next(1, 100);
            var repeatedString = new string('\u200B', length);

            App.NotificationManager.Show(
                result.Message + repeatedString,
                NotificationType.Error
                );

            return result;
        }

        await FillGamesListAsync().ConfigureAwait(true);

        if (fix is FileFixEntity fileFix &&
            fileFix.ConfigFile is not null &&
            _config.OpenConfigAfterInstall)
        {
            var length = App.Random.Next(1, 100);
            var repeatedString = new string('\u200B', length);

            App.NotificationManager.Show(
                result.Message + Environment.NewLine + "Open config file?" + repeatedString, 
                NotificationType.Information, 
                onClick: () => OpenConfigFileAsync(fixesList.Game, fileFix)
                );
        }
        else
        {
            var length = App.Random.Next(1, 100);
            var repeatedString = new string('\u200B', length);

            App.NotificationManager.Show(
                result.Message + repeatedString,
                NotificationType.Success
                );
        }

        return result;
    }

    /// <summary>
    /// Update games list
    /// </summary>
    private async Task UpdateAsync()
    {
        await _locker.WaitAsync().ConfigureAwait(true);
        IsInProgress = true;
        ProgressBarText = "Updating...";

        var result = await _mainModel.UpdateGamesListAsync().ConfigureAwait(true);

        await FillGamesListAsync().ConfigureAwait(true);

        if (!result.IsSuccess)
        {
            var length = App.Random.Next(1, 100);
            var repeatedString = new string('\u200B', length);

            App.NotificationManager.Show(
                result.Message + repeatedString,
                NotificationType.Error
                );
        }

        OnPropertyChanged(nameof(TagsComboboxList));

        IsInProgress = false;
        ProgressBarText = string.Empty;

        _ = _locker.Release();
    }

    /// <summary>
    /// Fill games and available games lists based on a search bar
    /// </summary>
    private async Task FillGamesListAsync()
    {
        var selectedGameId = SelectedGame?.GameId;
        var selectedFixGuid = SelectedFix?.Guid;

        await UpdateFilteredGamesListAsync(_additionalFix).ConfigureAwait(true);

        OnPropertyChanged(nameof(MainTabHeader));

        if (selectedGameId is not null)
        {
            SelectedGame = FilteredGamesList?.FirstOrDefault(x => x.GameId == selectedGameId);

            if (selectedFixGuid is not null)
            {
                SelectedFix = SelectedGameFixesList?.FirstOrDefault(x => x.Guid == selectedFixGuid);
            }
        }
    }

    /// <summary>
    /// Open config file for selected fix
    /// </summary>
    private async void OpenConfigFileAsync(GameEntity? game, BaseFixEntity? fix)
    {
        Guard2.IsOfType<FileFixEntity>(fix, out var fileFix);
        Guard.IsNotNull(fileFix?.ConfigFile);
        Guard.IsNotNull(game);

        if (!fix.IsInstalled)
        {
            return;
        }

        var pathToConfig = Path.Combine(game.InstallDir, fileFix.ConfigFile);

        if (fileFix.ConfigFile.EndsWith(".exe"))
        {
            _ = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(pathToConfig),
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(pathToConfig)
            });
        }
        else
        {
            var config = await File.ReadAllTextAsync(pathToConfig).ConfigureAwait(true);

            var result = await _popupEditor.ShowAndGetResultAsync("Config", config).ConfigureAwait(true);

            if (result is null)
            {
                return;
            }

            await File.WriteAllTextAsync(pathToConfig, result).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Get requirements for selected fix
    /// </summary>
    private void GetRequirementsString()
    {
        if (SelectedFix is null ||
            SelectedGame is null ||
            SelectedGameFixesList is null)
        {
            SelectedFixRequirements = null;
            SelectedFixDependencies = null;

            return;
        }

        var dependsOn = _mainModel.GetDependenciesForAFix(SelectedGame, SelectedFix);

        if (dependsOn is null)
        {
            SelectedFixRequirements = null;
        }

        if (dependsOn is not null)
        {
            SelectedFixRequirements = [.. dependsOn.Select(x => x.ToString())];
        }

        var dependedBy = _mainModel.GetDependentFixes(SelectedGameFixesList, SelectedFix.Guid);

        if (dependedBy is null)
        {
            SelectedFixDependencies = null;
        }

        if (dependedBy is not null)
        {
            SelectedFixDependencies = [.. dependedBy.Select(x => x.ToString())];
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

    private async void OnParameterChangedEvent(string parameterName)
    {
        if (parameterName.Equals(nameof(_config.ShowUninstalledGames)) ||
            parameterName.Equals(nameof(_config.ShowUnsupportedFixes)) ||
            parameterName.Equals(nameof(_config.HiddenTags)))
        {
            await FillGamesListAsync().ConfigureAwait(true);
        }

        if (parameterName.Equals(nameof(_config.UseLocalApiAndRepo)) ||
            parameterName.Equals(nameof(_config.LocalRepoPath)))
        {
            await UpdateAsync().ConfigureAwait(true);
        }
    }
}

