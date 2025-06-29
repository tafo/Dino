namespace Dino.EFCore.Tests.TestDomain;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = new();
}