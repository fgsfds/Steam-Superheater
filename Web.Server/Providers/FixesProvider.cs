using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Enums;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Web.Server.DbEntities;
using Web.Server.Helpers;

namespace Superheater.Web.Server.Providers
{
    public sealed class FixesProvider
    {
        private readonly DatabaseContextFactory _dbContextFactory;
        private readonly ILogger<FixesProvider> _logger;


        public FixesProvider(
            ILogger<FixesProvider> logger,
            DatabaseContextFactory dbContextFactory
            )
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
        }


        public List<FixesList> GetFixesList()
        {
            _logger.LogInformation("Started get fixes");
            Stopwatch sw = new();
            sw.Start();

            using var dbContext = _dbContextFactory.Get();

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

                            Url = fileFix!.Url,
                            FileSize = fileFix.FileSize,
                            InstallFolder = fileFix.InstallFolder,
                            ConfigFile = fileFix.ConfigFile,
                            FilesToDelete = fileFix.FilesToDelete,
                            FilesToBackup = fileFix.FilesToBackup,
                            FilesToPatch = fileFix.FilesToPatch,
                            RunAfterInstall = fileFix.RunAfterInstall,
                            MD5 = fileFix.MD5,
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

                            Entries = hostsFix.Entries
                        };

                        baseFixEntities.Add(fileFixEntity);
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

        public int ChangeFixScore(Guid fixGuid, sbyte score)
        {
            using var dbContext = _dbContextFactory.Get();
            var fix = dbContext.Scores.Find(fixGuid);

            int newScore;

            if (fix is null)
            {
                ScoresDbEntity newScoreEntity = new()
                { 
                    FixGuid = fixGuid,
                    Score = score 
                };

                dbContext.Scores.Add(newScoreEntity);
                newScore = score;
            }
            else
            {
                fix.Score += score;
                newScore = fix.Score;
            }

            dbContext.SaveChanges();
            return newScore;
        }

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
        
        public bool CheckIfFixExists(Guid fixGuid)
        {
            using var dbContext = _dbContextFactory.Get();
            var entity = dbContext.Fixes.Find(fixGuid);

            return entity is not null;
        }
    }
}
