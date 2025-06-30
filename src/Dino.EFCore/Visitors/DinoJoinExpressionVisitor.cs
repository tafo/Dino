namespace Dino.EFCore.Visitors;

using System.Linq.Expressions;
using System.Reflection;
using Core.Ast;
using Core.Ast.Expressions;
using Core.Ast.Queries;
using Core.Ast.Clauses;
using Core.Ast.Visitors;

public class DinoJoinExpressionVisitor<T> : IDinoQueryVisitor<Expression> where T : class
{
    private readonly Type _rootType;
    private readonly ParameterExpression _rootParameter;
    private readonly DinoFromClause _fromClause;
    private readonly Dictionary<string, (Type Type, Expression Accessor)> _tableAccessors = new();

    public DinoJoinExpressionVisitor(DinoFromClause fromClause)
    {
        _rootType = typeof(T);
        _rootParameter = Expression.Parameter(_rootType, "x");
        _fromClause = fromClause;
        
        // Initialize table accessors
        InitializeTableAccessors();
    }

    private void InitializeTableAccessors()
    {
        // Add root table
        var rootTableName = _fromClause.TableSource.TableName.ToLower();
        _tableAccessors[rootTableName] = (_rootType, _rootParameter);
        
        // Add alias if exists
        if (_fromClause.TableSource.Alias != null)
        {
            _tableAccessors[_fromClause.TableSource.Alias.ToLower()] = (_rootType, _rootParameter);
        }

        // Add joined tables
        foreach (var join in _fromClause.Joins)
        {
            var tableName = join.TableSource.TableName.ToLower();
            var navigationProperty = FindNavigationProperty(_rootType, join.TableSource.TableName);
            
            if (navigationProperty != null)
            {
                var accessor = Expression.Property(_rootParameter, navigationProperty);
                var joinedType = GetPropertyType(navigationProperty);
                
                _tableAccessors[tableName] = (joinedType, accessor);
                
                // Add alias if exists
                if (join.TableSource.Alias != null)
                {
                    _tableAccessors[join.TableSource.Alias.ToLower()] = (joinedType, accessor);
                }
            }
        }
    }

