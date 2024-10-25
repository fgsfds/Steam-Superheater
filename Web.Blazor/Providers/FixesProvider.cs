using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.RegistryFixV2;
using Common.Entities.Fixes.TextFix;
using Common.Enums;
using Common.Helpers;
using CommunityToolkit.Diagnostics;
using Database.Server;
using Database.Server.DbEntities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Cryptography;
using Web.Blazor.Telegram;

namespace Web.Blazor.Providers;

public sealed class FixesProvider
{
    private readonly DatabaseContextFactory _dbContextFactory;
    private readonly HttpClient _httpClient;
    private readonly ILogger<FixesProvider> _logger;
    private readonly TelegramBot _bot;

    private bool _isCheckFixesRunning = false;


    public FixesProvider(
        ILogger<FixesProvider> logger,
        HttpClient httpClient,
        DatabaseContextFactory dbContextFactory,
        TelegramBot bot
        )
    {
        _httpClient = httpClient;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _bot = bot;
    }


    /// <summary>
    /// Get list of fixes from the database
    /// </summary>
    public List<FixesList> GetFixesList(int tableVersion, Version? appVersion)
    {
        Stopwatch sw = new();
        sw.Start();

        //disposed later
        var dbContext = _dbContextFactory.Get();

        var fixesDb = dbContext.Fixes.AsNoTracking().Where(x => x.TableVersion > tableVersion).OrderBy(static x => x.Name.StartsWith("No Intro")).ThenBy(x => x.Name).ToLookup(static x => x.GameId);

        if (fixesDb.Count == 0)
        {
            return [];
        }
        
        var games = dbContext.Games.AsNoTracking().OrderBy(static x => x.Name).ToList();
        var fileFixesDb = dbContext.FileFixes.AsNoTracking().ToDictionary(static x => x.FixGuid);
        var regFixesDb = dbContext.RegistryFixes.AsNoTracking().ToLookup(static x => x.FixGuid);
        var hostsFixesDb = dbContext.HostsFixes.AsNoTracking().ToDictionary(static x => x.FixGuid);

        var tagsDict = dbContext.Tags.AsNoTracking().ToDictionary(static x => x.Id, static x => x.Tag);

        var tagsIdsDb = dbContext.TagsLists.AsNoTracking().ToLookup(static x => x.FixGuid, static x => x.TagId);
        var dependencies = dbContext.Dependencies.AsNoTracking().ToLookup(static x => x.FixGuid, static x => x.DependencyGuid);

        dbContext.Dispose();

        List<FixesList> fixesLists = new(games.Count);

        foreach (var game in games)
        {
            var fixes = fixesDb[game.Id];

            if (!fixes.Any())
            {
                continue;
            }

            List<BaseFixEntity> baseFixEntities = new(fixes.Count());

            foreach (var fix in fixes)
            {
                if (appVersion <= new Version(1,2,0))
                {
                    if (fix.FixType is FixTypeEnum.RegistryFixV2)
                    {
                        continue;
                    }
                }

                var deps = dependencies[fix.Guid];
                var tags = tagsIdsDb[fix.Guid].Select(x => tagsDict[x]);

                OSEnum supportedOSes = 0;

                if (fix.IsWindowsSupported)
                {
                    supportedOSes = supportedOSes.AddFlag(OSEnum.Windows);
                }
                if (fix.IsLinuxSupported)
                {
                    supportedOSes = supportedOSes.AddFlag(OSEnum.Linux);
                }

                if (fix.FixType is FixTypeEnum.FileFix)
                {
                    var fileFix = fileFixesDb[fix.Guid];

                    FileFixEntity fileFixEntity = new()
                    {
                        Name = fix.Name,
                        Version = fix.VersionOld,
                        VersionStr = fix.Version,
                        Guid = fix.Guid,
                        Description = fix.Description,
                        Changelog = fix.Changelog,
                        Dependencies = !deps.Any() ? null : [.. deps],
                        Tags = !tags.Any() ? null : [.. tags],
                        SupportedOSes = supportedOSes,
                        Installs = fix.Installs,
                        Score = fix.Score,
                        Notes = fix.Notes,
                        IsDisabled = fix.IsDisabled,

                        FileSize = fileFix.FileSize,
                        MD5 = fileFix.MD5,
                        Url = fileFix!.Url,
                        InstallFolder = fileFix.InstallFolder,
                        ConfigFile = fileFix.ConfigFile,
                        FilesToDelete = fileFix.FilesToDelete,
                        FilesToBackup = fileFix.FilesToBackup,
                        FilesToPatch = fileFix.FilesToPatch,
                        RunAfterInstall = fileFix.RunAfterInstall,
                        SharedFixGuid = fileFix.SharedFixGuid,
                        SharedFixInstallFolder = fileFix.SharedFixInstallFolder,
                        WineDllOverrides = fileFix.WineDllOverrides,
                        Variants = fileFix.Variants
                    };

                    baseFixEntities.Add(fileFixEntity);
                }
                else if (fix.FixType is FixTypeEnum.RegistryFix)
                {
                    var regFix = regFixesDb[fix.Guid].First();

                    RegistryFixEntity regFixEntity = new()
                    {
                        Name = fix.Name,
                        Version = fix.VersionOld,
                        VersionStr = fix.Version,
                        Guid = fix.Guid,
                        Description = fix.Description,
                        Changelog = fix.Changelog,
                        Dependencies = !deps.Any() ? null : [.. deps],
                        Tags = !tags.Any() ? null : [.. tags],
                        SupportedOSes = supportedOSes,
                        Installs = fix.Installs,
                        Score = fix.Score,
                        Notes = fix.Notes,
                        IsDisabled = fix.IsDisabled,

                        Key = regFix.Key,
                        ValueName = regFix.ValueName,
                        NewValueData = regFix.NewValueData,
                        ValueType = regFix.ValueType
                    };

                    baseFixEntities.Add(regFixEntity);
                }
                else if (fix.FixType is FixTypeEnum.RegistryFixV2)
                {
                    List<RegistryEntry> entries = [];

                    foreach (var entry in regFixesDb[fix.Guid])
                    {
                        entries.Add(new RegistryEntry()
                        {
                            Key = entry.Key,
                            ValueName = entry.ValueName,
                            ValueType = entry.ValueType,
                            NewValueData = entry.NewValueData,
                        });
                    }

                    RegistryFixV2Entity regFixEntity = new()
                    {
                        Name = fix.Name,
                        Version = fix.VersionOld,
                        VersionStr = fix.Version,
                        Guid = fix.Guid,
                        Description = fix.Description,
                        Changelog = fix.Changelog,
                        Dependencies = !deps.Any() ? null : [.. deps],
                        Tags = !tags.Any() ? null : [.. tags],
                        SupportedOSes = supportedOSes,
                        Installs = fix.Installs,
                        Score = fix.Score,
                        Notes = fix.Notes,
                        IsDisabled = fix.IsDisabled,

                        Entries = entries
                    };

                    baseFixEntities.Add(regFixEntity);
                }
                else if (fix.FixType is FixTypeEnum.HostsFix)
                {
                    var hostsFix = hostsFixesDb[fix.Guid];

                    HostsFixEntity hostsFixEntity = new()
                    {
                        Name = fix.Name,
                        Version = fix.VersionOld,
                        VersionStr = fix.Version,
                        Guid = fix.Guid,
                        Description = fix.Description,
                        Changelog = fix.Changelog,
                        Dependencies = !deps.Any() ? null : [.. deps],
                        Tags = !tags.Any() ? null : [.. tags],
                        SupportedOSes = supportedOSes,
                        Installs = fix.Installs,
                        Score = fix.Score,
                        Notes = fix.Notes,
                        IsDisabled = fix.IsDisabled,

                        Entries = hostsFix.Entries
                    };

                    baseFixEntities.Add(hostsFixEntity);
                }
                else if (fix.FixType is FixTypeEnum.TextFix)
                {
                    TextFixEntity textFixEntity = new()
                    {
                        Name = fix.Name,
                        Version = fix.VersionOld,
                        VersionStr = fix.Version,
                        Guid = fix.Guid,
                        Description = fix.Description,
                        Changelog = fix.Changelog,
                        Dependencies = !deps.Any() ? null : [.. deps],
                        Tags = !tags.Any() ? null : [.. tags],
                        SupportedOSes = supportedOSes,
                        Installs = fix.Installs,
                        Score = fix.Score,
                        Notes = fix.Notes,
                        IsDisabled = fix.IsDisabled
                    };

                    baseFixEntities.Add(textFixEntity);
                }
            }

            FixesList fixesList = new()
            {
                GameId = game.Id,
                GameName = game.Name,
                Fixes = baseFixEntities
            };

            fixesLists.Add(fixesList);
        }

        sw.Stop();
        _logger.LogInformation($"Got fixes in {sw.ElapsedMilliseconds} ms");

        return fixesLists;
    }

