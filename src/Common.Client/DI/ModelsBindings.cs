using Common.Client.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Client.DI;

public static class ModelsBindings
{
    public static void Load(ServiceCollection container)
    {
        _ = container.AddSingleton<EditorModel>();
        _ = container.AddSingleton<MainModel>();
    }
}

