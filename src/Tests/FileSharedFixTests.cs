using Common.Entities.Fixes.FileFix;
using Common.Helpers;

namespace Tests
{
    /// <summary>
    /// Tests that use instance data and should be run in a single thread
    /// </summary>
    public sealed partial class FileFixTests : IDisposable
    {
        private const string TestFixSharedZip = "test_fix_shared.zip";
        private const string TestFixShared2Zip = "test_fix_shared_v2.zip";

        #region Tests

        /// <summary>
        /// Install, update and uninstall fix that includes shared fix, only shared fix is updated
        /// </summary>
        [Fact]
        public async Task SharedUpdateSharedFix()
        {
            FileFixEntity sharedFixEntity = new()
            {
                Name = "shared test fix",
                Version = 1,
                Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA6"),
                Url = TestFixSharedZip
            };

            FileFixEntity fixEntity = new()
            {
                Name = "test fix",
                Version = 1,
                Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
                Url = TestFixZip,
                InstallFolder = "install folder",
                SharedFix = sharedFixEntity,
                SharedFixGuid = sharedFixEntity.Guid,
                SharedFixInstallFolder = "shared install folder"
            };

            await InstallAsync(fixEntity);

            await UpdateSharedFixAsync(fixEntity);
            
            Uninstall(fixEntity);
        }

        /// <summary>
        /// Install, update and uninstall fix that includes shared fix, only main fix is updated
        /// </summary>
        [Fact]
        public async Task SharedUpdateMainFix()
        {
            FileFixEntity sharedFixEntity = new()
            {
                Name = "shared test fix",
                Version = 1,
                Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA6"),
                Url = TestFixSharedZip
            };

            FileFixEntity fixEntity = new()
            {
                Name = "test fix",
                Version = 1,
                Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
                Url = TestFixZip,
                InstallFolder = "install folder",
                SharedFix = sharedFixEntity,
                SharedFixGuid = sharedFixEntity.Guid,
                SharedFixInstallFolder = "shared install folder"
            };

            await InstallAsync(fixEntity);

            await UpdateMainFixAsync(fixEntity);
            
            Uninstall(fixEntity);
        }

        #endregion Tests

        private async Task InstallAsync(FileFixEntity fixEntity)
        {
            //install
            File.Copy(
                Path.Combine(_rootDirectory, $"Resources\\{TestFixZip}"),
                Path.Combine(_rootDirectory, TestFixZip),
                true
                );
            File.Copy(
                Path.Combine(_rootDirectory, $"Resources\\{TestFixSharedZip}"),
                Path.Combine(_rootDirectory, TestFixSharedZip),
                true
                );

            await _fixManager.InstallFixAsync(_gameEntity, fixEntity, null, true);

            Assert.True(File.Exists("game\\shared install folder\\shared fix file.txt"));

            var installedActual = File.ReadAllText(Consts.InstalledFile);
            var installedExpected = $@"[
  {{
    ""$type"": ""FileFix"",
    ""BackupFolder"": ""test_fix"",
    ""FilesList"": [
      ""install folder\\start game.exe"",
      ""install folder\\subfolder\\file.txt""
    ],
    ""InstalledSharedFix"": {{
      ""BackupFolder"": null,
      ""FilesList"": [
        ""shared install folder\\"",
        ""shared install folder\\shared fix file.txt""
      ],
      ""InstalledSharedFix"": null,
      ""WineDllOverrides"": null,
      ""GameId"": 1,
      ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa6"",
      ""Version"": 1
    }},
    ""WineDllOverrides"": null,
    ""GameId"": 1,
    ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa5"",
    ""Version"": 1
  }}
]";

            Assert.Equal(installedActual, installedExpected);
        }

