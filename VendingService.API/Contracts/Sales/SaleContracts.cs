using System.ComponentModel.DataAnnotations;

namespace VendingService.API.Contracts.Sales;

public sealed record SaleListItem(
    int SaleId,
    int VendingMachineId,
    string VendingMachineName,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal TotalAmount,
    DateTime SoldAt,
    int SalePaymentMethodId,
    string SalePaymentMethodName);

public sealed record SaleDetails(
    int SaleId,
    int VendingMachineId,
    string VendingMachineName,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal TotalAmount,
    DateTime SoldAt,
    int SalePaymentMethodId,
    string SalePaymentMethodName);

public sealed record CreateSaleRequest(
    [Required] int VendingMachineId,
    [Required] int ProductId,
    [Range(1, int.MaxValue)] int Quantity,
    [Range(0, double.MaxValue)] decimal TotalAmount,
    [Required] DateTime SoldAt,
    [Required] int SalePaymentMethodId);
