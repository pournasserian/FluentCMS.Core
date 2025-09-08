using System.Text.Json;

namespace FluentCMS.Configuration.EntityFramework;

public interface ISettingService
{
    Task<IEnumerable<string>> GetAllKeys(CancellationToken cancellationToken = default);
    Task<T?> Get<T>(string key, CancellationToken cancellationToken = default);
    Task<T?> Update<T>(string key, T? value, CancellationToken cancellationToken = default);
}

public class SettingService(ISettingRepository repository) : ISettingService
{
    public Task<IEnumerable<string>> GetAllKeys(CancellationToken cancellationToken = default)
    {
        return repository.GetAll(cancellationToken)
            .ContinueWith(t => t.Result.Select(s => s.Key), cancellationToken);
    }

    public Task<T?> Get<T>(string key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<T?> Update<T>(string key, T? value, CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetAll(cancellationToken);
        var setting = settings.FirstOrDefault(s => s.Key == key);
        if (setting == null)
        {
            setting = new SettingDbModel 
            {
                Key = key,
                Value = value == null ? null : JsonSerializer.Serialize(value),
                Type = typeof(T).FullName
            };
            await repository.Add(setting, cancellationToken);
            return value;
        }

        if (setting.Type != typeof(T).FullName)
            throw new InvalidOperationException($"Setting with key '{key}' is of type '{setting.Type}', cannot update with type '{typeof(T).FullName}'");

        if (value == null)
            setting.Value = null;
        else
            setting.Value = JsonSerializer.Serialize(value);
        
        await repository.Update(setting, cancellationToken);

        return value;
    }
}
