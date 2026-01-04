using System.ComponentModel.DataAnnotations;

namespace VendingService.API.Contracts.VendingMachineProducts;

public sealed record VendingMachineProductItem(
    int VendingMachineId,
    int ProductId,
    string ProductName,
    int QuantityOnHand,
    int MinimumStock,
    decimal? AverageDailySales,
    DateTime UpdatedAt);

public sealed record UpsertVendingMachineProductRequest(
    [Range(0, int.MaxValue)] int QuantityOnHand,
    [Range(0, int.MaxValue)] int MinimumStock,
    [Range(0, double.MaxValue)] decimal? AverageDailySales);
