namespace FluentCMS.TodoApi;

public class ApiResult
{
    public List<ApiError> Errors { get; } = [];
    public string TraceId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string UniqueId { get; set; } = string.Empty;
    public double Duration { get; set; }
    public int Status { get; set; }
    public bool IsSuccess { get; set; }

    public ApiResult()
    {
        IsSuccess = true;
        Status = 200;
    }
}

public class ApiResult<TData> : ApiResult
{
    public TData? Data { get; set; }

    public ApiResult()
    {
    }

    public ApiResult(TData data)
    {
        Data = data;
    }
}

public class ApiPagedResult<TData> : ApiResult<IEnumerable<TData>>
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

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ApiError()
    {
    }

    public ApiError(string code)
    {
        Code = code;
    }

    public ApiError(string code, string description)
    {
        Code = code;
        Description = description;
    }

    public override string ToString()
    {
        return $"{Code}-{Description}";
    }
}

