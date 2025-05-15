using Microsoft.Extensions.Options;
using System.Reflection;

namespace FluentCMS.Options.EntityFramework;

public class EntityFrameworkOptionsProvider<TOptions> : IConfigureOptions<TOptions> where TOptions : class, new()
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConfigOptionsRegistery _optionsRegistery;

    public EntityFrameworkOptionsProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _optionsRegistery = _serviceProvider.GetRequiredService<ConfigOptionsRegistery>();
        _optionsRegistery.InitializeOptions();
    }

    public void Configure(TOptions options)
    {
        // We need to use a scope because DbContext is scoped
        using var scope = _serviceProvider.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<ConfigOptionsDbContext>();
        dbContext.InitializeDb();

        var type = typeof(TOptions);
        var registeredOptions = _optionsRegistery.GetDbOptions();
        if (!registeredOptions.ContainsKey(type))
            return;

        // Try to get options from database
        var typeName = type.FullName ?? type.Name;
        var storedOptions = dbContext.ConfigOptions
            .FirstOrDefault(o => o.TypeName == typeName);

        // If options exist in the database, apply them
        if (storedOptions != null)
        {
            var dbOptions = GetValue(storedOptions.Value);
            ApplyOptions(options, dbOptions);
        }
    }

    private TOptions GetValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return new TOptions();

        return JsonSerializer.Deserialize<TOptions>(value) ?? new TOptions();
    }

    // Apply the database options to the provided options instance
    private void ApplyOptions(TOptions target, TOptions source)
    {
        // Get all properties that can be copied
        var properties = typeof(TOptions)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var property in properties)
        {
            // Handle complex nested objects
            if (property.PropertyType.IsClass &&
                property.PropertyType != typeof(string) &&
                !property.PropertyType.IsArray)
            {
                var sourceValue = property.GetValue(source);
                var targetValue = property.GetValue(target);

                // If target property is null, create a new instance
                if (targetValue == null && sourceValue != null)
                {
                    targetValue = Activator.CreateInstance(property.PropertyType);
                    property.SetValue(target, targetValue);
                }

                // If both source and target values are not null, copy properties recursively
                if (sourceValue != null && targetValue != null)
                {
                    CopyProperties(targetValue, sourceValue);
                }
            }
            else
            {
                // For simple properties, just copy the value
                var value = property.GetValue(source);
                property.SetValue(target, value);
            }
        }
    }

    // Helper method to copy properties recursively
    private void CopyProperties(object target, object source)
    {
        var properties = source.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var property in properties)
        {
            var value = property.GetValue(source);
            property.SetValue(target, value);
        }
    }
}