    /// <summary>
    /// Change score of the fix
    /// </summary>
    /// <param name="fixGuid">Fix guid</param>
    /// <param name="increment">Increment</param>
    /// <returns>New score</returns>
    public async Task<int> ChangeFixScoreAsync(Guid fixGuid, sbyte increment)
    { 
        using var dbContext = _dbContextFactory.Get();

        var fix = dbContext.Fixes.Find(fixGuid)!;

        fix.Score += increment;

        var newScore = fix.Score;

        _ = dbContext.SaveChanges();

        var names = (from fixx in dbContext.Fixes
                     join gamee in dbContext.Games
                     on fixx.GameId equals gamee.Id
                     where fixx.Guid == fixGuid
                     select new Tuple<string, string>(gamee.Name, fixx.Name))
                 .First();

        await _bot.SendMessageAsync($"Fix score changed: {names.Item1} - {names.Item2} incr {increment}");

        return newScore;
    }

    /// <summary>
    /// Add 1 to fix installs count
    /// </summary>
    /// <param name="fixGuid">Fix guid</param>
    /// <returns>New installs count</returns>
    public int IncreaseFixInstallsCount(Guid fixGuid)
    {
        using var dbContext = _dbContextFactory.Get();

        var fix = dbContext.Fixes.Find(fixGuid)!;

        fix.Installs += 1;

        var newInstalls = fix.Installs;

        _ = dbContext.SaveChanges();

        return newInstalls;
    }

