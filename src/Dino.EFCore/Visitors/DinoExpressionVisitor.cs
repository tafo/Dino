namespace Dino.EFCore.Visitors;

using System.Linq.Expressions;
using System.Reflection;
using Core.Ast;
using Core.Ast.Expressions;
using Core.Ast.Queries;
using Core.Ast.Clauses;
using Dino.Core.Ast.Visitors;

public class DinoExpressionVisitor<T> : IDinoQueryVisitor<Expression> where T : class
{
    private readonly Dictionary<string, ParameterExpression> _parameters = new();
    private readonly ParameterExpression _rootParameter;
    private readonly Type _entityType;

    public DinoExpressionVisitor()
    {
        _entityType = typeof(T);
        _rootParameter = Expression.Parameter(_entityType, "x");
        _parameters[_entityType.Name.ToLower()] = _rootParameter;
        _parameters["x"] = _rootParameter;
    }

    public Expression<Func<T, bool>> BuildWhereExpression(DinoWhereClause whereClause)
    {
        var bodyExpression = whereClause.Accept(this);
        return Expression.Lambda<Func<T, bool>>(bodyExpression, _rootParameter);
    }

    public Expression Visit(DinoSelectQuery node)
    {
        throw new NotSupportedException("SelectQuery visit is not supported in expression visitor");
    }

    public Expression Visit(DinoFromClause node)
    {
        throw new NotSupportedException("FromClause visit is not supported in expression visitor");
    }

    public Expression Visit(DinoWhereClause node)
    {
        return node.Condition.Accept(this);
    }

    public Expression Visit(DinoJoinClause node)
    {
        throw new NotSupportedException("JoinClause visit is not supported in expression visitor");
    }

    public Expression Visit(DinoGroupByClause node)
    {
        throw new NotSupportedException("GroupByClause visit is not supported in expression visitor");
    }

    public Expression Visit(DinoOrderByClause node)
    {
        throw new NotSupportedException("OrderByClause visit is not supported in expression visitor");
    }

    public Expression Visit(DinoBinaryExpression node)
    {
        var left = node.Left.Accept(this);
        var right = node.Right.Accept(this);

        // Handle type conversions
        if (left.Type != right.Type)
        {
            if (right is ConstantExpression rightConstant && CanConvertConstant(rightConstant.Type, left.Type))
            {
                right = ConvertConstantExpression(rightConstant, left.Type);
            }
            else if (left is ConstantExpression leftConstant && CanConvertConstant(leftConstant.Type, right.Type))
            {
                left = ConvertConstantExpression(leftConstant, right.Type);
            }
        }

        return node.Operator switch
        {
            DinoBinaryOperator.Equal => Expression.Equal(left, right),
            DinoBinaryOperator.NotEqual => Expression.NotEqual(left, right),
            DinoBinaryOperator.GreaterThan => Expression.GreaterThan(left, right),
            DinoBinaryOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(left, right),
            DinoBinaryOperator.LessThan => Expression.LessThan(left, right),
            DinoBinaryOperator.LessThanOrEqual => Expression.LessThanOrEqual(left, right),
            DinoBinaryOperator.And => Expression.AndAlso(left, right),
            DinoBinaryOperator.Or => Expression.OrElse(left, right),
            DinoBinaryOperator.Add => Expression.Add(left, right),
            DinoBinaryOperator.Subtract => Expression.Subtract(left, right),
            DinoBinaryOperator.Multiply => Expression.Multiply(left, right),
            DinoBinaryOperator.Divide => Expression.Divide(left, right),
            DinoBinaryOperator.Modulo => Expression.Modulo(left, right),
            DinoBinaryOperator.Like => BuildLikeExpression(left, right),
            _ => throw new NotSupportedException($"Binary operator {node.Operator} is not supported")
        };
    }

    public Expression Visit(DinoUnaryExpression node)
    {
        var operand = node.Operand.Accept(this);

        return node.Operator switch
        {
            DinoUnaryOperator.Not => Expression.Not(operand),
            DinoUnaryOperator.Minus => Expression.Negate(operand),
            DinoUnaryOperator.Plus => operand,
            DinoUnaryOperator.IsNull => Expression.Equal(operand, Expression.Constant(null, operand.Type)),
            DinoUnaryOperator.IsNotNull => Expression.NotEqual(operand, Expression.Constant(null, operand.Type)),
            _ => throw new NotSupportedException($"Unary operator {node.Operator} is not supported")
        };
    }

    public Expression Visit(DinoIdentifierExpression node)
    {
        // Check if it's a parameter reference
        if (_parameters.TryGetValue(node.Name.ToLower(), out var parameter))
        {
            return parameter;
        }

        // Otherwise, it's a property access on the root parameter
        var property = _entityType.GetProperty(node.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property != null)
        {
            return Expression.Property(_rootParameter, property);
        }

        throw new InvalidOperationException($"Property '{node.Name}' not found on type '{_entityType.Name}'");
    }

    public Expression Visit(DinoLiteralExpression node)
    {
        return Expression.Constant(node.Value, node.ValueType);
    }

