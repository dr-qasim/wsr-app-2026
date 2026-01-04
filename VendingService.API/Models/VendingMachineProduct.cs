namespace VendingService.API.Models;

public sealed class VendingMachineProduct
{
    public int VendingMachineId { get; set; }
    public VendingMachine? VendingMachine { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int QuantityOnHand { get; set; }
    public int MinimumStock { get; set; }
    public decimal? AverageDailySales { get; set; }
    public DateTime UpdatedAt { get; set; }
}
