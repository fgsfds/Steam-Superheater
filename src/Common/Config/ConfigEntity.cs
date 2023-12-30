using Common.Enums;
using Generators;

namespace Common.Config
{
    public sealed partial class ConfigEntity
    {
        public ConfigEntity()
        {
            _deleteZipsAfterInstall = true;
            _openConfigAfterInstall = false;
            _lastReadNewsDate = DateTime.MinValue;
            _useLocalRepo = false;
            _localRepoPath = "LocalRepo";
            _theme = ThemeEnum.System;
            _hiddenTags = [];
            _showUninstalledGames = true;
            _useTestRepoBranch = false;
            _showUnsupportedFixes = false;
        }

        public delegate void ConfigChanged();
        public event ConfigChanged NotifyConfigChanged;

        public delegate void ParameterChanged(string parameterName);
        public event ParameterChanged NotifyParameterChanged;

        [ConfigParameter]
        private bool _deleteZipsAfterInstall;

        [ConfigParameter]
        private bool _openConfigAfterInstall;

        [ConfigParameter]
        private DateTime _lastReadNewsDate;

        [ConfigParameter]
        private bool _useLocalRepo;

        [ConfigParameter]
        private string _localRepoPath;

        [ConfigParameter]
        private ThemeEnum _theme;

        [ConfigParameter]
        private bool _showUninstalledGames;

        [ConfigParameter]
        private bool _useTestRepoBranch;

        [ConfigParameter]
        private bool _showUnsupportedFixes;

        [ConfigParameter]
        private List<string> _hiddenTags;
    }
}
