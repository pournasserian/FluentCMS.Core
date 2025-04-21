namespace FluentCMS.Core;

// Represents a sorting criterion for queries
public class SortOption<T>
{
    // Expression selecting the key to sort by
    public Expression<Func<T, object>> KeySelector { get; set; }

    // Direction of the sort
    public SortDirection Direction { get; set; }

    // Constructs a SortOption with the specified key selector and optional direction (default ascending)
    public SortOption(Expression<Func<T, object>> keySelector, SortDirection direction = SortDirection.Ascending)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        KeySelector = keySelector;
        Direction = direction;
    }
}
