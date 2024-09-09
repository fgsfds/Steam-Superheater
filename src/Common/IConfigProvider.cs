using Common.Enums;

namespace Common;

public interface IConfigProvider
{
    string ApiPassword { get; set; }
    bool DeleteZipsAfterInstall { get; set; }
    HashSet<string> HiddenTags { get; }
    DateTime LastReadNewsDate { get; set; }
    string LocalRepoPath { get; set; }
    bool OpenConfigAfterInstall { get; set; }
    bool ShowUninstalledGames { get; set; }
    bool ShowUnsupportedFixes { get; set; }
    ThemeEnum Theme { get; set; }
    Dictionary<Guid, bool> Upvotes { get; }
    bool UseLocalApiAndRepo { get; set; }

    void ChangeFixUpvoteState(Guid fixGuid, bool needToUpvote);
    void ChangeTagState(string tag, bool needToHide);

    delegate void ParameterChanged(string parameterName);
    event ParameterChanged ParameterChangedEvent;
}