    /// <summary>
    /// Add report for a fix
    /// </summary>
    /// <param name="fixGuid">Fix guid</param>
    /// <param name="text">Report text</param>
    public async Task<bool> AddReportAsync(Guid fixGuid, string text)
    {
        using var dbContext = _dbContextFactory.Get();

        ReportsDbEntity entity = new()
        {
            FixGuid = fixGuid,
            ReportText = text
        };

        _ = dbContext.Reports.Add(entity);
        _ = dbContext.SaveChanges();


        var names = (from fixx in dbContext.Fixes
                     join gamee in dbContext.Games
                     on fixx.GameId equals gamee.Id
                     where fixx.Guid == fixGuid
                     select new Tuple<string, string>(gamee.Name, fixx.Name))
                 .First();

        await _bot.SendMessageAsync($"Fix score changed: {names.Item1} - {names.Item2} text {text}");

        return true;
    }

    /// <summary>
    /// Check if fix exists in the database
    /// </summary>
    /// <param name="fixGuid">FIx guid</param>
    /// <returns>Does fix exist</returns>
    public int? CheckIfFixExists(Guid fixGuid)
    {
        using var dbContext = _dbContextFactory.Get();

        var fix = dbContext.Fixes.Find(fixGuid);

        if (fix is null)
        {
            return null;
        }

        return fix.VersionOld;
    }

