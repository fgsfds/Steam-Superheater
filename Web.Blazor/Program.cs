using Api.Common.Messages;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Providers;
using Database.Server;
using Microsoft.AspNetCore.Authorization;
using Web.Blazor.Helpers;
using Web.Blazor.Providers;
using Web.Blazor.Tasks;
using Web.Blazor.Telegram;

namespace Web.Blazor;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        _ = builder.Services.AddRazorPages();
        _ = builder.Services.AddServerSideBlazor();

        _ = builder.Services.AddControllers().AddJsonOptions(jsonOptions =>
        {
            jsonOptions.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = null;

            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(SourceEntityContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(AppReleaseEntityContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(GitHubReleaseEntityContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(NewsListEntityContext.Default);

            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(AddChangeNewsInMessageContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(AddFixInMessageContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(ChangeFixStateInMessageContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(ChangeScoreInMessageContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(ChangeScoreOutMessageContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(CheckIfFixExistsOutMessageContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(DatabaseVersionsOutMessageContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(GetFixesInMessageContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(GetFixesOutMessageContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(GetFixesStatsOutMessageContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(GetNewsOutMessageContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(GetReleasesOutMessageContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(IncreaseInstallsCountInMessageContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(IncreaseInstallsCountOutMessageContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(ReportFixInMessageContext.Default);
        });

        // Don't run tasks in dev mode
        if (!builder.Environment.IsDevelopment())
        {
            _ = builder.Services.AddHostedService<AppReleasesTask>();
            _ = builder.Services.AddHostedService<FileCheckerTask>();
        }

        _ = builder.Services.AddHostedService<StatsTask>();

        _ = builder.Services.AddSingleton<FixesProvider>();
        _ = builder.Services.AddSingleton<NewsProvider>();
        _ = builder.Services.AddSingleton<AppReleasesProvider>();
        _ = builder.Services.AddSingleton<StatsProvider>();
        _ = builder.Services.AddSingleton<DatabaseVersionsProvider>();
        _ = builder.Services.AddSingleton<EventsProvider>();

        _ = builder.Services.AddSingleton(CreateHttpClient);
        _ = builder.Services.AddSingleton<S3Client>();
        _ = builder.Services.AddSingleton((Func<IServiceProvider, DatabaseContextFactory>)(_ => new(builder.Environment.IsDevelopment())));
        _ = builder.Services.AddSingleton<TelegramBot>();
        _ = builder.Services.AddSingleton<ServerProperties>();

        _ = builder.Services.AddSingleton<IAuthorizationHandler, AuthorizationHandler>();


        var app = builder.Build();

        var properties = app.Services.GetService<ServerProperties>()!;

        if (builder.Environment.IsDevelopment())
        {
            properties!.IsDevMode = true;
        }

        // Creating database
        var dbContext = app.Services.GetService<DatabaseContextFactory>()!.Get();
        dbContext.Dispose();


        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            _ = app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            _ = app.UseHsts();
        }

        _ = app.MapControllers();
        _ = app.UseHttpsRedirection();
        _ = app.UseStaticFiles();
        _ = app.UseRouting();
        _ = app.UseAuthorization();
        _ = app.MapBlazorHub();
        _ = app.MapFallbackToPage("/_Host");


        // Don't start bot in dev mode
        if (!app.Environment.IsDevelopment())
        {
            _ = app.Services.GetService<TelegramBot>()!.StartAsync();
        }

        app.Run();
    }

    private static HttpClient CreateHttpClient(IServiceProvider provider)
    {
        HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Superheater");
        return httpClient;
    }
}

