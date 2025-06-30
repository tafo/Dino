using Microsoft.EntityFrameworkCore;
using Dino.EFCore.Extensions;

// Setup in-memory database
var options = new DbContextOptionsBuilder<BlogContext>()
    .UseInMemoryDatabase("QuickStartDemo")
    .Options;

await using var context = new BlogContext(options);

// Seed some data
context.Blogs.AddRange(
    new Blog { Title = "C# Tips & Tricks", Rating = 5, IsActive = true },
    new Blog { Title = "EF Core Best Practices", Rating = 4, IsActive = true },
    new Blog { Title = "Old Blog", Rating = 3, IsActive = false }
);
context.SaveChanges();

// Example 1: Simple Dino query
Console.WriteLine("=== Active Blogs ===");
var activeBlogs = await context.Blogs
    .ToDinoListAsync("SELECT * FROM blogs WHERE isActive = true ORDER BY rating DESC");

foreach (var blog in activeBlogs)
{
    Console.WriteLine($"- {blog.Title} (Rating: {blog.Rating})");
}

// Example 2: Parameterized query
Console.WriteLine("\n=== High Rated Blogs ===");
var parameters = new Dictionary<string, object?> { ["minRating"] = 4 };
var highRatedBlogs = await context.Blogs
    .ToDinoListAsync("SELECT * FROM blogs WHERE rating >= @minRating", parameters);

foreach (var blog in highRatedBlogs)
{
    Console.WriteLine($"- {blog.Title}");
}

// Example 3: Count query
var activeCount = await context.Blogs
    .DinoCountAsync("SELECT * FROM blogs WHERE isActive = true");
Console.WriteLine($"\nTotal active blogs: {activeCount}");

// Models
public class Blog
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int Rating { get; set; }
    public bool IsActive { get; set; }
}

public class BlogContext(DbContextOptions<BlogContext> options) : DbContext(options)
{
    public DbSet<Blog> Blogs { get; set; }
}