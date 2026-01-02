namespace VendingService.API.Models;

public sealed class SalePaymentMethod
{
    public int SalePaymentMethodId { get; set; }
    public required string Name { get; set; }
}

