namespace FluentCMS.Core;

public class QueryResult<T>
{
    // The collection of items returned by the query
    public IEnumerable<T> Items { get; set; } = [];

    // The total count of records that match the filter (before pagination)
    public int TotalCount { get; set; }
}
