using CommunityToolkit.Mvvm.ComponentModel;
using Common.Helpers;

namespace Superheater.ViewModels
{
    internal sealed partial class AboutViewModel : ObservableObject
    {
        public Version CurrentVersion => CommonProperties.CurrentVersion;
    }
}