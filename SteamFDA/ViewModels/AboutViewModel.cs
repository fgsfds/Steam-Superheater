using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamFDCommon.Models;
using SteamFDCommon.Updater;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SteamFDA.ViewModels
{
    public class AboutViewModel : ObservableObject
    {
        private readonly AboutModel _aboutModel;

        public bool IsUpdateAvailable { get; set; }

        public bool IsInProgress { get; set; }

        public IRelayCommand CheckForUpdatesCommand { get; set; }

        public Version CurrentVersion { get; set; }

        public AboutViewModel(AboutModel aboutModel)
        {
            _aboutModel = aboutModel ?? throw new NullReferenceException(nameof(aboutModel));

            CurrentVersion = Assembly.GetEntryAssembly().GetName().Version;

            IsUpdateAvailable = false;
            IsInProgress = false;

            CheckForUpdatesCommand = new RelayCommand(async () =>
            {
                IsInProgress = true;
                OnPropertyChanged(nameof(IsInProgress));
                CheckForUpdatesCommand.NotifyCanExecuteChanged();

                var updates = await _aboutModel.CheckForUpdates(CurrentVersion);

                if (updates)
                {
                    IsUpdateAvailable = true;
                    OnPropertyChanged(nameof(IsUpdateAvailable));
                }

                IsInProgress = false;
                OnPropertyChanged(nameof(IsInProgress));
                CheckForUpdatesCommand.NotifyCanExecuteChanged();
            },
            () => IsInProgress is false
            );
        }
    }
}