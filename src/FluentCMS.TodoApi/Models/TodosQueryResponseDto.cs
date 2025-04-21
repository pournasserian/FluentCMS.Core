namespace FluentCMS.TodoApi.Models;

public class TodosQueryResponseDto
{
    // Collection of todo items for the current page
    public ICollection<TodoResponseDto> Items { get; set; } = new List<TodoResponseDto>();

    // Total number of records matching the filter (before pagination)
    public int TotalCount { get; set; }

    // Current page number
    public int Page { get; set; }

    // Number of items per page
    public int PageSize { get; set; }

    // Total number of pages
    public int TotalPages { get; set; }
}
