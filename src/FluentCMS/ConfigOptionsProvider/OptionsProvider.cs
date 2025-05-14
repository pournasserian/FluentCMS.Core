using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FluentCMS.ConfigOptionsProvider;

// Custom options provider that reads from SQLite
public class OptionsProvider<TOptions>(IOptionRepository optionRepository, IOptions<TOptions> defaultOptions, IConfiguration configuration) : IConfigureOptions<TOptions> where TOptions : class, new()
{
    public void Configure(TOptions options)
    {
        // First, check if options are defined in appsettings.json
        var configSection = configuration.GetSection(typeof(TOptions).Name);
        if (configSection.Exists())
        {
            // Use options from appsettings.json
            options = new TOptions();
            configSection.Bind(options);
            return;
        }

        string typeFullName = typeof(TOptions).FullName ??
            throw new InvalidOperationException($"Type fullname for {typeof(TOptions).Name} cannot be null");

        // Try to get options from database
        var optionEntry = optionRepository.GetByTypeName(typeFullName).GetAwaiter().GetResult();

        if (optionEntry == null)
        {
            // Insert default options if they don't exist
            var defaultValue = defaultOptions.Value;
            var jsonOptions = JsonSerializer.Serialize(defaultValue);

            optionEntry = new OptionEntry
            {
                TypeName = typeFullName,
                Value = jsonOptions
            };

            optionRepository.Add(optionEntry).GetAwaiter().GetResult();

            // Copy default values to the options instance
            CopyValues(defaultValue, options);
        }
        else
        {
            // Deserialize options from database
            var dbOptions = JsonSerializer.Deserialize<TOptions>(optionEntry.Value) ??
                throw new InvalidOperationException($"Failed to deserialize options for {typeFullName}");

            // Copy values from database to the options instance
            CopyValues(dbOptions, options);
        }
    }

    private void CopyValues(TOptions source, TOptions destination)
    {
        // Serialize and deserialize to handle nested properties
        var json = JsonSerializer.Serialize(source);
        var tempOptions = JsonSerializer.Deserialize<TOptions>(json);

        // Copy all properties from tempOptions to destination
        var properties = typeof(TOptions).GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(tempOptions);
            property.SetValue(destination, value);
        }
    }
}
