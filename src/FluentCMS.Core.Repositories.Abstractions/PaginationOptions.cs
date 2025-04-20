namespace FluentCMS.Core.Repositories.Abstractions;

public class PaginationOptions
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int Skip => (PageNumber - 1) * PageSize;
}