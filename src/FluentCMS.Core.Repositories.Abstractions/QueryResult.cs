namespace FluentCMS.Core.Repositories.Abstractions;

public class QueryResult<T>
{
    // The collection of items returned by the query
    public IEnumerable<T> Items { get; set; } = [];

    // The total count of records that match the filter (before pagination)
    public long TotalCount { get; set; }
}
