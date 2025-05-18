using Common.Entities;
using Common.Enums;
using Common.Helpers;
using CommunityToolkit.Diagnostics;
using Database.Client;
using Database.Client.DbEntities;
using System.Runtime.CompilerServices;
using static Common.IConfigProvider;

namespace Common.Client.Config;

public sealed class ConfigProvider : IConfigProvider
{
    private readonly DatabaseContextFactory _dbContextFactory;

    public event ParameterChanged? ParameterChangedEvent;


    public ConfigProvider(DatabaseContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;

        using var dbContext = dbContextFactory.Get();

        _theme = Enum.TryParse<ThemeEnum>(dbContext.Settings.Find([nameof(Theme)])?.Value, out var result) ? result : ThemeEnum.System;
        _showUninstalledGames = bool.TryParse(dbContext.Settings.Find([nameof(ShowUninstalledGames)])?.Value, out var result2) && result2;
        _showUnsupportedFixes = bool.TryParse(dbContext.Settings.Find([nameof(ShowUnsupportedFixes)])?.Value, out var result3) && result3;
        _deleteZipsAfterInstall = bool.TryParse(dbContext.Settings.Find([nameof(DeleteZipsAfterInstall)])?.Value, out var result4) && result4;
        _openConfigAfterInstall = bool.TryParse(dbContext.Settings.Find([nameof(OpenConfigAfterInstall)])?.Value, out var result5) && result5;
        _useLocalApiAndRepo = bool.TryParse(dbContext.Settings.Find([nameof(UseLocalApiAndRepo)])?.Value, out var result6) && result6;
        _localRepoPath = dbContext.Settings.Find([nameof(LocalRepoPath)])?.Value ?? string.Empty;
        _apiPassword = dbContext.Settings.Find([nameof(ApiPassword)])?.Value ?? string.Empty;
        _lastReadNewsDate = DateTime.TryParse(dbContext.Settings.Find([nameof(LastReadNewsDate)])?.Value, out var time) ? time : DateTime.MinValue;
        _hiddenTags = [.. dbContext.HiddenTags.Select(x => x.Tag)];
        Upvotes = dbContext.Upvotes.ToDictionary(x => x.FixGuid, x => x.IsUpvoted);
        Sources = [.. dbContext.Sources
            .ToList()
            .Select(x =>
            {
                if (!UriHelper.TryParseUri(x.Url, out var uri))
                {
                    return null;
                }

                return new SourceEntity() { Name = "123", Url = uri, IsEnabled = x.IsEnabled };
            })
            .Where(x => x is not null)];
    }


    public bool AllowEventsInvoking { get; set; }


    private ThemeEnum _theme;
    public ThemeEnum Theme
    {
        get
        {
            return _theme;
        }
        set
        {
            _theme = value;
            SetSettingsDbValue(value.ToString());
        }
    }

    private bool _showUninstalledGames;
    public bool ShowUninstalledGames
    {
        get
        {
            return _showUninstalledGames;
        }
        set
        {
            _showUninstalledGames = value;
            SetSettingsDbValue(value.ToString());
        }
    }

    private bool _showUnsupportedFixes;
    public bool ShowUnsupportedFixes
    {
        get
        {
            return _showUnsupportedFixes;
        }
        set
        {
            _showUnsupportedFixes = value;
            SetSettingsDbValue(value.ToString());
        }
    }

    private bool _deleteZipsAfterInstall;
    public bool DeleteZipsAfterInstall
    {
        get
        {
            return _deleteZipsAfterInstall;
        }
        set
        {
            _deleteZipsAfterInstall = value;
            SetSettingsDbValue(value.ToString());
        }
    }

    private bool _openConfigAfterInstall;
    public bool OpenConfigAfterInstall
    {
        get
        {
            return _openConfigAfterInstall;
        }
        set
        {
            _openConfigAfterInstall = value;
            SetSettingsDbValue(value.ToString());
        }
    }

    private bool _useLocalApiAndRepo;
    public bool UseLocalApiAndRepo
    {
        get
        {
            return _useLocalApiAndRepo;
        }
        set
        {
            _useLocalApiAndRepo = value;
            SetSettingsDbValue(value.ToString());
        }
    }

    private string _localRepoPath;
    public string LocalRepoPath
    {
        get
        {
            return _localRepoPath;
        }
        set
        {
            _localRepoPath = value;
            SetSettingsDbValue(value);
        }
    }

    private string _apiPassword;
    public string ApiPassword
    {
        get
        {
            return _apiPassword;
        }
        set
        {
            _apiPassword = value;
            SetSettingsDbValue(value);
        }
    }

    private DateTime _lastReadNewsDate;
    public DateTime LastReadNewsDate
    {
        get
        {
            return _lastReadNewsDate;
        }
        set
        {
            _lastReadNewsDate = value;
            SetSettingsDbValue(value.ToUniversalTime().ToString());
        }
    }

    public Dictionary<Guid, bool> Upvotes { get; private set; }

    public List<SourceEntity> Sources { get; private set; }

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
        Upvotes = dbContext.Upvotes.ToDictionary(x => x.FixGuid, x => x.IsUpvoted);

        if (AllowEventsInvoking)
        {
            ParameterChangedEvent?.Invoke(nameof(Upvotes));
        }
    }

    private HashSet<string> _hiddenTags;
    public HashSet<string> HiddenTags
    {
        get
        {
            return _hiddenTags;
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
        _hiddenTags = [.. dbContext.HiddenTags.Select(x => x.Tag)];

        if (AllowEventsInvoking)
        {
            ParameterChangedEvent?.Invoke(nameof(HiddenTags));
        }
    }

    public void AddSource(Uri url)
    {
        using var dbContext = _dbContextFactory.Get();

        SourcesDbEntity newSource = new()
        {
            Url = url.ToString(),
            IsEnabled = true
        };

        var existing = dbContext.Sources.Add(newSource);

        _ = dbContext.SaveChanges();

        Sources = [.. dbContext.Sources
            .ToList()
            .Select(x =>
            {
                if (!UriHelper.TryParseUri(x.Url, out var uri))
                {
                    return null;
                }

                return new SourceEntity() { Name = "123", Url = uri, IsEnabled = x.IsEnabled };
            })
            .Where(x => x is not null)];

        if (AllowEventsInvoking)
        {
            ParameterChangedEvent?.Invoke(nameof(Sources));
        }
    }

    public void RemoveSource(Uri url)
    {
        using var dbContext = _dbContextFactory.Get();

        var existing = dbContext.Sources.Find(url.ToString());

        if (existing is null)
        {
            ThrowHelper.ThrowInvalidOperationException($"Can't find source {url} in the database.");
        }

        _ = dbContext.Sources.Remove(existing);

        _ = dbContext.SaveChanges();

        Sources = [.. dbContext.Sources
            .ToList()
            .Select(x =>
            {
                if (!UriHelper.TryParseUri(x.Url, out var uri))
                {
                    return null;
                }

                return new SourceEntity() { Name = "123", Url = uri, IsEnabled = x.IsEnabled };
            })
            .Where(x => x is not null)];

        if (AllowEventsInvoking)
        {
            ParameterChangedEvent?.Invoke(nameof(Sources));
        }
    }


    private void SetSettingsDbValue(string value, [CallerMemberName] string caller = "")
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

        if (AllowEventsInvoking)
        {
            ParameterChangedEvent?.Invoke(caller);
        }
    }
}

