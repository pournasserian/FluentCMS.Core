using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace FluentCMS.Configuration;


public class DatabaseConfigurationProvider(IServiceProvider serviceProvider) : ConfigurationProvider
{
    public override void Load()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OptionsDbContext>();

        try
        {
            context.Database.EnsureCreated();
            var options = context.Options.ToList();

            foreach (var option in options)
            {
                // Convert TypeName to configuration key format
                var configKey = ConvertTypeNameToConfigKey(option.TypeName);

                try
                {
                    var jsonDocument = JsonDocument.Parse(option.Value);
                    AddJsonToConfiguration(configKey, jsonDocument.RootElement);
                }
                catch (JsonException)
                {
                    // If it's not valid JSON, treat as string value
                    Data[configKey] = option.Value;
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception if needed, but don't fail the configuration loading
            Console.WriteLine($"Error loading configuration from database: {ex.Message}");
        }
    }

    private string ConvertTypeNameToConfigKey(string typeName)
    {
        // Convert "MyApp.Settings.EmailSettings" to "EmailSettings"
        var parts = typeName.Split('.');
        return parts[^1];
    }

    private void AddJsonToConfiguration(string prefix, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
                    AddJsonToConfiguration(key, property.Value);
                }
                break;
            case JsonValueKind.Array:
                for (int i = 0; i < element.GetArrayLength(); i++)
                {
                    var key = $"{prefix}:{i}";
                    AddJsonToConfiguration(key, element[i]);
                }
                break;
            default:
                Data[prefix] = element.ToString();
                break;
        }
    }
}