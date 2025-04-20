using System.Linq.Expressions;

namespace FluentCMS.Core.Repositories.Abstractions;

public enum SortDirection
{
    Ascending,
    Descending
}

public class SortExpression<T>(LambdaExpression expression, SortDirection direction)
{
    public LambdaExpression Expression { get; } = expression;

    public SortDirection Direction { get; } = direction;
}

public class SortOptions<T>
{
    public List<SortExpression<T>> Expressions { get; } = [];

    public void Add<TKey>(Expression<Func<T, TKey>> expression, SortDirection direction = SortDirection.Ascending)
    {
        Expressions.Add(new SortExpression<T>(expression, direction));
    }
}
