namespace Dino.EFCore.Tests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Extensions;
using TestDomain;

public class DinoQueryBuilderTests : IDisposable
{
    private readonly TestDbContext _context;

    public DinoQueryBuilderTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        TestDataSeeder.SeedData(_context);
    }

    [Fact]
    public async Task BuildQuery_SimpleSelect_ReturnsAllRecords()
    {
        // Arrange
        var query = "SELECT * FROM users";

        // Act
        var result = await _context.Users.ToDinoListAsync(query);

        // Assert
        result.Should().HaveCount(5);
        result.Should().Contain(u => u.Name == "John Doe");
        result.Should().Contain(u => u.Name == "Jane Smith");
    }

    [Fact]
    public async Task BuildQuery_WhereClause_FiltersCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE age > 30";

        // Act
        var result = await _context.Users.ToDinoListAsync(query);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Name == "Bob Johnson");
        result.Should().Contain(u => u.Name == "Charlie Wilson");
    }

    [Fact]
    public async Task BuildQuery_WhereWithMultipleConditions_FiltersCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE age > 25 AND status = 'active'";

        // Act
        var result = await _context.Users.ToDinoListAsync(query);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Name == "John Doe");
        result.Should().Contain(u => u.Name == "Charlie Wilson");
    }

    [Fact]
    public async Task BuildQuery_WhereWithIn_FiltersCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE status IN ('active', 'pending')";

        // Act
        var result = await _context.Users.ToDinoListAsync(query);

        // Assert
        result.Should().HaveCount(4);
        result.Should().NotContain(u => u.Status == "inactive");
    }

    [Fact]
    public async Task BuildQuery_WhereWithBetween_FiltersCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE age BETWEEN 25 AND 35";

        // Act
        var result = await _context.Users.ToDinoListAsync(query);

        // Assert
        result.Should().HaveCount(4);
        result.All(u => u.Age is >= 25 and <= 35).Should().BeTrue();
    }

    [Fact]
    public async Task BuildQuery_WhereWithLike_FiltersCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM products WHERE name LIKE 'M%'";

        // Act
        var result = await _context.Products.ToDinoListAsync(query);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Name == "Mouse");
        result.Should().Contain(p => p.Name == "Monitor");
    }

    [Fact]
    public async Task BuildQuery_OrderBy_SortsCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users ORDER BY age DESC";

        // Act
        var result = await _context.Users.ToDinoListAsync(query);

        // Assert
        result.Should().HaveCount(5);
        result[0].Name.Should().Be("Charlie Wilson"); // Age 45
        result[1].Name.Should().Be("Bob Johnson");    // Age 35
        result[2].Name.Should().Be("John Doe");       // Age 30
    }

    [Fact]
    public async Task BuildQuery_OrderByMultiple_SortsCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users ORDER BY status ASC, age DESC";

        // Act
        var result = await _context.Users.ToDinoListAsync(query);

        // Assert
        result.Should().HaveCount(5);
        // First should be active users ordered by age desc
        result.First(u => u.Status == "active").Age.Should().Be(45); // Charlie Wilson
    }

    [Fact]
    public async Task BuildQuery_WithLimit_ReturnsLimitedRecords()
    {
        // Arrange
        var query = "SELECT * FROM users LIMIT 3";

        // Act
        var result = await _context.Users.ToDinoListAsync(query);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task BuildQuery_WithLimitAndOffset_ReturnsPaginatedRecords()
    {
        // Arrange
        var query = "SELECT * FROM users ORDER BY id LIMIT 2 OFFSET 2";

        // Act
        var result = await _context.Users.ToDinoListAsync(query);

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(3);
        result[1].Id.Should().Be(4);
    }

    [Fact]
    public async Task BuildQuery_WithDistinct_ReturnsDistinctRecords()
    {
        // Arrange
        var query = "SELECT DISTINCT * FROM products WHERE category = 'Electronics'";

        // Act
        var result = await _context.Products.ToDinoListAsync(query);

        // Assert
        result.Should().HaveCount(4);
        result.All(p => p.Category == "Electronics").Should().BeTrue();
    }

    [Fact]
    public async Task BuildQuery_WithParameters_ReplacesCorrectly()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE age > @minAge AND status = @status";
        var parameters = new Dictionary<string, object?>
        {
            ["minAge"] = 28,
            ["status"] = "active"
        };

        // Act
        var result = await _context.Users.ToDinoListAsync(query, parameters);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Name == "John Doe");
        result.Should().Contain(u => u.Name == "Charlie Wilson");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithCondition_ReturnsFirstMatch()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE status = 'active' ORDER BY age DESC";

        // Act
        var result = await _context.Users.DinoFirstOrDefaultAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Charlie Wilson");
    }

    [Fact]
    public async Task CountAsync_WithCondition_ReturnsCorrectCount()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE age >= 30";

        // Act
        var count = await _context.Users.DinoCountAsync(query);

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task AnyAsync_WithCondition_ReturnsTrueWhenExists()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE email = 'john@example.com'";

        // Act
        var exists = await _context.Users.DinoAnyAsync(query);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_WithCondition_ReturnsFalseWhenNotExists()
    {
        // Arrange
        var query = "SELECT * FROM users WHERE email = 'nonexistent@example.com'";

        // Act
        var exists = await _context.Users.DinoAnyAsync(query);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task BuildQuery_ComplexQuery_WorksCorrectly()
    {
        // Arrange
        var query = @"SELECT * FROM products 
                      WHERE price BETWEEN 50 AND 500 
                      AND category = 'Electronics' 
                      ORDER BY price DESC 
                      LIMIT 3";

        // Act
        var result = await _context.Products.ToDinoListAsync(query);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Monitor");  // 300
        result[1].Name.Should().Be("Keyboard"); // 75
        result.All(p => p.Category == "Electronics").Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}