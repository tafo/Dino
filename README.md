# Dino 🦕

A lightweight Domain Specific Language (DSL) for Entity Framework Core that allows you to write SQL-like queries as strings while maintaining the full power and safety of EF Core.

[![NuGet](https://img.shields.io/nuget/v/Dino.EFCore.svg)](https://www.nuget.org/packages/Dino.EFCore)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Why Dino?

Dino bridges the gap between SQL and LINQ, perfect for:
- **Dynamic Query Building** - Construct queries from user input or configuration
- **SQL-First Developers** - Use familiar SQL syntax with EF Core
- **Report Generation** - Build complex queries dynamically
- **API Query Languages** - Expose a safe SQL-like query interface

## Features

- ✅ **SQL-like syntax** for familiar query writing
- ✅ **Full EF Core integration** - Works with your existing DbContext
- ✅ **Type-safe** parameter binding
- ✅ **Comprehensive SQL support**:
    - WHERE, ORDER BY, GROUP BY, HAVING clauses
    - JOINs with navigation property support
    - LIMIT/OFFSET pagination
    - IN, BETWEEN, LIKE, IS NULL operators
    - DISTINCT queries
    - Complex conditions with AND/OR
- ✅ **Dynamic query execution** - Query any table dynamically
- ✅ **Async/await** support throughout
- ✅ **Extensible** architecture

## Installation

```bash
dotnet add package Dino.EFCore
```

## Quick Start

```csharp
using Dino.EFCore.Extensions;

// Simple query
var activeUsers = await context.Users
    .ToDinoListAsync("SELECT * FROM users WHERE status = 'active'");

// With parameters (SQL injection safe!)
var parameters = new Dictionary<string, object?>
{
    ["minAge"] = 18,
    ["status"] = "active"
};

var adults = await context.Users
    .ToDinoListAsync(@"
        SELECT * FROM users 
        WHERE age >= @minAge 
        AND status = @status 
        ORDER BY name", parameters);
```

## Advanced Examples

### Dynamic Queries from User Input

```csharp
// Build queries dynamically based on user filters
public async Task<List<Product>> SearchProducts(ProductFilter filter)
{
    var query = new StringBuilder("SELECT * FROM products WHERE 1=1");
    var parameters = new Dictionary<string, object?>();

    if (!string.IsNullOrEmpty(filter.Category))
    {
        query.Append(" AND category = @category");
        parameters["category"] = filter.Category;
    }

    if (filter.MinPrice.HasValue)
    {
        query.Append(" AND price >= @minPrice");
        parameters["minPrice"] = filter.MinPrice.Value;
    }

    if (!string.IsNullOrEmpty(filter.SearchTerm))
    {
        query.Append(" AND name LIKE @search");
        parameters["search"] = $"%{filter.SearchTerm}%";
    }

    query.Append(" ORDER BY price ASC");

    return await context.Products.ToDinoListAsync(query.ToString(), parameters);
}
```

### Complex Business Queries

```csharp
// Get orders with customer information and items
var complexQuery = @"
    SELECT * FROM orders o
    JOIN users u ON o.userId = u.id
    WHERE o.orderDate BETWEEN @startDate AND @endDate
    AND o.totalAmount > @minAmount
    AND u.status = 'active'
    ORDER BY o.orderDate DESC
    LIMIT 100";

var parameters = new Dictionary<string, object?>
{
    ["startDate"] = new DateTime(2024, 1, 1),
    ["endDate"] = new DateTime(2024, 12, 31),
    ["minAmount"] = 1000m
};

var orders = await context.Orders.ToDinoListAsync(complexQuery, parameters);
```

### Dynamic Table Queries

```csharp
// Query any table dynamically
public async Task<List<object>> ExecuteDynamicQuery(string tableName, string conditions)
{
    var query = $"SELECT * FROM {tableName} WHERE {conditions}";
    return await context.ExecuteDinoQueryAsync(query);
}

// Get available tables
var tables = context.GetDinoTableNames();
// Returns: ["users", "orders", "products", ...]
```

### Pagination

```csharp
public async Task<PagedResult<T>> GetPagedAsync<T>(
    IQueryable<T> source, 
    int page, 
    int pageSize,
    string? orderBy = null) where T : class
{
    var countQuery = "SELECT * FROM items";
    var totalCount = await source.DinoCountAsync(countQuery);

    var query = $@"
        SELECT * FROM items 
        {(orderBy != null ? $"ORDER BY {orderBy}" : "")}
        LIMIT {pageSize} OFFSET {(page - 1) * pageSize}";

    var items = await source.ToDinoListAsync(query);

    return new PagedResult<T>
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
```

## Supported SQL Features

### Basic Queries
```sql
SELECT * FROM users
SELECT * FROM users WHERE age > 18
SELECT * FROM users ORDER BY name ASC, age DESC
SELECT * FROM users LIMIT 10 OFFSET 20
SELECT DISTINCT * FROM users WHERE status = 'active'
```

### WHERE Conditions
```sql
-- Comparison operators
WHERE age = 25
WHERE age != 25
WHERE age > 25
WHERE age >= 25
WHERE age < 25
WHERE age <= 25

-- Logical operators
WHERE age > 18 AND status = 'active'
WHERE status = 'active' OR status = 'pending'
WHERE NOT (status = 'inactive')

-- IN operator
WHERE status IN ('active', 'pending', 'approved')
WHERE categoryId IN (1, 2, 3)

-- BETWEEN operator
WHERE age BETWEEN 18 AND 65
WHERE price BETWEEN 100.00 AND 500.00
WHERE orderDate BETWEEN '2024-01-01' AND '2024-12-31'

-- LIKE operator (automatically uses EF Core's pattern matching)
WHERE name LIKE 'John%'      -- StartsWith
WHERE email LIKE '%@gmail.com' -- EndsWith
WHERE description LIKE '%important%' -- Contains

-- NULL checks
WHERE deletedAt IS NULL
WHERE deletedAt IS NOT NULL
```

### JOINs
```sql
-- Inner Join
SELECT * FROM orders o
JOIN users u ON o.userId = u.id

-- Multiple Joins
SELECT * FROM orderItems oi
JOIN orders o ON oi.orderId = o.id
JOIN users u ON o.userId = u.id
WHERE u.status = 'active'
```

### Parameters
```sql
-- Named parameters (recommended)
WHERE age > @minAge
WHERE status = @status
WHERE createdAt BETWEEN @startDate AND @endDate
WHERE tags IN (@tag1, @tag2, @tag3)
```

## API Reference

### Extension Methods

```csharp
// Convert IQueryable to Dino queryable
IDinoQueryable<T> AsDinoQueryable<T>(this IQueryable<T> source)

// Execute query and return list
Task<List<T>> ToDinoListAsync<T>(this IQueryable<T> source, string dsl)
Task<List<T>> ToDinoListAsync<T>(this IQueryable<T> source, string dsl, IDictionary<string, object?> parameters)

// Get first or default
Task<T?> DinoFirstOrDefaultAsync<T>(this IQueryable<T> source, string dsl)
Task<T?> DinoFirstOrDefaultAsync<T>(this IQueryable<T> source, string dsl, IDictionary<string, object?> parameters)

// Count records
Task<int> DinoCountAsync<T>(this IQueryable<T> source, string dsl)
Task<int> DinoCountAsync<T>(this IQueryable<T> source, string dsl, IDictionary<string, object?> parameters)

// Check if any records exist
Task<bool> DinoAnyAsync<T>(this IQueryable<T> source, string dsl)
Task<bool> DinoAnyAsync<T>(this IQueryable<T> source, string dsl, IDictionary<string, object?> parameters)

// Dynamic table queries
Task<List<object>> ExecuteDinoQueryAsync(this DbContext context, string query)
Task<List<T>> ExecuteDinoQueryAsync<T>(this DbContext context, string query)
List<string> GetDinoTableNames(this DbContext context)
```

## Integration with Entity Framework Core

Dino works seamlessly with your existing EF Core setup:

```csharp
public class ProductService
{
    private readonly AppDbContext _context;

    public async Task<List<Product>> GetProductsByDynamicQuery(string userQuery)
    {
        // Dino automatically includes related data when JOINs are detected
        var query = @"
            SELECT * FROM products p
            JOIN categories c ON p.categoryId = c.id
            WHERE p.price > 100
            AND c.name = 'Electronics'";

        // This returns Product entities with Category navigation property loaded
        return await _context.Products.ToDinoListAsync(query);
    }
}
```

## Performance Considerations

- Dino translates queries to LINQ expressions, maintaining EF Core's query optimization
- Use parameters instead of string concatenation for better performance and security
- Complex queries are translated to efficient SQL by EF Core
- Supports EF Core's query caching

## Security

- **Always use parameters** for user input to prevent SQL injection
- Table and column names are validated against your EF Core model
- Only supports SELECT queries (no data modification)

## Error Handling

```csharp
try
{
    var results = await context.Users.ToDinoListAsync(query, parameters);
}
catch (DinoParserException ex)
{
    // Syntax error in query
    Console.WriteLine($"Query syntax error: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    // Invalid table or column name
    Console.WriteLine($"Query error: {ex.Message}");
}
```

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Setup

```bash
# Clone the repository
git clone https://github.com/yourusername/dino.git
cd dino

# Build the solution
dotnet build

# Run tests
dotnet test

# Pack NuGet package
dotnet pack -c Release
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Roadmap

### Version 2.0
- [ ] Support for GROUP BY and aggregations (COUNT, SUM, AVG, etc.)
- [ ] Support for subqueries
- [ ] Support for UNION/INTERSECT/EXCEPT

### Version 3.0
- [ ] Support for CTEs (Common Table Expressions)
- [ ] Support for window functions
- [ ] Query validation and IntelliSense
- [ ] Visual query builder

## Acknowledgments

- Inspired by various SQL DSL implementations
- Built on top of Entity Framework Core
- Special thanks to all contributors

---

**Note**: Dino is designed for SELECT queries only. For data modifications, use standard EF Core methods to maintain proper change tracking and data integrity.