    /// <summary>
    /// Disable or enable fix in the database
    /// </summary>
    /// <param name="fixGuid">Fix guid</param>
    /// <param name="isDisabled">Is disabled</param>
    public bool ChangeFixDisabledState(Guid fixGuid, bool isDisabled)
    {
        using var dbContext = _dbContextFactory.Get();
        var fix = dbContext.Fixes.Find(fixGuid);

        var databaseVersions = dbContext.DatabaseVersions.Find(DatabaseTableEnum.Fixes)!;
        var newTableVersion = databaseVersions.Version + 1;

        Guard.IsNotNull(fix);

        fix.IsDisabled = isDisabled;
        fix.TableVersion = newTableVersion;
        databaseVersions.Version = newTableVersion;

        _ = dbContext.SaveChanges();

        return true;
    }

    /// <summary>
    /// Add or change fix in the database
    /// </summary>
    /// <param name="gameId">Game id</param>
    /// <param name="gameName">Game name</param>
    /// <param name="fix">Fix</param>
    /// <returns>Is successfully added</returns>
    public async Task<bool> AddFixAsync(int gameId, string gameName, BaseFixEntity fix)
    {
        Guard.IsNotNull(fix);

        if (fix is FileFixEntity fileFix1 &&
            fileFix1.Url is not null)
        {
            var result = await _httpClient.GetAsync(fileFix1.Url, HttpCompletionOption.ResponseHeadersRead);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogError($"File {fileFix1.Url} doesn't exist");
                return false;
            }
        }

