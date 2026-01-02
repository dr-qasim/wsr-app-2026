namespace VendingService.API.Models;

public sealed class Product
{
    public int ProductId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}

