namespace Dino.EFCore.Extensions;

using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Core.Parsing;

public static class DinoDbContextExtensions
{
    /// <summary>
    /// Execute a Dino query on the appropriate DbSet based on the FROM clause
    /// </summary>
    public static async Task<List<object>> ExecuteDinoQueryAsync(
        this DbContext context, 
        string query, 
        IDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        // Parse the query to get the main table name
        var parser = new DinoParser();
        var ast = parser.Parse(query, parameters);
        
        if (ast.FromClause == null)
        {
            throw new InvalidOperationException("Query must have a FROM clause");
        }
        
        var tableName = ast.FromClause.TableSource.TableName;
        
        // Find the DbSet for this table
        var dbSetProperty = FindDbSetProperty(context, tableName);
        if (dbSetProperty == null)
        {
            throw new InvalidOperationException($"No DbSet found for table '{tableName}'");
        }
        
        // Get the entity type
        var entityType = dbSetProperty.PropertyType.GetGenericArguments()[0];
        
        // Get the DbSet instance
        var dbSet = dbSetProperty.GetValue(context);
        if (dbSet == null)
        {
            throw new InvalidOperationException($"DbSet for table '{tableName}' is null");
        }
        
        // Create the generic method call
        var executeMethod = typeof(DinoDbContextExtensions)
            .GetMethod(nameof(ExecuteTypedDinoQueryAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(entityType);
        
        // Execute the query
        var task = (Task<List<object>>)executeMethod.Invoke(null, [dbSet, query, parameters, cancellationToken])!;
        return await task;
    }
    
    /// <summary>
    /// Execute a Dino query and return typed results
    /// </summary>
    public static async Task<List<T>> ExecuteDinoQueryAsync<T>(
        this DbContext context, 
        string query, 
        IDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // Parse the query to get the main table name
        var parser = new DinoParser();
        var ast = parser.Parse(query, parameters);
        
        if (ast.FromClause == null)
        {
            throw new InvalidOperationException("Query must have a FROM clause");
        }
        
        var tableName = ast.FromClause.TableSource.TableName;
        
        // Find the DbSet for this table
        var dbSetProperty = FindDbSetProperty(context, tableName);
        if (dbSetProperty == null)
        {
            throw new InvalidOperationException($"No DbSet found for table '{tableName}'");
        }
        
        // Verify the type matches
        var dbSetEntityType = dbSetProperty.PropertyType.GetGenericArguments()[0];
        if (dbSetEntityType != typeof(T))
        {
            throw new InvalidOperationException($"Table '{tableName}' is of type {dbSetEntityType.Name}, not {typeof(T).Name}");
        }
        
        // Get the DbSet<T>
        var dbSet = (DbSet<T>)dbSetProperty.GetValue(context)!;
        
        // Execute using existing extension method
        if (parameters != null)
        {
            return await dbSet.ToDinoListAsync(query, parameters, cancellationToken);
        }
        else
        {
            return await dbSet.ToDinoListAsync(query, cancellationToken);
        }
    }
    
    private static async Task<List<object>> ExecuteTypedDinoQueryAsync<T>(
        IQueryable<T> dbSet,
        string query,
        IDictionary<string, object?>? parameters,
        CancellationToken cancellationToken) where T : class
    {
        List<T> result;
        if (parameters != null)
        {
            result = await dbSet.ToDinoListAsync(query, parameters, cancellationToken);
        }
        else
        {
            result = await dbSet.ToDinoListAsync(query, cancellationToken);
        }
        return result.Cast<object>().ToList();
    }
    
    private static PropertyInfo? FindDbSetProperty(DbContext context, string tableName)
    {
        var contextType = context.GetType();
        var properties = contextType.GetProperties();
        
        // Try exact match first
        var exactMatch = properties.FirstOrDefault(p =>
            p.PropertyType.IsGenericType &&
            p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
            (p.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
             p.Name.Equals(tableName + "s", StringComparison.OrdinalIgnoreCase)));
        
        if (exactMatch != null)
            return exactMatch;
        
        // Try by entity type name
        foreach (var property in properties)
        {
            if (property.PropertyType.IsGenericType &&
                property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            {
                var entityType = property.PropertyType.GetGenericArguments()[0];
                if (entityType.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                    entityType.Name.Equals(tableName.TrimEnd('s'), StringComparison.OrdinalIgnoreCase))
                {
                    return property;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Get available table names from the DbContext
    /// </summary>
    public static List<string> GetDinoTableNames(this DbContext context)
    {
        var tables = new List<string>();
        var contextType = context.GetType();
        var properties = contextType.GetProperties();
        
        foreach (var property in properties)
        {
            if (property.PropertyType.IsGenericType &&
                property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            {
                // Add both the property name and the entity type name
                tables.Add(property.Name.ToLower());
                
                var entityType = property.PropertyType.GetGenericArguments()[0];
                tables.Add(entityType.Name.ToLower());
                
                // Add plural form if not already plural
                if (!property.Name.EndsWith("s"))
                    tables.Add(property.Name.ToLower() + "s");
                if (!entityType.Name.EndsWith("s"))
                    tables.Add(entityType.Name.ToLower() + "s");
            }
        }
        
        return tables.Distinct().OrderBy(t => t).ToList();
    }
}