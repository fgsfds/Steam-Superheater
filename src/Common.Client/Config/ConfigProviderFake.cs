using Common.Entities;
using Common.Enums;
using static Common.IConfigProvider;

namespace Common.Client.Config;

public sealed class ConfigProviderFake : IConfigProvider
{
    public bool DeleteZipsAfterInstall { get; set; } = false;
    public bool OpenConfigAfterInstall { get; set; } = false;
    public bool ShowUninstalledGames { get; set; } = false;
    public bool ShowUnsupportedFixes { get; set; } = false;
    public bool UseLocalApiAndRepo { get; set; } = false;
    public string ApiPassword { get; set; } = string.Empty;
    public string LocalRepoPath { get; set; } = string.Empty;
    public HashSet<string> HiddenTags { get; set; } = [];
    public DateTime LastReadNewsDate { get; set; } = DateTime.MinValue;
    public ThemeEnum Theme { get; set; } = ThemeEnum.System;
    public Dictionary<Guid, bool> Upvotes { get; set; } = [];
    public List<SourceEntity> Sources =>[];
    public bool AllowEventsInvoking { get; set; } = false;
    public bool IsConsented { get; set; } = true;
    public event ParameterChanged? ParameterChangedEvent;
    public void AddSource(Uri url) { }
    public void RemoveSource(Uri url) { }
    public void ChangeFixUpvoteState(Guid fixGuid, bool needToUpvote) { }
    public void ChangeTagState(string tag, bool needToHide) { }
}

