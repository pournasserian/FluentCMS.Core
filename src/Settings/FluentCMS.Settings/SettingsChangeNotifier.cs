using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;

namespace FluentCMS.Settings;

public sealed class SettingsChangeNotifier : ISettingsChangeNotifier
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _sources = new();

    public IChangeToken GetChangeToken(string key)
    {
        var cts = _sources.GetOrAdd(key, _ => new CancellationTokenSource());
        return new CancellationChangeToken(cts.Token);
    }


    public void SignalChanged(string key)
    {
        if (_sources.TryRemove(key, out var cts))
            cts.Cancel();
    }

    public void SignalAll()
    {
        foreach (var kv in _sources.ToArray())
            if (_sources.TryRemove(kv.Key, out var cts))
                cts.Cancel();
    }
}