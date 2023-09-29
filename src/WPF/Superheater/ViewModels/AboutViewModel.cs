using CommunityToolkit.Mvvm.ComponentModel;
using Common.Helpers;
using System;

namespace Superheater.ViewModels
{
    internal sealed partial class AboutViewModel : ObservableObject
    {
        public Version CurrentVersion => CommonProperties.CurrentVersion;
    }
}