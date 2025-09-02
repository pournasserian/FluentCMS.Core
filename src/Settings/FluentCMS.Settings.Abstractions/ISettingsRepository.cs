namespace FluentCMS.Settings.Abstractions;

public interface ISettingsRepository
{
    Task<SettingRecord?> Get(string key, CancellationToken ct = default);
    Task<IDictionary<string, SettingRecord>> GetAll(CancellationToken ct = default);
    Task Upsert(string key, SettingRecord record, CancellationToken ct = default);
}