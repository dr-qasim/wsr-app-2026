namespace VendingService.WPF.Contracts.VendingMachineProducts;

public sealed record VendingMachineProductItem(
    int VendingMachineId,
    int ProductId,
    string ProductName,
    int QuantityOnHand,
    int MinimumStock,
    decimal? AverageDailySales,
    DateTime UpdatedAt);
