using Common.Config;
using Common.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Helpers
{
    public static class ApiProperties
    {
        private static readonly ConfigEntity _config = BindingsManager.Provider.GetRequiredService<ConfigProvider>().Config;

        public static string ApiUrl => _config.UseLocalApiAndRepo ? "https://localhost:7093/api" : "https://superheater.fgsfds.link/api";
    }
}