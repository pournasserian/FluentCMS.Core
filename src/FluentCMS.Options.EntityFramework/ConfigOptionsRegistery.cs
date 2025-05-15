namespace FluentCMS.Options.EntityFramework;

public class ConfigOptionsRegistery(IServiceProvider serviceProvider, Dictionary<Type, object> pendingConfigurations)
{
    private bool _isInitialized = false;

    public IReadOnlyDictionary<Type, object> GetDbOptions() => pendingConfigurations;

    public void InitializeOptions()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        using var scope = serviceProvider.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<ConfigOptionsDbContext>();
        dbContext.InitializeDb();

        // Get all existing options in the database
        var existingOptions = dbContext.ConfigOptions.ToList();
        var existingTypeNames = existingOptions.Select(o => o.TypeName).ToHashSet();

        // Prepare serializer options
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
        };

        // Check each registered option
        var optionsToAdd = new List<ConfigOptions>();

        foreach (var optionEntry in pendingConfigurations)
        {
            var optionType = optionEntry.Key;
            var typeName = optionType.FullName ?? optionType.Name;

            // Skip if already exists in database
            if (existingTypeNames.Contains(typeName))
            {
                continue;
            }

            object optionInstance = optionEntry.Value;

            // Serialize and prepare to add to database
            var serializedValue = JsonSerializer.Serialize(optionInstance, jsonOptions);

            optionsToAdd.Add(new ConfigOptions
            {
                TypeName = typeName,
                Value = serializedValue
            });
        }

        // Add all missing options to the database
        if (optionsToAdd.Any())
        {
            dbContext.ConfigOptions.AddRange(optionsToAdd);
            dbContext.SaveChanges();
        }
    }
}