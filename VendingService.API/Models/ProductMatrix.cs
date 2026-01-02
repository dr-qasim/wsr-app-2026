namespace VendingService.API.Models;

public sealed class ProductMatrix
{
    public int ProductMatrixId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

