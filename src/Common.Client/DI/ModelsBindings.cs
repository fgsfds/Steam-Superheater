using Common.Client.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Client.DI;

/// <summary>
/// Registers the client models.
/// </summary>
public static class ModelsBindings
{
    /// <summary>
    /// Registers the client models.
    /// </summary>
    /// <param name="container">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection WithModels(this IServiceCollection container)
    {
        _ = container.AddSingleton<EditorModel>();
        _ = container.AddSingleton<MainModel>();

        return container;
    }
}
