namespace Dino.EFCore.Tests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Extensions;
using TestDomain;

public class DinoDynamicQueryTests : IDisposable
{
    private readonly TestDbContext _context;

    public DinoDynamicQueryTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        TestDataSeeder.SeedData(_context);
    }

    [Fact]
    public async Task ExecuteDinoQueryAsync_SimpleSelectFromUsers_ReturnsUsers()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE age > 25";

        // Act
        var result = await _context.ExecuteDinoQueryAsync(query);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(4);
        result.All(r => ((dynamic)r).Age > 25).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteDinoQueryAsync_SelectFromOrders_ReturnsOrders()
    {
        // Arrange
        var query = "SELECT * FROM orders WHERE totalAmount > 500";

        // Act
        var result = await _context.ExecuteDinoQueryAsync(query);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(2);
        result.All(r => ((dynamic)r).TotalAmount > 500).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteDinoQueryAsync_SelectFromProducts_ReturnsProducts()
    {
        // Arrange
        var query = "SELECT * FROM products WHERE category = 'Electronics' ORDER BY price DESC";

        // Act
        var result = await _context.ExecuteDinoQueryAsync(query);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(4);
        result.All(r => ((dynamic)r).Category == "Electronics").Should().BeTrue();
        
        // Check ordering
        var prices = result.Select(r => ((dynamic)r).Price).ToList();
        prices.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task ExecuteDinoQueryAsync_WithJoin_ReturnsCorrectData()
    {
        // Arrange
        var query = @"SELECT * FROM users u 
                      JOIN orders o ON u.id = o.userId 
                      WHERE o.totalAmount > 1000";

        // Act
        var result = await _context.ExecuteDinoQueryAsync(query);

        // Assert
        result.Should().NotBeEmpty();
        var users = result.Cast<User>().ToList();
        users.Should().Contain(u => u.Name == "John Doe");
    }

    [Fact]
    public async Task ExecuteDinoQueryAsync_WithParameters_WorksCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM products WHERE price BETWEEN @minPrice AND @maxPrice";
        var parameters = new Dictionary<string, object?>
        {
            ["minPrice"] = 50m,
            ["maxPrice"] = 300m
        };

        // Act
        var result = await _context.ExecuteDinoQueryAsync(query, parameters);

        // Assert
        result.Should().NotBeEmpty();
        result.All(r => ((dynamic)r).Price >= 50m && ((dynamic)r).Price <= 300m).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteDinoQueryAsync_TypedVersion_ReturnsTypedResults()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE status = 'active' ORDER BY name";

        // Act
        var result = await _context.ExecuteDinoQueryAsync<User>(query);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().BeOfType<List<User>>();
        result.All(u => u.Status == "active").Should().BeTrue();
        result.Should().BeInAscendingOrder(u => u.Name);
    }

    [Fact]
    public async Task ExecuteDinoQueryAsync_InvalidTable_ThrowsException()
    {
        // Arrange
        var query = "SELECT * FROM nonexistenttable";

        // Act & Assert
        var act = async () => await _context.ExecuteDinoQueryAsync(query);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No DbSet found for table 'nonexistenttable'");
    }

    [Fact]
    public async Task ExecuteDinoQueryAsync_NoFromClause_ThrowsException()
    {
        // Arrange
        var query = "SELECT 1";

        // Act & Assert
        var act = async () => await _context.ExecuteDinoQueryAsync(query);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Query must have a FROM clause");
    }

    [Fact]
    public async Task GetDinoTableNames_ReturnsAllAvailableTables()
    {
        // Act
        var tables = _context.GetDinoTableNames();

        // Assert
        tables.Should().Contain("users");
        tables.Should().Contain("user");
        tables.Should().Contain("orders");
        tables.Should().Contain("order");
        tables.Should().Contain("products");
        tables.Should().Contain("product");
        tables.Should().Contain("orderitems");
        tables.Should().Contain("orderitem");
    }

    [Fact]
    public async Task ExecuteDinoQueryAsync_ComplexJoinQuery_WorksCorrectly()
    {
        // Arrange - A query that could come from an ERP user
        var query = @"
            SELECT * FROM orders o
            JOIN users u ON o.userId = u.id
            JOIN orderitems oi ON o.id = oi.orderId
            WHERE u.status = 'active' 
            AND o.totalAmount > 500
            ORDER BY o.orderDate DESC
            LIMIT 10";

        // Act
        var result = await _context.ExecuteDinoQueryAsync(query);

        // Assert
        result.Should().NotBeEmpty();
        var orders = result.Cast<Order>().ToList();
        orders.All(o => o.User.Status == "active").Should().BeTrue();
        orders.All(o => o.TotalAmount > 500).Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}