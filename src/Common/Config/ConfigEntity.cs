using Common.Enums;
using Common.Helpers;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Common.Config
{
    public sealed class ConfigEntity
    {
        public ConfigEntity()
        {
            _deleteZipsAfterInstall = true;
            _openConfigAfterInstall = false;
            _lastReadNewsDate = DateTime.MinValue;
            _useLocalApiAndRepo = false;
            _localRepoPath = "LocalRepo";
            _theme = ThemeEnum.System;
            _hiddenTags = [];
            _showUninstalledGames = true;
            _showUnsupportedFixes = false;
            _upvotes = new();
        }

        public delegate void ConfigChanged();
        public event ConfigChanged NotifyConfigChanged;

        public delegate void ParameterChanged(string parameterName);
        public event ParameterChanged NotifyParameterChanged;

        private bool _deleteZipsAfterInstall;
        public bool DeleteZipsAfterInstall
        {
            get => _deleteZipsAfterInstall;
            set => SetConfigParameter(ref _deleteZipsAfterInstall, value);
        }

        private bool _openConfigAfterInstall;
        public bool OpenConfigAfterInstall
        {
            get => _openConfigAfterInstall;
            set => SetConfigParameter(ref _openConfigAfterInstall, value);
        }

        private DateTime _lastReadNewsDate;
        public DateTime LastReadNewsDate
        {
            get => _lastReadNewsDate;
            set => SetConfigParameter(ref _lastReadNewsDate, value);
        }

        private bool _useLocalApiAndRepo;
        public bool UseLocalApiAndRepo
        {
            get => _useLocalApiAndRepo;
            set => SetConfigParameter(ref _useLocalApiAndRepo, value);
        }

        private string _localRepoPath;
        public string LocalRepoPath
        {
            get => _localRepoPath;
            set => SetConfigParameter(ref _localRepoPath, value);
        }

        private ThemeEnum _theme;
        public ThemeEnum Theme
        {
            get => _theme;
            set => SetConfigParameter(ref _theme, value);
        }

        private bool _showUninstalledGames;
        public bool ShowUninstalledGames
        {
            get => _showUninstalledGames;
            set => SetConfigParameter(ref _showUninstalledGames, value);
        }

        private bool _showUnsupportedFixes;
        public bool ShowUnsupportedFixes
        {
            get => _showUnsupportedFixes;
            set => SetConfigParameter(ref _showUnsupportedFixes, value);
        }

        private List<string> _hiddenTags;
        public List<string> HiddenTags
        {
            get => _hiddenTags;
            set => SetConfigParameter(ref _hiddenTags, value);
        }

        private Dictionary<Guid, bool> _upvotes;
        public Dictionary<Guid, bool> Upvotes
        {
            get => _upvotes;
            set => SetConfigParameter(ref _upvotes, value);
        }

        public void ForceUpdateConfig()
        {
            NotifyConfigChanged?.Invoke();
        }

        /// <summary>
        /// Sets config parameter if changed and invokes notifier
        /// </summary>
        /// <param name="fieldName">Parameter field to change</param>
        /// <param name="value">New value</param>
        private void SetConfigParameter<T>(ref T fieldName, T value, [CallerMemberName] string callerName = "")
        {
            fieldName.ThrowIfNull();
            callerName.ThrowIfNullOrEmpty();

            if (!fieldName.Equals(value))
            {
                fieldName = value;
                NotifyConfigChanged?.Invoke();
                NotifyParameterChanged?.Invoke(callerName);
            }
        }
    }

    [JsonSourceGenerationOptions(
        WriteIndented = true,
        Converters = [typeof(JsonStringEnumConverter<ThemeEnum>)]
    )]
    [JsonSerializable(typeof(ConfigEntity))]
    internal sealed partial class ConfigEntityContext : JsonSerializerContext;
}
