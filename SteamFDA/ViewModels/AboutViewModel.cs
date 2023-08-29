using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamFDCommon;
using SteamFDCommon.Models;
using SteamFDCommon.Updater;
using System;
using System.Diagnostics;
using System.Reflection;

namespace SteamFDA.ViewModels
{
    public class AboutViewModel : ObservableObject
    {
        private readonly UpdateInstaller _updateInstaller;

        public bool IsUpdateAvailable { get; set; }

        public bool IsInProgress { get; set; }

        public bool IsReadyToInstall { get; set; }

        public Version CurrentVersion { get; set; }

        public AboutViewModel(UpdateInstaller updateInstaller)
        {
            _updateInstaller = updateInstaller ?? throw new NullReferenceException(nameof(updateInstaller));

            CurrentVersion = Assembly.GetEntryAssembly().GetName().Version;

            SetRelayCommands();
        }

        public IRelayCommand CheckForUpdatesCommand { get; set; }

        public IRelayCommand DownloadAndInstall { get; set; }

        private void SetRelayCommands()
        {
            CheckForUpdatesCommand = new RelayCommand(async () =>
            {
                IsInProgress = true;
                OnPropertyChanged(nameof(IsInProgress));
                CheckForUpdatesCommand.NotifyCanExecuteChanged();

                var updates = await _updateInstaller.CheckForUpdates(CurrentVersion);

                if (updates)
                {
                    IsUpdateAvailable = true;
                    OnPropertyChanged(nameof(IsUpdateAvailable));
                    DownloadAndInstall.NotifyCanExecuteChanged();
                }

                IsInProgress = false;
                OnPropertyChanged(nameof(IsInProgress));
                CheckForUpdatesCommand.NotifyCanExecuteChanged();
            },
            () => IsInProgress is false
            );

            DownloadAndInstall = new RelayCommand(async () =>
            {
                IsInProgress = true;
                OnPropertyChanged(nameof(IsInProgress));
                DownloadAndInstall.NotifyCanExecuteChanged();

                await _updateInstaller.DownloadLatestReleaseAndCreateLock();

                _updateInstaller.InstallUpdate();

                var mainWindows = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;
                mainWindows.Close();
            },
            () => IsUpdateAvailable is true
            );
        }
    }
}