using Common.Entities;
using Common.Entities.Fixes;
using Common.Interfaces;
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
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();

        builder.Services.AddControllers().AddJsonOptions(jsonOptions =>
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
            builder.Services.AddHostedService<AppReleasesTask>();
            builder.Services.AddHostedService<FileCheckerTask>();
        }

        builder.Services.AddHostedService<StatsTask>();

        builder.Services.AddSingleton<FixesProvider>();
        builder.Services.AddSingleton<NewsProvider>();
        builder.Services.AddSingleton<AppReleasesProvider>();
        builder.Services.AddSingleton<StatsProvider>();

        builder.Services.AddSingleton<HttpClient>(CreateHttpClient);
        builder.Services.AddSingleton<S3Client>();
        builder.Services.AddSingleton<DatabaseContextFactory>();
        builder.Services.AddSingleton<TelegramBot>();
        builder.Services.AddSingleton<IProperties, ServerProperties>();


        var app = builder.Build();


        if (builder.Environment.IsDevelopment())
        {
            var properties = app.Services.GetService<IProperties>();
            properties!.IsDevMode = true;
        }

        // Creating database
        var dbContext = app.Services.GetService<DatabaseContextFactory>()!.Get();
        dbContext.Dispose();


        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }


        app.MapControllers();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");


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
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Superheater");
        return httpClient;
    }
}

