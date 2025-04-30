using System.Linq.Expressions;


namespace DapperOrmCore.Visitors;

/// <summary>
/// Extracts navigation properties from an expression for use in database queries.
/// </summary>
public class NavigationPropertyExtractor : ExpressionVisitor
{
    private readonly List<string> _navigationProperties;
    /// <summary>
    /// Gets the collection of extracted navigation property names.
    /// </summary>
    public HashSet<string> NavigationProperties { get; } = new HashSet<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationPropertyExtractor"/> class.
    /// </summary>
    /// <param name="navigationProperties">A list of navigation property names to be extracted.</param>
    public NavigationPropertyExtractor(List<string> navigationProperties)
    {
        _navigationProperties = navigationProperties;
    }

    /// <summary>
    /// Extracts navigation properties from the specified predicate expression.
    /// </summary>
    /// <typeparam name="T">The type of the entity in the predicate.</typeparam>
    /// <param name="predicate">The expression containing the predicate to analyze.</param>
    /// <returns>An enumerable collection of navigation property names.</returns>
    public IEnumerable<string> Extract<T>(Expression<Func<T, bool>> predicate)
    {
        Visit(predicate);
        return NavigationProperties;
    }

    /// <summary>
    /// Visits a member expression and extracts navigation properties if applicable.
    /// </summary>
    /// <param name="node">The member expression to visit.</param>
    /// <returns>The visited expression.</returns>
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is MemberExpression memberExpr &&
            _navigationProperties.Contains(memberExpr.Member.Name))
        {
            NavigationProperties.Add(memberExpr.Member.Name);
        }
        return base.VisitMember(node);
    }
}