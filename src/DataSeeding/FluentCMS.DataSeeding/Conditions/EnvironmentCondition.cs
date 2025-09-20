using System;
using System.Threading;
using System.Threading.Tasks;
using FluentCMS.DataSeeding.Abstractions;
using FluentCMS.DataSeeding.Models;

namespace FluentCMS.DataSeeding.Conditions;

/// <summary>
/// A condition that evaluates based on the current hosting environment.
/// Commonly used to prevent seeding in production environments.
/// </summary>
public class EnvironmentCondition : ICondition
{
    private readonly Func<string, bool> _environmentPredicate;

    /// <summary>
    /// Initializes a new instance of EnvironmentCondition with a predicate function.
    /// </summary>
    /// <param name="environmentPredicate">Function that takes environment name and returns whether seeding should proceed</param>
    public EnvironmentCondition(Func<string, bool> environmentPredicate)
    {
        _environmentPredicate = environmentPredicate ?? throw new ArgumentNullException(nameof(environmentPredicate));
    }

    /// <summary>
    /// Creates a condition that allows seeding only in Development environment.
    /// </summary>
    /// <returns>A condition that passes only in Development</returns>
    public static EnvironmentCondition DevelopmentOnly()
    {
        return new EnvironmentCondition(env => 
            string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates a condition that allows seeding in Development and Staging environments.
    /// </summary>
    /// <returns>A condition that passes in Development and Staging</returns>
    public static EnvironmentCondition DevelopmentAndStaging()
    {
        return new EnvironmentCondition(env => 
            string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(env, "Staging", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates a condition that prevents seeding in Production environment.
    /// </summary>
    /// <returns>A condition that passes in all environments except Production</returns>
    public static EnvironmentCondition NotProduction()
    {
        return new EnvironmentCondition(env => 
            !string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates a condition that allows seeding in specific named environments.
    /// </summary>
    /// <param name="allowedEnvironments">Array of environment names where seeding is allowed</param>
    /// <returns>A condition that passes only in the specified environments</returns>
    public static EnvironmentCondition OnlyIn(params string[] allowedEnvironments)
    {
        if (allowedEnvironments == null || allowedEnvironments.Length == 0)
            throw new ArgumentException("At least one environment must be specified", nameof(allowedEnvironments));

        return new EnvironmentCondition(env =>
        {
            foreach (var allowed in allowedEnvironments)
            {
                if (string.Equals(env, allowed, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        });
    }

    /// <summary>
    /// Evaluates whether seeding should proceed based on the current environment.
    /// </summary>
    /// <param name="context">The seeding context providing access to services</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if seeding should proceed, false otherwise</returns>
    public Task<bool> ShouldExecute(SeedingContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get environment from any available environment service
            // This uses reflection to avoid compile-time dependencies
            var environmentName = GetEnvironmentFromServices(context) 
                               ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                               ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                               ?? "Production"; // Default to Production for safety

            return Task.FromResult(_environmentPredicate(environmentName));
        }
        catch (Exception)
        {
            // If we can't determine environment, default to not executing for safety
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Attempts to get environment name from available services using reflection.
    /// This avoids compile-time dependencies on hosting abstractions.
    /// </summary>
    private static string? GetEnvironmentFromServices(SeedingContext context)
    {
        try
        {
            // Try IWebHostEnvironment (ASP.NET Core)
            var webHostEnv = GetServiceByTypeName(context, "Microsoft.AspNetCore.Hosting.IWebHostEnvironment");
            if (webHostEnv != null)
            {
                var envNameProperty = webHostEnv.GetType().GetProperty("EnvironmentName");
                if (envNameProperty != null)
                {
                    return envNameProperty.GetValue(webHostEnv) as string;
                }
            }

            // Try IHostEnvironment (.NET Generic Host)
            var hostEnv = GetServiceByTypeName(context, "Microsoft.Extensions.Hosting.IHostEnvironment");
            if (hostEnv != null)
            {
                var envNameProperty = hostEnv.GetType().GetProperty("EnvironmentName");
                if (envNameProperty != null)
                {
                    return envNameProperty.GetValue(hostEnv) as string;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a service by type name using reflection to avoid compile-time dependencies.
    /// </summary>
    private static object? GetServiceByTypeName(SeedingContext context, string typeName)
    {
        try
        {
            // Look through loaded assemblies for the type
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    // Use reflection to call GetService<T> method
                    var getServiceMethod = context.GetType().GetMethod("GetService");
                    if (getServiceMethod != null)
                    {
                        var genericMethod = getServiceMethod.MakeGenericMethod(type);
                        return genericMethod.Invoke(context, null);
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
