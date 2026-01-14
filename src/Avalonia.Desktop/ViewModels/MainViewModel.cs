using System.Collections.Immutable;
using System.Diagnostics;
using Api.Axiom.Interface;
using Avalonia.Controls.Notifications;
using Avalonia.Desktop.Helpers;
using Avalonia.Desktop.ViewModels.Popups;
using Avalonia.Input.Platform;
using Common.Axiom;
using Common.Axiom.Entities;
using Common.Axiom.Entities.Fixes;
using Common.Axiom.Entities.Fixes.FileFix;
using Common.Axiom.Entities.Fixes.TextFix;
using Common.Axiom.Helpers;
using Common.Client;
using Common.Client.FixTools;
using Common.Client.Models;
using Common.Client.Providers.Interfaces;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Avalonia.Desktop.ViewModels;

internal sealed partial class MainViewModel : ObservableObject, ISearchBarViewModel, IProgressBarViewModel
{
    private readonly MainModel _mainModel;
    private readonly IApiInterface _apiInterface;
    private readonly FixManager _fixManager;
    private readonly IFixesProvider _fixesProvider;
    private readonly IConfigProvider _config;
    private readonly PopupMessageViewModel _popupMessage;
    private readonly PopupEditorViewModel _popupEditor;
    private readonly PopupStackViewModel _popupStack;
    private readonly ProgressReport _progressReport;
    private readonly ILogger _logger;
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

    public bool DoesFixRequireAdminRights => SelectedFix is not null && SelectedFix.DoesRequireAdminRights && !ClientProperties.IsAdmin;

    public string ShowVariantsPopupButtonText => SelectedFixVariant ?? "Select variant...";

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
            if (SelectedFix is null)
            {
                return string.Empty;
            }

            if (DoesFixRequireAdminRights)
            {
                return "Restart as admin...";
            }

            if (SelectedFixVariants is not null && SelectedFixVariant is null)
            {
                return "<- Select fix variant";
            }

            if (SelectedFix is FileFixEntity fileFix)
            {
                if (fileFix.Url is null)
                {
                    return SelectedFix.IsOutdated ? "Update" : "Install";
                }

                var pathToArchive = _config.UseLocalApiAndRepo
                    ? Path.Combine(_config.LocalRepoPath, "fixes", Path.GetFileName(fileFix.Url))
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(fileFix.Url));

                if (File.Exists(pathToArchive))
                {
                    return SelectedFix.IsOutdated ? "Update" : "Install";
                }

                if (fileFix.Url is null ||
                    fileFix.FileSize is null)
                {
                    return SelectedFix.IsOutdated ? "Download and update" : "Download and install";
                }

                var size = fileFix.FileSize;

                if (fileFix.SharedFix?.FileSize is not null)
                {
                    size += fileFix.SharedFix.FileSize;
                }

