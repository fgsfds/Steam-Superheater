using Common.Entities;
using Common.Entities.Fixes;
using Database.Server;
using Microsoft.AspNetCore.Authorization;
using Web.Blazor.Helpers;
using Web.Blazor.Providers;
using Web.Blazor.Tasks;
using Web.Blazor.Telegram;

namespace Web.Blazor;

public class Program
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
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(FixesListContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(AppReleaseEntityContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(GitHubReleaseEntityContext.Default);
            jsonOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(NewsEntityContext.Default);
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

        _ = builder.Services.AddSingleton<HttpClient>(CreateHttpClient);
        _ = builder.Services.AddSingleton<S3Client>();
        _ = builder.Services.AddSingleton<DatabaseContextFactory>(x => new(builder.Environment.IsDevelopment()));
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
            var bot = app.Services.GetService<TelegramBot>();
            _ = bot!.StartAsync();
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

