using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FluentCMS.Configuration.EntityFramework;


public class EfConfigurationSource : IConfigurationSource
{
    private DbContextOptions<ConfigurationDbContext> _options = new();

    public EfConfigurationSource()
    {
    }

    public void Init(Action<DbContextOptionsBuilder> optionsAction)
    {
        var builder = new DbContextOptionsBuilder<ConfigurationDbContext>();
        optionsAction(builder);
        _options = builder.Options;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new EfConfigurationProvider(_options, builder.Build());
    }
}
