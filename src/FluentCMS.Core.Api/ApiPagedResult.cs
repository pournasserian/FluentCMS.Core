namespace FluentCMS.Core.Api;

public interface IApiPagedResult
{
    bool HasNext { get; }
    bool HasPrevious { get; }
    int PageNumber { get; }
    int PageSize { get; }
    long TotalCount { get; }
    int TotalPages { get; }
}

public class ApiPagedResult<TData> : ApiResult<IEnumerable<TData>>, IApiPagedResult
{
    public int PageSize { get; }
    public int TotalPages { get; }
    public int PageNumber { get; }
    public long TotalCount { get; }
    public bool HasPrevious { get; }
    public bool HasNext { get; }

    public ApiPagedResult()
    {
    }

    public ApiPagedResult(IEnumerable<TData> data, long totalCount, int pageNumber, int pageSize) : base(data)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
        HasPrevious = PageNumber > 1;
        HasNext = PageNumber < TotalPages;
    }
}
