using FluentCMS.Settings.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Reflection;

namespace FluentCMS.Settings;

public sealed class SettingsOptionsSource<T>(ISettingsService settingsService, ISettingsChangeNotifier changeNotifier, string key) : IConfigureOptions<T>, IOptionsChangeTokenSource<T> where T : class, new()
{
    public void Configure(T options)
    {
        var latest = settingsService.Get<T>(key).GetAwaiter().GetResult() ?? new T();
        foreach (var p in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)) 
            if (p.CanRead && p.CanWrite) 
                p.SetValue(options, p.GetValue(latest));
    }

    public string Name => Options.DefaultName;
    
    public IChangeToken GetChangeToken() => changeNotifier.GetChangeToken(key);
}
