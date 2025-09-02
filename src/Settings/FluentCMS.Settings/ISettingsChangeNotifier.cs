using Microsoft.Extensions.Primitives;

namespace FluentCMS.Settings;

public interface ISettingsChangeNotifier
{
    IChangeToken GetChangeToken(string key);
    void SignalChanged(string key);
    void SignalAll();
}