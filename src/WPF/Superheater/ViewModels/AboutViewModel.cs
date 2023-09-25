using CommunityToolkit.Mvvm.ComponentModel;
using SteamFDCommon.Helpers;
using System;

namespace SteamFD.ViewModels
{
    internal partial class AboutViewModel : ObservableObject
    {
        public Version CurrentVersion => CommonProperties.CurrentVersion;
    }
}