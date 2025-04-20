using LiteDB;

namespace FluentCMS.Core.Repositories.LiteDB;

public class LiteDBOptions
{
    public string ConnectionString { get; set; } = default!;
    public Action<BsonMapper>? MapperConfiguration { get; set; }
}