        using var dbContext = _dbContextFactory.Get();
        using (var transaction = dbContext.Database.BeginTransaction())
        {
            var databaseVersions = dbContext.DatabaseVersions.Find(DatabaseTableEnum.Fixes)!;
            var newTableVersion = databaseVersions.Version + 1;

            //adding or modifying game
            var gameEntity = dbContext.Games.Find(gameId);

            if (gameEntity is null)
            {
                GamesDbEntity newGameEntity = new()
                {
                    Id = gameId,
                    Name = gameName
                };

                _ = dbContext.Games.Add(newGameEntity);
                _ = dbContext.SaveChanges();

                gameEntity = dbContext.Games.Find(gameId);

                if (gameEntity is null)
                {
                    _logger.LogError("Error while adding new game");
                    return false;
                }
            }
            else
            {
                gameEntity.Name = gameName;
            }


            //adding or modifying base fix
            var existingEntity = dbContext.Fixes.Find(fix.Guid);

            var fixType = fix is FileFixEntity ? FixTypeEnum.FileFix :
                fix is RegistryFixEntity ? FixTypeEnum.RegistryFix :
                fix is RegistryFixV2Entity ? FixTypeEnum.RegistryFixV2 :
                fix is HostsFixEntity ? FixTypeEnum.HostsFix :
                fix is TextFixEntity ? FixTypeEnum.TextFix :
                ThrowHelper.ThrowNotSupportedException<FixTypeEnum>();

            if (existingEntity is null)
            {
                FixesDbEntity newFixEntity = new()
                {
                    GameId = gameEntity.Id,
                    FixType = fixType,
                    Guid = fix.Guid,
                    Description = fix.Description,
                    Changelog = fix.Changelog,
                    IsLinuxSupported = fix.SupportedOSes.HasFlag(OSEnum.Linux),
                    IsWindowsSupported = fix.SupportedOSes.HasFlag(OSEnum.Windows),
                    Name = fix.Name,
                    Notes = fix.Notes,
                    VersionOld = fix.Version,
                    Version = fix.VersionStr,
                    IsDisabled = false,
                    TableVersion = newTableVersion,
                    Score = 0,
                    Installs = 0
                };

                _ = dbContext.Fixes.Add(newFixEntity);
                existingEntity = dbContext.Fixes.Find(fix.Guid);

                if (existingEntity is null)
                {
                    _logger.LogError("Error while adding new fix");
                    return false;
                }
            }
            else
            {
                existingEntity.GameId = gameEntity.Id;
                existingEntity.FixType = fixType;
                existingEntity.Description = fix.Description;
                existingEntity.Changelog = fix.Changelog;
                existingEntity.IsLinuxSupported = fix.SupportedOSes.HasFlag(OSEnum.Linux);
                existingEntity.IsWindowsSupported = fix.SupportedOSes.HasFlag(OSEnum.Windows);
                existingEntity.Name = fix.Name;
                existingEntity.Notes = fix.Notes;
                existingEntity.VersionOld = fix.Version;
                existingEntity.Version = fix.VersionStr;
                existingEntity.TableVersion = newTableVersion;
            }

            _ = dbContext.SaveChanges();


            //removing existing sub fix and adding new one
            DeleteExistingSubFixes(dbContext, existingEntity);

            if (fix is FileFixEntity fileFix)
            {
                var fileSize = fileFix.Url is null ? null : await GetFileSizeAsync(fileFix.Url);
                var fileMd5 = fileFix.Url is null ? null : await GetFileMD5Async(fileFix.Url);

                FileFixesDbEntity newFixEntity = new()
                {
                    FixGuid = fileFix.Guid,
                    Url = fileFix.Url,
                    FileSize = fileSize,
                    MD5 = fileMd5,
                    InstallFolder = fileFix.InstallFolder,
                    ConfigFile = fileFix.ConfigFile,
                    FilesToDelete = fileFix.FilesToDelete,
                    FilesToBackup = fileFix.FilesToBackup,
                    FilesToPatch = fileFix.FilesToPatch,
                    RunAfterInstall = fileFix.RunAfterInstall,
                    Variants = fileFix.Variants,
                    SharedFixGuid = fileFix.SharedFixGuid,
                    SharedFixInstallFolder = fileFix.SharedFixInstallFolder,
                    WineDllOverrides = fileFix.WineDllOverrides
                };

                _ = dbContext.FileFixes.Add(newFixEntity);
            }
            else if (fix is RegistryFixEntity regFix)
            {
                RegistryFixesDbEntity newFixEntity = new()
                {
                    FixGuid = regFix.Guid,
                    Key = regFix.Key,
                    ValueName = regFix.ValueName,
                    ValueType = regFix.ValueType,
                    NewValueData = regFix.NewValueData
                };

                _ = dbContext.RegistryFixes.Add(newFixEntity);
            }
            else if (fix is RegistryFixV2Entity regFix2)
            {
                foreach (var entry in regFix2.Entries)
                {
                    RegistryFixesDbEntity newFixEntity = new()
                    {
                        FixGuid = regFix2.Guid,
                        Key = entry.Key,
                        ValueName = entry.ValueName,
                        ValueType = entry.ValueType,
                        NewValueData = entry.NewValueData
                    };

                    _ = dbContext.RegistryFixes.Add(newFixEntity);
                }
            }
            else if (fix is HostsFixEntity hostsFix)
            {
                HostsFixesDbEntity newFixEntity = new()
                {
                    FixGuid = hostsFix.Guid,
                    Entries = hostsFix.Entries
                };

                _ = dbContext.HostsFixes.Add(newFixEntity);
            }
            else if (fix is TextFixEntity textFix)
            {
                //nothing to do
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            _ = dbContext.SaveChanges();


            //removing existing and adding new tags
            _ = dbContext.TagsLists.Where(x => x.FixGuid == fix.Guid).ExecuteDelete();

            if (fix.Tags is not null)
            {
                foreach (var tag in fix.Tags.ToList())
                {
                    var existingTag = dbContext.Tags.SingleOrDefault(x => x.Tag == tag);

                    if (existingTag is null)
                    {
                        var neww = dbContext.Tags.Add(new() { Tag = tag });
                        _ = dbContext.SaveChanges();

                        existingTag = dbContext.Tags.SingleOrDefault(x => x.Tag == tag);
                    }

                    TagsListsDbEntity newEntity = new()
                    {
                        FixGuid = fix.Guid,
                        TagId = existingTag!.Id
                    };

                    _ = dbContext.TagsLists.Add(newEntity);
                }
            }

            _ = dbContext.SaveChanges();


            //removing existing and adding new dependencies
            _ = dbContext.Dependencies.Where(x => x.FixGuid == fix.Guid).ExecuteDelete();

            if (fix.Dependencies is not null)
            {
                foreach (var tag in fix.Dependencies.ToList())
                {
                    DependenciesDbEntity newEntity = new()
                    {
                        FixGuid = fix.Guid,
                        DependencyGuid = tag
                    };

                    _ = dbContext.Dependencies.Add(newEntity);
                }
            }

            _ = dbContext.SaveChanges();

            databaseVersions.Version = newTableVersion;
            _ = dbContext.SaveChanges();

            transaction.Commit();
        }

        return true;
    }

