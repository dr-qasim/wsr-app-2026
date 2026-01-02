namespace VendingService.API.Models;

public sealed class Sale
{
    public int SaleId { get; set; }

    public int VendingMachineId { get; set; }
    public VendingMachine? VendingMachine { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime SoldAt { get; set; }

    public int SalePaymentMethodId { get; set; }
    public SalePaymentMethod? SalePaymentMethod { get; set; }
}

