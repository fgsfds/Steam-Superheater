using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.TextFix;
using Common.Enums;
using Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Web.Server.DbEntities;

namespace Web.Server.Database
{
    public sealed class DatabaseContext : DbContext
    {
        private static bool _isRunOnce = false;
        private readonly IProperties _properties;

        public DbSet<InstallsDbEntity> Installs { get; set; }
        public DbSet<ScoresDbEntity> Scores { get; set; }
        public DbSet<ReportsDbEntity> Reports { get; set; }
        public DbSet<NewsDbEntity> News { get; set; }
        public DbSet<GamesDbEntity> Games { get; set; }
        public DbSet<FixesDbEntity> Fixes { get; set; }
        public DbSet<TagsDbEntity> Tags { get; set; }
        public DbSet<TagsListsDbEntity> TagsLists { get; set; }
        public DbSet<DependenciesDbEntity> Dependencies { get; set; }
        public DbSet<FixTypeDbEntity> FixTypes { get; set; }
        public DbSet<HostsFixesDbEntity> HostsFixes { get; set; }
        public DbSet<RegistryValueTypeDbEntity> RegistryValueType { get; set; }
        public DbSet<RegistryFixesDbEntity> RegistryFixes { get; set; }
        public DbSet<FileFixesDbEntity> FileFixes { get; set; }

        public DatabaseContext(IProperties properties)
        {
            _properties = properties;

            if (_properties.IsDevMode)
            {
                if (!_isRunOnce)
                {
                    //Database.EnsureDeleted();
                    _isRunOnce = true;
                }
            }

            Database.EnsureCreated();

            if (Fixes is null || !Fixes.Any())
            {
                FillDb();
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_properties.IsDevMode)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=superheater;Username=postgres;Password=123;Include Error Detail=True");
            }
            else
            {
                var dbip = Environment.GetEnvironmentVariable("DbIp")!;
                var dbport = Environment.GetEnvironmentVariable("DbPort")!;
                var user = Environment.GetEnvironmentVariable("DbUser")!;
                var password = Environment.GetEnvironmentVariable("DbPass")!;
                optionsBuilder.UseNpgsql($"Host={dbip};Port={dbport};Database=superheater;Username={user};Password={password}");
            }
        }

