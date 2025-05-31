using Microsoft.Extensions.Configuration;

namespace FluentCMS.Configuration;

public class DatabaseConfigurationSource(IServiceProvider serviceProvider) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DatabaseConfigurationProvider(serviceProvider);
    }
}
