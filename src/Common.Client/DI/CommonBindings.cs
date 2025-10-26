using Api.Axiom.Interface;
using Common.Axiom.Helpers;
using Common.Client.FilesTools;
using Common.Client.FilesTools.Interfaces;
using Common.Client.FixTools;
using Common.Client.FixTools.FileFix;
using Common.Client.FixTools.HostsFix;
using Common.Client.FixTools.RegistryFix;
using Downloader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Common.Client.DI;

public static class CommonBindings
{
    public static void Load(ServiceCollection container, bool isDesigner)
    {
        if (!isDesigner)
        {
            _ = container.AddSingleton<ILogger>(CreateLogger);
        }

        _ = container.AddTransient<AppUpdateInstaller>();

        _ = container.AddTransient<FileFixInstaller>();
        _ = container.AddTransient<FileFixUpdater>();
        _ = container.AddTransient<FileFixUninstaller>();
        _ = container.AddTransient<FileFixChecker>();

        _ = container.AddTransient<RegistryFixInstaller>();
        _ = container.AddTransient<RegistryFixUpdater>();
        _ = container.AddTransient<RegistryFixUninstaller>();

        _ = container.AddTransient<HostsFixInstaller>();
        _ = container.AddTransient<HostsFixUpdater>();
        _ = container.AddTransient<HostsFixUninstaller>();

        _ = container.AddTransient<FixManager>();
        _ = container.AddTransient<ArchiveTools>();

        _ = container.AddSingleton<IFilesDownloader, FilesDownloader>();
        _ = container.AddSingleton<FilesUploader>();
        _ = container.AddSingleton<ProgressReport>();
        _ = container.AddSingleton<HttpClient>(CreateHttpClient);
        _ = container.AddSingleton<DownloadService>(CreateDownloadService);
        _ = container.AddSingleton<ISteamTools, SteamTools>();
        //_ = container.AddSingleton<IApiInterface, ServerApiInterface>();
        _ = container.AddSingleton<IApiInterface, FileApiInterface>();

    }

    private static HttpClient CreateHttpClient(IServiceProvider provider)
    {
        HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Superheater");
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        return httpClient;
    }

    private static DownloadService CreateDownloadService(IServiceProvider provider)
    {
        var conf = new DownloadConfiguration()
        {
            //MaximumMemoryBufferBytes = 1024 * 1024 * 512,
            ParallelDownload = true,
            ChunkCount = 4,
            ParallelCount = 4,
            MaximumBytesPerSecond = 0,
            MaxTryAgainOnFailure = 5,
            Timeout = 1000,
            RangeDownload = false,
            ClearPackageOnCompletionWithFailure = true,
            CheckDiskSizeBeforeDownload = true,
            EnableLiveStreaming = false,
            RequestConfiguration =
            {
                KeepAlive = true,
                UserAgent = "Superheater"
            }
        };

        return new DownloadService(conf);
    }

    private static ILogger CreateLogger(IServiceProvider service) => FileLoggerFactory.Create(ClientProperties.PathToLogFile);
}