    public Expression Visit(DinoFunctionCallExpression node)
    {
        var functionName = node.FunctionName.ToUpper();

        switch (functionName)
        {
            case "TOUPPER":
            case "UPPER":
                var upperArg = node.Arguments[0].Accept(this);
                return Expression.Call(upperArg, typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!);

            case "TOLOWER":
            case "LOWER":
                var lowerArg = node.Arguments[0].Accept(this);
                return Expression.Call(lowerArg, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);

            case "CONTAINS":
                var containsTarget = node.Arguments[0].Accept(this);
                var containsValue = node.Arguments[1].Accept(this);
                return Expression.Call(containsTarget, typeof(string).GetMethod("Contains", [typeof(string)])!, containsValue);

            case "STARTSWITH":
                var startsTarget = node.Arguments[0].Accept(this);
                var startsValue = node.Arguments[1].Accept(this);
                return Expression.Call(startsTarget, typeof(string).GetMethod("StartsWith", [typeof(string)])!, startsValue);

            case "ENDSWITH":
                var endsTarget = node.Arguments[0].Accept(this);
                var endsValue = node.Arguments[1].Accept(this);
                return Expression.Call(endsTarget, typeof(string).GetMethod("EndsWith", [typeof(string)])!, endsValue);

            case "LENGTH":
            case "LEN":
                var lengthArg = node.Arguments[0].Accept(this);
                return Expression.Property(lengthArg, typeof(string).GetProperty("Length")!);

            default:
                throw new NotSupportedException($"Function '{functionName}' is not supported");
        }
    }

    public Expression Visit(DinoParameterExpression node)
    {
        throw new NotSupportedException("Parameters should be replaced during parsing");
    }

    public Expression Visit(DinoMemberAccessExpression node)
    {
        var objectExpression = node.Object.Accept(this);
        var property = objectExpression.Type.GetProperty(node.MemberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        
        if (property == null)
        {
            throw new InvalidOperationException($"Property '{node.MemberName}' not found on type '{objectExpression.Type.Name}'");
        }

        return Expression.Property(objectExpression, property);
    }

    public Expression Visit(DinoInExpression node)
    {
        var expression = node.Expression.Accept(this);
        var values = node.Values.Select(v => v.Accept(this)).ToList();

        // Build array of values
        var elementType = expression.Type;
        var arrayExpression = Expression.NewArrayInit(elementType, values);

        // Call Contains method
        var containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);

        var containsExpression = Expression.Call(null, containsMethod, arrayExpression, expression);

        return node.IsNegated ? Expression.Not(containsExpression) : containsExpression;
    }

    public Expression Visit(DinoBetweenExpression node)
    {
        var expression = node.Expression.Accept(this);
        var lower = node.LowerBound.Accept(this);
        var upper = node.UpperBound.Accept(this);

        // expression >= lower && expression <= upper
        var lowerCheck = Expression.GreaterThanOrEqual(expression, lower);
        var upperCheck = Expression.LessThanOrEqual(expression, upper);
        var betweenExpression = Expression.AndAlso(lowerCheck, upperCheck);

        return node.IsNegated ? Expression.Not(betweenExpression) : betweenExpression;
    }

    public Expression Visit(DinoCaseExpression node)
    {
        throw new NotSupportedException("CASE expressions are not yet supported");
    }

    public Expression Visit(DinoExistsExpression node)
    {
        throw new NotSupportedException("EXISTS expressions require subquery support");
    }

    public Expression Visit(DinoSubqueryExpression node)
    {
        throw new NotSupportedException("Subqueries are not yet supported");
    }

    private Expression BuildLikeExpression(Expression left, Expression right)
    {
        if (right is ConstantExpression { Value: string pattern })
        {
            var stringValue = left;
            
            if (pattern.StartsWith("%") && pattern.EndsWith("%"))
            {
                // %value% -> Contains
                var value = pattern.Trim('%');
                var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;
                return Expression.Call(stringValue, containsMethod, Expression.Constant(value));
            }
            else if (pattern.StartsWith("%"))
            {
                // %value -> EndsWith
                var value = pattern.TrimStart('%');
                var endsWithMethod = typeof(string).GetMethod("EndsWith", [typeof(string)])!;
                return Expression.Call(stringValue, endsWithMethod, Expression.Constant(value));
            }
            else if (pattern.EndsWith("%"))
            {
                // value% -> StartsWith
                var value = pattern.TrimEnd('%');
                var startsWithMethod = typeof(string).GetMethod("StartsWith", [typeof(string)])!;
                return Expression.Call(stringValue, startsWithMethod, Expression.Constant(value));
            }
            else
            {
                // No wildcards -> Equals
                return Expression.Equal(stringValue, Expression.Constant(pattern));
            }
        }

        throw new NotSupportedException("Dynamic LIKE patterns are not supported");
    }

    private bool CanConvertConstant(Type from, Type to)
    {
        try
        {
            var testValue = Activator.CreateInstance(from);
            if (testValue != null)
            {
                _ = Convert.ChangeType(testValue, to);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private Expression ConvertConstantExpression(ConstantExpression constant, Type targetType)
    {
        if (constant.Value == null)
        {
            return Expression.Constant(null, targetType);
        }

        var convertedValue = Convert.ChangeType(constant.Value, targetType);
        return Expression.Constant(convertedValue, targetType);
    }
}