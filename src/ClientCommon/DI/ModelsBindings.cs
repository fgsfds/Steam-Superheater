using ClientCommon.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ClientCommon.DI
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
