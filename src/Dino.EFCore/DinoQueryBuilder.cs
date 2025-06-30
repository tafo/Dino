namespace Dino.EFCore;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Core.Parsing;
using Core.Ast.Clauses;
using Core.Ast;
using Core.Ast.Queries;
using Core.Ast.Expressions;
using Visitors;

public class DinoQueryBuilder<T>(IQueryable<T> source) : IDinoQueryable<T>
    where T : class
{
    private readonly IQueryable<T> _source = source ?? throw new ArgumentNullException(nameof(source));
    private readonly IDinoParser _parser = new DinoParser();

    public IQueryable<T> BuildQuery(string dsl)
    {
        return BuildQuery(dsl, null);
    }

    public IQueryable<T> BuildQuery(string dsl, IDictionary<string, object?>? parameters)
    {
        var ast = _parser.Parse(dsl, parameters);

        // If there are JOINs, we need to build a completely new query
        // Otherwise, use the simple approach
        return ast.FromClause is { Joins.Count: > 0 }
            ? BuildJoinQuery(ast)
            : BuildSimpleQuery(_source, ast);
    }

    private IQueryable<T> BuildSimpleQuery(IQueryable<T> query, DinoSelectQuery ast)
    {
        // Apply WHERE clause
        if (ast.WhereClause != null)
        {
            var visitor = new DinoExpressionVisitor<T>();
            var whereExpression = visitor.BuildWhereExpression(ast.WhereClause);
            query = query.Where(whereExpression);
        }

        // Apply ORDER BY clause
        if (ast.OrderByClause != null)
        {
            query = ApplyOrderBy(query, ast.OrderByClause);
        }

        // Apply DISTINCT
        if (ast.IsDistinct)
        {
            query = query.Distinct();
        }

        // Apply LIMIT/OFFSET
        if (ast.Offset.HasValue)
        {
            query = query.Skip(ast.Offset.Value);
        }

        if (ast.Limit.HasValue)
        {
            query = query.Take(ast.Limit.Value);
        }

        return query;
    }

    private IQueryable<T> BuildJoinQuery(DinoSelectQuery ast)
    {
        var query = _source;
        
        // Apply Includes for each JOIN
        foreach (var join in ast.FromClause!.Joins)
        {
            var navigationProperty = FindNavigationProperty(typeof(T), join.TableSource.TableName);
            if (navigationProperty != null)
            {
                query = query.Include(navigationProperty.Name);
            }
        }

        // Now we need to handle WHERE conditions that might reference joined tables
        if (ast.WhereClause != null)
        {
            try
            {
                // Create a custom expression builder that can handle joined table references
                var whereExpression = BuildJoinWhereExpression(ast.FromClause, ast.WhereClause);
                query = query.Where(whereExpression);
            }
            catch
            {
                // If we can't build WHERE expression for joins, try simple expression
                var visitor = new DinoExpressionVisitor<T>();
                var whereExpression = visitor.BuildWhereExpression(ast.WhereClause);
                query = query.Where(whereExpression);
            }
        }

        // Apply ORDER BY
        if (ast.OrderByClause != null)
        {
            query = ApplyJoinOrderBy(query, ast.OrderByClause, ast.FromClause);
        }

        // Apply DISTINCT
        if (ast.IsDistinct)
        {
            query = query.Distinct();
        }

        // Apply LIMIT/OFFSET
        if (ast.Offset.HasValue)
        {
            query = query.Skip(ast.Offset.Value);
        }

        if (ast.Limit.HasValue)
        {
            query = query.Take(ast.Limit.Value);
        }

        return query;
    }

    private Expression<Func<T, bool>> BuildJoinWhereExpression(DinoFromClause fromClause, DinoWhereClause whereClause)
    {
        var visitor = new DinoJoinExpressionVisitor<T>(fromClause);
        return visitor.BuildWhereExpression(whereClause);
    }

    private IQueryable<T> ApplyJoinOrderBy(IQueryable<T> query, DinoOrderByClause orderByClause, DinoFromClause fromClause)
    {
        IOrderedQueryable<T>? orderedQuery = null;
        var visitor = new DinoJoinExpressionVisitor<T>(fromClause);

        foreach (var item in orderByClause.Items)
        {
            var orderExpression = visitor.BuildOrderByExpression(item.Expression);
            
            if (orderExpression != null)
            {
                if (orderedQuery == null)
                {
                    orderedQuery = item.Direction == DinoOrderDirection.Ascending
                        ? query.OrderBy(orderExpression)
                        : query.OrderByDescending(orderExpression);
                }
                else
                {
                    orderedQuery = item.Direction == DinoOrderDirection.Ascending
                        ? orderedQuery.ThenBy(orderExpression)
                        : orderedQuery.ThenByDescending(orderExpression);
                }
            }
        }

        return orderedQuery ?? query;
    }

    public async Task<List<T>> ToListAsync(string dsl, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(dsl);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<T>> ToListAsync(string dsl, IDictionary<string, object?> parameters, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(dsl, parameters);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<T?> FirstOrDefaultAsync(string dsl, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(dsl);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<T?> FirstOrDefaultAsync(string dsl, IDictionary<string, object?> parameters, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(dsl, parameters);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CountAsync(string dsl, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(dsl);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> CountAsync(string dsl, IDictionary<string, object?> parameters, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(dsl, parameters);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(string dsl, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(dsl);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(string dsl, IDictionary<string, object?> parameters, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(dsl, parameters);
        return await query.AnyAsync(cancellationToken);
    }

    private System.Reflection.PropertyInfo? FindNavigationProperty(Type entityType, string relatedTableName)
    {
        // Try to find a navigation property that matches the table name
        var properties = entityType.GetProperties();
        
        // First try exact match (singular or plural)
        var exactMatch = properties.FirstOrDefault(p => 
            p.Name.Equals(relatedTableName, StringComparison.OrdinalIgnoreCase) ||
            p.Name.Equals(relatedTableName + "s", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Equals(relatedTableName.TrimEnd('s'), StringComparison.OrdinalIgnoreCase));
        
        if (exactMatch != null)
            return exactMatch;

        // Try to find by type name
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

    private IQueryable<T> ApplyOrderBy(IQueryable<T> query, DinoOrderByClause orderByClause)
    {
        IOrderedQueryable<T>? orderedQuery = null;
        var entityType = typeof(T);

        foreach (var item in orderByClause.Items)
        {
            // For now, only support simple property ordering
            if (item.Expression is DinoIdentifierExpression identifier)
            {
                var property = entityType.GetProperty(identifier.Name, 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.IgnoreCase);

                if (property == null)
                {
                    throw new InvalidOperationException($"Property '{identifier.Name}' not found on type '{entityType.Name}'");
                }

                var parameter = Expression.Parameter(entityType, "x");
                var memberAccess = Expression.MakeMemberAccess(parameter, property);
                var orderByExpression = Expression.Lambda(memberAccess, parameter);

                if (orderedQuery == null)
                {
                    orderedQuery = item.Direction == DinoOrderDirection.Ascending
                        ? Queryable.OrderBy(query, (dynamic)orderByExpression)
                        : Queryable.OrderByDescending(query, (dynamic)orderByExpression);
                }
                else
                {
                    orderedQuery = item.Direction == DinoOrderDirection.Ascending
                        ? Queryable.ThenBy(orderedQuery, (dynamic)orderByExpression)
                        : Queryable.ThenByDescending(orderedQuery, (dynamic)orderByExpression);
                }
            }
            else
            {
                throw new NotSupportedException("Complex ORDER BY expressions are not yet supported");
            }
        }

        return orderedQuery ?? query;
    }
}