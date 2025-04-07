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
    private bool _isNegated;

    public WhereExpressionVisitor(Dictionary<string, PropertyInfo> propertyMap)
    {
        _propertyMap = propertyMap;
        _parameters = new DynamicParameters();
        _sqlBuilder = new StringBuilder();
        _paramCounter = 0;
        _isNegated = false;
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
            string propertyName = node.Member.Name;
            var propertyInfo = typeof(T).GetProperties()
                .FirstOrDefault(p => p.Name == propertyName);

            if (propertyInfo == null)
            {
                throw new NotSupportedException($"Property '{propertyName}' not found in type '{typeof(T).Name}'");
            }

            string columnName = propertyInfo.GetCustomAttribute<ColumnAttribute>()?.Name ?? propertyName;

            if (!_propertyMap.ContainsKey(columnName))
            {
                throw new NotSupportedException($"Column '{columnName}' mapped from property '{propertyName}' not found in property map");
            }

            _sqlBuilder.Append(columnName);
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

    public override Expression? Visit(Expression? node)
    {
        if (node is MemberExpression member && member.Type == typeof(bool))
        {
            VisitMember(member);
            _sqlBuilder.Append(" = ");
            string paramName = $"@p{_paramCounter++}";
            _sqlBuilder.Append(paramName);
            bool value = _isNegated ? false : true;
            _parameters.Add(paramName, value);
            return node;
        }
        return base.Visit(node);
    }
}