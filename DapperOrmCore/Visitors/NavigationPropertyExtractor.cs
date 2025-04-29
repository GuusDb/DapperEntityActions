using System.Linq.Expressions;


namespace DapperOrmCore.Visitors;
internal class NavigationPropertyExtractor : ExpressionVisitor
{
    private readonly List<string> _navigationProperties;
    public HashSet<string> NavigationProperties { get; } = new HashSet<string>();

    public NavigationPropertyExtractor(List<string> navigationProperties)
    {
        _navigationProperties = navigationProperties;
    }

    public IEnumerable<string> Extract<T>(Expression<Func<T, bool>> predicate)
    {
        Visit(predicate);
        return NavigationProperties;
    }

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