    /// <summary>
    /// Check files' availability, md5 and size in the database
    /// </summary>
    /// <returns></returns>
    public async Task CheckFixesAsync()
    {
        if (_isCheckFixesRunning)
        {
            const string AlreadyRunning = "Fixes check already running";
            _logger.LogInformation(AlreadyRunning);
            _ = _bot.SendMessageAsync(AlreadyRunning);

            return;
        }
        _isCheckFixesRunning = true;

        const string Started = "Fixes check started";
        _logger.LogInformation(Started);
        _ = _bot.SendMessageAsync(Started);

        using var dbContext = _dbContextFactory.Get();
        var fixes = dbContext.FileFixes.AsNoTracking().Where(static x => x.Url != null);

        List<string> md5Errors = [];

        foreach (var fix in fixes)
        {
            try
            {
                if (fix.Url is null)
                {
                    continue;
                }

                var result = await _httpClient.GetAsync(fix.Url, HttpCompletionOption.ResponseHeadersRead);

                //file availability
                if (result is null || !result.IsSuccessStatusCode)
                {
                    var message = $"File doesn't exist or unavailable: {fix.Url}";
                    _logger.LogError(message);
                    _ = _bot.SendMessageAsync(message);
                    continue;
                }


                if (fix.MD5 is null)
                {
                    var message = $"File doesn't have MD5 in the database: {fix.Url}";
                    _logger.LogError(message);
                    _ = _bot.SendMessageAsync(message);
                }
                if (fix.FileSize is null)
                {
                    var message = $"File doesn't have size in the database: {fix.Url}";
                    _logger.LogError(message);
                    _ = _bot.SendMessageAsync(message);
                }


                //md5 for s3 files
                if (fix.Url.StartsWith(Consts.FilesBucketUrl))
                {
                    if (result.Headers.ETag?.Tag is null)
                    {
                        var message = $"File doesn't have ETag: {fix.Url}";
                        _logger.LogError(message);
                        _ = _bot.SendMessageAsync(message);
                    }
                    else
                    {
                        var md5 = result.Headers.ETag!.Tag.Replace("\"", "");

                        if (fix.FixGuid == Guid.Parse("42d240dc-9778-4c3e-9bab-99b5c994655b"))
                        {
                            //_logger.LogError($"Skipped: {fix.Url}");
                        }
                        else if (md5.Contains('-'))
                        {
                            var message = $"File has incorrect ETag: {fix.Url}";
                            _logger.LogError(message);
                            _ = _bot.SendMessageAsync(message);
                        }
                        else if (!md5.Equals(fix.MD5, StringComparison.OrdinalIgnoreCase))
                        {
                            var message = $"File's MD5 doesn't match: {fix.Url}";
                            _logger.LogError(message);
                            _ = _bot.SendMessageAsync(message);
                        }
                    }
                }
                //md5 of external files
                else if (result.Content.Headers.ContentMD5 is null)
                {
                    md5Errors.Add(fix.Url);
                    //var message = $"Fix doesn't have MD5 in the header: {fix.Url}";
                    //_logger.LogError(message);
                    //_ = _bot.SendMessageAsync(message);
                }
                else if (!BitConverter.ToString(result.Content.Headers.ContentMD5).Replace("-", string.Empty).Equals(fix.MD5, StringComparison.OrdinalIgnoreCase))
                {
                    var message = $"File's MD5 doesn't match: {fix.Url}";
                    _logger.LogError(message);
                    _ = _bot.SendMessageAsync(message);
                }


                //file size
                if (result.Content.Headers.ContentLength is null)
                {
                    var message = $"File doesn't have size in the header: {fix.Url}";
                    _logger.LogError(message);
                    _ = _bot.SendMessageAsync(message);
                }
                else if (result.Content.Headers.ContentLength != fix.FileSize)
                {
                    var message = $"File size doesn't match: {fix.Url}";
                    _logger.LogError(message);
                    _ = _bot.SendMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
                string? message = $"Fix check error: {fix.Url}";
                _logger.LogCritical(ex, message);
                _ = _bot.SendMessageAsync($"{message}{Environment.NewLine}{ex}");
            }
        }

        if (md5Errors.Count > 0)
        {
            _logger.LogError($"""
            Following fixes don't have MD5 in the header:

            {string.Join(Environment.NewLine, md5Errors)}
            """);
        }

        const string Message = "Fixes check ended";
        _logger.LogInformation(Message);
        _ = _bot.SendMessageAsync(Message);

        _isCheckFixesRunning = false;
    }

    public (Dictionary<Guid, int>, Dictionary<Guid, int>) GetFixesStats()
    {
        Dictionary<Guid, int> installs = [];
        Dictionary<Guid, int> scores = [];

        using var dbContext = _dbContextFactory.Get();

        foreach (var fix in dbContext.Fixes)
        {
            installs.Add(fix.Guid, fix.Installs);
            scores.Add(fix.Guid, fix.Score);
        }

        return (installs, scores);
    }


    /// <summary>
    /// Delete sub fixes from the database
    /// </summary>
    /// <param name="dbContext">DB context</param>
    /// <param name="existingEntity">Fix entity</param>
    private void DeleteExistingSubFixes(DatabaseContext dbContext, FixesDbEntity? existingEntity)
    {
        if (existingEntity is null)
        {
            return;
        }

        var a = dbContext.FileFixes.Find(existingEntity.Guid);
        if (a is not null)
        {
            _ = dbContext.FileFixes.Remove(a);
        }

        var b = dbContext.RegistryFixes.Where(x => x.FixGuid == existingEntity.Guid);
        if (b is not null && b.Any())
        {
           dbContext.RegistryFixes.RemoveRange(b);
        }

        var c = dbContext.HostsFixes.Find(existingEntity.Guid);
        if (c is not null)
        {
            _ = dbContext.HostsFixes.Remove(c);
        }
    }

    /// <summary>
    /// Get the size of the local or online file
    /// </summary>
    /// <param name="fixUrl">File url</param>
    /// <returns>Size of the file in bytes</returns>
    /// <exception cref="Exception">Http response error</exception>
    private async Task<long?> GetFileSizeAsync(string fixUrl)
    {
        using var response = await _httpClient.GetAsync(fixUrl, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            return ThrowHelper.ThrowInvalidDataException<long>($"Error while getting response for {fixUrl}: {response.StatusCode}");
        }
        else if (response.Content.Headers.ContentLength is not null)
        {
            return response.Content.Headers.ContentLength;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Get MD5 of the local or online file
    /// </summary>
    /// <param name="fixUrl">File url</param>
    /// <returns>MD5 of the fix file</returns>
    /// <exception cref="Exception">Http response error</exception>
    private async Task<string> GetFileMD5Async(string fixUrl)
    {
        using var response = await _httpClient.GetAsync(fixUrl, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            return ThrowHelper.ThrowInvalidDataException<string>($"Error while getting response for {fixUrl}: {response.StatusCode}");
        }
        else if (response.Content.Headers.ContentMD5 is not null)
        {
            return BitConverter.ToString(response.Content.Headers.ContentMD5).Replace("-", string.Empty);
        }
        else if (fixUrl.StartsWith(Consts.FilesBucketUrl) && response.Headers.ETag?.Tag is not null)
        {
            var md5fromEtag = response.Headers.ETag.Tag.Replace("\"", "");

            if (!md5fromEtag.Contains('-'))
            {
                return md5fromEtag.ToUpper();
            }
        }

        //if can't get md5 from the response, download zip
        await using var source = await response.Content.ReadAsStreamAsync();

        using var md5 = MD5.Create();

        var hash = Convert.ToHexString(await md5.ComputeHashAsync(source));

        return hash;
    }
}

