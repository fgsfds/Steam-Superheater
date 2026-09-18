using Api.Axiom.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Client.DI;

/// <summary>
/// Registers the API services.
/// </summary>
public static class ApiBindings
{
    /// <summary>
    /// Registers the API implementation.
    /// </summary>
    /// <param name="container">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection WithApi(this IServiceCollection container)
    {
        //_ = container.AddSingleton<IApiInterface, ServerApiInterface>();
        _ = container.AddSingleton<IApiInterface, GitHubApiInterface>();

        return container;
    }
}
