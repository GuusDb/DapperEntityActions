using Dapper;
using DapperOrmCore.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DapperOrmCore.Visitors;

public class WhereExpressionVisitor<T> : ExpressionVisitor
{
    private readonly Dictionary<string, PropertyInfo> _propertyMap;
    private readonly Dictionary<string, NavigationPropertyInfo> _navigationProperties;
    private readonly List<string> _referencedNavProps;
    private readonly StringBuilder _sqlBuilder = new StringBuilder();
    private readonly DynamicParameters _parameters = new DynamicParameters();
    private int _paramCounter = 0;
    private bool _isNegated;

    public WhereExpressionVisitor(
        Dictionary<string, PropertyInfo> propertyMap,
        Dictionary<string, NavigationPropertyInfo> navigationProperties,
        List<string> referencedNavProps)
    {
        _propertyMap = propertyMap;
        _navigationProperties = navigationProperties;
        _referencedNavProps = referencedNavProps;
    }

    public (string Sql, DynamicParameters Parameters) Translate(Expression<Func<T, bool>> expression)
    {
        Visit(expression.Body);
        return (_sqlBuilder.ToString(), _parameters);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        _sqlBuilder.Append("(");
        Visit(node.Left);
        _sqlBuilder.Append(node.NodeType switch
        {
            ExpressionType.Equal => " = ",
            ExpressionType.NotEqual => " != ",
            ExpressionType.GreaterThan => " > ",
            ExpressionType.LessThan => " < ",
            ExpressionType.AndAlso => " AND ",
            ExpressionType.OrElse => " OR ",
            _ => throw new NotSupportedException($"Operator '{node.NodeType}' not supported")
        });
        Visit(node.Right);
        _sqlBuilder.Append(")");
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is ParameterExpression paramExpr && paramExpr.Type == typeof(T))
        {
            // Direct property of the main entity (e.g., CoolMeasurement.TestCd)
            string propertyName = node.Member.Name;
            var propertyInfo = _propertyMap.Values.FirstOrDefault(p =>
                string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            if (propertyInfo == null)
                throw new NotSupportedException($"Property '{propertyName}' not found in property map");

            string columnName = propertyInfo.GetCustomAttribute<ColumnAttribute>()?.Name ?? propertyName;
            if (!_propertyMap.ContainsKey(columnName))
                throw new NotSupportedException($"Column '{columnName}' not found in property map");

            _sqlBuilder.Append($"t.{columnName}");
        }
        else if (node.Expression is MemberExpression memberExpr &&
                 _navigationProperties.ContainsKey(memberExpr.Member.Name))
        {
            // Navigation property (e.g., Test.Description)
            string navPropName = memberExpr.Member.Name;
            int index = _referencedNavProps.IndexOf(navPropName);
            if (index == -1)
                throw new InvalidOperationException($"Navigation property '{navPropName}' not referenced correctly");

            string alias = $"r{index + 1}";
            string propertyName = node.Member.Name;
            var relatedType = _navigationProperties[navPropName].RelatedType;
            var relatedProperty = relatedType.GetProperty(propertyName);
            if (relatedProperty == null)
                throw new NotSupportedException($"Property '{propertyName}' not found in related type '{relatedType.Name}'");

            string columnName = relatedProperty.GetCustomAttribute<ColumnAttribute>()?.Name ?? propertyName;
            _sqlBuilder.Append($"{alias}.{columnName}");
        }
        else
        {
            throw new NotSupportedException($"Member expression '{node}' is not supported");
        }
        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Not)
        {
            _isNegated = !_isNegated;
            Visit(node.Operand);
            _isNegated = false;
            return node;
        }
        if (node.NodeType == ExpressionType.Convert)
        {
            return Visit(node.Operand);
        }
        return base.VisitUnary(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        string paramName = $"p{_paramCounter++}";
        _sqlBuilder.Append($"@{paramName}");
        _parameters.Add(paramName, node.Value);
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == "Contains" && node.Object?.Type == typeof(string))
        {
            Visit(node.Object);
            _sqlBuilder.Append(" LIKE ");
            var value = Expression.Lambda(node.Arguments[0]).Compile().DynamicInvoke();
            string paramName = $"p{_paramCounter++}";
            _sqlBuilder.Append($"@{paramName}");
            _parameters.Add(paramName, $"%{value}%");
            return node;
        }
        throw new NotSupportedException($"Method '{node.Method.Name}' is not supported");
    }

    public override Expression Visit(Expression node)
    {
        if (node is MemberExpression member && member.Type == typeof(bool))
        {
            VisitMember(member);
            _sqlBuilder.Append(" = ");
            string paramName = $"p{_paramCounter++}";
            _sqlBuilder.Append($"@{paramName}");
            bool value = _isNegated ? false : true;
            _parameters.Add(paramName, value);
            return node;
        }
        return base.Visit(node);
    }
}