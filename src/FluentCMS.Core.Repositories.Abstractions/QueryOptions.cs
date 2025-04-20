using System.Linq.Expressions;

namespace FluentCMS.Core.Repositories.Abstractions;

public class QueryOptions<T>
{
    // Filter to apply to the query
    public Expression<Func<T, bool>>? Filter { get; set; }
    
    // Sorting options
    public SortOptions<T>? Sort { get; set; }

    // Pagination options
    public PaginationOptions? Pagination { get; set; }
}
