namespace FluentCMS.Core.Repositories.Abstractions;

public class QueryOptions<T>
{
    // Filter to apply to the query
    public Expression<Func<T, bool>>? Filter { get; set; }

    // Pagination options
    public PaginationOptions? Pagination { get; set; }

    // Sorting options
    public IList<SortOption<T>>? Sorting { get; set; }
}
