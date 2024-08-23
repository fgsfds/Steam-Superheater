using Avalonia.Input.Platform;
using AvaloniaEdit.Utils;
using Common;
using Common.Client;
using Common.Client.API;
using Common.Client.Config;
using Common.Client.FixTools;
using Common.Client.Models;
using Common.Entities.CombinedEntities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.TextFix;
using Common.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Superheater.Avalonia.Core.Helpers;
using Superheater.Avalonia.Core.ViewModels.Popups;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Superheater.Avalonia.Core.ViewModels
{
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

        private FixesList _additionalFix;

        private CancellationTokenSource _cancellationTokenSource;


        #region Binding Properties

        public ObservableCollection<FixFirstCombinedEntity> FilteredGamesList { get; set; } = [];

        public void UpdateFilteredGamesList(FixesList? additionalFix = null)
        {
            FilteredGamesList.Clear();
            FilteredGamesList.AddRange(_mainModel.GetFilteredGamesList(SearchBarText, SelectedTagFilter, additionalFix));
        }

        public ObservableCollection<BaseFixEntity> SelectedGameFixesList { get; set; } = [];

        public void UpdateSelectedGameFixesList()
        {
            SelectedGameFixesList.Clear();

            if (SelectedGame is not null)
            {
                SelectedGameFixesList.AddRange(SelectedGame.FixesList.Fixes.Where(static x => !x.IsHidden));
            }
        }

        public ImmutableList<string> SelectedFixTags => SelectedFix?.Tags is null ? [] : [.. SelectedFix.Tags.Where(x => !_config.HiddenTags.Contains(x))];

        public HashSet<string> TagsComboboxList => _mainModel.GetListOfTags();

        public ImmutableList<string> SelectedFixVariants => SelectedFix is FileFixEntity fileFix && fileFix.Variants is not null ? [.. fileFix.Variants] : [];


        public bool IsSteamGameMode => ClientProperties.IsInSteamDeckGameMode;

        public bool DoesSelectedFixHaveVariants => !SelectedFixVariants.IsEmpty ;

        public bool IsVariantSelectorEnabled => DoesSelectedFixHaveVariants && !IsSteamGameMode;

        public bool IsDeckVariantSelectorEnabled => DoesSelectedFixHaveVariants && IsSteamGameMode;

        public bool DoesSelectedFixHaveUpdates => SelectedFix?.IsOutdated ?? false;

        public bool SelectedFixHasTags => !SelectedFixTags.IsEmpty;

        public bool IsAdminMessageVisible => SelectedFix is HostsFixEntity && !ClientProperties.IsAdmin;


        public string ShowVariantsPopupButtonText => SelectedFixVariant is null ? "Select variant..." : SelectedFixVariant;

        public string SelectedFixRequirements => GetRequirementsString();

        public string SelectedFixUrl => _mainModel.GetSelectedFixUrl(SelectedFix);

        public string ShowPopupStackButtonText => SelectedTagFilter;

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

                if (DoesSelectedFixHaveVariants && SelectedFixVariant is null)
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

                if (SelectedGame is null || 
                    !SelectedGame.IsGameInstalled)
                {
                    return false;
                }

                return true;
            }
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdateGamesCommand))]
        [NotifyCanExecuteChangedFor(nameof(InstallFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(UpdateFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(UninstallFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenConfigCommand))]
        [NotifyCanExecuteChangedFor(nameof(LaunchGameCommand))]
        private bool _lockButtons;

        [ObservableProperty]
        private string _mainTabHeader = "Main";

        [ObservableProperty]
        private string _launchGameButtonText = "Launch game...";

        [ObservableProperty]
        private string _progressBarText = string.Empty;

        [ObservableProperty]
        private float _progressBarValue;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LaunchGameCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenGameFolderCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenPCGamingWikiCommand))]
        private FixFirstCombinedEntity? _selectedGame;
        partial void OnSelectedGameChanged(FixFirstCombinedEntity? oldValue, FixFirstCombinedEntity? newValue)
        {
            UpdateSelectedGameFixesList();
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedFixRequirements))]
        [NotifyPropertyChangedFor(nameof(DoesSelectedFixHaveUpdates))]
        [NotifyPropertyChangedFor(nameof(SelectedFixVariants))]
        [NotifyPropertyChangedFor(nameof(IsVariantSelectorEnabled))]
        [NotifyPropertyChangedFor(nameof(IsDeckVariantSelectorEnabled))]
        [NotifyPropertyChangedFor(nameof(SelectedFixUrl))]
        [NotifyPropertyChangedFor(nameof(SelectedFixTags))]
        [NotifyPropertyChangedFor(nameof(SelectedFixHasTags))]
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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowVariantsPopupButtonText))]
        [NotifyPropertyChangedFor(nameof(InstallButtonText))]
        [NotifyCanExecuteChangedFor(nameof(InstallFixCommand))]
        private string? _selectedFixVariant;

        [ObservableProperty]
        private string _selectedTagFilter;
        partial void OnSelectedTagFilterChanging(string value)
        {
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            if (_selectedTagFilter is null ||
                value is null)
            {
                return;
            }

            _selectedTagFilter = value;
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field

            FillGamesList();
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ClearSearchCommand))]
        private string _searchBarText;
        partial void OnSearchBarTextChanged(string value) => FillGamesList();

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
        private Task InstallFixAsync() => InstallUpdateFixAsync(false);
        private bool InstallFixCanExecute()
        {
            if (SelectedGame is null ||
                SelectedFix is null ||
                SelectedFix is TextFixEntity ||
                SelectedFix.IsInstalled ||
                !SelectedGame.IsGameInstalled ||
                (DoesSelectedFixHaveVariants && SelectedFixVariant is null) ||
                LockButtons
                )
            {
                return false;
            }

            var result = !_mainModel.DoesFixHaveNotInstalledDependencies(SelectedGame, SelectedFix);

            return result;
        }


        /// <summary>
        /// Cancel ongoing task
        /// </summary>
        [RelayCommand(CanExecute = (nameof(CancelCanExecute)))]
        private async Task CancelAsync()
        {
            if (_cancellationTokenSource is not null)
            {
                await _cancellationTokenSource.CancelAsync().ConfigureAwait(true);
                _cancellationTokenSource.Dispose();
            }
        }
        private bool CancelCanExecute() => LockButtons;


        /// <summary>
        /// Update selected fix
        /// </summary>
        [RelayCommand(CanExecute = (nameof(UpdateFixCanExecute)))]
        private Task UpdateFixAsync() => InstallUpdateFixAsync(true);
        public bool UpdateFixCanExecute() => (SelectedGame is not null && SelectedGame.IsGameInstalled) || !LockButtons;


        /// <summary>
        /// Uninstall selected fix
        /// </summary>
        [RelayCommand(CanExecute = (nameof(UninstallFixCanExecute)))]
        private void UninstallFix()
        {
            SelectedFix.ThrowIfNull();
            SelectedGame.ThrowIfNull();
            SelectedGame.Game.ThrowIfNull();

            IsInProgress = true;

            var fixUninstallResult = _fixManager.UninstallFix(SelectedGame.Game, SelectedFix);

            FillGamesList();

            IsInProgress = false;

            _popupMessage.Show(
                fixUninstallResult.IsSuccess ? "Success" : "Error",
                fixUninstallResult.Message,
                PopupMessageType.OkOnly
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

            var result = !_mainModel.DoesFixHaveInstalledDependentFixes(SelectedGameFixesList, SelectedFix.Guid);

            return result;
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
        private void OpenConfig() => OpenConfigFileAsync();
        private bool OpenConfigCanExecute() => SelectedFix is FileFixEntity fileFix && fileFix.ConfigFile is not null && fileFix.IsInstalled && SelectedGame is not null && SelectedGame.IsGameInstalled;


        /// <summary>
        /// Open selected game install folder
        /// </summary>
        [RelayCommand(CanExecute = (nameof(OpenGameFolderCanExecute)))]
        private void OpenGameFolder()
        {
            SelectedGame.ThrowIfNull();
            SelectedGame.Game.ThrowIfNull();

            Process.Start(new ProcessStartInfo
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
            SelectedGame.ThrowIfNull();

            Process.Start(new ProcessStartInfo
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
            SelectedGame.ThrowIfNull();

            Process.Start(new ProcessStartInfo
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
            SelectedGame.ThrowIfNull();

            Process.Start(new ProcessStartInfo
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
            SelectedGame.ThrowIfNull();

            Process.Start(new ProcessStartInfo
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
            var clipboard = AvaloniaProperties.TopLevel.Clipboard ?? ThrowHelper.ArgumentNullException<IClipboard>("Error while getting clipboard implementation");
            return clipboard.SetTextAsync(SelectedFixUrl);
        }


        /// <summary>
        /// Launch/install game
        /// </summary>
        [RelayCommand(CanExecute = (nameof(LaunchGameCanExecute)))]
        private void LaunchGame()
        {
            SelectedGame.ThrowIfNull();

            if (SelectedGame.IsGameInstalled)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"steam://rungameid/{SelectedGame.GameId}",
                    UseShellExecute = true
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"steam://install/{SelectedGame.GameId}",
                    UseShellExecute = true
                });
            }
        }
        private bool LaunchGameCanExecute()
        {
            if (SelectedGame is null)
            {
                return false;
            }

            if (LockButtons)
            {
                return false;
            }

            LaunchGameButtonText = SelectedGame.IsGameInstalled
                ? "Launch game..."
                : "Install game...";
 
            return true;
        }


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
            SelectedFix.ThrowIfNull();

            await _mainModel.ChangeVoteAsync(SelectedFix, true).ConfigureAwait(true);
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
            SelectedFix.ThrowIfNull();

            await _mainModel.ChangeVoteAsync(SelectedFix, false).ConfigureAwait(true);
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
            SelectedFix.ThrowIfNull();

            var reportText = await _popupEditor.ShowAndGetResultAsync(
                "Report fix",
                string.Empty
                ).ConfigureAwait(true);

            if (reportText is null)
            {
                return;
            }

            var result = await _apiInterface.ReportFixAsync(SelectedFix.Guid, reportText).ConfigureAwait(true);

            _popupMessage.Show(
                "Report fix",
                result.IsSuccess ? "Report sent" : result.Message, 
                PopupMessageType.OkOnly
                );
        }

        #endregion Relay Commands


        public void TestFix(FixesList newFix)
        {
            SearchBarText = string.Empty;
            SelectedTagFilter = Consts.All;

            _additionalFix = newFix;

            UpdateFilteredGamesList(_additionalFix);

            var game = FilteredGamesList.First(x => x.GameId == newFix.GameId);
            SelectedGame = game;

            var fix = SelectedGameFixesList.First(x => x.Guid == newFix.Fixes[0].Guid);
            SelectedFix = fix;
        }


        /// <summary>
        /// Install or update fix
        /// </summary>
        /// <param name="isUpdate">Update fix</param>
        private async Task InstallUpdateFixAsync(bool isUpdate)
        {
            SelectedFix.ThrowIfNull();
            SelectedGame.ThrowIfNull();
            SelectedGame.Game.ThrowIfNull();

            LockButtons = true;

            _progressReport.Progress.ProgressChanged += ProgressChanged;
            _progressReport.NotifyOperationMessageChanged += OperationMessageChanged;

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            await _mainModel.IncreaseInstalls(SelectedFix).ConfigureAwait(true);

            Result result;

            if (isUpdate)
            {
                result = await _fixManager.UpdateFixAsync(SelectedGame.Game, SelectedFix, SelectedFixVariant, false, cancellationToken).ConfigureAwait(true);
            }
            else
            {
                result = await _fixManager.InstallFixAsync(SelectedGame.Game, SelectedFix, SelectedFixVariant, false, cancellationToken).ConfigureAwait(true);
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
                    result = await _fixManager.InstallFixAsync(SelectedGame.Game, SelectedFix, SelectedFixVariant, true, cancellationToken).ConfigureAwait(true);
                }
            }

            _cancellationTokenSource.Dispose();

            LockButtons = false;

            _progressReport.Progress.ProgressChanged -= ProgressChanged;
            _progressReport.NotifyOperationMessageChanged -= OperationMessageChanged;

            ProgressBarValue = 0;
            ProgressBarText = string.Empty;

            if (!result.IsSuccess)
            {
                _popupMessage.Show(
                    "Error",
                    result.Message,
                    PopupMessageType.OkOnly
                    );

                return;
            }

            FillGamesList();

            if (SelectedFix is FileFixEntity fileFix &&
                fileFix.ConfigFile is not null &&
                _config.OpenConfigAfterInstall)
            {
                _popupMessage.Show(
                    "Success",
                    result.Message + Environment.NewLine + Environment.NewLine + "Open config file?",
                    PopupMessageType.YesNo,
                    OpenConfigFileAsync
                    );
            }
            else
            {
                _popupMessage.Show(
                    "Success",
                    result.Message,
                    PopupMessageType.OkOnly
                    );
            }
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

            FillGamesList();

            if (!result.IsSuccess)
            {
                _popupMessage.Show(
                    "Error",
                    result.Message,
                    PopupMessageType.OkOnly
                    );
            }

            OnPropertyChanged(nameof(TagsComboboxList));

            IsInProgress = false;
            ProgressBarText = string.Empty;

            _locker.Release();
        }

        /// <summary>
        /// Update tab header
        /// </summary>
        private void UpdateHeader()
        {
            MainTabHeader = "Main" + (_mainModel.HasUpdateableGames
                ? $" ({_mainModel.UpdateableGamesCount} {(_mainModel.UpdateableGamesCount < 2
                    ? "update"
                    : "updates")})"
                : string.Empty);
        }

        /// <summary>
        /// Fill games and available games lists based on a search bar
        /// </summary>
        private void FillGamesList()
        {
            var selectedGameId = SelectedGame?.GameId;
            var selectedFixGuid = SelectedFix?.Guid;

            UpdateFilteredGamesList(_additionalFix);

            UpdateHeader();

            if (selectedGameId is not null)
            {
                SelectedGame = FilteredGamesList.FirstOrDefault(x => x.GameId == selectedGameId);

                if (selectedFixGuid is not null)
                {
                    SelectedFix = SelectedGameFixesList.FirstOrDefault(x => x.Guid == selectedFixGuid);
                }
            }
        }

        /// <summary>
        /// Open config file for selected fix
        /// </summary>
        private async void OpenConfigFileAsync()
        {
            SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);
            SelectedGame.ThrowIfNull();
            SelectedGame.Game.ThrowIfNull();
            fileFix.ConfigFile.ThrowIfNull();

            var pathToConfig = Path.Combine(SelectedGame.Game.InstallDir, fileFix.ConfigFile);

            if (fileFix.ConfigFile.EndsWith(".exe"))
            {
                Process.Start(new ProcessStartInfo
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
        private string GetRequirementsString()
        {
            if (SelectedFix is null ||
                SelectedGame is null)
            {
                return string.Empty;
            }

            var dependsOn = _mainModel.GetDependenciesForAFix(SelectedGame, SelectedFix);

            string? requires = null;

            if (dependsOn is not null && dependsOn.Count != 0)
            {
                requires = "REQUIRES: " + string.Join(", ", dependsOn.Select(static x => x.Name));
            }

            string? required = null;

            var dependedBy = _mainModel.GetDependentFixes(SelectedGameFixesList, SelectedFix.Guid);

            if (dependedBy.Count != 0)
            {
                required = "REQUIRED BY: " + string.Join(", ", dependedBy.Select(static x => x.Name));
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
                FillGamesList();
            }

            if (parameterName.Equals(nameof(_config.UseLocalApiAndRepo)) ||
                parameterName.Equals(nameof(_config.LocalRepoPath)))
            {
                await UpdateAsync().ConfigureAwait(true);
            }
        }
    }
}