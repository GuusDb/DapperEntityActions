using Dapper;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DapperOrmCore;
internal class WhereExpressionVisitor<T> : ExpressionVisitor
{
    private readonly Dictionary<string, PropertyInfo> _propertyMap;
    private readonly DynamicParameters _parameters;
    private readonly StringBuilder _sqlBuilder;
    private int _paramCounter;

    public WhereExpressionVisitor(Dictionary<string, PropertyInfo> propertyMap)
    {
        _propertyMap = propertyMap;
        _parameters = new DynamicParameters();
        _sqlBuilder = new StringBuilder();
        _paramCounter = 0;
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

        switch (node.NodeType)
        {
            case ExpressionType.Equal:
                _sqlBuilder.Append(" = ");
                break;
            case ExpressionType.NotEqual:
                _sqlBuilder.Append(" != ");
                break;
            case ExpressionType.GreaterThan:
                _sqlBuilder.Append(" > ");
                break;
            case ExpressionType.LessThan:
                _sqlBuilder.Append(" < ");
                break;
            case ExpressionType.AndAlso:
                _sqlBuilder.Append(" AND ");
                break;
            case ExpressionType.OrElse:
                _sqlBuilder.Append(" OR ");
                break;
            default:
                throw new NotSupportedException($"Binary operator '{node.NodeType}' is not supported");
        }

        Visit(node.Right);
        _sqlBuilder.Append(")");

        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is ParameterExpression)
        {
            // Get the C# property name from the expression
            string propertyName = node.Member.Name;

            // Find the corresponding column name in the property map
            var propertyInfo = typeof(T).GetProperties()
                .FirstOrDefault(p => p.Name == propertyName);

            if (propertyInfo == null)
            {
                throw new NotSupportedException($"Property '{propertyName}' not found in type '{typeof(T).Name}'");
            }

            // Get the column name from the [Column] attribute or use the property name as fallback
            string columnName = propertyInfo.GetCustomAttribute<ColumnAttribute>()?.Name ?? propertyName;

            // Verify the column exists in the property map (which uses column names as keys)
            if (!_propertyMap.ContainsKey(columnName))
            {
                throw new NotSupportedException($"Column '{columnName}' mapped from property '{propertyName}' not found in property map");
            }

            _sqlBuilder.Append(columnName);
        }
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        string paramName = $"@p{_paramCounter++}";
        _sqlBuilder.Append(paramName);
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
            string paramName = $"@p{_paramCounter++}";
            _sqlBuilder.Append(paramName);
            _parameters.Add(paramName, $"%{value}%");
            return node;
        }
        throw new NotSupportedException($"Method '{node.Method.Name}' is not supported");
    }
}