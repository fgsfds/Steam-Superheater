using Common.Config;
using Common.DI;
using Common.Entities;
using Common.Entities.Fixes.HostsFix;
using Common.FixTools;
using Common.Helpers;
using Common.Providers;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;

namespace Tests
{
    /// <summary>
    /// Tests that use instance data and should be run in a single thread
    /// </summary>
    [Collection("Sync")]
    public sealed class HostsFixTests : IDisposable
    {
        private const string TestTempFolder = "test_temp";
        private readonly string _rootDirectory;
        private readonly string _testDirectory;
        private readonly string _hostsFilePath;

        private readonly FixManager _fixManager;
        private readonly InstalledFixesProvider _installedFixesProvider;

        private readonly GameEntity _gameEntity = new()
        {
            Id = 1,
            Name = "test game",
            InstallDir = "C:\\games\\test game\\",
            Icon = string.Empty
        };

        private readonly HostsFixEntity _fixEntity = new()
        {
            Name = "test fix",
            Version = 1,
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
            Entries = ["123 added entry "]
        };

        #region Test Preparations

        public HostsFixTests()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            BindingsManager.Reset();
            var container = BindingsManager.Instance;
            container.AddScoped<ConfigProvider>();
            container.AddScoped<InstalledFixesProvider>();
            container.AddScoped<FixesProvider>();
            CommonBindings.Load(container);

            _rootDirectory = Directory.GetCurrentDirectory();
            _testDirectory = Path.Combine(_rootDirectory, TestTempFolder);
            _hostsFilePath = Path.Combine(_testDirectory, "hosts");

            if (Directory.Exists(TestTempFolder))
            {
                Directory.Delete(TestTempFolder, true);
            }

            Directory.CreateDirectory(TestTempFolder);
            Directory.SetCurrentDirectory(_testDirectory);

            _fixManager = BindingsManager.Provider.GetRequiredService<FixManager>();
            _installedFixesProvider = BindingsManager.Provider.GetRequiredService<InstalledFixesProvider>();

            //create cache;
            _ = _installedFixesProvider.GetInstalledFixesListAsync().Result;

            File.Copy(
                Path.Combine(_rootDirectory, "Resources\\hosts"), 
                _hostsFilePath, 
                true
                );
        }

        public void Dispose()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            Directory.SetCurrentDirectory(_rootDirectory);

            Directory.Delete(TestTempFolder, true);
        }

        #endregion Test Preparations

        #region Tests

        [Fact]
        public async Task InstallUninstallFixTest()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            //Preparations
            var gameEntity = _gameEntity;
            var fixEntity = _fixEntity;

            //Install Fix
            await _fixManager.InstallFixAsync(gameEntity, fixEntity, null, true, new(), _hostsFilePath);

            CheckInstalled();

            _fixManager.UninstallFix(gameEntity, fixEntity, _hostsFilePath);

            CheckUninstalled();
        }

        #endregion Tests

        #region Private Methods

        private void CheckInstalled()
        {
            var hostsActual1 = File.ReadAllText(_hostsFilePath);
            var hostsExpected1 = $@"# Copyright (c) 1993-2009 Microsoft Corp.

0.0.0.0 google.com

0.0.0.0 test.site
0.0.0.0 testtesttest # comment

123 added entry  #c0650f19-f670-4f8a-8545-70f6c5171fa5";

            var instActual1 = File.ReadAllText(Consts.InstalledFile);
            var instExpected1 = $@"[
  {{
    ""$type"": ""HostsFix"",
    ""Entries"": [
      ""123 added entry ""
    ],
    ""GameId"": 1,
    ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa5"",
    ""Version"": 1
  }}
]";

            Assert.Equal(hostsActual1, hostsExpected1);
            Assert.Equal(instActual1, instExpected1);
        }

        private void CheckUninstalled()
        {
            var hostsActual2 = File.ReadAllText(_hostsFilePath);
            var hostsExpected2 = $@"# Copyright (c) 1993-2009 Microsoft Corp.

0.0.0.0 google.com

0.0.0.0 test.site
0.0.0.0 testtesttest # comment
";

            var instActual2 = File.ReadAllText(Consts.InstalledFile);
            var instExpected2 = $@"[]";

            Assert.Equal(hostsActual2, hostsExpected2);
            Assert.Equal(instActual2, instExpected2);
        }

        #endregion Private Methods
    }
}