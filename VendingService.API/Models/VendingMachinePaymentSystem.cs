namespace VendingService.API.Models;

public sealed class VendingMachinePaymentSystem
{
    public int VendingMachineId { get; set; }
    public VendingMachine? VendingMachine { get; set; }

    public int PaymentSystemId { get; set; }
    public PaymentSystem? PaymentSystem { get; set; }
}

