using Common.Enums;
using Common.Helpers;
using Database.Client;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Common.Client.Config
{
    public sealed class ConfigProvider : IConfigProvider
    {
        private readonly DatabaseContext _dbContext;

        public delegate void ParameterChanged(string parameterName);
        public event ParameterChanged ParameterChangedEvent;

        public ConfigProvider(DatabaseContextFactory dbContextFactory)
        {
            _dbContext = dbContextFactory.Get();

            ConvertOldConfig();
        }

        public ThemeEnum Theme
        {
            get => Enum.TryParse<ThemeEnum>(_dbContext.Settings.Find([nameof(Theme)])?.Value, out var result) ? result : ThemeEnum.System;
            set => SetSettingsValue(value.ToString());
        }

        public bool ShowUninstalledGames
        {
            get => bool.TryParse(_dbContext.Settings.Find([nameof(ShowUninstalledGames)])?.Value, out var result) && result;
            set => SetSettingsValue(value.ToString());
        }

        public bool ShowUnsupportedFixes
        {
            get => bool.TryParse(_dbContext.Settings.Find([nameof(ShowUnsupportedFixes)])?.Value, out var result) && result;
            set => SetSettingsValue(value.ToString());
        }

        public bool DeleteZipsAfterInstall
        {
            get => bool.TryParse(_dbContext.Settings.Find([nameof(DeleteZipsAfterInstall)])?.Value, out var result) && result;
            set => SetSettingsValue(value.ToString());
        }

        public bool OpenConfigAfterInstall
        {
            get => bool.TryParse(_dbContext.Settings.Find([nameof(OpenConfigAfterInstall)])?.Value, out var result) && result;
            set => SetSettingsValue(value.ToString());
        }

        public bool UseLocalApiAndRepo
        {
            get => bool.TryParse(_dbContext.Settings.Find([nameof(UseLocalApiAndRepo)])?.Value, out var result) && result;
            set => SetSettingsValue(value.ToString());
        }

        public string LocalRepoPath
        {
            get => _dbContext.Settings.Find([nameof(LocalRepoPath)])?.Value ?? string.Empty;
            set => SetSettingsValue(value);
        }

        public string ApiPassword
        {
            get => _dbContext.Settings.Find([nameof(ApiPassword)])?.Value ?? string.Empty;
            set => SetSettingsValue(value);
        }

        public DateTime LastReadNewsDate
        {
            get => DateTime.TryParse(_dbContext.Settings.Find([nameof(LastReadNewsDate)])?.Value, out var time) ? time : DateTime.MinValue;
            set => SetSettingsValue(value.ToUniversalTime().ToString());
        }

        public Dictionary<Guid, bool> Upvotes
        {
            get => _dbContext.Upvotes.ToDictionary(x => x.FixGuid, x => x.IsUpvoted);
        }

        public void ChangeFixUpvoteState(Guid fixGuid, bool needToUpvote)
        {
            var existing = _dbContext.Upvotes.Find([fixGuid]);

            if (existing is not null)
            {
                if (existing.IsUpvoted && needToUpvote)
                {
                    _dbContext.Upvotes.Remove(existing);
                }
                else if (existing.IsUpvoted && !needToUpvote)
                {
                    existing.IsUpvoted = false;
                }
                else if (!existing.IsUpvoted && needToUpvote)
                {
                    existing.IsUpvoted = true;
                }
                else if (!existing.IsUpvoted && !needToUpvote)
                {
                    _dbContext.Upvotes.Remove(existing);
                }
            }
            else
            {
                _dbContext.Upvotes.Add(new() { FixGuid = fixGuid, IsUpvoted = needToUpvote });
            }

            _dbContext.SaveChanges();
            ParameterChangedEvent?.Invoke(nameof(Upvotes));
        }

        public HashSet<string> HiddenTags
        {
            get => [.. _dbContext.HiddenTags.Select(x => x.Tag)];
        }

        public void ChangeTagState(string tag, bool needToHide)
        {
            var existing = _dbContext.HiddenTags.Find([tag]);

            if (existing is not null)
            {
                if (needToHide)
                {
                    return;
                }
                else
                {
                    _dbContext.HiddenTags.Remove(existing);
                }
            }
            else
            {
                if (needToHide)
                {
                    _dbContext.HiddenTags.Add(new() { Tag = tag });
                }
                else
                {
                    return;
                }
            }

            _dbContext.SaveChanges();
            ParameterChangedEvent?.Invoke(nameof(HiddenTags));
        }



        private void SetSettingsValue(string value, [CallerMemberName] string caller = "")
        {
            var setting = _dbContext.Settings.Find([caller]);

            if (setting is null)
            {
                _dbContext.Settings.Add(new() { Name = caller, Value = value });
            }
            else
            {
                setting.Value = value;
            }

            _dbContext.SaveChanges();
            ParameterChangedEvent?.Invoke(caller);
        }



        [Obsolete]
        private void ConvertOldConfig()
        {
            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "config.db")) ||
                !File.Exists(Path.Combine(Environment.CurrentDirectory, Consts.ConfigFile)))
            {
                return;
            }

            using (FileStream fs = new(Consts.ConfigFile, FileMode.Open))
            {
                var config = JsonSerializer.Deserialize(fs, ConfigEntityContext.Default.ConfigEntityObsolete);

                if (config is null)
                {
                    return;
                }

                _dbContext.Settings.Add(new() { Name = nameof(config.Theme), Value = config.Theme.ToString() });
                _dbContext.Settings.Add(new() { Name = nameof(config.DeleteZipsAfterInstall), Value = config.DeleteZipsAfterInstall.ToString() });
                _dbContext.Settings.Add(new() { Name = nameof(config.OpenConfigAfterInstall), Value = config.OpenConfigAfterInstall.ToString() });
                _dbContext.Settings.Add(new() { Name = nameof(config.UseLocalApiAndRepo), Value = config.UseLocalApiAndRepo.ToString() });
                _dbContext.Settings.Add(new() { Name = nameof(config.ShowUninstalledGames), Value = config.ShowUninstalledGames.ToString() });
                _dbContext.Settings.Add(new() { Name = nameof(config.ShowUnsupportedFixes), Value = config.ShowUnsupportedFixes.ToString() });
                _dbContext.Settings.Add(new() { Name = nameof(config.LocalRepoPath), Value = config.LocalRepoPath ?? string.Empty });
                _dbContext.Settings.Add(new() { Name = nameof(config.ApiPassword), Value = config.ApiPassword ?? string.Empty });
                _dbContext.Settings.Add(new() { Name = nameof(config.LastReadNewsDate), Value = config.LastReadNewsDate.ToUniversalTime().ToString() });

                if (config.Upvotes is not null)
                {
                    foreach (var addon in config.Upvotes)
                    {
                        _dbContext.Upvotes.Add(new() { FixGuid = addon.Key, IsUpvoted = addon.Value });
                    }
                }

                if (config.HiddenTags is not null)
                {
                    foreach (var tag in config.HiddenTags)
                    {
                        _dbContext.HiddenTags.Add(new() { Tag = tag });
                    }
                }

                _dbContext.SaveChanges();
            }

            File.Delete(Path.Combine(Environment.CurrentDirectory, Consts.ConfigFile));
        }
    }
}
