using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderContext.Domain.Repositories;
using OrderContext.Infratructure.Repositories;

namespace OrderContext.Infratructure;

/// <summary>
/// Extension methods for registering infrastructure services with dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services including DbContext (with Sqlite), repositories, and Unit of Work.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Sqlite database connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Register DbContext with Sqlite
        services.AddDbContext<OrderDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register Repositories
        services.AddScoped<IClientRepository, ClientRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Adds infrastructure services with a custom DbContext configuration action.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsAction">The DbContext options configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        // Register DbContext with custom options
        services.AddDbContext<OrderDbContext>(optionsAction);

        // Register Repositories
        services.AddScoped<IClientRepository, ClientRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Adds infrastructure services with an in-memory database for testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="databaseName">The name of the in-memory database.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructureForTesting(
        this IServiceCollection services,
        string databaseName = "TestDb")
    {
        // Register DbContext with InMemory database
        services.AddDbContext<OrderDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        // Register Repositories
        services.AddScoped<IClientRepository, ClientRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
