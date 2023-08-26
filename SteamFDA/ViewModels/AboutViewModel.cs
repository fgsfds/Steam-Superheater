using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamFDCommon.Updater;
using System.Reflection;

namespace SteamFDA.ViewModels
{
    public class AboutViewModel : ObservableObject
    {
        public bool IsUpdateAvailable { get; set; }

        public bool IsInProgress { get; set; }

        public IRelayCommand CheckForUpdatesCommand { get; set; }

        public AboutViewModel()
        {
            IsUpdateAvailable = false;
            IsInProgress = false;

            CheckForUpdatesCommand = new RelayCommand(async () =>
            {
                IsInProgress = true;
                await GithubReleasesProvider.ReadReleasesAsync(Assembly.GetEntryAssembly().GetName().Version);
                OnPropertyChanged(nameof(IsInProgress));
                CheckForUpdatesCommand.NotifyCanExecuteChanged();
            },
            () => IsInProgress is false
                    );
        }

        private void CheckForUpdates()
        {

        }
    }
}