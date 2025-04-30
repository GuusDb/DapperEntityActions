using Dapper;
using DapperOrmCore.Models;
using System.ComponentModel.DataAnnotations;
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
        if (node.Method.Name == "Any" && node.Arguments[0] is MemberExpression memberExpr &&
            _navigationProperties.ContainsKey(memberExpr.Member.Name) &&
            _navigationProperties[memberExpr.Member.Name].IsCollection)
        {
            // Handle x => x.Children.Any(c => c.Name == "Child1")
            string navPropName = memberExpr.Member.Name;
            var navProp = _navigationProperties[navPropName];
            string relatedTable = navProp.RelatedTableName;
            string fkColumn = navProp.ForeignKeyColumn;
            string alias = "c";
            string mainTablePkColumn = _propertyMap.First(p => p.Value.GetCustomAttribute<KeyAttribute>() != null).Key;

            _sqlBuilder.Append(_isNegated ? "NOT EXISTS (" : "EXISTS (");
            _sqlBuilder.Append($"SELECT 1 FROM {relatedTable} {alias} ");
            _sqlBuilder.Append($"WHERE {alias}.{fkColumn} = t.{mainTablePkColumn}");

            if (node.Arguments.Count > 1 && node.Arguments[1] is LambdaExpression lambda)
            {
                // Create a WhereExpressionVisitor for the related type
                var relatedType = navProp.RelatedType;
                var relatedPropertyMap = relatedType.GetProperties()
                    .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
                    .ToDictionary(
                        p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name,
                        p => p,
                        StringComparer.OrdinalIgnoreCase);

                // Instantiate WhereExpressionVisitor<TRelated>
                var visitorType = typeof(WhereExpressionVisitor<>).MakeGenericType(relatedType);
                var subVisitor = Activator.CreateInstance(
                    visitorType,
                    relatedPropertyMap,
                    new Dictionary<string, NavigationPropertyInfo>(),
                    new List<string>());

                // Create the lambda with the correct parameter type
                var delegateType = typeof(Func<,>).MakeGenericType(relatedType, typeof(bool));
                var lambdaExpr = Expression.Lambda(delegateType, lambda.Body, lambda.Parameters);

                // Translate the inner predicate
                var translateMethod = visitorType.GetMethod("Translate");
                var (subSql, subParams) = ((string, DynamicParameters))translateMethod.Invoke(subVisitor, new[] { lambdaExpr });

                if (!string.IsNullOrEmpty(subSql))
                {
                    _sqlBuilder.Append($" AND {subSql.Replace("t.", $"{alias}.")}");
                    foreach (var paramName in subParams.ParameterNames)
                    {
                        string newParamName = $"p{_paramCounter++}";
                        _parameters.Add(newParamName, subParams.Get<object>(paramName));
                        _sqlBuilder.Replace($"@{paramName}", $"@{newParamName}");
                    }
                }
            }

            _sqlBuilder.Append(")");
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