using Common.Interfaces;
using Superheater.Web.Server.Providers;
using Superheater.Web.Server.Tasks;
using Telegram;
using Web.Server.Helpers;

namespace Superheater.Web.Server
{
    public sealed class Server
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers().AddJsonOptions(jsonOptions =>
            {
                jsonOptions.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Don't run tasks in dev mode
            if (!builder.Environment.IsDevelopment())
            {
                builder.Services.AddHostedService<AppReleasesTask>();
                builder.Services.AddHostedService<FileCheckerTask>();
            }

            builder.Services.AddSingleton<FixesProvider>();
            builder.Services.AddSingleton<NewsProvider>();
            builder.Services.AddSingleton<AppReleasesProvider>();
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


            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.MapFallbackToFile("/index.html");


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
}