        [Obsolete]
        private bool FillDb()
        {
            try
            {
                using var httpClient = new HttpClient();
                var news = httpClient.GetFromJsonAsync<List<NewsEntity>>("https://s3.timeweb.cloud/b70f50a9-files/superheater/news.json").Result;
                var fixes = httpClient.GetStringAsync("https://s3.timeweb.cloud/b70f50a9-files/superheater/fixes.json").Result;
                var fixesList = JsonSerializer.Deserialize(fixes, FixesListContext.Default.ListFixesList);


                //NEWS
                foreach (var nn in news)
                {
                    News.Add(new() { Content = nn.Content, Date = nn.Date.ToUniversalTime() });
                }

                this.SaveChanges();


                //GAMES
                foreach (var game in fixesList)
                {
                    Games.Add(new() { Id = game.GameId, Name = game.GameName });
                }

                this.SaveChanges();


                //FIXTYPES
                FixTypes.Add(new() { Id = 1, Type = "File fix" });
                FixTypes.Add(new() { Id = 2, Type = "Registry fix" });
                FixTypes.Add(new() { Id = 3, Type = "Hosts fix" });
                FixTypes.Add(new() { Id = 4, Type = "Text fix" });

                RegistryValueType.Add(new() { Id = 1, Type = "String" });
                RegistryValueType.Add(new() { Id = 2, Type = "Dword" });

                this.SaveChanges();


                //TAGS
                List<TagsDbEntity> tags = new();

                foreach (var game in fixesList)
                {
                    foreach (var fix in game.Fixes)
                    {
                        if (fix.Tags is not null)
                        {
                            foreach (var tag in fix.Tags)
                            {
                                if (!tags.Any(x => x.Tag == tag))
                                {
                                    tags.Add(new() { Tag = tag });
                                }
                            }
                        }
                    }
                }

                Tags.AddRange(tags);
                this.SaveChanges();


                //FIXES
                foreach (var game in fixesList)
                {
                    foreach (var fix in game.Fixes)
                    {
                        var fixType = FixTypeEnum.FileFix;

                        if (fix is FileFixEntity)
                        {
                            fixType = FixTypeEnum.FileFix;
                        }
                        else if (fix is RegistryFixEntity)
                        {
                            fixType = FixTypeEnum.RegistryFix;
                        }
                        else if (fix is HostsFixEntity)
                        {
                            fixType = FixTypeEnum.HostsFix;
                        }
                        else if (fix is TextFixEntity)
                        {
                            fixType = FixTypeEnum.TextFix;
                        }

                        Fixes.Add(new()
                        {
                            Guid = fix.Guid,
                            Name = fix.Name,
                            Version = fix.Version,
                            IsWindowsSupported = fix.SupportedOSes.HasFlag(OSEnum.Windows),
                            IsLinuxSupported = fix.SupportedOSes.HasFlag(OSEnum.Linux),
                            Description = fix.Description,
                            Notes = fix.Notes,
                            GameId = game.GameId,
                            FixType = (byte)fixType
                        });
                    }
                }

                this.SaveChanges();


                //TYPED FIXES
                foreach (var game in fixesList)
                {
                    foreach (var fix in game.Fixes)
                    {
                        if (fix is FileFixEntity fFix)
                        {
                            FileFixesDbEntity entity = new()
                            {
                                FixGuid = fFix.Guid,
                                Url = fFix.Url,
                                FileSize = fFix.FileSize,
                                MD5 = fFix.MD5,
                                InstallFolder = fFix.InstallFolder,
                                ConfigFile = fFix.ConfigFile,
                                FilesToDelete = fFix.FilesToDelete,
                                FilesToBackup = fFix.FilesToBackup,
                                FilesToPatch = fFix.FilesToPatch,
                                RunAfterInstall = fFix.RunAfterInstall,
                                Variants = fFix.Variants,
                                SharedFixGuid = fFix.SharedFixGuid,
                                SharedFixInstallFolder = fFix.SharedFixInstallFolder,
                                WineDllOverrides = fFix.WineDllOverrides
                            };

                            FileFixes.Add(entity);
                        }
                        else if (fix is RegistryFixEntity rFix)
                        {
                            RegistryFixesDbEntity entity = new()
                            {
                                FixGuid = rFix.Guid,
                                Key = rFix.Key,
                                ValueName = rFix.ValueName,
                                NewValueData = rFix.NewValueData,
                                ValueType = (byte)rFix.ValueType
                            };

                            RegistryFixes.Add(entity);
                        }
                        else if (fix is HostsFixEntity hFix)
                        {
                            HostsFixesDbEntity entity = new()
                            { 
                                FixGuid = hFix.Guid,
                                Entries = hFix.Entries
                            };

                            HostsFixes.Add(entity);
                        }
                    }
                }

                this.SaveChanges();


                //TAGSLISTS
                foreach (var game in fixesList)
                {
                    foreach (var fix in game.Fixes)
                    {
                        var tagsList = fix.Tags;

                        if (tagsList is null)
                        {
                            continue;
                        }

                        foreach (var tag in tagsList)
                        {
                            var tagId = Tags.First(x => x.Tag == tag);

                            TagsListsDbEntity entiry = new()
                            {
                                FixGuid = fix.Guid,
                                TagId = tagId.Id
                            };

                            TagsLists.Add(entiry);
                        }
                    }
                }

                this.SaveChanges();


                //DEPENDENCIES
                foreach (var game in fixesList)
                {
                    foreach (var fix in game.Fixes)
                    {
                        var dependencies = fix.Dependencies;

                        if (dependencies is null)
                        {
                            continue;
                        }

                        foreach (var dep in dependencies)
                        {
                            DependenciesDbEntity entiry = new()
                            {
                                FixGuid = fix.Guid,
                                DependencyGuid = dep
                            };

                            Dependencies.Add(entiry);
                        }
                    }
                }

                this.SaveChanges();


                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