    private Type GetPropertyType(PropertyInfo property)
    {
        if (property.PropertyType.IsGenericType &&
            property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
        {
            return property.PropertyType.GetGenericArguments()[0];
        }
        return property.PropertyType;
    }

    public Expression<Func<T, bool>> BuildWhereExpression(DinoWhereClause whereClause)
    {
        var bodyExpression = whereClause.Accept(this);
        return Expression.Lambda<Func<T, bool>>(bodyExpression, _rootParameter);
    }

    public Expression<Func<T, object>>? BuildOrderByExpression(DinoExpression expression)
    {
        try
        {
            var bodyExpression = expression.Accept(this);
            
            // Box value types
            if (bodyExpression.Type.IsValueType)
            {
                bodyExpression = Expression.Convert(bodyExpression, typeof(object));
            }
            
            return Expression.Lambda<Func<T, object>>(bodyExpression, _rootParameter);
        }
        catch
        {
            // If we can't build the expression, return null
            return null;
        }
    }

    public Expression Visit(DinoIdentifierExpression node)
    {
        // Check if it's a table.column format
        if (node.Name.Contains('.'))
        {
            var parts = node.Name.Split('.');
            if (parts.Length == 2)
            {
                var tableName = parts[0].ToLower();
                var columnName = parts[1];
                
                if (_tableAccessors.TryGetValue(tableName, out var tableInfo))
                {
                    var (tableType, accessor) = tableInfo;
                    
                    // If accessor is a collection, we need special handling
                    if (accessor.Type.IsGenericType && 
                        accessor.Type.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        // For collection properties, we need to return a special marker
                        // that will be handled by the binary expression builder
                        return new CollectionPropertyExpression(accessor, tableType, columnName);
                    }
                    
                    var property = tableType.GetProperty(columnName, 
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    
                    if (property != null)
                    {
                        return Expression.Property(accessor, property);
                    }
                    
                    throw new InvalidOperationException($"Property '{columnName}' not found on type '{tableType.Name}'");
                }
                
                throw new InvalidOperationException($"Table or alias '{tableName}' not found in query");
            }
        }

        // Try to find property on root table
        var rootProperty = _rootType.GetProperty(node.Name, 
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        
        if (rootProperty != null)
        {
            return Expression.Property(_rootParameter, rootProperty);
        }

        throw new InvalidOperationException($"Property '{node.Name}' not found");
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

        // Special handling for collection properties
        if (left is CollectionPropertyExpression collectionProp)
        {
            return BuildCollectionExpression(collectionProp, node.Operator, right);
        }

        // Handle type conversions
        if (left.Type != right.Type)
        {
            if (IsNumericType(left.Type) && IsNumericType(right.Type))
            {
                var targetType = GetCommonNumericType(left.Type, right.Type);
                if (left.Type != targetType)
                    left = Expression.Convert(left, targetType);
                if (right.Type != targetType)
                    right = Expression.Convert(right, targetType);
            }
            else if (right is ConstantExpression rightConstant)
            {
                right = ConvertConstantExpression(rightConstant, left.Type);
            }
            else if (left is ConstantExpression leftConstant)
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

    private Expression BuildCollectionExpression(CollectionPropertyExpression collectionProp, DinoBinaryOperator op, Expression value)
    {
        // Build: collection.Any(item => item.Property [op] value)
        var itemParameter = Expression.Parameter(collectionProp.ElementType, "item");
        var property = collectionProp.ElementType.GetProperty(collectionProp.PropertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        
        if (property == null)
        {
            throw new InvalidOperationException($"Property '{collectionProp.PropertyName}' not found on type '{collectionProp.ElementType.Name}'");
        }

        var propertyAccess = Expression.Property(itemParameter, property);
        
        // Convert value if needed
        var convertedValue = value;
        if (propertyAccess.Type != value.Type && value is ConstantExpression constValue)
        {
            convertedValue = ConvertConstantExpression(constValue, propertyAccess.Type);
        }

        var comparison = op switch
        {
            DinoBinaryOperator.Equal => Expression.Equal(propertyAccess, convertedValue),
            DinoBinaryOperator.NotEqual => Expression.NotEqual(propertyAccess, convertedValue),
            DinoBinaryOperator.GreaterThan => Expression.GreaterThan(propertyAccess, convertedValue),
            DinoBinaryOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyAccess, convertedValue),
            DinoBinaryOperator.LessThan => Expression.LessThan(propertyAccess, convertedValue),
            DinoBinaryOperator.LessThanOrEqual => Expression.LessThanOrEqual(propertyAccess, convertedValue),
            DinoBinaryOperator.Like => BuildLikeExpression(propertyAccess, convertedValue),
            _ => throw new NotSupportedException($"Operator {op} is not supported for collection properties")
        };

        var lambda = Expression.Lambda(comparison, itemParameter);
        
        // Get the Any method
        var anyMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
            .MakeGenericMethod(collectionProp.ElementType);

        return Expression.Call(null, anyMethod, collectionProp.Collection, lambda);
    }

    // Helper class to represent collection property access
    private class CollectionPropertyExpression(Expression collection, Type elementType, string propertyName)
        : Expression
    {
        public Expression Collection { get; } = collection;
        public Type ElementType { get; } = elementType;
        public string PropertyName { get; } = propertyName;

        public override ExpressionType NodeType => ExpressionType.Extension;
        public override Type Type => typeof(bool);
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
        var property = objectExpression.Type.GetProperty(node.MemberName, 
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        
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

        var elementType = expression.Type;
        var arrayExpression = Expression.NewArrayInit(elementType, values);

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

        // Convert bounds to match expression type if needed
        var targetType = expression.Type;
        if (lower.Type != targetType)
            lower = Expression.Convert(lower, targetType);
        if (upper.Type != targetType)
            upper = Expression.Convert(upper, targetType);

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

    private PropertyInfo? FindNavigationProperty(Type entityType, string relatedTableName)
    {
        var properties = entityType.GetProperties();
        
        var exactMatch = properties.FirstOrDefault(p => 
            p.Name.Equals(relatedTableName, StringComparison.OrdinalIgnoreCase) ||
            p.Name.Equals(relatedTableName + "s", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Equals(relatedTableName.TrimEnd('s'), StringComparison.OrdinalIgnoreCase));
        
        if (exactMatch != null)
            return exactMatch;

        foreach (var property in properties)
        {
            if (property.PropertyType.IsGenericType && 
                property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = property.PropertyType.GetGenericArguments()[0];
                if (elementType.Name.Equals(relatedTableName, StringComparison.OrdinalIgnoreCase) ||
                    elementType.Name.Equals(relatedTableName.TrimEnd('s'), StringComparison.OrdinalIgnoreCase))
                {
                    return property;
                }
            }
            else if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
            {
                if (property.PropertyType.Name.Equals(relatedTableName, StringComparison.OrdinalIgnoreCase) ||
                    property.PropertyType.Name.Equals(relatedTableName.TrimEnd('s'), StringComparison.OrdinalIgnoreCase))
                {
                    return property;
                }
            }
        }

        return null;
    }

    private Expression BuildLikeExpression(Expression left, Expression right)
    {
        if (right is ConstantExpression { Value: string pattern })
        {
            var stringValue = left;
            
            if (pattern.StartsWith("%") && pattern.EndsWith("%"))
            {
                var value = pattern.Trim('%');
                var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;
                return Expression.Call(stringValue, containsMethod, Expression.Constant(value));
            }
            else if (pattern.StartsWith("%"))
            {
                var value = pattern.TrimStart('%');
                var endsWithMethod = typeof(string).GetMethod("EndsWith", [typeof(string)])!;
                return Expression.Call(stringValue, endsWithMethod, Expression.Constant(value));
            }
            else if (pattern.EndsWith("%"))
            {
                var value = pattern.TrimEnd('%');
                var startsWithMethod = typeof(string).GetMethod("StartsWith", [typeof(string)])!;
                return Expression.Call(stringValue, startsWithMethod, Expression.Constant(value));
            }
            else
            {
                return Expression.Equal(stringValue, Expression.Constant(pattern));
            }
        }

        throw new NotSupportedException("Dynamic LIKE patterns are not supported");
    }

    private bool IsNumericType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }

    private Type GetCommonNumericType(Type type1, Type type2)
    {
        if (type1 == typeof(decimal) || type2 == typeof(decimal))
            return typeof(decimal);
        if (type1 == typeof(double) || type2 == typeof(double))
            return typeof(double);
        if (type1 == typeof(float) || type2 == typeof(float))
            return typeof(float);
        if (type1 == typeof(long) || type2 == typeof(long))
            return typeof(long);
        return typeof(int);
    }

    private Expression ConvertConstantExpression(ConstantExpression constant, Type targetType)
    {
        if (constant.Value == null)
            return Expression.Constant(null, targetType);

        try
        {
            if (IsNumericType(targetType) && IsNumericType(constant.Type))
            {
                var convertedValue = Convert.ChangeType(constant.Value, targetType);
                return Expression.Constant(convertedValue, targetType);
            }

            var converted = Convert.ChangeType(constant.Value, targetType);
            return Expression.Constant(converted, targetType);
        }
        catch
        {
            return Expression.Convert(constant, targetType);
        }
    }
}