                return SelectedFix.IsOutdated ? $"Download ({size.ToSizeString()}) and update" : $"Download ({size.ToSizeString()}) and install";
            }

            return SelectedFix.IsOutdated ? "Update" : "Install";
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

            string result = SelectedFix.GetMarkdownDescription();

            if (SelectedFix is FileFixEntity fileFix && fileFix.SharedFix?.Description is not null)
            {
                result += Environment.NewLine + Environment.NewLine + fileFix.SharedFix.GetMarkdownDescription();
            }

            return result;
        }
    }

    public string? SelectedFixChangelog => SelectedFix?.Changelog;

    public string? SelectedFixNumberOfInstalls
    {
        get
        {
            if (SelectedFix is null ||
                _fixesProvider.Installs is null ||
                SelectedFix is not FileFixEntity ||
                (SelectedFix is FileFixEntity fileFix && fileFix.Url is null))
            {
                return null;
            }

            var result = _fixesProvider.Installs.TryGetValue(SelectedFix.Guid, out var installs);

            if (result)
            {
                if (installs == 1)
                {
                    return "1 download";
                }

                return $"{installs} downloads";
            }

            return null;
        }
    }

    public string SelectedFixScore
    {
        get
        {
            if (SelectedFix is null ||
                _fixesProvider.Scores is null)
            {
                return "-";
            }

            var result = _fixesProvider.Scores.TryGetValue(SelectedFix.Guid, out var score);

            if (result)
            {
                return score.ToString();
            }

            return "-";
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

            return hasUpvote && !isUpvote;
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
    [NotifyCanExecuteChangedFor(nameof(InstallUpdateFixCommand))]
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
    [NotifyPropertyChangedFor(nameof(DoesFixRequireAdminRights))]
    [NotifyPropertyChangedFor(nameof(SelectedFixDescription))]
    [NotifyPropertyChangedFor(nameof(SelectedFixChangelog))]
    [NotifyPropertyChangedFor(nameof(SelectedFixNumberOfInstalls))]
    [NotifyPropertyChangedFor(nameof(SelectedFixScore))]
    [NotifyPropertyChangedFor(nameof(IsSelectedFixUpvoted))]
    [NotifyPropertyChangedFor(nameof(IsSelectedFixDownvoted))]
    [NotifyPropertyChangedFor(nameof(IsStatsVisible))]
    [NotifyCanExecuteChangedFor(nameof(InstallUpdateFixCommand))]
    [NotifyCanExecuteChangedFor(nameof(UninstallFixCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenConfigCommand))]
    [NotifyCanExecuteChangedFor(nameof(LaunchGameCommand))]
    [NotifyCanExecuteChangedFor(nameof(UpvoteCommand))]
    [NotifyCanExecuteChangedFor(nameof(DownvoteCommand))]
    [NotifyCanExecuteChangedFor(nameof(CheckHashCommand))]
    private BaseFixEntity? _selectedFix;
    partial void OnSelectedFixChanged(BaseFixEntity? oldValue, BaseFixEntity? newValue)
    {
        IsDescriptionSelected = true;
        GetRequirementsString();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowVariantsPopupButtonText))]
    [NotifyPropertyChangedFor(nameof(InstallButtonText))]
    [NotifyCanExecuteChangedFor(nameof(InstallUpdateFixCommand))]
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

    [ObservableProperty]
    private bool _isDescriptionSelected;

    #endregion Binding Properties


    public MainViewModel(
        MainModel mainModel,
        IApiInterface apiInterface,
        FixManager fixManager,
        IFixesProvider fixesProvider,
        IConfigProvider config,
        PopupMessageViewModel popupMessage,
        PopupEditorViewModel popupEditor,
        ProgressReport progressReport,
        PopupStackViewModel popupStack,
        ILogger logger
        )
    {
        _mainModel = mainModel;
        _apiInterface = apiInterface;
        _fixManager = fixManager;
        _fixesProvider = fixesProvider;
        _config = config;
        _popupMessage = popupMessage;
        _popupEditor = popupEditor;
        _progressReport = progressReport;
        _popupStack = popupStack;
        _logger = logger;

        _searchBarText = string.Empty;

        SelectedTagFilter = TagsComboboxList.First();

        _config.ParameterChangedEvent += OnParameterChangedEvent;
    }


    #region Relay Commands

    /// <summary>
    /// VM initialization
    /// </summary>
    [RelayCommand]
    private async Task InitializeAsync()
    {
        await UpdateAsync(true, true, true).ConfigureAwait(true);
        await UpdateAsync(false, true, false).ConfigureAwait(true);

        if (!_config.IsConsented)
        {
            _popupMessage.Show(
            "Disclaimer",
            """
            This application is a community-driven utility designed to help users install optional, third-party fixes and improvements for their licensed Steam games.

            This Software is not affiliated with or endorsed by Valve Corporation or the respective game publishers.

            Use this Software and the associated content at your own discretion. Please be aware that using third-party modifications may occasionally lead to game instability or technical conflicts.

            While we aim for a seamless experience, we cannot guarantee the performance or compatibility of external content. The developers of this Software are not responsible for any technical issues or data loss that may arise from using community-made fixes.
            """,
            PopupMessageType.OkOnly
            );

            _config.IsConsented = true;
        }
    }


    /// <summary>
    /// Update games list
    /// </summary>
    [RelayCommand(CanExecute = nameof(UpdateGamesCanExecute))]
    private async Task UpdateGamesAsync()
    {
        await UpdateAsync(false, true, true).ConfigureAwait(true);

        InstallUpdateFixCommand.NotifyCanExecuteChanged();
        UninstallFixCommand.NotifyCanExecuteChanged();
    }
    private bool UpdateGamesCanExecute() => !LockButtons;


    /// <summary>
    /// Install selected fix
    /// </summary>
    [RelayCommand(CanExecute = nameof(InstallUpdateFixCanExecute))]
    private void InstallUpdateFix()
    {
        Guard.IsNotNull(SelectedGame);
        Guard.IsNotNull(SelectedFix);

        try
        {
            try
            {
                if (DoesFixRequireAdminRights)
                {
                    using var _ = Process.Start(new ProcessStartInfo { FileName = Environment.ProcessPath, UseShellExecute = true, Verb = "runas" });

                    Environment.Exit(0);
                }
            }
            catch (Exception)
            {
                NotificationsHelper.Show(
                    "Superheater needs to be run as admin in order to install this fix",
                    NotificationType.Error
                    );

                return;
            }

            _ = InstallUpdateFixAsync(
                SelectedGame,
                SelectedFix,
                SelectedFixVariant,
                false);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Error while installing fix {SelectedFix.Name} for {SelectedGame.GameName}");

            NotificationsHelper.Show(
                "Critical error while installing fix",
                NotificationType.Error
                );
        }
    }
    private bool InstallUpdateFixCanExecute()
    {
        if (SelectedGame is null ||
            SelectedFix is null ||
            SelectedFix is TextFixEntity ||
            !SelectedGame.IsGameInstalled ||
            (SelectedFixVariants is not null && SelectedFixVariant is null) ||
            (SelectedFix.IsInstalled && !SelectedFix.IsOutdated) ||
            LockButtons)
        {
            return false;
        }

        return true;
    }


    /// <summary>
    /// Cancel ongoing task
    /// </summary>
    [RelayCommand(CanExecute = nameof(CancelCanExecute))]
    private async Task CancelAsync() => await _cancellationTokenSource!.CancelAsync().ConfigureAwait(true);
    private bool CancelCanExecute() => LockButtons;

    /// <summary>
    /// Cancel ongoing task
    /// </summary>
    [RelayCommand(CanExecute = nameof(CheckHashCanExecute))]
    private async Task CheckHashAsync()
    {
        Guard2.IsOfType<FileFixEntity>(SelectedFix, out var fileFix);
        Guard.IsNotNull(SelectedGame?.Game);

        var fixUninstallResult = await _fixManager.CheckFixAsync(SelectedGame.Game, fileFix).ConfigureAwait(true);

        NotificationsHelper.Show(
            fixUninstallResult.Message,
            fixUninstallResult.IsSuccess ? NotificationType.Success : NotificationType.Error
            );
    }
    private bool CheckHashCanExecute() => (SelectedFix?.InstalledFix as FileInstalledFixEntity)?.FilesList?.Any(x => x.Value is not null) ?? false;


    /// <summary>
    /// Uninstall selected fix
    /// </summary>
    [RelayCommand(CanExecute = nameof(UninstallFixCanExecute))]
    private async Task UninstallFixAsync()
    {
        Guard.IsNotNull(SelectedFix);
        Guard.IsNotNull(SelectedGame?.Game);

        try
        {
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
                        NotificationsHelper.Show(
                            result.Message,
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

            NotificationsHelper.Show(
                fixUninstallResult.Message,
                fixUninstallResult.IsSuccess ? NotificationType.Success : NotificationType.Error
                );
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Error while uninstalling fix {SelectedFix.Name} for {SelectedGame.GameName}");

            NotificationsHelper.Show(
                "Critical error while uninstalling fix",
                NotificationType.Error
                );
        }
    }
    private bool UninstallFixCanExecute()
    {
        if (SelectedFix is null ||
            !SelectedFix.IsInstalled ||
            (SelectedGame is not null && !SelectedGame.IsGameInstalled) ||
            LockButtons ||
            DoesFixRequireAdminRights)
        {
            return false;
        }

        return true;
    }


    /// <summary>
    /// Clear search bar
    /// </summary>
    [RelayCommand(CanExecute = nameof(ClearSearchCanExecute))]
    private void ClearSearch() => SearchBarText = string.Empty;
    private bool ClearSearchCanExecute() => !string.IsNullOrEmpty(SearchBarText);


    /// <summary>
    /// Open config file for selected fix
    /// </summary>
    [RelayCommand(CanExecute = nameof(OpenConfigCanExecute))]
    private void OpenConfig()
    {
        Guard.IsNotNull(SelectedGame?.Game);
        Guard.IsNotNull(SelectedFix);

        OpenConfigFileAsync(SelectedGame.Game, SelectedFix);
    }

    private bool OpenConfigCanExecute() => SelectedFix is FileFixEntity fileFix && fileFix.ConfigFile is not null && fileFix.IsInstalled && SelectedGame is not null && SelectedGame.IsGameInstalled;


    /// <summary>
    /// Open selected game install folder
    /// </summary>
    [RelayCommand(CanExecute = nameof(OpenGameFolderCanExecute))]
    private void OpenGameFolder()
    {
        Guard.IsNotNull(SelectedGame?.Game);

        using var _ = Process.Start(new ProcessStartInfo
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

        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = ClientConstants.PCGamingWikiUrl + SelectedGame.GameId,
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
    [RelayCommand(CanExecute = nameof(LaunchGameCanExecute))]
    private void LaunchGame()
    {
        Guard.IsNotNull(SelectedGame);

        if (SelectedGame.IsGameInstalled)
        {
            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = $"steam://rungameid/{SelectedGame.GameId}",
                UseShellExecute = true
            });
        }
        else
        {
            using var _ = Process.Start(new ProcessStartInfo
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
    [RelayCommand(CanExecute = nameof(UpvoteCanExecute))]
    private async Task Upvote()
    {
        Guard.IsNotNull(SelectedFix);

        _ = await _mainModel.ChangeVoteAsync(SelectedFix, true).ConfigureAwait(true);
        OnPropertyChanged(nameof(SelectedFixScore));
        OnPropertyChanged(nameof(IsSelectedFixUpvoted));
        OnPropertyChanged(nameof(IsSelectedFixDownvoted));
    }
    private bool UpvoteCanExecute() => _fixesProvider.Scores is not null;


    /// <summary>
    /// Downvote fix
    /// </summary>
    [RelayCommand(CanExecute = nameof(DownvoteCanExecute))]
    private async Task Downvote()
    {
        Guard.IsNotNull(SelectedFix);

        _ = await _mainModel.ChangeVoteAsync(SelectedFix, false).ConfigureAwait(true);
        OnPropertyChanged(nameof(SelectedFixScore));
        OnPropertyChanged(nameof(IsSelectedFixUpvoted));
        OnPropertyChanged(nameof(IsSelectedFixDownvoted));
    }
    private bool DownvoteCanExecute() => _fixesProvider.Scores is not null;


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

        NotificationsHelper.Show(
            result.Message,
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

        var fixes = SelectedGame.Fixes.Where(static x => !x.IsHidden).OrderBy(static x => x.IsDisabled);

        SelectedGameFixesList = [.. fixes];
    }


    public async Task TestFixAsync(FixesList newFix)
    {
        SearchBarText = string.Empty;
        SelectedTagFilter = ClientConstants.All;

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
    private async Task<Result> InstallUpdateFixAsync(FixesList fixesList, BaseFixEntity fix, string? fixVariant, bool skipDependencies)
    {
        Guard.IsNotNull(fixesList.Game);
        Guard.IsNotNull(SelectedGame?.Game);

        LockButtons = true;

        _progressReport.Progress.ProgressChanged += ProgressChanged;
        _progressReport.NotifyOperationMessageChanged += OperationMessageChanged;

        var isUpdate = fix.IsInstalled;

        if (SelectedGame.Game.BuildId != SelectedGame.Game.TargetBuildId)
        {
            var res = await _popupMessage.ShowAndGetResultAsync(
                "Game update pending",
                $"""
                There is a pending update for {SelectedGame.Game.Name} on Steam.
                It's recommended to update the game before installing the fix.

                Do you want to ignore this message and install the fix anyway?
                """,
                PopupMessageType.YesNo).ConfigureAwait(true);

            if (!res)
            {
                return new(ResultEnum.Cancelled, string.Empty);
            }
        }

        if (!isUpdate && !skipDependencies)
        {
            var dependantFixes = _mainModel.GetNotInstalledDependencies(fixesList, fix);

            if (dependantFixes is not null)
            {
                var res = await _popupMessage.ShowAndGetResultAsync(
                    "Required fixes",
                    $"""
                    The following fixes are required for this fix to work.

                    {string.Join(Environment.NewLine, dependantFixes)}

                    Do you want to install them?
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

        _cancellationTokenSource = new();

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
                """
                MD5 hash of the downloaded file doesn't match the one from the database.
                
                This file may have been stealth-updated by the original author and wasn't yet verified by the maintainers.

                Do you still want to install the fix?
                """,
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
            NotificationsHelper.Show(
                result.Message,
                NotificationType.Error
                );

            return result;
        }

        await FillGamesListAsync().ConfigureAwait(true);

        if (fix is FileFixEntity fileFix &&
            fileFix.ConfigFile is not null &&
            _config.OpenConfigAfterInstall)
        {
            NotificationsHelper.Show(
                result.Message + Environment.NewLine + "Open config file?",
                NotificationType.Information,
                onClick: () => OpenConfigFileAsync(fixesList.Game, fileFix)
                );
        }
        else
        {
            NotificationsHelper.Show(
                result.Message,
                NotificationType.Success
                );
        }

        return result;
    }

    /// <summary>
    /// Update games list
    /// </summary>
    /// <param name="localFixesOnly">Load only local cached fixes</param>
    /// <param name="dropFixesCache">Drop current fixes cache and create new</param>
    /// <param name="dropGamesCache">Drop current games cache and create new</param>
    private async Task UpdateAsync(bool localFixesOnly, bool dropFixesCache, bool dropGamesCache)
    {
        await _locker.WaitAsync().ConfigureAwait(true);
        IsInProgress = true;
        ProgressBarText = "Updating...";

        var result = await _mainModel.UpdateGamesListAsync(localFixesOnly, dropFixesCache, dropGamesCache).ConfigureAwait(true);

        await FillGamesListAsync().ConfigureAwait(true);

        if (!result.IsSuccess)
        {
            NotificationsHelper.Show(
                result.Message,
                NotificationType.Error
                );
        }

        OnPropertyChanged(nameof(TagsComboboxList));
        UpvoteCommand.NotifyCanExecuteChanged();
        DownvoteCommand.NotifyCanExecuteChanged();

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
    private async void OpenConfigFileAsync(GameEntity game, BaseFixEntity fix)
    {
        try
        {
            Guard2.IsOfType<FileFixEntity>(fix, out var fileFix);
            Guard.IsNotNull(fileFix.ConfigFile);
            Guard.IsNotNull(game);

            if (!fix.IsInstalled)
            {
                return;
            }

            var pathToConfig = Path.Combine(game.InstallDir, fileFix.ConfigFile);

            if (fileFix.ConfigFile.EndsWith(".exe"))
            {
                using var _ = Process.Start(new ProcessStartInfo
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
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Error while opening config for {fix.Name} for {game.Name}");

            NotificationsHelper.Show(
                "Critical error while installing fix",
                NotificationType.Error
                );
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

        if (parameterName.Equals(nameof(_config.UseLocalApiAndRepo)))
        {
            await UpdateAsync(false, true, false).ConfigureAwait(true);
        }
    }
}

