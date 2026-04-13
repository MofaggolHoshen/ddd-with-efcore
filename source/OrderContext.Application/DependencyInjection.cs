using Microsoft.Extensions.DependencyInjection;
using OrderContext.Application.Services;

namespace OrderContext.Application;

/// <summary>
/// Extension methods for registering application services with dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds application layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register Application Services
        services.AddScoped<ClientApplicationService>();

        return services;
    }
}
