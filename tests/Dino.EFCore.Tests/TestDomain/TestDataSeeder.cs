namespace Dino.EFCore.Tests.TestDomain;

public static class TestDataSeeder
{
    public static void SeedData(TestDbContext context)
    {
        // Clear existing data
        context.OrderItems.RemoveRange(context.OrderItems);
        context.Orders.RemoveRange(context.Orders);
        context.Users.RemoveRange(context.Users);
        context.Products.RemoveRange(context.Products);
        context.SaveChanges();

        // Add Users
        var users = new List<User>
        {
            new() { Id = 1, Name = "John Doe", Age = 30, Email = "john@example.com", Status = "active", CreatedAt = DateTime.Now.AddDays(-30) },
            new() { Id = 2, Name = "Jane Smith", Age = 25, Email = "jane@example.com", Status = "active", CreatedAt = DateTime.Now.AddDays(-20) },
            new() { Id = 3, Name = "Bob Johnson", Age = 35, Email = "bob@example.com", Status = "inactive", CreatedAt = DateTime.Now.AddDays(-15) },
            new() { Id = 4, Name = "Alice Brown", Age = 28, Email = "alice@example.com", Status = "pending", CreatedAt = DateTime.Now.AddDays(-10) },
            new() { Id = 5, Name = "Charlie Wilson", Age = 45, Email = "charlie@example.com", Status = "active", CreatedAt = DateTime.Now.AddDays(-5) }
        };
        context.Users.AddRange(users);

        // Add Products
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Laptop", Category = "Electronics", Price = 1200.00m, Stock = 10 },
            new() { Id = 2, Name = "Mouse", Category = "Electronics", Price = 25.00m, Stock = 50 },
            new() { Id = 3, Name = "Keyboard", Category = "Electronics", Price = 75.00m, Stock = 30 },
            new() { Id = 4, Name = "Monitor", Category = "Electronics", Price = 300.00m, Stock = 15 },
            new() { Id = 5, Name = "Desk", Category = "Furniture", Price = 500.00m, Stock = 5 },
            new() { Id = 6, Name = "Chair", Category = "Furniture", Price = 200.00m, Stock = 20 }
        };
        context.Products.AddRange(products);

        // Add Orders
        var orders = new List<Order>
        {
            new() 
            { 
                Id = 1, 
                OrderNumber = "ORD-001", 
                TotalAmount = 1225.00m, 
                OrderDate = DateTime.Now.AddDays(-7), 
                UserId = 1,
                Items =
                [
                    new OrderItem { Id = 1, ProductName = "Laptop", Quantity = 1, Price = 1200.00m },
                    new OrderItem { Id = 2, ProductName = "Mouse", Quantity = 1, Price = 25.00m }
                ]
            },
            new() 
            { 
                Id = 2, 
                OrderNumber = "ORD-002", 
                TotalAmount = 375.00m, 
                OrderDate = DateTime.Now.AddDays(-5), 
                UserId = 2,
                Items =
                [
                    new OrderItem { Id = 3, ProductName = "Keyboard", Quantity = 1, Price = 75.00m },
                    new OrderItem { Id = 4, ProductName = "Monitor", Quantity = 1, Price = 300.00m }
                ]
            },
            new() 
            { 
                Id = 3, 
                OrderNumber = "ORD-003", 
                TotalAmount = 700.00m, 
                OrderDate = DateTime.Now.AddDays(-3), 
                UserId = 1,
                Items =
                [
                    new OrderItem { Id = 5, ProductName = "Desk", Quantity = 1, Price = 500.00m },
                    new OrderItem { Id = 6, ProductName = "Chair", Quantity = 1, Price = 200.00m }
                ]
            }
        };
        context.Orders.AddRange(orders);

        context.SaveChanges();
    }
}