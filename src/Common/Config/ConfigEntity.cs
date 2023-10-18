using Common.Helpers;

namespace Common.Config
{
    public sealed class ConfigEntity
    {
        internal ConfigEntity()
        {
            _deleteZipsAfterInstall = true;
            _openConfigAfterInstall = false;
            _lastReadNewsDate = DateTime.MinValue;
            _useLocalRepo = false;
            _localRepoPath = "LocalRepo";
            _theme = ThemeEnum.System;
            _hiddenTags = new();
            _showUninstalledGames = true;
            _useTestRepoBranch = false;
            _showUnsupportedFixes = false;
        }

        public delegate void ConfigChanged();
        public event ConfigChanged NotifyConfigChanged;

        public delegate void ParameterChanged(string parameterName);
        public event ParameterChanged NotifyParameterChanged;

        private bool _deleteZipsAfterInstall;
        public bool DeleteZipsAfterInstall
        {
            get => _deleteZipsAfterInstall;
            set
            {
                if (_deleteZipsAfterInstall != value)
                {
                    _deleteZipsAfterInstall = value;
                    NotifyConfigChanged?.Invoke();
                    NotifyParameterChanged?.Invoke(nameof(DeleteZipsAfterInstall));
                }
            }
        }

        private bool _openConfigAfterInstall;
        public bool OpenConfigAfterInstall
        {
            get => _openConfigAfterInstall;
            set
            {
                if (_openConfigAfterInstall != value)
                {
                    _openConfigAfterInstall = value;
                    NotifyConfigChanged?.Invoke();
                    NotifyParameterChanged?.Invoke(nameof(OpenConfigAfterInstall));
                }
            }
        }

        private DateTime _lastReadNewsDate;
        public DateTime LastReadNewsDate
        {
            get => _lastReadNewsDate;
            set
            {
                if (_lastReadNewsDate != value)
                {
                    _lastReadNewsDate = value;
                    NotifyConfigChanged?.Invoke();
                    NotifyParameterChanged?.Invoke(nameof(LastReadNewsDate));
                }
            }
        }

        private bool _useLocalRepo;
        public bool UseLocalRepo
        {
            get => _useLocalRepo;
            set
            {
                if (_useLocalRepo != value)
                {
                    _useLocalRepo = value;
                    NotifyConfigChanged?.Invoke();
                    NotifyParameterChanged?.Invoke(nameof(UseLocalRepo));
                }
            }
        }

        private string _localRepoPath;
        public string LocalRepoPath
        {
            get => _localRepoPath;
            set
            {
                if (_localRepoPath != value)
                {
                    _localRepoPath = value;
                    NotifyConfigChanged?.Invoke();
                    NotifyParameterChanged?.Invoke(nameof(LocalRepoPath));
                }
            }
        }

        private ThemeEnum _theme;
        public ThemeEnum Theme
        {
            get => _theme;
            set
            {
                if (_theme != value)
                {
                    _theme = value;
                    NotifyConfigChanged?.Invoke();
                    NotifyParameterChanged?.Invoke(nameof(Theme));
                }
            }
        }

        private bool _showUninstalledGames;
        public bool ShowUninstalledGames
        {
            get => _showUninstalledGames;
            set
            {
                if (_showUninstalledGames != value)
                {
                    _showUninstalledGames = value;
                    NotifyConfigChanged?.Invoke();
                    NotifyParameterChanged?.Invoke(nameof(ShowUninstalledGames));
                }
            }
        }

        private bool _useTestRepoBranch;
        public bool UseTestRepoBranch
        {
            get => _useTestRepoBranch;
            set
            {
                if (_useTestRepoBranch != value)
                {
                    _useTestRepoBranch = value;
                    NotifyConfigChanged?.Invoke();
                    NotifyParameterChanged?.Invoke(nameof(UseTestRepoBranch));
                }
            }
        }

        private bool _showUnsupportedFixes;
        public bool ShowUnsupportedFixes
        {
            get => _showUnsupportedFixes;
            set
            {
                if (_showUnsupportedFixes != value)
                {
                    _showUnsupportedFixes = value;
                    NotifyConfigChanged?.Invoke();
                    NotifyParameterChanged?.Invoke(nameof(ShowUnsupportedFixes));
                }
            }
        }

        private List<string> _hiddenTags;
        public List<string> HiddenTags
        {
            get => _hiddenTags;
            set
            {
                if (_hiddenTags != value)
                {
                    _hiddenTags = value;
                    NotifyConfigChanged?.Invoke();
                    NotifyParameterChanged?.Invoke(nameof(HiddenTags));
                }
            }
        }
    }
}