        private async Task UpdateMainFixAsync(FileFixEntity fixEntity)
        {
            fixEntity.Version = 2;
            fixEntity.Url = TestFixV2Zip;

            //update
            File.Copy(
                Path.Combine(_rootDirectory, $"Resources\\{TestFixSharedZip}"),
                Path.Combine(_rootDirectory, TestFixSharedZip),
                true
                );
            File.Copy(
                Path.Combine(_rootDirectory, $"Resources\\{TestFixV2Zip}"),
                Path.Combine(_rootDirectory, TestFixV2Zip),
                true
                );

            await _fixManager.UpdateFixAsync(_gameEntity, fixEntity, null, true);

            Assert.True(File.Exists("game\\shared install folder\\shared fix file.txt"));

            var newFileActual = File.ReadAllText("game\\install folder\\start game.exe");
            var newFileExpected = "fix_v2";

            var installedActual = File.ReadAllText(Consts.InstalledFile);
            var installedExpected = $@"[
  {{
    ""$type"": ""FileFix"",
    ""BackupFolder"": ""test_fix"",
    ""FilesList"": [
      ""install folder\\start game.exe""
    ],
    ""InstalledSharedFix"": {{
      ""BackupFolder"": null,
      ""FilesList"": [
        ""shared install folder\\"",
        ""shared install folder\\shared fix file.txt""
      ],
      ""InstalledSharedFix"": null,
      ""WineDllOverrides"": null,
      ""GameId"": 1,
      ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa6"",
      ""Version"": 1
    }},
    ""WineDllOverrides"": null,
    ""GameId"": 1,
    ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa5"",
    ""Version"": 2
  }}
]";

            Assert.Equal(newFileActual, newFileExpected);
            Assert.Equal(installedActual, installedExpected);
        }

        private async Task UpdateSharedFixAsync(FileFixEntity fixEntity)
        {
            FileFixEntity sharedFixEntity2 = new()
            {
                Name = "shared test fix",
                Version = 2,
                Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA6"),
                Url = TestFixShared2Zip
            };

            fixEntity.SharedFix = sharedFixEntity2;

            //update
            File.Copy(
                Path.Combine(_rootDirectory, $"Resources\\{TestFixZip}"),
                Path.Combine(_rootDirectory, TestFixZip),
                true
                );
            File.Copy(
                Path.Combine(_rootDirectory, $"Resources\\{TestFixShared2Zip}"),
                Path.Combine(_rootDirectory, TestFixShared2Zip),
                true
                );

            await _fixManager.UpdateFixAsync(_gameEntity, fixEntity, null, true);

            Assert.True(File.Exists("game\\shared install folder\\shared fix file 2.txt"));

            var installedActual = File.ReadAllText(Consts.InstalledFile);
            var installedExpected = $@"[
  {{
    ""$type"": ""FileFix"",
    ""BackupFolder"": ""test_fix"",
    ""FilesList"": [
      ""install folder\\start game.exe"",
      ""install folder\\subfolder\\file.txt""
    ],
    ""InstalledSharedFix"": {{
      ""BackupFolder"": null,
      ""FilesList"": [
        ""shared install folder\\"",
        ""shared install folder\\shared fix file 2.txt"",
        ""shared install folder\\shared fix file.txt""
      ],
      ""InstalledSharedFix"": null,
      ""WineDllOverrides"": null,
      ""GameId"": 1,
      ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa6"",
      ""Version"": 2
    }},
    ""WineDllOverrides"": null,
    ""GameId"": 1,
    ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa5"",
    ""Version"": 1
  }}
]";

            Assert.Equal(installedActual, installedExpected);
        }

        private void Uninstall(FileFixEntity fixEntity)
        {
            //uninstall
            _fixManager.UninstallFix(_gameEntity, fixEntity);

            Assert.False(Directory.Exists("game\\shared install folder\\"));

            CheckOriginalFiles();

            var installedActual = File.ReadAllText(Consts.InstalledFile);
            var installedExpected = $@"[]";

            Assert.Equal(installedActual, installedExpected);
        }
    }
}