using Common.Client.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Client.DI
{
    public static class ModelsBindings
    {
        public static void Load(ServiceCollection container)
        {
            container.AddSingleton<EditorModel>();
            container.AddSingleton<MainModel>();
            container.AddSingleton<NewsModel>();
        }
    }
}
