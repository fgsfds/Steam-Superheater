using Api.Axiom.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Client.DI;

public static class ApiBindings
{
    public static void Load(ServiceCollection container)
    {
        //_ = container.AddSingleton<IApiInterface, ServerApiInterface>();
        _ = container.AddSingleton<IApiInterface, GitHubApiInterface>();
    }
}
