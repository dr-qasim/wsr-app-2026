namespace VendingService.WPF.Contracts.Sales;

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
