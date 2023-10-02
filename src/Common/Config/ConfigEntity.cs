namespace Common.Config
{
    public sealed class ConfigEntity
    {
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

        private int _lastReadNewsVersion;
        public int LastReadNewsVersion
        {
            get => _lastReadNewsVersion;
            set
            {
                if (_lastReadNewsVersion != value)
                {
                    _lastReadNewsVersion = value;
                    NotifyConfigChanged?.Invoke();
                    NotifyParameterChanged?.Invoke(nameof(LastReadNewsVersion));
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

        private string _theme;
        public string Theme
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

        private int _installedUpdater;
        public int InstalledUpdater
        {
            get => _installedUpdater;
            set
            {
                if (_installedUpdater != value)
                {
                    _installedUpdater = value;
                    NotifyConfigChanged?.Invoke();
                    NotifyParameterChanged?.Invoke(nameof(InstalledUpdater));
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

        internal ConfigEntity()
        {
            _deleteZipsAfterInstall = true;
            _openConfigAfterInstall = false;
            _lastReadNewsVersion = 0;
            _useLocalRepo = false;
            _localRepoPath = "LocalRepo";
            _theme = "System";
            _installedUpdater = 0;
            _showUninstalledGames = true;
        }
    }
}
