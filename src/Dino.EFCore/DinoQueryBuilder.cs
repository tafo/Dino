namespace Dino.EFCore;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Core.Parsing;
using Core.Ast.Clauses;
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
        var query = _source;

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

    private IQueryable<T> ApplyOrderBy(IQueryable<T> query, DinoOrderByClause orderByClause)
    {
        IOrderedQueryable<T>? orderedQuery = null;
        var entityType = typeof(T);

        foreach (var item in orderByClause.Items)
        {
            // For now, only support simple property ordering
            if (item.Expression is Dino.Core.Ast.Expressions.DinoIdentifierExpression identifier)
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
                    orderedQuery = item.Direction == Core.Ast.DinoOrderDirection.Ascending
                        ? Queryable.OrderBy(query, (dynamic)orderByExpression)
                        : Queryable.OrderByDescending(query, (dynamic)orderByExpression);
                }
                else
                {
                    orderedQuery = item.Direction == Core.Ast.DinoOrderDirection.Ascending
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