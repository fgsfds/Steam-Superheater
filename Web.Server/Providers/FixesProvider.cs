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


        public FixesProvider(
            ILogger<FixesProvider> logger,
            HttpClient httpClient,
            DatabaseContextFactory dbContextFactory
            )
        {
            _httpClient = httpClient;
            _logger = logger;
            _dbContextFactory = dbContextFactory;
        }


        /// <summary>
        /// Get list of fixes from the databse
        /// </summary>
        public List<FixesList> GetFixesList()
        {
            _logger.LogInformation("Started get fixes");
            Stopwatch sw = new();
            sw.Start();

            var dbContext = _dbContextFactory.Get();

            var tagsDict = dbContext.Tags.AsNoTracking().ToDictionary(static x => x.Id, static x => x.Tag);
            var games = dbContext.Games.AsNoTracking().OrderBy(static x => x.Name).ToList();
            var dependencies = dbContext.Dependencies.AsNoTracking().ToLookup(static x => x.FixGuid, static x => x.DependencyGuid);
            var tagsIdsDb = dbContext.TagsLists.AsNoTracking().ToLookup(static x => x.FixGuid, static x => x.TagId);
            var installsDb = dbContext.Installs.AsNoTracking().ToDictionary(static x => x.FixGuid, static x => x.Installs);
            var scoresDb = dbContext.Scores.AsNoTracking().ToDictionary(static x => x.FixGuid, static x => x.Score);

            var fixesDb = dbContext.Fixes.AsNoTracking().ToLookup(static x => x.GameId);
            var fileFixesDb = dbContext.FileFixes.AsNoTracking().ToDictionary(static x => x.FixGuid);
            var regFixesDb = dbContext.RegistryFixes.AsNoTracking().ToDictionary(static x => x.FixGuid);
            var hostsFixesDb = dbContext.HostsFixes.AsNoTracking().ToDictionary(static x => x.FixGuid);

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
        public int ChangeFixScore(Guid fixGuid, sbyte increment)
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
        public void AddReport(Guid fixGuid, string text)
        {
            using var dbContext = _dbContextFactory.Get();

            ReportsDbEntity entity = new()
            {
                FixGuid = fixGuid,
                ReportText = text
            };

            dbContext.Reports.Add(entity);
            dbContext.SaveChanges();
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
        /// Disable or enable fix in the datase
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

            entity.IsDisabled = isDisabled;

            dbContext.SaveChanges();

            return true;
        }

        /// <summary>
        /// Add or change fix in the database
        /// </summary>
        /// <param name="gameId">Game id</param>
        /// <param name="gameName">Game name</param>
        /// <param name="fixJson">Fix</param>
        /// <param name="password">API password</param>
        /// <returns>Is adding successfull</returns>
        public async Task<bool> AddFixAsync(int gameId, string gameName, string fixJson, string password)
        {
            var apiPassword = Environment.GetEnvironmentVariable("ApiPass")!;

            if (!apiPassword.Equals(password))
            {
                return false;
            }

            var fix = JsonSerializer.Deserialize(fixJson, FixesListContext.Default.BaseFixEntity);

            using var dbContext = _dbContextFactory.Get();
            using (var transaction = dbContext.Database.BeginTransaction())
            {
                var existingEntity = dbContext.Fixes.Find(fix.Guid);


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
                }
                else
                {
                    gameEntity.Name = gameName;
                }


                //adding or modifying base fix
                var fixType = fix.GetType() == typeof(FileFixEntity) ? FixTypeEnum.FileFix :
                                fix.GetType() == typeof(RegistryFixEntity) ? FixTypeEnum.RegistryFix :
                                fix.GetType() == typeof(HostsFixEntity) ? FixTypeEnum.HostsFix :
                                fix.GetType() == typeof(TextFixEntity) ? FixTypeEnum.TextFix : 0;

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
                    var fileSize = fileFix.Url is null ? null : await GetFileSizeAsync(fileFix.Url).ConfigureAwait(false);
                    var fileMd5 = fileFix.Url is null ? null : await GetFileMD5Async(fileFix.Url).ConfigureAwait(false);

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
        /// Delete sub fixes from the database
        /// </summary>
        /// <param name="dbContext">DB context</param>
        /// <param name="existingEntity">Fix entity</param>
        private static void DeleteExistingSubFixes(DatabaseContext dbContext, FixesDbEntity? existingEntity)
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
        /// Get the size the local or online file
        /// </summary>
        /// <param name="client">Http client</param>
        /// <param name="fix">Fix entity</param>
        /// <returns>Size of the file in bytes</returns>
        /// <exception cref="Exception">Http response error</exception>
        private async Task<long?> GetFileSizeAsync(string fixUrl)
        {
            using var response = await _httpClient.GetAsync(fixUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

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
        /// <param name="fix">Fix entity</param>
        /// <returns>MD5 of the fix file</returns>
        /// <exception cref="Exception">Http response error</exception>
        private async Task<string> GetFileMD5Async(string fixUrl)
        {
            using var response = await _httpClient.GetAsync(fixUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return ThrowHelper.Exception<string>($"Error while getting response for {fixUrl}: {response.StatusCode}");
            }
            else if (response.Content.Headers.ContentMD5 is not null)
            {
                return BitConverter.ToString(response.Content.Headers.ContentMD5).Replace("-", string.Empty);
            }
            else
            {
                //if can't get md5 from the response, download zip
                var currentDir = Directory.GetCurrentDirectory();
                var fileName = Path.GetFileName(fixUrl);
                var pathToFile = Path.Combine(currentDir, "temp", fileName);

                if (!Directory.Exists(Path.GetDirectoryName(pathToFile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(pathToFile));
                }

                await using (FileStream file = new(pathToFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await using var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                    await source.CopyToAsync(file).ConfigureAwait(false);
                }

                string hash;

                using (var md5 = MD5.Create())
                {
                    await using var stream = File.OpenRead(pathToFile);

                    hash = Convert.ToHexString(await md5.ComputeHashAsync(stream).ConfigureAwait(false));
                }

                File.Delete(pathToFile);
                return hash;
            }
        }
    }
}
