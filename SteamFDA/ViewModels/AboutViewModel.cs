using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamFDCommon.Models;
using System;
using System.Diagnostics;
using System.Reflection;

namespace SteamFDA.ViewModels
{
    public class AboutViewModel : ObservableObject
    {
        private readonly AboutModel _aboutModel;

        public bool IsUpdateAvailable { get; set; }

        public bool IsInProgress { get; set; }

        public Version CurrentVersion { get; set; }

        public AboutViewModel(AboutModel aboutModel)
        {
            _aboutModel = aboutModel ?? throw new NullReferenceException(nameof(aboutModel));

            CurrentVersion = Assembly.GetEntryAssembly().GetName().Version;

            SetRelayCommands();
        }

        public IRelayCommand CheckForUpdatesCommand { get; set; }

        public IRelayCommand DownloadAndInstall { get; set; }

        private void SetRelayCommands()
        {
            CheckForUpdatesCommand = new RelayCommand(async () =>
            {
                Process.Start("Updater.exe");
                var mainWindows = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;
                mainWindows.Close();



                //IsInProgress = true;
                //OnPropertyChanged(nameof(IsInProgress));
                //CheckForUpdatesCommand.NotifyCanExecuteChanged();

                //var updates = await _aboutModel.CheckForUpdates(CurrentVersion);

                //if (updates)
                //{
                //    IsUpdateAvailable = true;
                //    OnPropertyChanged(nameof(IsUpdateAvailable));
                //    DownloadAndInstall.NotifyCanExecuteChanged();
                //}

                //IsInProgress = false;
                //OnPropertyChanged(nameof(IsInProgress));
                //CheckForUpdatesCommand.NotifyCanExecuteChanged();
            },
            () => IsInProgress is false
            );

            DownloadAndInstall = new RelayCommand(async () =>
            {
                IsInProgress = true;
                OnPropertyChanged(nameof(IsInProgress));
                DownloadAndInstall.NotifyCanExecuteChanged();

                await _aboutModel.DownloadLatestReleaseAndCreateLock();

                IsInProgress = false;
                OnPropertyChanged(nameof(IsInProgress));
                DownloadAndInstall.NotifyCanExecuteChanged();
            },
            () => IsUpdateAvailable is true
            );
        }
    }
}