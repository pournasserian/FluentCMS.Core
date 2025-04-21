namespace FluentCMS.Core.Repositories.LiteDB;

public static class ExpressionHelpers
{
    /// <summary>
    /// Extracts the property name from a lambda expression like x => x.PropertyName
    /// </summary>
    public static string ExtractPropertyNameFromExpression<T, TProperty>(Expression<Func<T, TProperty>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression memberExpr)
        {
            return memberExpr.Member.Name;
        }

        throw new ArgumentException("Expression does not reference a property or field", nameof(expression));
    }
}
