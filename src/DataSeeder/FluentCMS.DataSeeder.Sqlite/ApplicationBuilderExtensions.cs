using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentCMS.DataSeeder.Sqlite;


/// <summary>
/// Extension methods for IApplicationBuilder to integrate database seeding middleware
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds database seeding middleware to the application pipeline
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type</typeparam>
    /// <param name="app">The application builder</param>
    /// <param name="runOnStartup">Whether to run seeding on application startup</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseDatabaseSeeding<TDbContext>(
        this IApplicationBuilder app,
        bool runOnStartup = true)
        where TDbContext : DbContext
    {
        if (runOnStartup)
        {
            // Execute seeding on startup
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TDbContext>();
            var seedingService = scope.ServiceProvider.GetRequiredService<ISeedingService>();
            var logger = scope.ServiceProvider.GetService<ILogger<TDbContext>>();

            try
            {
                seedingService.ExecuteSeeding(context).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to execute database seeding on startup");
                throw;
            }
        }

        return app;
    }

    /// <summary>
    /// Adds database seeding middleware with manual execution control
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="seedingAction">Action to execute for seeding</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseDatabaseSeeding(
        this IApplicationBuilder app,
        Func<IServiceProvider, Task> seedingAction)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var logger = scope.ServiceProvider.GetService<ILogger>();

        try
        {
            seedingAction(scope.ServiceProvider).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to execute custom database seeding on startup");
            throw;
        }

        return app;
    }

    /// <summary>
    /// Executes database seeding for multiple DbContext types
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="contextTypes">The DbContext types to seed</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseDatabaseSeeding(
        this IApplicationBuilder app,
        params Type[] contextTypes)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var seedingService = scope.ServiceProvider.GetRequiredService<ISeedingService>();
        var logger = scope.ServiceProvider.GetService<ILogger>();

        foreach (var contextType in contextTypes)
        {
            if (!typeof(DbContext).IsAssignableFrom(contextType))
            {
                logger?.LogWarning("Type {ContextType} is not a DbContext, skipping seeding", contextType.Name);
                continue;
            }

            try
            {
                var context = scope.ServiceProvider.GetRequiredService(contextType) as DbContext;
                if (context != null)
                {
                    seedingService.ExecuteSeeding(context).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to execute database seeding for context {ContextType}", contextType.Name);
                throw;
            }
        }

        return app;
    }
}
