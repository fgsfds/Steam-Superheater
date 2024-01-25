using Avalonia.Input.Platform;
using Common.Config;
using Common.Entities.CombinedEntities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.TextFix;
using Common.Helpers;
using Common.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Superheater.Avalonia.Core.Helpers;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class MainViewModel : ObservableObject, ISearchBarViewModel, IProgressBarViewModel
    {
        public MainViewModel(
            MainModel mainModel,
            ConfigProvider config,
            PopupMessageViewModel popupMessage,
            ProgressReport progressReport
            )
        {
            _mainModel = mainModel;
            _config = config.Config;
            _popupMessage = popupMessage;
            _progressReport = progressReport;

            _searchBarText = string.Empty;

            SelectedTagFilter = TagsComboboxList.First();

            _config.NotifyParameterChanged += NotifyParameterChanged;
        }

        private readonly MainModel _mainModel;
        private readonly ConfigEntity _config;
        private readonly PopupMessageViewModel _popupMessage;
        private readonly ProgressReport _progressReport;
        private readonly SemaphoreSlim _locker = new(1);
        private bool _lockButtons;


        #region Binding Properties

        public ImmutableList<FixFirstCombinedEntity> FilteredGamesList => _mainModel.GetFilteredGamesList(SearchBarText, SelectedTagFilter);

        public ImmutableList<BaseFixEntity> SelectedGameFixesList => SelectedGame is null ? [] : [.. SelectedGame.FixesList.Fixes.Where(static x => !x.IsHidden)];

        public ImmutableList<string> SelectedFixTags => SelectedFix?.Tags is null ? [] : [.. SelectedFix.Tags.Where(x => !_config.HiddenTags.Contains(x))];

        public HashSet<string> TagsComboboxList => _mainModel.GetListOfTags();

        public ImmutableList<string> SelectedFixVariants => SelectedFix is FileFixEntity fileFix && fileFix.Variants is not null ? [.. fileFix.Variants] : [];


        public bool IsTagsComboboxVisible => true;

        public bool IsSteamGameMode => CommonProperties.IsInSteamDeckGameMode;

        public bool DoesSelectedFixHaveVariants => !SelectedFixVariants.IsEmpty;

        public bool DoesSelectedFixHaveUpdates => SelectedFix?.IsOutdated ?? false;

        public bool SelectedFixHasTags => !SelectedFixTags.IsEmpty;

        public bool IsAdminMessageVisible => SelectedFix is HostsFixEntity && !CommonProperties.IsAdmin;


        public string SelectedFixRequirements => GetRequirementsString();

        public string SelectedFixUrl => _mainModel.GetSelectedFixUrl(SelectedFix);

        public string InstallButtonText
        {
            get
            {
                if (SelectedFix is HostsFixEntity &&
                    !CommonProperties.IsAdmin)
                {
                    return "Restart as admin...";
                }

                if (SelectedFix is FileFixEntity fileFix)
                {
                    if (fileFix.Url is null)
                    {
                        return "Install";
                    }

                    var pathToArchive = _config.UseLocalRepo
                    ? Path.Combine(_config.LocalRepoPath, "fixes", Path.GetFileName(fileFix.Url))
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(fileFix.Url));

                    if (File.Exists(pathToArchive))
                    {
                        return "Install";
                    }

                    if (fileFix.FileSize is not null)
                    {
                        return $"Download ({fileFix.FileSize.ToSizeString()}) and install";
                    }

                    return $"Download and install";
                }

                return "Install";
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


        [ObservableProperty]
        private string _mainTabHeader = "Main";

        [ObservableProperty]
        private string _launchGameButtonText = "Launch game...";

        [ObservableProperty]
        private string _progressBarText = string.Empty;

        [ObservableProperty]
        private float _progressBarValue = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedGameFixesList))]
        [NotifyCanExecuteChangedFor(nameof(LaunchGameCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenGameFolderCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenPCGamingWikiCommand))]
        private FixFirstCombinedEntity? _selectedGame;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedFixRequirements))]
        [NotifyPropertyChangedFor(nameof(DoesSelectedFixHaveUpdates))]
        [NotifyPropertyChangedFor(nameof(SelectedFixVariants))]
        [NotifyPropertyChangedFor(nameof(DoesSelectedFixHaveVariants))]
        [NotifyPropertyChangedFor(nameof(SelectedFixUrl))]
        [NotifyPropertyChangedFor(nameof(SelectedFixTags))]
        [NotifyPropertyChangedFor(nameof(SelectedFixHasTags))]
        [NotifyPropertyChangedFor(nameof(InstallButtonText))]
        [NotifyPropertyChangedFor(nameof(IsAdminMessageVisible))]
        [NotifyPropertyChangedFor(nameof(SelectedFixDescription))]
        [NotifyCanExecuteChangedFor(nameof(InstallFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(UninstallFixCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenConfigCommand))]
        [NotifyCanExecuteChangedFor(nameof(UpdateFixCommand))]
        private BaseFixEntity? _selectedFix;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(InstallFixCommand))]
        private string? _selectedFixVariant;

        [ObservableProperty]
        private string _selectedTagFilter;
        partial void OnSelectedTagFilterChanged(string value) => FillGamesList();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ClearSearchCommand))]
        private string _searchBarText;
        partial void OnSearchBarTextChanged(string value) => FillGamesList();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdateGamesCommand))]
        private bool _isInProgress;
        partial void OnIsInProgressChanged(bool value) => _lockButtons = value;

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
        [RelayCommand(CanExecute = (nameof(UpdateGamesCanExecute)))]
        private async Task UpdateGamesAsync()
        {
            await UpdateAsync(false);

            InstallFixCommand.NotifyCanExecuteChanged();
            UninstallFixCommand.NotifyCanExecuteChanged();
            UpdateFixCommand.NotifyCanExecuteChanged();
        }
        private bool UpdateGamesCanExecute() => !_lockButtons;


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
                _lockButtons)
            {
                return false;
            }

            var result = !_mainModel.DoesFixHaveNotInstalledDependencies(SelectedGame, SelectedFix);

            return result;
        }


        /// <summary>
        /// Update selected fix
        /// </summary>
        [RelayCommand(CanExecute = (nameof(UpdateFixCanExecute)))]
        private Task UpdateFixAsync() => InstallUpdateFixAsync(true);
        public bool UpdateFixCanExecute() => (SelectedGame is not null && SelectedGame.IsGameInstalled) || !_lockButtons;


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

            UpdateGamesCommand.NotifyCanExecuteChanged();

            var fixUninstallResult = _mainModel.UninstallFix(SelectedGame.Game, SelectedFix);

            FillGamesList();

            IsInProgress = false;

            InstallFixCommand.NotifyCanExecuteChanged();
            UninstallFixCommand.NotifyCanExecuteChanged();
            OpenConfigCommand.NotifyCanExecuteChanged();
            UpdateGamesCommand.NotifyCanExecuteChanged();

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
                _lockButtons)
            {
                return false;
            }

            var result = !_mainModel.DoesFixHaveInstalledDependentFixes(SelectedGameFixesList, SelectedFix.Guid);

            return result;
        }


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
        /// Clear search bar
        /// </summary>
        [RelayCommand(CanExecute = (nameof(ClearSearchCanExecute)))]
        private void ClearSearch() => SearchBarText = string.Empty;
        private bool ClearSearchCanExecute() => !string.IsNullOrEmpty(SearchBarText);


        /// <summary>
        /// Open config file for selected fix
        /// </summary>
        [RelayCommand(CanExecute = (nameof(OpenConfigCanExecute)))]
        private void OpenConfig() => OpenConfigXml();
        private bool OpenConfigCanExecute() => SelectedFix is FileFixEntity fileFix && fileFix.ConfigFile is not null && fileFix.IsInstalled && SelectedGame is not null && SelectedGame.IsGameInstalled;


        /// <summary>
        /// Open PCGW page for selected game
        /// </summary>
        [RelayCommand(CanExecute = (nameof(OpenPCGamingWikiCanExecute)))]
        private void OpenPCGamingWiki()
        {
            SelectedGame.ThrowIfNull();

            Process.Start(new ProcessStartInfo
            {
                FileName = Consts.PCGamingWikiUrl + SelectedGame.GameId,
                UseShellExecute = true
            });
        }
        private bool OpenPCGamingWikiCanExecute() => SelectedGame is not null;


        /// <summary>
        /// Copy file URL to clipboard
        /// </summary>
        [RelayCommand]
        private Task UrlCopyToClipboardAsync()
        {
            var clipboard = Properties.TopLevel.Clipboard ?? ThrowHelper.ArgumentNullException<IClipboard>("Error while getting clipboard implementation");
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

            LaunchGameButtonText = SelectedGame.IsGameInstalled
                ? "Launch game..."
                : "Install game...";
 
            return true;
        }


        /// <summary>
        /// Close app
        /// </summary>
        [RelayCommand]
        private void CloseApp() => Properties.MainWindow.Close();


        /// <summary>
        /// Hide selected tag
        /// </summary>
        [RelayCommand]
        private void HideTag(string tag) => _mainModel.HideTag(tag);

        #endregion Relay Commands


        /// <summary>
        /// Install or update fix
        /// </summary>
        /// <param name="isUpdate">Update fix</param>
        private async Task InstallUpdateFixAsync(bool isUpdate)
        {
            SelectedFix.ThrowIfNull();
            SelectedGame.ThrowIfNull();
            SelectedGame.Game.ThrowIfNull();

            _lockButtons = true;

            UpdateGamesCommand.NotifyCanExecuteChanged();
            InstallFixCommand.NotifyCanExecuteChanged();
            UpdateFixCommand.NotifyCanExecuteChanged();
            UninstallFixCommand.NotifyCanExecuteChanged();
            OpenConfigCommand.NotifyCanExecuteChanged();

            _progressReport.Progress.ProgressChanged += ProgressChanged;
            _progressReport.NotifyOperationMessageChanged += OperationMessageChanged;

            Result result;

            if (isUpdate)
            {
                result = await _mainModel.UpdateFixAsync(SelectedGame.Game, SelectedFix, SelectedFixVariant, false);
            }
            else
            {
                result = await _mainModel.InstallFixAsync(SelectedGame.Game, SelectedFix, SelectedFixVariant, false);
            }

            if (result == ResultEnum.MD5Error)
            {
                var popupResult = await _popupMessage.ShowAndGetResultAsync(
                    "Warning",
                    @"MD5 of the file doesn't match the database. This file wasn't verified by the maintainer.

Do you still want to install the fix?",
                    PopupMessageType.YesNo
                    );

                if (popupResult)
                {
                    result = await _mainModel.InstallFixAsync(SelectedGame.Game, SelectedFix, SelectedFixVariant, true);
                }
            }

            FillGamesList();

            _lockButtons = false;

            UninstallFixCommand.NotifyCanExecuteChanged();
            OpenConfigCommand.NotifyCanExecuteChanged();
            UpdateGamesCommand.NotifyCanExecuteChanged();

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

            if (SelectedFix is FileFixEntity fileFix &&
                fileFix.ConfigFile is not null &&
                _config.OpenConfigAfterInstall)
            {
                _popupMessage.Show(
                    "Success",
                    result.Message + Environment.NewLine + Environment.NewLine + "Open config file?",
                    PopupMessageType.YesNo,
                    OpenConfigXml
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
        /// <param name="useCache">Use cached list</param>
        private async Task UpdateAsync(bool useCache)
        {
            await _locker.WaitAsync();
            IsInProgress = true;
            ProgressBarText = "Updating...";

            var result = await _mainModel.UpdateGamesListAsync(useCache);

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

            OnPropertyChanged(nameof(FilteredGamesList));

            UpdateHeader();

            if (selectedGameId is not null && FilteredGamesList.Exists(x => x.GameId == selectedGameId))
            {

                SelectedGame = FilteredGamesList.First(x => x.GameId == selectedGameId);

                if (selectedFixGuid is not null &&
                    SelectedGameFixesList.Exists(x => x.Guid == selectedFixGuid))
                {
                    SelectedFix = SelectedGameFixesList.First(x => x.Guid == selectedFixGuid);
                }
            }
        }

        /// <summary>
        /// Open config file for selected fix
        /// </summary>
        private void OpenConfigXml()
        {
            SelectedFix.ThrowIfNotType<FileFixEntity>(out var fileFix);
            SelectedGame.ThrowIfNull();
            SelectedGame.Game.ThrowIfNull();
            fileFix.ConfigFile.ThrowIfNull();

            var pathToConfig = Path.Combine(SelectedGame.Game.InstallDir, fileFix.ConfigFile);

            var workingDir = fileFix.ConfigFile.EndsWith(".exe") ? Path.GetDirectoryName(pathToConfig) : Directory.GetCurrentDirectory();

            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(pathToConfig),
                UseShellExecute = true,
                WorkingDirectory = workingDir
            });
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

            if (dependsOn.Count != 0)
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

        private async void NotifyParameterChanged(string parameterName)
        {
            if (parameterName.Equals(nameof(_config.ShowUninstalledGames)))
            {
                await UpdateAsync(true);
            }

            if (parameterName.Equals(nameof(_config.ShowUnsupportedFixes)) ||
                parameterName.Equals(nameof(_config.HiddenTags)))
            {
                await UpdateAsync(true);
                OnPropertyChanged(nameof(SelectedGameFixesList));
                OnPropertyChanged(nameof(SelectedFixTags));
            }

            if (parameterName.Equals(nameof(_config.UseTestRepoBranch)) ||
                parameterName.Equals(nameof(_config.UseLocalRepo)) ||
                parameterName.Equals(nameof(_config.LocalRepoPath)))
            {
                await UpdateAsync(false);
            }
        }
    }
}