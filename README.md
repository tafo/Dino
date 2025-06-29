# Dino 🦕

A lightweight Domain Specific Language (DSL) for Entity Framework Core that allows you to write SQL-like queries as strings while maintaining the full power and safety of EF Core.

## Features

- ✅ SQL-like syntax for familiar query writing
- ✅ Full EF Core integration
- ✅ Type-safe parameter binding
- ✅ Support for WHERE, ORDER BY, GROUP BY, HAVING clauses
- ✅ Support for LIMIT/OFFSET pagination
- ✅ Support for IN, BETWEEN, LIKE operators
- ✅ Async query execution
- ✅ Extensible architecture

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

// With parameters
var parameters = new Dictionary<string, object?>
{
    ["minAge"] = 18,
    ["status"] = "active"
};

var adults = await context.Users
    .ToDinoListAsync("SELECT * FROM users WHERE age >= @minAge AND status = @status", parameters);

// Complex query
var query = @"
    SELECT * FROM orders 
    WHERE orderDate BETWEEN '2024-01-01' AND '2024-12-31'
    AND totalAmount > 1000
    ORDER BY orderDate DESC
    LIMIT 10
";

var topOrders = await context.Orders.ToDinoListAsync(query);
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

-- BETWEEN operator
WHERE age BETWEEN 18 AND 65
WHERE price BETWEEN 100.00 AND 500.00

-- LIKE operator
WHERE name LIKE 'John%'
WHERE email LIKE '%@gmail.com'
WHERE description LIKE '%important%'

-- NULL checks
WHERE deletedAt IS NULL
WHERE deletedAt IS NOT NULL
```

### Parameters
```sql
WHERE age > @minAge
WHERE status = @status
WHERE createdAt BETWEEN @startDate AND @endDate
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
```

## Advanced Usage

### Using with Complex Queries

```csharp
var query = @"
    SELECT * FROM products 
    WHERE price BETWEEN @minPrice AND @maxPrice
    AND category = @category
    AND name LIKE @searchTerm
    ORDER BY price DESC, name ASC
    LIMIT @pageSize OFFSET @offset
";

var parameters = new Dictionary<string, object?>
{
    ["minPrice"] = 50m,
    ["maxPrice"] = 500m,
    ["category"] = "Electronics",
    ["searchTerm"] = "%laptop%",
    ["pageSize"] = 10,
    ["offset"] = 20
};

var products = await context.Products.ToDinoListAsync(query, parameters);
```

### Building Dynamic Queries

```csharp
var queryBuilder = new StringBuilder("SELECT * FROM users WHERE 1=1");

if (!string.IsNullOrEmpty(searchTerm))
{
    queryBuilder.Append(" AND name LIKE @searchTerm");
}

if (minAge.HasValue)
{
    queryBuilder.Append(" AND age >= @minAge");
}

var results = await context.Users.ToDinoListAsync(
    queryBuilder.ToString(), 
    new Dictionary<string, object?>
    {
        ["searchTerm"] = $"%{searchTerm}%",
        ["minAge"] = minAge
    });
```

## Architecture

Dino consists of three main components:

1. **Lexer**: Tokenizes the SQL-like string into tokens
2. **Parser**: Builds an Abstract Syntax Tree (AST) from tokens
3. **Expression Visitor**: Converts the AST into LINQ Expression Trees

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Inspired by various SQL DSL implementations
- Built on top of Entity Framework Core
- Thanks to all contributors

## Roadmap

- [ ] Support for JOINs
- [ ] Support for GROUP BY and aggregations
- [ ] Support for subqueries
- [ ] Support for CASE WHEN expressions
- [ ] Support for window functions
- [ ] Query validation and IntelliSense