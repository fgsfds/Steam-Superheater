namespace SteamFDCommon.Config
{
    public class ConfigEntity
    {
        public delegate void ConfigParameterChanged();
        public event ConfigParameterChanged Notify;

        private bool _deleteZipsAfterInstall;
        public bool DeleteZipsAfterInstall
        {
            get => _deleteZipsAfterInstall;
            set
            {
                _deleteZipsAfterInstall = value;
                Notify?.Invoke();
            }
        }

        private bool _openConfigAfterInstall;
        public bool OpenConfigAfterInstall
        {
            get => _openConfigAfterInstall;
            set
            {
                _openConfigAfterInstall = value;
                Notify?.Invoke();
            }
        }

        private bool _showEditorTab;
        public bool ShowEditorTab
        {
            get => _showEditorTab;
            set
            {
                _showEditorTab = value;
                Notify?.Invoke();
            }
        }

        private int _lastReadNewsVersion;
        public int LastReadNewsVersion
        {
            get => _lastReadNewsVersion;
            set
            {
                _lastReadNewsVersion = value;
                Notify?.Invoke();
            }
        }

        private bool _useLocalRepo;
        public bool UseLocalRepo
        {
            get => _useLocalRepo;
            set
            {
                _useLocalRepo = value;
                Notify?.Invoke();
            }
        }

        private string _localRepoPath;
        public string LocalRepoPath
        {
            get => _localRepoPath;
            set
            {
                _localRepoPath = value;
                Notify?.Invoke();
            }
        }

        internal ConfigEntity()
        {
            _deleteZipsAfterInstall = true;
            _openConfigAfterInstall = false;
            _showEditorTab = false;
            _lastReadNewsVersion = 0;
            _useLocalRepo = false;
            _localRepoPath = "LocalRepo";
        }
    }
}
