using LiteDB;

namespace FluentCMS.Core.Repositories.LiteDB;

public interface ILiteDBContext
{
    ILiteDatabase Database { get; }
}