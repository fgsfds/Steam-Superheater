using Common.Enums;
using Database.Client;
using System.Runtime.CompilerServices;
using static Common.IConfigProvider;

namespace Common.Client.Config;

public sealed class ConfigProvider : IConfigProvider
{
    private readonly DatabaseContextFactory _dbContextFactory;

    public event ParameterChanged ParameterChangedEvent;

    public ConfigProvider(DatabaseContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public ThemeEnum Theme
    {
        get
        {
            using var dbContext = _dbContextFactory.Get();

            return Enum.TryParse<ThemeEnum>(dbContext.Settings.Find([nameof(Theme)])?.Value, out var result) ? result : ThemeEnum.System;
        }

        set => SetSettingsValue(value.ToString());
    }

    public bool ShowUninstalledGames
    {
        get
        {
            using var dbContext = _dbContextFactory.Get();

            return bool.TryParse(dbContext.Settings.Find([nameof(ShowUninstalledGames)])?.Value, out var result) ? result : true;
        }

        set => SetSettingsValue(value.ToString());
    }

    public bool ShowUnsupportedFixes
    {
        get
        {
            using var dbContext = _dbContextFactory.Get();

            return bool.TryParse(dbContext.Settings.Find([nameof(ShowUnsupportedFixes)])?.Value, out var result) && result;
        }

        set => SetSettingsValue(value.ToString());
    }

    public bool DeleteZipsAfterInstall
    {
        get
        {
            using var dbContext = _dbContextFactory.Get();

            return bool.TryParse(dbContext.Settings.Find([nameof(DeleteZipsAfterInstall)])?.Value, out var result) && result;
        }

        set => SetSettingsValue(value.ToString());
    }

    public bool OpenConfigAfterInstall
    {
        get
        {
            using var dbContext = _dbContextFactory.Get();

            return bool.TryParse(dbContext.Settings.Find([nameof(OpenConfigAfterInstall)])?.Value, out var result) && result;
        }

        set => SetSettingsValue(value.ToString());
    }

    public bool UseLocalApiAndRepo
    {
        get
        {
            using var dbContext = _dbContextFactory.Get();

            return bool.TryParse(dbContext.Settings.Find([nameof(UseLocalApiAndRepo)])?.Value, out var result) && result;
        }

        set => SetSettingsValue(value.ToString());
    }

    public string LocalRepoPath
    {
        get
        {
            using var dbContext = _dbContextFactory.Get();

            return dbContext.Settings.Find([nameof(LocalRepoPath)])?.Value ?? string.Empty;
        }

        set => SetSettingsValue(value);
    }

    public string ApiPassword
    {
        get
        {
            using var dbContext = _dbContextFactory.Get();

            return dbContext.Settings.Find([nameof(ApiPassword)])?.Value ?? string.Empty;
        }

        set => SetSettingsValue(value);
    }

    public DateTime LastReadNewsDate
    {
        get
        {
            using var dbContext = _dbContextFactory.Get();

            return DateTime.TryParse(dbContext.Settings.Find([nameof(LastReadNewsDate)])?.Value, out var time) ? time : DateTime.MinValue;
        }

        set => SetSettingsValue(value.ToUniversalTime().ToString());
    }

    public Dictionary<Guid, bool> Upvotes
    {
        get
        {
            using var dbContext = _dbContextFactory.Get();

            return dbContext.Upvotes.ToDictionary(x => x.FixGuid, x => x.IsUpvoted);
        }
    }

    public void ChangeFixUpvoteState(Guid fixGuid, bool needToUpvote)
    {
        using var dbContext = _dbContextFactory.Get();

        var existing = dbContext.Upvotes.Find([fixGuid]);

        if (existing is not null)
        {
            if (existing.IsUpvoted && needToUpvote)
            {
                _ = dbContext.Upvotes.Remove(existing);
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
                _ = dbContext.Upvotes.Remove(existing);
            }
        }
        else
        {
            _ = dbContext.Upvotes.Add(new() { FixGuid = fixGuid, IsUpvoted = needToUpvote });
        }

        _ = dbContext.SaveChanges();
        ParameterChangedEvent?.Invoke(nameof(Upvotes));
    }

    public HashSet<string> HiddenTags
    {
        get
        {
            using var dbContext = _dbContextFactory.Get();

            return [.. dbContext.HiddenTags.Select(x => x.Tag)];
        }
    }

    public void ChangeTagState(string tag, bool needToHide)
    {
        using var dbContext = _dbContextFactory.Get();

        var existing = dbContext.HiddenTags.Find([tag]);

        if (existing is not null)
        {
            if (needToHide)
            {
                return;
            }
            else
            {
                _ = dbContext.HiddenTags.Remove(existing);
            }
        }
        else
        {
            if (needToHide)
            {
                _ = dbContext.HiddenTags.Add(new() { Tag = tag });
            }
            else
            {
                return;
            }
        }

        _ = dbContext.SaveChanges();
        ParameterChangedEvent?.Invoke(nameof(HiddenTags));
    }


    private void SetSettingsValue(string value, [CallerMemberName] string caller = "")
    {
        using var dbContext = _dbContextFactory.Get();

        var setting = dbContext.Settings.Find([caller]);

        if (setting is null)
        {
            _ = dbContext.Settings.Add(new() { Name = caller, Value = value });
        }
        else
        {
            setting.Value = value;
        }

        _ = dbContext.SaveChanges();
        ParameterChangedEvent?.Invoke(caller);
    }
}

