using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.TextFix;
using Common.Enums;
using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Telegram;
using Web.Server.Database;
using Web.Server.DbEntities;
using Web.Server.Helpers;

namespace Superheater.Web.Server.Providers
{
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
        public List<FixesList> GetFixesList()
        {
            Stopwatch sw = new();
            sw.Start();

            //disposed later
            var dbContext = _dbContextFactory.Get();

            var games = dbContext.Games.AsNoTracking().OrderBy(static x => x.Name).ToList();
            var fixesDb = dbContext.Fixes.AsNoTracking().OrderBy(static x => x.Name).ToLookup(static x => x.GameId);
            var fileFixesDb = dbContext.FileFixes.AsNoTracking().ToDictionary(static x => x.FixGuid);
            var regFixesDb = dbContext.RegistryFixes.AsNoTracking().ToDictionary(static x => x.FixGuid);
            var hostsFixesDb = dbContext.HostsFixes.AsNoTracking().ToDictionary(static x => x.FixGuid);

            var tagsDict = dbContext.Tags.AsNoTracking().ToDictionary(static x => x.Id, static x => x.Tag);

            var tagsIdsDb = dbContext.TagsLists.AsNoTracking().ToLookup(static x => x.FixGuid, static x => x.TagId);
            var dependencies = dbContext.Dependencies.AsNoTracking().ToLookup(static x => x.FixGuid, static x => x.DependencyGuid);

            var installsDb = dbContext.Installs.AsNoTracking().ToDictionary(static x => x.FixGuid, static x => x.Installs);
            var scoresDb = dbContext.Scores.AsNoTracking().ToDictionary(static x => x.FixGuid, static x => x.Score);


            dbContext.Dispose();

            List<FixesList> fixesLists = new(games.Count);

            foreach (var game in games)
            {
                var fixes = fixesDb[game.Id];

                List<BaseFixEntity> baseFixEntities = new(fixes.Count());

                foreach (var fix in fixes)
                {
                    var type = (FixTypeEnum)fix.FixType;
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

                    if (type is FixTypeEnum.FileFix)
                    {
                        var fileFix = fileFixesDb[fix.Guid];

                        FileFixEntity fileFixEntity = new()
                        {
                            Name = fix.Name,
                            Version = fix.Version,
                            Guid = fix.Guid,
                            Description = fix.Description,
                            Dependencies = !deps.Any() ? null : [.. deps],
                            Tags = !tags.Any() ? null : [.. tags],
                            SupportedOSes = supportedOSes,
                            Installs = installsDb.GetValueOrDefault(fix.Guid),
                            Score = scoresDb.GetValueOrDefault(fix.Guid),
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
                    else if (type is FixTypeEnum.RegistryFix)
                    {
                        var regFix = regFixesDb[fix.Guid];

                        RegistryFixEntity fileFixEntity = new()
                        {
                            Name = fix.Name,
                            Version = fix.Version,
                            Guid = fix.Guid,
                            Description = fix.Description,
                            Dependencies = !deps.Any() ? null : [.. deps],
                            Tags = !tags.Any() ? null : [.. tags],
                            SupportedOSes = supportedOSes,
                            Installs = installsDb.GetValueOrDefault(fix.Guid),
                            Score = scoresDb.GetValueOrDefault(fix.Guid),
                            Notes = fix.Notes,
                            IsDisabled = fix.IsDisabled,

                            Key = regFix.Key,
                            ValueName = regFix.ValueName,
                            NewValueData = regFix.NewValueData,
                            ValueType = (RegistryValueTypeEnum)regFix.ValueType
                        };

                        baseFixEntities.Add(fileFixEntity);
                    }
                    else if (type is FixTypeEnum.HostsFix)
                    {
                        var hostsFix = hostsFixesDb[fix.Guid];

                        HostsFixEntity fileFixEntity = new()
                        {
                            Name = fix.Name,
                            Version = fix.Version,
                            Guid = fix.Guid,
                            Description = fix.Description,
                            Dependencies = !deps.Any() ? null : [.. deps],
                            Tags = !tags.Any() ? null : [.. tags],
                            SupportedOSes = supportedOSes,
                            Installs = installsDb.GetValueOrDefault(fix.Guid),
                            Score = scoresDb.GetValueOrDefault(fix.Guid),
                            Notes = fix.Notes,
                            IsDisabled = fix.IsDisabled,

                            Entries = hostsFix.Entries
                        };

                        baseFixEntities.Add(fileFixEntity);
                    }
                    else if (type is FixTypeEnum.TextFix)
                    {
                        TextFixEntity textFixEntity = new()
                        {
                            Name = fix.Name,
                            Version = fix.Version,
                            Guid = fix.Guid,
                            Description = fix.Description,
                            Dependencies = !deps.Any() ? null : [.. deps],
                            Tags = !tags.Any() ? null : [.. tags],
                            SupportedOSes = supportedOSes,
                            Installs = installsDb.GetValueOrDefault(fix.Guid),
                            Score = scoresDb.GetValueOrDefault(fix.Guid),
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

            var fix = dbContext.Scores.Find(fixGuid);

            int newScore;

            if (fix is null)
            {
                ScoresDbEntity newScoreEntity = new()
                {
                    FixGuid = fixGuid,
                    Score = increment
                };

                dbContext.Scores.Add(newScoreEntity);
                newScore = increment;
            }
            else
            {
                fix.Score += increment;
                newScore = fix.Score;
            }

            dbContext.SaveChanges();


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
            var fix = dbContext.Installs.Find(fixGuid);

            int newInstalls;

            if (fix is null)
            {
                InstallsDbEntity newInstallsEntity = new()
                {
                    FixGuid = fixGuid,
                    Installs = 1
                };

                dbContext.Installs.Add(newInstallsEntity);
                newInstalls = 1;
            }
            else
            {
                fix.Installs += 1;
                newInstalls = fix.Installs;
            }

            dbContext.SaveChanges();
            return newInstalls;
        }

        /// <summary>
        /// Add report for a fix
        /// </summary>
        /// <param name="fixGuid">Fix guid</param>
        /// <param name="text">Report text</param>
        public async Task AddReportAsync(Guid fixGuid, string text)
        {
            using var dbContext = _dbContextFactory.Get();

            ReportsDbEntity entity = new()
            {
                FixGuid = fixGuid,
                ReportText = text
            };

            dbContext.Reports.Add(entity);
            dbContext.SaveChanges();


            var names = (from fixx in dbContext.Fixes
                         join gamee in dbContext.Games
                         on fixx.GameId equals gamee.Id
                         where fixx.Guid == fixGuid
                         select new Tuple<string, string>(gamee.Name, fixx.Name))
                     .First();

            await _bot.SendMessageAsync($"Fix score changed: {names.Item1} - {names.Item2} text {text}");
        }
        
        /// <summary>
        /// Check if fix exists in the database
        /// </summary>
        /// <param name="fixGuid">FIx guid</param>
        /// <returns>Does fix exist</returns>
        public bool CheckIfFixExists(Guid fixGuid)
        {
            using var dbContext = _dbContextFactory.Get();
            var entity = dbContext.Fixes.Find(fixGuid);

            return entity is not null;
        }

        /// <summary>
        /// Disable or enable fix in the database
        /// </summary>
        /// <param name="fixGuid">Fix guid</param>
        /// <param name="isDisabled">Is disabled</param>
        /// <param name="password">API password</param>
        /// <returns></returns>
        public bool ChangeFixDisabledState(Guid fixGuid, bool isDisabled, string password)
        {
            var apiPassword = Environment.GetEnvironmentVariable("ApiPass")!;

            if (!apiPassword.Equals(password))
            {
                return false;
            }

            using var dbContext = _dbContextFactory.Get();
            var entity = dbContext.Fixes.Find(fixGuid);

            entity.ThrowIfNull();

            entity.IsDisabled = isDisabled;

            dbContext.SaveChanges();

            return true;
        }

        
        public async Task<bool> ForceCheckFixesAsync(string password)
        {
            var apiPassword = Environment.GetEnvironmentVariable("ApiPass")!;

            if (!apiPassword.Equals(password))
            {
                return false;
            }

            await CheckFixesAsync();

            return true;
        }

        /// <summary>
        /// Add or change fix in the database
        /// </summary>
        /// <param name="gameId">Game id</param>
        /// <param name="gameName">Game name</param>
        /// <param name="fixJson">Fix</param>
        /// <param name="password">API password</param>
        /// <returns>Is adding successful</returns>
        public async Task<bool> AddFixAsync(int gameId, string gameName, string fixJson, string password)
        {
            var apiPassword = Environment.GetEnvironmentVariable("ApiPass")!;

            if (!apiPassword.Equals(password))
            {
                return false;
            }

            var fix = JsonSerializer.Deserialize(fixJson, FixesListContext.Default.BaseFixEntity);

            fix.ThrowIfNull();

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
                //adding or modifying game
                var gameEntity = dbContext.Games.Find(gameId);

                if (gameEntity is null)
                {
                    GamesDbEntity newGameEntity = new()
                    {
                        Id = gameId,
                        Name = gameName
                    };

                    dbContext.Games.Add(newGameEntity);
                    dbContext.SaveChanges();

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

                var fixType = fix.GetType() == typeof(FileFixEntity) ? FixTypeEnum.FileFix :
                                fix.GetType() == typeof(RegistryFixEntity) ? FixTypeEnum.RegistryFix :
                                fix.GetType() == typeof(HostsFixEntity) ? FixTypeEnum.HostsFix :
                                fix.GetType() == typeof(TextFixEntity) ? FixTypeEnum.TextFix :
                                0;

                if (existingEntity is null)
                {
                    FixesDbEntity newFixEntity = new()
                    {
                        GameId = gameEntity.Id,
                        FixType = (byte)fixType,
                        Guid = fix.Guid,
                        Description = fix.Description,
                        IsLinuxSupported = fix.SupportedOSes.HasFlag(OSEnum.Linux),
                        IsWindowsSupported = fix.SupportedOSes.HasFlag(OSEnum.Windows),
                        Name = fix.Name,
                        Notes = fix.Notes,
                        Version = fix.Version,
                        IsDisabled = true
                    };

                    dbContext.Fixes.Add(newFixEntity);
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
                    existingEntity.FixType = (byte)fixType;
                    existingEntity.Description = fix.Description;
                    existingEntity.IsLinuxSupported = fix.SupportedOSes.HasFlag(OSEnum.Linux);
                    existingEntity.IsWindowsSupported = fix.SupportedOSes.HasFlag(OSEnum.Windows);
                    existingEntity.Name = fix.Name;
                    existingEntity.Notes = fix.Notes;
                    existingEntity.Version = fix.Version;
                }

                dbContext.SaveChanges();


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

                    dbContext.FileFixes.Add(newFixEntity);
                }
                else if (fix is RegistryFixEntity regFix)
                {
                    RegistryFixesDbEntity newFixEntity = new()
                    {
                        FixGuid = regFix.Guid,
                        Key = regFix.Key,
                        ValueName = regFix.ValueName,
                        ValueType = (byte)regFix.ValueType,
                        NewValueData = regFix.NewValueData
                    };

                    dbContext.RegistryFixes.Add(newFixEntity);
                }
                else if (fix is HostsFixEntity hostsFix)
                {
                    HostsFixesDbEntity newFixEntity = new()
                    {
                        FixGuid = hostsFix.Guid,
                        Entries = hostsFix.Entries
                    };

                    dbContext.HostsFixes.Add(newFixEntity);
                }
                else if (fix is TextFixEntity textFix)
                {
                    //nothing to do
                }

                dbContext.SaveChanges();


                //removing existing and adding new tags
                if (fix.Tags is not null)
                {
                    var tags = dbContext.TagsLists.Where(x => x.FixGuid == fix.Guid);

                    foreach (var tag in tags)
                    {
                        dbContext.TagsLists.Remove(tag);
                    }

                    foreach (var tag in fix.Tags.ToList())
                    {
                        var existingTag = dbContext.Tags.SingleOrDefault(x => x.Tag == tag);

                        if (existingTag is null)
                        {
                            var neww = dbContext.Tags.Add(new() { Tag = tag });
                            dbContext.SaveChanges();

                            existingTag = dbContext.Tags.SingleOrDefault(x => x.Tag == tag);
                        }

                        TagsListsDbEntity newEntity = new()
                        {
                            FixGuid = fix.Guid,
                            TagId = existingTag!.Id
                        };

                        dbContext.TagsLists.Add(newEntity);
                    }
                }

                dbContext.SaveChanges();
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
                _logger.LogInformation("Fixes check already running");
                await _bot.SendMessageAsync($"Fixes check already running");

                return;
            }

            try
            {
                _isCheckFixesRunning = true;

                _logger.LogInformation("Fixes check started");
                await _bot.SendMessageAsync($"Fixes check started");

                using var dbContext = _dbContextFactory.Get();
                var fixes = dbContext.FileFixes.AsNoTracking().Where(static x => x.Url != null);

                foreach (var fix in fixes)
                {
                    if (fix.Url is null)
                    {
                        continue;
                    }

                    var result = await _httpClient.GetAsync(fix.Url, HttpCompletionOption.ResponseHeadersRead);

                    if (result is null || !result.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Fix doesn't exist or unavailable: {fix.Url}");
                        await _bot.SendMessageAsync($"Fix doesn't exist or unavailable: {fix.Url}");
                        continue;
                    }


                    if (fix.MD5 is null)
                    {
                        _logger.LogError($"Fix doesn't have MD5 in the database: {fix.Url}");
                        await _bot.SendMessageAsync($"Fix doesn't have MD5 in the database: {fix.Url}");
                    }
                    if (fix.FileSize is null)
                    {
                        _logger.LogError($"Fix doesn't have file size in the database: {fix.Url}");
                        await _bot.SendMessageAsync($"Fix doesn't have file size in the database: {fix.Url}");
                    }


                    if (fix.Url.StartsWith(Consts.FilesBucketUrl))
                    {
                        if (result.Headers.ETag?.Tag is null)
                        {
                            _logger.LogError($"Fix doesn't have ETag: {fix.Url}");
                            await _bot.SendMessageAsync($"Fix doesn't have ETag: {fix.Url}");
                        }
                        else
                        {
                            var md5 = result.Headers.ETag!.Tag.Replace("\"", "");

                            if (md5.Contains('-'))
                            {
                                _logger.LogError($"Fix has incorrect ETag: {fix.Url}");
                                await _bot.SendMessageAsync($"Fix has incorrect ETag: {fix.Url}");
                            }
                            else if (!md5.Equals(fix.MD5, StringComparison.InvariantCultureIgnoreCase))
                            {
                                _logger.LogError($"Fix MD5 doesn't match: {fix.Url}");
                                await _bot.SendMessageAsync($"Fix MD5 doesn't match: {fix.Url}");
                            }
                        }
                    }
                    else if (result.Content.Headers.ContentMD5 is null)
                    {
                        _logger.LogError($"Fix doesn't have MD5 in the header: {fix.Url}");
                        await _bot.SendMessageAsync($"Fix doesn't have MD5 in the header: {fix.Url}");
                    }
                    else if (!BitConverter.ToString(result.Content.Headers.ContentMD5).Replace("-", string.Empty).Equals(fix.MD5, StringComparison.InvariantCultureIgnoreCase))
                    {
                        _logger.LogError($"Fix MD5 doesn't match: {fix.Url}");
                        await _bot.SendMessageAsync($"Fix MD5 doesn't match: {fix.Url}");
                    }


                    if (result.Content.Headers.ContentLength is null)
                    {
                        _logger.LogError($"Fix doesn't have size in the header: {fix.Url}");
                        await _bot.SendMessageAsync($"Fix doesn't have size in the header: {fix.Url}");
                    }
                    else if (result.Content.Headers.ContentLength != fix.FileSize)
                    {
                        _logger.LogError($"Fix size doesn't match: {fix.Url}");
                        await _bot.SendMessageAsync($"Fix size doesn't match: {fix.Url}");
                    }
                }

                _logger.LogInformation("Fixes check ended");
                await _bot.SendMessageAsync($"Fixes check ended");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Fixes check error: {ex.Message}");
                await _bot.SendMessageAsync($"Fixes check error: {ex.Message}");
            }
            finally
            {
                _isCheckFixesRunning = false;
            }
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
                dbContext.FileFixes.Remove(a);
            }

            var b = dbContext.RegistryFixes.Find(existingEntity.Guid);
            if (b is not null)
            {
                dbContext.RegistryFixes.Remove(b);
            }

            var c = dbContext.HostsFixes.Find(existingEntity.Guid);
            if (c is not null)
            {
                dbContext.HostsFixes.Remove(c);
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
                return ThrowHelper.Exception<long>($"Error while getting response for {fixUrl}: {response.StatusCode}");
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
                return ThrowHelper.Exception<string>($"Error while getting response for {fixUrl}: {response.StatusCode}");
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
}
