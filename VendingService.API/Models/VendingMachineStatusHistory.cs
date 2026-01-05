namespace VendingService.API.Models;

public sealed class VendingMachineStatusHistory
{
    public int VendingMachineStatusHistoryId { get; set; }

    public int VendingMachineId { get; set; }
    public VendingMachine? VendingMachine { get; set; }

    public int VendingMachineStatusId { get; set; }
    public VendingMachineStatus? VendingMachineStatus { get; set; }

    public int? ChangedByUserAccountId { get; set; }
    public UserAccount? ChangedByUserAccount { get; set; }

    public DateTime ChangedAt { get; set; }
}
