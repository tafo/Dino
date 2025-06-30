using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Dino.EFCore.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Configure services
builder.Services.AddDbContext<SampleDbContext>(options =>
    options.UseInMemoryDatabase("DinoSample"));

var host = builder.Build();

// Seed sample data
using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
    await SeedData(context);
    
    // Example 1: Simple query
    Console.WriteLine("=== Example 1: Simple Query ===");
    var activeUsers = await context.Users.ToDinoListAsync(
        "SELECT * FROM users WHERE isActive = true ORDER BY name");
    
    foreach (var user in activeUsers)
    {
        Console.WriteLine($"- {user.Name} ({user.Email})");
    }
    
    // Example 2: Query with parameters
    Console.WriteLine("\n=== Example 2: Parameterized Query ===");
    var parameters = new Dictionary<string, object?>
    {
        ["minOrders"] = 2,
        ["status"] = "Premium"
    };
    
    var premiumUsers = await context.Users.ToDinoListAsync(@"
        SELECT * FROM users 
        WHERE orderCount >= @minOrders 
        AND status = @status", parameters);
    
    foreach (var user in premiumUsers)
    {
        Console.WriteLine($"- {user.Name}: {user.OrderCount} orders");
    }
    
    // Example 3: Query with JOIN
    Console.WriteLine("\n=== Example 3: JOIN Query ===");
    var ordersWithUsers = await context.Orders.ToDinoListAsync(@"
        SELECT * FROM orders o
        JOIN users u ON o.userId = u.id
        WHERE o.total > 100
        ORDER BY o.orderDate DESC");
    
    foreach (var order in ordersWithUsers)
    {
        Console.WriteLine($"- Order #{order.OrderNumber}: ${order.Total} by {order.User.Name}");
    }
    
    // Example 4: Complex conditions
    Console.WriteLine("\n=== Example 4: Complex Query ===");
    var complexQuery = @"
        SELECT * FROM products 
        WHERE price BETWEEN 10 AND 100 
        AND category IN ('Electronics', 'Books')
        AND name LIKE '%Pro%'
        ORDER BY price DESC
        LIMIT 5";
    
    var products = await context.Products.ToDinoListAsync(complexQuery);
    
    foreach (var product in products)
    {
        Console.WriteLine($"- {product.Name} ({product.Category}): ${product.Price}");
    }
    
    // Example 5: Dynamic table query
    Console.WriteLine("\n=== Example 5: Dynamic Table Query ===");
    var dynamicResults = await context.ExecuteDinoQueryAsync(
        "SELECT * FROM users WHERE city = 'New York'");
    
    Console.WriteLine($"Found {dynamicResults.Count} users in New York");
    
    // Example 6: Aggregation queries
    Console.WriteLine("\n=== Example 6: Count Query ===");
    var activeCount = await context.Users.DinoCountAsync(
        "SELECT * FROM users WHERE isActive = true");
    
    Console.WriteLine($"Active users: {activeCount}");
    
    // Example 7: First or default
    Console.WriteLine("\n=== Example 7: First Query ===");
    var firstPremium = await context.Users.DinoFirstOrDefaultAsync(
        "SELECT * FROM users WHERE status = 'Premium' ORDER BY joinDate");
    
    if (firstPremium != null)
    {
        Console.WriteLine($"First premium user: {firstPremium.Name} (joined {firstPremium.JoinDate:yyyy-MM-dd})");
    }
}

static async Task SeedData(SampleDbContext context)
{
    // Clear existing data
    context.Products.RemoveRange(context.Products);
    context.Orders.RemoveRange(context.Orders);
    context.Users.RemoveRange(context.Users);
    await context.SaveChangesAsync();
    
    // Add users
    var users = new List<User>
    {
        new() { Id = 1, Name = "Alice Johnson", Email = "alice@email.com", City = "New York", 
                Status = "Premium", OrderCount = 5, IsActive = true, JoinDate = DateTime.Now.AddDays(-365) },
        new() { Id = 2, Name = "Bob Smith", Email = "bob@email.com", City = "Los Angeles", 
                Status = "Regular", OrderCount = 2, IsActive = true, JoinDate = DateTime.Now.AddDays(-180) },
        new() { Id = 3, Name = "Charlie Brown", Email = "charlie@email.com", City = "New York", 
                Status = "Premium", OrderCount = 8, IsActive = true, JoinDate = DateTime.Now.AddDays(-90) },
        new() { Id = 4, Name = "Diana Prince", Email = "diana@email.com", City = "Chicago", 
                Status = "Regular", OrderCount = 1, IsActive = false, JoinDate = DateTime.Now.AddDays(-30) },
        new() { Id = 5, Name = "Eve Wilson", Email = "eve@email.com", City = "New York", 
                Status = "Premium", OrderCount = 3, IsActive = true, JoinDate = DateTime.Now.AddDays(-60) }
    };
    context.Users.AddRange(users);
    
    // Add products
    var products = new List<Product>
    {
        new() { Id = 1, Name = "Laptop Pro", Category = "Electronics", Price = 1299.99m },
        new() { Id = 2, Name = "Wireless Mouse Pro", Category = "Electronics", Price = 79.99m },
        new() { Id = 3, Name = "Programming Book Pro", Category = "Books", Price = 49.99m },
        new() { Id = 4, Name = "Mechanical Keyboard", Category = "Electronics", Price = 149.99m },
        new() { Id = 5, Name = "Monitor Pro", Category = "Electronics", Price = 399.99m },
        new() { Id = 6, Name = "Design Book", Category = "Books", Price = 39.99m },
        new() { Id = 7, Name = "Office Chair", Category = "Furniture", Price = 299.99m }
    };
    context.Products.AddRange(products);
    
    // Add orders
    var orders = new List<Order>
    {
        new() { Id = 1, OrderNumber = "ORD-001", UserId = 1, Total = 1379.98m, 
                OrderDate = DateTime.Now.AddDays(-10) },
        new() { Id = 2, OrderNumber = "ORD-002", UserId = 2, Total = 79.99m, 
                OrderDate = DateTime.Now.AddDays(-5) },
        new() { Id = 3, OrderNumber = "ORD-003", UserId = 3, Total = 549.97m, 
                OrderDate = DateTime.Now.AddDays(-3) },
        new() { Id = 4, OrderNumber = "ORD-004", UserId = 1, Total = 299.99m, 
                OrderDate = DateTime.Now.AddDays(-1) }
    };
    context.Orders.AddRange(orders);
    
    await context.SaveChangesAsync();
}

// Entity models
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string City { get; set; } = "";
    public string Status { get; set; } = "";
    public int OrderCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinDate { get; set; }
    public List<Order> Orders { get; set; } = new();
}

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public decimal Total { get; set; }
    public DateTime OrderDate { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
}

// DbContext
public class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId);
    }
}